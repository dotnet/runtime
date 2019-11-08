// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Security;

namespace System.Net.Quic.Implementations.Mock
{
    internal sealed class MockImplementationProvider : QuicImplementationProvider
    {
        internal override QuicListenerProvider CreateListener(IPEndPoint listenEndPoint, SslServerAuthenticationOptions sslServerAuthenticationOptions)
        {
            return new MockListener(listenEndPoint, sslServerAuthenticationOptions);
        }

        internal override QuicConnectionProvider CreateConnection(IPEndPoint remoteEndPoint, SslClientAuthenticationOptions sslClientAuthenticationOptions, IPEndPoint localEndPoint)
        {
            return new MockConnection(remoteEndPoint, sslClientAuthenticationOptions, localEndPoint);
        }
    }
}
