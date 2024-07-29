// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class IdnMappingGetUnicodeTests
    {
        public static IEnumerable<object[]> GetUnicode_TestData()
        {
            yield return new object[] { "xn--yda", 0, 7, "\u0101" };
            yield return new object[] { "axn--ydab", 1, 7, "\u0101" };

            yield return new object[] { "xn--aa-cla", 0, 10, "\u0101\u0061a" };
            yield return new object[] { "xn--ab-dla", 0, 10, "\u0061\u0101\u0062" };
            yield return new object[] { "xn--ab-ela", 0, 10, "\u0061\u0062\u0101"  };

            yield return new object[] { "xn--097ccd", 0, 10, "\uD800\uDF00\uD800\uDF01\uD800\uDF02" }; // Surrogate pairs
            yield return new object[] { "xn--ab-ic6nfag", 0, 14, "\uD800\uDF00\u0061\uD800\uDF01b\uD800\uDF02" }; // Surrogate pairs separated by ASCII
            yield return new object[] { "xn--yda263v6b6kfag", 0, 18, "\uD800\uDF00\u0101\uD800\uDF01\u305D\uD800\uDF02" }; // Surrogate pairs separated by non-ASCII
            yield return new object[] { "xn--a-nha4529qfag", 0, 17, "\uD800\uDF00\u0101\uD800\uDF01\u0061\uD800\uDF02" }; // Surrogate pairs separated by ASCII and non-ASCII
            yield return new object[] { "\u0061\u0062\u0063", 0, 3, "\u0061\u0062\u0063" }; // ASCII only code points
            yield return new object[] { "xn--d9juau41awczczp", 0, 19, "\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067" }; // Non-ASCII only code points
            yield return new object[] { "xn--de-jg4avhby1noc0d", 0, 21, "\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0" }; // ASCII and non-ASCII code points
            yield return new object[] { "abc.xn--d9juau41awczczp.xn--de-jg4avhby1noc0d", 0, 45, "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0" }; // Fully qualified domain name

            // Embedded domain name conversion (NLS + only)(Priority 1)
            // Per the spec [7], "The index and count parameters (when provided) allow the
            // conversion to be done on a larger string where the domain name is embedded
            // (such as a URI or IRI). The output string is only the converted FQDN or
            // label, not the whole input string (if the input string contains more
            // character than the substring to convert)."
            // Fully Qualified Domain Name (Label1.Label2.Label3)
            yield return new object[] { "abc.xn--d9juau41awczczp.xn--de-jg4avhby1noc0d", 0, 45, "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0" };
            yield return new object[] { "abc.xn--d9juau41awczczp", 0, 23, "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067" };
            yield return new object[] { "abc.xn--d9juau41awczczp.", 0, 24, "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067." };
            yield return new object[] { "xn--d9juau41awczczp.xn--de-jg4avhby1noc0d", 0, 41, "\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0" };
            yield return new object[] { "xn--d9juau41awczczp", 0, 19, "\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067" };
            yield return new object[] { "xn--d9juau41awczczp.", 0, 20, "\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067." };
            yield return new object[] { "xn--de-jg4avhby1noc0d", 0, 21, "\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0" };
        }

        [Theory]
        [MemberData(nameof(GetUnicode_TestData))]
        public void GetUnicode(string ascii, int index, int count, string expected)
        {
            if (index + count == ascii.Length)
            {
                if (index == 0)
                {
                    Assert.Equal(expected, new IdnMapping().GetUnicode(ascii));
                }
                Assert.Equal(expected, new IdnMapping().GetUnicode(ascii, index));
            }
            Assert.Equal(expected, new IdnMapping().GetUnicode(ascii, index, count));
        }

        [Theory]
        [InlineData("www.microsoft.com")]
        [InlineData("bing.com")]
        public void GetUnicode_NoTranslationNeeded_ResultIsSameObjectAsInput(string input)
        {
            Assert.Same(input, new IdnMapping().GetUnicode(input));
            Assert.NotSame(input, new IdnMapping().GetUnicode(input.Substring(1)));
            Assert.NotSame(input, new IdnMapping().GetUnicode(input.Substring(0, input.Length - 1)));
        }

        public static IEnumerable<object[]> GetUnicode_Invalid_TestData()
        {
            // Ascii is null
            yield return new object[] { null, 0, 0, typeof(ArgumentNullException) };
            yield return new object[] { null, -5, -10, typeof(ArgumentNullException) };

            // Index or count are invalid
            yield return new object[] { "abc", -1, 0, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", 0, -1, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", -5, -10, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", 2, 2, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", 4, 99, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", 3, 0, typeof(ArgumentException) };

            // Null containing strings
            yield return new object[] { "abc\u0000", 0, 4, typeof(ArgumentException) };
            yield return new object[] { "ab\u0000c", 0, 4, typeof(ArgumentException) };

            // Invalid unicode strings
            for (int i = 0; i <= 0x1F; i++)
            {
                yield return new object[] { "abc" + (char)i + "def", 0, 7, typeof(ArgumentException) };
            }

            yield return new object[] { "abc" + (char)0x7F + "def", 0, 7, typeof(ArgumentException) };

            if (PlatformDetection.IsNlsGlobalization) // expected platform differences, see https://github.com/dotnet/runtime/issues/17190
            {
                yield return new object[] { "xn--\u1234", 0, 5, typeof(ArgumentException) };
                yield return new object[] { "xn--\u1234pck", 0, 8, typeof(ArgumentException) };
            }
        }

        [Theory]
        [MemberData(nameof(GetUnicode_Invalid_TestData))]
        public void GetUnicode_Invalid(string ascii, int index, int count, Type exceptionType)
        {
            static void getUnicode_Invalid(IdnMapping idnMapping, string ascii, int index, int count, Type exceptionType)
            {
                if (ascii == null || index + count == ascii.Length)
                {
                    if (ascii == null || index == 0)
                    {
                        Assert.Throws(exceptionType, () => idnMapping.GetUnicode(ascii));
                    }
                    Assert.Throws(exceptionType, () => idnMapping.GetUnicode(ascii, index));
                }
                Assert.Throws(exceptionType, () => idnMapping.GetUnicode(ascii, index, count));
            }

            getUnicode_Invalid(new IdnMapping() { UseStd3AsciiRules = false }, ascii, index, count, exceptionType);
            getUnicode_Invalid(new IdnMapping() { UseStd3AsciiRules = true }, ascii, index, count, exceptionType);
        }
    }
}
