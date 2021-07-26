// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class MediaTypeHeaderValueTest
    {
        [Fact]
        public void Ctor_MediaTypeNull_Throw()
        {
            AssertExtensions.Throws<ArgumentException>("mediaType", () => { new MediaTypeHeaderValue(null); });
        }

        [Fact]
        public void Ctor_MediaTypeEmpty_Throw()
        {
            // null and empty should be treated the same. So we also throw for empty strings.
            AssertExtensions.Throws<ArgumentException>("mediaType", () => { new MediaTypeHeaderValue(string.Empty); });
        }

        [Fact]
        public void Ctor_MediaTypeInvalidFormat_ThrowFormatException()
        {
            // When adding values using strongly typed objects, no leading/trailing LWS (whitespace) are allowed.
            AssertFormatException(" text/plain ");
            AssertFormatException("text / plain");
            AssertFormatException("text/ plain");
            AssertFormatException("text /plain");
            AssertFormatException("text/plain ");
            AssertFormatException(" text/plain");
            AssertFormatException("te xt/plain");
            AssertFormatException("te=xt/plain");
            AssertFormatException("te\u00E4xt/plain");
            AssertFormatException("text/pl\u00E4in");
            AssertFormatException("text");
            AssertFormatException("\"text/plain\"");
            AssertFormatException("text/plain; charset=utf-8; ");
            AssertFormatException("text/plain;");
            AssertFormatException("text/plain;charset=utf-8"); // ctor takes only media-type name, no parameters
        }

        [Fact]
        public void Ctor_MediaTypeValidFormat_SuccessfullyCreated()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/plain");
            Assert.Equal("text/plain", mediaType.MediaType);
            Assert.Equal(0, mediaType.Parameters.Count);
            Assert.Null(mediaType.CharSet);
        }

        [Fact]
        public void Parameters_AddNull_Throw()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/plain");

            Assert.Throws<ArgumentNullException>(() => { mediaType.Parameters.Add(null); });
        }

        [Fact]
        public void MediaType_SetAndGetMediaType_MatchExpectations()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/plain");
            Assert.Equal("text/plain", mediaType.MediaType);

            mediaType.MediaType = "application/xml";
            Assert.Equal("application/xml", mediaType.MediaType);
        }

        [Fact]
        public void CharSet_SetCharSetAndValidateObject_ParametersEntryForCharSetAdded()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/plain");
            mediaType.CharSet = "mycharset";
            Assert.Equal("mycharset", mediaType.CharSet);
            Assert.Equal(1, mediaType.Parameters.Count);
            Assert.Equal("charset", mediaType.Parameters.First().Name);

            mediaType.CharSet = null;
            Assert.Null(mediaType.CharSet);
            Assert.Equal(0, mediaType.Parameters.Count);
            mediaType.CharSet = null; // It's OK to set it again to null; no exception.
        }

        [Fact]
        public void CharSet_AddCharSetParameterThenUseProperty_ParametersEntryIsOverwritten()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/plain");

            // Note that uppercase letters are used. Comparison should happen case-insensitive.
            NameValueHeaderValue charset = new NameValueHeaderValue("CHARSET", "old_charset");
            mediaType.Parameters.Add(charset);
            Assert.Equal(1, mediaType.Parameters.Count);
            Assert.Equal("CHARSET", mediaType.Parameters.First().Name);

            mediaType.CharSet = "new_charset";
            Assert.Equal("new_charset", mediaType.CharSet);
            Assert.Equal(1, mediaType.Parameters.Count);
            Assert.Equal("CHARSET", mediaType.Parameters.First().Name);

            mediaType.Parameters.Remove(charset);
            Assert.Null(mediaType.CharSet);
        }

        [Fact]
        public void ToString_UseDifferentMediaTypes_AllSerializedCorrectly()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/plain");
            Assert.Equal("text/plain", mediaType.ToString());

            mediaType.CharSet = "utf-8";
            Assert.Equal("text/plain; charset=utf-8", mediaType.ToString());

            mediaType.Parameters.Add(new NameValueHeaderValue("custom", "\"custom value\""));
            Assert.Equal("text/plain; charset=utf-8; custom=\"custom value\"", mediaType.ToString());

            mediaType.CharSet = null;
            Assert.Equal("text/plain; custom=\"custom value\"", mediaType.ToString());
        }

        [Fact]
        public void GetHashCode_UseMediaTypeWithAndWithoutParameters_SameOrDifferentHashCodes()
        {
            MediaTypeHeaderValue mediaType1 = new MediaTypeHeaderValue("text/plain");
            MediaTypeHeaderValue mediaType2 = new MediaTypeHeaderValue("text/plain");
            mediaType2.CharSet = "utf-8";
            MediaTypeHeaderValue mediaType3 = new MediaTypeHeaderValue("text/plain");
            mediaType3.Parameters.Add(new NameValueHeaderValue("name", "value"));
            MediaTypeHeaderValue mediaType4 = new MediaTypeHeaderValue("TEXT/plain");
            MediaTypeHeaderValue mediaType5 = new MediaTypeHeaderValue("TEXT/plain");
            mediaType5.Parameters.Add(new NameValueHeaderValue("CHARSET", "UTF-8"));

            Assert.NotEqual(mediaType1.GetHashCode(), mediaType2.GetHashCode());
            Assert.NotEqual(mediaType1.GetHashCode(), mediaType3.GetHashCode());
            Assert.NotEqual(mediaType2.GetHashCode(), mediaType3.GetHashCode());
            Assert.Equal(mediaType1.GetHashCode(), mediaType4.GetHashCode());
            Assert.Equal(mediaType2.GetHashCode(), mediaType5.GetHashCode());
        }

        [Fact]
        public void Equals_UseMediaTypeWithAndWithoutParameters_EqualOrNotEqualNoExceptions()
        {
            MediaTypeHeaderValue mediaType1 = new MediaTypeHeaderValue("text/plain");
            MediaTypeHeaderValue mediaType2 = new MediaTypeHeaderValue("text/plain");
            mediaType2.CharSet = "utf-8";
            MediaTypeHeaderValue mediaType3 = new MediaTypeHeaderValue("text/plain");
            mediaType3.Parameters.Add(new NameValueHeaderValue("name", "value"));
            MediaTypeHeaderValue mediaType4 = new MediaTypeHeaderValue("TEXT/plain");
            MediaTypeHeaderValue mediaType5 = new MediaTypeHeaderValue("TEXT/plain");
            mediaType5.Parameters.Add(new NameValueHeaderValue("CHARSET", "UTF-8"));
            MediaTypeHeaderValue mediaType6 = new MediaTypeHeaderValue("TEXT/plain");
            mediaType6.Parameters.Add(new NameValueHeaderValue("CHARSET", "UTF-8"));
            mediaType6.Parameters.Add(new NameValueHeaderValue("custom", "value"));
            MediaTypeHeaderValue mediaType7 = new MediaTypeHeaderValue("text/other");

            Assert.False(mediaType1.Equals(mediaType2), "No params vs. charset.");
            Assert.False(mediaType2.Equals(mediaType1), "charset vs. no params.");
            Assert.False(mediaType1.Equals(null), "No params vs. <null>.");
            Assert.False(mediaType1.Equals(mediaType3), "No params vs. custom param.");
            Assert.False(mediaType2.Equals(mediaType3), "charset vs. custom param.");
            Assert.True(mediaType1.Equals(mediaType4), "Different casing.");
            Assert.True(mediaType2.Equals(mediaType5), "Different casing in charset.");
            Assert.False(mediaType5.Equals(mediaType6), "charset vs. custom param.");
            Assert.False(mediaType1.Equals(mediaType7), "text/plain vs. text/other.");
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            MediaTypeHeaderValue source = new MediaTypeHeaderValue("application/xml");
            MediaTypeHeaderValue clone = (MediaTypeHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.MediaType, clone.MediaType);
            Assert.Equal(0, clone.Parameters.Count);

            source.CharSet = "utf-8";
            clone = (MediaTypeHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.MediaType, clone.MediaType);
            Assert.Equal("utf-8", clone.CharSet);
            Assert.Equal(1, clone.Parameters.Count);

            source.Parameters.Add(new NameValueHeaderValue("custom", "customValue"));
            clone = (MediaTypeHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.MediaType, clone.MediaType);
            Assert.Equal("utf-8", clone.CharSet);
            Assert.Equal(2, clone.Parameters.Count);
            Assert.Equal("custom", clone.Parameters.ElementAt(1).Name);
            Assert.Equal("customValue", clone.Parameters.ElementAt(1).Value);
        }

        [Fact]
        public void GetMediaTypeLength_DifferentValidScenarios_AllReturnNonZero()
        {
            MediaTypeHeaderValue result = null;

            Assert.Equal(11, MediaTypeHeaderValue.GetMediaTypeLength("text/plain , other/charset", 0,
                DummyCreator, out result));
            Assert.Equal("text/plain", result.MediaType);
            Assert.Equal(0, result.Parameters.Count);

            Assert.Equal(10, MediaTypeHeaderValue.GetMediaTypeLength("text/plain", 0, DummyCreator, out result));
            Assert.Equal("text/plain", result.MediaType);
            Assert.Equal(0, result.Parameters.Count);

            Assert.Equal(30, MediaTypeHeaderValue.GetMediaTypeLength("text/plain; charset=iso-8859-1", 0,
                DummyCreator, out result));
            Assert.Equal("text/plain", result.MediaType);
            Assert.Equal("iso-8859-1", result.CharSet);
            Assert.Equal(1, result.Parameters.Count);

            Assert.Equal(38, MediaTypeHeaderValue.GetMediaTypeLength(" text/plain; custom=value;charset=utf-8",
                1, DummyCreator, out result));
            Assert.Equal("text/plain", result.MediaType);
            Assert.Equal("utf-8", result.CharSet);
            Assert.Equal(2, result.Parameters.Count);

            Assert.Equal(18, MediaTypeHeaderValue.GetMediaTypeLength(" text/plain; custom, next/mediatype",
                1, DummyCreator, out result));
            Assert.Equal("text/plain", result.MediaType);
            Assert.Null(result.CharSet);
            Assert.Equal(1, result.Parameters.Count);
            Assert.Equal("custom", result.Parameters.ElementAt(0).Name);
            Assert.Null(result.Parameters.ElementAt(0).Value);

            Assert.Equal(46, MediaTypeHeaderValue.GetMediaTypeLength(
                "text / plain ; custom = \"x\" ; charset = utf-8 , next/mediatype", 0, DummyCreator, out result));
            Assert.Equal("text/plain", result.MediaType);
            Assert.Equal("utf-8", result.CharSet);
            Assert.Equal(2, result.Parameters.Count);
            Assert.Equal("custom", result.Parameters.ElementAt(0).Name);
            Assert.Equal("\"x\"", result.Parameters.ElementAt(0).Value);
            Assert.Equal("charset", result.Parameters.ElementAt(1).Name);
            Assert.Equal("utf-8", result.Parameters.ElementAt(1).Value);

            Assert.Equal(35, MediaTypeHeaderValue.GetMediaTypeLength(
                "text/plain;custom=\"x\";charset=utf-8,next/mediatype", 0, DummyCreator, out result));
            Assert.Equal("text/plain", result.MediaType);
            Assert.Equal("utf-8", result.CharSet);
            Assert.Equal(2, result.Parameters.Count);
            Assert.Equal("custom", result.Parameters.ElementAt(0).Name);
            Assert.Equal("\"x\"", result.Parameters.ElementAt(0).Value);
            Assert.Equal("charset", result.Parameters.ElementAt(1).Name);
            Assert.Equal("utf-8", result.Parameters.ElementAt(1).Value);
        }

        [Fact]
        public void GetMediaTypeLength_UseCustomCreator_CustomCreatorUsedToCreateMediaTypeInstance()
        {
            MediaTypeHeaderValue result = null;

            // Path: media-type only
            Assert.Equal(10, MediaTypeHeaderValue.GetMediaTypeLength("text/plain", 0,
                () => { return new MediaTypeWithQualityHeaderValue(); }, out result));
            Assert.Equal("text/plain", result.MediaType);
            Assert.Equal(0, result.Parameters.Count);
            Assert.IsType<MediaTypeWithQualityHeaderValue>(result);

            // Path: media-type and parameters
            Assert.Equal(25, MediaTypeHeaderValue.GetMediaTypeLength("text/plain; charset=utf-8", 0,
                () => { return new MediaTypeWithQualityHeaderValue(); }, out result));
            Assert.Equal("text/plain", result.MediaType);
            Assert.Equal(1, result.Parameters.Count);
            Assert.Equal("utf-8", result.CharSet);
            Assert.IsType<MediaTypeWithQualityHeaderValue>(result);
        }

        [Fact]
        public void GetMediaTypeLength_DifferentInvalidScenarios_AllReturnZero()
        {
            MediaTypeHeaderValue result = null;

            Assert.Equal(0, MediaTypeHeaderValue.GetMediaTypeLength(" text/plain", 0, DummyCreator, out result));
            Assert.Null(result);
            Assert.Equal(0, MediaTypeHeaderValue.GetMediaTypeLength("text/plain;", 0, DummyCreator, out result));
            Assert.Null(result);
            Assert.Equal(0, MediaTypeHeaderValue.GetMediaTypeLength("text/plain;name=", 0, DummyCreator, out result));
            Assert.Null(result);
            Assert.Equal(0, MediaTypeHeaderValue.GetMediaTypeLength("text/plain;name=value;", 0, DummyCreator, out result));
            Assert.Null(result);
            Assert.Equal(0, MediaTypeHeaderValue.GetMediaTypeLength("text/plain;", 0, DummyCreator, out result));
            Assert.Null(result);
            Assert.Equal(0, MediaTypeHeaderValue.GetMediaTypeLength(null, 0, DummyCreator, out result));
            Assert.Null(result);
            Assert.Equal(0, MediaTypeHeaderValue.GetMediaTypeLength(string.Empty, 0, DummyCreator, out result));
            Assert.Null(result);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            MediaTypeHeaderValue expected = new MediaTypeHeaderValue("text/plain");
            CheckValidParse(" text/plain  ", expected);
            CheckValidParse("text/plain", expected);

            // We don't have to test all possible input strings, since most of the pieces are handled by other parsers.
            // The purpose of this test is to verify that these other parsers are combined correctly to build a
            // media-type parser.
            expected.CharSet = "utf-8";
            CheckValidParse(" text   /  plain ;  charset =   utf-8 ", expected);
            CheckValidParse("  text/plain;charset=utf-8", expected);
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
            CheckInvalidParse("\r\n text/plain  ");
            CheckInvalidParse("\r\n text   /  plain ;  charset =   utf-8 ");
            CheckInvalidParse("text / plain ; custom =\r\n \"x\" ; charset = utf-8 , next/mediatype");
        }

        #region Helper methods

        private void CheckValidParse(string input, MediaTypeHeaderValue expectedResult)
        {
            MediaTypeHeaderValue result = MediaTypeHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);

            Assert.True(MediaTypeHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { MediaTypeHeaderValue.Parse(input); });

            Assert.False(MediaTypeHeaderValue.TryParse(input, out MediaTypeHeaderValue result));
            Assert.Null(result);
        }

        private static void AssertFormatException(string mediaType)
        {
            Assert.Throws<FormatException>(() => { new MediaTypeHeaderValue(mediaType); });
        }

        private static MediaTypeHeaderValue DummyCreator()
        {
            return new MediaTypeHeaderValue();
        }
        #endregion
    }
}
