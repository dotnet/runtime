// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    // Note:  Disposing the HttpClient object automatically disposes the handler within. So, it is not necessary
    // to separately Dispose (or have a 'using' statement) for the handler.
    public class HttpClientHandlerTest : HttpClientTestBase
    {
        readonly ITestOutputHelper _output;
        private const string ExpectedContent = "Test contest";
        private const string Username = "testuser";
        private const string Password = "password";

        private readonly NetworkCredential _credential = new NetworkCredential(Username, Password);

        public static bool IsNotWindows7 => !PlatformDetection.IsWindows7;

        public static readonly object[][] EchoServers = Configuration.Http.EchoServers;
        public static readonly object[][] VerifyUploadServers = Configuration.Http.VerifyUploadServers;
        public static readonly object[][] CompressedServers = Configuration.Http.CompressedServers;
        public static readonly object[][] HeaderValueAndUris = {
            new object[] { "X-CustomHeader", "x-value", Configuration.Http.RemoteEchoServer },
            new object[] { "X-CustomHeader", "x-value", Configuration.Http.RedirectUriForDestinationUri(
                secure:false,
                statusCode:302,
                destinationUri:Configuration.Http.RemoteEchoServer,
                hops:1) },
        };
        public static readonly object[][] HeaderWithEmptyValueAndUris = {
            new object[] { "X-Cust-Header-NoValue", "" , Configuration.Http.RemoteEchoServer },
            new object[] { "X-Cust-Header-NoValue", "" , Configuration.Http.RedirectUriForDestinationUri(
                secure:false,
                statusCode:302,
                destinationUri:Configuration.Http.RemoteEchoServer,
                hops:1) },
        };
        public static readonly object[][] Http2Servers = Configuration.Http.Http2Servers;
        public static readonly object[][] Http2NoPushServers = Configuration.Http.Http2NoPushServers;

        public static readonly object[][] RedirectStatusCodes = {
            new object[] { 300 },
            new object[] { 301 },
            new object[] { 302 },
            new object[] { 303 },
            new object[] { 307 }
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
        private static bool NotWindowsUAPOrBeforeVersion1709 => !PlatformDetection.IsUap || PlatformDetection.IsWindows10Version1709OrGreater;

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

        [Fact]
        public void Properties_Get_CountIsZero()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                IDictionary<String, object> dict = handler.Properties;
                Assert.Same(dict, handler.Properties);
                Assert.Equal(0, dict.Count);
            }
        }

        [Fact]
        public void Properties_AddItemToDictionary_ItemPresent()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                IDictionary<String, object> dict = handler.Properties;

                var item = new Object();
                dict.Add("item", item);

                object value;
                Assert.True(dict.TryGetValue("item", out value));
                Assert.Equal(item, value);
            }
        }

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task SendAsync_GetWithInvalidHostHeader_ThrowsException()
        {
            if (PlatformDetection.IsNetCore && !UseManagedHandler)
            {
                // [ActiveIssue(24862)]
                // WinHttpHandler and CurlHandler do not use the Host header to influence the SSL auth.
                // .NET Framework and ManagedHandler do.
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
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task GetAsync_IPv6LinkLocalAddressUri_Success()
        {
            using (HttpClient client = CreateHttpClient())
            {
                var options = new LoopbackServer.Options { Address = LoopbackServer.GetIPv6LinkLocalAddress() };
                await LoopbackServer.CreateServerAsync(async (server, url) =>
                {
                    _output.WriteLine(url.ToString());
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        LoopbackServer.ReadRequestAndSendResponseAsync(server, options: options),
                        client.GetAsync(url));
                }, options);
            }
        }

        [OuterLoop] // TODO: Issue #11345
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
                        LoopbackServer.ReadRequestAndSendResponseAsync(server, options: options),
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task SendAsync_Cancel_CancellationTokenPropagates()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();
            using (HttpClient client = CreateHttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, Configuration.Http.RemoteEchoServer);
                OperationCanceledException ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    client.SendAsync(request, cts.Token));
                Assert.True(cts.Token.IsCancellationRequested, "cts token IsCancellationRequested");
                if (!PlatformDetection.IsFullFramework)
                {
                    // .NET Framework has bug where it doesn't propagate token information.
                    Assert.True(ex.CancellationToken.IsCancellationRequested, "exception token IsCancellationRequested");
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void GetAsync_ServerNeedsAuthAndNoCredential_StatusCodeUnauthorized()
        {
            // UAP HTTP stack caches connections per-process. This causes interference when these tests run in
            // the same process as the other tests. Each test needs to be isolated to its own process.
            // See dicussion: https://github.com/dotnet/corefx/issues/21945
            RemoteInvoke(async useManagedHandlerString =>
            {
                using (var client = CreateHttpClient(useManagedHandlerString))
                {
                    Uri uri = Configuration.Http.BasicAuthUriForCreds(secure: false, userName: Username, password: Password);
                    using (HttpResponseMessage response = await client.GetAsync(uri))
                    {
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }

                    return SuccessExitCode;
                }
            }, UseManagedHandler.ToString()).Dispose();
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData("WWW-Authenticate: CustomAuth\r\n")]
        [InlineData("")] // RFC7235 requires servers to send this header with 401 but some servers don't.
        public async Task GetAsync_ServerNeedsNonStandardAuthAndSetCredential_StatusCodeUnauthorized(string authHeaders)
        {
            string responseHeaders =
                $"HTTP/1.1 401 Unauthorized\r\nDate: {DateTimeOffset.UtcNow:R}\r\n{authHeaders}Content-Length: 0\r\n\r\n";
            _output.WriteLine(responseHeaders);
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.Credentials = new NetworkCredential("unused", "unused");
                using (var client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<List<string>> serverTask = LoopbackServer.ReadRequestAndSendResponseAsync(server, responseHeaders);

                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);
                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    }
                }
            });
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory, MemberData(nameof(RedirectStatusCodes))]
        public async Task GetAsync_AllowAutoRedirectFalse_RedirectFromHttpToHttp_StatusCodeRedirect(int statusCode)
        {
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

        [ActiveIssue(23769)]
        [ActiveIssue(22707, TestPlatforms.AnyUnix)]
        [OuterLoop] // TODO: Issue #11345
        [Theory, MemberData(nameof(RedirectStatusCodesOldMethodsNewMethods))]
        public async Task AllowAutoRedirect_True_ValidateNewMethodUsedOnRedirection(
            int statusCode, string oldMethod, string newMethod)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            using (var client = new HttpClient(handler))
            {
                await LoopbackServer.CreateServerAsync(async (origServer, origUrl) =>
                {
                    var request = new HttpRequestMessage(new HttpMethod(oldMethod), origUrl);

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(request);

                    Task<List<string>> serverTask = LoopbackServer.ReadRequestAndSendResponseAsync(origServer,
                            $"HTTP/1.1 {statusCode} OK\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            $"Location: {origUrl}\r\n" +
                            "\r\n");
                    await Task.WhenAny(getResponseTask, serverTask);
                    Assert.False(getResponseTask.IsCompleted, $"{getResponseTask.Status}: {getResponseTask.Exception}");
                    await serverTask;

                    serverTask = LoopbackServer.ReadRequestAndSendResponseAsync(origServer,
                            $"HTTP/1.1 200 OK\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            "\r\n");
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    List<string> receivedRequest = await serverTask;
                    string[] statusLineParts = receivedRequest[0].Split(' ');

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(200, (int)response.StatusCode);
                        Assert.Equal(newMethod, statusLineParts[0]);
                    }
                });
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory, MemberData(nameof(RedirectStatusCodes))]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpToHttp_StatusCodeOK(int statusCode)
        {
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

        [OuterLoop] // TODO: Issue #11345
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
        [OuterLoop] // TODO: Issue #11345
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
            if (PlatformDetection.IsWindows && PlatformDetection.IsNetCore && !UseManagedHandler)
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
                    Task<List<string>> serverTask = LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            $"HTTP/1.1 302 OK\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            "\r\n");
                    await TestHelper.WhenAllCompletedOrAnyFailed(getTask, serverTask);

                    using (HttpResponseMessage response = await getTask)
                    {
                        Assert.Equal(302, (int)response.StatusCode);
                    }
                });
            }
        }

        [OuterLoop] // TODO: Issue #11345
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
        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(3, 2)]
        [InlineData(3, 3)]
        [InlineData(3, 4)]
        public async Task GetAsync_MaxAutomaticRedirectionsNServerHops_ThrowsIfTooMany(int maxHops, int hops)
        {
            if (PlatformDetection.IsWindows && !PlatformDetection.IsWindows10Version1703OrGreater)
            {
                // Skip this test if running on Windows but on a release prior to Windows 10 Creators Update.
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
                    await Assert.ThrowsAsync<HttpRequestException>(() => t);
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

                        Task redirectTask = LoopbackServer.ReadRequestAndSendResponseAsync(redirectServer);

                        await TestHelper.WhenAllCompletedOrAnyFailed(
                            getResponseTask,
                            LoopbackServer.ReadRequestAndSendResponseAsync(origServer,
                                $"HTTP/1.1 {statusCode} OK\r\n" +
                                $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                                $"Location: {redirectUrl}\r\n" +
                                "\r\n"));

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

        [OuterLoop] // TODO: Issue #11345
        [ConditionalTheory(nameof(IsNotWindows7))] // Skip test on Win7 since WinHTTP has bugs w/ fragments.
        [InlineData("#origFragment", "", "#origFragment", false)]
        [InlineData("#origFragment", "", "#origFragment", true)]
        [InlineData("", "#redirFragment", "#redirFragment", false)]
        [InlineData("", "#redirFragment", "#redirFragment", true)]
        [InlineData("#origFragment", "#redirFragment", "#redirFragment", false)]
        [InlineData("#origFragment", "#redirFragment", "#redirFragment", true)]
        public async Task GetAsync_AllowAutoRedirectTrue_RetainsOriginalFragmentIfAppropriate(
            string origFragment, string redirFragment, string expectedFragment, bool useRelativeRedirect)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (var client = new HttpClient(handler))
            {
                await LoopbackServer.CreateServerAsync(async (origServer, origUrl) =>
                {
                    origUrl = new Uri(origUrl.ToString() + origFragment);
                    Uri redirectUrl = useRelativeRedirect ?
                        new Uri(origUrl.PathAndQuery + redirFragment, UriKind.Relative) :
                        new Uri(origUrl.ToString() + redirFragment);
                    Uri expectedUrl = new Uri(origUrl.ToString() + expectedFragment);

                    Task<HttpResponseMessage> getResponse = client.GetAsync(origUrl);
                    Task firstRequest = LoopbackServer.ReadRequestAndSendResponseAsync(origServer,
                            $"HTTP/1.1 302 OK\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            $"Location: {redirectUrl}\r\n" +
                            "\r\n");
                    Assert.Equal(firstRequest, await Task.WhenAny(firstRequest, getResponse));

                    Task secondRequest = LoopbackServer.ReadRequestAndSendResponseAsync(origServer,
                            $"HTTP/1.1 200 OK\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            "\r\n");
                    await TestHelper.WhenAllCompletedOrAnyFailed(secondRequest, getResponse);

                    using (HttpResponseMessage response = await getResponse)
                    {
                        Assert.Equal(200, (int)response.StatusCode);
                        Assert.Equal(expectedUrl, response.RequestMessage.RequestUri);
                    }
                });
            }
        }

        [Fact]
        [OuterLoop] // Test uses azure endpoint.
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

        [Fact]
        [OuterLoop] // Test uses azure endpoint.
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

        [OuterLoop] // TODO: Issue #11345
        [Theory, MemberData(nameof(RedirectStatusCodes))]
        public async Task GetAsync_CredentialIsCredentialCacheUriRedirect_StatusCodeOK(int statusCode)
        {
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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task GetAsync_DefaultCoookieContainer_NoCookieSent()
        {
            using (HttpClient client = CreateHttpClient())
            {
                using (HttpResponseMessage httpResponse = await client.GetAsync(Configuration.Http.RemoteEchoServer))
                {
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.False(TestHelper.JsonMessageContainsKey(responseText, "Cookie"));
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData("cookieName1", "cookieValue1")]
        public async Task GetAsync_SetCookieContainer_CookieSent(string cookieName, string cookieValue)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(Configuration.Http.RemoteEchoServer, new Cookie(cookieName, cookieValue));
            handler.CookieContainer = cookieContainer;
            using (var client = new HttpClient(handler))
            {
                using (HttpResponseMessage httpResponse = await client.GetAsync(Configuration.Http.RemoteEchoServer))
                {
                    Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.True(TestHelper.JsonMessageContainsKeyValue(responseText, cookieName, cookieValue));
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData("cookieName1", "cookieValue1")]
        public async Task GetAsync_RedirectResponseHasCookie_CookieSentToFinalUri(string cookieName, string cookieValue)
        {
            Uri uri = Configuration.Http.RedirectUriForDestinationUri(
                secure: false,
                statusCode: 302,
                destinationUri: Configuration.Http.RemoteEchoServer,
                hops: 1);
            using (HttpClient client = CreateHttpClient())
            {
                client.DefaultRequestHeaders.Add(
                    "X-SetCookie",
                    string.Format("{0}={1};Path=/", cookieName, cookieValue));
                using (HttpResponseMessage httpResponse = await client.GetAsync(uri))
                {
                    string responseText = await httpResponse.Content.ReadAsStringAsync();
                    _output.WriteLine(responseText);
                    Assert.True(TestHelper.JsonMessageContainsKeyValue(responseText, cookieName, cookieValue));
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [ConditionalTheory(nameof(NotWindowsUAPOrBeforeVersion1709)), MemberData(nameof(HeaderWithEmptyValueAndUris))]
        public async Task GetAsync_RequestHeadersAddCustomHeaders_HeaderAndEmptyValueSent(string name, string value, Uri uri)
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

        [OuterLoop] // TODO: Issue #11345
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

        private static KeyValuePair<string, string> GenerateCookie(string name, char repeat, int overallHeaderValueLength)
        {
            string emptyHeaderValue = $"{name}=; Path=/";

            Debug.Assert(overallHeaderValueLength > emptyHeaderValue.Length);

            int valueCount = overallHeaderValueLength - emptyHeaderValue.Length;
            string value = new string(repeat, valueCount);

            return new KeyValuePair<string, string>(name, value);
        }

        public static object[][] CookieNameValues =
        {
            // WinHttpHandler calls WinHttpQueryHeaders to iterate through multiple Set-Cookie header values,
            // using an initial buffer size of 128 chars. If the buffer is not large enough, WinHttpQueryHeaders
            // returns an insufficient buffer error, allowing WinHttpHandler to try again with a larger buffer.
            // Sometimes when WinHttpQueryHeaders fails due to insufficient buffer, it still advances the
            // iteration index, which would cause header values to be missed if not handled correctly.
            //
            // In particular, WinHttpQueryHeader behaves as follows for the following header value lengths:
            //  * 0-127 chars: succeeds, index advances from 0 to 1.
            //  * 128-255 chars: fails due to insufficient buffer, index advances from 0 to 1.
            //  * 256+ chars: fails due to insufficient buffer, index stays at 0.
            //
            // The below overall header value lengths were chosen to exercise reading header values at these
            // edges, to ensure WinHttpHandler does not miss multiple Set-Cookie headers.

            new object[] { GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 126) },
            new object[] { GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 127) },
            new object[] { GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 128) },
            new object[] { GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 129) },

            new object[] { GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 254) },
            new object[] { GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 255) },
            new object[] { GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 256) },
            new object[] { GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 257) },

            new object[]
            {
                new KeyValuePair<string, string>(
                    ".AspNetCore.Antiforgery.Xam7_OeLcN4",
                    "CfDJ8NGNxAt7CbdClq3UJ8_6w_4661wRQZT1aDtUOIUKshbcV4P0NdS8klCL5qGSN-PNBBV7w23G6MYpQ81t0PMmzIN4O04fqhZ0u1YPv66mixtkX3iTi291DgwT3o5kozfQhe08-RAExEmXpoCbueP_QYM")
            }
        };

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(CookieNameValues))]
        public async Task GetAsync_ResponseWithSetCookieHeaders_AllCookiesRead(KeyValuePair<string, string> cookie1)
        {
            var cookie2 = new KeyValuePair<string, string>(".AspNetCore.Session", "RAExEmXpoCbueP_QYM");
            var cookie3 = new KeyValuePair<string, string>("name", "value");

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClientHandler handler = CreateHttpClientHandler())
                using (var client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    await TestHelper.WhenAllCompletedOrAnyFailed(
                        getResponseTask,
                        LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            $"HTTP/1.1 200 OK\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            $"Set-Cookie: {cookie1.Key}={cookie1.Value}; Path=/\r\n" +
                            $"Set-Cookie   : {cookie2.Key}={cookie2.Value}; Path=/\r\n" + // space before colon to verify header is trimmed and recognized
                            $"Set-Cookie: {cookie3.Key}={cookie3.Value}; Path=/\r\n" +
                            "\r\n"));

                    using (HttpResponseMessage response = await getResponseTask)
                    {
                        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                        CookieCollection cookies = handler.CookieContainer.GetCookies(url);

                        Assert.Equal(3, cookies.Count);
                        Assert.Equal(cookie1.Value, cookies[cookie1.Key].Value);
                        Assert.Equal(cookie2.Value, cookies[cookie2.Key].Value);
                        Assert.Equal(cookie3.Value, cookies[cookie3.Key].Value);
                    }
                }
            });
        }

        [OuterLoop] // TODO: Issue #11345
        [ActiveIssue(17174, TestPlatforms.AnyUnix)] // https://github.com/curl/curl/issues/1354
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetAsync_TrailingHeaders_Ignored(bool includeTrailerHeader)
        {
            if (UseManagedHandler)
            {
                // TODO #23130: The managed handler isn't correctly handling trailing headers.
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
                        LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            "HTTP/1.1 200 OK\r\n" +
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
                    }
                }
            });
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData("")] // missing size
        [InlineData("10000000000000000")] // overflowing size
        public async Task GetAsync_InvalidChunkSize_ThrowsHttpRequestException(string chunkSize)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    string partialResponse = "HTTP/1.1 200 OK\r\n" +
                        "Transfer-Encoding: chunked\r\n" +
                        "\r\n" +
                        $"{chunkSize}\r\n";

                    var tcs = new TaskCompletionSource<bool>();
                    Task serverTask =
                        LoopbackServer.AcceptSocketAsync(server, async (s, stream, reader, writer) =>
                        {
                            var list = await LoopbackServer.ReadWriteAcceptedAsync(s, reader, writer, partialResponse);
                            await tcs.Task;
                            return list;
                        }, null);

                    await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));
                    tcs.SetResult(true);
                    await serverTask;
                }
            });
        }

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task SendAsync_ReadFromSlowStreamingServer_PartialDataReturned()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    Task<HttpResponseMessage> getResponse = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                    await LoopbackServer.AcceptSocketAsync(server, async (s, stream, reader, writer) =>
                    {
                        while (!string.IsNullOrEmpty(reader.ReadLine())) ;
                        await writer.WriteAsync(
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

                        return null;
                    });
                }
            });
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task Dispose_DisposingHandlerCancelsActiveOperationsWithoutResponses()
        {
            if (UseManagedHandler)
            {
                // TODO #23131: The ManagedHandler isn't correctly handling disposal of the handler.
                // It should cause the outstanding requests to be canceled with OperationCanceledExceptions,
                // whereas currently it's resulting in ObjectDisposedExceptions.
                return;
            }

            await LoopbackServer.CreateServerAsync(async (socket1, url1) =>
            {
                await LoopbackServer.CreateServerAsync(async (socket2, url2) =>
                {
                    await LoopbackServer.CreateServerAsync(async (socket3, url3) =>
                    {
                        var unblockServers = new TaskCompletionSource<bool>(TaskContinuationOptions.RunContinuationsAsynchronously);

                        // First server connects but doesn't send any response yet
                        Task server1 = LoopbackServer.AcceptSocketAsync(socket1, async (s, stream, reader, writer) =>
                        {
                            await unblockServers.Task;
                            return null;
                        });

                        // Second server connects and sends some but not all headers
                        Task server2 = LoopbackServer.AcceptSocketAsync(socket2, async (s, stream, reader, writer) =>
                        {
                            while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) ;
                            await writer.WriteAsync($"HTTP/1.1 200 OK\r\n");
                            await unblockServers.Task;
                            return null;
                        });

                        // Third server connects and sends all headers and some but not all of the body
                        Task server3 = LoopbackServer.AcceptSocketAsync(socket3, async (s, stream, reader, writer) =>
                        {
                            while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) ;
                            await writer.WriteAsync($"HTTP/1.1 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 20\r\n\r\n");
                            await writer.WriteAsync("1234567890");
                            await unblockServers.Task;
                            await writer.WriteAsync("1234567890");
                            s.Shutdown(SocketShutdown.Send);
                            return null;
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
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => get1);
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => get2);

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

        [OuterLoop] // TODO: Issue #11345
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
                    await LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            $"HTTP/1.1 {statusCode}\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            "\r\n");

                    await Assert.ThrowsAsync<HttpRequestException>(() => getResponseTask);
                }
            });
        }

        #region Post Methods Tests

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [InlineData(null)]
        public async Task PostAsync_ExpectContinue_Success(bool? expectContinue)
        {
            using (HttpClient client = CreateHttpClient())
            {
                var req = new HttpRequestMessage(HttpMethod.Post, Configuration.Http.RemoteEchoServer)
                {
                    Content = new StringContent("Test String", Encoding.UTF8)
                };
                req.Headers.ExpectContinue = expectContinue;

                using (HttpResponseMessage response = await client.SendAsync(req))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    if (UseManagedHandler)
                    {
                        const string ExpectedReqHeader = "\"Expect\": \"100-continue\"";
                        if (expectContinue == true)
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

        [OuterLoop] // TODO: Issue #11345
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
        [OuterLoop] // takes several seconds
        [Fact]
        public async Task PostAsync_RedirectWith307_LargePayload()
        {
            await PostAsync_Redirect_LargePayload_Helper(307, true);
        }

        [OuterLoop] // takes several seconds
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

        [OuterLoop] // TODO: Issue #11345
        [Theory, MemberData(nameof(EchoServers))] // NOTE: will not work for in-box System.Net.Http.dll due to disposal of request content
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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

        [ActiveIssue(20010, TargetFrameworkMonikers.Uap)] // Test hangs. But this test seems invalid. An HttpRequestMessage can only be sent once.
        [OuterLoop] // TODO: Issue #11345
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

        [OuterLoop] // TODO: Issue #11345
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
                    if (method == "TRACE" && (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || UseManagedHandler))
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
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task SendAsync_RequestVersion10_ServerReceivesVersion10Request()
        {
            Version receivedRequestVersion = await SendRequestAndGetRequestVersionAsync(new Version(1, 0));
            Assert.Equal(new Version(1, 0), receivedRequestVersion);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task SendAsync_RequestVersion11_ServerReceivesVersion11Request()
        {
            Version receivedRequestVersion = await SendRequestAndGetRequestVersionAsync(new Version(1, 1));
            Assert.Equal(new Version(1, 1), receivedRequestVersion);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Throws exception sending request using Version(0,0)")]
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task SendAsync_RequestVersionNotSpecified_ServerReceivesVersion11Request()
        {
            // The default value for HttpRequestMessage.Version is Version(1,1).
            // So, we need to set something different (0,0), to test the "unknown" version.
            Version receivedRequestVersion = await SendRequestAndGetRequestVersionAsync(new Version(0, 0));
            Assert.Equal(new Version(1, 1), receivedRequestVersion);
        }

        [ActiveIssue(23770, TestPlatforms.AnyUnix)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Specifying Version(2,0) throws exception on netfx")]
        [OuterLoop] // TODO: Issue #11345
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
            if (UseManagedHandler)
            {
                // TODO #23134: The managed handler doesn't yet support HTTP/2.
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
        [OuterLoop] // TODO: Issue #11345
        [ConditionalTheory(nameof(IsWindows10Version1607OrGreater)), MemberData(nameof(Http2NoPushServers))]
        public async Task SendAsync_RequestVersion20_ResponseVersion20(Uri server)
        {
            if (UseManagedHandler)
            {
                // TODO #23134: The managed handler doesn't yet support HTTP/2.
                return;
            }

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
                    Task<List<string>> serverTask = LoopbackServer.ReadRequestAndSendResponseAsync(server);
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

        #region Proxy tests
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP does not support custom proxies.")]
        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(CredentialsForProxy))]
        public async Task Proxy_BypassFalse_GetRequestGoesThroughCustomProxy(ICredentials creds, bool wrapCredsInCache)
        {
            if (UseManagedHandler)
            {
                // TODO #23135: ManagedHandler currently gets error "System.NotImplementedException : Basic auth: can't handle ':' in domain "dom:\ain""
                return;
            }

            int port;
            Task<LoopbackGetRequestHttpProxy.ProxyResult> proxyTask = LoopbackGetRequestHttpProxy.StartAsync(
                out port,
                requireAuth: creds != null && creds != CredentialCache.DefaultCredentials,
                expectCreds: true);
            Uri proxyUrl = new Uri($"http://localhost:{port}");

            const string BasicAuth = "Basic";
            if (wrapCredsInCache)
            {
                Assert.IsAssignableFrom<NetworkCredential>(creds);
                var cache = new CredentialCache();
                cache.Add(proxyUrl, BasicAuth, (NetworkCredential)creds);
                creds = cache;
            }

            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.Proxy = new UseSpecifiedUriWebProxy(proxyUrl, creds);

                Task<HttpResponseMessage> responseTask = client.GetAsync(Configuration.Http.RemoteEchoServer);
                Task<string> responseStringTask = responseTask.ContinueWith(t => t.Result.Content.ReadAsStringAsync(), TaskScheduler.Default).Unwrap();
                await TestHelper.WhenAllCompletedOrAnyFailed(proxyTask, responseTask, responseStringTask);

                using (responseTask.Result)
                {
                    TestHelper.VerifyResponseBody(responseStringTask.Result, responseTask.Result.Content.Headers.ContentMD5, false, null);
                    Assert.Equal(Encoding.ASCII.GetString(proxyTask.Result.ResponseContent), responseStringTask.Result);

                    NetworkCredential nc = creds?.GetCredential(proxyUrl, BasicAuth);
                    string expectedAuth =
                        nc == null || nc == CredentialCache.DefaultCredentials ? null :
                        string.IsNullOrEmpty(nc.Domain) ? $"{nc.UserName}:{nc.Password}" :
                        $"{nc.Domain}\\{nc.UserName}:{nc.Password}";
                    Assert.Equal(expectedAuth, proxyTask.Result.AuthenticationHeaderValue);
                }
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP does not support custom proxies.")]
        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [MemberData(nameof(BypassedProxies))]
        public async Task Proxy_BypassTrue_GetRequestDoesntGoesThroughCustomProxy(IWebProxy proxy)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Proxy = proxy;
            using (var client = new HttpClient(handler))
            using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer))
            {
                TestHelper.VerifyResponseBody(
                    await response.Content.ReadAsStringAsync(),
                    response.Content.Headers.ContentMD5,
                    false,
                    null);
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "UAP does not support custom proxies.")]
        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task Proxy_HaveNoCredsAndUseAuthenticatedCustomProxy_ProxyAuthenticationRequiredStatusCode()
        {
            int port;
            Task<LoopbackGetRequestHttpProxy.ProxyResult> proxyTask = LoopbackGetRequestHttpProxy.StartAsync(
                out port,
                requireAuth: true,
                expectCreds: false);
            Uri proxyUrl = new Uri($"http://localhost:{port}");

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Proxy = new UseSpecifiedUriWebProxy(proxyUrl, null);
            using (var client = new HttpClient(handler))
            {
                Task<HttpResponseMessage> responseTask = client.GetAsync(Configuration.Http.RemoteEchoServer);
                await (new Task[] { proxyTask, responseTask }).WhenAllOrAnyFailed();
                using (responseTask.Result)
                {
                    Assert.Equal(HttpStatusCode.ProxyAuthenticationRequired, responseTask.Result.StatusCode);
                }
            }
        }

        private static IEnumerable<object[]> BypassedProxies()
        {
            yield return new object[] { null };
            yield return new object[] { new UseSpecifiedUriWebProxy(new Uri($"http://{Guid.NewGuid().ToString().Substring(0, 15)}:12345"), bypass: true) };
        }

        private static IEnumerable<object[]> CredentialsForProxy()
        {
            yield return new object[] { null, false };
            foreach (bool wrapCredsInCache in new[] { true, false })
            {
                yield return new object[] { new NetworkCredential("user:name", "password"), wrapCredsInCache };
                yield return new object[] { new NetworkCredential("username", "password", "dom:\\ain"), wrapCredsInCache };
            }
        }
        #endregion

        #region Uri wire transmission encoding tests
        [OuterLoop] // TODO: Issue #11345
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
                    Task<List<string>> serverTask = LoopbackServer.ReadRequestAndSendResponseAsync(server);

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
