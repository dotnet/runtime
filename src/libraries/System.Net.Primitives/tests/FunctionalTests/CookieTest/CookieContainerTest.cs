// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public partial class CookieContainerTest
    {
        private const string CookieName1 = "CookieName1";
        private const string CookieName2 = "CookieName2";
        private const string CookieValue1 = "CookieValue1";
        private const string CookieValue2 = "CookieValue2";

        [Fact]
        public void Add_CookieVersion1AndRootDomainWithNoLeadingDot_Success()
        {
            const string SchemePrefix = "http://";
            const string OriginalDomain = "contoso.com";

            var container = new CookieContainer();
            var cookie = new Cookie(CookieName1, CookieValue1) { Version = 1, Domain = OriginalDomain };
            var uri = new Uri(SchemePrefix + OriginalDomain);
            container.Add(uri, cookie);
            Assert.Equal(1, container.GetCookies(uri).Count);
        }

        [Fact]
        public void GetCookies_AddCookiesWithImplicitDomain_CookiesReturnedOnlyForExactDomainMatch()
        {
            const string SchemePrefix = "http://";
            const string OriginalDomain = "contoso.com";

            var container = new CookieContainer();
            var cookie1 = new Cookie(CookieName1, CookieValue1);
            var cookie2 = new Cookie(CookieName2, CookieValue2) { Version = 1 };
            var uri = new Uri(SchemePrefix + OriginalDomain);
            container.Add(uri, cookie1);
            container.Add(uri, cookie2);

            var cookies = container.GetCookies(uri);
            Assert.Equal(2, cookies.Count);
            Assert.Equal(OriginalDomain, cookies[CookieName1].Domain);
            Assert.Equal(OriginalDomain, cookies[CookieName2].Domain);

            uri = new Uri(SchemePrefix + "www." + OriginalDomain);
            cookies = container.GetCookies(uri);
            Assert.Equal(0, cookies.Count);

            uri = new Uri(SchemePrefix + "x.www." + OriginalDomain);
            cookies = container.GetCookies(uri);
            Assert.Equal(0, cookies.Count);

            uri = new Uri(SchemePrefix + "y.x.www." + OriginalDomain);
            cookies = container.GetCookies(uri);
            Assert.Equal(0, cookies.Count);

            uri = new Uri(SchemePrefix + "z.y.x.www." + OriginalDomain);
            cookies = container.GetCookies(uri);
            Assert.Equal(0, cookies.Count);
        }

        [Fact]
        public void GetCookies_AddCookieVersion0WithExplicitDomain_CookieReturnedForDomainAndSubdomains()
        {
            const string SchemePrefix = "http://";
            const string OriginalDomain = "contoso.com";

            var container = new CookieContainer();
            var cookie1 = new Cookie(CookieName1, CookieValue1) { Domain = OriginalDomain };
            container.Add(new Uri(SchemePrefix + OriginalDomain), cookie1);

            var uri = new Uri(SchemePrefix + OriginalDomain);
            var cookies = container.GetCookies(uri);
            Assert.Equal(1, cookies.Count);
            Assert.Equal(OriginalDomain, cookies[CookieName1].Domain);

            uri = new Uri(SchemePrefix + "www." + OriginalDomain);
            cookies = container.GetCookies(uri);
            Assert.Equal(1, cookies.Count);

            uri = new Uri(SchemePrefix + "x.www." + OriginalDomain);
            cookies = container.GetCookies(uri);
            Assert.Equal(1, cookies.Count);

            uri = new Uri(SchemePrefix + "y.x.www." + OriginalDomain);
            cookies = container.GetCookies(uri);
            Assert.Equal(1, cookies.Count);

            uri = new Uri(SchemePrefix + "z.y.x.www." + OriginalDomain);
            cookies = container.GetCookies(uri);
            Assert.Equal(1, cookies.Count);
        }

        [Fact]
        public void GetCookies_AddCookieVersion1WithExplicitDomain_CookieReturnedForDomainAndOneLevelSubDomain()
        {
            const string SchemePrefix = "http://";
            const string OriginalDomain = "contoso.com";
            const string OriginalDomainWithLeadingDot = "." + OriginalDomain;

            var container = new CookieContainer();
            var cookie1 = new Cookie(CookieName1, CookieValue1) { Domain = OriginalDomainWithLeadingDot, Version = 1 };
            container.Add(new Uri(SchemePrefix + OriginalDomain), cookie1);

            var uri = new Uri(SchemePrefix + OriginalDomain);
            var cookies = container.GetCookies(uri);
            Assert.Equal(1, cookies.Count);
            Assert.Equal(OriginalDomainWithLeadingDot, cookies[CookieName1].Domain);

            uri = new Uri(SchemePrefix + "www." + OriginalDomain);
            cookies = container.GetCookies(uri);
            Assert.Equal(1, cookies.Count);

            uri = new Uri(SchemePrefix + "x.www." + OriginalDomain);
            cookies = container.GetCookies(uri);
            Assert.Equal(0, cookies.Count);

            uri = new Uri(SchemePrefix + "y.x.www." + OriginalDomain);
            cookies = container.GetCookies(uri);
            Assert.Equal(0, cookies.Count);

            uri = new Uri(SchemePrefix + "z.y.x.www." + OriginalDomain);
            cookies = container.GetCookies(uri);
            Assert.Equal(0, cookies.Count);
        }
    }
}
