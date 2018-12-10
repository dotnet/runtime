// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    // Note:  Disposing the HttpClient object automatically disposes the handler within. So, it is not necessary
    // to separately Dispose (or have a 'using' statement) for the handler.
    public abstract class HttpClientHandlerTest : HttpClientTestBase
    {
        readonly ITestOutputHelper _output;
        private const string ExpectedContent = "Test content";
        private const string Username = "testuser";
        private const string Password = "password";
        private const string HttpDefaultPort = "80";

        private readonly NetworkCredential _credential = new NetworkCredential(Username, Password);

        public static readonly object[][] EchoServers = Configuration.Http.EchoServers;
        public static readonly object[][] VerifyUploadServers = Configuration.Http.VerifyUploadServers;
        public static readonly object[][] CompressedServers = Configuration.Http.CompressedServers;
        public static readonly object[][] Http2Servers = Configuration.Http.Http2Servers;
        public static readonly object[][] Http2NoPushServers = Configuration.Http.Http2NoPushServers;

        public static readonly object[][] RedirectStatusCodes = {
            new object[] { 300 },
            new object[] { 301 },
            new object[] { 302 },
            new object[] { 303 },
            new object[] { 307 },
            new object[] { 308 }
        };

        public static readonly object[][] RedirectStatusCodesOldMethodsNewMethods = {
            new object[] { 300, "GET", "GET" },
            new object[] { 300, "POST", "GET" },
            new object[] { 300, "HEAD", "HEAD" },

            new object[] { 301, "GET", "GET" },
            new object[] { 301, "POST", "GET" },
            new object[] { 301, "HEAD", "HEAD" },

            new object[] { 302, "GET", "GET" },
            new object[] { 302, "POST", "GET" },
            new object[] { 302, "HEAD", "HEAD" },

            new object[] { 303, "GET", "GET" },
            new object[] { 303, "POST", "GET" },
            new object[] { 303, "HEAD", "HEAD" },

            new object[] { 307, "GET", "GET" },
            new object[] { 307, "POST", "POST" },
            new object[] { 307, "HEAD", "HEAD" },

            new object[] { 308, "GET", "GET" },
            new object[] { 308, "POST", "POST" },
            new object[] { 308, "HEAD", "HEAD" },
        };

        // Standard HTTP methods defined in RFC7231: http://tools.ietf.org/html/rfc7231#section-4.3
        //     "GET", "HEAD", "POST", "PUT", "DELETE", "OPTIONS", "TRACE"
        public static readonly IEnumerable<object[]> HttpMethods =
            GetMethods("GET", "HEAD", "POST", "PUT", "DELETE", "OPTIONS", "TRACE", "CUSTOM1");
        public static readonly IEnumerable<object[]> HttpMethodsThatAllowContent =
            GetMethods("GET", "POST", "PUT", "DELETE", "OPTIONS", "CUSTOM1");
        public static readonly IEnumerable<object[]> HttpMethodsThatDontAllowContent =
            GetMethods("HEAD", "TRACE");

        private static bool IsWindows10Version1607OrGreater => PlatformDetection.IsWindows10Version1607OrGreater;

        private static IEnumerable<object[]> GetMethods(params string[] methods)
        {
            foreach (string method in methods)
            {
                foreach (bool secure in new[] { true, false })
                {
                    yield return new object[] { method, secure };
                }
            }
        }

        public HttpClientHandlerTest(ITestOutputHelper output)
        {
            _output = output;
            if (PlatformDetection.IsFullFramework)
            {
                // On .NET Framework, the default limit for connections/server is very low (2). 
                // On .NET Core, the default limit is higher. Since these tests run in parallel,
                // the limit needs to be increased to avoid timeouts when running the tests.
                System.Net.ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            }
        }

        [Fact]
        public void CookieContainer_SetNull_ThrowsArgumentNullException()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Throws<ArgumentNullException>(() => handler.CookieContainer = null);
            }
        }

        [Fact]
        public void Ctor_ExpectedDefaultPropertyValues_CommonPlatform()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                // Same as .NET Framework (Desktop).
                Assert.Equal(DecompressionMethods.None, handler.AutomaticDecompression);
                Assert.True(handler.AllowAutoRedirect);
                Assert.Equal(ClientCertificateOption.Manual, handler.ClientCertificateOptions);
                CookieContainer cookies = handler.CookieContainer;
                Assert.NotNull(cookies);
                Assert.Equal(0, cookies.Count);
                Assert.Null(handler.Credentials);
                Assert.Equal(50, handler.MaxAutomaticRedirections);
                Assert.NotNull(handler.Properties);
                Assert.Equal(null, handler.Proxy);
                Assert.True(handler.SupportsAutomaticDecompression);
                Assert.True(handler.UseCookies);
                Assert.False(handler.UseDefaultCredentials);
                Assert.True(handler.UseProxy);
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)]
        [Fact]
        public void Ctor_ExpectedDefaultPropertyValues_NotUapPlatform()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                // Same as .NET Framework (Desktop).
                Assert.Equal(64, handler.MaxResponseHeadersLength);
                Assert.False(handler.PreAuthenticate);
                Assert.True(handler.SupportsProxy);
                Assert.True(handler.SupportsRedirectConfiguration);

                // Changes from .NET Framework (Desktop).
                if (!PlatformDetection.IsFullFramework)
                {
                    Assert.False(handler.CheckCertificateRevocationList);
                    Assert.Equal(0, handler.MaxRequestContentBufferSize);
                    Assert.Equal(SslProtocols.None, handler.SslProtocols);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsUap))]
        public void Ctor_ExpectedDefaultPropertyValues_UapPlatform()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.True(handler.CheckCertificateRevocationList);
                Assert.Equal(0, handler.MaxRequestContentBufferSize);
                Assert.Equal(-1, handler.MaxResponseHeadersLength);
                Assert.True(handler.PreAuthenticate);
                Assert.Equal(SslProtocols.None, handler.SslProtocols);
                Assert.False(handler.SupportsProxy);
                Assert.False(handler.SupportsRedirectConfiguration);
            }
        }

        [Fact]
        public void Credentials_SetGet_Roundtrips()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                var creds = new NetworkCredential("username", "password", "domain");

                handler.Credentials = null;
                Assert.Null(handler.Credentials);

                handler.Credentials = creds;
                Assert.Same(creds, handler.Credentials);

                handler.Credentials = CredentialCache.DefaultCredentials;
                Assert.Same(CredentialCache.DefaultCredentials, handler.Credentials);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void MaxAutomaticRedirections_InvalidValue_Throws(int redirects)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => handler.MaxAutomaticRedirections = redirects);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData((long)int.MaxValue + (long)1)]
        public void MaxRequestContentBufferSize_SetInvalidValue_ThrowsArgumentOutOfRangeException(long value)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => handler.MaxRequestContentBufferSize = value);
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP will send default credentials based on other criteria.")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UseDefaultCredentials_SetToFalseAndServerNeedsAuth_StatusCodeUnauthorized(bool useProxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.UseProxy = useProxy;
            handler.UseDefaultCredentials = false;
            using (var client = new HttpClient(handler))
            {
                Uri uri = Configuration.Http.NegotiateAuthUriForDefaultCreds(secure: false);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [ActiveIssue(29802, TargetFrameworkMonikers.Uap)]
        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(RedirectStatusCodes))]
        public async Task DefaultHeaders_SetCredentials_ClearedOnRedirect(int statusCode)
        {
            if (statusCode == 308 && (PlatformDetection.IsFullFramework || IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                string credentialString = _credential.UserName + ":" + _credential.Password;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentialString);
                Uri uri = Configuration.Http.RedirectUriForDestinationUri(
                    secure: false,
                    statusCode: statusCode,
                    destinationUri: Configuration.Http.RemoteEchoServer,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    string responseText = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.False(TestHelper.JsonMessageContainsKey(responseText, "Authorization"));
                }
            }
        }

        [Fact]
        public void Properties_Get_CountIsZero()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                IDictionary<string, object> dict = handler.Properties;
                Assert.Same(dict, handler.Properties);
                Assert.Equal(0, dict.Count);
            }
        }

        [Fact]
        public void Properties_AddItemToDictionary_ItemPresent()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                IDictionary<string, object> dict = handler.Properties;

                var item = new object();
                dict.Add("item", item);

                object value;
                Assert.True(dict.TryGetValue("item", out value));
                Assert.Equal(item, value);
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(EchoServers))]
        public async Task SendAsync_SimpleGet_Success(Uri remoteServer)
        {
            using (HttpClient client = CreateHttpClient())
            using (HttpResponseMessage response = await client.GetAsync(remoteServer))
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseContent);
                TestHelper.VerifyResponseBody(
                    responseContent,
                    response.Content.Headers.ContentMD5,
                    false,
                    null);
            }
        }

        [OuterLoop("Uses external server")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendAsync_GetWithValidHostHeader_Success(bool withPort)
        {
            var m = new HttpRequestMessage(HttpMethod.Get, Configuration.Http.SecureRemoteEchoServer);
            m.Headers.Host = withPort ? Configuration.Http.SecureHost + ":123" : Configuration.Http.SecureHost;

            using (HttpClient client = CreateHttpClient())
            using (HttpResponseMessage response = await client.SendAsync(m))
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseContent);
                TestHelper.VerifyResponseBody(
                    responseContent,
                    response.Content.Headers.ContentMD5,
                    false,
                    null);
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task SendAsync_GetWithInvalidHostHeader_ThrowsException()
        {
            if (PlatformDetection.IsNetCore && !UseSocketsHttpHandler)
            {
                // Only .NET Framework and SocketsHttpHandler use the Host header to influence the SSL auth.
                return;
            }

            var m = new HttpRequestMessage(HttpMethod.Get, Configuration.Http.SecureRemoteEchoServer);
            m.Headers.Host = "hostheaderthatdoesnotmatch";

            using (HttpClient client = CreateHttpClient())
            {
                await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(m));
            }
        }

        [ActiveIssue(22158, TargetFrameworkMonikers.Uap)]
        [Fact]
        public async Task GetAsync_IPv6LinkLocalAddressUri_Success()
        {
            using (HttpClient client = CreateHttpClient())
            {
                var options = new LoopbackServer.Options { Address = TestHelper.GetIPv6LinkLocalAddress() };
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    _output.WriteLine(url.ToString());
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        server.AcceptConnectionSendResponseAndCloseAsync(),
                        client.GetAsync(url));
                }, options);
            }
        }

        [Theory]
        [MemberData(nameof(GetAsync_IPBasedUri_Success_MemberData))]
        public async Task GetAsync_IPBasedUri_Success(IPAddress address)
        {
            using (HttpClient client = CreateHttpClient())
            {
                var options = new LoopbackServer.Options { Address = address };
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    _output.WriteLine(url.ToString());
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        server.AcceptConnectionSendResponseAndCloseAsync(),
                        client.GetAsync(url));
                }, options);
            }
        }

        public static IEnumerable<object[]> GetAsync_IPBasedUri_Success_MemberData()
        {
            foreach (var addr in new[] { IPAddress.Loopback, IPAddress.IPv6Loopback })
            {
                if (addr != null)
                {
                    yield return new object[] { addr };
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task SendAsync_MultipleRequestsReusingSameClient_Success()
        {
            using (HttpClient client = CreateHttpClient())
            {
                for (int i = 0; i < 3; i++)
                {
                    using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task GetAsync_ResponseContentAfterClientAndHandlerDispose_Success()
        {
            using (HttpClient client = CreateHttpClient())
            using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.SecureRemoteEchoServer))
            {
                client.Dispose();
                Assert.NotNull(response);
                string responseContent = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseContent);
                TestHelper.VerifyResponseBody(responseContent, response.Content.Headers.ContentMD5, false, null);
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task SendAsync_Cancel_CancellationTokenPropagates()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            using (HttpClient client = CreateHttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, Configuration.Http.RemoteEchoServer);
                Task t = client.SendAsync(request, cts.Token);
                OperationCanceledException ex;
                if (PlatformDetection.IsUap)
                {
                    ex = await Assert.ThrowsAsync<OperationCanceledException>(() => t);
                }
                else
                {
                    ex = await Assert.ThrowsAsync<TaskCanceledException>(() => t);
                }

                Assert.True(cts.Token.IsCancellationRequested, "cts token IsCancellationRequested");
                if (!PlatformDetection.IsFullFramework)
                {
                    // .NET Framework has bug where it doesn't propagate token information.
                    Assert.True(ex.CancellationToken.IsCancellationRequested, "exception token IsCancellationRequested");
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(CompressedServers))]
        public async Task GetAsync_SetAutomaticDecompression_ContentDecompressed(Uri server)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (var client = new HttpClient(handler))
            {
                using (HttpResponseMessage response = await client.GetAsync(server))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        null);
                }
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP HTTP stack doesn't support .Proxy property")]
        [Theory]
        [InlineData("[::1234]")]
        [InlineData("[::1234]:8080")]
        public async Task GetAsync_IPv6AddressInHostHeader_CorrectlyFormatted(string host)
        {
            string ipv6Address = "http://" + host;
            bool connectionAccepted = false;

            await LoopbackServer.CreateClientAndServerAsync(async proxyUri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    handler.Proxy = new WebProxy(proxyUri);
                    try { await client.GetAsync(ipv6Address); } catch { }
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                connectionAccepted = true;
                List<string> headers = await connection.ReadRequestHeaderAndSendResponseAsync();
                Assert.Contains($"Host: {host}", headers);
            }));

            Assert.True(connectionAccepted);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP HTTP stack doesn't support .Proxy property")]
        [Theory]
        [InlineData("1.2.3.4")]
        [InlineData("1.2.3.4:8080")]
        [InlineData("[::1234]")]
        [InlineData("[::1234]:8080")]
        public async Task ProxiedIPAddressRequest_NotDefaultPort_CorrectlyFormatted(string host)
        {
            string uri = "http://" + host;
            bool connectionAccepted = false;

            await LoopbackServer.CreateClientAndServerAsync(async proxyUri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    handler.Proxy = new WebProxy(proxyUri);
                    try { await client.GetAsync(uri); } catch { }
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                connectionAccepted = true;
                List<string> headers = await connection.ReadRequestHeaderAndSendResponseAsync();
                Assert.Contains($"GET {uri}/ HTTP/1.1", headers);
            }));

            Assert.True(connectionAccepted);
        }

        public static IEnumerable<object[]> DestinationHost_MemberData()
        {
            yield return new object[] { Configuration.Http.Host };
            yield return new object[] { "1.2.3.4" };
            yield return new object[] { "[::1234]" };
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP HTTP stack doesn't support .Proxy property")]
        [Theory]
        [OuterLoop("Uses external server")]
        [MemberData(nameof(DestinationHost_MemberData))]
        public async Task ProxiedRequest_DefaultPort_PortStrippedOffInUri(string host)
        {
            string addressUri = $"http://{host}:{HttpDefaultPort}/";
            string expectedAddressUri = $"http://{host}/";
            bool connectionAccepted = false;

            await LoopbackServer.CreateClientAndServerAsync(async proxyUri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    handler.Proxy = new WebProxy(proxyUri);
                    try { await client.GetAsync(addressUri); } catch { }
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                connectionAccepted = true;
                List<string> headers = await connection.ReadRequestHeaderAndSendResponseAsync();
                Assert.Contains($"GET {expectedAddressUri} HTTP/1.1", headers);
            }));

            Assert.True(connectionAccepted);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP HTTP stack doesn't support .Proxy property")]
        [Fact]
        [OuterLoop("Uses external server")]
        public async Task ProxyTunnelRequest_PortSpecified_NotStrippedOffInUri()
        {
            // Https proxy request will use CONNECT tunnel, even the default 443 port is specified, it will not be stripped off.
            string requestTarget = $"{Configuration.Http.SecureHost}:443";
            string addressUri = $"https://{requestTarget}/";
            bool connectionAccepted = false;

            await LoopbackServer.CreateClientAndServerAsync(async proxyUri =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    handler.Proxy = new WebProxy(proxyUri);
                    handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
                    try { await client.GetAsync(addressUri); } catch { }
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                connectionAccepted = true;
                List<string> headers = await connection.ReadRequestHeaderAndSendResponseAsync();
                Assert.Contains($"CONNECT {requestTarget} HTTP/1.1", headers);
            }));

            Assert.True(connectionAccepted);
        }

        public static IEnumerable<object[]> SecureAndNonSecure_IPBasedUri_MemberData() =>
            from address in new[] { IPAddress.Loopback, IPAddress.IPv6Loopback }
            from useSsl in new[] { true, false }
            select new object[] { address, useSsl };

        [ActiveIssue(30056, TargetFrameworkMonikers.Uap)]
        [Theory]
        [MemberData(nameof(SecureAndNonSecure_IPBasedUri_MemberData))]
        public async Task GetAsync_SecureAndNonSecureIPBasedUri_CorrectlyFormatted(IPAddress address, bool useSsl)
        {
            var options = new LoopbackServer.Options { Address = address, UseSsl= useSsl };
            bool connectionAccepted = false;
            string host = "";

            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                host = $"{url.Host}:{url.Port}";
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    if (useSsl)
                    {
                        handler.ServerCertificateCustomValidationCallback = TestHelper.AllowAllCertificates;
                    }
                    try { await client.GetAsync(url); } catch { }
                }
            }, server => server.AcceptConnectionAsync(async connection =>
            {
                connectionAccepted = true;
                List<string> headers = await connection.ReadRequestHeaderAndSendResponseAsync();
                Assert.Contains($"Host: {host}", headers);
            }), options);

            Assert.True(connectionAccepted);
        }

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(CompressedServers))]
        public async Task GetAsync_SetAutomaticDecompression_HeadersRemoved(Uri server)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(server, HttpCompletionOption.ResponseHeadersRead))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                Assert.False(response.Content.Headers.Contains("Content-Encoding"), "Content-Encoding unexpectedly found");
                Assert.False(response.Content.Headers.Contains("Content-Length"), "Content-Length unexpectedly found");
            }
        }

        [Theory]
