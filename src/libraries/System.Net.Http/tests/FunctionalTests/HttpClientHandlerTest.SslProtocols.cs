// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "SslProtocols not supported on UAP")]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "SslProtocols property requires .NET 4.7.2")]
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
#if !netstandard
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
            if (!BackendSupportsSslConfiguration)
            {
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
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
            foreach (SslProtocols protocol in new[] { SslProtocols.Tls, SslProtocols.Tls11, SslProtocols.Tls12 })
            {
                yield return new object[] { protocol, false };
                yield return new object[] { protocol, true };
            }

            // These protocols are disabled by default, so we can only connect with them explicitly.
            // On certain platforms these are completely disabled and cannot be used at all.
#pragma warning disable 0618
            if (PlatformDetection.SupportsSsl3)
            {
                yield return new object[] { SslProtocols.Ssl3, true };
            }
            if (PlatformDetection.IsWindows && !PlatformDetection.IsWindows10Version1607OrGreater)
            {
                yield return new object[] { SslProtocols.Ssl2, true };
            }
#pragma warning restore 0618
#if !netstandard
            // These protocols are new, and might not be enabled everywhere yet
            if (PlatformDetection.IsUbuntu1810OrHigher)
            {
                yield return new object[] { SslProtocols.Tls13, false };
                yield return new object[] { SslProtocols.Tls13, true };
            }
#endif
        }

        [ConditionalTheory]
        [MemberData(nameof(GetAsync_AllowedSSLVersion_Succeeds_MemberData))]
        public async Task GetAsync_AllowedSSLVersion_Succeeds(SslProtocols acceptedProtocol, bool requestOnlyThisProtocol)
        {
            if (!BackendSupportsSslConfiguration)
            {
                return;
            }

#pragma warning disable 0618
            if (IsCurlHandler && PlatformDetection.IsRedHatFamily6 && acceptedProtocol == SslProtocols.Ssl3)
            {
                // Issue: #28790: SSLv3 is supported on RHEL 6, but it fails with curl.
                throw new SkipTestException("CurlHandler (libCurl) has problems with SSL3 on RH6");
            }
#pragma warning restore 0618

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;

                if (requestOnlyThisProtocol)
                {
                    handler.SslProtocols = acceptedProtocol;
                }
                var options = new LoopbackServer.Options { UseSsl = true, SslProtocols = acceptedProtocol };
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        server.AcceptConnectionSendResponseAndCloseAsync(),
                        client.GetAsync(url));
                }, options);
            }
        }

        public static IEnumerable<object[]> SupportedSSLVersionServers()
        {
#pragma warning disable 0618 // SSL2/3 are deprecated
            if (PlatformDetection.IsWindows ||
                PlatformDetection.IsOSX ||
                (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && PlatformDetection.OpenSslVersion < new Version(1, 0, 2) && !PlatformDetection.IsDebian))
            {
                yield return new object[] { SslProtocols.Ssl3, Configuration.Http.SSLv3RemoteServer };
            }
#pragma warning restore 0618
            yield return new object[] { SslProtocols.Tls, Configuration.Http.TLSv10RemoteServer };
            yield return new object[] { SslProtocols.Tls11, Configuration.Http.TLSv11RemoteServer };
            yield return new object[] { SslProtocols.Tls12, Configuration.Http.TLSv12RemoteServer };
        }

        // We have tests that validate with SslStream, but that's limited by what the current OS supports.
        // This tests provides additional validation against an external server.
        [ActiveIssue(26186)]
        [OuterLoop("Avoid www.ssllabs.com dependency in innerloop.")]
        [Theory]
        [MemberData(nameof(SupportedSSLVersionServers))]
        public async Task GetAsync_SupportedSSLVersion_Succeeds(SslProtocols sslProtocols, string url)
        {
            if (UseSocketsHttpHandler)
            {
                // TODO #26186: SocketsHttpHandler is failing on some OSes.
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                handler.SslProtocols = sslProtocols;
                using (var client = new HttpClient(handler))
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
            if (PlatformDetection.IsWindows10Version1607OrGreater)
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
            using (HttpClient client = new HttpClient(handler))
            {
                handler.SslProtocols = sslProtocols;
                await Assert.ThrowsAsync<HttpRequestException>(() => RemoteServerQuery.Run(() => client.GetAsync(url), remoteServerExceptionWrapper, url));
            }
        }

        [Fact]
        public async Task GetAsync_NoSpecifiedProtocol_DefaultsToTls12()
        {
            if (!BackendSupportsSslConfiguration)
            {
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
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
            if (!BackendSupportsSslConfiguration)
            {
                return;
            }

            if (IsWinHttpHandler && 
                allowedClientProtocols == (SslProtocols.Tls11 | SslProtocols.Tls12) &&
                acceptedServerProtocols == SslProtocols.Tls)
            {
                // Native WinHTTP sometimes uses multiple TCP connections to try other TLS protocols when
                // getting TLS protocol failures as part of its TLS fallback algorithm. The loopback server
                // doesn't expect this and stops listening for more connections. This causes unexpected test
                // failures. See dotnet/corefx #8538.
                return;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
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
