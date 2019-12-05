// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicImplementationProvider : QuicImplementationProvider
    {
        // TODO these "session" objects should probably be statics and only create one.
        // No need to create two different sessions for client and server.
        private MsQuicSession _clientSession = new MsQuicSession("client");
        private MsQuicSession _serverSession = new MsQuicSession("server");

        internal override QuicListenerProvider CreateListener(QuicListenerOptions options)
        {
            return _serverSession.ListenerOpen(options);
        }

        internal override QuicConnectionProvider CreateConnection(QuicClientConnectionOptions options)
        {
            return _clientSession.ConnectionOpen(options);
        }
    }
}