#if netcoreapp
        [InlineData(DecompressionMethods.Brotli, "br", "")]
        [InlineData(DecompressionMethods.Brotli, "br", "br")]
        [InlineData(DecompressionMethods.Brotli, "br", "gzip")]
        [InlineData(DecompressionMethods.Brotli, "br", "gzip, deflate")]
#endif
        [InlineData(DecompressionMethods.GZip, "gzip", "")]
        [InlineData(DecompressionMethods.Deflate, "deflate", "")]
        [InlineData(DecompressionMethods.GZip | DecompressionMethods.Deflate, "gzip, deflate", "")]
        [InlineData(DecompressionMethods.GZip, "gzip", "gzip")]
        [InlineData(DecompressionMethods.Deflate, "deflate", "deflate")]
        [InlineData(DecompressionMethods.GZip, "gzip", "deflate")]
        [InlineData(DecompressionMethods.GZip, "gzip", "br")]
        [InlineData(DecompressionMethods.Deflate, "deflate", "gzip")]
        [InlineData(DecompressionMethods.Deflate, "deflate", "br")]
        [InlineData(DecompressionMethods.GZip | DecompressionMethods.Deflate, "gzip, deflate", "gzip, deflate")]
        public async Task GetAsync_SetAutomaticDecompression_AcceptEncodingHeaderSentWithNoDuplicates(
            DecompressionMethods methods,
            string encodings,
            string manualAcceptEncodingHeaderValues)
        {
            if (IsCurlHandler)
            {
                // Skip these tests on CurlHandler, dotnet/corefx #29905.
                return;
            }

            if (!UseSocketsHttpHandler &&
                (encodings.Contains("br") || manualAcceptEncodingHeaderValues.Contains("br")))
            {
                // Brotli encoding only supported on SocketsHttpHandler.
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.AutomaticDecompression = methods;

                using (var client = new HttpClient(handler))
                {
                    if (!string.IsNullOrEmpty(manualAcceptEncodingHeaderValues))
                    {
                        client.DefaultRequestHeaders.Add("Accept-Encoding", manualAcceptEncodingHeaderValues);
                    }

                    Task<HttpResponseMessage> clientTask = client.GetAsync(url);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();
                    await TaskTimeoutExtensions.WhenAllOrAnyFailed(new Task[] { clientTask, serverTask }); 

                    List<string> requestLines = await serverTask;
                    string requestLinesString = string.Join("\r\n", requestLines);
                    _output.WriteLine(requestLinesString);

                    Assert.InRange(Regex.Matches(requestLinesString, "Accept-Encoding").Count, 1, 1);
                    Assert.InRange(Regex.Matches(requestLinesString, encodings).Count, 1, 1);
                    if (!string.IsNullOrEmpty(manualAcceptEncodingHeaderValues))
                    {
                        Assert.InRange(Regex.Matches(requestLinesString, manualAcceptEncodingHeaderValues).Count, 1, 1);
                    }

                    using (HttpResponseMessage response = await clientTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }                    
                }
            });
        }

        [ActiveIssue(32647, TargetFrameworkMonikers.Uap)]
        [OuterLoop("Uses external server")]
        [Fact]
        public async Task GetAsync_ServerNeedsBasicAuthAndSetDefaultCredentials_StatusCodeUnauthorized()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Credentials = CredentialCache.DefaultCredentials;
            using (var client = new HttpClient(handler))
            {
                Uri uri = Configuration.Http.BasicAuthUriForCreds(secure: false, userName: Username, password: Password);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task GetAsync_ServerNeedsAuthAndSetCredential_StatusCodeOK()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Credentials = _credential;
            using (var client = new HttpClient(handler))
            {
                Uri uri = Configuration.Http.BasicAuthUriForCreds(secure: false, userName: Username, password: Password);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public void GetAsync_ServerNeedsAuthAndNoCredential_StatusCodeUnauthorized()
        {
            // UAP HTTP stack caches connections per-process. This causes interference when these tests run in
            // the same process as the other tests. Each test needs to be isolated to its own process.
            // See dicussion: https://github.com/dotnet/corefx/issues/21945
            RemoteInvoke(async useSocketsHttpHandlerString =>
            {
                using (var client = CreateHttpClient(useSocketsHttpHandlerString))
                {
                    Uri uri = Configuration.Http.BasicAuthUriForCreds(secure: false, userName: Username, password: Password);
                    using (HttpResponseMessage response = await client.GetAsync(uri))
                    {
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }

                    return SuccessExitCode;
                }
            }, UseSocketsHttpHandler.ToString()).Dispose();
        }

        [Theory]
        [InlineData("WWW-Authenticate: CustomAuth\r\n")]
        [InlineData("")] // RFC7235 requires servers to send this header with 401 but some servers don't.
        public async Task GetAsync_ServerNeedsNonStandardAuthAndSetCredential_StatusCodeUnauthorized(string authHeaders)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.Credentials = new NetworkCredential("unused", "unused");
                using (var client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Unauthorized);

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);
                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            });
        }

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(RedirectStatusCodes))]
        public async Task GetAsync_AllowAutoRedirectFalse_RedirectFromHttpToHttp_StatusCodeRedirect(int statusCode)
        {
            if (statusCode == 308 && (PlatformDetection.IsFullFramework || IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = false;
            using (var client = new HttpClient(handler))
            {
                Uri uri = Configuration.Http.RedirectUriForDestinationUri(
                    secure: false,
                    statusCode: statusCode,
                    destinationUri: Configuration.Http.RemoteEchoServer,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(statusCode, (int)response.StatusCode);
                    Assert.Equal(uri, response.RequestMessage.RequestUri);
                }
            }
        }

        [Theory, MemberData(nameof(RedirectStatusCodesOldMethodsNewMethods))]
        public async Task AllowAutoRedirect_True_ValidateNewMethodUsedOnRedirection(
            int statusCode, string oldMethod, string newMethod)
        {
            if (IsCurlHandler && statusCode == 300 && oldMethod == "POST")
            {
                // Known behavior: curl does not change method to "GET"
                // https://github.com/dotnet/corefx/issues/26434
                newMethod = "POST";
            }

            if (statusCode == 308 && (PlatformDetection.IsFullFramework || IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                await LoopbackServer.CreateServerAsync(async (origServer, origUrl) =>
                {
                    var request = new HttpRequestMessage(new HttpMethod(oldMethod), origUrl);

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);

                    await LoopbackServer.CreateServerAsync(async (redirServer, redirUrl) =>
                    {
                        // Original URL will redirect to a different URL
                        Task<List<string>> serverTask = origServer.AcceptConnectionSendResponseAndCloseAsync((HttpStatusCode)statusCode, $"Location: {redirUrl}\r\n");

                        await Task.WhenAny(getResponseTask, serverTask);
                        Assert.False(getResponseTask.IsCompleted, $"{getResponseTask.Status}: {getResponseTask.Exception}");
                        await serverTask;

                        // Redirected URL answers with success
                        serverTask = redirServer.AcceptConnectionSendResponseAndCloseAsync();
                        await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                        List<string> receivedRequest = await serverTask;

                        string[] statusLineParts = receivedRequest[0].Split(' ');

                        using (HttpResponseMessage response = await getResponseTask)
                        {
                            Assert.Equal(200, (int)response.StatusCode);
                            Assert.Equal(newMethod, statusLineParts[0]);
                        }
                    });
                });
            }
        }

        [ActiveIssue(30063, TargetFrameworkMonikers.Uap)] // fails due to TE header
        [Theory]
        [InlineData(300)]
        [InlineData(301)]
        [InlineData(302)]
        [InlineData(303)]
        public async Task AllowAutoRedirect_True_PostToGetDoesNotSendTE(int statusCode)
        {
            if (IsCurlHandler && statusCode == 300)
            {
                // ISSUE #26434:
                // CurlHandler doesn't change POST to GET for 300 response (see above test).
                return;
            }

            if (IsWinHttpHandler)
            {
                // ISSUE #27440:
                // This test occasionally fails on WinHttpHandler.
                // Likely this is due to the way the loopback server is sending the response before reading the entire request.
                // We should change the server behavior here.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                await LoopbackServer.CreateServerAsync(async (origServer, origUrl) =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, origUrl);
                    request.Content = new StringContent(ExpectedContent);
                    request.Headers.TransferEncodingChunked = true;

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);

                    await LoopbackServer.CreateServerAsync(async (redirServer, redirUrl) =>
                    {
                        // Original URL will redirect to a different URL
                        Task serverTask = origServer.AcceptConnectionAsync(async connection =>
                        {
                            // Send Connection: close so the client will close connection after request is sent,
                            // meaning we can just read to the end to get the content
                            await connection.ReadRequestHeaderAndSendResponseAsync((HttpStatusCode)statusCode, $"Location: {redirUrl}\r\nConnection: close\r\n");
                            connection.Socket.Shutdown(SocketShutdown.Send);
                            await connection.Reader.ReadToEndAsync();
                        });

                        await Task.WhenAny(getResponseTask, serverTask);
                        Assert.False(getResponseTask.IsCompleted, $"{getResponseTask.Status}: {getResponseTask.Exception}");
                        await serverTask;

                        // Redirected URL answers with success
                        List<string> receivedRequest = null;
                        string receivedContent = null;
                        Task serverTask2 = redirServer.AcceptConnectionAsync(async connection =>
                        {
                            // Send Connection: close so the client will close connection after request is sent,
                            // meaning we can just read to the end to get the content
                            receivedRequest = await connection.ReadRequestHeaderAndSendResponseAsync(additionalHeaders: "Connection: close\r\n");
                            connection.Socket.Shutdown(SocketShutdown.Send);
                            receivedContent = await connection.Reader.ReadToEndAsync();
                        });

                        await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask2);

                        string[] statusLineParts = receivedRequest[0].Split(' ');
                        Assert.Equal("GET", statusLineParts[0]);
                        Assert.DoesNotContain(receivedRequest, line => line.StartsWith("Transfer-Encoding"));
                        Assert.DoesNotContain(receivedRequest, line => line.StartsWith("Content-Length"));

                        using (HttpResponseMessage response = await getResponseTask)
                        {
                            Assert.Equal(200, (int)response.StatusCode);
                        }
                    });
                });
            }
        }

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(RedirectStatusCodes))]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpToHttp_StatusCodeOK(int statusCode)
        {
            if (statusCode == 308 && (PlatformDetection.IsFullFramework || IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                Uri uri = Configuration.Http.RedirectUriForDestinationUri(
                    secure: false,
                    statusCode: statusCode,
                    destinationUri: Configuration.Http.RemoteEchoServer,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(Configuration.Http.RemoteEchoServer, response.RequestMessage.RequestUri);
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpToHttps_StatusCodeOK()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                Uri uri = Configuration.Http.RedirectUriForDestinationUri(
                    secure: false,
                    statusCode: 302,
                    destinationUri: Configuration.Http.SecureRemoteEchoServer,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(Configuration.Http.SecureRemoteEchoServer, response.RequestMessage.RequestUri);
                }
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework allows HTTPS to HTTP redirection")]
        [OuterLoop("Uses external server")]
        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpsToHttp_StatusCodeRedirect()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                Uri uri = Configuration.Http.RedirectUriForDestinationUri(
                    secure: true,
                    statusCode: 302,
                    destinationUri: Configuration.Http.RemoteEchoServer,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);

                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
                    Assert.Equal(uri, response.RequestMessage.RequestUri);
                }
            }
        }

        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectWithoutLocation_ReturnsOriginalResponse()
        {
            // [ActiveIssue(24819, TestPlatforms.Windows)]
            if (PlatformDetection.IsWindows && PlatformDetection.IsNetCore && !UseSocketsHttpHandler)
            {
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    Task<HttpResponseMessage> getTask = client.GetAsync(url);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Found);
                    await TestHelper.WhenAllCompletedOrAnyFailed(getTask, serverTask);

                    using (HttpResponseMessage response = await getTask)
                    {
                        Assert.Equal(302, (int)response.StatusCode);
                    }
                });
            }
        }

        [ActiveIssue(32647, TargetFrameworkMonikers.Uap)]
        [OuterLoop("Uses external server")]
        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectToUriWithParams_RequestMsgUriSet()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            Uri targetUri = Configuration.Http.BasicAuthUriForCreds(secure: false, userName: Username, password: Password);
            using (var client = new HttpClient(handler))
            {
                Uri uri = Configuration.Http.RedirectUriForDestinationUri(
                    secure: false,
                    statusCode: 302,
                    destinationUri: targetUri,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    Assert.Equal(targetUri, response.RequestMessage.RequestUri);
                }
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not currently supported on UAP")]
        [OuterLoop("Uses external server")]
        [Theory]
        [InlineData(3, 2)]
        [InlineData(3, 3)]
        [InlineData(3, 4)]
        public async Task GetAsync_MaxAutomaticRedirectionsNServerHops_ThrowsIfTooMany(int maxHops, int hops)
        {
            if (IsWinHttpHandler && !PlatformDetection.IsWindows10Version1703OrGreater)
            {
                // Skip this test if using WinHttpHandler but on a release prior to Windows 10 Creators Update.
                _output.WriteLine("Skipping test due to Windows 10 version prior to Version 1703.");
                return;
            }
            else if (PlatformDetection.IsFullFramework)
            {
                // Skip this test if running on .NET Framework. Exceeding max redirections will not throw
                // exception. Instead, it simply returns the 3xx response.
                _output.WriteLine("Skipping test on .NET Framework due to behavior difference.");
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.MaxAutomaticRedirections = maxHops;
            using (var client = new HttpClient(handler))
            {
                Task<HttpResponseMessage> t = client.GetAsync(Configuration.Http.RedirectUriForDestinationUri(
                    secure: false,
                    statusCode: 302,
                    destinationUri: Configuration.Http.RemoteEchoServer,
                    hops: hops));

                if (hops <= maxHops)
                {
                    using (HttpResponseMessage response = await t)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal(Configuration.Http.RemoteEchoServer, response.RequestMessage.RequestUri);
                    }
                }
                else
                {
                    if (UseSocketsHttpHandler)
                    {
                        using (HttpResponseMessage response = await t)
                        {
                            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
                        }
                    }
                    else
                    {
                        await Assert.ThrowsAsync<HttpRequestException>(() => t);
                    }
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectWithRelativeLocation()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                Uri uri = Configuration.Http.RedirectUriForDestinationUri(
                    secure: false,
                    statusCode: 302,
                    destinationUri: Configuration.Http.RemoteEchoServer,
                    hops: 1,
                    relative: true);
                _output.WriteLine("Uri: {0}", uri);

                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(Configuration.Http.RemoteEchoServer, response.RequestMessage.RequestUri);
                }
            }
        }

        [Theory]
        [InlineData(200)]
        [InlineData(201)]
        [InlineData(400)]
        public async Task GetAsync_AllowAutoRedirectTrue_NonRedirectStatusCode_LocationHeader_NoRedirect(int statusCode)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                await LoopbackServer.CreateServerAsync(async (origServer, origUrl) =>
                {
                    await LoopbackServer.CreateServerAsync(async (redirectServer, redirectUrl) =>
                    {
                        Task<HttpResponseMessage> getResponseTask = client.GetAsync(origUrl);

                        Task redirectTask = redirectServer.AcceptConnectionSendResponseAndCloseAsync();

                        await TestHelper.WhenAllCompletedOrAnyFailed(
                            getResponseTask,
                            origServer.AcceptConnectionSendResponseAndCloseAsync((HttpStatusCode)statusCode, $"Location: {redirectUrl}\r\n"));

                        using (HttpResponseMessage response = await getResponseTask)
                        {
                            Assert.Equal(statusCode, (int)response.StatusCode);
                            Assert.Equal(origUrl, response.RequestMessage.RequestUri);
                            Assert.False(redirectTask.IsCompleted, "Should not have redirected to Location");
                        }
                    });
                });
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Doesn't handle fragments according to https://tools.ietf.org/html/rfc7231#section-7.1.2")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Doesn't handle fragments according to https://tools.ietf.org/html/rfc7231#section-7.1.2")]
        [Theory]
        [InlineData("#origFragment", "", "#origFragment", false)]
        [InlineData("#origFragment", "", "#origFragment", true)]
        [InlineData("", "#redirFragment", "#redirFragment", false)]
        [InlineData("", "#redirFragment", "#redirFragment", true)]
        [InlineData("#origFragment", "#redirFragment", "#redirFragment", false)]
        [InlineData("#origFragment", "#redirFragment", "#redirFragment", true)]
        public async Task GetAsync_AllowAutoRedirectTrue_RetainsOriginalFragmentIfAppropriate(
            string origFragment, string redirFragment, string expectedFragment, bool useRelativeRedirect)
        {
            if (IsCurlHandler)
            {
                // libcurl doesn't append fragment component to CURLINFO_EFFECTIVE_URL after redirect
                return;
            }

            if (IsWinHttpHandler)
            {
                // According to https://tools.ietf.org/html/rfc7231#section-7.1.2,
                // "If the Location value provided in a 3xx (Redirection) response does
                //  not have a fragment component, a user agent MUST process the
                //  redirection as if the value inherits the fragment component of the
                //  URI reference used to generate the request target(i.e., the
                //  redirection inherits the original reference's fragment, if any)."
                // WINHTTP is not doing this, and thus neither is WinHttpHandler.
                // It also sometimes doesn't include the fragments for redirects
                // even in other cases.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                await LoopbackServer.CreateServerAsync(async (origServer, origUrl) =>
                {
                    origUrl = new UriBuilder(origUrl) { Fragment = origFragment }.Uri;
                    Uri redirectUrl = new UriBuilder(origUrl) { Fragment = redirFragment }.Uri;
                    if (useRelativeRedirect)
                    {
                        redirectUrl = new Uri(redirectUrl.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.SafeUnescaped), UriKind.Relative);
                    }
                    Uri expectedUrl = new UriBuilder(origUrl) { Fragment = expectedFragment }.Uri;

                    // Make and receive the first request that'll be redirected.
                    Task<HttpResponseMessage> getResponse = client.GetAsync(origUrl);
                    Task firstRequest = origServer.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Found, $"Location: {redirectUrl}\r\n");
                    Assert.Equal(firstRequest, await Task.WhenAny(firstRequest, getResponse));

                    // Receive the second request.
                    Task<List<string>> secondRequest = origServer.AcceptConnectionSendResponseAndCloseAsync();
                    await TestHelper.WhenAllCompletedOrAnyFailed(secondRequest, getResponse);

                    // Make sure the server received the second request for the right Uri.
                    Assert.NotEmpty(secondRequest.Result);
                    string[] statusLineParts = secondRequest.Result[0].Split(' ');
                    Assert.Equal(3, statusLineParts.Length);
                    Assert.Equal(expectedUrl.GetComponents(UriComponents.PathAndQuery, UriFormat.SafeUnescaped), statusLineParts[1]);

                    // Make sure the request message was updated with the correct redirected location.
                    using (HttpResponseMessage response = await getResponse)
                    {
                        Assert.Equal(200, (int)response.StatusCode);
                        Assert.Equal(expectedUrl.ToString(), response.RequestMessage.RequestUri.ToString());
                    }
                });
            }
        }

        [ActiveIssue(32647, TargetFrameworkMonikers.Uap)]
        [Fact]
        [OuterLoop("Uses external server")]
        public async Task GetAsync_CredentialIsNetworkCredentialUriRedirect_StatusCodeUnauthorized()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Credentials = _credential;
            using (var client = new HttpClient(handler))
            {
                Uri redirectUri = Configuration.Http.RedirectUriForCreds(
                    secure: false,
                    statusCode: 302,
                    userName: Username,
                    password: Password);
                using (HttpResponseMessage unAuthResponse = await client.GetAsync(redirectUri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, unAuthResponse.StatusCode);
                }
            }
        }

        [ActiveIssue(32647, TargetFrameworkMonikers.Uap)]
        [Fact]
        [OuterLoop("Uses external server")]
        public async Task HttpClientHandler_CredentialIsNotCredentialCacheAfterRedirect_StatusCodeOK()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Credentials = _credential;
            using (var client = new HttpClient(handler))
            {
                Uri redirectUri = Configuration.Http.RedirectUriForCreds(
                    secure: false,
                    statusCode: 302,
                    userName: Username,
                    password: Password);
                using (HttpResponseMessage unAuthResponse = await client.GetAsync(redirectUri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, unAuthResponse.StatusCode);
                }

                // Use the same handler to perform get request, authentication should succeed after redirect.
                Uri uri = Configuration.Http.BasicAuthUriForCreds(secure: true, userName: Username, password: Password);
                using (HttpResponseMessage authResponse = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, authResponse.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(RedirectStatusCodes))]
        public async Task GetAsync_CredentialIsCredentialCacheUriRedirect_StatusCodeOK(int statusCode)
        {
            if (statusCode == 308 && (PlatformDetection.IsFullFramework || IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            Uri uri = Configuration.Http.BasicAuthUriForCreds(secure: false, userName: Username, password: Password);
            Uri redirectUri = Configuration.Http.RedirectUriForCreds(
                secure: false,
                statusCode: statusCode,
                userName: Username,
                password: Password);
            _output.WriteLine(uri.AbsoluteUri);
            _output.WriteLine(redirectUri.AbsoluteUri);
            var credentialCache = new CredentialCache();
            credentialCache.Add(uri, "Basic", _credential);

            HttpClientHandler handler = CreateHttpClientHandler();
            if (PlatformDetection.IsUap)
            {
                // UAP does not support CredentialCache for Credentials.
                Assert.Throws<PlatformNotSupportedException>(() => handler.Credentials = credentialCache);
            }
            else
            {
                handler.Credentials = credentialCache;
                using (var client = new HttpClient(handler))
                {
                    using (HttpResponseMessage response = await client.GetAsync(redirectUri))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Equal(uri, response.RequestMessage.RequestUri);
                    }
                }
            }
        }

        [OuterLoop("Uses external server")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)]
        [Theory]
        [MemberData(nameof(HeaderEchoUrisMemberData))]
        public async Task GetAsync_RequestHeadersAddCustomHeaders_HeaderAndEmptyValueSent(Uri uri)
        {
            if (IsWinHttpHandler && !PlatformDetection.IsWindows10Version1709OrGreater)
            {
                return;
            }

            string name = "X-Cust-Header-NoValue";
            string value = "";
            using (HttpClient client = CreateHttpClient())
            {
                _output.WriteLine($"name={name}, value={value}");
                client.DefaultRequestHeaders.Add(name, value);
                using (HttpResponseMessage httpResponse = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.True(TestHelper.JsonMessageContainsKeyValue(responseText, name, value));
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(HeaderValueAndUris))]
        public async Task GetAsync_RequestHeadersAddCustomHeaders_HeaderAndValueSent(string name, string value, Uri uri)
        {
            using (HttpClient client = CreateHttpClient())
            {
                _output.WriteLine($"name={name}, value={value}");
                client.DefaultRequestHeaders.Add(name, value);
                using (HttpResponseMessage httpResponse = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.True(TestHelper.JsonMessageContainsKeyValue(responseText, name, value));
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(HeaderEchoUrisMemberData))]
        public async Task GetAsync_LargeRequestHeader_HeadersAndValuesSent(Uri uri)
        {
            // Unfortunately, our remote servers seem to have pretty strict limits (around 16K?)
            // on the total size of the request header.
            // TODO: Figure out how to reconfigure remote endpoints to allow larger request headers,
            // and then increase the limits in this test.

            string headerValue = new string('a', 2048);
            const int headerCount = 6;

            using (HttpClient client = CreateHttpClient())
            {
                for (int i = 0; i < headerCount; i++)
                {
                    client.DefaultRequestHeaders.Add($"Header-{i}", headerValue);
                }

                using (HttpResponseMessage httpResponse = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                    string responseText = await httpResponse.Content.ReadAsStringAsync();

                    for (int i = 0; i < headerCount; i++)
                    {
                        Assert.True(TestHelper.JsonMessageContainsKeyValue(responseText, $"Header-{i}", headerValue));
                    }
                }
            }
        }

        public static IEnumerable<object[]> HeaderValueAndUris()
        {
            foreach (Uri uri in HeaderEchoUris())
            {
                yield return new object[] { "X-CustomHeader", "x-value", uri };
                yield return new object[] { "MyHeader", "1, 2, 3", uri };

                // Construct a header value with every valid character (except space)
                string allchars = "";
                for (int i = 0x21; i <= 0x7E; i++)
                {
                    allchars = allchars + (char)i;
                }

                // Put a space in the middle so it's not interpreted as insignificant leading/trailing whitespace
                allchars = allchars + " " + allchars;

                yield return new object[] { "All-Valid-Chars-Header", allchars, uri };
            }
        }

        public static IEnumerable<Uri> HeaderEchoUris()
        {
            foreach (Uri uri in Configuration.Http.EchoServerList)
            {
                yield return uri;
                yield return Configuration.Http.RedirectUriForDestinationUri(
                    secure: false,
                    statusCode: 302,
                    destinationUri: uri,
                    hops: 1);
            }
        }

        public static IEnumerable<object[]> HeaderEchoUrisMemberData() => HeaderEchoUris().Select(uri => new object[] { uri });

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP HTTP ignores invalid headers")]
        [Theory]
        [InlineData(":")]
        [InlineData("\x1234: \x5678")]
        [InlineData("nocolon")]
        [InlineData("no colon")]
        [InlineData("Content-Length      ")]
        public async Task GetAsync_InvalidHeaderNameValue_ThrowsHttpRequestException(string invalidHeader)
        {
            if (IsCurlHandler && invalidHeader.Contains(':'))
            {
                // Issue #27172
                // CurlHandler allows these headers as long as they have a colon.
                return;
            }

            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var client = CreateHttpClient())
                {
                    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetStringAsync(uri));
                }
            }, server => server.AcceptConnectionSendCustomResponseAndCloseAsync($"HTTP/1.1 200 OK\r\n{invalidHeader}\r\nContent-Length: 11\r\n\r\nhello world"));
        }

        [Fact]
        public async Task PostAsync_ManyDifferentRequestHeaders_SentCorrectly()
        {
            if (IsWinHttpHandler)
            {
                // Issue #27171
                // Fails consistently with:
                // System.InvalidCastException: "Unable to cast object of type 'System.Object[]' to type 'System.Net.Http.WinHttpRequestState'"
                // This appears to be due to adding the Expect: 100-continue header, which causes winhttp
                // to fail with a "The parameter is incorrect" error, which in turn causes the request to
                // be torn down, and in doing so, we this this during disposal of the SafeWinHttpHandle.
                return;
            }

            // Using examples from https://en.wikipedia.org/wiki/List_of_HTTP_header_fields#Request_fields
            // Exercises all exposed request.Headers and request.Content.Headers strongly-typed properties
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var client = CreateHttpClient())
                {
                    byte[] contentArray = Encoding.ASCII.GetBytes("hello world");
                    var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = new ByteArrayContent(contentArray) };

                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                    request.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
                    request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                    request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                    request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));
                    request.Headers.Add("Accept-Datetime", "Thu, 31 May 2007 20:35:00 GMT");
                    request.Headers.Add("Access-Control-Request-Method", "GET");
                    request.Headers.Add("Access-Control-Request-Headers", "GET");
                    request.Headers.Add("Age", "12");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "QWxhZGRpbjpvcGVuIHNlc2FtZQ==");
                    request.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
                    request.Headers.Connection.Add("close");
                    request.Headers.Add("Cookie", "$Version=1; Skin=new");
                    request.Content.Headers.ContentLength = contentArray.Length;
                    request.Content.Headers.ContentMD5 = MD5.Create().ComputeHash(contentArray);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    request.Headers.Date = DateTimeOffset.Parse("Tue, 15 Nov 1994 08:12:31 GMT");
                    request.Headers.Expect.Add(new NameValueWithParametersHeaderValue("100-continue"));
                    request.Headers.Add("Forwarded", "for=192.0.2.60;proto=http;by=203.0.113.43");
                    request.Headers.Add("From", "User Name <user@example.com>");
                    request.Headers.Host = "en.wikipedia.org:8080";
                    request.Headers.IfMatch.Add(new EntityTagHeaderValue("\"37060cd8c284d8af7ad3082f209582d\""));
                    request.Headers.IfModifiedSince = DateTimeOffset.Parse("Sat, 29 Oct 1994 19:43:31 GMT");
                    request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"737060cd8c284d8af7ad3082f209582d\""));
                    request.Headers.IfRange = new RangeConditionHeaderValue(DateTimeOffset.Parse("Wed, 21 Oct 2015 07:28:00 GMT"));
                    request.Headers.IfUnmodifiedSince = DateTimeOffset.Parse("Sat, 29 Oct 1994 19:43:31 GMT");
                    request.Headers.MaxForwards = 10;
                    request.Headers.Add("Origin", "http://www.example-social-network.com");
                    request.Headers.Pragma.Add(new NameValueHeaderValue("no-cache"));
                    request.Headers.ProxyAuthorization = new AuthenticationHeaderValue("Basic", "QWxhZGRpbjpvcGVuIHNlc2FtZQ==");
                    request.Headers.Range = new RangeHeaderValue(500, 999);
                    request.Headers.Referrer = new Uri("http://en.wikipedia.org/wiki/Main_Page");
                    request.Headers.TE.Add(new TransferCodingWithQualityHeaderValue("trailers"));
                    request.Headers.TE.Add(new TransferCodingWithQualityHeaderValue("deflate"));
                    request.Headers.Trailer.Add("MyTrailer");
                    request.Headers.TransferEncoding.Add(new TransferCodingHeaderValue("chunked"));
                    request.Headers.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Mozilla", "5.0")));
                    request.Headers.Upgrade.Add(new ProductHeaderValue("HTTPS", "1.3"));
                    request.Headers.Upgrade.Add(new ProductHeaderValue("IRC", "6.9"));
                    request.Headers.Upgrade.Add(new ProductHeaderValue("RTA", "x11"));
                    request.Headers.Upgrade.Add(new ProductHeaderValue("websocket"));
                    request.Headers.Via.Add(new ViaHeaderValue("1.0", "fred"));
                    request.Headers.Via.Add(new ViaHeaderValue("1.1", "example.com", null, "(Apache/1.1)"));
                    request.Headers.Warning.Add(new WarningHeaderValue(199, "-", "\"Miscellaneous warning\""));
                    request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                    request.Headers.Add("DNT", "1 (Do Not Track Enabled)");
                    request.Headers.Add("X-Forwarded-For", "client1");
                    request.Headers.Add("X-Forwarded-For", "proxy1");
                    request.Headers.Add("X-Forwarded-For", "proxy2");
                    request.Headers.Add("X-Forwarded-Host", "en.wikipedia.org:8080");
                    request.Headers.Add("X-Forwarded-Proto", "https");
                    request.Headers.Add("Front-End-Https", "https");
                    request.Headers.Add("X-Http-Method-Override", "DELETE");
                    request.Headers.Add("X-ATT-DeviceId", "GT-P7320/P7320XXLPG");
                    request.Headers.Add("X-Wap-Profile", "http://wap.samsungmobile.com/uaprof/SGH-I777.xml");
                    request.Headers.Add("Proxy-Connection", "keep-alive");
                    request.Headers.Add("X-UIDH", "...");
                    request.Headers.Add("X-Csrf-Token", "i8XNjC4b8KVok4uw5RftR38Wgp2BFwql");
                    request.Headers.Add("X-Request-ID", "f058ebd6-02f7-4d3f-942e-904344e8cde5");
                    request.Headers.Add("X-Request-ID", "f058ebd6-02f7-4d3f-942e-904344e8cde5");

                    (await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)).Dispose();
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    var headersSet = new HashSet<string>();
                    string line;
                    while (!string.IsNullOrEmpty(line = await connection.Reader.ReadLineAsync()))
                    {
                        Assert.True(headersSet.Add(line));
                    }

                    await connection.Writer.WriteAsync($"HTTP/1.1 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\nConnection: close\r\nContent-Length: 0\r\n\r\n");
                    while (await connection.Socket.ReceiveAsync(new ArraySegment<byte>(new byte[1000]), SocketFlags.None) > 0);

                    Assert.Contains("Accept-Charset: utf-8", headersSet);
                    Assert.Contains("Accept-Encoding: gzip, deflate", headersSet);
                    Assert.Contains("Accept-Language: en-US", headersSet);
                    Assert.Contains("Accept-Datetime: Thu, 31 May 2007 20:35:00 GMT", headersSet);
                    Assert.Contains("Access-Control-Request-Method: GET", headersSet);
                    Assert.Contains("Access-Control-Request-Headers: GET", headersSet);
                    Assert.Contains("Age: 12", headersSet);
                    Assert.Contains("Authorization: Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==", headersSet);
                    Assert.Contains("Cache-Control: no-cache", headersSet);
                    Assert.Contains("Connection: close", headersSet, StringComparer.OrdinalIgnoreCase); // NetFxHandler uses "Close" vs "close"
                    if (!IsNetfxHandler)
                    {
                        Assert.Contains("Cookie: $Version=1; Skin=new", headersSet);
                    }
                    Assert.Contains("Date: Tue, 15 Nov 1994 08:12:31 GMT", headersSet);
                    Assert.Contains("Expect: 100-continue", headersSet);
                    Assert.Contains("Forwarded: for=192.0.2.60;proto=http;by=203.0.113.43", headersSet);
                    Assert.Contains("From: User Name <user@example.com>", headersSet);
                    Assert.Contains("Host: en.wikipedia.org:8080", headersSet);
                    Assert.Contains("If-Match: \"37060cd8c284d8af7ad3082f209582d\"", headersSet);
                    Assert.Contains("If-Modified-Since: Sat, 29 Oct 1994 19:43:31 GMT", headersSet);
                    Assert.Contains("If-None-Match: \"737060cd8c284d8af7ad3082f209582d\"", headersSet);
                    Assert.Contains("If-Range: Wed, 21 Oct 2015 07:28:00 GMT", headersSet);
                    Assert.Contains("If-Unmodified-Since: Sat, 29 Oct 1994 19:43:31 GMT", headersSet);
                    Assert.Contains("Max-Forwards: 10", headersSet);
                    Assert.Contains("Origin: http://www.example-social-network.com", headersSet);
                    Assert.Contains("Pragma: no-cache", headersSet);
                    Assert.Contains("Proxy-Authorization: Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==", headersSet);
                    Assert.Contains("Range: bytes=500-999", headersSet);
                    Assert.Contains("Referer: http://en.wikipedia.org/wiki/Main_Page", headersSet);
                    Assert.Contains("TE: trailers, deflate", headersSet);
                    Assert.Contains("Trailer: MyTrailer", headersSet);
                    Assert.Contains("Transfer-Encoding: chunked", headersSet);
                    Assert.Contains("User-Agent: Mozilla/5.0", headersSet);
                    Assert.Contains("Upgrade: HTTPS/1.3, IRC/6.9, RTA/x11, websocket", headersSet);
                    Assert.Contains("Via: 1.0 fred, 1.1 example.com (Apache/1.1)", headersSet);
                    Assert.Contains("Warning: 199 - \"Miscellaneous warning\"", headersSet);
                    Assert.Contains("X-Requested-With: XMLHttpRequest", headersSet);
                    Assert.Contains("DNT: 1 (Do Not Track Enabled)", headersSet);
                    Assert.Contains("X-Forwarded-For: client1, proxy1, proxy2", headersSet);
                    Assert.Contains("X-Forwarded-Host: en.wikipedia.org:8080", headersSet);
                    Assert.Contains("X-Forwarded-Proto: https", headersSet);
                    Assert.Contains("Front-End-Https: https", headersSet);
                    Assert.Contains("X-Http-Method-Override: DELETE", headersSet);
                    Assert.Contains("X-ATT-DeviceId: GT-P7320/P7320XXLPG", headersSet);
                    Assert.Contains("X-Wap-Profile: http://wap.samsungmobile.com/uaprof/SGH-I777.xml", headersSet);
                    if (!IsNetfxHandler && !PlatformDetection.IsUap)
                    {
                        Assert.Contains("Proxy-Connection: keep-alive", headersSet);
                    }
                    Assert.Contains("X-UIDH: ...", headersSet);
                    Assert.Contains("X-Csrf-Token: i8XNjC4b8KVok4uw5RftR38Wgp2BFwql", headersSet);
                    Assert.Contains("X-Request-ID: f058ebd6-02f7-4d3f-942e-904344e8cde5, f058ebd6-02f7-4d3f-942e-904344e8cde5", headersSet);
                });
            });
        }

        public static IEnumerable<object[]> GetAsync_ManyDifferentResponseHeaders_ParsedCorrectly_MemberData() =>
            from newline in new[] { "\n", "\r\n" }
            from fold in new[] { "", newline + " ", newline + "\t", newline + "    " }
            from dribble in new[] { false, true }
            select new object[] { newline, fold, dribble };

        [ActiveIssue(30060, TargetFrameworkMonikers.Uap)]
        [Theory]
        [MemberData(nameof(GetAsync_ManyDifferentResponseHeaders_ParsedCorrectly_MemberData))]
        public async Task GetAsync_ManyDifferentResponseHeaders_ParsedCorrectly(string newline, string fold, bool dribble)
        {
            if (IsCurlHandler && !string.IsNullOrEmpty(fold))
            {
                // CurlHandler doesn't currently support folded headers.
                return;
            }

            if (IsNetfxHandler && newline == "\n")
            {
                // NetFxHandler doesn't allow LF-only line endings.
                return;
            }

            // Using examples from https://en.wikipedia.org/wiki/List_of_HTTP_header_fields#Response_fields
            // Exercises all exposed response.Headers and response.Content.Headers strongly-typed properties
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var client = CreateHttpClient())
                using (HttpResponseMessage resp = await client.GetAsync(uri))
                {
                    Assert.Equal("1.1", resp.Version.ToString());
                    Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
                    Assert.Contains("*", resp.Headers.GetValues("Access-Control-Allow-Origin"));
                    Assert.Contains("text/example;charset=utf-8", resp.Headers.GetValues("Accept-Patch"));
                    Assert.Contains("bytes", resp.Headers.AcceptRanges);
                    Assert.Equal(TimeSpan.FromSeconds(12), resp.Headers.Age.GetValueOrDefault());
                    Assert.Contains("Bearer 63123a47139a49829bcd8d03005ca9d7", resp.Headers.GetValues("Authorization"));
                    Assert.Contains("GET", resp.Content.Headers.Allow);
                    Assert.Contains("HEAD", resp.Content.Headers.Allow);
                    Assert.Contains("http/1.1=\"http2.example.com:8001\"; ma=7200", resp.Headers.GetValues("Alt-Svc"));
                    Assert.Equal(TimeSpan.FromSeconds(3600), resp.Headers.CacheControl.MaxAge.GetValueOrDefault());
                    Assert.Contains("close", resp.Headers.Connection);
                    Assert.True(resp.Headers.ConnectionClose.GetValueOrDefault());
                    Assert.Equal("attachment", resp.Content.Headers.ContentDisposition.DispositionType);
                    Assert.Equal("\"fname.ext\"", resp.Content.Headers.ContentDisposition.FileName);
                    Assert.Contains("gzip", resp.Content.Headers.ContentEncoding);
                    Assert.Contains("da", resp.Content.Headers.ContentLanguage);
                    Assert.Equal(new Uri("/index.htm", UriKind.Relative), resp.Content.Headers.ContentLocation);
                    Assert.Equal(Convert.FromBase64String("Q2hlY2sgSW50ZWdyaXR5IQ=="), resp.Content.Headers.ContentMD5);
                    Assert.Equal("bytes", resp.Content.Headers.ContentRange.Unit);
                    Assert.Equal(21010, resp.Content.Headers.ContentRange.From.GetValueOrDefault());
                    Assert.Equal(47021, resp.Content.Headers.ContentRange.To.GetValueOrDefault());
                    Assert.Equal(47022, resp.Content.Headers.ContentRange.Length.GetValueOrDefault());
                    Assert.Equal("text/html", resp.Content.Headers.ContentType.MediaType);
                    Assert.Equal("utf-8", resp.Content.Headers.ContentType.CharSet);
                    Assert.Equal(DateTimeOffset.Parse("Tue, 15 Nov 1994 08:12:31 GMT"), resp.Headers.Date.GetValueOrDefault());
                    Assert.Equal("\"737060cd8c284d8af7ad3082f209582d\"", resp.Headers.ETag.Tag);
                    Assert.Equal(DateTimeOffset.Parse("Thu, 01 Dec 1994 16:00:00 GMT"), resp.Content.Headers.Expires.GetValueOrDefault());
                    Assert.Equal(DateTimeOffset.Parse("Tue, 15 Nov 1994 12:45:26 GMT"), resp.Content.Headers.LastModified.GetValueOrDefault());
                    Assert.Contains("</feed>; rel=\"alternate\"", resp.Headers.GetValues("Link"));
                    Assert.Equal(new Uri("http://www.w3.org/pub/WWW/People.html"), resp.Headers.Location);
                    Assert.Contains("CP=\"This is not a P3P policy!\"", resp.Headers.GetValues("P3P"));
                    Assert.Contains(new NameValueHeaderValue("no-cache"), resp.Headers.Pragma);
                    Assert.Contains(new AuthenticationHeaderValue("basic"), resp.Headers.ProxyAuthenticate);
                    Assert.Contains("max-age=2592000; pin-sha256=\"E9CZ9INDbd+2eRQozYqqbQ2yXLVKB9+xcprMF+44U1g=\"", resp.Headers.GetValues("Public-Key-Pins"));
                    Assert.Equal(TimeSpan.FromSeconds(120), resp.Headers.RetryAfter.Delta.GetValueOrDefault());
                    Assert.Contains(new ProductInfoHeaderValue("Apache", "2.4.1"), resp.Headers.Server);
                    Assert.Contains("UserID=JohnDoe; Max-Age=3600; Version=1", resp.Headers.GetValues("Set-Cookie"));
                    Assert.Contains("max-age=16070400; includeSubDomains", resp.Headers.GetValues("Strict-Transport-Security"));
                    Assert.Contains("Max-Forwards", resp.Headers.Trailer);
                    Assert.Contains("?", resp.Headers.GetValues("Tk"));
                    Assert.Contains(new ProductHeaderValue("HTTPS", "1.3"), resp.Headers.Upgrade);
                    Assert.Contains(new ProductHeaderValue("IRC", "6.9"), resp.Headers.Upgrade);
                    Assert.Contains(new ProductHeaderValue("websocket"), resp.Headers.Upgrade);
                    Assert.Contains("Accept-Language", resp.Headers.Vary);
                    Assert.Contains(new ViaHeaderValue("1.0", "fred"), resp.Headers.Via);
                    Assert.Contains(new ViaHeaderValue("1.1", "example.com", null, "(Apache/1.1)"), resp.Headers.Via);
                    Assert.Contains(new WarningHeaderValue(199, "-", "\"Miscellaneous warning\"", DateTimeOffset.Parse("Wed, 21 Oct 2015 07:28:00 GMT")), resp.Headers.Warning);
                    Assert.Contains(new AuthenticationHeaderValue("Basic"), resp.Headers.WwwAuthenticate);
                    Assert.Contains("deny", resp.Headers.GetValues("X-Frame-Options"));
                    Assert.Contains("default-src 'self'", resp.Headers.GetValues("X-WebKit-CSP"));
                    Assert.Contains("5; url=http://www.w3.org/pub/WWW/People.html", resp.Headers.GetValues("Refresh"));
                    Assert.Contains("200 OK", resp.Headers.GetValues("Status"));
                    Assert.Contains("<origin>[, <origin>]*", resp.Headers.GetValues("Timing-Allow-Origin"));
                    Assert.Contains("42.666", resp.Headers.GetValues("X-Content-Duration"));
                    Assert.Contains("nosniff", resp.Headers.GetValues("X-Content-Type-Options"));
                    Assert.Contains("PHP/5.4.0", resp.Headers.GetValues("X-Powered-By"));
                    Assert.Contains("f058ebd6-02f7-4d3f-942e-904344e8cde5", resp.Headers.GetValues("X-Request-ID"));
                    Assert.Contains("IE=EmulateIE7", resp.Headers.GetValues("X-UA-Compatible"));
                    Assert.Contains("1; mode=block", resp.Headers.GetValues("X-XSS-Protection"));
                }
            }, server => server.AcceptConnectionSendCustomResponseAndCloseAsync(
                $"HTTP/1.1 200 OK{newline}" +
                $"Access-Control-Allow-Origin:{fold} *{newline}" +
                $"Accept-Patch:{fold} text/example;charset=utf-8{newline}" +
                $"Accept-Ranges:{fold} bytes{newline}" +
                $"Age: {fold}12{newline}" +
                $"Authorization: Bearer 63123a47139a49829bcd8d03005ca9d7{newline}" +
                $"Allow: {fold}GET, HEAD{newline}" +
                $"Alt-Svc:{fold} http/1.1=\"http2.example.com:8001\"; ma=7200{newline}" +
                $"Cache-Control: {fold}max-age=3600{newline}" +
                $"Connection:{fold} close{newline}" +
                $"Content-Disposition: {fold}attachment;{fold} filename=\"fname.ext\"{newline}" +
                $"Content-Encoding: {fold}gzip{newline}" +
                $"Content-Language:{fold} da{newline}" +
                $"Content-Location: {fold}/index.htm{newline}" +
                $"Content-MD5:{fold} Q2hlY2sgSW50ZWdyaXR5IQ=={newline}" +
                $"Content-Range: {fold}bytes {fold}21010-47021/47022{newline}" +
                $"Content-Type: text/html;{fold} charset=utf-8{newline}" +
                $"Date: Tue, 15 Nov 1994{fold} 08:12:31 GMT{newline}" +
                $"ETag: {fold}\"737060cd8c284d8af7ad3082f209582d\"{newline}" +
                $"Expires: Thu,{fold} 01 Dec 1994 16:00:00 GMT{newline}" +
                $"Last-Modified:{fold} Tue, 15 Nov 1994 12:45:26 GMT{newline}" +
                $"Link:{fold} </feed>; rel=\"alternate\"{newline}" +
                $"Location:{fold} http://www.w3.org/pub/WWW/People.html{newline}" +
                $"P3P: {fold}CP=\"This is not a P3P policy!\"{newline}" +
                $"Pragma: {fold}no-cache{newline}" +
                $"Proxy-Authenticate:{fold} Basic{newline}" +
                $"Public-Key-Pins:{fold} max-age=2592000; pin-sha256=\"E9CZ9INDbd+2eRQozYqqbQ2yXLVKB9+xcprMF+44U1g=\"{newline}" +
                $"Retry-After: {fold}120{newline}" +
                $"Server: {fold}Apache/2.4.1{fold} (Unix){newline}" +
                $"Set-Cookie: {fold}UserID=JohnDoe; Max-Age=3600; Version=1{newline}" +
                $"Strict-Transport-Security: {fold}max-age=16070400; includeSubDomains{newline}" +
                $"Trailer: {fold}Max-Forwards{newline}" +
                $"Tk: {fold}?{newline}" +
                $"Upgrade: HTTPS/1.3,{fold} IRC/6.9,{fold} RTA/x11, {fold}websocket{newline}" +
                $"Vary:{fold} Accept-Language{newline}" +
                $"Via:{fold} 1.0 fred, 1.1 example.com{fold} (Apache/1.1){newline}" +
                $"Warning:{fold} 199 - \"Miscellaneous warning\" \"Wed, 21 Oct 2015 07:28:00 GMT\"{newline}" +
                $"WWW-Authenticate: {fold}Basic{newline}" +
                $"X-Frame-Options: {fold}deny{newline}" +
                $"X-WebKit-CSP: default-src 'self'{newline}" +
                $"Refresh: {fold}5; url=http://www.w3.org/pub/WWW/People.html{newline}" +
                $"Status: {fold}200 OK{newline}" +
                $"Timing-Allow-Origin: {fold}<origin>[, <origin>]*{newline}" +
                $"Upgrade-Insecure-Requests:{fold} 1{newline}" +
                $"X-Content-Duration:{fold} 42.666{newline}" +
                $"X-Content-Type-Options: {fold}nosniff{newline}" +
                $"X-Powered-By: {fold}PHP/5.4.0{newline}" +
                $"X-Request-ID:{fold} f058ebd6-02f7-4d3f-942e-904344e8cde5{newline}" +
                $"X-UA-Compatible: {fold}IE=EmulateIE7{newline}" +
                $"X-XSS-Protection:{fold} 1; mode=block{newline}" +
                $"{newline}"),
                dribble ? new LoopbackServer.Options { StreamWrapper = s => new DribbleStream(s) } : null);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetAsync_TrailingHeaders_Ignored(bool includeTrailerHeader)
        {
            if (IsCurlHandler)
            {
                // ActiveIssue #17174: CurlHandler has a problem here
                // https://github.com/curl/curl/issues/1354
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        getResponseTask,
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            "HTTP/1.1 200 OK\r\n" +
                            "Connection: close\r\n" +
                            "Transfer-Encoding: chunked\r\n" +
                            (includeTrailerHeader ? "Trailer: MyCoolTrailerHeader\r\n" : "") +
                            "\r\n" +
                            "4\r\n" +
                            "data\r\n" +
                            "0\r\n" +
                            "MyCoolTrailerHeader: amazingtrailer\r\n" +
                            "\r\n"));

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        if (includeTrailerHeader)
                        {
                            Assert.Contains("MyCoolTrailerHeader", response.Headers.GetValues("Trailer"));
                        }
                        Assert.False(response.Headers.Contains("MyCoolTrailerHeader"), "Trailer should have been ignored");

                        string data = await response.Content.ReadAsStringAsync();
                        Assert.Contains("data", data);
                        Assert.DoesNotContain("MyCoolTrailerHeader", data);
                        Assert.DoesNotContain("amazingtrailer", data);
                    }
                }
            });
        }

        [Fact]
        public async Task GetAsync_NonTraditionalChunkSizes_Accepted()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        getResponseTask,
                        server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            "HTTP/1.1 200 OK\r\n" +
                            "Connection: close\r\n" +
                            "Transfer-Encoding: chunked\r\n" +
                            "\r\n" +
                            "4    \r\n" + // whitespace after size
                            "data\r\n" +
                            "5;somekey=somevalue\r\n" + // chunk extension
                            "hello\r\n" +
                            "7\t ;chunkextension\r\n" + // tabs/spaces then chunk extension
                            "netcore\r\n" +
                            "0\r\n" +
                            "\r\n"));

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        string data = await response.Content.ReadAsStringAsync();
                        Assert.Contains("data", data);
                        Assert.Contains("hello", data);
                        Assert.Contains("netcore", data);
                        Assert.DoesNotContain("somekey", data);
                        Assert.DoesNotContain("somevalue", data);
                        Assert.DoesNotContain("chunkextension", data);
                    }
                }
            });
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Test hangs due to bugs in WinRT HTTP stack")]
        [Theory]
        [InlineData("")] // missing size
        [InlineData("    ")] // missing size
        [InlineData("10000000000000000")] // overflowing size
        [InlineData("xyz")] // non-hex
        [InlineData("7gibberish")] // valid size then gibberish
        [InlineData("7\v\f")] // unacceptable whitespace
        public async Task GetAsync_InvalidChunkSize_ThrowsHttpRequestException(string chunkSize)
        {
            if (IsCurlHandler)
            {
                // libcurl allows any arbitrary characters after the hex value
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    string partialResponse = "HTTP/1.1 200 OK\r\n" +
                        "Transfer-Encoding: chunked\r\n" +
                        "\r\n" +
                        $"{chunkSize}\r\n";

                    var tcs = new TaskCompletionSource<bool>();
                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                        {
                            await connection.ReadRequestHeaderAndSendCustomResponseAsync(partialResponse);
                            await tcs.Task;
                        });

                    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
                    tcs.SetResult(true);
                    await serverTask;
                }
            });
        }
        
        [Fact]
        public async Task GetAsync_InvalidChunkTerminator_ThrowsHttpRequestException()
        {
            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
                }
            }, server => server.AcceptConnectionSendCustomResponseAndCloseAsync(
                "HTTP/1.1 200 OK\r\n" +
                "Connection: close\r\n" +
                "Transfer-Encoding: chunked\r\n" +
                "\r\n" +
                "5\r\n" +
                "hello" + // missing \r\n terminator
                            //"5\r\n" +
                            //"world" + // missing \r\n terminator
                "0\r\n" +
                "\r\n"));
        }

        [Fact]
        public async Task GetAsync_InfiniteChunkSize_ThrowsHttpRequestException()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    client.Timeout = Timeout.InfiniteTimeSpan;

                    var cts = new CancellationTokenSource();
                    var tcs = new TaskCompletionSource<bool>();
                    Task serverTask = server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync("HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n");
                        TextWriter writer = connection.Writer;
                        try
                        {
                            while (!cts.IsCancellationRequested) // infinite to make sure implementation doesn't OOM
                            {
                                await writer.WriteAsync(new string(' ', 10000));
                                await Task.Delay(1);
                            }
                        }
                        catch { }
                    });

                    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
                    cts.Cancel();
                    await serverTask;
                }
            });
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "No exception thrown")]
        [Fact]
        public async Task SendAsync_TransferEncodingSetButNoRequestContent_Throws()
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "http://bing.com");
            req.Headers.TransferEncodingChunked = true;
            using (HttpClient c = CreateHttpClient())
            {
                HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() => c.SendAsync(req));
                Assert.IsType<InvalidOperationException>(error.InnerException);
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task GetAsync_ResponseHeadersRead_ReadFromEachIterativelyDoesntDeadlock()
        {
            using (HttpClient client = CreateHttpClient())
            {
                const int NumGets = 5;
                Task<HttpResponseMessage>[] responseTasks = (from _ in Enumerable.Range(0, NumGets)
                                                             select client.GetAsync(Configuration.Http.RemoteEchoServer, HttpCompletionOption.ResponseHeadersRead)).ToArray();
                for (int i = responseTasks.Length - 1; i >= 0; i--) // read backwards to increase likelihood that we wait on a different task than has data available
                {
                    using (HttpResponseMessage response = await responseTasks[i])
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        _output.WriteLine(responseContent);
                        TestHelper.VerifyResponseBody(
                            responseContent,
                            response.Content.Headers.ContentMD5,
                            false,
                            null);
                    }
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task SendAsync_HttpRequestMsgResponseHeadersRead_StatusCodeOK()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, Configuration.Http.SecureRemoteEchoServer);
            using (HttpClient client = CreateHttpClient())
            {
                using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        null);
                }
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "netfx's ConnectStream.ReadAsync tries to read beyond data already buffered, causing hangs #18864")]
        [OuterLoop("Slow response")]
        [Fact]
        public async Task SendAsync_ReadFromSlowStreamingServer_PartialDataReturned()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    Task<HttpResponseMessage> getResponse = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAndSendCustomResponseAsync(
                            "HTTP/1.1 200 OK\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            "Content-Length: 16000\r\n" +
                            "\r\n" +
                            "less than 16000 bytes");

                        using (HttpResponseMessage response = await getResponse)
                        {
                            var buffer = new byte[8000];
                            using (Stream clientStream = await response.Content.ReadAsStreamAsync())
                            {
                                int bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
                                _output.WriteLine($"Bytes read from stream: {bytesRead}");
                                Assert.True(bytesRead < buffer.Length, "bytesRead should be less than buffer.Length");
                            }
                        }
                    });
                }
            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(null)]
        public async Task ReadAsStreamAsync_HandlerProducesWellBehavedResponseStream(bool? chunked)
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                using (var client = new HttpMessageInvoker(CreateHttpClientHandler()))
                using (HttpResponseMessage response = await client.SendAsync(request, CancellationToken.None))
                {
                    using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        Assert.Same(responseStream, await response.Content.ReadAsStreamAsync());

                        // Boolean properties returning correct values
                        Assert.True(responseStream.CanRead);
                        Assert.False(responseStream.CanWrite);
                        Assert.False(responseStream.CanSeek);

                        // Not supported operations
                        Assert.Throws<NotSupportedException>(() => responseStream.BeginWrite(new byte[1], 0, 1, null, null));
                        Assert.Throws<NotSupportedException>(() => responseStream.Length);
                        Assert.Throws<NotSupportedException>(() => responseStream.Position);
                        Assert.Throws<NotSupportedException>(() => responseStream.Position = 0);
                        Assert.Throws<NotSupportedException>(() => responseStream.Seek(0, SeekOrigin.Begin));
                        Assert.Throws<NotSupportedException>(() => responseStream.SetLength(0));
                        Assert.Throws<NotSupportedException>(() => responseStream.Write(new byte[1], 0, 1));
                        Assert.Throws<NotSupportedException>(() => responseStream.Write(new Span<byte>(new byte[1])));
                        Assert.Throws<NotSupportedException>(() => { responseStream.WriteAsync(new Memory<byte>(new byte[1])); });
                        Assert.Throws<NotSupportedException>(() => { responseStream.WriteAsync(new byte[1], 0, 1); });
                        Assert.Throws<NotSupportedException>(() => responseStream.WriteByte(1));

                        // Invalid arguments
                        var nonWritableStream = new MemoryStream(new byte[1], false);
                        var disposedStream = new MemoryStream();
                        disposedStream.Dispose();
                        Assert.Throws<ArgumentNullException>(() => responseStream.CopyTo(null));
                        Assert.Throws<ArgumentOutOfRangeException>(() => responseStream.CopyTo(Stream.Null, 0));
                        Assert.Throws<ArgumentNullException>(() => { responseStream.CopyToAsync(null, 100, default); });
                        Assert.Throws<ArgumentOutOfRangeException>(() => { responseStream.CopyToAsync(Stream.Null, 0, default); });
                        Assert.Throws<ArgumentOutOfRangeException>(() => { responseStream.CopyToAsync(Stream.Null, -1, default); });
                        Assert.Throws<NotSupportedException>(() => { responseStream.CopyToAsync(nonWritableStream, 100, default); });
                        Assert.Throws<ObjectDisposedException>(() => { responseStream.CopyToAsync(disposedStream, 100, default); });
                        Assert.Throws<ArgumentNullException>(() => responseStream.Read(null, 0, 100));
                        Assert.Throws<ArgumentOutOfRangeException>(() => responseStream.Read(new byte[1], -1, 1));
                        Assert.ThrowsAny<ArgumentException>(() => responseStream.Read(new byte[1], 2, 1));
                        Assert.Throws<ArgumentOutOfRangeException>(() => responseStream.Read(new byte[1], 0, -1));
                        Assert.ThrowsAny<ArgumentException>(() => responseStream.Read(new byte[1], 0, 2));
                        Assert.Throws<ArgumentNullException>(() => responseStream.BeginRead(null, 0, 100, null, null));
                        Assert.Throws<ArgumentOutOfRangeException>(() => responseStream.BeginRead(new byte[1], -1, 1, null, null));
                        Assert.ThrowsAny<ArgumentException>(() => responseStream.BeginRead(new byte[1], 2, 1, null, null));
                        Assert.Throws<ArgumentOutOfRangeException>(() => responseStream.BeginRead(new byte[1], 0, -1, null, null));
                        Assert.ThrowsAny<ArgumentException>(() => responseStream.BeginRead(new byte[1], 0, 2, null, null));
                        Assert.Throws<ArgumentNullException>(() => responseStream.EndRead(null));
                        if (IsNetfxHandler)
                        {
                            // Argument exceptions on netfx are thrown out of these asynchronously rather than synchronously
                            await Assert.ThrowsAsync<ArgumentNullException>(() => responseStream.ReadAsync(null, 0, 100, default));
                            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => responseStream.ReadAsync(new byte[1], -1, 1, default));
                            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => responseStream.ReadAsync(new byte[1], 2, 1, default));
                            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => responseStream.ReadAsync(new byte[1], 0, -1, default));
                            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => responseStream.ReadAsync(new byte[1], 0, 2, default));
                        }
                        else
                        {
                            Assert.Throws<ArgumentNullException>(() => { responseStream.ReadAsync(null, 0, 100, default); });
                            Assert.Throws<ArgumentOutOfRangeException>(() => { responseStream.ReadAsync(new byte[1], -1, 1, default); });
                            Assert.ThrowsAny<ArgumentException>(() => { responseStream.ReadAsync(new byte[1], 2, 1, default); });
                            Assert.Throws<ArgumentOutOfRangeException>(() => { responseStream.ReadAsync(new byte[1], 0, -1, default); });
                            Assert.ThrowsAny<ArgumentException>(() => { responseStream.ReadAsync(new byte[1], 0, 2, default); });
                        }

                        // Various forms of reading
                        var buffer = new byte[1];

                        Assert.Equal('h', responseStream.ReadByte());

                        Assert.Equal(1, await Task.Factory.FromAsync(responseStream.BeginRead, responseStream.EndRead, buffer, 0, 1, null));
                        Assert.Equal((byte)'e', buffer[0]);

                        Assert.Equal(1, await responseStream.ReadAsync(new Memory<byte>(buffer)));
                        Assert.Equal((byte)'l', buffer[0]);

                        Assert.Equal(1, await responseStream.ReadAsync(buffer, 0, 1));
                        Assert.Equal((byte)'l', buffer[0]);

                        Assert.Equal(1, responseStream.Read(new Span<byte>(buffer)));
                        Assert.Equal((byte)'o', buffer[0]);

                        Assert.Equal(1, responseStream.Read(buffer, 0, 1));
                        Assert.Equal((byte)' ', buffer[0]);

                        if (!IsNetfxHandler)
                        {
                            // Doing any of these 0-byte reads causes the connection to fail.
                            Assert.Equal(0, await Task.Factory.FromAsync(responseStream.BeginRead, responseStream.EndRead, Array.Empty<byte>(), 0, 0, null));
                            Assert.Equal(0, await responseStream.ReadAsync(Memory<byte>.Empty));
                            Assert.Equal(0, await responseStream.ReadAsync(Array.Empty<byte>(), 0, 0));
                            Assert.Equal(0, responseStream.Read(Span<byte>.Empty));
                            Assert.Equal(0, responseStream.Read(Array.Empty<byte>(), 0, 0));
                        }

                        // And copying
                        var ms = new MemoryStream();
                        await responseStream.CopyToAsync(ms);
                        Assert.Equal("world", Encoding.ASCII.GetString(ms.ToArray()));

                        // Read and copy again once we've exhausted all data
                        ms = new MemoryStream();
                        await responseStream.CopyToAsync(ms);
                        responseStream.CopyTo(ms);
                        Assert.Equal(0, ms.Length);
                        Assert.Equal(-1, responseStream.ReadByte());
                        Assert.Equal(0, responseStream.Read(buffer, 0, 1));
                        Assert.Equal(0, responseStream.Read(new Span<byte>(buffer)));
                        Assert.Equal(0, await responseStream.ReadAsync(buffer, 0, 1));
                        Assert.Equal(0, await responseStream.ReadAsync(new Memory<byte>(buffer)));
                        Assert.Equal(0, await Task.Factory.FromAsync(responseStream.BeginRead, responseStream.EndRead, buffer, 0, 1, null));
                    }
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestHeaderAsync();
                    await connection.Writer.WriteAsync("HTTP/1.1 200 OK\r\n");
                    switch (chunked)
                    {
                        case true:
                            await connection.Writer.WriteAsync("Transfer-Encoding: chunked\r\n\r\n3\r\nhel\r\n8\r\nlo world\r\n0\r\n\r\n");
                            break;

                        case false:
                            await connection.Writer.WriteAsync("Content-Length: 11\r\n\r\nhello world");
                            break;

                        case null:
                            await connection.Writer.WriteAsync("\r\nhello world");
                            break;
                    }
                });
            });
        }

        [Fact]
        public async Task ReadAsStreamAsync_EmptyResponseBody_HandlerProducesWellBehavedResponseStream()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var client = new HttpMessageInvoker(CreateHttpClientHandler()))
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, uri);

                    using (HttpResponseMessage response = await client.SendAsync(request, CancellationToken.None))
                    using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        // Boolean properties returning correct values
                        Assert.True(responseStream.CanRead);
                        Assert.False(responseStream.CanWrite);
                        Assert.False(responseStream.CanSeek);

                        // Not supported operations
                        Assert.Throws<NotSupportedException>(() => responseStream.BeginWrite(new byte[1], 0, 1, null, null));
                        Assert.Throws<NotSupportedException>(() => responseStream.Length);
                        Assert.Throws<NotSupportedException>(() => responseStream.Position);
                        Assert.Throws<NotSupportedException>(() => responseStream.Position = 0);
                        Assert.Throws<NotSupportedException>(() => responseStream.Seek(0, SeekOrigin.Begin));
                        Assert.Throws<NotSupportedException>(() => responseStream.SetLength(0));
                        Assert.Throws<NotSupportedException>(() => responseStream.Write(new byte[1], 0, 1));
                        Assert.Throws<NotSupportedException>(() => responseStream.Write(new Span<byte>(new byte[1])));
                        await Assert.ThrowsAsync<NotSupportedException>(async () => await responseStream.WriteAsync(new Memory<byte>(new byte[1])));
                        await Assert.ThrowsAsync<NotSupportedException>(async () => await responseStream.WriteAsync(new byte[1], 0, 1));
                        Assert.Throws<NotSupportedException>(() => responseStream.WriteByte(1));

                        // Invalid arguments
                        var nonWritableStream = new MemoryStream(new byte[1], false);
                        var disposedStream = new MemoryStream();
                        disposedStream.Dispose();
                        Assert.Throws<ArgumentNullException>(() => responseStream.CopyTo(null));
                        Assert.Throws<ArgumentOutOfRangeException>(() => responseStream.CopyTo(Stream.Null, 0));
                        Assert.Throws<ArgumentNullException>(() => { responseStream.CopyToAsync(null, 100, default); });
                        Assert.Throws<ArgumentOutOfRangeException>(() => { responseStream.CopyToAsync(Stream.Null, 0, default); });
                        Assert.Throws<ArgumentOutOfRangeException>(() => { responseStream.CopyToAsync(Stream.Null, -1, default); });
                        Assert.Throws<NotSupportedException>(() => { responseStream.CopyToAsync(nonWritableStream, 100, default); });
                        Assert.Throws<ObjectDisposedException>(() => { responseStream.CopyToAsync(disposedStream, 100, default); });
                        Assert.Throws<ArgumentNullException>(() => responseStream.Read(null, 0, 100));
                        Assert.Throws<ArgumentOutOfRangeException>(() => responseStream.Read(new byte[1], -1, 1));
                        Assert.ThrowsAny<ArgumentException>(() => responseStream.Read(new byte[1], 2, 1));
                        Assert.Throws<ArgumentOutOfRangeException>(() => responseStream.Read(new byte[1], 0, -1));
                        Assert.ThrowsAny<ArgumentException>(() => responseStream.Read(new byte[1], 0, 2));
                        Assert.Throws<ArgumentNullException>(() => responseStream.BeginRead(null, 0, 100, null, null));
                        Assert.Throws<ArgumentOutOfRangeException>(() => responseStream.BeginRead(new byte[1], -1, 1, null, null));
                        Assert.ThrowsAny<ArgumentException>(() => responseStream.BeginRead(new byte[1], 2, 1, null, null));
                        Assert.Throws<ArgumentOutOfRangeException>(() => responseStream.BeginRead(new byte[1], 0, -1, null, null));
                        Assert.ThrowsAny<ArgumentException>(() => responseStream.BeginRead(new byte[1], 0, 2, null, null));
                        Assert.Throws<ArgumentNullException>(() => responseStream.EndRead(null));
                        if (!IsNetfxHandler)
                        {
                            // The netfx handler doesn't validate these arguments.
                            Assert.Throws<ArgumentNullException>(() => { responseStream.CopyTo(null); });
                            Assert.Throws<ArgumentNullException>(() => { responseStream.CopyToAsync(null, 100, default); });
                            Assert.Throws<ArgumentNullException>(() => { responseStream.CopyToAsync(null, 100, default); });
                            Assert.Throws<ArgumentNullException>(() => { responseStream.Read(null, 0, 100); });
                            Assert.Throws<ArgumentNullException>(() => { responseStream.ReadAsync(null, 0, 100, default); });
                            Assert.Throws<ArgumentNullException>(() => { responseStream.BeginRead(null, 0, 100, null, null); });
                        }

                        // Empty reads
                        var buffer = new byte[1];
                        Assert.Equal(-1, responseStream.ReadByte());
                        Assert.Equal(0, await Task.Factory.FromAsync(responseStream.BeginRead, responseStream.EndRead, buffer, 0, 1, null));
                        Assert.Equal(0, await responseStream.ReadAsync(new Memory<byte>(buffer)));
                        Assert.Equal(0, await responseStream.ReadAsync(buffer, 0, 1));
                        Assert.Equal(0, responseStream.Read(new Span<byte>(buffer)));
                        Assert.Equal(0, responseStream.Read(buffer, 0, 1));

                        // Empty copies
                        var ms = new MemoryStream();
                        await responseStream.CopyToAsync(ms);
                        Assert.Equal(0, ms.Length);
                        responseStream.CopyTo(ms);
                        Assert.Equal(0, ms.Length);
                    }
                }
            },
            server => server.AcceptConnectionSendResponseAndCloseAsync());
        }

        [Fact]
        public async Task Dispose_DisposingHandlerCancelsActiveOperationsWithoutResponses()
        {
            if (UseSocketsHttpHandler)
            {
                // TODO #23131: The SocketsHttpHandler isn't correctly handling disposal of the handler.
                // It should cause the outstanding requests to be canceled with OperationCanceledExceptions,
                // whereas currently it's resulting in ObjectDisposedExceptions.
                return;
            }

            if (PlatformDetection.IsFullFramework)
            {
                // Skip test on .NET Framework. It will sometimes not throw TaskCanceledException.
                // Instead it might throw the following top-level and inner exceptions depending
                // on race conditions.
                //
                // System.Net.Http.HttpRequestException : Error while copying content to a stream.
                // ---- System.IO.IOException : The read operation failed, see inner exception.
                //-------- System.Net.WebException : The request was aborted: The request was canceled.
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server1, url1) =>
            {
                await LoopbackServer.CreateServerAsync(async (server2, url2) =>
                {
                    await LoopbackServer.CreateServerAsync(async (server3, url3) =>
                    {
                        var unblockServers = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                        // First server connects but doesn't send any response yet
                        Task serverTask1 = server1.AcceptConnectionAsync(async connection1 =>
                        {
                            await unblockServers.Task;
                        });

                        // Second server connects and sends some but not all headers
                        Task serverTask2 = server2.AcceptConnectionAsync(async connection2 =>
                        {
                            await connection2.ReadRequestHeaderAndSendCustomResponseAsync($"HTTP/1.1 200 OK\r\n");
                            await unblockServers.Task;
                        });

                        // Third server connects and sends all headers and some but not all of the body
                        Task serverTask3 = server3.AcceptConnectionAsync(async connection3 =>
                        {
                            await connection3.ReadRequestHeaderAndSendCustomResponseAsync($"HTTP/1.1 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 20\r\n\r\n");
                            await connection3.Writer.WriteAsync("1234567890");
                            await unblockServers.Task;
                            await connection3.Writer.WriteAsync("1234567890");
                            connection3.Socket.Shutdown(SocketShutdown.Send);
                        });

                        // Make three requests
                        Task<HttpResponseMessage> get1, get2;
                        HttpResponseMessage response3;
                        using (HttpClient client = CreateHttpClient())
                        {
                            get1 = client.GetAsync(url1, HttpCompletionOption.ResponseHeadersRead);
                            get2 = client.GetAsync(url2, HttpCompletionOption.ResponseHeadersRead);
                            response3 = await client.GetAsync(url3, HttpCompletionOption.ResponseHeadersRead);
                        } // Dispose the handler while requests are still outstanding

                        // Requests 1 and 2 should be canceled as we haven't finished receiving their headers
                        await Assert.ThrowsAsync<TaskCanceledException>(() => get1);
                        await Assert.ThrowsAsync<TaskCanceledException>(() => get2);

                        // Request 3 should still be active, and we should be able to receive all of the data.
                        unblockServers.SetResult(true);
                        using (response3)
                        {
                            Assert.Equal("12345678901234567890", await response3.Content.ReadAsStringAsync());
                        }
                    });
                });
            });
        }

        [Theory]
        [InlineData(99)]
        [InlineData(1000)]
        public async Task GetAsync_StatusCodeOutOfRange_ExpectedException(int statusCode)
        {
            if (PlatformDetection.IsUap && statusCode == 99)
            {
                // UAP platform allows this status code due to historical reasons.
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    await server.AcceptConnectionSendCustomResponseAndCloseAsync(
                            $"HTTP/1.1 {statusCode}\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            "Connection: close\r\n" +
                            "\r\n");

                    await Assert.ThrowsAsync<HttpRequestException>(() => getResponseTask);
                }
            });
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework currently does not allow unicode in DNS names")]
        [OuterLoop("Uses external server")]
        [Fact]
        public async Task GetAsync_UnicodeHostName_SuccessStatusCodeInResponse()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                // international version of the Starbucks website
                // punycode: xn--oy2b35ckwhba574atvuzkc.com                
                string server = "http://\uc2a4\ud0c0\ubc85\uc2a4\ucf54\ub9ac\uc544.com";
                using (HttpResponseMessage response = await client.GetAsync(server))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }

