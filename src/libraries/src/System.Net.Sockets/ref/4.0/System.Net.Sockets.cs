// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

// This file was hand edited to reflect the proposed contract for System.Net.Sockets 4.0.0.0.
// For questions please contact nclteam

namespace System.Net.Sockets
{
    public enum ProtocolType
    {
        Tcp = 6,
        Udp = 17,
        Unknown = -1,
        Unspecified = 0,
    }

    public partial class Socket : System.IDisposable
    {
        public Socket(System.Net.Sockets.AddressFamily addressFamily, System.Net.Sockets.SocketType socketType, System.Net.Sockets.ProtocolType protocolType) { }
        public Socket(System.Net.Sockets.SocketType socketType, System.Net.Sockets.ProtocolType protocolType) { }
        public System.Net.Sockets.AddressFamily AddressFamily { get { return default(System.Net.Sockets.AddressFamily); } }
        public bool Connected { get { return default(bool); } }
        public System.Net.EndPoint LocalEndPoint { get { return default(System.Net.EndPoint); } }
        public bool NoDelay { get { return default(bool); } set { } }
        public static bool OSSupportsIPv4 { get { return default(bool); } }
        public static bool OSSupportsIPv6 { get { return default(bool); } }
        public System.Net.Sockets.ProtocolType ProtocolType { get { return default(System.Net.Sockets.ProtocolType); } }
        public int ReceiveBufferSize { get { return default(int); } set { } }
        public System.Net.EndPoint RemoteEndPoint { get { return default(System.Net.EndPoint); } }
        public int SendBufferSize { get { return default(int); } set { } }
        public short Ttl { get { return default(short); } set { } }
        public bool AcceptAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public void Bind(System.Net.EndPoint localEP) { }
        public static void CancelConnectAsync(System.Net.Sockets.SocketAsyncEventArgs e) { }
        public bool ConnectAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public static bool ConnectAsync(System.Net.Sockets.SocketType socketType, System.Net.Sockets.ProtocolType protocolType, System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~Socket() { }
        public void Listen(int backlog) { }
        public bool ReceiveAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public bool ReceiveFromAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public bool SendAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public bool SendToAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public void Shutdown(System.Net.Sockets.SocketShutdown how) { }
    }
    public partial class SocketAsyncEventArgs : System.EventArgs, System.IDisposable
    {
        public SocketAsyncEventArgs() { }
        public System.Net.Sockets.Socket AcceptSocket { get { return default(System.Net.Sockets.Socket); } set { } }
        public byte[] Buffer { get { return default(byte[]); } }
        public System.Collections.Generic.IList<System.ArraySegment<byte>> BufferList { get { return default(System.Collections.Generic.IList<System.ArraySegment<byte>>); } set { } }
        public int BytesTransferred { get { return default(int); } }
        public System.Exception ConnectByNameError { get { return default(System.Exception); } }
        public System.Net.Sockets.Socket ConnectSocket { get { return default(System.Net.Sockets.Socket); } }
        public int Count { get { return default(int); } }
        public System.Net.Sockets.SocketAsyncOperation LastOperation { get { return default(System.Net.Sockets.SocketAsyncOperation); } }
        public int Offset { get { return default(int); } }
        public System.Net.EndPoint RemoteEndPoint { get { return default(System.Net.EndPoint); } set { } }
        public System.Net.Sockets.SocketError SocketError { get { return default(System.Net.Sockets.SocketError); } set { } }
        public object UserToken { get { return default(object); } set { } }
        public event System.EventHandler<System.Net.Sockets.SocketAsyncEventArgs> Completed { add { } remove { } }
        public void Dispose() { }
        ~SocketAsyncEventArgs() { }
        protected virtual void OnCompleted(System.Net.Sockets.SocketAsyncEventArgs e) { }
        public void SetBuffer(byte[] buffer, int offset, int count) { }
        public void SetBuffer(int offset, int count) { }
    }
    public enum SocketAsyncOperation
    {
        Accept = 1,
        Connect = 2,
        None = 0,
        Receive = 4,
        ReceiveFrom = 5,
        Send = 7,
        SendTo = 9,
    }
    public enum SocketShutdown
    {
        Both = 2,
        Receive = 0,
        Send = 1,
    }
    public enum SocketType
    {
        Dgram = 2,
        Stream = 1,
        Unknown = -1,
    }
}
