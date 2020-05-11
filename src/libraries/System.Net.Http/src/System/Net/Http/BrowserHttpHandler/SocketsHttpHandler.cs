// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    public sealed class SocketsHttpHandler : HttpMessageHandler
    {
        public bool UseCookies
        {
            get => throw new PlatformNotSupportedException("Property UseCookies is not supported.");
            set => throw new PlatformNotSupportedException("Property UseCookies is not supported.");
        }

        [AllowNull]
        public CookieContainer CookieContainer
        {
            get => throw new PlatformNotSupportedException("Property CookieContainer is not supported.");
            set => throw new PlatformNotSupportedException("Property CookieContainer is not supported.");
        }

        public DecompressionMethods AutomaticDecompression
        {
            get => throw new PlatformNotSupportedException("Property AutomaticDecompression is not supported.");
            set => throw new PlatformNotSupportedException("Property AutomaticDecompression is not supported.");
        }

        public bool UseProxy
        {
            get => throw new PlatformNotSupportedException("Property UseProxy is not supported.");
            set => throw new PlatformNotSupportedException("Property UseProxy is not supported.");
        }

        public IWebProxy? Proxy
        {
            get => throw new PlatformNotSupportedException("Property Proxy is not supported.");
            set => throw new PlatformNotSupportedException("Property Proxy is not supported.");
        }

        public ICredentials? DefaultProxyCredentials
        {
            get => throw new PlatformNotSupportedException("Property Credentials is not supported.");
            set => throw new PlatformNotSupportedException("Property Credentials is not supported.");
        }

        public bool PreAuthenticate
        {
            get => throw new PlatformNotSupportedException("Property PreAuthenticate is not supported.");
            set => throw new PlatformNotSupportedException("Property PreAuthenticate is not supported.");
        }

        public ICredentials? Credentials
        {
            get => throw new PlatformNotSupportedException("Property Credentials is not supported.");
            set => throw new PlatformNotSupportedException("Property Credentials is not supported.");
        }

        public bool AllowAutoRedirect
        {
            get => throw new PlatformNotSupportedException("Property AllowAutoRedirect is not supported.");
            set => throw new PlatformNotSupportedException("Property AllowAutoRedirect is not supported.");
        }

        public int MaxAutomaticRedirections
        {
            get => throw new PlatformNotSupportedException("Property MaxAutomaticRedirections is not supported.");
            set => throw new PlatformNotSupportedException("Property MaxAutomaticRedirections is not supported.");
        }

        public int MaxConnectionsPerServer
        {
            get => throw new PlatformNotSupportedException("Property MaxConnectionsPerServer is not supported.");
            set => throw new PlatformNotSupportedException("Property MaxConnectionsPerServer is not supported.");
        }

        public int MaxResponseDrainSize
        {
            get => throw new PlatformNotSupportedException("Property MaxResponseDrainSize is not supported.");
            set => throw new PlatformNotSupportedException("Property MaxResponseDrainSize is not supported.");
        }

        public TimeSpan ResponseDrainTimeout
        {
            get => throw new PlatformNotSupportedException("Property ResponseDrainTimeout is not supported.");
            set => throw new PlatformNotSupportedException("Property ResponseDrainTimeout is not supported.");
        }

        public int MaxResponseHeadersLength
        {
            get => throw new PlatformNotSupportedException("Property MaxResponseHeadersLength is not supported.");
            set => throw new PlatformNotSupportedException("Property MaxResponseHeadersLength is not supported.");
        }

        [AllowNull]
        public SslClientAuthenticationOptions SslOptions
        {
            get => throw new PlatformNotSupportedException("Property SslOptions is not supported.");
            set => throw new PlatformNotSupportedException("Property SslOptions is not supported.");
        }

        public TimeSpan PooledConnectionLifetime
        {
            get => throw new PlatformNotSupportedException("Property PooledConnectionLifetime is not supported.");
            set => throw new PlatformNotSupportedException("Property PooledConnectionLifetime is not supported.");
        }

        public TimeSpan PooledConnectionIdleTimeout
        {
            get => throw new PlatformNotSupportedException("Property PooledConnectionLifetime is not supported.");
            set => throw new PlatformNotSupportedException("Property PooledConnectionLifetime is not supported.");
        }

        public TimeSpan ConnectTimeout
        {
            get => throw new PlatformNotSupportedException("Property ConnectTimeout is not supported.");
            set => throw new PlatformNotSupportedException("Property ConnectTimeout is not supported.");
        }

        public TimeSpan Expect100ContinueTimeout
        {
            get => throw new PlatformNotSupportedException("Property Expect100ContinueTimeout is not supported.");
            set => throw new PlatformNotSupportedException("Property Expect100ContinueTimeout is not supported.");
        }

        public IDictionary<string, object?> Properties => throw new PlatformNotSupportedException("Property Properties is not supported.");

        protected internal override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) => throw new PlatformNotSupportedException("Method SendAsync is not supported.");
    }
}