#region Post Methods Tests

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(VerifyUploadServers))]
        public async Task PostAsync_CallMethodTwice_StringContent(Uri remoteServer)
        {
            using (HttpClient client = CreateHttpClient())
            {
                string data = "Test String";
                var content = new StringContent(data, Encoding.UTF8);
                content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(data);
                HttpResponseMessage response;
                using (response = await client.PostAsync(remoteServer, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }

                // Repeat call.
                content = new StringContent(data, Encoding.UTF8);
                content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(data);
                using (response = await client.PostAsync(remoteServer, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(VerifyUploadServers))]
        public async Task PostAsync_CallMethod_UnicodeStringContent(Uri remoteServer)
        {
            using (HttpClient client = CreateHttpClient())
            {
                string data = "\ub4f1\uffc7\u4e82\u67ab4\uc6d4\ud1a0\uc694\uc77c\uffda3\u3155\uc218\uffdb";
                var content = new StringContent(data, Encoding.UTF8);
                content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(data);

                using (HttpResponseMessage response = await client.PostAsync(remoteServer, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(VerifyUploadServersStreamsAndExpectedData))]
        public async Task PostAsync_CallMethod_StreamContent(Uri remoteServer, HttpContent content, byte[] expectedData)
        {
            using (HttpClient client = CreateHttpClient())
            {
                content.Headers.ContentMD5 = TestHelper.ComputeMD5Hash(expectedData);

                using (HttpResponseMessage response = await client.PostAsync(remoteServer, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                }
            }
        }

        private sealed class StreamContentWithSyncAsyncCopy : StreamContent
        {
            private readonly Stream _stream;
            private readonly bool _syncCopy;

            public StreamContentWithSyncAsyncCopy(Stream stream, bool syncCopy) : base(stream)
            {
                _stream = stream;
                _syncCopy = syncCopy;
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                if (_syncCopy)
                {
                    try
                    {
                        _stream.CopyTo(stream, 128); // arbitrary size likely to require multiple read/writes
                        return Task.CompletedTask;
                    }
                    catch (Exception exc)
                    {
                        return Task.FromException(exc);
                    }
                }

                return base.SerializeToStreamAsync(stream, context);
            }
        }

        public static IEnumerable<object[]> VerifyUploadServersStreamsAndExpectedData
        {
            get
            {
                foreach (object[] serverArr in VerifyUploadServers) // target server
                    foreach (bool syncCopy in new[] { true, false }) // force the content copy to happen via Read/Write or ReadAsync/WriteAsync
                    {
                        Uri server = (Uri)serverArr[0];

                        byte[] data = new byte[1234];
                        new Random(42).NextBytes(data);

                        // A MemoryStream
                        {
                            var memStream = new MemoryStream(data, writable: false);
                            yield return new object[] { server, new StreamContentWithSyncAsyncCopy(memStream, syncCopy: syncCopy), data };
                        }

                        // A multipart content that provides its own stream from CreateContentReadStreamAsync
                        {
                            var mc = new MultipartContent();
                            mc.Add(new ByteArrayContent(data));
                            var memStream = new MemoryStream();
                            mc.CopyToAsync(memStream).GetAwaiter().GetResult();
                            yield return new object[] { server, mc, memStream.ToArray() };
                        }

                        // A stream that provides the data synchronously and has a known length
                        {
                            var wrappedMemStream = new MemoryStream(data, writable: false);
                            var syncKnownLengthStream = new DelegateStream(
                                canReadFunc: () => wrappedMemStream.CanRead,
                                canSeekFunc: () => wrappedMemStream.CanSeek,
                                lengthFunc: () => wrappedMemStream.Length,
                                positionGetFunc: () => wrappedMemStream.Position,
                                positionSetFunc: p => wrappedMemStream.Position = p,
                                readFunc: (buffer, offset, count) => wrappedMemStream.Read(buffer, offset, count),
                                readAsyncFunc: (buffer, offset, count, token) => wrappedMemStream.ReadAsync(buffer, offset, count, token));
                            yield return new object[] { server, new StreamContentWithSyncAsyncCopy(syncKnownLengthStream, syncCopy: syncCopy), data };
                        }

                        // A stream that provides the data synchronously and has an unknown length
                        {
                            int syncUnknownLengthStreamOffset = 0;

                            Func<byte[], int, int, int> readFunc = (buffer, offset, count) =>
                            {
                                int bytesRemaining = data.Length - syncUnknownLengthStreamOffset;
                                int bytesToCopy = Math.Min(bytesRemaining, count);
                                Array.Copy(data, syncUnknownLengthStreamOffset, buffer, offset, bytesToCopy);
                                syncUnknownLengthStreamOffset += bytesToCopy;
                                return bytesToCopy;
                            };

                            var syncUnknownLengthStream = new DelegateStream(
                                canReadFunc: () => true,
                                canSeekFunc: () => false,
                                readFunc: readFunc,
                                readAsyncFunc: (buffer, offset, count, token) => Task.FromResult(readFunc(buffer, offset, count)));
                            yield return new object[] { server, new StreamContentWithSyncAsyncCopy(syncUnknownLengthStream, syncCopy: syncCopy), data };
                        }

                        // A stream that provides the data asynchronously
                        {
                            int asyncStreamOffset = 0, maxDataPerRead = 100;

                            Func<byte[], int, int, int> readFunc = (buffer, offset, count) =>
                            {
                                int bytesRemaining = data.Length - asyncStreamOffset;
                                int bytesToCopy = Math.Min(bytesRemaining, Math.Min(maxDataPerRead, count));
                                Array.Copy(data, asyncStreamOffset, buffer, offset, bytesToCopy);
                                asyncStreamOffset += bytesToCopy;
                                return bytesToCopy;
                            };

                            var asyncStream = new DelegateStream(
                                canReadFunc: () => true,
                                canSeekFunc: () => false,
                                readFunc: readFunc,
                                readAsyncFunc: async (buffer, offset, count, token) =>
                                {
                                    await Task.Delay(1).ConfigureAwait(false);
                                    return readFunc(buffer, offset, count);
                                });
                            yield return new object[] { server, new StreamContentWithSyncAsyncCopy(asyncStream, syncCopy: syncCopy), data };
                        }

                        // Providing data from a FormUrlEncodedContent's stream
                        {
                            var formContent = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("key", "val") });
                            yield return new object[] { server, formContent, Encoding.GetEncoding("iso-8859-1").GetBytes("key=val") };
                        }
                    }
            }
        }

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(EchoServers))]
        public async Task PostAsync_CallMethod_NullContent(Uri remoteServer)
        {
            using (HttpClient client = CreateHttpClient())
            {
                using (HttpResponseMessage response = await client.PostAsync(remoteServer, null))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        string.Empty);
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(EchoServers))]
        public async Task PostAsync_CallMethod_EmptyContent(Uri remoteServer)
        {
            using (HttpClient client = CreateHttpClient())
            {
                var content = new StringContent(string.Empty);
                using (HttpResponseMessage response = await client.PostAsync(remoteServer, content))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        string.Empty);
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Theory]
        [InlineData(false, "1.0")]
        [InlineData(true, "1.0")]
        [InlineData(null, "1.0")]
        [InlineData(false, "1.1")]
        [InlineData(true, "1.1")]
        [InlineData(null, "1.1")]
        public async Task PostAsync_ExpectContinue_Success(bool? expectContinue, string version)
        {
            using (HttpClient client = CreateHttpClient())
            {
                var req = new HttpRequestMessage(HttpMethod.Post, Configuration.Http.RemoteEchoServer)
                {
                    Content = new StringContent("Test String", Encoding.UTF8)
                };
                req.Headers.ExpectContinue = expectContinue;
                req.Version = new Version(version);

                using (HttpResponseMessage response = await client.SendAsync(req))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    if (UseSocketsHttpHandler)
                    {
                        const string ExpectedReqHeader = "\"Expect\": \"100-continue\"";
                        if (expectContinue == true && version == "1.1")
                        {
                            Assert.Contains(ExpectedReqHeader, await response.Content.ReadAsStringAsync());
                        }
                        else
                        {
                            Assert.DoesNotContain(ExpectedReqHeader, await response.Content.ReadAsStringAsync());
                        }
                    }
                }
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Doesn't send content for get requests")]
        [ActiveIssue(29802, TargetFrameworkMonikers.Uap)]
        [Fact]
        public async Task GetAsync_ExpectContinueTrue_NoContent_StillSendsHeader()
        {
            const string ExpectedContent = "Hello, expecting and continuing world.";
            var clientCompleted = new TaskCompletionSource<bool>();
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (var client = CreateHttpClient())
                {
                    client.DefaultRequestHeaders.ExpectContinue = true;
                    Assert.Equal(ExpectedContent, await client.GetStringAsync(uri));
                    clientCompleted.SetResult(true);
                }
            }, async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    List<string> headers = await connection.ReadRequestHeaderAsync();
                    Assert.Contains("Expect: 100-continue", headers);

                    await connection.Writer.WriteAsync("HTTP/1.1 100 Continue\r\n\r\n");
                    await connection.SendResponseAsync(content: ExpectedContent);
                    await clientCompleted.Task; // make sure server closing the connection isn't what let the client complete
                });
            });
        }

        [ActiveIssue(30061, TargetFrameworkMonikers.Uap)]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PostAsync_ThrowFromContentCopy_RequestFails(bool syncFailure)
        {
            await LoopbackServer.CreateServerAsync(async (server, uri) =>
            {
                Task responseTask = server.AcceptConnectionAsync(async connection =>
                {
                    var buffer = new byte[1000];
                    while (await connection.Socket.ReceiveAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), SocketFlags.None) != 0);
                });

                using (var client = CreateHttpClient())
                {
                    Exception error = new FormatException();
                    var content = new StreamContent(new DelegateStream(
                        canSeekFunc: () => true,
                        lengthFunc: () => 12345678,
                        positionGetFunc: () => 0,
                        canReadFunc: () => true,
                        readFunc: (buffer, offset, count) => throw error,
                        readAsyncFunc: (buffer, offset, count, cancellationToken) => syncFailure ? throw error : Task.Delay(1).ContinueWith<int>(_ => throw error)));

                    if (PlatformDetection.IsUap)
                    {
                        HttpRequestException requestException = await Assert.ThrowsAsync<HttpRequestException>(() => client.PostAsync(uri, content));
                        Assert.Same(error, requestException.InnerException);
                    }
                    else
                    {
                        Assert.Same(error, await Assert.ThrowsAsync<FormatException>(() => client.PostAsync(uri, content)));
                    }
                }
            });
        }

        [OuterLoop("Uses external server")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PostAsync_Redirect_ResultingGetFormattedCorrectly(bool secure)
        {
            const string ContentString = "This is the content string.";
            var content = new StringContent(ContentString);
            Uri redirectUri = Configuration.Http.RedirectUriForDestinationUri(
                secure,
                302,
                secure ? Configuration.Http.SecureRemoteEchoServer : Configuration.Http.RemoteEchoServer,
                1);

            using (HttpClient client = CreateHttpClient())
            using (HttpResponseMessage response = await client.PostAsync(redirectUri, content))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string responseContent = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain(ContentString, responseContent);
                Assert.DoesNotContain("Content-Length", responseContent);
            }
        }

        [ActiveIssue(22191, TargetFrameworkMonikers.Uap)]
        [OuterLoop("Takes several seconds")]
        [Fact]
        public async Task PostAsync_RedirectWith307_LargePayload()
        {
            await PostAsync_Redirect_LargePayload_Helper(307, true);
        }

        [OuterLoop("Takes several seconds")]
        [Fact]
        public async Task PostAsync_RedirectWith302_LargePayload()
        {
            await PostAsync_Redirect_LargePayload_Helper(302, false);
        }

        public async Task PostAsync_Redirect_LargePayload_Helper(int statusCode, bool expectRedirectToPost)
        {
            using (var fs = new FileStream(Path.Combine(Path.GetTempPath(), Path.GetTempFileName()), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 0x1000, FileOptions.DeleteOnClose))
            {
                string contentString = string.Join("", Enumerable.Repeat("Content", 100000));
                byte[] contentBytes = Encoding.UTF32.GetBytes(contentString);
                fs.Write(contentBytes, 0, contentBytes.Length);
                fs.Flush(flushToDisk: true);
                fs.Position = 0;

                Uri redirectUri = Configuration.Http.RedirectUriForDestinationUri(false, statusCode, Configuration.Http.SecureRemoteEchoServer, 1);

                using (HttpClient client = CreateHttpClient())
                using (HttpResponseMessage response = await client.PostAsync(redirectUri, new StreamContent(fs)))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    if (expectRedirectToPost)
                    {
                        Assert.InRange(response.Content.Headers.ContentLength.GetValueOrDefault(), contentBytes.Length, int.MaxValue);
                    }
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(EchoServers))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework disposes request content after send")]
        [ActiveIssue(31104, TestPlatforms.AnyUnix)]
        public async Task PostAsync_ReuseRequestContent_Success(Uri remoteServer)
        {
            const string ContentString = "This is the content string.";
            using (HttpClient client = CreateHttpClient())
            {
                var content = new StringContent(ContentString);
                for (int i = 0; i < 2; i++)
                {
                    using (HttpResponseMessage response = await client.PostAsync(remoteServer, content))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.Contains(ContentString, await response.Content.ReadAsStringAsync());
                    }
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Theory]
        [InlineData(HttpStatusCode.MethodNotAllowed, "Custom description")]
        [InlineData(HttpStatusCode.MethodNotAllowed, "")]
        public async Task GetAsync_CallMethod_ExpectedStatusLine(HttpStatusCode statusCode, string reasonPhrase)
        {
            using (HttpClient client = CreateHttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.StatusCodeUri(
                    false,
                    (int)statusCode,
                    reasonPhrase)))
                {
                    Assert.Equal(statusCode, response.StatusCode);
                    Assert.Equal(reasonPhrase, response.ReasonPhrase);
                }
            }
        }

