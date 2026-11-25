// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;

using Xunit;

namespace NetPrimitivesUnitTests
{
    public class CookieInternalTest
    {
        private const string SchemePrefix = "http://";
        private const string OriginalDomain = "contoso.com";
        private const string CookieName = "CookieName";
        private const string CookieValue = "CookieValue";

        [Fact]
        public void DomainImplicit_AddCookieWithImplicitDomain_DomainImplicitTrue()
        {
            var container = new CookieContainer();
            var cookie = new Cookie(CookieName, CookieValue);
            var uri = new Uri(SchemePrefix + OriginalDomain);
            container.Add(uri, cookie);

            var cookies = container.GetCookies(uri);
            Assert.Equal(1, cookies.Count);
            Assert.True(cookies[CookieName].DomainImplicit);
        }

        [Fact]
        public void DomainImplicit_AddCookieWithExplicitDomain_DomainImplicitFalse()
        {
            var container = new CookieContainer();
            var cookie1 = new Cookie(CookieName, CookieValue) { Domain = OriginalDomain };
            var uri = new Uri(SchemePrefix + OriginalDomain);
            container.Add(uri, cookie1);

            var cookies = container.GetCookies(uri);
            Assert.Equal(1, cookies.Count);
            Assert.False(cookies[CookieName].DomainImplicit);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("\"", "\"")]
        [InlineData("\"\"", "")]
        [InlineData("/a", "/a")]
        [InlineData("\"/a\"", "/a")]
        [InlineData("\"/a", "\"/a")]
        [InlineData("/a\"", "/a\"")]
        [InlineData("\"\"/a\"\"", "\"/a\"")]
        [InlineData("\"a\0b\"", "a\0b")]
        public void CookieParserCheckQuoted_RemovesOnlySurroundingQuotes(string value, string expected)
        {
            Assert.Equal(expected, CookieParser.CheckQuoted(value));
            Assert.Equal(expected, CookieParser.CheckQuotedSpan(value).ToString());

            if (value.Length == expected.Length)
            {
                Assert.Same(value, CookieParser.CheckQuoted(value));
            }
        }

        [Theory]
        [InlineData("expires=Thu, 09 Jun 2050 10:18:14 GMT; version=1")]
        [InlineData("expires=\"Thu, 09 Jun 2050 10:18:14 GMT\"; version=\"1\"")]
        public void CookieParserGet_ExpiresAndVersion_ParsesAttributes(string attributes)
        {
            var parser = new CookieParser($"{CookieName}={CookieValue}; {attributes}");
            Cookie cookie = parser.Get();

            Assert.NotNull(cookie);
            Assert.Equal(CookieName, cookie.Name);
            Assert.Equal(CookieValue, cookie.Value);
            Assert.Equal(1, cookie.Version);
            Assert.Equal(new DateTime(2050, 6, 9, 10, 18, 14, DateTimeKind.Utc), cookie.Expires);
            Assert.Equal(DateTimeKind.Utc, cookie.Expires.Kind);
        }

        [Theory]
        [InlineData("max-age=-1")]
        [InlineData("max-age=\"-1\"")]
        public void CookieParserGet_MaxAge_ExpiresCookie(string attribute)
        {
            var parser = new CookieParser($"{CookieName}={CookieValue}; {attribute}");
            Cookie cookie = parser.Get();

            Assert.NotNull(cookie);
            Assert.Equal(CookieName, cookie.Name);
            Assert.Equal(CookieValue, cookie.Value);
            Assert.True(cookie.Expired);
        }

        [Theory]
        [InlineData("cookie_name=cookie_value", new[] { "cookie_name", "cookie_value" })]
        [InlineData("cookie_name=cookie_value;", new[] { "cookie_name", "cookie_value" })]
        [InlineData("cookie_name1=cookie_value1;cookie_name2=cookie_value2", new[] { "cookie_name1", "cookie_value1", "cookie_name2", "cookie_value2" })]
        [InlineData("cookie_name1=cookie_value1;cookie_name2=cookie_value2;", new[] { "cookie_name1", "cookie_value1", "cookie_name2", "cookie_value2" })]
        public void CookieParserGetServer_SetCookieHeaderValue_Success(string cookieString, string[] expectedStrings)
        {
            int index = 0;
            int cookieCount = 0;
            var parser = new CookieParser(cookieString);
            while (true)
            {
                Cookie cookie = parser.GetServer();
                if (cookie == null)
                {
                    break;
                }

                cookieCount++;
                Assert.Equal(expectedStrings[index++], cookie.Name);
                Assert.Equal(expectedStrings[index++], cookie.Value);
            }

            int expectedCookieCount = expectedStrings.Length >> 1;
            Assert.Equal(expectedCookieCount, cookieCount);
        }
    }
}
