// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

namespace System.Net.Http
{
    [UnsupportedOSPlatform("browser")]
    public sealed class SocketsHttpHandler : HttpMessageHandler
    {
        ///<summary>
        ///Gets a value that indicates whether the this Type is supported.
        ///<summary/>
        [UnsupportedOSPlatformGuard("browser")]
        public static bool IsSupported => false;

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public bool UseCookies
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        [AllowNull]
        public CookieContainer CookieContainer
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public DecompressionMethods AutomaticDecompression
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public bool UseProxy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public IWebProxy? Proxy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public ICredentials? DefaultProxyCredentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public bool PreAuthenticate
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public ICredentials? Credentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public bool AllowAutoRedirect
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public int MaxAutomaticRedirections
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public int MaxConnectionsPerServer
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public int MaxResponseDrainSize
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public TimeSpan ResponseDrainTimeout
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public int MaxResponseHeadersLength
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        [AllowNull]
        public SslClientAuthenticationOptions SslOptions
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public TimeSpan PooledConnectionLifetime
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public TimeSpan PooledConnectionIdleTimeout
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public TimeSpan ConnectTimeout
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public TimeSpan Expect100ContinueTimeout
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public int InitialHttp2StreamWindowSize
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public TimeSpan KeepAlivePingDelay
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public TimeSpan KeepAlivePingTimeout
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public HttpKeepAlivePingPolicy KeepAlivePingPolicy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public IDictionary<string, object?> Properties => throw new PlatformNotSupportedException();

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public HeaderEncodingSelector<HttpRequestMessage>? RequestHeaderEncodingSelector
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public HeaderEncodingSelector<HttpRequestMessage>? ResponseHeaderEncodingSelector
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        [CLSCompliant(false)]
        public DistributedContextPropagator? ActivityHeadersPropagator
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        protected internal override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) => throw new PlatformNotSupportedException();

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public bool EnableMultipleHttp2Connections
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>>? ConnectCallback
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        ///<summary>
        ///Throws a <see cref="PlatformNotSupportedException"/> exception in all cases.
        ///</summary>
        public Func<SocketsHttpPlaintextStreamFilterContext, CancellationToken, ValueTask<Stream>>? PlaintextStreamFilter
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }
    }
}
