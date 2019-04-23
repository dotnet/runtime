// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.IO.Ports
{
    public enum Handshake
    {
        None = 0,
        XOnXOff = 1,
        RequestToSend = 2,
        RequestToSendXOnXOff = 3,
    }
    public enum Parity
    {
        None = 0,
        Odd = 1,
        Even = 2,
        Mark = 3,
        Space = 4,
    }
    public enum SerialData
    {
        Chars = 1,
        Eof = 2,
    }
    public partial class SerialDataReceivedEventArgs : System.EventArgs
    {
        internal SerialDataReceivedEventArgs() { }
        public System.IO.Ports.SerialData EventType { get { throw null; } }
    }
    public delegate void SerialDataReceivedEventHandler(object sender, System.IO.Ports.SerialDataReceivedEventArgs e);
    public enum SerialError
    {
        RXOver = 1,
        Overrun = 2,
        RXParity = 4,
        Frame = 8,
        TXFull = 256,
    }
    public partial class SerialErrorReceivedEventArgs : System.EventArgs
    {
        internal SerialErrorReceivedEventArgs() { }
        public System.IO.Ports.SerialError EventType { get { throw null; } }
    }
    public delegate void SerialErrorReceivedEventHandler(object sender, System.IO.Ports.SerialErrorReceivedEventArgs e);
    public enum SerialPinChange
    {
        CtsChanged = 8,
        DsrChanged = 16,
        CDChanged = 32,
        Break = 64,
        Ring = 256,
    }
    public partial class SerialPinChangedEventArgs : System.EventArgs
    {
        internal SerialPinChangedEventArgs() { }
        public System.IO.Ports.SerialPinChange EventType { get { throw null; } }
    }
    public delegate void SerialPinChangedEventHandler(object sender, System.IO.Ports.SerialPinChangedEventArgs e);
    public partial class SerialPort : System.ComponentModel.Component
    {
        public const int InfiniteTimeout = -1;
        public SerialPort() { }
        public SerialPort(System.ComponentModel.IContainer container) { }
        public SerialPort(string portName) { }
        public SerialPort(string portName, int baudRate) { }
        public SerialPort(string portName, int baudRate, System.IO.Ports.Parity parity) { }
        public SerialPort(string portName, int baudRate, System.IO.Ports.Parity parity, int dataBits) { }
        public SerialPort(string portName, int baudRate, System.IO.Ports.Parity parity, int dataBits, System.IO.Ports.StopBits stopBits) { }
        public System.IO.Stream BaseStream { get { throw null; } }
        public int BaudRate { get { throw null; } set { } }
        public bool BreakState { get { throw null; } set { } }
        public int BytesToRead { get { throw null; } }
        public int BytesToWrite { get { throw null; } }
        public bool CDHolding { get { throw null; } }
        public bool CtsHolding { get { throw null; } }
        public int DataBits { get { throw null; } set { } }
        public bool DiscardNull { get { throw null; } set { } }
        public bool DsrHolding { get { throw null; } }
        public bool DtrEnable { get { throw null; } set { } }
        public System.Text.Encoding Encoding { get { throw null; } set { } }
        public System.IO.Ports.Handshake Handshake { get { throw null; } set { } }
        public bool IsOpen { get { throw null; } }
        public string NewLine { get { throw null; } set { } }
        public System.IO.Ports.Parity Parity { get { throw null; } set { } }
        public byte ParityReplace { get { throw null; } set { } }
        public string PortName { get { throw null; } set { } }
        public int ReadBufferSize { get { throw null; } set { } }
        public int ReadTimeout { get { throw null; } set { } }
        public int ReceivedBytesThreshold { get { throw null; } set { } }
        public bool RtsEnable { get { throw null; } set { } }
        public System.IO.Ports.StopBits StopBits { get { throw null; } set { } }
        public int WriteBufferSize { get { throw null; } set { } }
        public int WriteTimeout { get { throw null; } set { } }
        public event System.IO.Ports.SerialDataReceivedEventHandler DataReceived { add { } remove { } }
        public event System.IO.Ports.SerialErrorReceivedEventHandler ErrorReceived { add { } remove { } }
        public event System.IO.Ports.SerialPinChangedEventHandler PinChanged { add { } remove { } }
        public void Close() { }
        public void DiscardInBuffer() { }
        public void DiscardOutBuffer() { }
        protected override void Dispose(bool disposing) { }
        public static string[] GetPortNames() { throw null; }
        public void Open() { }
        public int Read(byte[] buffer, int offset, int count) { throw null; }
        public int Read(char[] buffer, int offset, int count) { throw null; }
        public int ReadByte() { throw null; }
        public int ReadChar() { throw null; }
        public string ReadExisting() { throw null; }
        public string ReadLine() { throw null; }
        public string ReadTo(string value) { throw null; }
        public void Write(byte[] buffer, int offset, int count) { }
        public void Write(char[] buffer, int offset, int count) { }
        public void Write(string text) { }
        public void WriteLine(string text) { }
    }
    public enum StopBits
    {
        None = 0,
        One = 1,
        Two = 2,
        OnePointFive = 3,
    }
}
