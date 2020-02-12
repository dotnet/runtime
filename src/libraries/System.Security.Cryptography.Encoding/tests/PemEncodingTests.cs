// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encoding.Tests
{
    public static class PemEncodingTests
    {
        [Fact]
        public static void Find_ThrowsWhenNoPem()
        {
            AssertExtensions.Throws<ArgumentException>("pemData",
                () => PemEncoding.Find(string.Empty));
        }

        [Fact]
        public static void Find_Simple()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            PemFields fields = PemEncoding.Find(content);
            Assert.Equal("TEST", content[fields.Label]);
            Assert.Equal(content, content[fields.Location]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
            Assert.Equal(3, fields.DecodedDataLength);
        }


        [Fact]
        public static void TryFind_True_Minimum()
        {
            string content = "-----BEGIN ----------END -----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal(string.Empty, content[fields.Label]);
            Assert.Equal(content, content[fields.Location]);
            Assert.Equal(string.Empty, content[fields.Base64Data]);
            Assert.Equal(0, fields.DecodedDataLength);
        }

        [Fact]
        public static void TryFind_True_Simple()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("TEST", content[fields.Label]);
            Assert.Equal(content, content[fields.Location]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
            Assert.Equal(3, fields.DecodedDataLength);
        }

        [Fact]
        public static void TryFind_True_WindowsStyleEol()
        {
            string content = "-----BEGIN TEST-----\r\nZm\r\n9v\r\n-----END TEST-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("TEST", content[fields.Label]);
            Assert.Equal(content, content[fields.Location]);
            Assert.Equal("Zm\r\n9v", content[fields.Base64Data]);
            Assert.Equal(3, fields.DecodedDataLength);
        }


        [Fact]
        public static void TryFind_True_EolMixed()
        {
            string content = "-----BEGIN TEST-----\rZm\r\n9v\n-----END TEST-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("TEST", content[fields.Label]);
            Assert.Equal(content, content[fields.Location]);
            Assert.Equal("Zm\r\n9v", content[fields.Base64Data]);
            Assert.Equal(3, fields.DecodedDataLength);
        }

        [Fact]
        public static void TryFind_True_OneLine()
        {
            string content = "-----BEGIN TEST-----Zm9v-----END TEST-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("TEST", content[fields.Label]);
            Assert.Equal(content, content[fields.Location]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
            Assert.Equal(3, fields.DecodedDataLength);
        }

        [Fact]
        public static void TryFind_True_MultiLineBase64()
        {
            string content = "-----BEGIN TEST-----\nZm\n9v\n-----END TEST-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("TEST", content[fields.Label]);
            Assert.Equal(content, content[fields.Location]);
            Assert.Equal("Zm\n9v", content[fields.Base64Data]);
            Assert.Equal(3, fields.DecodedDataLength);
        }

        [Fact]
        public static void TryFind_True_PrecedingLines()
        {
            string content = "boop\n-----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("TEST", content[fields.Label]);
            Assert.Equal(content["boop\n".Length..], content[fields.Location]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
            Assert.Equal(3, fields.DecodedDataLength);
        }

        [Fact]
        public static void TryFind_True_PrecedingLines_WindowsEolStyle()
        {
            string content = "boop\r\n-----BEGIN TEST-----\r\nZm9v\r\n-----END TEST-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("TEST", content[fields.Label]);
            Assert.Equal(content["boop\r\n".Length..], content[fields.Location]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
            Assert.Equal(3, fields.DecodedDataLength);
        }

        [Fact]
        public static void TryFind_True_PrecedingLines_ReturnLines()
        {
            string content = "boop\r-----BEGIN TEST-----\rZm9v\r-----END TEST-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("TEST", content[fields.Label]);
            Assert.Equal(content["boop\r".Length..], content[fields.Location]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
            Assert.Equal(3, fields.DecodedDataLength);
        }

        [Fact]
        public static void TryFind_True_PrecedingLinesAndWhitespaceBeforePreeb()
        {
            string content = "boop\n   -----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("TEST", content[fields.Label]);
            Assert.Equal(content["boop\n   ".Length..], content[fields.Location]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
            Assert.Equal(3, fields.DecodedDataLength);
        }

        [Fact]
        public static void TryFind_True_TrailingWhitespaceAfterPosteb()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END TEST-----    ";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("TEST", content[fields.Label]);
            Assert.Equal(content[..^"    ".Length], content[fields.Location]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
            Assert.Equal(3, fields.DecodedDataLength);
        }

        [Fact]
        public static void TryFind_True_EmptyLabel()
        {
            string content = "-----BEGIN -----\nZm9v\n-----END -----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal(11..11, fields.Label);
            Assert.Equal(content, content[fields.Location]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
            Assert.Equal(3, fields.DecodedDataLength);
        }

        [Fact]
        public static void TryFind_True_EmptyContent_OneLine()
        {
            string content = "-----BEGIN EMPTY----------END EMPTY-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("EMPTY", content[fields.Label]);
            Assert.Equal(content, content[fields.Location]);
            Assert.Equal(21..21, fields.Base64Data);
            Assert.Equal(0, fields.DecodedDataLength);
        }

        [Fact]
        public static void TryFind_True_EmptyContent_ManyLinesOfWhitespace()
        {
            string content = "-----BEGIN EMPTY-----\n\t\n\t\n\t  \n-----END EMPTY-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("EMPTY", content[fields.Label]);
            Assert.Equal(content, content[fields.Location]);
            Assert.Equal(30..30, fields.Base64Data);
            Assert.Equal(0, fields.DecodedDataLength);
        }

        [Theory]
        [InlineData("CERTIFICATE")]
        [InlineData("X509 CRL")]
        [InlineData("PKCS7")]
        [InlineData("PRIVATE KEY")]
        [InlineData("RSA PRIVATE KEY")]
        public static void TryFind_True_CommonLabels(string label)
        {
            string content = $"-----BEGIN {label}-----\nZm9v\n-----END {label}-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal(label, content[fields.Label]);
        }

        [Fact]
        public static void TryFind_True_MultiPem()
        {
            string content = @"
-----BEGIN EC PARAMETERS-----
BgUrgQQACg==
-----END EC PARAMETERS-----
-----BEGIN EC PRIVATE KEY-----
MHQCAQEEIIpP2qP/mGWDAojQDNrNfUHwYGNPKeO6VLt+POJeCJ3OoAcGBSuBBAAK
oUQDQgAEeDThNbdvTkptgvfNOpETlKBcWDUKs9IcQ/RaFeBntqt+6J875A79YhmD
D7ofwIDcVqzOJQDhSN54EQ17CFQiwg==
-----END EC PRIVATE KEY-----
";
            ReadOnlySpan<char> pem = content;
            List<string> labels = new List<string>();
            while (PemEncoding.TryFind(pem, out PemFields fields))
            {
                labels.Add(pem[fields.Label].ToString());
                pem = pem[fields.Location.End..];
            }

            Assert.Equal(new string[] { "EC PARAMETERS", "EC PRIVATE KEY" }, labels);
        }

        [Fact]
        public static void TryFind_True_FindsPemAfterPemWithInvalidBase64()
        {
            string content = @"
-----BEGIN TEST-----
$$$$
-----END TEST-----
-----BEGIN TEST2-----
Zm9v
-----END TEST2-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("TEST2", content[fields.Label]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
        }

        [Fact]
        public static void TryFind_True_FindsPemAfterPemWithInvalidLabel()
        {
            string content = @"
-----BEGIN ------
YmFy
-----END ------
-----BEGIN TEST2-----
Zm9v
-----END TEST2-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("TEST2", content[fields.Label]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
        }

        [Fact]
        public static void TryFind_False_Empty()
        {
            Assert.False(PemEncoding.TryFind(string.Empty, out _));
        }

        [Fact]
        public static void TryFind_False_PostEbBeforePreEb()
        {
            string content = "-----END TEST-----\n-----BEGIN TEST-----\nZm9v";
            Assert.False(PemEncoding.TryFind(content, out _));
        }

        [Theory]
        [InlineData("\tOOPS")]
        [InlineData(" OOPS")]
        [InlineData("-OOPS")]
        public static void TryFind_False_InvalidLabel(string label)
        {
            string content = $"-----BEGIN {label}-----\nZm9v\n-----END {label}-----";
            Assert.False(PemEncoding.TryFind(content, out _));
        }

        [Fact]
        public static void TryFind_False_InvalidBase64()
        {
            string content = "-----BEGIN TEST-----\n$$$$\n-----END TEST-----";
            Assert.False(PemEncoding.TryFind(content, out _));
        }

        [Fact]
        public static void TryFind_False_PrecedingLinesAndSignificantCharsBeforePreeb()
        {
            string content = "boop\nbeep-----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            Assert.False(PemEncoding.TryFind(content, out _));
        }

        [Fact]
        public static void TryFind_False_ContentOnPostEbLine()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END TEST-----boop";
            Assert.False(PemEncoding.TryFind(content, out _));
        }

        [Fact]
        public static void TryFind_False_NoPostEncapBoundary()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n";
            Assert.False(PemEncoding.TryFind(content, out _));
        }

        [Fact]
        public static void TryFind_False_IncompletePostEncapBoundary()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END TEST";
            Assert.False(PemEncoding.TryFind(content, out _));
        }
    }
}
