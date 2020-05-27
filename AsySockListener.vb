Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

'Copyright Salvatore Novelli
'salvatore.novelli@tiscali.it

' State object for reading client data asynchronously

Public Class StateObject
    ' Client socket.
    Public workSocket As Socket = Nothing
    ' Size of receive buffer.
    Public Shared BufferSize As Integer = 256
    ' Receive buffer.
    Public buffer(256) As Byte
    ' Received data string.
    Public sb As New StringBuilder()
End Class 'StateObject

Public Class AsynchronousSocketListener

    Private Shared m_mainSocket As Socket
    Private Shared m_activeSockets() As Socket
    Private Shared m_clientCount As Integer = 0

    Shared m_iPort As Integer
    Shared m_strIPAddress As String

    Public Shared Event logEntry(ByVal strData As String)
    Public Shared Event dataRecived(ByVal buffer() As Byte, ByVal bytesRecived As Integer)
    Public Shared Event dataSent(ByVal buffer() As Byte, ByVal bytesSent As Integer)
    Public Shared Event clientNotReachable(ByVal sSocket As Socket)

    Public Shared m_SendTimeout As Integer
    Public Shared m_fNoDelay As Boolean

    Public Sub New(ByVal strIPAddress As String, ByVal iPort As Integer)

        m_iPort = iPort
        m_strIPAddress = strIPAddress

    End Sub
    Public Sub Start()
        StartListening()
    End Sub
    Private Shared Sub StartListening()

        ' Data buffer for incoming data.
        Dim bytes() As Byte = New [Byte](1024) {}

        Dim localEndPoint As New IPEndPoint(IPAddress.Any, m_iPort)

        ' Intializes a TCP/IP socket.
        m_mainSocket = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

        m_mainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, CInt(m_SendTimeout))
        m_mainSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, m_fNoDelay)

        ' Bind the socket to the local endpoint and listen for incoming 
        ' connections.
        Try
            m_mainSocket.Bind(localEndPoint)
            m_mainSocket.Listen(4)

            'Console.WriteLine("Waiting for a connection...")
            RaiseEvent logEntry("Waiting for a connection...")

            m_mainSocket.BeginAccept(New AsyncCallback(AddressOf onClientConnect), 0)

        Catch e As Exception
            'Console.WriteLine(e.ToString())
            RaiseEvent logEntry(e.ToString())
        End Try

    End Sub
    Public Shared Sub onClientConnect(ByVal ar As IAsyncResult)

        Try
            ReDim Preserve m_activeSockets(0 To m_clientCount + 1)

            m_activeSockets(m_clientCount) = m_mainSocket.EndAccept(ar)

            'Console.WriteLine("Client connected!")
            RaiseEvent logEntry("Client connected: " + m_activeSockets(m_clientCount).RemoteEndPoint.ToString)

            WaitForData(m_activeSockets(m_clientCount))

            m_clientCount = m_clientCount + 1

            m_mainSocket.BeginAccept(New AsyncCallback(AddressOf onClientConnect), 0)

        Catch ex As Exception
            RaiseEvent logEntry(ex.ToString)
        End Try

    End Sub
    Private Shared Sub WaitForData(ByVal sSocket As Socket)
        Try
            ' Create the state object.
            Dim state As New StateObject()
            state.workSocket = sSocket

            If sSocket.Connected Then
                sSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, New AsyncCallback(AddressOf onDataReceived), state)
            Else
                RaiseEvent clientNotReachable(sSocket)
            End If

        Catch ex As Exception

            If sSocket.Connected Then
                RaiseEvent logEntry(ex.ToString)
            Else
                RaiseEvent clientNotReachable(sSocket)
            End If

        End Try
    End Sub
    Public Shared Sub onDataReceived(ByVal ar As IAsyncResult)

        Dim state As StateObject = CType(ar.AsyncState, StateObject)
        Dim handler As Socket = state.workSocket
        Try

            Dim content As [String] = [String].Empty

            ' Retrieve the state object and the handler socket
            ' from the asynchronous state object.

            ' Read data from client socket. 
            Dim bytesRead As Integer
            If handler.Connected Then

                bytesRead = handler.EndReceive(ar)

            Else
                RaiseEvent clientNotReachable(handler)
                Exit Sub
            End If

            If bytesRead > 0 Then

                content = Encoding.ASCII.GetString(state.buffer, 0, bytesRead)

                'Console.Write(content)
                RaiseEvent dataRecived(state.buffer, bytesRead)

            End If

            WaitForData(handler)
        Catch ex As Exception
            If (handler.Connected) Then
                RaiseEvent logEntry(ex.ToString)
            Else
                RaiseEvent clientNotReachable(handler)
            End If
        End Try
    End Sub

    Public Sub StopListening()
        If m_mainSocket.Connected Then
            m_mainSocket.Close()
        End If
    End Sub

    Public Sub SendData(ByVal buffer() As Byte, ByVal bytesToWrite As Integer)
        If (m_clientCount > 0) Then
            Send(buffer, bytesToWrite)
        Else
            RaiseEvent logEntry("Socket: Unabel to send data, no clients connected!")
        End If
    End Sub

    Private Shared Sub Send(ByVal buffer() As Byte, ByVal bytesToWrite As Integer)
        ' Convert the string data to byte data using ASCII encoding.ù

        Dim curSocket As Socket

        curSocket = Nothing

        Try

            For Each socket As Socket In m_activeSockets
                curSocket = socket
                If (Not IsNothing(socket)) Then
                    If (socket.Connected) Then

                        socket.Send(buffer, bytesToWrite, SocketFlags.None)

                        RaiseEvent dataSent(buffer, bytesToWrite)
                    Else
                        RaiseEvent clientNotReachable(curSocket)
                    End If
                End If
            Next

        Catch ex As Exception
            If (curSocket.Connected) Then
                RaiseEvent logEntry(ex.ToString)
            Else
                RaiseEvent clientNotReachable(curSocket)
            End If
        End Try

    End Sub

    Private Sub AsynchronousSocketListener_clientNotReachable(ByVal sSocket As Socket) Handles Me.clientNotReachable
        RaiseEvent logEntry("Client not reachable: " + sSocket.RemoteEndPoint.ToString)
    End Sub
End Class 