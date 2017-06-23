// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class RangeItemHeaderValueTest
    {
        [Fact]
        public void Ctor_BothValuesNull_Throw()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => { new RangeItemHeaderValue(null, null); });
        }

        [Fact]
        public void Ctor_FromValueNegative_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { new RangeItemHeaderValue(-1, null); });
        }

        [Fact]
        public void Ctor_FromGreaterThanToValue_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { new RangeItemHeaderValue(2, 1); });
        }

        [Fact]
        public void Ctor_ToValueNegative_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { new RangeItemHeaderValue(null, -1); });
        }

        [Fact]
        public void Ctor_ValidFormat_SuccessfullyCreated()
        {
            RangeItemHeaderValue rangeItem = new RangeItemHeaderValue(1, 2);
            Assert.Equal(1, rangeItem.From);
            Assert.Equal(2, rangeItem.To);
        }

        [Fact]
        public void ToString_UseDifferentRangeItems_AllSerializedCorrectly()
        {
            // Make sure ToString() doesn't add any separators.
            RangeItemHeaderValue rangeItem = new RangeItemHeaderValue(1000000000, 2000000000);
            Assert.Equal("1000000000-2000000000", rangeItem.ToString());

            rangeItem = new RangeItemHeaderValue(5, null);
            Assert.Equal("5-", rangeItem.ToString());

            rangeItem = new RangeItemHeaderValue(null, 10);
            Assert.Equal("-10", rangeItem.ToString());
        }

        [Fact]
        public void GetHashCode_UseSameAndDifferentRangeItems_SameOrDifferentHashCodes()
        {
            RangeItemHeaderValue rangeItem1 = new RangeItemHeaderValue(1, 2);
            RangeItemHeaderValue rangeItem2 = new RangeItemHeaderValue(1, null);
            RangeItemHeaderValue rangeItem3 = new RangeItemHeaderValue(null, 2);
            RangeItemHeaderValue rangeItem4 = new RangeItemHeaderValue(2, 2);
            RangeItemHeaderValue rangeItem5 = new RangeItemHeaderValue(1, 2);

            Assert.NotEqual(rangeItem1.GetHashCode(), rangeItem2.GetHashCode());
            Assert.NotEqual(rangeItem1.GetHashCode(), rangeItem3.GetHashCode());
            Assert.NotEqual(rangeItem1.GetHashCode(), rangeItem4.GetHashCode());
            Assert.Equal(rangeItem1.GetHashCode(), rangeItem5.GetHashCode());
        }

        [Fact]
        public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
        {
            RangeItemHeaderValue rangeItem1 = new RangeItemHeaderValue(1, 2);
            RangeItemHeaderValue rangeItem2 = new RangeItemHeaderValue(1, null);
            RangeItemHeaderValue rangeItem3 = new RangeItemHeaderValue(null, 2);
            RangeItemHeaderValue rangeItem4 = new RangeItemHeaderValue(2, 2);
            RangeItemHeaderValue rangeItem5 = new RangeItemHeaderValue(1, 2);

            Assert.False(rangeItem1.Equals(rangeItem2), "1-2 vs. 1-.");
            Assert.False(rangeItem2.Equals(rangeItem1), "1- vs. 1-2.");
            Assert.False(rangeItem1.Equals(null), "1-2 vs. null.");
            Assert.False(rangeItem1.Equals(rangeItem3), "1-2 vs. -2.");
            Assert.False(rangeItem3.Equals(rangeItem1), "-2 vs. 1-2.");
            Assert.False(rangeItem1.Equals(rangeItem4), "1-2 vs. 2-2.");
            Assert.True(rangeItem1.Equals(rangeItem5), "1-2 vs. 1-2.");
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            RangeItemHeaderValue source = new RangeItemHeaderValue(1, 2);
            RangeItemHeaderValue clone = (RangeItemHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.From, clone.From);
            Assert.Equal(source.To, clone.To);

            source = new RangeItemHeaderValue(1, null);
            clone = (RangeItemHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.From, clone.From);
            Assert.Equal(source.To, clone.To);

            source = new RangeItemHeaderValue(null, 2);
            clone = (RangeItemHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.From, clone.From);
            Assert.Equal(source.To, clone.To);
        }

        [Fact]
        public void GetRangeItemLength_DifferentValidScenarios_AllReturnNonZero()
        {
            CheckValidGetRangeItemLength("1-2", 0, 3, 1, 2);
            CheckValidGetRangeItemLength("0-0", 0, 3, 0, 0);
            CheckValidGetRangeItemLength(" 1-", 1, 2, 1, null);
            CheckValidGetRangeItemLength(" -2", 1, 2, null, 2);

            // Note that the parser will only parse '1-' as a valid range and ignore '-2'. It is the callers 
            // responsibility to determine if this is indeed a valid range
            CheckValidGetRangeItemLength(" 1--2", 1, 2, 1, null);

            CheckValidGetRangeItemLength(" 684684 - 123456789012345 !", 1, 25, 684684, 123456789012345);

            // The separator doesn't matter. It only parses until the first non-whitespace
            CheckValidGetRangeItemLength(" 1 - 2 ,", 1, 6, 1, 2);
        }

        [Fact]
        public void GetRangeItemLength_DifferentInvalidScenarios_AllReturnZero()
        {
            CheckInvalidGetRangeItemLength(" 1-2", 0); // no leading spaces.
            CheckInvalidGetRangeItemLength("2-1", 0);
            CheckInvalidGetRangeItemLength("-", 0);
            CheckInvalidGetRangeItemLength("1", 0);
            CheckInvalidGetRangeItemLength(null, 0);
            CheckInvalidGetRangeItemLength(string.Empty, 0);
            CheckInvalidGetRangeItemLength("12345678901234567890123-", 0); // >>Int64.MaxValue
            CheckInvalidGetRangeItemLength("-12345678901234567890123", 0); // >>Int64.MaxValue
            CheckInvalidGetRangeItemLength("9999999999999999999-", 0); // 19-digit numbers outside the Int64 range.
            CheckInvalidGetRangeItemLength("-9999999999999999999", 0); // 19-digit numbers outside the Int64 range.
        }

        [Fact]
        public void GetRangeItemListLength_DifferentValidScenarios_AllReturnNonZero()
        {
            CheckValidGetRangeItemListLength("x,,1-2, 3 -  , , -6 , ,,", 1, 23,
                new Tuple<long?, long?>(1, 2), new Tuple<long?, long?>(3, null), new Tuple<long?, long?>(null, 6));
            CheckValidGetRangeItemListLength("1-2,", 0, 4, new Tuple<long?, long?>(1, 2));
            CheckValidGetRangeItemListLength("1-", 0, 2, new Tuple<long?, long?>(1, null));
        }

        [Fact]
        public void GetRangeItemListLength_DifferentInvalidScenarios_AllReturnZero()
        {
            CheckInvalidGetRangeItemListLength(null, 0);
            CheckInvalidGetRangeItemListLength(string.Empty, 0);
            CheckInvalidGetRangeItemListLength("1-2", 3);
            CheckInvalidGetRangeItemListLength(",,", 0);
            CheckInvalidGetRangeItemListLength("1", 0);
            CheckInvalidGetRangeItemListLength("1-2,3", 0);
            CheckInvalidGetRangeItemListLength("1--2", 0);
            CheckInvalidGetRangeItemListLength("1,-2", 0);
            CheckInvalidGetRangeItemListLength("-", 0);
            CheckInvalidGetRangeItemListLength("--", 0);
        }

        #region Helper methods

        private static void AssertFormatException(string tag)
        {
            Assert.Throws<FormatException>(() => { new EntityTagHeaderValue(tag); });
        }

        private static void CheckValidGetRangeItemLength(string input, int startIndex, int expectedLength,
            long? expectedFrom, long? expectedTo)
        {
            RangeItemHeaderValue result = null;
            Assert.Equal(expectedLength, RangeItemHeaderValue.GetRangeItemLength(input, startIndex, out result));
            Assert.Equal(expectedFrom, result.From);
            Assert.Equal(expectedTo, result.To);
        }

        private static void CheckInvalidGetRangeItemLength(string input, int startIndex)
        {
            RangeItemHeaderValue result = null;
            Assert.Equal(0, RangeItemHeaderValue.GetRangeItemLength(input, startIndex, out result));
            Assert.Null(result);
        }

        private static void CheckValidGetRangeItemListLength(string input, int startIndex, int expectedLength,
            params Tuple<long?, long?>[] expectedRanges)
        {
            List<RangeItemHeaderValue> ranges = new List<RangeItemHeaderValue>();
            Assert.Equal(expectedLength, RangeItemHeaderValue.GetRangeItemListLength(input, startIndex, ranges));

            Assert.Equal<int>(expectedRanges.Length, ranges.Count);

            for (int i = 0; i < expectedRanges.Length; i++)
            {
                Assert.Equal(expectedRanges[i].Item1, ranges[i].From);
                Assert.Equal(expectedRanges[i].Item2, ranges[i].To);
            }
        }

        private static void CheckInvalidGetRangeItemListLength(string input, int startIndex)
        {
            List<RangeItemHeaderValue> ranges = new List<RangeItemHeaderValue>();
            Assert.Equal(0, RangeItemHeaderValue.GetRangeItemListLength(input, startIndex, ranges));
        }
        #endregion
    }
}
