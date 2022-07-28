// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class RangeConditionHeaderValueTest
    {
        [Fact]
        public void Ctor_EntityTagOverload_MatchExpectation()
        {
            RangeConditionHeaderValue rangeCondition = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"x\""));
            Assert.Equal(new EntityTagHeaderValue("\"x\""), rangeCondition.EntityTag);
            Assert.Null(rangeCondition.Date);

            EntityTagHeaderValue input = null;
            Assert.Throws<ArgumentNullException>(() => { new RangeConditionHeaderValue(input); });
        }

        [Fact]
        public void Ctor_EntityTagStringOverload_MatchExpectation()
        {
            RangeConditionHeaderValue rangeCondition = new RangeConditionHeaderValue("\"y\"");
            Assert.Equal(new EntityTagHeaderValue("\"y\""), rangeCondition.EntityTag);
            Assert.Null(rangeCondition.Date);

            AssertExtensions.Throws<ArgumentException>("tag", () => { new RangeConditionHeaderValue((string)null); });
        }

        [Fact]
        public void Ctor_DateOverload_MatchExpectation()
        {
            RangeConditionHeaderValue rangeCondition = new RangeConditionHeaderValue(
                new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            Assert.Null(rangeCondition.EntityTag);
            Assert.Equal(new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero), rangeCondition.Date);
        }

        [Fact]
        public void ToString_UseDifferentrangeConditions_AllSerializedCorrectly()
        {
            RangeConditionHeaderValue rangeCondition = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"x\""));
            Assert.Equal("\"x\"", rangeCondition.ToString());

            rangeCondition = new RangeConditionHeaderValue(new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            Assert.Equal("Thu, 15 Jul 2010 12:33:57 GMT", rangeCondition.ToString());
        }

        [Fact]
        public void GetHashCode_UseSameAndDifferentrangeConditions_SameOrDifferentHashCodes()
        {
            RangeConditionHeaderValue rangeCondition1 = new RangeConditionHeaderValue("\"x\"");
            RangeConditionHeaderValue rangeCondition2 = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"x\""));
            RangeConditionHeaderValue rangeCondition3 = new RangeConditionHeaderValue(
                new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            RangeConditionHeaderValue rangeCondition4 = new RangeConditionHeaderValue(
                new DateTimeOffset(2008, 8, 16, 13, 44, 10, TimeSpan.Zero));
            RangeConditionHeaderValue rangeCondition5 = new RangeConditionHeaderValue(
                new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            RangeConditionHeaderValue rangeCondition6 = new RangeConditionHeaderValue(
                new EntityTagHeaderValue("\"x\"", true));

            Assert.Equal(rangeCondition1.GetHashCode(), rangeCondition2.GetHashCode());
            Assert.NotEqual(rangeCondition1.GetHashCode(), rangeCondition3.GetHashCode());
            Assert.NotEqual(rangeCondition3.GetHashCode(), rangeCondition4.GetHashCode());
            Assert.Equal(rangeCondition3.GetHashCode(), rangeCondition5.GetHashCode());
            Assert.NotEqual(rangeCondition1.GetHashCode(), rangeCondition6.GetHashCode());
        }

        [Fact]
        public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
        {
            RangeConditionHeaderValue rangeCondition1 = new RangeConditionHeaderValue("\"x\"");
            RangeConditionHeaderValue rangeCondition2 = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"x\""));
            RangeConditionHeaderValue rangeCondition3 = new RangeConditionHeaderValue(
                new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            RangeConditionHeaderValue rangeCondition4 = new RangeConditionHeaderValue(
                new DateTimeOffset(2008, 8, 16, 13, 44, 10, TimeSpan.Zero));
            RangeConditionHeaderValue rangeCondition5 = new RangeConditionHeaderValue(
                new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            RangeConditionHeaderValue rangeCondition6 = new RangeConditionHeaderValue(
                new EntityTagHeaderValue("\"x\"", true));

            Assert.False(rangeCondition1.Equals(null), "\"x\" vs. <null>");
            Assert.True(rangeCondition1.Equals(rangeCondition2), "\"x\" vs. \"x\"");
            Assert.False(rangeCondition1.Equals(rangeCondition3), "\"x\" vs. date");
            Assert.False(rangeCondition3.Equals(rangeCondition1), "date vs. \"x\"");
            Assert.False(rangeCondition3.Equals(rangeCondition4), "date vs. different date");
            Assert.True(rangeCondition3.Equals(rangeCondition5), "date vs. date");
            Assert.False(rangeCondition1.Equals(rangeCondition6), "\"x\" vs. W/\"x\"");
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            RangeConditionHeaderValue source = new RangeConditionHeaderValue(new EntityTagHeaderValue("\"x\""));
            RangeConditionHeaderValue clone = (RangeConditionHeaderValue)((ICloneable)source).Clone();

            Assert.Equal(source.EntityTag, clone.EntityTag);
            Assert.Null(clone.Date);

            source = new RangeConditionHeaderValue(new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            clone = (RangeConditionHeaderValue)((ICloneable)source).Clone();

            Assert.Null(clone.EntityTag);
            Assert.Equal(source.Date, clone.Date);
        }

        [Fact]
        public void GetRangeConditionLength_DifferentValidScenarios_AllReturnNonZero()
        {
            RangeConditionHeaderValue result = null;

            CallGetRangeConditionLength(" W/ \"tag\" ", 1, 9, out result);
            Assert.Equal(new EntityTagHeaderValue("\"tag\"", true), result.EntityTag);
            Assert.Null(result.Date);

            CallGetRangeConditionLength(" w/\"tag\"", 1, 7, out result);
            Assert.Equal(new EntityTagHeaderValue("\"tag\"", true), result.EntityTag);
            Assert.Null(result.Date);

            CallGetRangeConditionLength("\"tag\"", 0, 5, out result);
            Assert.Equal(new EntityTagHeaderValue("\"tag\""), result.EntityTag);
            Assert.Null(result.Date);

            CallGetRangeConditionLength("Wed, 09 Nov 1994 08:49:37 GMT", 0, 29, out result);
            Assert.Null(result.EntityTag);
            Assert.Equal(new DateTimeOffset(1994, 11, 9, 8, 49, 37, TimeSpan.Zero), result.Date);

            CallGetRangeConditionLength("Sun, 06 Nov 1994 08:49:37 GMT", 0, 29, out result);
            Assert.Null(result.EntityTag);
            Assert.Equal(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero), result.Date);
        }

        [Fact]
        public void GetRangeConditionLength_DifferentInvalidScenarios_AllReturnZero()
        {
            CheckInvalidGetRangeConditionLength(" \"x\"", 0); // no leading whitespace allowed
            CheckInvalidGetRangeConditionLength(" Wed 09 Nov 1994 08:49:37 GMT", 0);
            CheckInvalidGetRangeConditionLength("\"x", 0);
            CheckInvalidGetRangeConditionLength("Wed, 09 Nov", 0);
            CheckInvalidGetRangeConditionLength("W/Wed 09 Nov 1994 08:49:37 GMT", 0);
            CheckInvalidGetRangeConditionLength("\"x\",", 0);
            CheckInvalidGetRangeConditionLength("Wed 09 Nov 1994 08:49:37 GMT,", 0);

            CheckInvalidGetRangeConditionLength("", 0);
            CheckInvalidGetRangeConditionLength(null, 0);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParse("  \"x\" ", new RangeConditionHeaderValue("\"x\""));
            CheckValidParse("  Sun, 06 Nov 1994 08:49:37 GMT ",
                new RangeConditionHeaderValue(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero)));
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("\"x\" ,"); // no delimiter allowed
            CheckInvalidParse("Sun, 06 Nov 1994 08:49:37 GMT ,"); // no delimiter allowed
            CheckInvalidParse("\"x\" Sun, 06 Nov 1994 08:49:37 GMT");
            CheckInvalidParse("Sun, 06 Nov 1994 08:49:37 GMT \"x\"");
            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
        }

        #region Helper methods

        private void CheckValidParse(string input, RangeConditionHeaderValue expectedResult)
        {
            RangeConditionHeaderValue result = RangeConditionHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);

            Assert.True(RangeConditionHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { RangeConditionHeaderValue.Parse(input); });

            Assert.False(RangeConditionHeaderValue.TryParse(input, out RangeConditionHeaderValue result));
            Assert.Null(result);
        }

        private static void CallGetRangeConditionLength(string input, int startIndex, int expectedLength,
            out RangeConditionHeaderValue result)
        {
            object temp = null;
            Assert.Equal(expectedLength, RangeConditionHeaderValue.GetRangeConditionLength(input, startIndex,
                out temp));
            result = temp as RangeConditionHeaderValue;
        }

        private static void CheckInvalidGetRangeConditionLength(string input, int startIndex)
        {
            object result = null;
            Assert.Equal(0, RangeConditionHeaderValue.GetRangeConditionLength(input, startIndex, out result));
            Assert.Null(result);
        }
        #endregion
    }
}