#endregion

#region Various HTTP Method Tests

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(HttpMethods))]
        public async Task SendAsync_SendRequestUsingMethodToEchoServerWithNoContent_MethodCorrectlySent(
            string method,
            bool secureServer)
        {
            using (HttpClient client = CreateHttpClient())
            {
                var request = new HttpRequestMessage(
                    new HttpMethod(method),
                    secureServer ? Configuration.Http.SecureRemoteEchoServer : Configuration.Http.RemoteEchoServer);

                if (PlatformDetection.IsUap && method == "TRACE")
                {
                    HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
                    Assert.IsType<PlatformNotSupportedException>(ex.InnerException);
                }
                else
                {
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        TestHelper.VerifyRequestMethod(response, method);
                    }
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(HttpMethodsThatAllowContent))]
        public async Task SendAsync_SendRequestUsingMethodToEchoServerWithContent_Success(
            string method,
            bool secureServer)
        {
            if (PlatformDetection.IsFullFramework && method == "GET")
            {
                // .NET Framework doesn't allow a content body with this HTTP verb.
                // It will throw a System.Net.ProtocolViolation exception.
                return;
            }

            using (HttpClient client = CreateHttpClient())
            {
                var request = new HttpRequestMessage(
                    new HttpMethod(method),
                    secureServer ? Configuration.Http.SecureRemoteEchoServer : Configuration.Http.RemoteEchoServer);
                request.Content = new StringContent(ExpectedContent);
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    TestHelper.VerifyRequestMethod(response, method);
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _output.WriteLine(responseContent);

                    Assert.Contains($"\"Content-Length\": \"{request.Content.Headers.ContentLength.Value}\"", responseContent);
                    TestHelper.VerifyResponseBody(
                        responseContent,
                        response.Content.Headers.ContentMD5,
                        false,
                        ExpectedContent);
                }
            }
        }

        [ActiveIssue(30057, TargetFrameworkMonikers.Uap)]
        [OuterLoop("Uses external server")]
        [Theory]
        [InlineData("12345678910", 0)]
        [InlineData("12345678910", 5)]
        public async Task SendAsync_SendSameRequestMultipleTimesDirectlyOnHandler_Success(string stringContent, int startingPosition)
        {
            using (var handler = new HttpMessageInvoker(CreateHttpClientHandler()))
            {
                byte[] byteContent = Encoding.ASCII.GetBytes(stringContent);
                var content = new MemoryStream();
                content.Write(byteContent, 0, byteContent.Length);
                content.Position = startingPosition;
                var request = new HttpRequestMessage(HttpMethod.Post, Configuration.Http.RemoteEchoServer) { Content = new StreamContent(content) };

                for (int iter = 0; iter < 2; iter++)
                {
                    using (HttpResponseMessage response = await handler.SendAsync(request, CancellationToken.None))
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                        string responseContent = await response.Content.ReadAsStringAsync();

                        Assert.Contains($"\"Content-Length\": \"{request.Content.Headers.ContentLength.Value}\"", responseContent);

                        Assert.Contains(stringContent.Substring(startingPosition), responseContent);
                        if (startingPosition != 0)
                        {
                            Assert.DoesNotContain(stringContent.Substring(0, startingPosition), responseContent);
                        }
                    }
                }
            }
        }

        [OuterLoop("Uses external server")]
        [Theory, MemberData(nameof(HttpMethodsThatDontAllowContent))]
        public async Task SendAsync_SendRequestUsingNoBodyMethodToEchoServerWithContent_NoBodySent(
            string method,
            bool secureServer)
        {
            if (PlatformDetection.IsFullFramework && method == "HEAD")
            {
                // .NET Framework doesn't allow a content body with this HTTP verb.
                // It will throw a System.Net.ProtocolViolation exception.
                return;
            }

            if (PlatformDetection.IsUap && method == "TRACE")
            {
                // UAP platform doesn't allow a content body with this HTTP verb.
                // It will throw an exception HttpRequestException/COMException
                // with "The requested operation is invalid" message.
                return;
            }

            using (HttpClient client = CreateHttpClient())
            {
                var request = new HttpRequestMessage(
                    new HttpMethod(method),
                    secureServer ? Configuration.Http.SecureRemoteEchoServer : Configuration.Http.RemoteEchoServer)
                {
                    Content = new StringContent(ExpectedContent)
                };

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    if (method == "TRACE" && (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || UseSocketsHttpHandler))
                    {
                        // .NET Framework also allows the HttpWebRequest and HttpClient APIs to send a request using 'TRACE' 
                        // verb and a request body. The usual response from a server is "400 Bad Request".
                        // See here for more info: https://github.com/dotnet/corefx/issues/9023
                        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                    }
                    else
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        TestHelper.VerifyRequestMethod(response, method);
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Assert.False(responseContent.Contains(ExpectedContent));
                    }
                }
            }
        }
