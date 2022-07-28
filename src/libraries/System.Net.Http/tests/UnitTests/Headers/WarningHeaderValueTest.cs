// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class WarningHeaderValueTest
    {
        [Fact]
        public void Ctor_3ParamsOverload_AllFieldsInitializedCorrectly()
        {
            WarningHeaderValue warning = new WarningHeaderValue(111, ".host", "\"Revalidation failed\"");
            Assert.Equal(111, warning.Code);
            Assert.Equal(".host", warning.Agent);
            Assert.Equal("\"Revalidation failed\"", warning.Text);
            Assert.Null(warning.Date);

            warning = new WarningHeaderValue(112, "[::1]", "\"Disconnected operation\"");
            Assert.Equal(112, warning.Code);
            Assert.Equal("[::1]", warning.Agent);
            Assert.Equal("\"Disconnected operation\"", warning.Text);
            Assert.Null(warning.Date);

            Assert.Throws<ArgumentOutOfRangeException>(() => { new WarningHeaderValue(-1, "host", "\"\""); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { new WarningHeaderValue(1000, "host", "\"\""); });

            AssertExtensions.Throws<ArgumentException>("agent", () => { new WarningHeaderValue(100, null, "\"\""); });
            AssertExtensions.Throws<ArgumentException>("agent", () => { new WarningHeaderValue(100, "", "\"\""); });
            Assert.Throws<FormatException>(() => { new WarningHeaderValue(100, "x y", "\"\""); });
            Assert.Throws<FormatException>(() => { new WarningHeaderValue(100, "x ", "\"\""); });
            Assert.Throws<FormatException>(() => { new WarningHeaderValue(100, " x", "\"\""); });

            AssertExtensions.Throws<ArgumentException>("agent", () => { new WarningHeaderValue(100, null, "\"\""); });
            AssertExtensions.Throws<ArgumentException>("agent", () => { new WarningHeaderValue(100, "", "\"\""); });
            Assert.Throws<FormatException>(() => { new WarningHeaderValue(100, "h", "x"); });
            Assert.Throws<FormatException>(() => { new WarningHeaderValue(100, "h", "\"x"); });
        }

        [Fact]
        public void Ctor_4ParamsOverload_AllFieldsInitializedCorrectly()
        {
            WarningHeaderValue warning = new WarningHeaderValue(111, "host", "\"Revalidation failed\"",
                new DateTimeOffset(2010, 7, 19, 17, 9, 15, TimeSpan.Zero));
            Assert.Equal(111, warning.Code);
            Assert.Equal("host", warning.Agent);
            Assert.Equal("\"Revalidation failed\"", warning.Text);
            Assert.Equal(new DateTimeOffset(2010, 7, 19, 17, 9, 15, TimeSpan.Zero), warning.Date);

            Assert.Throws<ArgumentOutOfRangeException>(() => { new WarningHeaderValue(-1, "host", "\"\""); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { new WarningHeaderValue(1000, "host", "\"\""); });

            AssertExtensions.Throws<ArgumentException>("agent", () => { new WarningHeaderValue(100, null, "\"\""); });
            AssertExtensions.Throws<ArgumentException>("agent", () => { new WarningHeaderValue(100, "", "\"\""); });
            Assert.Throws<FormatException>(() => { new WarningHeaderValue(100, "[::1]:80(x)", "\"\""); });
            Assert.Throws<FormatException>(() => { new WarningHeaderValue(100, "host::80", "\"\""); });
            Assert.Throws<FormatException>(() => { new WarningHeaderValue(100, "192.168.0.1=", "\"\""); });

            AssertExtensions.Throws<ArgumentException>("agent", () => { new WarningHeaderValue(100, null, "\"\""); });
            AssertExtensions.Throws<ArgumentException>("agent", () => { new WarningHeaderValue(100, "", "\"\""); });
            Assert.Throws<FormatException>(() => { new WarningHeaderValue(100, "h", "(x)"); });
            Assert.Throws<FormatException>(() => { new WarningHeaderValue(100, "h", "\"x\"y"); });
        }

        [Fact]
        public void ToString_UseDifferentRanges_AllSerializedCorrectly()
        {
            WarningHeaderValue warning = new WarningHeaderValue(113, "host:80", "\"Heuristic expiration\"");
            Assert.Equal("113 host:80 \"Heuristic expiration\"", warning.ToString());

            warning = new WarningHeaderValue(199, "[::1]", "\"Miscellaneous warning\"",
                new DateTimeOffset(2010, 7, 19, 18, 31, 27, TimeSpan.Zero));
            Assert.Equal("199 [::1] \"Miscellaneous warning\" \"Mon, 19 Jul 2010 18:31:27 GMT\"", warning.ToString());
        }

        [Fact]
        public void GetHashCode_UseSameAndDifferentRanges_SameOrDifferentHashCodes()
        {
            WarningHeaderValue warning1 = new WarningHeaderValue(214, "host", "\"Transformation applied\"");
            WarningHeaderValue warning2 = new WarningHeaderValue(214, "HOST", "\"Transformation applied\"");
            WarningHeaderValue warning3 = new WarningHeaderValue(215, "host", "\"Transformation applied\"");
            WarningHeaderValue warning4 = new WarningHeaderValue(214, "other", "\"Transformation applied\"");
            WarningHeaderValue warning5 = new WarningHeaderValue(214, "host", "\"TRANSFORMATION APPLIED\"");
            WarningHeaderValue warning6 = new WarningHeaderValue(214, "host", "\"Transformation applied\"",
                new DateTimeOffset(2010, 7, 19, 18, 31, 27, TimeSpan.Zero));
            WarningHeaderValue warning7 = new WarningHeaderValue(214, "host", "\"Transformation applied\"",
                new DateTimeOffset(2011, 7, 19, 18, 31, 27, TimeSpan.Zero));
            WarningHeaderValue warning8 = new WarningHeaderValue(214, "host", "\"Transformation applied\"",
                new DateTimeOffset(2010, 7, 19, 18, 31, 27, TimeSpan.Zero));

            Assert.Equal(warning1.GetHashCode(), warning2.GetHashCode());
            Assert.NotEqual(warning1.GetHashCode(), warning3.GetHashCode());
            Assert.NotEqual(warning1.GetHashCode(), warning4.GetHashCode());
            Assert.NotEqual(warning1.GetHashCode(), warning6.GetHashCode());
            Assert.NotEqual(warning1.GetHashCode(), warning7.GetHashCode());
            Assert.NotEqual(warning6.GetHashCode(), warning7.GetHashCode());
            Assert.Equal(warning6.GetHashCode(), warning8.GetHashCode());
        }

        [Fact]
        public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
        {
            WarningHeaderValue warning1 = new WarningHeaderValue(214, "host", "\"Transformation applied\"");
            WarningHeaderValue warning2 = new WarningHeaderValue(214, "HOST", "\"Transformation applied\"");
            WarningHeaderValue warning3 = new WarningHeaderValue(215, "host", "\"Transformation applied\"");
            WarningHeaderValue warning4 = new WarningHeaderValue(214, "other", "\"Transformation applied\"");
            WarningHeaderValue warning5 = new WarningHeaderValue(214, "host", "\"TRANSFORMATION APPLIED\"");
            WarningHeaderValue warning6 = new WarningHeaderValue(214, "host", "\"Transformation applied\"",
                new DateTimeOffset(2010, 7, 19, 18, 31, 27, TimeSpan.Zero));
            WarningHeaderValue warning7 = new WarningHeaderValue(214, "host", "\"Transformation applied\"",
                new DateTimeOffset(2011, 7, 19, 18, 31, 27, TimeSpan.Zero));
            WarningHeaderValue warning8 = new WarningHeaderValue(214, "host", "\"Transformation applied\"",
                new DateTimeOffset(2010, 7, 19, 18, 31, 27, TimeSpan.Zero));

            Assert.False(warning1.Equals(null), "214 host \"t.a.\" vs. <null>");
            Assert.True(warning1.Equals(warning2), "214 host \"t.a.\" vs. 214 HOST \"t.a.\"");
            Assert.False(warning1.Equals(warning3), "214 host \"t.a.\" vs. 215 host \"t.a.\"");
            Assert.False(warning1.Equals(warning4), "214 host \"t.a.\" vs. 214 other \"t.a.\"");
            Assert.False(warning1.Equals(warning6), "214 host \"t.a.\" vs. 214 host \"T.A.\"");
            Assert.False(warning1.Equals(warning7), "214 host \"t.a.\" vs. 214 host \"t.a.\" \"D\"");
            Assert.False(warning7.Equals(warning1), "214 host \"t.a.\" \"D\" vs. 214 host \"t.a.\"");
            Assert.False(warning6.Equals(warning7), "214 host \"t.a.\" \"D\" vs. 214 host \"t.a.\" \"other D\"");
            Assert.True(warning6.Equals(warning8), "214 host \"t.a.\" \"D\" vs. 214 host \"t.a.\" \"D\"");
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            WarningHeaderValue source = new WarningHeaderValue(299, "host", "\"Miscellaneous persistent warning\"");
            WarningHeaderValue clone = (WarningHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Code, clone.Code);
            Assert.Equal(source.Agent, clone.Agent);
            Assert.Equal(source.Text, clone.Text);
            Assert.Equal(source.Date, clone.Date);

            source = new WarningHeaderValue(110, "host", "\"Response is stale\"",
                new DateTimeOffset(2010, 7, 31, 15, 37, 05, TimeSpan.Zero));
            clone = (WarningHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Code, clone.Code);
            Assert.Equal(source.Agent, clone.Agent);
            Assert.Equal(source.Text, clone.Text);
            Assert.Equal(source.Date, clone.Date);
        }

        [Fact]
        public void GetWarningLength_DifferentValidScenarios_AllReturnNonZero()
        {
            CheckGetWarningLength(" 199 .host \"Miscellaneous warning\"  ", 1, 35,
                new WarningHeaderValue(199, ".host", "\"Miscellaneous warning\""));
            CheckGetWarningLength("987 [FE18:AB64::156]:80 \"\" \"Tue, 20 Jul 2010 01:02:03 GMT\"", 0, 58,
                new WarningHeaderValue(987, "[FE18:AB64::156]:80", "\"\"",
                    new DateTimeOffset(2010, 7, 20, 1, 2, 3, TimeSpan.Zero)));

            // The parser reads until it reaches an invalid/unexpected character. If until then it was able to create
            // a valid WarningHeaderValue, it will return the length of the parsed string. Therefore a string like
            // the following is considered valid (until '[')
            CheckGetWarningLength("1 h \"t\"[", 0, 7, new WarningHeaderValue(1, "h", "\"t\""));
            CheckGetWarningLength("1 h \"t\" \"Tue, 20 Jul 2010 01:02:03 GMT\"[", 0, 39,
                new WarningHeaderValue(1, "h", "\"t\"",
                    new DateTimeOffset(2010, 7, 20, 1, 2, 3, TimeSpan.Zero)));

            CheckGetWarningLength(null, 0, 0, null);
            CheckGetWarningLength(string.Empty, 0, 0, null);
            CheckGetWarningLength("  ", 0, 0, null);
        }

        [Fact]
        public void GetWarningLength_DifferentInvalidScenarios_AllReturnZero()
        {
            CheckInvalidWarningViaLength(" 123 host", 0); // no leading whitespace allowed

            // No delimiter between two values
            CheckInvalidWarningViaLength("123host \"t\"", 0);
            CheckInvalidWarningViaLength("123 host\"t\"", 0);
            CheckInvalidWarningViaLength("123 host \"t\"\"Tue, 20 Jul 2010 01:02:03 GMT\"", 0);
            CheckInvalidWarningViaLength("123 http://host \"t\"", 0);

            CheckInvalidWarningViaLength("1=host \"t\"", 0);
            CheckInvalidWarningViaLength("1.1 host \"invalid_quoted_string", 0);
            CheckInvalidWarningViaLength("=", 0);
            CheckInvalidWarningViaLength("121 host=\"t\"", 0);
            CheckInvalidWarningViaLength("121 host= \"t\"", 0);
            CheckInvalidWarningViaLength("121 host =\"t\"", 0);
            CheckInvalidWarningViaLength("121 host = \"t\"", 0);
            CheckInvalidWarningViaLength("121 host =", 0);
            CheckInvalidWarningViaLength("121 = \"t\"", 0);
            CheckInvalidWarningViaLength("123", 0);
            CheckInvalidWarningViaLength("123 host", 0);
            CheckInvalidWarningViaLength("  ", 0);
            CheckInvalidWarningViaLength("123 example.com[ \"t\"", 0);
            CheckInvalidWarningViaLength("123 / \"t\"", 0);
            CheckInvalidWarningViaLength("123 host::80 \"t\"", 0);
            CheckInvalidWarningViaLength("123 host/\"t\"", 0);
            CheckInvalidWarningViaLength("123 host \"t\" \"Tue, 20 Jul 2010 01:02:03 GMT", 0);
            CheckInvalidWarningViaLength("123 host \"t\" \"Tue, 200 Jul 2010 01:02:03 GMT\"", 0);
            CheckInvalidWarningViaLength("123 host \"t\" \"\"", 0);

            CheckInvalidWarningViaLength(" 199 .host \r\n \"Miscellaneous warning\" \r\n ", 0);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParse(" 123   host \"text\"", new WarningHeaderValue(123, "host", "\"text\""));
            CheckValidParse(" 50  192.168.0.1  \"text  \"  \"Tue, 20 Jul 2010 01:02:03 GMT\" ",
                new WarningHeaderValue(50, "192.168.0.1", "\"text  \"",
                    new DateTimeOffset(2010, 7, 20, 1, 2, 3, TimeSpan.Zero)));
            CheckValidParse(" 123 h \"t\"", new WarningHeaderValue(123, "h", "\"t\""));
            CheckValidParse("1 h \"t\"", new WarningHeaderValue(1, "h", "\"t\""));
            CheckValidParse("1 h \"t\" \"Tue, 20 Jul 2010 01:02:03 GMT\"",
                new WarningHeaderValue(1, "h", "\"t\"",
                    new DateTimeOffset(2010, 7, 20, 1, 2, 3, TimeSpan.Zero)));
            CheckValidParse("1 \u4F1A \"t\" ", new WarningHeaderValue(1, "\u4F1A", "\"t\""));
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("1.1 host \"text\"");
            CheckInvalidParse("11 host text");
            CheckInvalidParse("11 host \"text\" Tue, 20 Jul 2010 01:02:03 GMT");
            CheckInvalidParse("11 host \"text\" 123 next \"text\"");
            CheckInvalidParse("\u4F1A");
            CheckInvalidParse("123 \u4F1A");
            CheckInvalidParse("111 [::1]:80\r(comment) \"text\"");
            CheckInvalidParse("111 [::1]:80\n(comment) \"text\"");

            CheckInvalidParse("X , , 123   host \"text\", ,next");
            CheckInvalidParse("X 50  192.168.0.1  \"text  \"  \"Tue, 20 Jul 2010 01:02:03 GMT\" , ,next");
            CheckInvalidParse(" ,123 h \"t\",");
            CheckInvalidParse("1 \u4F1A \"t\" ,,");

            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
            CheckInvalidParse("  ");
            CheckInvalidParse("  ,,");
        }

        #region Helper methods

        private void CheckValidParse(string input, WarningHeaderValue expectedResult)
        {
            WarningHeaderValue result = WarningHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);

            Assert.True(WarningHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { WarningHeaderValue.Parse(input); });

            Assert.False(WarningHeaderValue.TryParse(input, out WarningHeaderValue result));
            Assert.Null(result);
        }

        private static void CheckGetWarningLength(string input, int startIndex, int expectedLength,
            WarningHeaderValue expectedResult)
        {
            object result = null;
            Assert.Equal(expectedLength, WarningHeaderValue.GetWarningLength(input, startIndex, out result));
            Assert.Equal(expectedResult, result);
        }

        private static void CheckInvalidWarningViaLength(string input, int startIndex)
        {
            object result = null;
            Assert.Equal(0, WarningHeaderValue.GetWarningLength(input, startIndex, out result));
            Assert.Null(result);
        }
        #endregion
    }
}
