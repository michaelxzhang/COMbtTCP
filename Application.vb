Imports System.IO.Ports


'Copyright Salvatore Novelli
'salvatore.novelli@tiscali.it
Public Class Form

    Delegate Sub ConsolleWriteDelegate(ByVal [text] As String)
    Delegate Sub RXDataTBWriteDelegate(ByVal [val] As Integer)
    Delegate Sub TXDataTBWriteDelegate(ByVal [val] As Integer)

    Private WithEvents sockServer As AsynchronousSocketListener
    Private WithEvents sockClient As AsynchronousClient
    Private WithEvents serialPort As New SerialPort

    Private lastConsoleMsg As String = ""
    Private lastConsoleMsgTime As Date
    Private m_AppConfig As AppSettings

    Private m_fConsole As Boolean = False

    Const WINDOW_HEIGHT_BIG = 576
    Const WINDOW_HEIGHT_SMALL = 303

    Private Shared fServer As Boolean

    Private m_TCP_SendTimeout As Integer = 3000
    Private m_TCP_fNoDelay As Boolean = True

    Private Sub Form_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        m_AppConfig = New AppSettings(AppSettings.Config.SharedFile)

        loadSerialPortInfo()

        LoadConfiguration()

        Dim strText As String = "Program Started"
        Dim time As String
        time = TimeOfDay.TimeOfDay.ToString()

        strText = time + ":   " + strText
        consoleWrite(strText)

        Me.Height = WINDOW_HEIGHT_SMALL
        Me.Width = 500

        BtnStop.Enabled = False

        LED_TX.Image = My.Resources.led_red
        LED_RX.Image = My.Resources.led_red

    End Sub

    Private Sub loadSerialPortInfo()

        ' Allow the user to set the appropriate properties.
        setPortNameCB()
        setBaudRateCB()
        setParityCB()
        setDataBitsCB()
        setStopBitsCB()
        setHandshakeCB()

    End Sub
    Private Sub setHandshakeCB()

        Dim s As String
        For Each s In [Enum].GetNames(GetType(Handshake))
            Me.HandshakeCB.Items.Add(s)
        Next s

        Me.HandshakeCB.SelectedText = [Enum].GetName(GetType(Handshake), Me.serialPort.Handshake)

    End Sub
    Private Sub setStopBitsCB()

        Dim s As String
        For Each s In [Enum].GetNames(GetType(StopBits))
            Me.StopBitsCB.Items.Add(s)
        Next s

        Me.StopBitsCB.SelectedText = [Enum].GetName(GetType(StopBits), Me.serialPort.StopBits)

    End Sub
    Private Sub setDataBitsCB()
        Me.DataBitsTB.Text = serialPort.DataBits.ToString
    End Sub
    Private Sub setParityCB()

        Dim s As String
        For Each s In [Enum].GetNames(GetType(Parity))
            Me.ParityCB.Items.Add(s)
        Next s

        Me.ParityCB.SelectedText = [Enum].GetName(GetType(Parity), Me.serialPort.Parity)

    End Sub
    Private Sub setBaudRateCB()

        Me.BaudRateTB.Text = serialPort.BaudRate.ToString

    End Sub
    Private Sub setPortNameCB()

        Dim s As String
        For Each s In serialPort.GetPortNames()
            Me.PortNameCB.Items.Add(s)

        Next s

        Me.PortNameCB.SelectedIndex = 0

    End Sub

    Private Sub Client_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        SaveCurrentConfiguration()

        disableForm()
        fServer = False
        sockClient = New AsynchronousClient(Me.ServerIPAddr.Text, CInt(Me.ServerPort.Text))

        AsynchronousClient.m_fNoDelay = m_TCP_fNoDelay
        AsynchronousClient.m_SendTimeout = m_TCP_SendTimeout

        sockClient.Start()

    End Sub
    Private Sub Server_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        SaveCurrentConfiguration()

        disableForm()
        fServer = True
        sockServer = New AsynchronousSocketListener(Me.ServerIPAddr.Text, CInt(Me.ServerPort.Text))

        AsynchronousSocketListener.m_fNoDelay = m_TCP_fNoDelay
        AsynchronousSocketListener.m_SendTimeout = m_TCP_SendTimeout

        sockServer.Start()

    End Sub

    Private Sub SaveCurrentConfiguration()
        m_AppConfig.SaveSetting("IPAddress", Me.ServerIPAddr.Text)
        m_AppConfig.SaveSetting("Port", Me.ServerPort.Text)
        m_AppConfig.SaveSetting("Serial Port", Me.PortNameCB.Text)
        m_AppConfig.SaveSetting("Serial Port Baudrate", Me.BaudRateTB.Text)
        m_AppConfig.SaveSetting("Serial Port Databits", Me.DataBitsTB.Text)
        m_AppConfig.SaveSetting("Serial Port Parity", Me.ParityCB.Text)
        m_AppConfig.SaveSetting("Serial Port Stopbit", Me.StopBitsCB.Text)
        m_AppConfig.SaveSetting("Serial Port Handshake", Me.HandshakeCB.Text)
    End Sub

    Private Sub LoadConfiguration()
        Dim strTmp As String

        strTmp = m_AppConfig.GetSetting("IPAddress")


        If strTmp Is Nothing Then
            strTmp = "127.0.0.1"
        End If
        Me.ServerIPAddr.Text = strTmp

        strTmp = m_AppConfig.GetSetting("Port")
        If strTmp Is Nothing Then
            strTmp = "8000"
        End If
        Me.ServerPort.Text = strTmp

        strTmp = m_AppConfig.GetSetting("Serial Port")
        If strTmp IsNot Nothing Then
            Me.PortNameCB.Text = strTmp
        End If

        strTmp = m_AppConfig.GetSetting("Serial Port Baudrate")
        If strTmp IsNot Nothing Then
            Me.BaudRateTB.Text = strTmp
        End If

        strTmp = m_AppConfig.GetSetting("Serial Port Databits")
        If strTmp IsNot Nothing Then
            Me.DataBitsTB.Text = strTmp
        End If

        strTmp = m_AppConfig.GetSetting("Serial Port Parity")
        If strTmp IsNot Nothing Then
            Me.ParityCB.Text = strTmp
        End If

        strTmp = m_AppConfig.GetSetting("Serial Port Stopbit")
        If strTmp IsNot Nothing Then
            Me.StopBitsCB.Text = strTmp
        End If

        strTmp = m_AppConfig.GetSetting("Serial Port Handshake")
        If strTmp IsNot Nothing Then
            Me.HandshakeCB.Text = strTmp
        End If

    End Sub

    Private Sub addRXDataTBValue(ByVal dataRecived As Integer)
        If Me.InvokeRequired Then

            Dim d As New RXDataTBWriteDelegate(AddressOf addRXDataTBValue)
            Me.Invoke(d, New Object() {dataRecived})


        Else
            Me.RXDataTB.Text = CStr(CInt(Me.RXDataTB.Text) + dataRecived)
        End If
    End Sub
    Private Sub addTXDataTBValue(ByVal dataTransmitted As Integer)
        If Me.InvokeRequired Then

            Dim d As New TXDataTBWriteDelegate(AddressOf addTXDataTBValue)
            Me.Invoke(d, New Object() {dataTransmitted})


        Else
            Me.TXDataTB.Text = CStr(CInt(Me.TXDataTB.Text) + dataTransmitted)
        End If
    End Sub
    Private Sub consolleWriteline(ByVal strText As String)

        If strText <> lastConsoleMsg Then

            lastConsoleMsg = strText
            lastConsoleMsgTime = Now()
            consoleWriteEntry(strText)

        Else

            If (DateDiff(DateInterval.Second, lastConsoleMsgTime, Now) > 4) Then
                'la stessa cosa si può scrivere al massimo ogni 4 secondi
                consoleWriteEntry(strText)
                lastConsoleMsg = strText
                lastConsoleMsgTime = Now()
            Else
                consoleWrite(".")
            End If

        End If

    End Sub
    Private Sub consoleWriteEntry(ByVal strText As String)

        Dim time As String = TimeOfDay.TimeOfDay.ToString()
        strText = vbCrLf + time + ":   " + strText
        consoleWrite(strText)

    End Sub

    Private Sub consoleWrite(ByVal strText As String)

        If Me.InvokeRequired Then

            Dim d As New ConsolleWriteDelegate(AddressOf consoleWrite)
            Me.Invoke(d, New Object() {strText})

        Else
            Me.Consolle.AppendText(strText)
        End If

    End Sub

    Private Sub disableForm()
        Me.ServerIPAddr.Enabled = False
        Me.ServerPort.Enabled = False
    End Sub

    Private Sub enableForm()
        Me.ServerIPAddr.Enabled = True
        Me.ServerPort.Enabled = True
    End Sub

    Private Sub Form_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If (Not IsNothing(sockServer)) Then
            sockServer.StopListening()
            sockServer = Nothing
        End If
    End Sub
    Private Sub Form_Leave(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Leave

    End Sub

    Private Sub GetCOMPB_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Try

            Me.GroupBox2.Enabled = False
            serialPort.PortName = Me.PortNameCB.Text
            serialPort.BaudRate = CInt(Me.BaudRateTB.Text)
            serialPort.Parity = CType([Enum].Parse(GetType(Parity), Me.ParityCB.Text), Parity)
            serialPort.DataBits = CInt(Me.DataBitsTB.Text)
            serialPort.StopBits = CType([Enum].Parse(GetType(StopBits), Me.StopBitsCB.Text), StopBits)
            serialPort.Handshake = CType([Enum].Parse(GetType(Handshake), Me.HandshakeCB.Text), Handshake)
            ' Set the read/write timeouts
            serialPort.ReadTimeout = 500
            serialPort.WriteTimeout = 500

            serialPort.Open()
            consolleWriteline(serialPort.PortName + " correctly opened!")

        Catch ex As Exception

            consolleWriteline("Unable to open " + Me.PortNameCB.Text + ".")
            Me.GroupBox2.Enabled = True

        Finally

        End Try

    End Sub

    Private Sub serialPort_DataReceived(ByVal sender As Object, ByVal e As System.IO.Ports.SerialDataReceivedEventArgs) Handles serialPort.DataReceived

        Dim buffSize As Integer = 5000
        Dim buffer(buffSize) As Byte
        Dim index As Integer = 0

        While serialPort.BytesToRead > 0
            buffer(index) = serialPort.ReadByte()
            index = index + 1
        End While

        If fServer Then
            If Not IsNothing(sockServer) Then
                sockServer.SendData(buffer, index)
            Else
                consolleWriteline("COM Listener: Unable to forward data - socket not started!")
            End If
        Else
            If Not IsNothing(sockClient) Then
                sockClient.SendData(buffer, index)
            Else
                consolleWriteline("COM Listener: Unable to forward data - socket not started!")
            End If

        End If
    End Sub

    Private Sub sockServer_dataRecived(ByVal buffer() As Byte, ByVal bytesRecived As Integer) Handles sockServer.dataRecived
        'consolleWriteline("Recived: " + strData)

        addRXDataTBValue(bytesRecived)

        Try

            serialPort.Write(buffer, 0, bytesRecived)

        Catch ex As Exception

            If serialPort.IsOpen Then
                consolleWriteline(ex.ToString)
            Else
                consolleWriteline("SOCK: Unable to write on COM. The port is CLOSED.")
            End If

        End Try

    End Sub

    Private Sub sockServer_dataSent(ByVal buffer() As Byte, ByVal bytesSent As Integer) Handles sockServer.dataSent
        addTXDataTBValue(bytesSent)
    End Sub

    Private Sub sockServer_logEntry(ByVal strData As String) Handles sockServer.logEntry
        TimeOfDay.ToString()

        consolleWriteline(strData)
    End Sub

    Private Sub sockClient_dataRecived(ByVal buffer() As Byte, ByVal bytesRecived As Integer) Handles sockClient.dataRecived
        'consolleWriteline("Recived: " + strData)

        addRXDataTBValue(bytesRecived)
        Try

            serialPort.Write(buffer, 0, bytesRecived)

        Catch ex As Exception

            If serialPort.IsOpen Then
                consolleWriteline(ex.ToString)
            Else
                consolleWriteline("SOCK: Unable to write on COM. The port is CLOSED.")
            End If

        End Try
    End Sub

    Private Sub sockClient_dataSent(ByVal buffer() As Byte, ByVal bytesSent As Integer) Handles sockClient.dataSent
        addTXDataTBValue(bytesSent)
    End Sub

    Private Sub sockclient_logEntry(ByVal strData As String) Handles sockClient.logEntry
        TimeOfDay.ToString()

        consolleWriteline(strData)
    End Sub

    Private Sub sockClient_serverNotReachable(ByVal sSocket As System.Net.Sockets.Socket) Handles sockClient.serverNotReachable
        consolleWriteline("Server NOT Reachable")
        'sockClient.RetryConnection()
    End Sub

    Private Sub InfoToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MainMenu_Info.Click
        Dim ab As New AboutBox
        ab.Show()
    End Sub

    Private Sub ConsolePB_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ConsolePB.Click

        If m_fConsole Then
            Me.Height = WINDOW_HEIGHT_SMALL
            Me.Width = 500

            Me.ConsolePB.Text = "Console >>"
        Else
            Me.Height = WINDOW_HEIGHT_BIG
            Me.Width = 500
            Me.ConsolePB.Text = "Console <<"
        End If

        m_fConsole = Not m_fConsole

    End Sub

    Private Sub ExitToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ExitToolStripMenuItem.Click
        Dim settings As New TCPSettings

        settings.SendTimeoutTB.Text = m_TCP_SendTimeout
        settings.fNoDelay.Checked = m_TCP_fNoDelay

        settings.ShowDialog()

        If settings.DialogResult = Windows.Forms.DialogResult.OK Then

            m_TCP_SendTimeout = settings.SendTimeoutTB.Text
            m_TCP_fNoDelay = settings.fNoDelay.Checked

        End If

    End Sub

    Private Sub BtnStart_Click(sender As Object, e As EventArgs) Handles BtnStart.Click

        SaveCurrentConfiguration()

        disableForm()
        BtnStart.Enabled = False
        BtnStop.Enabled = True

        If Radio_Server.Checked Then

            fServer = True
            sockServer = New AsynchronousSocketListener(Me.ServerIPAddr.Text, CInt(Me.ServerPort.Text))

            AsynchronousSocketListener.m_fNoDelay = m_TCP_fNoDelay
            AsynchronousSocketListener.m_SendTimeout = m_TCP_SendTimeout

            sockServer.Start()
        ElseIf Radio_Client.Checked Then

            fServer = False
            sockClient = New AsynchronousClient(Me.ServerIPAddr.Text, CInt(Me.ServerPort.Text))

            AsynchronousClient.m_fNoDelay = m_TCP_fNoDelay
            AsynchronousClient.m_SendTimeout = m_TCP_SendTimeout

            sockClient.Start()
        End If

        Try

            Me.GroupBox2.Enabled = False
            serialPort.PortName = Me.PortNameCB.Text
            serialPort.BaudRate = CInt(Me.BaudRateTB.Text)
            serialPort.Parity = CType([Enum].Parse(GetType(Parity), Me.ParityCB.Text), Parity)
            serialPort.DataBits = CInt(Me.DataBitsTB.Text)
            serialPort.StopBits = CType([Enum].Parse(GetType(StopBits), Me.StopBitsCB.Text), StopBits)
            serialPort.Handshake = CType([Enum].Parse(GetType(Handshake), Me.HandshakeCB.Text), Handshake)
            ' Set the read/write timeouts
            serialPort.ReadTimeout = 500
            serialPort.WriteTimeout = 500

            serialPort.Open()
            consolleWriteline(serialPort.PortName + " correctly opened!")

        Catch ex As Exception

            consolleWriteline("Unable to open " + Me.PortNameCB.Text + ".")
            Me.GroupBox2.Enabled = True

        Finally

        End Try

    End Sub

    Private Sub BtnStop_Click(sender As Object, e As EventArgs) Handles BtnStop.Click
        If Radio_Server.Checked Then
            sockServer.StopListening()
        Else
            sockClient.Close()
        End If

        consolleWriteline("Socket closed.")

        serialPort.Close()
        consolleWriteline(serialPort.PortName + " closed!")

        enableForm()
        BtnStart.Enabled = True
        BtnStop.Enabled = False

        Me.GroupBox2.Enabled = True

        LED_TX.Image = My.Resources.led_red
        LED_RX.Image = My.Resources.led_red

    End Sub


    Private Sub Radio_Client_MouseClick(sender As Object, e As MouseEventArgs) Handles Radio_Client.MouseClick
        Radio_Server.Checked = False
    End Sub

    Private Sub Radio_Server_MouseClick(sender As Object, e As MouseEventArgs) Handles Radio_Server.MouseClick
        Radio_Client.Checked = False
    End Sub

    Private Sub RXDataTB_TextChanged(sender As Object, e As EventArgs) Handles RXDataTB.TextChanged
        LED_RX.Image = My.Resources.Resources.led_green
        TimerRx.Enabled = True
        'LED_TX.Image = My.Resources.Resources.led_red
    End Sub

    Private Sub RXDataTB_TurnoffLed(sender As Object, e As EventArgs) Handles TimerRx.Tick
        LED_RX.Image = My.Resources.Resources.led_red
        'LED_TX.Image = My.Resources.Resources.led_red
    End Sub

    Private Sub TXDataTB_TextChanged(sender As Object, e As EventArgs) Handles TXDataTB.TextChanged
        'LED_RX.Image = My.Resources.Resources.led_red
        LED_TX.Image = My.Resources.Resources.led_green
        TimerTx.Enabled = True
    End Sub

    Private Sub TXDataTB_TurnoffLed(sender As Object, e As EventArgs) Handles TimerTx.Tick
        'LED_RX.Image = My.Resources.Resources.led_red
        LED_TX.Image = My.Resources.Resources.led_red
    End Sub
End Class