#endregion

#region Version tests
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP does not allow HTTP/1.0 requests.")]
        [OuterLoop("Uses external server")]
        [Fact]
        public async Task SendAsync_RequestVersion10_ServerReceivesVersion10Request()
        {
            Version receivedRequestVersion = await SendRequestAndGetRequestVersionAsync(new Version(1, 0));
            Assert.Equal(new Version(1, 0), receivedRequestVersion);
        }

        [OuterLoop("Uses external server")]
        [Fact]
        public async Task SendAsync_RequestVersion11_ServerReceivesVersion11Request()
        {
            Version receivedRequestVersion = await SendRequestAndGetRequestVersionAsync(new Version(1, 1));
            Assert.Equal(new Version(1, 1), receivedRequestVersion);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Throws exception sending request using Version(0,0)")]
        [OuterLoop("Uses external server")]
        [Fact]
        public async Task SendAsync_RequestVersionNotSpecified_ServerReceivesVersion11Request()
        {
            // SocketsHttpHandler treats 0.0 as a bad version, and throws.
            if (UseSocketsHttpHandler)
            {
                return;
            }

            // The default value for HttpRequestMessage.Version is Version(1,1).
            // So, we need to set something different (0,0), to test the "unknown" version.
            Version receivedRequestVersion = await SendRequestAndGetRequestVersionAsync(new Version(0, 0));
            Assert.Equal(new Version(1, 1), receivedRequestVersion);
        }

        [ActiveIssue(23037, TestPlatforms.AnyUnix)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Specifying Version(2,0) throws exception on netfx")]
        [OuterLoop("Uses external server")]
        [Theory]
        [MemberData(nameof(Http2Servers))]
        public async Task SendAsync_RequestVersion20_ResponseVersion20IfHttp2Supported(Uri server)
        {
            if (PlatformDetection.IsWindows && !PlatformDetection.IsWindows10Version1703OrGreater)
            {
                // Skip this test if running on Windows but on a release prior to Windows 10 Creators Update.
                _output.WriteLine("Skipping test due to Windows 10 version prior to Version 1703.");
                return;
            }
            if (UseSocketsHttpHandler)
            {
                // TODO #23134: SocketsHttpHandler doesn't yet support HTTP/2.
                return;
            }

            // We don't currently have a good way to test whether HTTP/2 is supported without
            // using the same mechanism we're trying to test, so for now we allow both 2.0 and 1.1 responses.
            var request = new HttpRequestMessage(HttpMethod.Get, server);
            request.Version = new Version(2, 0);

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler, false))
            {
                // It is generally expected that the test hosts will be trusted, so we don't register a validation
                // callback in the usual case.
                // 
                // However, on our Debian 8 test machines, a combination of a server side TLS chain,
                // the client chain processor, and the distribution's CA bundle results in an incomplete/untrusted
                // certificate chain. See https://github.com/dotnet/corefx/issues/9244 for more details.
                if (PlatformDetection.IsDebian8)
                {
                    // Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>
                    handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) =>
                    {
                        Assert.InRange(chain.ChainStatus.Length, 0, 1);

                        if (chain.ChainStatus.Length > 0)
                        {
                            Assert.Equal(X509ChainStatusFlags.PartialChain, chain.ChainStatus[0].Status);
                        }

                        return true;
                    };
                }

                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.True(
                        response.Version == new Version(2, 0) ||
                        response.Version == new Version(1, 1),
                        "Response version " + response.Version);
                }
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Specifying Version(2,0) throws exception on netfx")]
        [Fact]
        public async Task SendAsync_RequestVersion20_HttpNotHttps_NoUpgradeRequest()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    (await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, uri) { Version = new Version(2, 0) })).Dispose();
                }
            }, async server =>
            {
                List<string> headers = await server.AcceptConnectionSendResponseAndCloseAsync();
                Assert.All(headers, header => Assert.DoesNotContain("Upgrade", header, StringComparison.OrdinalIgnoreCase));
            });
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Specifying Version(2,0) throws exception on netfx")]
        [OuterLoop("Uses external server")]
        [ConditionalTheory(nameof(IsWindows10Version1607OrGreater)), MemberData(nameof(Http2NoPushServers))]
        public async Task SendAsync_RequestVersion20_ResponseVersion20(Uri server)
        {
            _output.WriteLine(server.AbsoluteUri.ToString());
            var request = new HttpRequestMessage(HttpMethod.Get, server);
            request.Version = new Version(2, 0);

            HttpClientHandler handler = CreateHttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(new Version(2, 0), response.Version);
                }
            }
        }

        private async Task<Version> SendRequestAndGetRequestVersionAsync(Version requestVersion)
        {
            Version receivedRequestVersion = null;

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Version = requestVersion;

                using (HttpClient client = CreateHttpClient())
                {
                    Task<HttpResponseMessage> getResponse = client.SendAsync(request);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponse, serverTask);

                    List<string> receivedRequest = await serverTask;
                    using (HttpResponseMessage response = await getResponse)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    }

                    string statusLine = receivedRequest[0];
                    if (statusLine.Contains("/1.0"))
                    {
                        receivedRequestVersion = new Version(1, 0);
                    }
                    else if (statusLine.Contains("/1.1"))
                    {
                        receivedRequestVersion = new Version(1, 1);
                    }
                    else
                    {
                        Assert.True(false, "Invalid HTTP request version");
                    }

                }
            });

            return receivedRequestVersion;
        }
#endregion

#region Uri wire transmission encoding tests
        [Fact]
        public async Task SendRequest_UriPathHasReservedChars_ServerReceivedExpectedPath()
        {
            await LoopbackServer.CreateServerAsync(async (server, rootUrl) =>
            {
                var uri = new Uri($"http://{rootUrl.Host}:{rootUrl.Port}/test[]");
                _output.WriteLine(uri.AbsoluteUri.ToString());
                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                string statusLine = string.Empty;

                using (HttpClient client = CreateHttpClient())
                {
                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);
                    List<string> receivedRequest = await serverTask;
                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        Assert.True(receivedRequest[0].Contains(uri.PathAndQuery), $"statusLine should contain {uri.PathAndQuery}");
                    }
                }
            });
        }
#endregion
    }
}
