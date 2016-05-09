// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.PrivateUri.Tests
{
    /// <summary>
    /// Summary description for IdnDnsSafeHostTest
    /// </summary>
    public class IdnDnsSafeHostTest
    {
        [Fact]
        public void IdnDnsSafeHost_IdnOffWithBuiltInScheme_Success()
        {
            Uri test = new Uri("http://www.\u30AF.com/");

            string dns = test.DnsSafeHost;

            Assert.True(dns.IndexOf('%') < 0, "% found");
            Assert.True(test.AbsoluteUri.IndexOf('%') < 0, "% found: " + test.AbsoluteUri);
            Assert.True(test.ToString().IndexOf('%') < 0, "% found: " + test.ToString());
        }

        [Fact]
        public void IdnDnsSafeHost_IdnOffWithUnregisteredScheme_Success()
        {
            Uri test = new Uri("any://www.\u30AF.com/");

            string dns = test.DnsSafeHost;

            Assert.True(dns.IndexOf('%') < 0, "% found");
            Assert.True(test.AbsoluteUri.IndexOf('%') < 0, "% found: " + test.AbsoluteUri);
            Assert.True(test.ToString().IndexOf('%') < 0, "% found: " + test.ToString());
        }

        [Fact]
        public void IdnDnsSafeHost_IPv6Host_ScopeIdButNoBrackets()
        {
            Uri test = new Uri("http://[::1%23]/");

            Assert.Equal("::1%23", test.DnsSafeHost);
            Assert.Equal("::1%23", test.IdnHost);
            Assert.Equal("[::1]", test.Host);
        }

        [Fact]
        public void IdnDnsSafeHost_MixedCase_ToLowerCase()
        {
            Uri test = new Uri("HTTPS://www.xn--pck.COM/");

            Assert.Equal("www.xn--pck.com", test.Host);
            Assert.Equal("www.xn--pck.com", test.DnsSafeHost);
            Assert.Equal("www.xn--pck.com", test.IdnHost);
            Assert.Equal("https://www.xn--pck.com/", test.AbsoluteUri);
        }

        [Fact]
        public void IdnDnsSafeHost_SingleLabelAllExceptIntranet_Unicode()
        {
            Uri test = new Uri("HTTPS://\u30AF/");

            Assert.Equal("\u30AF", test.Host);
            Assert.Equal("\u30AF", test.DnsSafeHost);
            Assert.Equal("xn--pck", test.IdnHost);
            Assert.Equal("https://\u30AF/", test.AbsoluteUri);
        }

        [Fact]
        public void IdnDnsSafeHost_MultiLabelAllExceptIntranet_Punycode()
        {
            Uri test = new Uri("HTTPS://\u30AF.com/");

            Assert.Equal("\u30AF.com", test.Host);
            Assert.Equal("\u30AF.com", test.DnsSafeHost);
            Assert.Equal("xn--pck.com", test.IdnHost);
            Assert.Equal("https://\u30AF.com/", test.AbsoluteUri);
        }
    }
}
