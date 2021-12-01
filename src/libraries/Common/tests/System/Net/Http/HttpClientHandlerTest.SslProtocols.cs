// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    public abstract partial class HttpClientHandler_SslProtocols_Test : HttpClientHandlerTestBase
    {
        public HttpClientHandler_SslProtocols_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void DefaultProtocols_MatchesExpected()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Equal(SslProtocols.None, handler.SslProtocols);
            }
        }

        [Theory]
        [InlineData(SslProtocols.None)]
        [InlineData(SslProtocols.Tls)]
        [InlineData(SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls11 | SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls12)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12)]
#if !NETFRAMEWORK
        [InlineData(SslProtocols.Tls13)]
        [InlineData(SslProtocols.Tls11 | SslProtocols.Tls13)]
        [InlineData(SslProtocols.Tls12 | SslProtocols.Tls13)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls13)]
        [InlineData(SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13)]
#endif
        public void SetGetProtocols_Roundtrips(SslProtocols protocols)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                handler.SslProtocols = protocols;
                Assert.Equal(protocols, handler.SslProtocols);
            }
        }

        [Fact]
        public async Task SetProtocols_AfterRequest_ThrowsException()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        server.AcceptConnectionSendResponseAndCloseAsync(),
                        client.GetAsync(url));
                });
                Assert.Throws<InvalidOperationException>(() => handler.SslProtocols = SslProtocols.Tls12);
            }
        }


        public static IEnumerable<object[]> GetAsync_AllowedSSLVersion_Succeeds_MemberData()
        {
            // These protocols are all enabled by default, so we can connect with them both when
            // explicitly specifying it in the client and when not.
            foreach (SslProtocols protocol in Enum.GetValues(typeof(SslProtocols)))
            {
                if (protocol != SslProtocols.None && (protocol & SslProtocolSupport.SupportedSslProtocols) == protocol)
                {
                    yield return new object[] { protocol, true };
#pragma warning disable 0618 // SSL2/3 are deprecated
                    // On certain platforms these are completely disabled and cannot be used at all.
                    if (protocol != SslProtocols.Ssl2 && protocol != SslProtocols.Ssl3)
                    {
                        yield return new object[] { protocol, false };
                    }
#pragma warning restore 0618
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAsync_AllowedSSLVersion_Succeeds_MemberData))]
        public async Task GetAsync_AllowedSSLVersion_Succeeds(SslProtocols acceptedProtocol, bool requestOnlyThisProtocol)
        {
            int count = 0;
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback =
                    (request, cert, chain, errors) => { count++; return true; };

                if (requestOnlyThisProtocol)
                {
                    handler.SslProtocols = acceptedProtocol;
                }
                else
                {
                    // Explicitly setting protocols clears implementation default
                    // restrictions on minimum TLS/SSL version
                    // We currently know that some platforms like Debian 10 OpenSSL
                    // will by default block < TLS 1.2
#pragma warning disable 0618 // SSL2/3 are deprecated
#if !NETFRAMEWORK
                    handler.SslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13;
#else
                    handler.SslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | (SslProtocols)12288;
#endif
#pragma warning restore 0618
                }

                // Use a different SNI for each connection to prevent TLS 1.3 renegotiation issue: https://github.com/dotnet/runtime/issues/47378
                client.DefaultRequestHeaders.Host = GetTestSNIName();

                var options = new LoopbackServer.Options { UseSsl = true, SslProtocols = acceptedProtocol };
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                      server.AcceptConnectionSendResponseAndCloseAsync(),
                      client.GetAsync(url));
                }, options);

                Assert.Equal(1, count);
            }

            string GetTestSNIName()
            {
                string name = $"{nameof(GetAsync_AllowedSSLVersion_Succeeds)}_{acceptedProtocol}_{requestOnlyThisProtocol}";
                if (PlatformDetection.IsAndroid)
                {
                    // Android does not support underscores in host names
                    name = name.Replace("_", string.Empty);
                }

                return name;
            }
        }

        public static IEnumerable<object[]> SupportedSSLVersionServers()
        {
#pragma warning disable 0618 // SSL2/3 are deprecated
            if (PlatformDetection.SupportsSsl3)
            {
                yield return new object[] { SslProtocols.Ssl3, Configuration.Http.SSLv3RemoteServer };
            }
#pragma warning restore 0618
            if (PlatformDetection.SupportsTls10)
            {
                yield return new object[] { SslProtocols.Tls, Configuration.Http.TLSv10RemoteServer };
            }

            if (PlatformDetection.SupportsTls11)
            {
                yield return new object[] { SslProtocols.Tls11, Configuration.Http.TLSv11RemoteServer };
            }

            if (PlatformDetection.SupportsTls12)
            {
                yield return new object[] { SslProtocols.Tls12, Configuration.Http.TLSv12RemoteServer };
            }
        }

        // We have tests that validate with SslStream, but that's limited by what the current OS supports.
        // This tests provides additional validation against an external server.
        [OuterLoop("Avoid www.ssllabs.com dependency in innerloop.")]
        [Theory]
        [MemberData(nameof(SupportedSSLVersionServers))]
        public async Task GetAsync_SupportedSSLVersion_Succeeds(SslProtocols sslProtocols, string url)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                handler.SslProtocols = sslProtocols;
                using (HttpClient client = CreateHttpClient(handler))
                {
                    (await RemoteServerQuery.Run(() => client.GetAsync(url), remoteServerExceptionWrapper, url)).Dispose();
                }
            }
        }

        public Func<Exception, bool> remoteServerExceptionWrapper = (exception) =>
        {
            Type exceptionType = exception.GetType();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // On linux, taskcanceledexception is thrown.
                return exceptionType.Equals(typeof(TaskCanceledException));
            }
            else
            {
                // The internal exceptions return operation timed out.
                return exceptionType.Equals(typeof(HttpRequestException)) && exception.InnerException.Message.Contains("timed out");
            }
        };

        public static IEnumerable<object[]> NotSupportedSSLVersionServers()
        {
#pragma warning disable 0618
            if (PlatformDetection.SupportsSsl2)
            {
                yield return new object[] { SslProtocols.Ssl2, Configuration.Http.SSLv2RemoteServer };
            }
#pragma warning restore 0618
        }

        // We have tests that validate with SslStream, but that's limited by what the current OS supports.
        // This tests provides additional validation against an external server.
        [OuterLoop("Avoid www.ssllabs.com dependency in innerloop.")]
        [Theory]
        [MemberData(nameof(NotSupportedSSLVersionServers))]
        public async Task GetAsync_UnsupportedSSLVersion_Throws(SslProtocols sslProtocols, string url)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.SslProtocols = sslProtocols;
                await Assert.ThrowsAsync<HttpRequestException>(() => RemoteServerQuery.Run(() => client.GetAsync(url), remoteServerExceptionWrapper, url));
            }
        }

        [Fact]
        public async Task GetAsync_NoSpecifiedProtocol_DefaultsToTls12()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

                var options = new LoopbackServer.Options { UseSsl = true, SslProtocols = SslProtocols.Tls12 };
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        client.GetAsync(url),
                        server.AcceptConnectionAsync(async connection =>
                        {
                            Assert.Equal(SslProtocols.Tls12, Assert.IsType<SslStream>(connection.Stream).SslProtocol);
                            await connection.ReadRequestHeaderAndSendResponseAsync();
                        }));
                }, options);
            }
        }

        [Theory]
