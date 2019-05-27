// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Test.Common;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public abstract class HttpClientHandlerTest_Cookies : HttpClientHandlerTestBase
    {
        private const string s_cookieName = "ABC";
        private const string s_cookieValue = "123";
        private const string s_expectedCookieHeaderValue = "ABC=123";

        private const string s_customCookieHeaderValue = "CustomCookie=456";

        private const string s_simpleContent = "Hello world!";

        public HttpClientHandlerTest_Cookies(ITestOutputHelper output) : base(output) { }

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
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = CreateHttpClient())
                    {
                        await client.GetAsync(uri);
                    }
                },
                async server =>
                {
                    HttpRequestData requestData = await server.HandleRequestAsync();
                    Assert.Equal(0, requestData.GetHeaderValueCount("Cookie"));
                });
        }

        [Theory]
        [MemberData(nameof(CookieNamesValuesAndUseCookies))]
        public async Task GetAsync_SetCookieContainer_CookieSent(string cookieName, string cookieValue, bool useCookies)
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    HttpClientHandler handler = CreateHttpClientHandler();
                    handler.CookieContainer = CreateSingleCookieContainer(uri, cookieName, cookieValue);
                    handler.UseCookies = useCookies;

                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        await client.GetAsync(uri);
                    }
                },
                async server =>
                {
                    HttpRequestData requestData = await server.HandleRequestAsync();
                    if (useCookies)
                    {
                        Assert.Equal(GetCookieHeaderValue(cookieName, cookieValue), requestData.GetSingleHeaderValue("Cookie"));
                    }
                    else
                    {
                        Assert.Equal(0, requestData.GetHeaderValueCount("Cookie"));
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

            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    HttpClientHandler handler = CreateHttpClientHandler();
                    var cookieContainer = new CookieContainer();
                    foreach (Cookie c in cookies)
                    {
                        cookieContainer.Add(uri, c);
                    }
                    handler.CookieContainer = cookieContainer;

                    using (HttpClient client = CreateHttpClient(handler))
                    {
                        await client.GetAsync(uri);
                    }
                },
                async server =>
                {
                    HttpRequestData requestData = await server.HandleRequestAsync();
                    string expectedHeaderValue = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}").ToArray());
                    Assert.Equal(expectedHeaderValue, requestData.GetSingleHeaderValue("Cookie"));
                });
        }

        [Fact]
        public async Task GetAsync_AddCookieHeader_CookieHeaderSent()
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = CreateHttpClient())
                    {
                        var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri) { Version = VersionFromUseHttp2 };
                        requestMessage.Headers.Add("Cookie", s_customCookieHeaderValue);

                        await client.SendAsync(requestMessage);
                    }
                },
                async server =>
                {
                    HttpRequestData requestData = await server.HandleRequestAsync();
                    Assert.Equal(s_customCookieHeaderValue, requestData.GetSingleHeaderValue("Cookie"));
                });
        }

        [ActiveIssue(30051, TargetFrameworkMonikers.Uap)]
        [Fact]
        public async Task GetAsync_AddMultipleCookieHeaders_CookiesSent()
        {
            await LoopbackServerFactory.CreateClientAndServerAsync(
                async uri =>
                {
                    using (HttpClient client = CreateHttpClient())
                    {
                        var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri) { Version = VersionFromUseHttp2 };
                        requestMessage.Headers.Add("Cookie", "A=1");
                        requestMessage.Headers.Add("Cookie", "B=2");
                        requestMessage.Headers.Add("Cookie", "C=3");

                        await client.SendAsync(requestMessage);
                    }
                },
                async server =>
                {
                    HttpRequestData requestData = await server.HandleRequestAsync();

                    // Multiple Cookie header values are treated as any other header values and are 
                    // concatenated using ", " as the separator.

                    string cookieHeaderValue = requestData.GetSingleHeaderValue("Cookie");

                    var cookieValues = cookieHeaderValue.Split(new string[] { ", " }, StringSplitOptions.None);
                    Assert.Contains("A=1", cookieValues);
                    Assert.Contains("B=2", cookieValues);
                    Assert.Contains("C=3", cookieValues);
                    Assert.Equal(3, cookieValues.Count());
                });
        }

        private string GetCookieValue(HttpRequestData request)
        {
            if (!LoopbackServerFactory.IsHttp2)
            {
                // HTTP/1.x must have only one value.
                return request.GetSingleHeaderValue("Cookie");
            }

            string cookieHeaderValue = null;
            string[] cookieHeaderValues = request.GetHeaderValues("Cookie");

            foreach (string header in cookieHeaderValues)
            {
                if (cookieHeaderValue == null)
                {
                    cookieHeaderValue = header;
                }
                else
                {
                    // rfc7540 8.1.2.5 states multiple cookie headers should be represented as single value.
                    cookieHeaderValue = String.Concat(cookieHeaderValue, "; ", header);
                }
            }

            return cookieHeaderValue;
        }

        [ConditionalFact]
        public async Task GetAsync_SetCookieContainerAndCookieHeader_BothCookiesSent()
        {
            if (IsCurlHandler)
            {
                // CurlHandler ignores container cookies when custom Cookie header is set.
                // SocketsHttpHandler behaves the expected way. Not worth fixing in CurlHandler as it is going away.
                throw new SkipTestException("Platform limitation with curl");
            }

            await LoopbackServerFactory.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.CookieContainer = CreateSingleCookieContainer(url);

                using (HttpClient client = CreateHttpClient(handler))
                {
                    var requestMessage = new HttpRequestMessage(HttpMethod.Get, url) { Version = VersionFromUseHttp2 };
                    requestMessage.Headers.Add("Cookie", s_customCookieHeaderValue);

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(requestMessage);
                    Task<HttpRequestData> serverTask = server.HandleRequestAsync();
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    HttpRequestData requestData = await serverTask;
                    string cookieHeaderValue = GetCookieValue(requestData);
                    var cookies = cookieHeaderValue.Split(new string[] { "; " }, StringSplitOptions.None);
                    Assert.Contains(s_expectedCookieHeaderValue, cookies);
                    Assert.Contains(s_customCookieHeaderValue, cookies);
                    Assert.Equal(2, cookies.Count());
                }
            });
        }

        [ActiveIssue(30051, TargetFrameworkMonikers.Uap)]
        [ConditionalFact]
        public async Task GetAsync_SetCookieContainerAndMultipleCookieHeaders_BothCookiesSent()
        {
            if (IsCurlHandler)
            {
                // CurlHandler ignores container cookies when custom Cookie header is set.
                // SocketsHttpHandler behaves the expected way. Not worth fixing in CurlHandler as it is going away.
                throw new SkipTestException("Platform limitation with curl");
            }

            await LoopbackServerFactory.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.CookieContainer = CreateSingleCookieContainer(url);

                using (HttpClient client = CreateHttpClient(handler))
                {
                    var requestMessage = new HttpRequestMessage(HttpMethod.Get, url) { Version = VersionFromUseHttp2 };
                    requestMessage.Headers.Add("Cookie", "A=1");
                    requestMessage.Headers.Add("Cookie", "B=2");

                    Task<HttpResponseMessage> getResponseTask = client.SendAsync(requestMessage);
                    Task<HttpRequestData> serverTask = server.HandleRequestAsync();
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    HttpRequestData requestData = await serverTask;
                    string cookieHeaderValue = GetCookieValue(requestData);

                    // Multiple Cookie header values are treated as any other header values and are
                    // concatenated using ", " as the separator.  The container cookie is concatenated to
                    // one of these values using the "; " cookie separator.

                    var cookieValues = cookieHeaderValue.Split(new string[] { ", " }, StringSplitOptions.None);
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

            await LoopbackServerFactory.CreateClientAndServerAsync(async url =>
            {
                Uri url1 = new Uri(url, path1);
                Uri url2 = new Uri(url, path2);
                Uri unusedUrl = new Uri(url, "/unused");

                HttpClientHandler handler = CreateHttpClientHandler();
                handler.CookieContainer = new CookieContainer();
                handler.CookieContainer.Add(url1, new Cookie("cookie1", "value1"));
                handler.CookieContainer.Add(url2, new Cookie("cookie2", "value2"));
                handler.CookieContainer.Add(unusedUrl, new Cookie("cookie3", "value3"));

                using (HttpClient client = CreateHttpClient(handler))
                {
                    client.DefaultRequestHeaders.ConnectionClose = true; // to avoid issues with connection pooling
                    await client.GetAsync(url1);
                }
            },
            async server =>
            {
                HttpRequestData requestData1 = await server.HandleRequestAsync(HttpStatusCode.Found, new HttpHeaderData[] { new HttpHeaderData("Location", path2) });
                Assert.Equal("cookie1=value1", requestData1.GetSingleHeaderValue("Cookie"));

                HttpRequestData requestData2 = await server.HandleRequestAsync(content: s_simpleContent);
                Assert.Equal("cookie2=value2", requestData2.GetSingleHeaderValue("Cookie"));
            });
        }

        //
        // Receive cookie tests
        //

        [Theory]
        [MemberData(nameof(CookieNamesValuesAndUseCookies))]
        public async Task GetAsync_ReceiveSetCookieHeader_CookieAdded(string cookieName, string cookieValue, bool useCookies)
        {
            await LoopbackServerFactory.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.UseCookies = useCookies;

                using (HttpClient client = CreateHttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<HttpRequestData> serverTask = server.HandleRequestAsync(
                        HttpStatusCode.OK, new HttpHeaderData[] { new HttpHeaderData("Set-Cookie", GetCookieHeaderValue(cookieName, cookieValue)) }, s_simpleContent);
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
            await LoopbackServerFactory.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();

                using (HttpClient client = CreateHttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<HttpRequestData> serverTask = server.HandleRequestAsync(
                        HttpStatusCode.OK,
                        new HttpHeaderData[]
                        {
                            new HttpHeaderData("Set-Cookie", "A=1; Path=/"),
                            new HttpHeaderData("Set-Cookie", "B=2; Path=/"),
                            new HttpHeaderData("Set-Cookie", "C=3; Path=/")
                        },
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

            await LoopbackServerFactory.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.CookieContainer = CreateSingleCookieContainer(url);

                using (HttpClient client = CreateHttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<HttpRequestData> serverTask = server.HandleRequestAsync(
                        HttpStatusCode.OK,
                        new HttpHeaderData[] { new HttpHeaderData("Set-Cookie", $"{s_cookieName}={newCookieValue}") },
                        s_simpleContent);
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    CookieCollection collection = handler.CookieContainer.GetCookies(url);
                    Assert.Equal(1, collection.Count);
                    Assert.Equal(s_cookieName, collection[0].Name);
                    Assert.Equal(newCookieValue, collection[0].Value);
                }
            });
        }

        [ActiveIssue(30051, TargetFrameworkMonikers.Uap)] // fails to remove cookie
        [Fact]
        public async Task GetAsync_ReceiveSetCookieHeader_CookieRemoved()
        {
            await LoopbackServerFactory.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.CookieContainer = CreateSingleCookieContainer(url);

                using (HttpClient client = CreateHttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<HttpRequestData> serverTask = server.HandleRequestAsync(
                        HttpStatusCode.OK,
                        new HttpHeaderData[] { new HttpHeaderData("Set-Cookie", $"{s_cookieName}=; Expires=Sun, 06 Nov 1994 08:49:37 GMT") },
                        s_simpleContent);
                    await TestHelper.WhenAllCompletedOrAnyFailed(getResponseTask, serverTask);

                    CookieCollection collection = handler.CookieContainer.GetCookies(url);
                    Assert.Equal(0, collection.Count);
                }
            });
        }

        [ActiveIssue(30051, TargetFrameworkMonikers.Uap)] // only adds one cookie
        [Fact]
        public async Task GetAsync_ReceiveInvalidSetCookieHeader_ValidCookiesAdded()
        {
            if (IsNetfxHandler)
            {
                // NetfxHandler incorrectly only processes one valid cookie 
                return;
            }

            await LoopbackServerFactory.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();

                using (HttpClient client = CreateHttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<HttpRequestData> serverTask = server.HandleRequestAsync(
                        HttpStatusCode.OK,
                        new HttpHeaderData[]
                        {
                            new HttpHeaderData("Set-Cookie", "A=1; Path=/;Expires=asdfsadgads"), // invalid Expires
                            new HttpHeaderData("Set-Cookie", "B=2; Path=/"),
                            new HttpHeaderData("Set-Cookie", "C=3; Path=/")
                        },
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

            await LoopbackServerFactory.CreateClientAndServerAsync(async url =>
            {
                Uri url1 = new Uri(url, path1);

                HttpClientHandler handler = CreateHttpClientHandler();

                using (HttpClient client = CreateHttpClient(handler))
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
                HttpRequestData requestData1 = await server.HandleRequestAsync(
                    HttpStatusCode.Found,
                    new HttpHeaderData[]
                    {
                        new HttpHeaderData("Location", $"{path2}"),
                        new HttpHeaderData("Set-Cookie", "A=1; Path=/")
                    });

                Assert.Equal(0, requestData1.GetHeaderValueCount("Cookie"));

                HttpRequestData requestData2 = await server.HandleRequestAsync(
                    HttpStatusCode.OK,
                    new HttpHeaderData[]
                    {
                        new HttpHeaderData("Set-Cookie", "B=2; Path=/")
                    },
                    s_simpleContent);

                Assert.Equal("A=1", requestData2.GetSingleHeaderValue("Cookie"));
            });
        }

        [ActiveIssue(30050, TargetFrameworkMonikers.Uap)]
        [Fact]
        public async Task GetAsyncWithBasicAuth_ReceiveSetCookie_CookieSent()
        {
            if (IsWinHttpHandler)
            {
                // Issue #26986
                // WinHttpHandler does not process the cookie.
                return;
            }

            await LoopbackServerFactory.CreateClientAndServerAsync(async url =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();
                handler.Credentials = new NetworkCredential("user", "pass");

                using (HttpClient client = CreateHttpClient(handler))
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
                HttpRequestData requestData1 = await server.HandleRequestAsync(
                    HttpStatusCode.Unauthorized,
                    new HttpHeaderData[]
                    {
                        new HttpHeaderData("WWW-Authenticate", "Basic realm=\"WallyWorld\""),
                        new HttpHeaderData("Set-Cookie", "A=1; Path=/")
                    });

                Assert.Equal(0, requestData1.GetHeaderValueCount("Cookie"));

                HttpRequestData requestData2 = await server.HandleRequestAsync(
                    HttpStatusCode.OK,
                    new HttpHeaderData[]
                    {
                        new HttpHeaderData("Set-Cookie", "B=2; Path=/")
                    },
                    s_simpleContent);

                Assert.Equal("A=1", requestData2.GetSingleHeaderValue("Cookie"));
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
                yield return new object[] { "Hello World", "value", useCookies };
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

    public abstract class HttpClientHandlerTest_Cookies_Http11 : HttpClientHandlerTestBase
    {
        public HttpClientHandlerTest_Cookies_Http11(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task GetAsync_ReceiveMultipleSetCookieHeaders_CookieAdded()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                HttpClientHandler handler = CreateHttpClientHandler();

                using (HttpClient client = CreateHttpClient(handler))
                {
                    Task<HttpResponseMessage> getResponseTask = client.GetAsync(url);
                    Task<List<string>> serverTask = server.AcceptConnectionSendResponseAndCloseAsync(
                        HttpStatusCode.OK,
                        $"Set-Cookie: A=1; Path=/\r\n" +
                        $"Set-Cookie   : B=2; Path=/\r\n" + // space before colon to verify header is trimmed and recognized
                        $"Set-Cookie:    C=3; Path=/\r\n",
                        "Hello world!");
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
    }
}
