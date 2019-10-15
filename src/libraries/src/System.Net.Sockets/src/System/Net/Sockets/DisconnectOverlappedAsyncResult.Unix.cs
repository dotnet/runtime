// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    // DisconnectOverlappedAsyncResult - used to take care of storage for async Socket BeginDisconnect call.
    internal sealed partial class DisconnectOverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        internal void PostCompletion(SocketError errorCode) => CompletionCallback(0, errorCode);
    }
}
