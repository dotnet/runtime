// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class RetryConditionHeaderValueTest
    {
        [Fact]
        public void Ctor_EntityTagOverload_MatchExpectation()
        {
            RetryConditionHeaderValue retryCondition = new RetryConditionHeaderValue(new TimeSpan(0, 0, 3));
            Assert.Equal(new TimeSpan(0, 0, 3), retryCondition.Delta);
            Assert.Null(retryCondition.Date);

            Assert.Throws<ArgumentOutOfRangeException>(() => { new RetryConditionHeaderValue(new TimeSpan(1234567, 0, 0)); });
        }

        [Fact]
        public void Ctor_DateOverload_MatchExpectation()
        {
            RetryConditionHeaderValue retryCondition = new RetryConditionHeaderValue(
                new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            Assert.Null(retryCondition.Delta);
            Assert.Equal(new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero), retryCondition.Date);
        }

        [Fact]
        public void ToString_UseDifferentRetryConditions_AllSerializedCorrectly()
        {
            RetryConditionHeaderValue retryCondition = new RetryConditionHeaderValue(new TimeSpan(0, 0, 50000000));
            Assert.Equal("50000000", retryCondition.ToString());

            retryCondition = new RetryConditionHeaderValue(new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            Assert.Equal("Thu, 15 Jul 2010 12:33:57 GMT", retryCondition.ToString());
        }

        [Fact]
        public void GetHashCode_UseSameAndDifferentRetryConditions_SameOrDifferentHashCodes()
        {
            RetryConditionHeaderValue retryCondition1 = new RetryConditionHeaderValue(new TimeSpan(0, 0, 1000000));
            RetryConditionHeaderValue retryCondition2 = new RetryConditionHeaderValue(new TimeSpan(0, 0, 1000000));
            RetryConditionHeaderValue retryCondition3 = new RetryConditionHeaderValue(
                new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            RetryConditionHeaderValue retryCondition4 = new RetryConditionHeaderValue(
                new DateTimeOffset(2008, 8, 16, 13, 44, 10, TimeSpan.Zero));
            RetryConditionHeaderValue retryCondition5 = new RetryConditionHeaderValue(
                new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            RetryConditionHeaderValue retryCondition6 = new RetryConditionHeaderValue(new TimeSpan(0, 0, 2000000));

            Assert.Equal(retryCondition1.GetHashCode(), retryCondition2.GetHashCode());
            Assert.NotEqual(retryCondition1.GetHashCode(), retryCondition3.GetHashCode());
            Assert.NotEqual(retryCondition3.GetHashCode(), retryCondition4.GetHashCode());
            Assert.Equal(retryCondition3.GetHashCode(), retryCondition5.GetHashCode());
            Assert.NotEqual(retryCondition1.GetHashCode(), retryCondition6.GetHashCode());
        }

        [Fact]
        public void Equals_UseSameAndDifferentRetrys_EqualOrNotEqualNoExceptions()
        {
            RetryConditionHeaderValue retryCondition1 = new RetryConditionHeaderValue(new TimeSpan(0, 0, 1000000));
            RetryConditionHeaderValue retryCondition2 = new RetryConditionHeaderValue(new TimeSpan(0, 0, 1000000));
            RetryConditionHeaderValue retryCondition3 = new RetryConditionHeaderValue(
                new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            RetryConditionHeaderValue retryCondition4 = new RetryConditionHeaderValue(
                new DateTimeOffset(2008, 8, 16, 13, 44, 10, TimeSpan.Zero));
            RetryConditionHeaderValue retryCondition5 = new RetryConditionHeaderValue(
                new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            RetryConditionHeaderValue retryCondition6 = new RetryConditionHeaderValue(new TimeSpan(0, 0, 2000000));

            Assert.False(retryCondition1.Equals(null), "delta vs. <null>");
            Assert.True(retryCondition1.Equals(retryCondition2), "delta vs. delta");
            Assert.False(retryCondition1.Equals(retryCondition3), "delta vs. date");
            Assert.False(retryCondition3.Equals(retryCondition1), "date vs. delta");
            Assert.False(retryCondition3.Equals(retryCondition4), "date vs. different date");
            Assert.True(retryCondition3.Equals(retryCondition5), "date vs. date");
            Assert.False(retryCondition1.Equals(retryCondition6), "delta vs. different delta");
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            RetryConditionHeaderValue source = new RetryConditionHeaderValue(new TimeSpan(0, 0, 123456789));
            RetryConditionHeaderValue clone = (RetryConditionHeaderValue)((ICloneable)source).Clone();

            Assert.Equal(source.Delta, clone.Delta);
            Assert.Null(clone.Date);

            source = new RetryConditionHeaderValue(new DateTimeOffset(2010, 7, 15, 12, 33, 57, TimeSpan.Zero));
            clone = (RetryConditionHeaderValue)((ICloneable)source).Clone();

            Assert.Null(clone.Delta);
            Assert.Equal(source.Date, clone.Date);
        }

        [Fact]
        public void GetRetryConditionLength_DifferentValidScenarios_AllReturnNonZero()
        {
            RetryConditionHeaderValue result = null;

            CallGetRetryConditionLength(" 1234567890 ", 1, 11, out result);
            Assert.Equal(new TimeSpan(0, 0, 1234567890), result.Delta);
            Assert.Null(result.Date);

            CallGetRetryConditionLength("1", 0, 1, out result);
            Assert.Equal(new TimeSpan(0, 0, 1), result.Delta);
            Assert.Null(result.Date);

            CallGetRetryConditionLength("001", 0, 3, out result);
            Assert.Equal(new TimeSpan(0, 0, 1), result.Delta);
            Assert.Null(result.Date);

            CallGetRetryConditionLength("Wed, 09 Nov 1994 08:49:37 GMT", 0, 29, out result);
            Assert.Null(result.Delta);
            Assert.Equal(new DateTimeOffset(1994, 11, 9, 8, 49, 37, TimeSpan.Zero), result.Date);

            CallGetRetryConditionLength("Sun, 06 Nov 1994 08:49:37 GMT     ", 0, 34, out result);
            Assert.Null(result.Delta);
            Assert.Equal(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero), result.Date);
        }

        [Fact]
        public void GetRetryConditionLength_DifferentInvalidScenarios_AllReturnZero()
        {
            CheckInvalidGetRetryConditionLength(" 1", 0); // no leading whitespace allowed
            CheckInvalidGetRetryConditionLength(" Wed 09 Nov 1994 08:49:37 GMT", 0);
            CheckInvalidGetRetryConditionLength("-5", 0);

            // Even though the first char is a valid 'delta', GetRetryConditionLength() expects the whole string to be
            // a valid 'delta'.
            CheckInvalidGetRetryConditionLength("1.5", 0);
            CheckInvalidGetRetryConditionLength("5123,", 0);

            CheckInvalidGetRetryConditionLength("123456789012345678901234567890", 0); // >>Int32.MaxValue
            CheckInvalidGetRetryConditionLength("9999999999", 0); // >Int32.MaxValue but same amount of digits

            CheckInvalidGetRetryConditionLength("Wed, 09 Nov", 0);
            CheckInvalidGetRetryConditionLength("W/Wed 09 Nov 1994 08:49:37 GMT", 0);
            CheckInvalidGetRetryConditionLength("Wed 09 Nov 1994 08:49:37 GMT,", 0);

            CheckInvalidGetRetryConditionLength("", 0);
            CheckInvalidGetRetryConditionLength(null, 0);

            CheckInvalidGetRetryConditionLength(" 1234567890\n ", 0);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParse("  123456789 ", new RetryConditionHeaderValue(new TimeSpan(0, 0, 123456789)));
            CheckValidParse("  Sun, 06 Nov 1994 08:49:37 GMT ",
                new RetryConditionHeaderValue(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero)));
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("123 ,"); // no delimiter allowed
            CheckInvalidParse("Sun, 06 Nov 1994 08:49:37 GMT ,"); // no delimiter allowed
            CheckInvalidParse("123 Sun, 06 Nov 1994 08:49:37 GMT");
            CheckInvalidParse("Sun, 06 Nov 1994 08:49:37 GMT \"x\"");
            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
        }

        #region Helper methods

        private void CheckValidParse(string input, RetryConditionHeaderValue expectedResult)
        {
            RetryConditionHeaderValue result = RetryConditionHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);

            Assert.True(RetryConditionHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { RetryConditionHeaderValue.Parse(input); });

            Assert.False(RetryConditionHeaderValue.TryParse(input, out RetryConditionHeaderValue result));
            Assert.Null(result);
        }

        private static void CallGetRetryConditionLength(string input, int startIndex, int expectedLength,
            out RetryConditionHeaderValue result)
        {
            object temp = null;
            Assert.Equal(expectedLength, RetryConditionHeaderValue.GetRetryConditionLength(input, startIndex,
                out temp));
            result = temp as RetryConditionHeaderValue;
        }

        private static void CheckInvalidGetRetryConditionLength(string input, int startIndex)
        {
            object result = null;
            Assert.Equal(0, RetryConditionHeaderValue.GetRetryConditionLength(input, startIndex, out result));
            Assert.Null(result);
        }
        #endregion
    }
}
