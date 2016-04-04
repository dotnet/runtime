// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class HttpClientHandler_SslProtocols_Test
    {
        [Fact]
        public void DefaultProtocols_MatchesExpected()
        {
            using (var handler = new HttpClientHandler())
            {
                const SslProtocols expectedDefault = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
                Assert.Equal(expectedDefault, handler.SslProtocols);
            }
        }

        [Theory]
        [InlineData(SslProtocols.Tls)]
        [InlineData(SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls11 | SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12)]
        public void SetGetProtocols_Roundtrips(SslProtocols protocols)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.SslProtocols = protocols;
                Assert.Equal(protocols, handler.SslProtocols);
            }
        }

        [Fact]
        public async Task SetProtcols_AfterRequest_ThrowsException()
        {
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                (await client.GetAsync(HttpTestServers.RemoteEchoServer)).Dispose();
                Assert.Throws<InvalidOperationException>(() => handler.SslProtocols = SslProtocols.Tls12);
            }
        }

        [Theory]
        [InlineData(SslProtocols.None)]
        [InlineData(~SslProtocols.None)]
#pragma warning disable 0618 // obsolete warning
        [InlineData(SslProtocols.Ssl2)]
        [InlineData(SslProtocols.Ssl3)]
        [InlineData(SslProtocols.Ssl2 | SslProtocols.Ssl3)]
        [InlineData(SslProtocols.Ssl2 | SslProtocols.Ssl3 | SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12)]
#pragma warning restore 0618
        public void DisabledProtocols_SetSslProtocols_ThrowsException(SslProtocols disabledProtocols)
        {
            using (var handler = new HttpClientHandler())
            {
                Assert.Throws<NotSupportedException>(() => handler.SslProtocols = disabledProtocols);
            }
        }

        [Theory]
        [InlineData(SslProtocols.Tls, HttpTestServers.TLSv10RemoteServer)]
        [InlineData(SslProtocols.Tls11, HttpTestServers.TLSv11RemoteServer)]
        [InlineData(SslProtocols.Tls12, HttpTestServers.TLSv12RemoteServer)]
        public async Task GetAsync_AllowedSSLVersion_Succeeds(SslProtocols acceptedProtocol, string url)
        {
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.SslProtocols = acceptedProtocol;
                using (await client.GetAsync(url)) { }
            }
        }

        [Theory]
        [InlineData(SslProtocols.Tls11, HttpTestServers.TLSv10RemoteServer)]
        [InlineData(SslProtocols.Tls12, HttpTestServers.TLSv11RemoteServer)]
        [InlineData(SslProtocols.Tls, HttpTestServers.TLSv12RemoteServer)]
        public async Task GetAsync_AllowedSSLVersionDiffersFromServer_ThrowsException(SslProtocols allowedProtocol, string url)
        {
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.SslProtocols = allowedProtocol;
                await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
            }
        }

        [Fact]
        public async Task GetAsync_DisallowTls10_AllowTls11_AllowTls12()
        {
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12;
                if (BackendSupportsFineGrainedProtocols)
                {
                    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(HttpTestServers.TLSv10RemoteServer));
                    using (await client.GetAsync(HttpTestServers.TLSv11RemoteServer)) { }
                    using (await client.GetAsync(HttpTestServers.TLSv12RemoteServer)) { }
                }
                else
                {
                    await Assert.ThrowsAsync<NotSupportedException>(() => client.GetAsync(HttpTestServers.TLSv11RemoteServer));
                }
            }
        }

        private static bool BackendSupportsFineGrainedProtocols =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
            (CurlSslVersionDescription()?.StartsWith("OpenSSL") ?? false);

        [DllImport("System.Net.Http.Native", EntryPoint = "HttpNative_GetSslVersionDescription")]
        private static extern string CurlSslVersionDescription();
    }
}
