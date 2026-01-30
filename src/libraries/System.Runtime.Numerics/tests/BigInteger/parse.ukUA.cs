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

                // This should parse successfully with AllowTrailingWhite
                BigInteger result = BigInteger.Parse(utf8Bytes, NumberStyles.AllowTrailingWhite);
                Assert.Equal(BigInteger.Parse("123"), result);

                // Also test with string parsing
                result = BigInteger.Parse(testNumber, NumberStyles.AllowTrailingWhite);
                Assert.Equal(BigInteger.Parse("123"), result);
            }
        }

        [Fact]
        public static void ParseUkrainianCultureWithNBSP()
        {
            using (new ThreadCultureChange(new CultureInfo("uk-UA")))
            {
                // Ukrainian culture uses NBSP (0xA0) as NumberGroupSeparator
                // Test that NBSP works in both string and UTF-8 parsing

                // Test with NBSP in input (0xA0)
                string testWithNBSP = "1\u00a0234\u00a0567";
                byte[] utf8WithNBSP = Encoding.UTF8.GetBytes(testWithNBSP);
                BigInteger resultNBSP = BigInteger.Parse(utf8WithNBSP, NumberStyles.AllowThousands);
                Assert.Equal(BigInteger.Parse("1234567"), resultNBSP);

                // Also test string parsing
                BigInteger resultString = BigInteger.Parse(testWithNBSP, NumberStyles.AllowThousands);
                Assert.Equal(BigInteger.Parse("1234567"), resultString);
            }
        }
    }
}
