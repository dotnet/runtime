// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpCookieProtocolTests : HttpClientTestBase
    {
        private const string s_cookieName = "ABC";
        private const string s_cookieValue = "123";
        private const string s_expectedCookieHeaderValue = "ABC=123";

        private const string s_customCookieHeaderValue = "CustomCookie=456";

        private const string s_simpleContent = "Hello world!";

        //
        // Send cookie tests
        //

        private static CookieContainer CreateSingleCookieContainer(Uri uri) => CreateSingleCookieContainer(uri, s_cookieName, s_cookieValue);

        private static CookieContainer CreateSingleCookieContainer(Uri uri, string cookieName, string cookieValue)
        {
            var container = new CookieContainer();
            container.Add(uri, new Cookie(cookieName, cookieValue));
            return container;
        }

        private static string GetCookieHeaderValue(string cookieName, string cookieValue) => $"{cookieName}={cookieValue}";


        [Fact]
        public async Task GetAsync_DefaultCoookieContainer_NoCookieSent()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (HttpClient client = CreateHttpClient())
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    List<string> requestLines = await serverTask;

                    Assert.Equal(0, requestLines.Count(s => s.StartsWith("Cookie:")));
                }
            });
        }

        [Theory]
        [MemberData(nameof(CookieNamesValuesAndUseCookies))]
        public async Task GetAsync_SetCookieContainer_CookieSent(string cookieName, string cookieValue, bool useCookies)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.CookieContainer = CreateSingleCookieContainer(url, cookieName, cookieValue);
                handler.UseCookies = useCookies;

                using (HttpClient client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    List<string> requestLines = await serverTask;

                    if (useCookies)
                    {
                        Assert.Contains($"Cookie: {GetCookieHeaderValue(cookieName, cookieValue)}", requestLines);
                        Assert.Equal(1, requestLines.Count(s => s.StartsWith("Cookie:")));
                    }
                    else
                    {
                        Assert.Equal(0, requestLines.Count(s => s.StartsWith("Cookie:")));
                    }
                }
            });
        }

        [Fact]
        public async Task GetAsync_SetCookieContainerMultipleCookies_CookiesSent()
        {
            var cookies = new Cookie[]
            {
                new Cookie("hello", "world"),
                new Cookie("foo", "bar"),
                new Cookie("ABC", "123")
            };

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();

                var cookieContainer = new CookieContainer();
                foreach (Cookie c in cookies)
                {
                    cookieContainer.Add(url, c);
                }

                handler.CookieContainer = cookieContainer;

                using (HttpClient client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    List<string> requestLines = await serverTask;

                    string expectedHeader = "Cookie: " + string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}").ToArray());
                    Assert.Contains(expectedHeader, requestLines);
                    Assert.Equal(1, requestLines.Count(s => s.StartsWith("Cookie:")));
                }
            });
        }

        [Fact]
        public async Task GetAsync_AddCookieHeader_CookieHeaderSent()
        {
            if (IsNetfxHandler)
            {
                // Netfx handler does not support custom cookie header
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                using (HttpClient client = new HttpClient(handler))
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                    requestMessage.Headers.Add("Cookie", s_customCookieHeaderValue);

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(requestMessage);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();

                    List<string> requestLines = await serverTask;

                    Assert.Contains($"Cookie: {s_customCookieHeaderValue}", requestLines);
                    Assert.Equal(1, requestLines.Count(s => s.StartsWith("Cookie:")));
                }
            });
        }

        [Fact]
        public async Task GetAsync_AddMultipleCookieHeaders_CookiesSent()
        {
            if (IsNetfxHandler)
            {
                // Netfx handler does not support custom cookie header
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                using (HttpClient client = new HttpClient(handler))
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                    requestMessage.Headers.Add("Cookie", "A=1");
                    requestMessage.Headers.Add("Cookie", "B=2");
                    requestMessage.Headers.Add("Cookie", "C=3");

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(requestMessage);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();

                    List<string> requestLines = await serverTask;

                    Assert.Equal(1, requestLines.Count(s => s.StartsWith("Cookie: ")));

                    // Multiple Cookie header values are treated as any other header values and are 
                    // concatenated using ", " as the separator.

                    var cookieValues = requestLines.Single(s => s.StartsWith("Cookie: ")).Substring(8).Split(new string[] { ", " }, StringSplitOptions.None);
                    Assert.Contains("A=1", cookieValues);
                    Assert.Contains("B=2", cookieValues);
                    Assert.Contains("C=3", cookieValues);
                    Assert.Equal(3, cookieValues.Count());
                }
            });
        }

        [Fact]
        public async Task GetAsync_SetCookieContainerAndCookieHeader_BothCookiesSent()
        {
            if (IsNetfxHandler)
            {
                // Netfx handler does not support custom cookie header
                return;
            }

            if (IsCurlHandler)
            {
                // Issue #26983
                // CurlHandler ignores container cookies when custom Cookie header is set.
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.CookieContainer = CreateSingleCookieContainer(url);

                using (HttpClient client = new HttpClient(handler))
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                    requestMessage.Headers.Add("Cookie", s_customCookieHeaderValue);

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(requestMessage);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    List<string> requestLines = await serverTask;

                    Assert.Equal(1, requestLines.Count(s => s.StartsWith("Cookie: ")));

                    var cookies = requestLines.Single(s => s.StartsWith("Cookie: ")).Substring(8).Split(new string[] { "; " }, StringSplitOptions.None);
                    Assert.Contains(s_expectedCookieHeaderValue, cookies);
                    Assert.Contains(s_customCookieHeaderValue, cookies);
                    Assert.Equal(2, cookies.Count());
                }
            });
        }

        [Fact]
        public async Task GetAsync_SetCookieContainerAndMultipleCookieHeaders_BothCookiesSent()
        {
            if (IsNetfxHandler)
            {
                // Netfx handler does not support custom cookie header
                return;
            }

            if (IsCurlHandler)
            {
                // Issue #26983
                // CurlHandler ignores container cookies when custom Cookie header is set.
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.CookieContainer = CreateSingleCookieContainer(url);

                using (HttpClient client = new HttpClient(handler))
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                    requestMessage.Headers.Add("Cookie", "A=1");
                    requestMessage.Headers.Add("Cookie", "B=2");

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(requestMessage);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync();
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    List<string> requestLines = await serverTask;

                    Assert.Equal(1, requestLines.Count(s => s.StartsWith("Cookie: ")));

                    // Multiple Cookie header values are treated as any other header values and are 
                    // concatenated using ", " as the separator.  The container cookie is concatenated to
                    // one of these values using the "; " cookie separator.

                    var cookieValues = requestLines.Single(s => s.StartsWith("Cookie: ")).Substring(8).Split(new string[] { ", " }, StringSplitOptions.None);
                    Assert.Equal(2, cookieValues.Count());

                    // Find container cookie and remove it so we can validate the rest of the cookie header values
                    bool sawContainerCookie = false;
                    for (int i = 0; i < cookieValues.Length; i++)
                    {
                        if (cookieValues[i].Contains(';'))
                        {
                            Assert.False(sawContainerCookie);

                            var cookies = cookieValues[i].Split(new string[] { "; " }, StringSplitOptions.None);
                            Assert.Equal(2, cookies.Count());
                            Assert.Contains(s_expectedCookieHeaderValue, cookies);

                            sawContainerCookie = true;
                            cookieValues[i] = cookies.Where(c => c != s_expectedCookieHeaderValue).Single();
                        }
                    }

                    Assert.Contains("A=1", cookieValues);
                    Assert.Contains("B=2", cookieValues);
                }
            });
        }

        [Fact]
        public async Task GetAsyncWithRedirect_SetCookieContainer_CorrectCookiesSent()
        {
            const string path1 = "/foo";
            const string path2 = "/bar";

            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                Uri url1 = new Uri(url, path1);
                Uri url2 = new Uri(url, path2);
                Uri unusedUrl = new Uri(url, "/unused");

                HttpClientHandler handler = CreateHttpClientHandler();
                handler.CookieContainer = new CookieContainer();
                handler.CookieContainer.Add(url1, new Cookie("cookie1", "value1"));
                handler.CookieContainer.Add(url2, new Cookie("cookie2", "value2"));
                handler.CookieContainer.Add(unusedUrl, new Cookie("cookie3", "value3"));

                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.ConnectionClose = true; // to avoid issues with connection pooling
                    await client.GetAsync(url1);
                }
            },
            async server =>
            {
                List<string> request1Lines = await server.AcceptConnectionSendResponseAndCloseAsync(HttpStatusCode.Found, $"Location: {path2}\r\n");

                Assert.Contains($"Cookie: cookie1=value1", request1Lines);
                Assert.Equal(1, request1Lines.Count(s => s.StartsWith("Cookie:")));

                List<string> request2Lines = await server.AcceptConnectionSendResponseAndCloseAsync(content: s_simpleContent);

                Assert.Contains($"Cookie: cookie2=value2", request2Lines);
                Assert.Equal(1, request2Lines.Count(s => s.StartsWith("Cookie:")));
            });
        }

        //
        // Receive cookie tests
        //

        [Theory]
        [MemberData(nameof(CookieNamesValuesAndUseCookies))]
        public async Task GetAsync_ReceiveSetCookieHeader_CookieAdded(string cookieName, string cookieValue, bool useCookies)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.UseCookies = useCookies;

                using (HttpClient client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync(
                        HttpStatusCode.OK, $"Set-Cookie: {GetCookieHeaderValue(cookieName, cookieValue)}\r\n", s_simpleContent);
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    CookieCollection collection = handler.CookieContainer.GetCookies(url);

                    if (useCookies)
                    {
                        Assert.Equal(1, collection.Count);
                        Assert.Equal(cookieName, collection[0].Name);
                        Assert.Equal(cookieValue, collection[0].Value);
                    }
                    else
                    {
                        Assert.Equal(0, collection.Count);
                    }
                }
            });
        }

        [Fact]
        public async Task GetAsync_ReceiveMultipleSetCookieHeaders_CookieAdded()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();

                using (HttpClient client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync(
                        HttpStatusCode.OK,
                        $"Set-Cookie: A=1; Path=/\r\n" +
                        $"Set-Cookie   : B=2; Path=/\r\n" + // space before colon to verify header is trimmed and recognized
                        $"Set-Cookie:    C=3; Path=/\r\n",
                        s_simpleContent);
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    CookieCollection collection = handler.CookieContainer.GetCookies(url);
                    Assert.Equal(3, collection.Count);

                    // Convert to array so we can more easily process contents, since CookieCollection does not implement IEnumerable<Cookie>
                    Cookie[] cookies = new Cookie[3];
                    collection.CopyTo(cookies, 0);

                    Assert.Contains(cookies, c => c.Name == "A" && c.Value == "1");
                    Assert.Contains(cookies, c => c.Name == "B" && c.Value == "2");
                    Assert.Contains(cookies, c => c.Name == "C" && c.Value == "3");
                }
            });
        }

        [Fact]
        public async Task GetAsync_ReceiveSetCookieHeader_CookieUpdated()
        {
            const string newCookieValue = "789";

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.CookieContainer = CreateSingleCookieContainer(url);

                using (HttpClient client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync(
                        HttpStatusCode.OK, $"Set-Cookie: {s_cookieName}={newCookieValue}\r\n", s_simpleContent);
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    CookieCollection collection = handler.CookieContainer.GetCookies(url);
                    Assert.Equal(1, collection.Count);
                    Assert.Equal(s_cookieName, collection[0].Name);
                    Assert.Equal(newCookieValue, collection[0].Value);
                }
            });
        }

        [Fact]
        public async Task GetAsync_ReceiveSetCookieHeader_CookieRemoved()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.CookieContainer = CreateSingleCookieContainer(url);

                using (HttpClient client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync(
                        HttpStatusCode.OK, $"Set-Cookie: {s_cookieName}=; Expires=Sun, 06 Nov 1994 08:49:37 GMT\r\n", s_simpleContent);
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    CookieCollection collection = handler.CookieContainer.GetCookies(url);
                    Assert.Equal(0, collection.Count);
                }
            });
        }

        [Fact]
        public async Task GetAsync_ReceiveInvalidSetCookieHeader_ValidCookiesAdded()
        {
            if (IsNetfxHandler)
            {
                // NetfxHandler incorrectly only processes one valid cookie 
                return;
            }

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();

                using (HttpClient client = new HttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync(
                        HttpStatusCode.OK,
                        $"Set-Cookie: A=1; Path=/;Expires=asdfsadgads\r\n" +    // invalid Expires
                        $"Set-Cookie: B=2; Path=/\r\n" + 
                        $"Set-Cookie: C=3; Path=/\r\n",
                        s_simpleContent);
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    CookieCollection collection = handler.CookieContainer.GetCookies(url);
                    Assert.Equal(2, collection.Count);

                    // Convert to array so we can more easily process contents, since CookieCollection does not implement IEnumerable<Cookie>
                    Cookie[] cookies = new Cookie[3];
                    collection.CopyTo(cookies, 0);

                    Assert.Contains(cookies, c => c.Name == "B" && c.Value == "2");
                    Assert.Contains(cookies, c => c.Name == "C" && c.Value == "3");
                }
            });
        }

        [Fact]
        public async Task GetAsyncWithRedirect_ReceiveSetCookie_CookieSent()
        {
            const string path1 = "/foo";
            const string path2 = "/bar";

            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                Uri url1 = new Uri(url, path1);

                HttpClientHandler handler = CreateHttpClientHandler();

                using (HttpClient client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.ConnectionClose = true; // to avoid issues with connection pooling
                    await client.GetAsync(url1);

                    CookieCollection collection = handler.CookieContainer.GetCookies(url);

                    Assert.Equal(2, collection.Count);

                    // Convert to array so we can more easily process contents, since CookieCollection does not implement IEnumerable<Cookie>
                    Cookie[] cookies = new Cookie[2];
                    collection.CopyTo(cookies, 0);

                    Assert.Contains(cookies, c => c.Name == "A" && c.Value == "1");
                    Assert.Contains(cookies, c => c.Name == "B" && c.Value == "2");
                }
            },
            async server =>
            {
                List<string> request1Lines = await server.AcceptConnectionSendResponseAndCloseAsync(
                    HttpStatusCode.Found, $"Location: {path2}\r\nSet-Cookie: A=1; Path=/\r\n");

                Assert.Equal(0, request1Lines.Count(s => s.StartsWith("Cookie:")));

                List<string> request2Lines = await server.AcceptConnectionSendResponseAndCloseAsync(
                    HttpStatusCode.OK, $"Set-Cookie: B=2; Path=/\r\n", s_simpleContent);

                Assert.Contains($"Cookie: A=1", request2Lines);
                Assert.Equal(1, request2Lines.Count(s => s.StartsWith("Cookie:")));
            });
        }

        [Fact]
        public async Task GetAsyncWithBasicAuth_ReceiveSetCookie_CookieSent()
        {
            if (IsWinHttpHandler)
            {
                // Issue #26986
                // WinHttpHandler does not process the cookie.
                return;
            }

            await LoopbackServer.CreateClientAndServerAsync(async url =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.Credentials = new NetworkCredential("user", "pass");

                using (HttpClient client = new HttpClient(handler))
                {
                    await client.GetAsync(url);

                    CookieCollection collection = handler.CookieContainer.GetCookies(url);

                    Assert.Equal(2, collection.Count);

                    // Convert to array so we can more easily process contents, since CookieCollection does not implement IEnumerable<Cookie>
                    Cookie[] cookies = new Cookie[2];
                    collection.CopyTo(cookies, 0);

                    Assert.Contains(cookies, c => c.Name == "A" && c.Value == "1");
                    Assert.Contains(cookies, c => c.Name == "B" && c.Value == "2");
                }
            },
            async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    List<string> request1Lines = await connection.ReadRequestHeaderAndSendResponseAsync(
                        HttpStatusCode.Unauthorized,
                        $"WWW-Authenticate: Basic realm=\"WallyWorld\"\r\nSet-Cookie: A=1; Path=/\r\n");

                    Assert.Equal(0, request1Lines.Count(s => s.StartsWith("Cookie:")));

                    List<string> request2Lines = await connection.ReadRequestHeaderAndSendResponseAsync(
                        HttpStatusCode.OK,
                        $"Set-Cookie: B=2; Path=/\r\n",
                        s_simpleContent);

                    Assert.Contains($"Cookie: A=1", request2Lines);
                    Assert.Equal(1, request2Lines.Count(s => s.StartsWith("Cookie:")));
                });
            });
        }

        // 
        // MemberData stuff
        //

        private static string GenerateCookie(string name, char repeat, int overallHeaderValueLength)
        {
            string emptyHeaderValue = $"{name}=; Path=/";

            Debug.Assert(overallHeaderValueLength > emptyHeaderValue.Length);

            int valueCount = overallHeaderValueLength - emptyHeaderValue.Length;
            return new string(repeat, valueCount);
        }

        public static IEnumerable<object[]> CookieNamesValuesAndUseCookies()
        {
            foreach (bool useCookies in new[] { true, false })
            {
                yield return new object[] { "ABC", "123", useCookies };
                yield return new object[] { "Hello", "World", useCookies };
                yield return new object[] { "foo", "bar", useCookies };

                yield return new object[] { ".AspNetCore.Session", "RAExEmXpoCbueP_QYM", useCookies };

                yield return new object[]
                {
                    ".AspNetCore.Antiforgery.Xam7_OeLcN4",
                    "CfDJ8NGNxAt7CbdClq3UJ8_6w_4661wRQZT1aDtUOIUKshbcV4P0NdS8klCL5qGSN-PNBBV7w23G6MYpQ81t0PMmzIN4O04fqhZ0u1YPv66mixtkX3iTi291DgwT3o5kozfQhe08-RAExEmXpoCbueP_QYM",
                    useCookies
                };

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

                yield return new object[] { "foo", GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 126), useCookies };
                yield return new object[] { "foo", GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 127), useCookies };
                yield return new object[] { "foo", GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 128), useCookies };
                yield return new object[] { "foo", GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 129), useCookies };

                yield return new object[] { "foo", GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 254), useCookies };
                yield return new object[] { "foo", GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 255), useCookies };
                yield return new object[] { "foo", GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 256), useCookies };
                yield return new object[] { "foo", GenerateCookie(name: "foo", repeat: 'a', overallHeaderValueLength: 257), useCookies };
            }
        }
    }
}
