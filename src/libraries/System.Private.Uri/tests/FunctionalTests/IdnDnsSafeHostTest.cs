// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [Theory]
        [InlineData("foo", "foo", "foo", "foo")]
        [InlineData("BAR", "bar", "bar", "bar")]
        [InlineData("\u00FC", "\u00FC", "\u00FC", "xn--tda")]
        [InlineData("\u00FC.\u00FC", "\u00FC.\u00FC", "\u00FC.\u00FC", "xn--tda.xn--tda")]
        [InlineData("\u00FC.foo.\u00FC", "\u00FC.foo.\u00FC", "\u00FC.foo.\u00FC", "xn--tda.foo.xn--tda")]
        [InlineData("xn--tda", "xn--tda", "xn--tda", "xn--tda")]
        [InlineData("xn--tda.xn--tda", "xn--tda.xn--tda", "xn--tda.xn--tda", "xn--tda.xn--tda")]
        [InlineData("127.0.0.1", "127.0.0.1", "127.0.0.1", "127.0.0.1")]
        [InlineData("127.0.o.1", "127.0.o.1", "127.0.o.1", "127.0.o.1")]
        [InlineData("127.0.0.1.1", "127.0.0.1.1", "127.0.0.1.1", "127.0.0.1.1")]
        [InlineData("[::]", "[::]", "::", "::")]
        [InlineData("[123::]", "[123::]", "123::", "123::")]
        [InlineData("[123:123::]", "[123:123::]", "123:123::", "123:123::")]
        [InlineData("[123:123::%]", "[123:123::]", "123:123::%", "123:123::%")]
        [InlineData("[123:123::%foo]", "[123:123::]", "123:123::%foo", "123:123::%foo")]
        [InlineData("[123:123::%foo%20bar]", "[123:123::]", "123:123::%foo%20bar", "123:123::%foo%20bar")]
        public void Host_DnsSafeHost_IdnHost_ProcessedCorrectly(string hostString, string host, string dnsSafeHost, string idnHost)
        {
            Asserts($"wss://{hostString}", host, dnsSafeHost, idnHost);
            Asserts($"wss://{hostString}:1", host, dnsSafeHost, idnHost);
            Asserts($"http://{hostString}", host, dnsSafeHost, idnHost);
            Asserts($"http://{hostString}:1", host, dnsSafeHost, idnHost);
            Asserts($"https://{hostString}", host, dnsSafeHost, idnHost);
            Asserts($"https://{hostString}:1", host, dnsSafeHost, idnHost);

            Asserts($"\\\\{hostString}", host, dnsSafeHost, idnHost);
            Asserts($"file:////{hostString}", host, dnsSafeHost, idnHost);

            static void Asserts(string uriString, string host, string dnsSafeHost, string idnHost)
            {
                var uri = new Uri(uriString);

                Assert.Equal(host, uri.Host);
                Assert.Equal(dnsSafeHost, uri.DnsSafeHost);
                Assert.Equal(idnHost, uri.IdnHost);

                if (host == dnsSafeHost)
                {
                    Assert.Same(uri.Host, uri.DnsSafeHost);
                }
                else
                {
                    Assert.Same(uri.DnsSafeHost, uri.IdnHost);
                }
            }
        }
    }
}