#pragma warning disable 0618 // SSL2/3 are deprecated
        [InlineData(SslProtocols.Ssl2, SslProtocols.Tls12)]
        [InlineData(SslProtocols.Ssl3, SslProtocols.Tls12)]
#pragma warning restore 0618
        [InlineData(SslProtocols.Tls11, SslProtocols.Tls)]
        [InlineData(SslProtocols.Tls11 | SslProtocols.Tls12, SslProtocols.Tls)] // Skip this on WinHttpHandler.
        [InlineData(SslProtocols.Tls12, SslProtocols.Tls11)]
        [InlineData(SslProtocols.Tls, SslProtocols.Tls12)]
        public async Task GetAsync_AllowedClientSslVersionDiffersFromServer_ThrowsException(
            SslProtocols allowedClientProtocols, SslProtocols acceptedServerProtocols)
        {
            if (IsWinHttpHandler &&
                allowedClientProtocols == (SslProtocols.Tls11 | SslProtocols.Tls12) &&
                acceptedServerProtocols == SslProtocols.Tls)
            {
                // Native WinHTTP sometimes uses multiple TCP connections to try other TLS protocols when
                // getting TLS protocol failures as part of its TLS fallback algorithm. The loopback server
                // doesn't expect this and stops listening for more connections. This causes unexpected test
                // failures. See https://github.com/dotnet/runtime/issues/17287.
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (HttpClient client = CreateHttpClient(handler))
            {
                handler.SslProtocols = allowedClientProtocols;
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

                var options = new LoopbackServer.Options { UseSsl = true, SslProtocols = acceptedServerProtocols };
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    Task serverTask = server.AcceptConnectionSendResponseAndCloseAsync();
                    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
                    try
                    {
                        await serverTask;
                    }
                    catch (Exception e) when (e is IOException || e is AuthenticationException)
                    {
                        // Some SSL implementations simply close or reset connection after protocol mismatch.
                        // Newer OpenSSL sends Fatal Alert message before closing.
                        return;
                    }
                    // We expect negotiation to fail so one or the other expected exception should be thrown.
                    Assert.True(false, "Expected exception did not happen.");
                }, options);
            }
        }
    }
}
