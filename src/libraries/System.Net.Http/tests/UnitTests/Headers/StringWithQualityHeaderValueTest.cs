// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class StringWithQualityHeaderValueTest
    {
        [Fact]
        public void Ctor_StringOnlyOverload_MatchExpectation()
        {
            StringWithQualityHeaderValue value = new StringWithQualityHeaderValue("token");
            Assert.Equal("token", value.Value);
            Assert.Null(value.Quality);

            AssertExtensions.Throws<ArgumentException>("value", () => { new StringWithQualityHeaderValue(null); });
            AssertExtensions.Throws<ArgumentException>("value", () => { new StringWithQualityHeaderValue(""); });
            Assert.Throws<FormatException>(() => { new StringWithQualityHeaderValue("in valid"); });
        }

        [Fact]
        public void Ctor_StringWithQualityOverload_MatchExpectation()
        {
            StringWithQualityHeaderValue value = new StringWithQualityHeaderValue("token", 0.5);
            Assert.Equal("token", value.Value);
            Assert.Equal(0.5, value.Quality);

            AssertExtensions.Throws<ArgumentException>("value", () => { new StringWithQualityHeaderValue(null, 0.1); });
            AssertExtensions.Throws<ArgumentException>("value", () => { new StringWithQualityHeaderValue("", 0.1); });
            Assert.Throws<FormatException>(() => { new StringWithQualityHeaderValue("in valid", 0.1); });

            Assert.Throws<ArgumentOutOfRangeException>(() => { new StringWithQualityHeaderValue("t", 1.1); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { new StringWithQualityHeaderValue("t", -0.1); });
        }

        [Fact]
        public void ToString_UseDifferentValues_AllSerializedCorrectly()
        {
            StringWithQualityHeaderValue value = new StringWithQualityHeaderValue("token");
            Assert.Equal("token", value.ToString());

            value = new StringWithQualityHeaderValue("token", 0.1);
            Assert.Equal("token; q=0.1", value.ToString());

            value = new StringWithQualityHeaderValue("token", 0);
            Assert.Equal("token; q=0.0", value.ToString());

            value = new StringWithQualityHeaderValue("token", 1);
            Assert.Equal("token; q=1.0", value.ToString());

            // Note that the quality value gets rounded
            value = new StringWithQualityHeaderValue("token", 0.56789);
            Assert.Equal("token; q=0.568", value.ToString());
        }

        [Fact]
        public void GetHashCode_UseSameAndDifferentValues_SameOrDifferentHashCodes()
        {
            StringWithQualityHeaderValue value1 = new StringWithQualityHeaderValue("t", 0.123);
            StringWithQualityHeaderValue value2 = new StringWithQualityHeaderValue("t", 0.123);
            StringWithQualityHeaderValue value3 = new StringWithQualityHeaderValue("T", 0.123);
            StringWithQualityHeaderValue value4 = new StringWithQualityHeaderValue("t");
            StringWithQualityHeaderValue value5 = new StringWithQualityHeaderValue("x", 0.123);
            StringWithQualityHeaderValue value6 = new StringWithQualityHeaderValue("t", 0.5);
            StringWithQualityHeaderValue value7 = new StringWithQualityHeaderValue("t", 0.1234);
            StringWithQualityHeaderValue value8 = new StringWithQualityHeaderValue("T");
            StringWithQualityHeaderValue value9 = new StringWithQualityHeaderValue("x");

            Assert.Equal(value1.GetHashCode(), value2.GetHashCode());
            Assert.Equal(value1.GetHashCode(), value3.GetHashCode());
            Assert.NotEqual(value1.GetHashCode(), value4.GetHashCode());
            Assert.NotEqual(value1.GetHashCode(), value5.GetHashCode());
            Assert.NotEqual(value1.GetHashCode(), value6.GetHashCode());
            Assert.NotEqual(value1.GetHashCode(), value7.GetHashCode());
            Assert.Equal(value4.GetHashCode(), value8.GetHashCode());
            Assert.NotEqual(value4.GetHashCode(), value9.GetHashCode());
        }

        [Fact]
        public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
        {
            StringWithQualityHeaderValue value1 = new StringWithQualityHeaderValue("t", 0.123);
            StringWithQualityHeaderValue value2 = new StringWithQualityHeaderValue("t", 0.123);
            StringWithQualityHeaderValue value3 = new StringWithQualityHeaderValue("T", 0.123);
            StringWithQualityHeaderValue value4 = new StringWithQualityHeaderValue("t");
            StringWithQualityHeaderValue value5 = new StringWithQualityHeaderValue("x", 0.123);
            StringWithQualityHeaderValue value6 = new StringWithQualityHeaderValue("t", 0.5);
            StringWithQualityHeaderValue value7 = new StringWithQualityHeaderValue("t", 0.1234);
            StringWithQualityHeaderValue value8 = new StringWithQualityHeaderValue("T");
            StringWithQualityHeaderValue value9 = new StringWithQualityHeaderValue("x");

            Assert.False(value1.Equals(null), "t; q=0.123 vs. <null>");
            Assert.True(value1.Equals(value2), "t; q=0.123 vs. t; q=0.123");
            Assert.True(value1.Equals(value3), "t; q=0.123 vs. T; q=0.123");
            Assert.False(value1.Equals(value4), "t; q=0.123 vs. t");
            Assert.False(value4.Equals(value1), "t vs. t; q=0.123");
            Assert.False(value1.Equals(value5), "t; q=0.123 vs. x; q=0.123");
            Assert.False(value1.Equals(value6), "t; q=0.123 vs. t; q=0.5");
            Assert.False(value1.Equals(value7), "t; q=0.123 vs. t; q=0.1234");
            Assert.True(value4.Equals(value8), "t vs. T");
            Assert.False(value4.Equals(value9), "t vs. T");
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            StringWithQualityHeaderValue source = new StringWithQualityHeaderValue("token", 0.123);
            StringWithQualityHeaderValue clone = (StringWithQualityHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Value, clone.Value);
            Assert.Equal(source.Quality, clone.Quality);

            source = new StringWithQualityHeaderValue("token");
            clone = (StringWithQualityHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Value, clone.Value);
            Assert.Null(source.Quality);
        }

        [Fact]
        public void GetStringWithQualityLength_DifferentValidScenarios_AllReturnNonZero()
        {
            StringWithQualityHeaderValue result = null;

            CallGetStringWithQualityLength(" token ; q = 0.123 ,", 1, 18, out result);
            Assert.Equal("token", result.Value);
            Assert.Equal(0.123, result.Quality);

            CallGetStringWithQualityLength("token;q=1 , x", 0, 10, out result);
            Assert.Equal("token", result.Value);
            Assert.Equal(1, result.Quality);

            CallGetStringWithQualityLength("*", 0, 1, out result);
            Assert.Equal("*", result.Value);
            Assert.Null(result.Quality);

            CallGetStringWithQualityLength("t;q=0.", 0, 6, out result);
            Assert.Equal("t", result.Value);
            Assert.Equal(0, result.Quality);

            CallGetStringWithQualityLength("t;q=1.,", 0, 6, out result);
            Assert.Equal("t", result.Value);
            Assert.Equal(1, result.Quality);

            CallGetStringWithQualityLength("t ;  q  =   0X", 0, 13, out result);
            Assert.Equal("t", result.Value);
            Assert.Equal(0, result.Quality);

            CallGetStringWithQualityLength("t ;  q  =   0,", 0, 13, out result);
            Assert.Equal("t", result.Value);
            Assert.Equal(0, result.Quality);
        }

        [Fact]
        public void GetStringWithQualityLength_DifferentInvalidScenarios_AllReturnZero()
        {
            CheckInvalidGetStringWithQualityLength(" t", 0); // no leading whitespace allowed
            CheckInvalidGetStringWithQualityLength("t;q=", 0);
            CheckInvalidGetStringWithQualityLength("t;q=-1", 0);
            CheckInvalidGetStringWithQualityLength("t;q=1.00001", 0);
            CheckInvalidGetStringWithQualityLength("t;q", 0);
            CheckInvalidGetStringWithQualityLength("t;", 0);
            CheckInvalidGetStringWithQualityLength("t;;q=1", 0);
            CheckInvalidGetStringWithQualityLength("t;q=a", 0);
            CheckInvalidGetStringWithQualityLength("t;qa", 0);
            CheckInvalidGetStringWithQualityLength("t;q1", 0);

            CheckInvalidGetStringWithQualityLength("", 0);
            CheckInvalidGetStringWithQualityLength(null, 0);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParse("text", new StringWithQualityHeaderValue("text"));
            CheckValidParse("text;q=0.5", new StringWithQualityHeaderValue("text", 0.5));
            CheckValidParse("text ; q = 0.5", new StringWithQualityHeaderValue("text", 0.5));
            CheckValidParse(" text ; q = 0.5 ", new StringWithQualityHeaderValue("text", 0.5));
            CheckValidParse("  text  ", new StringWithQualityHeaderValue("text"));
            CheckValidParse("  text  ;  q = 0.123", new StringWithQualityHeaderValue("text", 0.123));
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("text,");
            CheckInvalidParse("\r\n text ; q = 0.5, next_text  ");
            CheckInvalidParse("  text,next_text  ");
            CheckInvalidParse(" ,, text, , ,next");
            CheckInvalidParse(" ,, text, , ,");
            CheckInvalidParse(", \r\n text \r\n ; \r\n q = 0.123");
            CheckInvalidParse("te\u00E4xt");
            CheckInvalidParse("text\u4F1A");
            CheckInvalidParse("\u4F1A");
            CheckInvalidParse("t;q=\u4F1A");
            CheckInvalidParse("t;q=");
            CheckInvalidParse("t;q");
            CheckInvalidParse("t;\u4F1A=1");
            CheckInvalidParse("t;q\u4F1A=1");
            CheckInvalidParse("t y");
            CheckInvalidParse("t;q=1 y");

            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
            CheckInvalidParse("  ");
            CheckInvalidParse("  ,,");
        }

        #region Helper methods

        private void CheckValidParse(string input, StringWithQualityHeaderValue expectedResult)
        {
            StringWithQualityHeaderValue result = StringWithQualityHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);

            Assert.True(StringWithQualityHeaderValue.TryParse(input, out result));
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
            Assert.Throws<FormatException>(() => { StringWithQualityHeaderValue.Parse(input); });

            Assert.False(StringWithQualityHeaderValue.TryParse(input, out StringWithQualityHeaderValue result));
            Assert.Null(result);
        }

        private static void CallGetStringWithQualityLength(string input, int startIndex, int expectedLength,
            out StringWithQualityHeaderValue result)
        {
            object temp = null;
            Assert.Equal(expectedLength, StringWithQualityHeaderValue.GetStringWithQualityLength(input,
                startIndex, out temp));
            result = temp as StringWithQualityHeaderValue;
        }

        private static void CheckInvalidGetStringWithQualityLength(string input, int startIndex)
        {
            object result = null;
            Assert.Equal(0, StringWithQualityHeaderValue.GetStringWithQualityLength(input, startIndex, out result));
            Assert.Null(result);
        }
        #endregion
    }
}
