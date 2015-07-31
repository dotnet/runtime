// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    }
}
