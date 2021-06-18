// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

#if WINHTTPHANDLER_TEST
    using HttpClientHandler = System.Net.Http.WinHttpClientHandler;
#endif

    public abstract class HttpClientHandlerTest_AutoRedirect : HttpClientHandlerTestBase
    {
        private const string ExpectedContent = "Test content";
        private const string Username = "testuser";
        private const string Password = "password";

        private readonly NetworkCredential _credential = new NetworkCredential(Username, Password);

        public static IEnumerable<object[]> RemoteServersAndRedirectStatusCodes()
        {
            foreach (Configuration.Http.RemoteServer remoteServer in Configuration.Http.RemoteServers)
            {
                yield return new object[] { remoteServer, 300 };
                yield return new object[] { remoteServer, 301 };
                yield return new object[] { remoteServer, 302 };
                yield return new object[] { remoteServer, 303 };
                yield return new object[] { remoteServer, 307 };
                yield return new object[] { remoteServer, 308 };
            }
        }

        public static IEnumerable<object[]> RedirectStatusCodesOldMethodsNewMethods()
        {
            foreach (int statusCode in new[] { 300, 301, 302, 303, 307, 308 })
            {
                yield return new object[] { statusCode, "GET", "GET" };
                yield return new object[] { statusCode, "HEAD", "HEAD" };

                yield return new object[] { statusCode, "POST", statusCode <= 303 ? "GET" : "POST" };

                yield return new object[] { statusCode, "DELETE", statusCode == 303 ? "GET" : "DELETE" };
                yield return new object[] { statusCode, "OPTIONS", statusCode == 303 ? "GET" : "OPTIONS" };
                yield return new object[] { statusCode, "PATCH", statusCode == 303 ? "GET" : "PATCH" };
                yield return new object[] { statusCode, "PUT", statusCode == 303 ? "GET" : "PUT" };
                yield return new object[] { statusCode, "MYCUSTOMMETHOD", statusCode == 303 ? "GET" : "MYCUSTOMMETHOD" };
            }
        }
        public HttpClientHandlerTest_AutoRedirect(ITestOutputHelper output) : base(output) { }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersAndRedirectStatusCodes))]
        public async Task GetAsync_AllowAutoRedirectFalse_RedirectFromHttpToHttp_StatusCodeRedirect(Configuration.Http.RemoteServer remoteServer, int statusCode)
        {
            if (statusCode == 308 && (IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = false;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri uri = remoteServer.RedirectUriForDestinationUri(
                    statusCode: statusCode,
                    destinationUri: remoteServer.EchoUri,
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
            if (statusCode == 308 && (IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            using (HttpClient client = CreateHttpClient(handler))
            {
                await LoopbackServer.CreateServerAsync(async (origServer, origUrl) =>
                {
                    var request = new HttpRequestMessage(new HttpMethod(oldMethod), origUrl) { Version = UseVersion };

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(TestAsync, request);

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

        [Theory]
        [InlineData(300)]
        [InlineData(301)]
        [InlineData(302)]
        [InlineData(303)]
        public async Task AllowAutoRedirect_True_PostToGetDoesNotSendTE(int statusCode)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            using (HttpClient client = CreateHttpClient(handler))
            {
                await LoopbackServer.CreateServerAsync(async (origServer, origUrl) =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, origUrl) { Version = UseVersion };
                    request.Content = new StringContent(ExpectedContent);
                    request.Headers.TransferEncodingChunked = true;

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(TestAsync, request);

                    await LoopbackServer.CreateServerAsync(async (redirServer, redirUrl) =>
                    {
                        // Original URL will redirect to a different URL
                        Task serverTask = origServer.AcceptConnectionAsync(async connection =>
                        {
                            // Send Connection: close so the client will close connection after request is sent,
                            // meaning we can just read to the end to get the content
                            await connection.ReadRequestHeaderAndSendResponseAsync((HttpStatusCode)statusCode, $"Location: {redirUrl}\r\nConnection: close\r\n");
                            connection.Socket.Shutdown(SocketShutdown.Send);
                            await connection.ReadToEndAsync();
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
                            receivedContent = await connection.ReadToEndAsync();
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

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersAndRedirectStatusCodes))]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpToHttp_StatusCodeOK(Configuration.Http.RemoteServer remoteServer, int statusCode)
        {
            if (statusCode == 308 && (IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri uri = remoteServer.RedirectUriForDestinationUri(
                    statusCode: statusCode,
                    destinationUri: remoteServer.EchoUri,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(remoteServer.EchoUri, response.RequestMessage.RequestUri);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpToHttps_StatusCodeOK()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (HttpClient client = CreateHttpClient(handler))
            {
                Uri uri = Configuration.Http.RemoteHttp11Server.RedirectUriForDestinationUri(
                    statusCode: 302,
                    destinationUri: Configuration.Http.RemoteSecureHttp11Server.EchoUri,
                    hops: 1);
                _output.WriteLine("Uri: {0}", uri);
                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(Configuration.Http.SecureRemoteEchoServer, response.RequestMessage.RequestUri);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Fact]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectFromHttpsToHttp_StatusCodeRedirect()
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (HttpClient client = CreateHttpClient(handler))
            {
                Uri uri = Configuration.Http.RemoteSecureHttp11Server.RedirectUriForDestinationUri(
                    statusCode: 302,
                    destinationUri: Configuration.Http.RemoteHttp11Server.EchoUri,
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
            if (IsWinHttpHandler) // see https://github.com/dotnet/runtime/issues/23930#issuecomment-338776074 for details
            {
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (HttpClient client = CreateHttpClient(handler))
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

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectToUriWithParams_RequestMsgUriSet(Configuration.Http.RemoteServer remoteServer)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            Uri targetUri = remoteServer.BasicAuthUriForCreds(userName: Username, password: Password);
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri uri = remoteServer.RedirectUriForDestinationUri(
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

        [OuterLoop("Uses external servers")]
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

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.MaxAutomaticRedirections = maxHops;
            using (HttpClient client = CreateHttpClient(handler))
            {
                Task<HttpResponseMessage> t = client.GetAsync(Configuration.Http.RemoteHttp11Server.RedirectUriForDestinationUri(
                    statusCode: 302,
                    destinationUri: Configuration.Http.RemoteHttp11Server.EchoUri,
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
                    if (!IsWinHttpHandler)
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

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task GetAsync_AllowAutoRedirectTrue_RedirectWithRelativeLocation(Configuration.Http.RemoteServer remoteServer)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.AllowAutoRedirect = true;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri uri = remoteServer.RedirectUriForDestinationUri(
                    statusCode: 302,
                    destinationUri: remoteServer.EchoUri,
                    hops: 1,
                    relative: true);
                _output.WriteLine("Uri: {0}", uri);

                using (HttpResponseMessage response = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(remoteServer.EchoUri, response.RequestMessage.RequestUri);
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
            using (HttpClient client = CreateHttpClient(handler))
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
            using (HttpClient client = CreateHttpClient(handler))
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

        [Theory, MemberData(nameof(RemoteServersMemberData))]
        [OuterLoop("Uses external servers")]
        public async Task GetAsync_CredentialIsNetworkCredentialUriRedirect_StatusCodeUnauthorized(Configuration.Http.RemoteServer remoteServer)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Credentials = _credential;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri redirectUri = remoteServer.RedirectUriForCreds(
                    statusCode: 302,
                    userName: Username,
                    password: Password);
                using (HttpResponseMessage unAuthResponse = await client.GetAsync(redirectUri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, unAuthResponse.StatusCode);
                }
            }
        }

        [Theory, MemberData(nameof(RemoteServersMemberData))]
        [OuterLoop("Uses external servers")]
        public async Task HttpClientHandler_CredentialIsNotCredentialCacheAfterRedirect_StatusCodeOK(Configuration.Http.RemoteServer remoteServer)
        {
            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Credentials = _credential;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                Uri redirectUri = remoteServer.RedirectUriForCreds(
                    statusCode: 302,
                    userName: Username,
                    password: Password);
                using (HttpResponseMessage unAuthResponse = await client.GetAsync(redirectUri))
                {
                    Assert.Equal(HttpStatusCode.Unauthorized, unAuthResponse.StatusCode);
                }

                // Use the same handler to perform get request, authentication should succeed after redirect.
                Uri uri = remoteServer.BasicAuthUriForCreds(userName: Username, password: Password);
                using (HttpResponseMessage authResponse = await client.GetAsync(uri))
                {
                    Assert.Equal(HttpStatusCode.OK, authResponse.StatusCode);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersAndRedirectStatusCodes))]
        public async Task GetAsync_CredentialIsCredentialCacheUriRedirect_StatusCodeOK(Configuration.Http.RemoteServer remoteServer, int statusCode)
        {
            if (statusCode == 308 && (IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            Uri uri = remoteServer.BasicAuthUriForCreds(userName: Username, password: Password);
            Uri redirectUri = remoteServer.RedirectUriForCreds(
                statusCode: statusCode,
                userName: Username,
                password: Password);
            _output.WriteLine(uri.AbsoluteUri);
            _output.WriteLine(redirectUri.AbsoluteUri);
            var credentialCache = new CredentialCache();
            credentialCache.Add(uri, "Basic", _credential);

            HttpClientHandler handler = CreateHttpClientHandler();
            handler.Credentials = credentialCache;
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                using (HttpResponseMessage response = await client.GetAsync(redirectUri))
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal(uri, response.RequestMessage.RequestUri);
                }
            }
        }

        [OuterLoop("Uses external servers")]
        [Theory, MemberData(nameof(RemoteServersAndRedirectStatusCodes))]
        public async Task DefaultHeaders_SetCredentials_ClearedOnRedirect(Configuration.Http.RemoteServer remoteServer, int statusCode)
        {
            if (statusCode == 308 && (IsWinHttpHandler && PlatformDetection.WindowsVersion < 10))
            {
                // 308 redirects are not supported on old versions of WinHttp, or on .NET Framework.
                return;
            }

            HttpClientHandler handler = CreateHttpClientHandler();
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer, handler))
            {
                string credentialString = _credential.UserName + ":" + _credential.Password;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentialString);
                Uri uri = remoteServer.RedirectUriForDestinationUri(
                    statusCode: statusCode,
                    destinationUri: remoteServer.EchoUri,
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
    }
}
