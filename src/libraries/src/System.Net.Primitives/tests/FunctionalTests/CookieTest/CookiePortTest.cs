// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;

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
            _cookie = new Cookie("name", "value1", "/path", "localhost");
            // use both space and comma as delimiter
            _cookie.Port = "\"80 110,1050, 1090 ,1100\"";
            _cc.Add(new Uri("http://localhost/path"), _cookie);
        }

        [Fact]
        public void Port_SetMultiplePorts_Port1Set()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://localhost:80/path"));
            Assert.Equal(1, cookies.Count);
        }

        [Fact]
        public void Port_SpaceDelimiter_PortSet()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://localhost:110/path"));
            Assert.Equal(1, cookies.Count);
        }

        [Fact]
        public void Port_CommaDelimiter_PortSet()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://localhost:1050/path"));
            Assert.Equal(1, cookies.Count);
        }

        [Fact]
        public void Port_CommaSpaceDelimiter_PortSet()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://localhost:1090/path"));
            Assert.Equal(1, cookies.Count);
        }

        [Fact]
        public void Port_SpaceCommaDelimiter_PortSet()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://localhost:1100/path"));
            Assert.Equal(1, cookies.Count);
        }

        [Fact]
        public void Port_SetMultiplePorts_NoPortMatch()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://localhost:1000/path"));
            Assert.Equal(0, cookies.Count);
        }

        [Fact]
        public void Port_SetMultiplePorts_NoPathMatch()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://localhost:1050"));
            Assert.Equal(0, cookies.Count);
        }

        [Fact]
        public void Port_NoPortSpecified_ReturnsCookies()
        {
            CookieCollection cookies = _cc.GetCookies(new Uri("http://localhost/path"));
            Assert.Equal(1, cookies.Count);
        }
    }
}
