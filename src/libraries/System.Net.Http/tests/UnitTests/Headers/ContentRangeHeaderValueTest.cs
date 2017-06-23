// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class ContentRangeHeaderValueTest
    {
        [Fact]
        public void Ctor_LengthOnlyOverloadUseInvalidValues_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { ContentRangeHeaderValue v = new ContentRangeHeaderValue(-1); });
        }

        [Fact]
        public void Ctor_LengthOnlyOverloadValidValues_ValuesCorrectlySet()
        {
            ContentRangeHeaderValue range = new ContentRangeHeaderValue(5);

            Assert.False(range.HasRange);
            Assert.True(range.HasLength);
            Assert.Equal("bytes", range.Unit);
            Assert.Null(range.From);
            Assert.Null(range.To);
            Assert.Equal(5, range.Length);
        }

        [Fact]
        public void Ctor_FromAndToOverloadUseInvalidValues_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { new ContentRangeHeaderValue(-1, 1); }); // "Negative 'from'"
            Assert.Throws<ArgumentOutOfRangeException>(() => { new ContentRangeHeaderValue(0, -1); }); // "Negative 'to'"
            Assert.Throws<ArgumentOutOfRangeException>(() => { new ContentRangeHeaderValue(2, 1); }); // "'from' > 'to'"
        }

        [Fact]
        public void Ctor_FromAndToOverloadValidValues_ValuesCorrectlySet()
        {
            ContentRangeHeaderValue range = new ContentRangeHeaderValue(0, 1);

            Assert.True(range.HasRange);
            Assert.False(range.HasLength);
            Assert.Equal("bytes", range.Unit);
            Assert.Equal(0, range.From);
            Assert.Equal(1, range.To);
            Assert.Null(range.Length);
        }

        [Fact]
        public void Ctor_FromToAndLengthOverloadUseInvalidValues_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { new ContentRangeHeaderValue(-1, 1, 2); }); // "Negative 'from'"
            Assert.Throws<ArgumentOutOfRangeException>(() => { new ContentRangeHeaderValue(0, -1, 2); }); // "Negative 'to'"
            Assert.Throws<ArgumentOutOfRangeException>(() => { new ContentRangeHeaderValue(0, 1, -1); }); // "Negative 'length'"
            Assert.Throws<ArgumentOutOfRangeException>(() => { new ContentRangeHeaderValue(2, 1, 3); }); // "'from' > 'to'"
            Assert.Throws<ArgumentOutOfRangeException>(() => { new ContentRangeHeaderValue(1, 2, 1); }); // "'to' > 'length'"
        }

        [Fact]
        public void Ctor_FromToAndLengthOverloadValidValues_ValuesCorrectlySet()
        {
            ContentRangeHeaderValue range = new ContentRangeHeaderValue(0, 1, 2);

            Assert.True(range.HasRange);
            Assert.True(range.HasLength);
            Assert.Equal("bytes", range.Unit);
            Assert.Equal(0, range.From);
            Assert.Equal(1, range.To);
            Assert.Equal(2, range.Length);
        }

        [Fact]
        public void Unit_GetAndSetValidAndInvalidValues_MatchExpectation()
        {
            ContentRangeHeaderValue range = new ContentRangeHeaderValue(0);
            range.Unit = "myunit";
            Assert.Equal("myunit", range.Unit); // "Unit (custom value)"

            AssertExtensions.Throws<ArgumentException>("value", () => { range.Unit = null; }); // "<null>"
            AssertExtensions.Throws<ArgumentException>("value", () => { range.Unit = ""; }); // "empty string"
            Assert.Throws<FormatException>(() => { range.Unit = " x"; }); // "leading space"
            Assert.Throws<FormatException>(() => { range.Unit = "x "; }); // "trailing space"
            Assert.Throws<FormatException>(() => { range.Unit = "x y"; }); // "invalid token"
        }

        [Fact]
        public void ToString_UseDifferentRanges_AllSerializedCorrectly()
        {
            ContentRangeHeaderValue range = new ContentRangeHeaderValue(1, 2, 3);
            range.Unit = "myunit";
            Assert.Equal("myunit 1-2/3", range.ToString()); // "Range with all fields set"

            range = new ContentRangeHeaderValue(123456789012345678, 123456789012345679);
            Assert.Equal("bytes 123456789012345678-123456789012345679/*", range.ToString()); // "Only range, no length"

            range = new ContentRangeHeaderValue(150);
            Assert.Equal("bytes */150", range.ToString()); // "Only length, no range"
        }

        [Fact]
        public void GetHashCode_UseSameAndDifferentRanges_SameOrDifferentHashCodes()
        {
            ContentRangeHeaderValue range1 = new ContentRangeHeaderValue(1, 2, 5);
            ContentRangeHeaderValue range2 = new ContentRangeHeaderValue(1, 2);
            ContentRangeHeaderValue range3 = new ContentRangeHeaderValue(5);
            ContentRangeHeaderValue range4 = new ContentRangeHeaderValue(1, 2, 5);
            range4.Unit = "BYTES";
            ContentRangeHeaderValue range5 = new ContentRangeHeaderValue(1, 2, 5);
            range5.Unit = "myunit";

            Assert.NotEqual(range1.GetHashCode(), range2.GetHashCode()); // "bytes 1-2/5 vs. bytes 1-2/*"
            Assert.NotEqual(range1.GetHashCode(), range3.GetHashCode()); // "bytes 1-2/5 vs. bytes */5"
            Assert.NotEqual(range2.GetHashCode(), range3.GetHashCode()); // "bytes 1-2/* vs. bytes */5"
            Assert.Equal(range1.GetHashCode(), range4.GetHashCode()); // "bytes 1-2/5 vs. BYTES 1-2/5"
            Assert.NotEqual(range1.GetHashCode(), range5.GetHashCode()); // "bytes 1-2/5 vs. myunit 1-2/5"
        }

        [Fact]
        public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
        {
            ContentRangeHeaderValue range1 = new ContentRangeHeaderValue(1, 2, 5);
            ContentRangeHeaderValue range2 = new ContentRangeHeaderValue(1, 2);
            ContentRangeHeaderValue range3 = new ContentRangeHeaderValue(5);
            ContentRangeHeaderValue range4 = new ContentRangeHeaderValue(1, 2, 5);
            range4.Unit = "BYTES";
            ContentRangeHeaderValue range5 = new ContentRangeHeaderValue(1, 2, 5);
            range5.Unit = "myunit";
            ContentRangeHeaderValue range6 = new ContentRangeHeaderValue(1, 3, 5);
            ContentRangeHeaderValue range7 = new ContentRangeHeaderValue(2, 2, 5);
            ContentRangeHeaderValue range8 = new ContentRangeHeaderValue(1, 2, 6);

            Assert.False(range1.Equals(null)); // "bytes 1-2/5 vs. <null>"
            Assert.False(range1.Equals(range2)); // "bytes 1-2/5 vs. bytes 1-2/*"
            Assert.False(range1.Equals(range3)); // "bytes 1-2/5 vs. bytes */5"
            Assert.False(range2.Equals(range3)); // "bytes 1-2/* vs. bytes */5"
            Assert.True(range1.Equals(range4)); // "bytes 1-2/5 vs. BYTES 1-2/5"
            Assert.True(range4.Equals(range1)); // "BYTES 1-2/5 vs. bytes 1-2/5"
            Assert.False(range1.Equals(range5)); // "bytes 1-2/5 vs. myunit 1-2/5"
            Assert.False(range1.Equals(range6)); // "bytes 1-2/5 vs. bytes 1-3/5"
            Assert.False(range1.Equals(range7)); // "bytes 1-2/5 vs. bytes 2-2/5"
            Assert.False(range1.Equals(range8)); // "bytes 1-2/5 vs. bytes 1-2/6"
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            ContentRangeHeaderValue source = new ContentRangeHeaderValue(1, 2, 5);
            source.Unit = "custom";
            ContentRangeHeaderValue clone = (ContentRangeHeaderValue)((ICloneable)source).Clone();

            Assert.Equal(source.Unit, clone.Unit);
            Assert.Equal(source.From, clone.From);
            Assert.Equal(source.To, clone.To);
            Assert.Equal(source.Length, clone.Length);
        }

        [Fact]
        public void GetContentRangeLength_DifferentValidScenarios_AllReturnNonZero()
        {
            ContentRangeHeaderValue result = null;

            CallGetContentRangeLength("bytes 1-2/3", 0, 11, out result);
            Assert.Equal("bytes", result.Unit);
            Assert.Equal(1, result.From);
            Assert.Equal(2, result.To);
            Assert.Equal(3, result.Length);
            Assert.True(result.HasRange);
            Assert.True(result.HasLength);

            CallGetContentRangeLength(" custom 1234567890123456789-1234567890123456799/*", 1, 48, out result);
            Assert.Equal("custom", result.Unit);
            Assert.Equal(1234567890123456789, result.From);
            Assert.Equal(1234567890123456799, result.To);
            Assert.Null(result.Length);
            Assert.True(result.HasRange);
            Assert.False(result.HasLength);

            // Note that the final space should be skipped by GetContentRangeLength() and be considered by the returned
            // value.            
            CallGetContentRangeLength(" custom * / 123 ", 1, 15, out result);
            Assert.Equal("custom", result.Unit);
            Assert.Null(result.From);
            Assert.Null(result.To);
            Assert.Equal(123, result.Length);
            Assert.False(result.HasRange);
            Assert.True(result.HasLength);

            // Note that we don't have a public constructor for value 'bytes */*' since the RFC doesn't mention a 
            // scenario for it. However, if a server returns this value, we're flexible and accept it.
            CallGetContentRangeLength("bytes */*", 0, 9, out result);
            Assert.Equal("bytes", result.Unit);
            Assert.Null(result.From);
            Assert.Null(result.To);
            Assert.Null(result.Length);
            Assert.False(result.HasRange);
            Assert.False(result.HasLength);
        }

        [Fact]
        public void GetContentRangeLength_DifferentInvalidScenarios_AllReturnZero()
        {
            CheckInvalidGetContentRangeLength(" bytes 1-2/3", 0);
            CheckInvalidGetContentRangeLength("bytes 3-2/5", 0);
            CheckInvalidGetContentRangeLength("bytes 6-6/5", 0);
            CheckInvalidGetContentRangeLength("bytes 1-6/5", 0);
            CheckInvalidGetContentRangeLength("bytes 1-2/", 0);
            CheckInvalidGetContentRangeLength("bytes 1-2", 0);
            CheckInvalidGetContentRangeLength("bytes 1-/", 0);
            CheckInvalidGetContentRangeLength("bytes 1-", 0);
            CheckInvalidGetContentRangeLength("bytes 1", 0);
            CheckInvalidGetContentRangeLength("bytes ", 0);
            CheckInvalidGetContentRangeLength("bytes a-2/3", 0);
            CheckInvalidGetContentRangeLength("bytes 1-b/3", 0);
            CheckInvalidGetContentRangeLength("bytes 1-2/c", 0);
            CheckInvalidGetContentRangeLength("bytes1-2/3", 0);

            // More than 19 digits >>Int64.MaxValue
            CheckInvalidGetContentRangeLength("bytes 1-12345678901234567890/3", 0);
            CheckInvalidGetContentRangeLength("bytes 12345678901234567890-3/3", 0);
            CheckInvalidGetContentRangeLength("bytes 1-2/12345678901234567890", 0);

            // Exceed Int64.MaxValue, but use 19 digits
            CheckInvalidGetContentRangeLength("bytes 1-9999999999999999999/3", 0);
            CheckInvalidGetContentRangeLength("bytes 9999999999999999999-3/3", 0);
            CheckInvalidGetContentRangeLength("bytes 1-2/9999999999999999999", 0);

            CheckInvalidGetContentRangeLength(string.Empty, 0);
            CheckInvalidGetContentRangeLength(null, 0);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            // Only verify parser functionality (i.e. ContentRangeHeaderParser.TryParse()). We don't need to validate
            // all possible range values (verification done by tests for ContentRangeHeaderValue.GetContentRangeLength()).
            CheckValidParse(" bytes 1-2/3 ", new ContentRangeHeaderValue(1, 2, 3));
            CheckValidParse("bytes  *  /  3", new ContentRangeHeaderValue(3));
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("bytes 1-2/3,"); // no character after 'length' allowed
            CheckInvalidParse("x bytes 1-2/3");
            CheckInvalidParse("bytes 1-2/3.4");
            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            // Only verify parser functionality (i.e. ContentRangeHeaderParser.TryParse()). We don't need to validate
            // all possible range values (verification done by tests for ContentRangeHeaderValue.GetContentRangeLength()).
            CheckValidTryParse(" bytes 1-2/3 ", new ContentRangeHeaderValue(1, 2, 3));
            CheckValidTryParse("bytes  *  /  3", new ContentRangeHeaderValue(3));
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidTryParse("bytes 1-2/3,"); // no character after 'length' allowed
            CheckInvalidTryParse("x bytes 1-2/3");
            CheckInvalidTryParse("bytes 1-2/3.4");
            CheckInvalidTryParse(null);
            CheckInvalidTryParse(string.Empty);
        }

        #region Helper methods

        private void CheckValidParse(string input, ContentRangeHeaderValue expectedResult)
        {
            ContentRangeHeaderValue result = ContentRangeHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { ContentRangeHeaderValue.Parse(input); });
        }

        private void CheckValidTryParse(string input, ContentRangeHeaderValue expectedResult)
        {
            ContentRangeHeaderValue result = null;
            Assert.True(ContentRangeHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidTryParse(string input)
        {
            ContentRangeHeaderValue result = null;
            Assert.False(ContentRangeHeaderValue.TryParse(input, out result));
            Assert.Null(result);
        }

        private static void CallGetContentRangeLength(string input, int startIndex, int expectedLength,
            out ContentRangeHeaderValue result)
        {
            object temp = null;
            Assert.Equal(expectedLength, ContentRangeHeaderValue.GetContentRangeLength(input, startIndex, out temp));
            result = temp as ContentRangeHeaderValue;
        }

        private static void CheckInvalidGetContentRangeLength(string input, int startIndex)
        {
            object result = null;
            Assert.Equal(0, ContentRangeHeaderValue.GetContentRangeLength(input, startIndex, out result));
            Assert.Null(result);
        }
        #endregion
    }
}
