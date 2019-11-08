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
        public QuicConnection(IPEndPoint remoteEndPoint, System.Net.Security.SslClientAuthenticationOptions sslClientAuthenticationOptions, IPEndPoint localEndPoint = null, System.Net.Quic.Implementations.QuicImplementationProvider implementationProvider = null) { }
    }
    public sealed partial class QuicListener : IDisposable
    {
        public QuicListener(IPEndPoint listenEndPoint, System.Net.Security.SslServerAuthenticationOptions sslServerAuthenticationOptions, System.Net.Quic.Implementations.QuicImplementationProvider implementationProvider = null) { }
    }
    public static class QuicImplementationProviders
    {
        public static System.Net.Quic.Implementations.QuicImplementationProvider Mock { get { throw null; } }
    }
}
namespace System.Net.Quic.Implementations
{ 
    public abstract class QuicImplementationProvider
    {
        internal QuicImplementationProvider() { }
    }
}
