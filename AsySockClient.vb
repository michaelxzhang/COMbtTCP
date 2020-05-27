Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.Text

'Copyright Salvatore Novelli
'salvatore.novelli@tiscali.it

Public Class AsynchronousClient

    Private Shared m_Socket As Socket

    Public Shared Connected As Boolean = False
    Shared m_iPort As Integer
    Shared m_strIPAddress As String

    Public Shared Event logEntry(ByVal strData As String)
    Public Shared Event dataRecived(ByVal buffer() As Byte, ByVal bytesRecived As Integer)
    Public Shared Event dataSent(ByVal buffer() As Byte, ByVal bytesSent As Integer)
    Public Shared Event serverNotReachable(ByVal sSocket As Socket)

    Public Shared m_SendTimeout As Integer = 10000
    Public Shared m_fNoDelay As Boolean = True

    Public Sub RetryConnection()
        StartClient()
    End Sub

    Public Sub New(ByVal strIPAddress As String, ByVal iPort As Integer)

        m_iPort = iPort
        m_strIPAddress = strIPAddress

    End Sub

    Public Sub Start()
        StartClient()
    End Sub

    Public Sub Close()
        If m_Socket.Connected Then
            m_Socket.Shutdown(SocketShutdown.Both)
            m_Socket.Close()
        End If
    End Sub

    Private Sub internalConsoleEntry(ByVal strText As String) Handles Me.logEntry

        'Console.WriteLine(strText)

    End Sub
    Private Shared Sub StartClient()
        ' Connect to a remote device.
        Try
            ' Establish the remote endpoint for the socket.
            ' The name of the
            ' remote device is "host.contoso.com".

            '  Create a TCP/IP socket.
            m_Socket = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

            m_Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, CInt(m_SendTimeout))
            m_Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, m_fNoDelay)

            ' Connect to the remote endpoint.

            RaiseEvent logEntry("Attempting connection...")
            'SocketOptionName.AcceptConnection = SocketOptionName.Linger
            m_Socket.BeginConnect(m_strIPAddress, m_iPort, AddressOf onConnectSuccesfull, m_Socket)

            If m_Socket.Connected Then
                Connected = True
            Else
                Connected = False
            End If

        Catch e As Exception
            RaiseEvent logEntry(e.ToString())
        End Try
    End Sub 'StartClient
    Private Shared Sub onConnectSuccesfull(ByVal ar As IAsyncResult)

        Dim client As Socket = CType(ar.AsyncState, Socket)

        Try
            ' Retrieve the socket from the state object.

            ' Complete the connection.
            client.EndConnect(ar)

            RaiseEvent logEntry("Socket connected to " + client.RemoteEndPoint.ToString())
            WaitForData()

            ' Signal that the connection has been made.

        Catch e As Exception
            If client.Connected Then
                RaiseEvent logEntry(e.ToString())
            Else
                RaiseEvent serverNotReachable(client)
            End If
        End Try
    End Sub

    Private Shared Sub WaitForData()
        Try

            ' Create the state object.
            Dim state As New StateObject()
            state.workSocket = m_Socket

            If m_Socket.Connected Then
                m_Socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, New AsyncCallback(AddressOf onDataReceived), state)
            Else
                RaiseEvent serverNotReachable(m_Socket)
            End If

        Catch ex As Exception

            If m_Socket.Connected Then
                RaiseEvent logEntry(ex.ToString)
            Else
                RaiseEvent serverNotReachable(m_Socket)
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
                RaiseEvent serverNotReachable(handler)
                Exit Sub
            End If

            If bytesRead > 0 Then

                content = Encoding.ASCII.GetString(state.buffer, 0, bytesRead)

                'Console.Write(content)
                RaiseEvent dataRecived(state.buffer, bytesRead)

            End If

            WaitForData()
        Catch ex As Exception
            If (handler.Connected) Then
                RaiseEvent logEntry(ex.ToString)
            Else
                RaiseEvent serverNotReachable(handler)
            End If
        End Try
    End Sub

    Public Sub SendData(ByVal buffer() As Byte, ByVal bytesToWrite As Integer)
        If m_Socket.Connected Then
            Send(buffer, bytesToWrite)
        Else
            RaiseEvent logEntry("Socket: Unabel to send data, no connection to server!")

        End If
    End Sub
    Private Shared Sub Send(ByVal buffer() As Byte, ByVal bytesToWrite As Integer)
        ' Convert the string data to byte data using ASCII encoding.

        Dim curSocket As Socket

        curSocket = Nothing

        Try

            If (m_Socket.Connected) Then
                m_Socket.Send(buffer, bytesToWrite, SocketFlags.None)
                RaiseEvent dataSent(buffer, bytesToWrite)
            Else
                RaiseEvent serverNotReachable(curSocket)
            End If

        Catch ex As Exception
            If (curSocket.Connected) Then
                RaiseEvent logEntry(ex.ToString)
            Else
                RaiseEvent serverNotReachable(curSocket)
            End If
        End Try

    End Sub

End Class 'AsynchronousClient