// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class RangeHeaderValueTest
    {
        [Fact]
        public void Ctor_InvalidRange_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { new RangeHeaderValue(5, 2); });
        }

        [Fact]
        public void Unit_GetAndSetValidAndInvalidValues_MatchExpectation()
        {
            RangeHeaderValue range = new RangeHeaderValue();
            range.Unit = "myunit";
            Assert.Equal("myunit", range.Unit);

            AssertExtensions.Throws<ArgumentException>("value", () => { range.Unit = null; });
            AssertExtensions.Throws<ArgumentException>("value", () => { range.Unit = ""; });
            Assert.Throws<FormatException>(() => { range.Unit = " x"; });
            Assert.Throws<FormatException>(() => { range.Unit = "x "; });
            Assert.Throws<FormatException>(() => { range.Unit = "x y"; });
        }

        [Fact]
        public void ToString_UseDifferentRanges_AllSerializedCorrectly()
        {
            RangeHeaderValue range = new RangeHeaderValue();
            range.Unit = "myunit";
            range.Ranges.Add(new RangeItemHeaderValue(1, 3));
            Assert.Equal("myunit=1-3", range.ToString());

            range.Ranges.Add(new RangeItemHeaderValue(5, null));
            range.Ranges.Add(new RangeItemHeaderValue(null, 17));
            Assert.Equal("myunit=1-3, 5-, -17", range.ToString());
        }

        [Fact]
        public void GetHashCode_UseSameAndDifferentRanges_SameOrDifferentHashCodes()
        {
            RangeHeaderValue range1 = new RangeHeaderValue(1, 2);
            RangeHeaderValue range2 = new RangeHeaderValue(1, 2);
            range2.Unit = "BYTES";
            RangeHeaderValue range3 = new RangeHeaderValue(1, null);
            RangeHeaderValue range4 = new RangeHeaderValue(null, 2);
            RangeHeaderValue range5 = new RangeHeaderValue();
            range5.Ranges.Add(new RangeItemHeaderValue(1, 2));
            range5.Ranges.Add(new RangeItemHeaderValue(3, 4));
            RangeHeaderValue range6 = new RangeHeaderValue();
            range6.Ranges.Add(new RangeItemHeaderValue(3, 4)); // reverse order of range5
            range6.Ranges.Add(new RangeItemHeaderValue(1, 2));

            Assert.Equal(range1.GetHashCode(), range2.GetHashCode());
            Assert.NotEqual(range1.GetHashCode(), range3.GetHashCode());
            Assert.NotEqual(range1.GetHashCode(), range4.GetHashCode());
            Assert.NotEqual(range1.GetHashCode(), range5.GetHashCode());
            Assert.Equal(range5.GetHashCode(), range6.GetHashCode());
        }

        [Fact]
        public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
        {
            RangeHeaderValue range1 = new RangeHeaderValue(1, 2);
            RangeHeaderValue range2 = new RangeHeaderValue(1, 2);
            range2.Unit = "BYTES";
            RangeHeaderValue range3 = new RangeHeaderValue(1, null);
            RangeHeaderValue range4 = new RangeHeaderValue(null, 2);
            RangeHeaderValue range5 = new RangeHeaderValue();
            range5.Ranges.Add(new RangeItemHeaderValue(1, 2));
            range5.Ranges.Add(new RangeItemHeaderValue(3, 4));
            RangeHeaderValue range6 = new RangeHeaderValue();
            range6.Ranges.Add(new RangeItemHeaderValue(3, 4)); // reverse order of range5
            range6.Ranges.Add(new RangeItemHeaderValue(1, 2));
            RangeHeaderValue range7 = new RangeHeaderValue(1, 2);
            range7.Unit = "other";

            Assert.False(range1.Equals(null), "bytes=1-2 vs. <null>");
            Assert.True(range1.Equals(range2), "bytes=1-2 vs. BYTES=1-2");
            Assert.False(range1.Equals(range3), "bytes=1-2 vs. bytes=1-");
            Assert.False(range1.Equals(range4), "bytes=1-2 vs. bytes=-2");
            Assert.False(range1.Equals(range5), "bytes=1-2 vs. bytes=1-2,3-4");
            Assert.True(range5.Equals(range6), "bytes=1-2,3-4 vs. bytes=3-4,1-2");
            Assert.False(range1.Equals(range7), "bytes=1-2 vs. other=1-2");
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            RangeHeaderValue source = new RangeHeaderValue(1, 2);
            RangeHeaderValue clone = (RangeHeaderValue)((ICloneable)source).Clone();

            Assert.Equal(1, source.Ranges.Count);
            Assert.Equal(source.Unit, clone.Unit);
            Assert.Equal(source.Ranges.Count, clone.Ranges.Count);
            Assert.Equal(source.Ranges.ElementAt(0), clone.Ranges.ElementAt(0));

            source.Unit = "custom";
            source.Ranges.Add(new RangeItemHeaderValue(3, null));
            source.Ranges.Add(new RangeItemHeaderValue(null, 4));
            clone = (RangeHeaderValue)((ICloneable)source).Clone();

            Assert.Equal(3, source.Ranges.Count);
            Assert.Equal(source.Unit, clone.Unit);
            Assert.Equal(source.Ranges.Count, clone.Ranges.Count);
            Assert.Equal(source.Ranges.ElementAt(0), clone.Ranges.ElementAt(0));
            Assert.Equal(source.Ranges.ElementAt(1), clone.Ranges.ElementAt(1));
            Assert.Equal(source.Ranges.ElementAt(2), clone.Ranges.ElementAt(2));
        }

        [Fact]
        public void GetRangeLength_DifferentValidScenarios_AllReturnNonZero()
        {
            RangeHeaderValue result = null;

            CallGetRangeLength(" custom = 1 - 2", 1, 14, out result);
            Assert.Equal("custom", result.Unit);
            Assert.Equal(1, result.Ranges.Count);
            Assert.Equal(new RangeItemHeaderValue(1, 2), result.Ranges.First());

            CallGetRangeLength("bytes =1-2,,3-, , ,-4,,", 0, 23, out result);
            Assert.Equal("bytes", result.Unit);
            Assert.Equal(3, result.Ranges.Count);
            Assert.Equal(new RangeItemHeaderValue(1, 2), result.Ranges.ElementAt(0));
            Assert.Equal(new RangeItemHeaderValue(3, null), result.Ranges.ElementAt(1));
            Assert.Equal(new RangeItemHeaderValue(null, 4), result.Ranges.ElementAt(2));
        }

        [Fact]
        public void GetRangeLength_DifferentInvalidScenarios_AllReturnZero()
        {
            CheckInvalidGetRangeLength(" bytes=1-2", 0); // no leading whitespace allowed
            CheckInvalidGetRangeLength("bytes=1", 0);
            CheckInvalidGetRangeLength("bytes=", 0);
            CheckInvalidGetRangeLength("bytes", 0);
            CheckInvalidGetRangeLength("bytes 1-2", 0);
            CheckInvalidGetRangeLength("bytes=1-2.5", 0);
            CheckInvalidGetRangeLength("bytes= ,,, , ,,", 0);

            CheckInvalidGetRangeLength("", 0);
            CheckInvalidGetRangeLength(null, 0);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParse(" bytes=1-2 ", new RangeHeaderValue(1, 2));

            RangeHeaderValue expected = new RangeHeaderValue();
            expected.Unit = "custom";
            expected.Ranges.Add(new RangeItemHeaderValue(null, 5));
            expected.Ranges.Add(new RangeItemHeaderValue(1, 4));
            CheckValidParse("custom = -  5 , 1 - 4 ,,", expected);
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("bytes=1-2x"); // only delimiter ',' allowed after last range
            CheckInvalidParse("x bytes=1-2");
            CheckInvalidParse("bytes=1-2.4");
            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidTryParse(" bytes=1-2 ", new RangeHeaderValue(1, 2));

            RangeHeaderValue expected = new RangeHeaderValue();
            expected.Unit = "custom";
            expected.Ranges.Add(new RangeItemHeaderValue(null, 5));
            expected.Ranges.Add(new RangeItemHeaderValue(1, 4));
            CheckValidTryParse("custom = -  5 , 1 - 4 ,,", expected);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidTryParse("bytes=1-2x"); // only delimiter ',' allowed after last range
            CheckInvalidTryParse("x bytes=1-2");
            CheckInvalidTryParse("bytes=1-2.4");
            CheckInvalidTryParse(null);
            CheckInvalidTryParse(string.Empty);
        }

        #region Helper methods

        private void CheckValidParse(string input, RangeHeaderValue expectedResult)
        {
            RangeHeaderValue result = RangeHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { RangeHeaderValue.Parse(input); });
        }

        private void CheckValidTryParse(string input, RangeHeaderValue expectedResult)
        {
            RangeHeaderValue result = null;
            Assert.True(RangeHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidTryParse(string input)
        {
            RangeHeaderValue result = null;
            Assert.False(RangeHeaderValue.TryParse(input, out result));
            Assert.Null(result);
        }

        private static void CallGetRangeLength(string input, int startIndex, int expectedLength,
            out RangeHeaderValue result)
        {
            object temp = null;
            Assert.Equal(expectedLength, RangeHeaderValue.GetRangeLength(input, startIndex, out temp));
            result = temp as RangeHeaderValue;
        }

        private static void CheckInvalidGetRangeLength(string input, int startIndex)
        {
            object result = null;
            Assert.Equal(0, RangeHeaderValue.GetRangeLength(input, startIndex, out result));
            Assert.Null(result);
        }
        #endregion
    }
}
