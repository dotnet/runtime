// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Sockets
{
    // !!! DELETEME!!!
    internal partial class OverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        private Internals.SocketAddress? _socketAddress;

        internal OverlappedAsyncResult(Socket socket, object? asyncState, AsyncCallback? asyncCallback) :
            base(socket, asyncState, asyncCallback)
        {
        }

        internal Internals.SocketAddress? SocketAddress
        {
            get { return _socketAddress; }
            set { _socketAddress = value; }
        }
    }

    // !!! DELETEME!!!
    internal sealed class OriginalAddressOverlappedAsyncResult : OverlappedAsyncResult
    {
        internal OriginalAddressOverlappedAsyncResult(Socket socket, object? asyncState, AsyncCallback? asyncCallback) :
            base(socket, asyncState, asyncCallback)
        {
        }

        internal Internals.SocketAddress? SocketAddressOriginal { get; set; }
    }
}
