// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class MediaTypeHeaderParserTest
    {
        [Fact]
        public void Properties_ReadValues_MatchExpectation()
        {
            MediaTypeHeaderParser parser = MediaTypeHeaderParser.SingleValueParser;
            Assert.False(parser.SupportsMultipleValues, "SupportsMultipleValues");
            Assert.Null(parser.Comparer);

            parser = MediaTypeHeaderParser.MultipleValuesParser;
            Assert.True(parser.SupportsMultipleValues, "SupportsMultipleValues");
            Assert.Null(parser.Comparer);
        }

        [Fact]
        public void Parse_ValidValue_ReturnsMediaTypeHeaderValue()
        {
            // This test verifies that Parse() correctly calls TryParse().
            MediaTypeHeaderParser parser = MediaTypeHeaderParser.SingleValueParser;
            int index = 2;

            MediaTypeHeaderValue expected = new MediaTypeHeaderValue("text/plain");
            expected.CharSet = "utf-8";
            Assert.True(expected.Equals(parser.ParseValue("   text / plain ; charset = utf-8 ", null, ref index)));
            Assert.Equal(34, index);
        }

        [Fact]
        public void Parse_InvalidValue_Throw()
        {
            // This test verifies that Parse() correctly calls TryParse().
            MediaTypeHeaderParser parser = MediaTypeHeaderParser.SingleValueParser;
            int index = 0;

            // only one value allowed.
            Assert.Throws<FormatException>(() => { parser.ParseValue("text/plain; charset=utf-8, next/mediatype", null, ref index); });
        }

        [Fact]
        public void Parse_NullValue_Throw()
        {
            MediaTypeHeaderParser parser = MediaTypeHeaderParser.SingleValueParser;
            int index = 0;

            Assert.Throws<FormatException>(() => { parser.ParseValue(null, null, ref index); });
        }

        [Fact]
        public void TryParse_SetOfValidValueStringsForMediaType_ParsedCorrectly()
        {
            MediaTypeHeaderValue expected = new MediaTypeHeaderValue("text/plain");
            CheckValidParsedValue("text/plain", 0, expected, 10, false);

            // We don't have to test all possible input strings, since most of the pieces are handled by other parsers.
            // The purpose of this test is to verify that these other parsers are combined correctly to build a
            // media-type parser.
            expected.CharSet = "utf-8";
            CheckValidParsedValue("  text/plain;charset=utf-8", 2, expected, 26, false);
        }

        [Fact]
        public void TryParse_SetOfValidValueStringsForMediaTypeWithQuality_ParsedCorrectly()
        {
            MediaTypeWithQualityHeaderValue expected = new MediaTypeWithQualityHeaderValue("text/plain");
            CheckValidParsedValue("text/plain", 0, expected, 10, true);
            CheckValidParsedValue("text/plain,", 0, expected, 11, true);
            CheckValidParsedValue("text/plain , ", 0, expected, 13, true);
            CheckValidParsedValue("text/plain, next/mediatype", 0, expected, 12, true);
            CheckValidParsedValue("  ", 0, null, 2, true);
            CheckValidParsedValue("", 0, null, 0, true);
            CheckValidParsedValue(null, 0, null, 0, true);
            CheckValidParsedValue("  ,,", 0, null, 4, true);

            // Note that even if the whole string is invalid, the first media-type value is valid. When the parser
            // gets called again using the result-index (13), then it fails: I.e. we have 1 valid media-type and an
            // invalid one.
            CheckValidParsedValue("text/plain , invalid", 0, expected, 13, true);
            CheckValidParsedValue("text/plain , \r\n", 0, expected, 13, true);

            // We don't have to test all possible input strings, since most of the pieces are handled by other parsers.
            // The purpose of this test is to verify that these other parsers are combined correctly to build a
            // media-type parser.
            expected.CharSet = "utf-8";
            CheckValidParsedValue("  text/plain;charset=utf-8", 2, expected, 26, true);
            CheckValidParsedValue("  text/plain;charset=utf-8, ", 1, expected, 28, true);
            CheckValidParsedValue("  text/plain;charset=utf-8, next/mediatype", 2, expected, 28, true);
        }

        [Fact]
        public void TryParse_SetOfValidMultiValueStrings_ParsedCorrectly()
        {
            // Values that are valid for multi, but invalid for single parser
            MediaTypeWithQualityHeaderValue expected = null;
            Check("");
            Check("  ");
            Check(null);

            expected = new MediaTypeWithQualityHeaderValue("text/plain");
            Check("text/plain ,");
            Check("text/plain,");

            expected.CharSet = "utf-8";
            Check("text/plain; charset=utf-8 ,");
            Check("text/plain; charset=utf-8,");

            void Check(string input)
            {
                CheckValidParsedValue(input, 0, expected, expectedIndex: input?.Length ?? 0, supportsMultipleValues: true);

                CheckInvalidParsedValue(input, 0, supportsMultipleValues: false);
            }
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalid("text/plain\u4F1A", 0);
            CheckInvalid("textplain", 0);
            CheckInvalid("text/", 0);
            CheckInvalid("\r\n text/plain  ", 0);
            CheckInvalid("\r\n text   /  plain ;  charset =   utf-8 ", 1);
            CheckInvalid("\r\n text/plain  , next/mediatype", 0);
            CheckInvalid(" \r text/plain", 0);
            CheckInvalid(" \n text/plain", 0);
            CheckInvalid(" \r\n text/plain", 0);
            CheckInvalid("text/plain , invalid", 12);
            CheckInvalid("text/plain , invalid", 13);
            CheckInvalid("text/plain , \r ", 12);
            CheckInvalid("text/plain , \n ", 12);
            CheckInvalid("text/plain , \r\n ", 12);
            CheckInvalid("text/plain , \r\n", 13);

            static void CheckInvalid(string input, int startIndex)
            {
                CheckInvalidParsedValue(input, startIndex, supportsMultipleValues: true);
                CheckInvalidParsedValue(input, startIndex, supportsMultipleValues: false);
            }
        }

        #region Helper methods

        private static void CheckValidParsedValue(string input, int startIndex, MediaTypeHeaderValue expectedResult,
            int expectedIndex, bool supportsMultipleValues)
        {
            MediaTypeHeaderParser parser = supportsMultipleValues ? MediaTypeHeaderParser.MultipleValuesParser : MediaTypeHeaderParser.SingleValueParser;

            Assert.True(parser.TryParseValue(input, null, ref startIndex, out object result),
                string.Format("TryParse returned false. Input: '{0}', Index: {1}", input, startIndex));
            Assert.Equal(expectedIndex, startIndex);
            Assert.Equal(expectedResult, result);
        }

        private static void CheckInvalidParsedValue(string input, int startIndex, bool supportsMultipleValues)
        {
            MediaTypeHeaderParser parser = supportsMultipleValues ? MediaTypeHeaderParser.MultipleValuesParser : MediaTypeHeaderParser.SingleValueParser;

            int newIndex = startIndex;
            Assert.False(parser.TryParseValue(input, null, ref newIndex, out object result),
                string.Format("TryParse returned true. Input: '{0}', Index: {1}", input, startIndex));
            Assert.Null(result);
            Assert.Equal(startIndex, newIndex);
        }
        #endregion
    }
}
