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
        public static void Find_Success_Simple()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 0..44,
                expectedBase64: 21..25,
                expectedLabel: 11..15);
            Assert.Equal("TEST", content[fields.Label]);
            Assert.Equal(content, content[fields.Location]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
            Assert.Equal(3, fields.DecodedDataLength);
        }

        [Fact]
        public static void Find_Success_IncompletePreebPrefixed()
        {
            string content = "-----BEGIN FAIL -----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            AssertPemFound(content,
                expectedLocation: 16..60,
                expectedBase64: 37..41,
                expectedLabel: 27..31);
        }

        [Fact]
        public static void Find_Success_CompletePreebPrefixedDifferentLabel()
        {
            string content = "-----BEGIN FAIL----- -----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 21..65,
                expectedBase64: 42..46,
                expectedLabel: 32..36);

            Assert.Equal("TEST", content[fields.Label]);
        }

        [Fact]
        public static void Find_Success_CompletePreebPrefixedSameLabel()
        {
            string content = "-----BEGIN TEST----- -----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 21..65,
                expectedBase64: 42..46,
                expectedLabel: 32..36);

            Assert.Equal("TEST", content[fields.Label]);
        }

        [Fact]
        public static void Find_Success_PreebEndingOverlap()
        {
            string content = "-----BEGIN TEST -----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 16..60,
                expectedBase64: 37..41,
                expectedLabel: 27..31);

            Assert.Equal("TEST", content[fields.Label]);
            Assert.Equal(3, fields.DecodedDataLength);
        }

        [Fact]
        public static void Find_Success_LargeLabel()
        {
            string label = new string('A', 275);
            string content = $"-----BEGIN {label}-----\nZm9v\n-----END {label}-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 0..587,
                expectedBase64: 292..296,
                expectedLabel: 11..286);

            Assert.Equal(label, content[fields.Label]);
        }

        [Fact]
        public static void Find_Success_Minimum()
        {
            string content = "-----BEGIN ----------END -----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 0..30,
                expectedBase64: 16..16,
                expectedLabel: 11..11);
            Assert.Equal(0, fields.DecodedDataLength);
        }

        [Fact]
        public static void Find_Success_PrecedingContentAndWhitespaceBeforePreeb()
        {
            string content = "boop   -----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            AssertPemFound(content,
                expectedLocation: 7..51,
                expectedBase64: 28..32,
                expectedLabel: 18..22);
        }

        [Fact]
        public static void Find_Success_TrailingWhitespaceAfterPosteb()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END TEST-----    ";
            AssertPemFound(content,
                expectedLocation: 0..44,
                expectedBase64: 21..25,
                expectedLabel: 11..15);
        }

        [Fact]
        public static void Find_Success_EmptyLabel()
        {
            string content = "-----BEGIN -----\nZm9v\n-----END -----";
            AssertPemFound(content,
                expectedLocation: 0..36,
                expectedBase64: 17..21,
                expectedLabel: 11..11);
        }

        [Fact]
        public static void Find_Success_EmptyContent_OneLine()
        {
            string content = "-----BEGIN EMPTY----------END EMPTY-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 0..40,
                expectedBase64: 21..21,
                expectedLabel: 11..16);
            Assert.Equal(0, fields.DecodedDataLength);
        }

        [Fact]
        public static void Find_Success_EmptyContent_ManyLinesOfWhitespace()
        {
            string content = "-----BEGIN EMPTY-----\n\t\n\t\n\t  \n-----END EMPTY-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 0..49,
                expectedBase64: 30..30,
                expectedLabel: 11..16);
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

        [Theory]
        [InlineData("H E L L O")]
        [InlineData("H-E-L-L-O")]
        [InlineData("H-E-L-L-O ")]
        [InlineData("HEL-LO")]
        public static void TryFind_True_LabelsWithHyphenSpace(string label)
        {
            string content = $"-----BEGIN {label}-----\nZm9v\n-----END {label}-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal(label, content[fields.Label]);
        }

        [Fact]
        public static void TryFind_True_LabelCharacterBoundaries()
        {
            string content = $"-----BEGIN !PANIC~~~-----\nAHHH\n-----END !PANIC~~~-----";
            Assert.True(PemEncoding.TryFind(content, out PemFields fields));
            Assert.Equal("!PANIC~~~", content[fields.Label]);
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
        [InlineData("te\x7fst")]
        [InlineData("te\x19st")]
        [InlineData("te  st")] //two spaces
        [InlineData("te- st")]
        public static void Find_Fail_InvalidLabel(string label)
        {
            string content = $"-----BEGIN {label}-----\nZm9v\n-----END {label}-----";
            AssertNoPemFound(content);
        }

        [Fact]
        public static void Find_Fail_InvalidBase64()
        {
            string content = "-----BEGIN TEST-----\n$$$$\n-----END TEST-----";
            AssertNoPemFound(content);
        }

        [Fact]
        public static void Find_Fail_PrecedingLinesAndSignificantCharsBeforePreeb()
        {
            string content = "boop\nbeep-----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            AssertNoPemFound(content);
        }

        [Fact]
        public static void Find_Fail_ContentOnPostEbLine()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END TEST-----boop";
            AssertNoPemFound(content);
        }

        [Fact]
        public static void Find_Fail_MismatchedLabels()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END FAIL-----";
            AssertNoPemFound(content);
        }

        [Fact]
        public static void Find_Fail_NoPostEncapBoundary()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n";
            AssertNoPemFound(content);
        }

        [Fact]
        public static void Find_Fail_IncompletePostEncapBoundary()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END TEST";
            AssertNoPemFound(content);
        }

        [Fact]
        public static void GetEncodedSize_Empty()
        {
            int size = PemEncoding.GetEncodedSize(labelLength: 0, dataLength: 0);
            Assert.Equal(31, size);
        }

        [Theory]
        [InlineData(1, 0, 33)]
        [InlineData(1, 1, 38)]
        [InlineData(16, 2048, 2838)]
        public static void GetEncodedSize_Simple(int labelLength, int dataLength, int expectedSize)
        {
            int size = PemEncoding.GetEncodedSize(labelLength, dataLength);
            Assert.Equal(expectedSize, size);
        }

        [Theory]
        [InlineData(1_073_741_808, 0, int.MaxValue)]
        [InlineData(1_073_741_805, 1, int.MaxValue - 1)]
        [InlineData(0, 1_585_834_053, int.MaxValue - 2)]
        [InlineData(1, 1_585_834_053, int.MaxValue)]
        public static void GetEncodedSize_Boundaries(int labelLength, int dataLength, int expectedSize)
        {
            int size = PemEncoding.GetEncodedSize(labelLength, dataLength);
            Assert.Equal(expectedSize, size);
        }

        [Fact]
        public static void GetEncodedSize_LabelLength_Overflow()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("labelLength",
                () => PemEncoding.GetEncodedSize(labelLength: 1_073_741_809, dataLength: 0));
        }

        [Fact]
        public static void GetEncodedSize_DataLength_Overflow()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dataLength",
                () => PemEncoding.GetEncodedSize(labelLength: 0, dataLength: 1_585_834_054));
        }

        [Fact]
        public static void GetEncodedSize_Combined_Overflow()
        {
            Assert.Throws<ArgumentException>(
                () => PemEncoding.GetEncodedSize(labelLength: 2, dataLength: 1_585_834_052));
        }

        [Fact]
        public static void GetEncodedSize_DataLength_Negative()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dataLength",
                () => PemEncoding.GetEncodedSize(labelLength: 0, dataLength: -1));
        }

        [Fact]
        public static void GetEncodedSize_LabelLength_Negative()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("labelLength",
                () => PemEncoding.GetEncodedSize(labelLength: -1, dataLength: 0));
        }

        [Fact]
        public static void TryWrite_Simple()
        {
            char[] buffer = new char[1000];
            string label = "HELLO";
            byte[] content = new byte[] { 0x66, 0x6F, 0x6F };
            Assert.True(PemEncoding.TryWrite(label, content, buffer, out int charsWritten));
            string pem = new string(buffer, 0, charsWritten);
            Assert.Equal("-----BEGIN HELLO-----\nZm9v\n-----END HELLO-----", pem);
        }

        [Fact]
        public static void TryWrite_Empty()
        {
            char[] buffer = new char[31];
            Assert.True(PemEncoding.TryWrite(default, default, buffer, out int charsWritten));
            string pem = new string(buffer, 0, charsWritten);
            Assert.Equal("-----BEGIN -----\n-----END -----", pem);
        }

        [Fact]
        public static void TryWrite_BufferTooSmall()
        {
            char[] buffer = new char[30];
            Assert.False(PemEncoding.TryWrite(default, default, buffer, out _));
        }

        [Fact]
        public static void TryWrite_ExactLineNoPadding()
        {
            char[] buffer = new char[1000];
            ReadOnlySpan<byte> data = new byte[] {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7
            };
            string label = "FANCY DATA";
            Assert.True(PemEncoding.TryWrite(label, data, buffer, out int charsWritten));
            string pem = new string(buffer, 0, charsWritten);
            string expected =
                "-----BEGIN FANCY DATA-----\n" +
                "AAECAwQFBgcICQABAgMEBQYHCAkAAQIDBAUGBwgJAAECAwQFBgcICQABAgMEBQYH\n" +
                "-----END FANCY DATA-----";
            Assert.Equal(expected, pem);
        }

        [Fact]
        public static void TryWrite_DoesNotWriteOutsideBounds()
        {
            Span<char> buffer = new char[1000];
            buffer.Fill('!');
            ReadOnlySpan<byte> data = new byte[] {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7
            };

            Span<char> write = buffer[10..];
            string label = "FANCY DATA";
            Assert.True(PemEncoding.TryWrite(label, data, write, out int charsWritten));
            string pem = new string(buffer[..(charsWritten + 20)]);
            string expected =
                "!!!!!!!!!!-----BEGIN FANCY DATA-----\n" +
                "AAECAwQFBgcICQABAgMEBQYHCAkAAQIDBAUGBwgJAAECAwQFBgcICQABAgMEBQYH\n" +
                "-----END FANCY DATA-----!!!!!!!!!!";
            Assert.Equal(expected, pem);
        }

        [Fact]
        public static void TryWrite_WrapPadding()
        {
            char[] buffer = new char[1000];
            ReadOnlySpan<byte> data = new byte[] {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9
            };
            string label = "UNFANCY DATA";
            Assert.True(PemEncoding.TryWrite(label, data, buffer, out int charsWritten));
            string pem = new string(buffer, 0, charsWritten);
            string expected =
                "-----BEGIN UNFANCY DATA-----\n" +
                "AAECAwQFBgcICQABAgMEBQYHCAkAAQIDBAUGBwgJAAECAwQFBgcICQABAgMEBQYH\n" +
                "CAk=\n" +
                "-----END UNFANCY DATA-----";
            Assert.Equal(expected, pem);
        }

        [Fact]
        public static void TryWrite_EcKey()
        {
            char[] buffer = new char[1000];
            ReadOnlySpan<byte> data = new byte[] {
                0x30, 0x74, 0x02, 0x01, 0x01, 0x04, 0x20, 0x20,
                0x59, 0xef, 0xff, 0x13, 0xd4, 0x92, 0xf6, 0x6a,
                0x6b, 0xcd, 0x07, 0xf4, 0x12, 0x86, 0x08, 0x6d,
                0x81, 0x93, 0xed, 0x9c, 0xf0, 0xf8, 0x5b, 0xeb,
                0x00, 0x70, 0x7c, 0x40, 0xfa, 0x12, 0x6c, 0xa0,
                0x07, 0x06, 0x05, 0x2b, 0x81, 0x04, 0x00, 0x0a,
                0xa1, 0x44, 0x03, 0x42, 0x00, 0x04, 0xdf, 0x23,
                0x42, 0xe5, 0xab, 0x3c, 0x25, 0x53, 0x79, 0x32,
                0x31, 0x7d, 0xe6, 0x87, 0xcd, 0x4a, 0x04, 0x41,
                0x55, 0x78, 0xdf, 0xd0, 0x22, 0xad, 0x60, 0x44,
                0x96, 0x7c, 0xf9, 0xe6, 0xbd, 0x3d, 0xe7, 0xf9,
                0xc3, 0x0c, 0x25, 0x40, 0x7d, 0x95, 0x42, 0x5f,
                0x76, 0x41, 0x4d, 0x81, 0xa4, 0x81, 0xec, 0x99,
                0x41, 0xfa, 0x4a, 0xd9, 0x55, 0x55, 0x7c, 0x4f,
                0xb1, 0xd9, 0x41, 0x75, 0x43, 0x44
            };
            string label = "EC PRIVATE KEY";
            Assert.True(PemEncoding.TryWrite(label, data, buffer, out int charsWritten));
            string pem = new string(buffer, 0, charsWritten);
            string expected =
                "-----BEGIN EC PRIVATE KEY-----\n" +
                "MHQCAQEEICBZ7/8T1JL2amvNB/QShghtgZPtnPD4W+sAcHxA+hJsoAcGBSuBBAAK\n" +
                "oUQDQgAE3yNC5as8JVN5MjF95ofNSgRBVXjf0CKtYESWfPnmvT3n+cMMJUB9lUJf\n" +
                "dkFNgaSB7JlB+krZVVV8T7HZQXVDRA==\n" +
                "-----END EC PRIVATE KEY-----";
            Assert.Equal(expected, pem);
        }

        [Fact]
        public static void TryWrite_Throws_InvalidLabel()
        {
            char[] buffer = new char[50];
            AssertExtensions.Throws<ArgumentException>("label", () =>
                PemEncoding.TryWrite("\n", default, buffer, out _));
        }

        [Fact]
        public static void Write_Empty()
        {
            char[] result = PemEncoding.Write(default, default);
            Assert.Equal("-----BEGIN -----\n-----END -----", result);
        }

        [Fact]
        public static void Write_Simple()
        {
            ReadOnlySpan<byte> data = new byte[] {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                0, 1, 2, 3, 4, 5, 6, 7
            };
            string label = "FANCY DATA";
            char[] result = PemEncoding.Write(label, data);
            string expected =
                "-----BEGIN FANCY DATA-----\n" +
                "AAECAwQFBgcICQABAgMEBQYHCAkAAQIDBAUGBwgJAAECAwQFBgcICQABAgMEBQYH\n" +
                "-----END FANCY DATA-----";
            Assert.Equal(expected, result);
        }

        private static PemFields AssertPemFound(
            ReadOnlySpan<char> input,
            Range expectedLocation,
            Range expectedBase64,
            Range expectedLabel)
        {
            bool tryFind = PemEncoding.TryFind(input, out PemFields tryFields);
            Assert.True(tryFind, "TryFind did not succeed but was expected to");

            PemFields fields = PemEncoding.Find(input);
            Assert.Equal(fields.Base64Data, tryFields.Base64Data);
            Assert.Equal(fields.Location, tryFields.Location);
            Assert.Equal(fields.Label, tryFields.Label);
            Assert.Equal(fields.DecodedDataLength, tryFields.DecodedDataLength);

            return fields;
        }

        private static void AssertNoPemFound(ReadOnlySpan<char> input)
        {
            bool tryFind = PemEncoding.TryFind(input, out _);
            Assert.False(tryFind, "TryFind did succeed but was not expected to");

            //Can't use AssertExtensions because it requires capturing a ref struct
            try
            {
                PemEncoding.Find(input);
            }
            catch (ArgumentException ae)
            {
                Assert.Equal("pemData", ae.ParamName);
                // Pass
            }
        }
    }
}
