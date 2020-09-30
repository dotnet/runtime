// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public class CookiePortTest
    {
        private CookieContainer _cc;
        private Cookie _cookie;

        public CookiePortTest()
        {
            _cc = new CookieContainer();
            _cookie = new Cookie("name", "value1", "/path", "example.com");
            // use both space and comma as delimiter
            _cookie.Port = "\"80 110,1050, 1090 ,1100\"";
            _cc.Add(new Uri("http://example.com/path"), _cookie);
        }

        [Fact]
        public void Port_SetMultiplePorts_Port1Set()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://example.com:80/path"));
            Assert.Equal(1, cookies.Count);
        }

        [Fact]
        public void Port_SpaceDelimiter_PortSet()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://example.com:110/path"));
            Assert.Equal(1, cookies.Count);
        }

        [Fact]
        public void Port_CommaDelimiter_PortSet()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://example.com:1050/path"));
            Assert.Equal(1, cookies.Count);
        }

        [Fact]
        public void Port_CommaSpaceDelimiter_PortSet()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://example.com:1090/path"));
            Assert.Equal(1, cookies.Count);
        }

        [Fact]
        public void Port_SpaceCommaDelimiter_PortSet()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://example.com:1100/path"));
            Assert.Equal(1, cookies.Count);
        }

        [Fact]
        public void Port_SetMultiplePorts_NoPortMatch()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://example.com:1000/path"));
            Assert.Equal(0, cookies.Count);
        }

        [Fact]
        public void Port_SetMultiplePorts_NoPathMatch()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://example.com:1050"));
            Assert.Equal(0, cookies.Count);
        }

        [Fact]
        public void Port_NoPortSpecified_ReturnsCookies()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://example.com/path"));
            Assert.Equal(1, cookies.Count);
        }
    }
}
