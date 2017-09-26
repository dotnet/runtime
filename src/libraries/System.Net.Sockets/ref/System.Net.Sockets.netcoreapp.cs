// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Net.Sockets
{
    public partial class Socket : System.IDisposable
    {
        public int Receive(Span<byte> buffer) { throw null; }
        public int Receive(Span<byte> buffer, System.Net.Sockets.SocketFlags socketFlags) { throw null; }
        public int Receive(Span<byte> buffer, System.Net.Sockets.SocketFlags socketFlags, out System.Net.Sockets.SocketError errorCode) { throw null; }
        public int Send(ReadOnlySpan<byte> buffer) { throw null; }
        public int Send(ReadOnlySpan<byte> buffer, System.Net.Sockets.SocketFlags socketFlags) { throw null; }
        public int Send(ReadOnlySpan<byte> buffer, System.Net.Sockets.SocketFlags socketFlags, out System.Net.Sockets.SocketError errorCode) { throw null; }
    }
}
