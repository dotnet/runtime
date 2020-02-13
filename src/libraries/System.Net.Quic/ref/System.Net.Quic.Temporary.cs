// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System.Threading;

namespace System.Net.Quic
{
    public sealed partial class QuicConnection : System.IDisposable
    {
        public QuicConnection(System.Net.Quic.Implementations.QuicImplementationProvider implementationProvider, IPEndPoint remoteEndPoint, System.Net.Security.SslClientAuthenticationOptions sslClientAuthenticationOptions, IPEndPoint localEndPoint = null) { }
        public QuicConnection(System.Net.Quic.Implementations.QuicImplementationProvider implementationProvider, QuicClientConnectionOptions options) { }
    }
    public sealed partial class QuicListener : IDisposable
    {
        public QuicListener(System.Net.Quic.Implementations.QuicImplementationProvider implementationProvider, IPEndPoint listenEndPoint, System.Net.Security.SslServerAuthenticationOptions sslServerAuthenticationOptions) { }
        public QuicListener(System.Net.Quic.Implementations.QuicImplementationProvider implementationProvider, QuicListenerOptions option) { }
    }
    public static class QuicImplementationProviders
    {
        public static System.Net.Quic.Implementations.QuicImplementationProvider Mock { get { throw null; } }
        public static System.Net.Quic.Implementations.QuicImplementationProvider MsQuic { get { throw null; } }
        public static System.Net.Quic.Implementations.QuicImplementationProvider Default { get { throw null; } }
    }
}
namespace System.Net.Quic.Implementations
{ 
    public abstract class QuicImplementationProvider
    {
        internal QuicImplementationProvider() { }
    }
}
