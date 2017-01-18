// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.PrivateUri.Tests
{
    public class UriGetComponentsTest
    {
        [Fact]
        public void GetComponents_ASCIIHost_LowerCaseResult()
        {
            Uri testUri = new Uri("http://MixedCase.HostName");
            Assert.Equal("mixedcase.hostname", testUri.GetComponents(UriComponents.Host, UriFormat.UriEscaped));
            Assert.Equal("mixedcase.hostname", testUri.Host);
        }

        [Fact]
        public void GetComponents_PunycodeHostIriOnIdnOff_LowerCaseResult()
        {
            Uri testUri = new Uri("http://wWw.xn--pCk.Com");
            Assert.Equal("www.xn--pck.com", testUri.GetComponents(UriComponents.Host, UriFormat.UriEscaped));
            Assert.Equal("www.xn--pck.com", testUri.Host);
            Assert.Equal("www.xn--pck.com", testUri.DnsSafeHost);
        }

        [Fact]
        public void GetComponents_UnknownScheme_ComponentsUnaffected()
        {
            Uri testUri;

            testUri = new Uri("eb://00000000000000000000000/dir1/dir2?query=expression#fragment");
            Assert.Equal("00000000000000000000000", testUri.Authority);
            Assert.Equal("eb", testUri.Scheme);
            Assert.Equal("/dir1/dir2", testUri.AbsolutePath);
            Assert.Equal("?query=expression", testUri.Query);
            Assert.Equal("#fragment", testUri.Fragment);
            Assert.Equal("eb://00000000000000000000000/dir1/dir2?query=expression#fragment", testUri.AbsoluteUri);

            // Hex numbers should not be converted
            testUri = new Uri("eb://123.231.0x0f.1/dir");
            Assert.Equal("123.231.0x0f.1", testUri.Authority);

            // Octal numbers should not be converted
            testUri = new Uri("eb://123.032.123.023/dir");
            Assert.Equal("123.032.123.023", testUri.Authority);
            testUri = new Uri("eb://123.002.123.023/dir");
            Assert.Equal("123.002.123.023", testUri.Authority);
            testUri = new Uri("eb://123.0032.123.023/dir");
            Assert.Equal("123.0032.123.023", testUri.Authority);

            // IP Address containing 0, 00 and 000
            testUri = new Uri("abcd://123.0.10.100/dir");
            Assert.Equal("123.0.10.100", testUri.Authority);
            testUri = new Uri("efghi://123.00.10.100/dir");
            Assert.Equal("123.00.10.100", testUri.Authority);
            testUri = new Uri("ijklmn://123.000.10.100/dir");
            Assert.Equal("123.000.10.100", testUri.Authority);

            // Known limitation: port will always be canonicalized since it is exposed as an int.
            testUri = new Uri("abc://127.00.000.001:01234");
            Assert.Equal("127.00.000.001:1234", testUri.Authority);

            // Known limitation: port 0 is ignored.
            testUri = new Uri("cbd://127.00.1.2:0000");
            Assert.Equal("127.00.1.2", testUri.Authority);
            testUri = new Uri("cbd://127.00.1.2:0");
            Assert.Equal("127.00.1.2", testUri.Authority);

            testUri = new Uri("eb://[0000::01:123.32.123.23]/dir");
            Assert.Equal("[::1:7b20:7b17]", testUri.Authority);

            // TODO #8330 : Canonical IPv6 is still performed for unknown schemes.
            Assert.NotEqual("[0000::01:123.32.123.23]", testUri.Authority);
        }

        // Visual Studio 12 has a dependency on this behavior.
        [Fact]
        public void GetComponents_UnknownScheme_LocalHostAndPort_Success()
        {
            Uri testUri = new Uri("tcp://127.0.0.1:23714");
            Assert.Equal("127.0.0.1", testUri.Host);
            Assert.Equal(23714, testUri.Port);
        }
    }
}
