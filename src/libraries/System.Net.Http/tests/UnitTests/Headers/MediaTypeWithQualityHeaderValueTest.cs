// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class MediaTypeWithQualityHeaderValueTest
    {
        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            // This test just verifies that MediaTypeWithQualityHeaderValue calls the correct base implementation.
            MediaTypeWithQualityHeaderValue source = new MediaTypeWithQualityHeaderValue("application/xml");
            MediaTypeWithQualityHeaderValue clone = (MediaTypeWithQualityHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.MediaType, clone.MediaType);
            Assert.Equal(0, clone.Parameters.Count);

            source.CharSet = "utf-8";
            source.Quality = 0.1;
            clone = (MediaTypeWithQualityHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.MediaType, clone.MediaType);
            Assert.Equal("utf-8", clone.CharSet);
            Assert.Equal(0.1, clone.Quality);
            Assert.Equal(2, clone.Parameters.Count);

            source.Parameters.Add(new NameValueHeaderValue("custom", "customValue"));
            clone = (MediaTypeWithQualityHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.MediaType, clone.MediaType);
            Assert.Equal("utf-8", clone.CharSet);
            Assert.Equal(0.1, clone.Quality);
            Assert.Equal(3, clone.Parameters.Count);
            Assert.Equal("custom", clone.Parameters.ElementAt(2).Name);
            Assert.Equal("customValue", clone.Parameters.ElementAt(2).Value);
        }

        [Fact]
        public void Ctor_AddNameAndQuality_QualityParameterAdded()
        {
            MediaTypeWithQualityHeaderValue mediaType = new MediaTypeWithQualityHeaderValue("application/xml", 0.08);
            Assert.Equal(0.08, mediaType.Quality);
            Assert.Equal("application/xml", mediaType.MediaType);
            Assert.Equal(1, mediaType.Parameters.Count);
        }

        [Fact]
        public void Quality_SetCharSetAndValidateObject_ParametersEntryForCharSetAdded()
        {
            MediaTypeWithQualityHeaderValue mediaType = new MediaTypeWithQualityHeaderValue("text/plain");
            mediaType.Quality = 0.563156454;
            Assert.Equal(0.563, mediaType.Quality);
            Assert.Equal(1, mediaType.Parameters.Count);
            Assert.Equal("q", mediaType.Parameters.First().Name);
            Assert.Equal("0.563", mediaType.Parameters.First().Value);

            mediaType.Quality = null;
            Assert.Null(mediaType.Quality);
            Assert.Equal(0, mediaType.Parameters.Count);
            mediaType.Quality = null; // It's OK to set it again to null; no exception.
        }

        [Fact]
        public void Quality_AddQualityParameterThenUseProperty_ParametersEntryIsOverwritten()
        {
            MediaTypeWithQualityHeaderValue mediaType = new MediaTypeWithQualityHeaderValue("text/plain");

            NameValueHeaderValue quality = new NameValueHeaderValue("q", "0.132");
            mediaType.Parameters.Add(quality);
            Assert.Equal(1, mediaType.Parameters.Count);
            Assert.Equal("q", mediaType.Parameters.First().Name);
            Assert.Equal(0.132, mediaType.Quality);

            mediaType.Quality = 0.9;
            Assert.Equal(0.9, mediaType.Quality);
            Assert.Equal(1, mediaType.Parameters.Count);
            Assert.Equal("q", mediaType.Parameters.First().Name);

            mediaType.Parameters.Remove(quality);
            Assert.Null(mediaType.Quality);
        }

        [Fact]
        public void Quality_AddQualityParameterUpperCase_CaseInsensitiveComparison()
        {
            MediaTypeWithQualityHeaderValue mediaType = new MediaTypeWithQualityHeaderValue("text/plain");

            NameValueHeaderValue quality = new NameValueHeaderValue("Q", "0.132");
            mediaType.Parameters.Add(quality);
            Assert.Equal(1, mediaType.Parameters.Count);
            Assert.Equal("Q", mediaType.Parameters.First().Name);
            Assert.Equal(0.132, mediaType.Quality);
        }

        [Fact]
        public void Quality_LessThanZero_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                MediaTypeWithQualityHeaderValue mediaType = new MediaTypeWithQualityHeaderValue("application/xml", -0.01); });
        }

        [Fact]
        public void Quality_GreaterThanOne_Throw()
        {
            MediaTypeWithQualityHeaderValue mediaType = new MediaTypeWithQualityHeaderValue("application/xml");

            Assert.Throws<ArgumentOutOfRangeException>(() => { mediaType.Quality = 1.01; });
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            MediaTypeWithQualityHeaderValue expected = new MediaTypeWithQualityHeaderValue("text/plain");
            CheckValidParse("text/plain", expected);
            CheckValidParse(" text/plain  ", expected);

            // We don't have to test all possible input strings, since most of the pieces are handled by other parsers.
            // The purpose of this test is to verify that these other parsers are combined correctly to build a
            // media-type parser.
            expected.CharSet = "utf-8";
            CheckValidParse(" text   /  plain ;  charset =   utf-8 ", expected);
            CheckValidParse("  text/plain;charset=utf-8", expected);

            MediaTypeWithQualityHeaderValue value1 = new MediaTypeWithQualityHeaderValue("text/plain");
            value1.CharSet = "iso-8859-1";
            value1.Quality = 1.0;

            CheckValidParse("text/plain; charset=iso-8859-1; q=1.0", value1);

            MediaTypeWithQualityHeaderValue value2 = new MediaTypeWithQualityHeaderValue("*/xml");
            value2.CharSet = "utf-8";
            value2.Quality = 0.5;

            CheckValidParse(" */xml; charset=utf-8; q=0.5", value2);
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("");
            CheckInvalidParse("  ");
            CheckInvalidParse(null);
            CheckInvalidParse("text/plain\u4F1A");
            CheckInvalidParse("text/plain ,");
            CheckInvalidParse("text/plain,");
            CheckInvalidParse("text/plain; charset=utf-8 ,");
            CheckInvalidParse("text/plain; charset=utf-8,");
            CheckInvalidParse("textplain");
            CheckInvalidParse("text/");
            CheckInvalidParse(",, , ,,text/plain; charset=iso-8859-1; q=1.0, */xml; charset=utf-8; q=0.5,,,");
            CheckInvalidParse(",, , ,,text/plain; charset=iso-8859-1; q=1.0,\r\n */xml; charset=utf-8; q=0.5,,,");
            CheckInvalidParse("text/plain; charset=iso-8859-1; q=1.0, */xml; charset=utf-8; q=0.5");
            CheckInvalidParse(" , */xml; charset=utf-8; q=0.5 ");
            CheckInvalidParse("text/plain; charset=iso-8859-1; q=1.0 , ");
            CheckInvalidParse("\r\n */xml; charset=utf-8; q=0.5");
            CheckInvalidParse("text/plain\r");
            CheckInvalidParse("text/plain\n");
            CheckInvalidParse("text/plain\r\n");
            CheckInvalidParse("text/plain\r\n ");
            CheckInvalidParse("\r text/plain");
            CheckInvalidParse("\n text/plain");
            CheckInvalidParse("\ntext/plain");
            CheckInvalidParse("\r\n text/plain");
        }

        #region Helper methods

        private void CheckValidParse(string input, MediaTypeWithQualityHeaderValue expectedResult)
        {
            MediaTypeWithQualityHeaderValue result = MediaTypeWithQualityHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);

            result = null;
            Assert.True(MediaTypeWithQualityHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);

            // New lines are never allowed
            for (int i = 0; i < input.Length; i++)
            {
                CheckInvalidParse(input.Insert(i, "\r"));
                CheckInvalidParse(input.Insert(i, "\n"));
                CheckInvalidParse(input.Insert(i, "\r\n"));
                CheckInvalidParse(input.Insert(i, "\r\n "));
            }
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { MediaTypeWithQualityHeaderValue.Parse(input); });

            MediaTypeWithQualityHeaderValue result = null;
            Assert.False(MediaTypeWithQualityHeaderValue.TryParse(input, out result));
            Assert.Null(result);
        }
        #endregion
    }
}
