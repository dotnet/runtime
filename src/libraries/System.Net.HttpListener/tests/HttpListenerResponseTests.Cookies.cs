// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // httpsys component missing in Nano.
    public class HttpListenerResponseCookiesTests : HttpListenerResponseTestBase
    {
        [Fact]
        public async Task Cookies_GetSet_ReturnsExpected()
        {
            HttpListenerResponse response = await GetResponse();
            Assert.Same(response.Cookies, response.Cookies);
            Assert.Empty(response.Cookies);

            var cookies = new CookieCollection() { new Cookie("name", "value") };
            response.Cookies = cookies;
            Assert.Equal(cookies, response.Cookies);

            response.Cookies = null;
            Assert.Empty(response.Cookies);
        }

        public static IEnumerable<object[]> Cookies_TestData()
        {
            yield return new object[]
            {
                new CookieCollection()
                {
                    new Cookie()
                },
                120, null, null
            };

            yield return new object[]
            {
                new CookieCollection()
                {
                    new Cookie(),
                    new Cookie("name", "value")
                },
                144, "Set-Cookie: name=value", null
            };

            yield return new object[]
            {
                new CookieCollection()
                {
                    new Cookie("name", "value")
                },
                144, "Set-Cookie: name=value", null
            };

            yield return new object[]
            {
                new CookieCollection()
                {
                    new Cookie("foo bar", "value")
                },
                147, "Set-Cookie: foo bar=value", null
            };

            yield return new object[]
            {
                new CookieCollection()
                {
                    new Cookie("name1", "value1"),
                    new Cookie("name2", "value2")
                },
                160, "Set-Cookie: name1=value1, name2=value2", null
            };

            yield return new object[]
            {
                new CookieCollection()
                {
                    new Cookie("name1", "value1") { Port = "\"200\"" },
                    new Cookie("name2", "value2") { Port = "\"300\"" },
                },
                207, null, "Set-Cookie2: name1=value1; Port=\"200\"; Version=1, name2=value2; Port=\"300\"; Version=1"
            };

            yield return new object[]
            {
                new CookieCollection()
                {
                    new Cookie("name1", "value1"),
                    new Cookie("name2", "value2") { Port = "\"300\"" },
                },
                196, "Set-Cookie: name1=value1", "Set-Cookie2: name2=value2; Port=\"300\"; Version=1"
            };
        }

        [Theory]
        [MemberData(nameof(Cookies_TestData))]
        public async Task Cookies_SetAndSend_ClientReceivesExpectedHeaders(CookieCollection cookies, int expectedBytes, string expectedSetCookie, string expectedSetCookie2)
        {
            HttpListenerResponse response = await GetResponse();
            response.Cookies = cookies;

            response.Close();

            Assert.Equal(expectedSetCookie?.Replace("Set-Cookie: ", ""), response.Headers["Set-Cookie"]);
            Assert.Equal(expectedSetCookie2?.Replace("Set-Cookie2: ", ""), response.Headers["Set-Cookie2"]);

            string clientResponse = GetClientResponse(expectedBytes);
            if (expectedSetCookie != null)
            {
                Assert.Contains($"\r\n{expectedSetCookie}\r\n", clientResponse);
            }
            else
            {
                Assert.DoesNotContain("Set-Cookie:", clientResponse);
            }

            if (expectedSetCookie2 != null)
            {
                Assert.Contains($"\r\n{expectedSetCookie2}\r\n", clientResponse);
            }
            else
            {
                Assert.DoesNotContain("Set-Cookie2:", clientResponse);
            }
        }

        [Fact]
        public async Task Cookies_SetInHeader_ClientReceivesExpectedHeaders()
        {
            HttpListenerResponse response = await GetResponse();
            response.Headers["Set-Cookie"] = "name1=value1";
            response.Headers["Set-Cookie2"] = "name2=value2";

            response.Close();

            string clientResponse = GetClientResponse(expectedLength:173);
            Assert.Contains("\r\nSet-Cookie: name1=value1\r\n", clientResponse);
            Assert.Contains("\r\nSet-Cookie2: name2=value2\r\n", clientResponse);
        }

        [Fact]
        public async Task Cookies_SetCookie2InHeadersButNotInCookies_RemovesFromHeaders()
        {
            HttpListenerResponse response = await GetResponse();
            response.Headers["Set-Cookie"] = "name1=value2";
            response.Headers["Set-Cookie2"] = "name2=value2";

            response.Cookies.Add(new Cookie("name3", "value3") { Port = "\"200\"" });

            response.Close();

            Assert.Null(response.Headers["Set-Cookie"]);
            Assert.Equal("name3=value3; Port=\"200\"; Version=1", response.Headers["Set-Cookie2"]);

            string clientResponse = GetClientResponse(expectedLength:170);
            Assert.DoesNotContain("Set-Cookie:", clientResponse);
            Assert.Contains("\r\nSet-Cookie2: name3=value3; Port=\"200\"; Version=1\r\n", clientResponse);
        }

        [Fact]
        public async Task Cookies_SetCookieInHeadersButNotInCookies_RemovesFromHeaders()
        {
            HttpListenerResponse response = await GetResponse();
            response.Headers["Set-Cookie"] = "name1=value2";
            response.Headers["Set-Cookie2"] = "name2=value2";

            response.Cookies.Add(new Cookie("name3", "value3"));

            response.Close();

            Assert.Equal("name3=value3", response.Headers["Set-Cookie"]);
            Assert.Null(response.Headers["Set-Cookie2"]);

            string clientResponse = GetClientResponse(expectedLength:146);
            Assert.Contains("\r\nSet-Cookie: name3=value3\r\n", clientResponse);
            Assert.DoesNotContain("Set-Cookie2", clientResponse);
        }
  
        [Fact]
        public async Task Cookies_AddMultipleInHeader_ClientReceivesExpectedHeaders()
        {
            HttpListenerResponse response = await GetResponse();
            response.Headers.Add("Set-Cookie", "name1=value1");
            response.Headers.Add("Set-Cookie", "name2=value2");
            response.Headers.Add("Set-Cookie", "name3=value3");
            response.Headers.Add("Set-Cookie", "name4=value4");

            response.Close();

            string clientResponse = GetClientResponse(expectedLength:224);
            Assert.Contains("\r\nSet-Cookie: name1=value1\r\n", clientResponse);
            Assert.Contains("\r\nSet-Cookie: name2=value2\r\n", clientResponse);
            Assert.Contains("\r\nSet-Cookie: name3=value3\r\n", clientResponse);
            Assert.Contains("\r\nSet-Cookie: name4=value4\r\n", clientResponse);
        }

        [Fact]
        public async Task AppendCookie_ValidCookie_AddsCookieToCollection()
        {
            HttpListenerResponse response = await GetResponse();

            var cookie1 = new Cookie("name1", "value");
            var cookie2 = new Cookie("name2", "value2");

            response.AppendCookie(cookie1);
            response.AppendCookie(cookie2);
            Assert.Equal(new Cookie[] { cookie1, cookie2 }, response.Cookies.Cast<Cookie>());

            var cookie3 = new Cookie("name1", "value2");
            response.AppendCookie(cookie3);
            Assert.Equal(new Cookie[] { cookie3, cookie2 }, response.Cookies.Cast<Cookie>());

            // Cookies are not cloned.
            cookie3.Value = "value3";
            Assert.Equal("value3", response.Cookies[0].Value);
        }

        [Fact]
        public async Task AppendCookie_NullCookie_ThrowsArgumentNullException()
        {
            HttpListenerResponse response = await GetResponse();
            AssertExtensions.Throws<ArgumentNullException>("cookie", () => response.AppendCookie(null));
        }

        [Fact]
        public async Task SetCookie_ValidCookie_AddsCookieToCollection()
        {
            HttpListenerResponse response = await GetResponse();

            var cookie1 = new Cookie("name1", "value1");
            var cookie2 = new Cookie("name2", "value2");

            response.SetCookie(cookie1);
            response.SetCookie(cookie2);
            Assert.Equal(new Cookie[] { cookie1, cookie2 }, response.Cookies.Cast<Cookie>());
        }

        [Fact]
        public async Task SetCookie_ValidCookie_ClonesCookie()
        {
            HttpListenerResponse response = await GetResponse();
            var cookie = new Cookie("name", "value");
            response.SetCookie(cookie);

            // Cookies are cloned.
            cookie.Value = "value3";
            Assert.Equal("value", response.Cookies[0].Value);
        }

        [Fact]
        public async Task SetCookie_NullCookie_ThrowsArgumentNullException()
        {
            HttpListenerResponse response = await GetResponse();
            AssertExtensions.Throws<ArgumentNullException>("cookie", () => response.SetCookie(null));
        }

        [Fact]
        public async Task SetCookie_CookieDoesntExist_ThrowsArgumentException()
        {
            HttpListenerResponse response = await GetResponse();
            var cookie1 = new Cookie("name", "value");

            response.SetCookie(cookie1);
            AssertExtensions.Throws<ArgumentException>("cookie", () => response.SetCookie(cookie1));

            var cookie2 = new Cookie("name", "value2");
            AssertExtensions.Throws<ArgumentException>("cookie", () => response.SetCookie(cookie2));
            Assert.Equal(new Cookie[] { cookie2 }, response.Cookies.Cast<Cookie>());
        }
    }
}
