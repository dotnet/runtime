// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public abstract class PemEncodingFindTests
    {
        [Fact]
        public void Find_Success_Simple()
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
        public void Find_Success_IncompletePreebPrefixed()
        {
            string content = "-----BEGIN FAIL -----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            AssertPemFound(content,
                expectedLocation: 16..60,
                expectedBase64: 37..41,
                expectedLabel: 27..31);
        }

        [Fact]
        public void Find_Success_CompletePreebPrefixedDifferentLabel()
        {
            string content = "-----BEGIN FAIL----- -----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 21..65,
                expectedBase64: 42..46,
                expectedLabel: 32..36);

            Assert.Equal("TEST", content[fields.Label]);
        }

        [Fact]
        public void Find_Success_CompletePreebPrefixedSameLabel()
        {
            string content = "-----BEGIN TEST----- -----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 21..65,
                expectedBase64: 42..46,
                expectedLabel: 32..36);

            Assert.Equal("TEST", content[fields.Label]);
        }

        [Fact]
        public void Find_Success_PreebEndingOverlap()
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
        public void Find_Success_LargeLabel()
        {
            string label = new string('A', 275);
            string content = $"-----BEGIN {label}-----\nZm9v\n-----END {label}-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 0..586,
                expectedBase64: 292..296,
                expectedLabel: 11..286);

            Assert.Equal(label, content[fields.Label]);
        }

        [Fact]
        public void Find_Success_Minimum()
        {
            string content = "-----BEGIN ----------END -----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 0..30,
                expectedBase64: 16..16,
                expectedLabel: 11..11);
            Assert.Equal(0, fields.DecodedDataLength);
        }

        [Fact]
        public void Find_Success_PrecedingContentAndWhitespaceBeforePreeb()
        {
            string content = "boop   -----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            AssertPemFound(content,
                expectedLocation: 7..51,
                expectedBase64: 28..32,
                expectedLabel: 18..22);
        }

        [Fact]
        public void Find_Success_TrailingWhitespaceAfterPosteb()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END TEST-----    ";
            AssertPemFound(content,
                expectedLocation: 0..44,
                expectedBase64: 21..25,
                expectedLabel: 11..15);
        }

        [Fact]
        public void Find_Success_EmptyLabel()
        {
            string content = "-----BEGIN -----\nZm9v\n-----END -----";
            AssertPemFound(content,
                expectedLocation: 0..36,
                expectedBase64: 17..21,
                expectedLabel: 11..11);
        }

        [Fact]
        public void Find_Success_EmptyContent_OneLine()
        {
            string content = "-----BEGIN EMPTY----------END EMPTY-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 0..40,
                expectedBase64: 21..21,
                expectedLabel: 11..16);
            Assert.Equal(0, fields.DecodedDataLength);
        }

        [Fact]
        public void Find_Success_EmptyContent_ManyLinesOfWhitespace()
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
        public void Find_Success_CommonLabels(string label)
        {
            string content = $"-----BEGIN {label}-----\nZm9v\n-----END {label}-----";
            PemFields fields = FindPem(content);
            Assert.Equal(label, content[fields.Label]);
        }

        [Theory]
        [InlineData("H E L L O")]
        [InlineData("H-E-L-L-O")]
        [InlineData("HEL-LO")]
        public void Find_Success_LabelsWithHyphenSpace(string label)
        {
            string content = $"-----BEGIN {label}-----\nZm9v\n-----END {label}-----";
            PemFields fields = FindPem(content);
            Assert.Equal(label, content[fields.Label]);
        }

        [Fact]
        public void Find_Success_SingleLetterLabel()
        {
            string content = "-----BEGIN H-----\nZm9v\n-----END H-----";
            AssertPemFound(content,
                expectedLocation: 0..38,
                expectedBase64: 18..22,
                expectedLabel: 11..12);
        }

        [Fact]
        public void Find_Success_LabelCharacterBoundaries()
        {
            string content = $"-----BEGIN !PANIC~~~-----\nAHHH\n-----END !PANIC~~~-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 0..54,
                expectedBase64: 26..30,
                expectedLabel: 11..20);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\n")]
        [InlineData("\r")]
        [InlineData("\t")]
        public void Find_Success_WhiteSpaceBeforePreebSeparatesFromPriorContent(string whiteSpace)
        {
            string content = $"blah{whiteSpace}-----BEGIN TEST-----\nZn9v\n-----END TEST-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 5..49,
                expectedBase64: 26..30,
                expectedLabel: 16..20);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("\n")]
        [InlineData("\r")]
        [InlineData("\t")]
        public void Find_Success_WhiteSpaceAfterPpostebSeparatesFromSubsequentContent(string whiteSpace)
        {
            string content = $"-----BEGIN TEST-----\nZn9v\n-----END TEST-----{whiteSpace}blah";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 0..44,
                expectedBase64: 21..25,
                expectedLabel: 11..15);
        }

        [Fact]
        public void Find_Success_Base64SurroundingWhiteSpaceStripped()
        {
            string content = $"-----BEGIN A-----\r\n Zm9v\n\r \t-----END A-----";
            PemFields fields = AssertPemFound(content,
                expectedLocation: 0..43,
                expectedBase64: 20..24,
                expectedLabel: 11..12);
        }

        [Fact]
        public void Find_Success_FindsPemAfterPemWithInvalidBase64()
        {
            string content = @"
-----BEGIN TEST-----
$$$$
-----END TEST-----
-----BEGIN TEST2-----
Zm9v
-----END TEST2-----";
            PemFields fields = FindPem(content);
            Assert.Equal("TEST2", content[fields.Label]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
        }

        [Fact]
        public void Find_Success_FindsPemAfterPemWithInvalidLabel()
        {
            string content = @"
-----BEGIN ------
YmFy
-----END ------
-----BEGIN TEST2-----
Zm9v
-----END TEST2-----";

            PemFields fields = FindPem(content);
            Assert.Equal("TEST2", content[fields.Label]);
            Assert.Equal("Zm9v", content[fields.Base64Data]);
        }

        [Fact]
        public void TryFind_Success_AfterSuccessiveInvalidBase64()
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < 100; i++)
            {
                builder.Append($"-----BEGIN CERTIFICATE-----\n${i:000}\n-----END CERTIFICATE-----\n");
            }

            builder.Append($"-----BEGIN CERTIFICATE-----\nZm9v\n-----END CERTIFICATE-----");

            AssertPemFound(builder.ToString(),
                expectedLocation: 5900..5958,
                expectedBase64: 5928..5932,
                expectedLabel: 5911..5922);
        }

        [Fact]
        public void Find_Fail_Empty()
        {
            AssertNoPemFound(string.Empty);
        }

        [Fact]
        public void Find_Fail_InvalidBase64_MultipleInvalid_WithSurroundingText()
        {
            string content = @"
CN=Intermediate1
-----BEGIN CERTIFICATE-----
MII
-----END CERTIFICATE-----
CN=Intermediate2
-----BEGIN CERTIFICATE-----
MII
-----END CERTIFICATE-----
";
            AssertNoPemFound(content);
        }

        [Fact]
        public void Find_Fail_PostEbBeforePreEb()
        {
            string content = "-----END TEST-----\n-----BEGIN TEST-----\nZm9v";
            AssertNoPemFound(content);
        }

        [Theory]
        [InlineData("\tOOPS")]
        [InlineData(" OOPS")]
        [InlineData(" ")]
        [InlineData("-")]
        [InlineData("-OOPS")]
        [InlineData("te\x7fst")]
        [InlineData("te\x19st")]
        [InlineData("te  st")] //two spaces
        [InlineData("te- st")]
        [InlineData("test ")] //last is space, must be labelchar
        [InlineData("test-")] //last is hyphen, must be labelchar
        public void Find_Fail_InvalidLabel(string label)
        {
            string content = $"-----BEGIN {label}-----\nZm9v\n-----END {label}-----";
            AssertNoPemFound(content);
        }

        [Fact]
        public void Find_Fail_InvalidBase64()
        {
            string content = "-----BEGIN TEST-----\n$$$$\n-----END TEST-----";
            AssertNoPemFound(content);
        }

        [Fact]
        public void Find_Fail_PrecedingLinesAndSignificantCharsBeforePreeb()
        {
            string content = "boop\nbeep-----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            AssertNoPemFound(content);
        }


        [Theory]
        [InlineData("\u200A")] // hair space
        [InlineData("\v")]
        [InlineData("\f")]
        public void Find_Fail_NotPermittedWhiteSpaceSeparatorsForPreeb(string whiteSpace)
        {
            string content = $"boop{whiteSpace}-----BEGIN TEST-----\nZm9v\n-----END TEST-----";
            AssertNoPemFound(content);
        }

        [Theory]
        [InlineData("\u200A")] // hair space
        [InlineData("\v")]
        [InlineData("\f")]
        public void Find_Fail_NotPermittedWhiteSpaceSeparatorsForPosteb(string whiteSpace)
        {
            string content = $"-----BEGIN TEST-----\nZm9v\n-----END TEST-----{whiteSpace}boop";
            AssertNoPemFound(content);
        }

        [Fact]
        public void Find_Fail_ContentOnPostEbLine()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END TEST-----boop";
            AssertNoPemFound(content);
        }

        [Fact]
        public void Find_Fail_MismatchedLabels()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END FAIL-----";
            AssertNoPemFound(content);
        }

        [Fact]
        public void Find_Fail_NoPostEncapBoundary()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n";
            AssertNoPemFound(content);
        }

        [Fact]
        public void Find_Fail_IncompletePostEncapBoundary()
        {
            string content = "-----BEGIN TEST-----\nZm9v\n-----END TEST";
            AssertNoPemFound(content);
        }

        [Fact]
        public void Find_Fail_InvalidBase64_Size()
        {
            string content = "-----BEGIN TEST-----\nZ\n-----END TEST-----";
            AssertNoPemFound(content);
        }

        [Fact]
        public void Find_Fail_InvalidBase64_ExtraPadding()
        {
            string content = "-----BEGIN TEST-----\nZm9v====\n-----END TEST-----";
            AssertNoPemFound(content);
        }

        [Fact]
        public void Find_Fail_InvalidBase64_MissingPadding()
        {
            string content = "-----BEGIN TEST-----\nZm8\n-----END TEST-----";
            AssertNoPemFound(content);
        }

        [Theory]
        [InlineData("", 0)]
        [InlineData("cA==", 1)]
        [InlineData("cGU=", 2)]
        [InlineData("cGVu", 3)]
        [InlineData("cGVubg==", 4)]
        [InlineData("cGVubnk=", 5)]
        [InlineData("cGVubnkh", 6)]
        [InlineData("c G V u b n k h", 6)]
        public void Find_Success_DecodeSize(string base64, int expectedSize)
        {
            string content = $"-----BEGIN TEST-----\n{base64}\n-----END TEST-----";
            PemFields fields = FindPem(content);
            Assert.Equal(expectedSize, fields.DecodedDataLength);
            Assert.Equal(base64, content[fields.Base64Data]);
        }

        private PemFields AssertPemFound(
            ReadOnlySpan<char> input,
            Range expectedLocation,
            Range expectedBase64,
            Range expectedLabel)
        {
            PemFields fields = FindPem(input);
            Assert.Equal(expectedBase64, fields.Base64Data);
            Assert.Equal(expectedLocation, fields.Location);
            Assert.Equal(expectedLabel, fields.Label);

            return fields;
        }

        protected abstract void AssertNoPemFound(ReadOnlySpan<char> input);

        protected abstract PemFields FindPem(ReadOnlySpan<char> input);
    }

    public class PemEncodingFindThrowingTests : PemEncodingFindTests
    {
        protected override PemFields FindPem(ReadOnlySpan<char> input) => PemEncoding.Find(input);

        protected override void AssertNoPemFound(ReadOnlySpan<char> input)
        {
            AssertExtensions.Throws<ArgumentException, char>("pemData", input, x => PemEncoding.Find(x));
        }
    }

    public class PemEncodingFindTryTests : PemEncodingFindTests
    {
        protected override PemFields FindPem(ReadOnlySpan<char> input)
        {
            bool found = PemEncoding.TryFind(input, out PemFields fields);
            Assert.True(found, "Did not find PEM.");
            return fields;
        }

        protected override void AssertNoPemFound(ReadOnlySpan<char> input)
        {
            bool found = PemEncoding.TryFind(input, out _);
            Assert.False(found, "Found PEM when not expected");
        }
    }
}
