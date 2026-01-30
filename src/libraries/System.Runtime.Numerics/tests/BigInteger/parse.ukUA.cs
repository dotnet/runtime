// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Tests;
using System.Text;
using Xunit;

namespace System.Numerics.Tests
{
    public class parseTestUkUA
    {
        [Fact]
        public static void ParseUkrainianCultureWithTrailingSpaces()
        {
            using (new ThreadCultureChange(new CultureInfo("uk-UA")))
            {
                // Test UTF-8 parsing with trailing spaces and AllowThousands
                // Ukrainian culture uses NBSP (0xA0) as NumberGroupSeparator
                // The parser should accept regular space (0x20) as equivalent
                string testNumber = "123 ";
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(testNumber);

                // This should parse successfully with AllowThousands
                BigInteger result = BigInteger.Parse(utf8Bytes, NumberStyles.AllowThousands);
                Assert.Equal(BigInteger.Parse("123"), result);

                // Also test with AllowTrailingWhite
                result = BigInteger.Parse(utf8Bytes, NumberStyles.AllowTrailingWhite);
                Assert.Equal(BigInteger.Parse("123"), result);

                // Test with combined styles
                result = BigInteger.Parse(utf8Bytes, NumberStyles.AllowThousands | NumberStyles.AllowTrailingWhite);
                Assert.Equal(BigInteger.Parse("123"), result);
            }
        }

        [Fact]
        public static void ParseUkrainianCultureWithNumberGroupSeparator()
        {
            using (new ThreadCultureChange(new CultureInfo("uk-UA")))
            {
                // Ukrainian culture uses NBSP (0xA0) as NumberGroupSeparator
                // Test that both NBSP and regular space work in UTF-8 parsing

                // Test with NBSP in input (0xA0)
                string testWithNBSP = "1\u00a0234\u00a0567";
                byte[] utf8WithNBSP = Encoding.UTF8.GetBytes(testWithNBSP);
                BigInteger resultNBSP = BigInteger.Parse(utf8WithNBSP, NumberStyles.AllowThousands);
                Assert.Equal(BigInteger.Parse("1234567"), resultNBSP);

                // Test with regular space in input (0x20)
                string testWithSpace = "1 234 567";
                byte[] utf8WithSpace = Encoding.UTF8.GetBytes(testWithSpace);
                BigInteger resultSpace = BigInteger.Parse(utf8WithSpace, NumberStyles.AllowThousands);
                Assert.Equal(BigInteger.Parse("1234567"), resultSpace);

                // Both should produce the same result
                Assert.Equal(resultNBSP, resultSpace);
            }
        }

        [Fact]
        public static void ParseUkrainianCultureConsistency()
        {
            using (new ThreadCultureChange(new CultureInfo("uk-UA")))
            {
                // Ensure UTF-8 parsing behaves identically to string parsing
                string[] testCases = new[]
                {
                    "123",
                    "123 ",
                    " 123",
                    " 123 ",
                    "1\u00a0234",
                    "1 234",
                    "-123",
                    "+123"
                };

                foreach (string testCase in testCases)
                {
                    byte[] utf8Bytes = Encoding.UTF8.GetBytes(testCase);

                    // Test with different NumberStyles
                    NumberStyles[] stylesToTest = new[]
                    {
                        NumberStyles.Integer,
                        NumberStyles.AllowThousands,
                        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                        NumberStyles.Number
                    };

                    foreach (NumberStyles style in stylesToTest)
                    {
                        bool stringParsed = BigInteger.TryParse(testCase, style, null, out BigInteger stringResult);
                        bool utf8Parsed = BigInteger.TryParse(utf8Bytes, style, null, out BigInteger utf8Result);

                        // UTF-8 and string parsing should have same success/failure
                        Assert.Equal(stringParsed, utf8Parsed);

                        // If both succeeded, results should be equal
                        if (stringParsed && utf8Parsed)
                        {
                            Assert.Equal(stringResult, utf8Result);
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData("1 234 567", NumberStyles.AllowThousands)]
        [InlineData("1\u00a0234\u00a0567", NumberStyles.AllowThousands)]
        [InlineData("-1 234", NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands)]
        [InlineData("+1 234", NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands)]
        [InlineData(" 123 ", NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite)]
        public static void ParseUkrainianCultureVariousFormats(string input, NumberStyles style)
        {
            using (new ThreadCultureChange(new CultureInfo("uk-UA")))
            {
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(input);

                // Both string and UTF-8 parsing should succeed
                BigInteger stringResult = BigInteger.Parse(input, style);
                BigInteger utf8Result = BigInteger.Parse(utf8Bytes, style);

                // Results should be equal
                Assert.Equal(stringResult, utf8Result);
            }
        }
    }
}
