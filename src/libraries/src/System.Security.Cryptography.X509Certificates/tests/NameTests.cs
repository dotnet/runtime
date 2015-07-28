// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public static class NameTests
    {
        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestEncode()
        {
            byte[] expectedEncoding = "300e310c300a06035504031303466f6f".HexToByteArray();

            X500DistinguishedName n = new X500DistinguishedName("CN=Foo");
            String s = n.Name;
            Assert.Equal("CN=Foo", s);
            byte[] rawData = n.RawData;
            Assert.Equal(expectedEncoding, rawData);
        }

        [Fact]
        public static void TestDecode()
        {
            byte[] encoding = "300e310c300a06035504031303466f6f".HexToByteArray();

            X500DistinguishedName n = new X500DistinguishedName(encoding);
            String s = n.Name;
            Assert.Equal("CN=Foo", s);
            byte[] rawData = n.RawData;
            Assert.Equal(encoding, rawData);
        }

        [Fact]
        [ActiveIssue(1993, PlatformID.AnyUnix)]
        public static void TestFormat()
        {
            byte[] encoding = "300e310c300a06035504031303466f6f".HexToByteArray();
            String s;

            X500DistinguishedName n = new X500DistinguishedName(encoding);

            s = n.Format(multiLine: false);
            Assert.Equal("CN=Foo", s);

            s = n.Format(multiLine: true);
            Assert.Equal("CN=Foo\r\n", s);
        }
    }
}
