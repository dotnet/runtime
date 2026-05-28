// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class IdnMappingGetAsciiTests
    {
        public static IEnumerable<object[]> GetAscii_TestData()
        {
            for (int i = 0x20; i < 0x7F; i++)
            {
                char c = (char)i;

                // We test '.' separately
                if (c == '.')
                {
                    continue;
                }

                string ascii = c.ToString();
                if (!PlatformDetection.IsIcuGlobalization || c != '-') // expected platform differences, see https://github.com/dotnet/runtime/issues/17190
                {
                    yield return new object[] { ascii, 0, 1, ascii };
                }
            }

            yield return new object[] { "\u0101", 0, 1, "xn--yda" };
            yield return new object[] { "\u0101\u0061\u0041", 0, 3, "xn--aa-cla" };
            yield return new object[] { "\u0061\u0101\u0062", 0, 3, "xn--ab-dla" };
            yield return new object[] { "\u0061\u0062\u0101", 0, 3, "xn--ab-ela" };

            yield return new object[] { "\uD800\uDF00\uD800\uDF01\uD800\uDF02", 0, 6, "xn--097ccd" }; // Surrogate pairs
            yield return new object[] { "\uD800\uDF00\u0061\uD800\uDF01\u0042\uD800\uDF02", 0, 8, "xn--ab-ic6nfag" }; // Surrogate pairs separated by ASCII
            yield return new object[] { "\uD800\uDF00\u0101\uD800\uDF01\u305D\uD800\uDF02", 0, 8, "xn--yda263v6b6kfag" }; // Surrogate pairs separated by non-ASCII
            yield return new object[] { "\uD800\uDF00\u0101\uD800\uDF01\u0061\uD800\uDF02", 0, 8, "xn--a-nha4529qfag" }; // Surrogate pairs separated by ASCII and non-ASCII
            yield return new object[] { "\u0061\u0062\u0063", 0, 3, "\u0061\u0062\u0063" }; // ASCII only code points
            yield return new object[] { "\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067", 0, 7, "xn--d9juau41awczczp" }; // Non-ASCII only code points
            yield return new object[] { "\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 9, "xn--de-jg4avhby1noc0d" }; // ASCII and non-ASCII code points
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 21, "abc.xn--d9juau41awczczp.xn--de-jg4avhby1noc0d" }; // Fully qualified domain name

            // Embedded domain name conversion (NLS + only)(Priority 1)
            // Per the spec [7], "The index and count parameters (when provided) allow the
            // conversion to be done on a larger string where the domain name is embedded
            // (such as a URI or IRI). The output string is only the converted FQDN or
            // label, not the whole input string (if the input string contains more
            // character than the substring to convert)."
            // Fully Qualified Domain Name (Label1.Label2.Label3)
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 21, "abc.xn--d9juau41awczczp.xn--de-jg4avhby1noc0d" };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 11, "abc.xn--d9juau41awczczp" };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 12, "abc.xn--d9juau41awczczp." };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 4, 17, "xn--d9juau41awczczp.xn--de-jg4avhby1noc0d" };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 4, 7, "xn--d9juau41awczczp" };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 4, 8, "xn--d9juau41awczczp." };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 12, 9, "xn--de-jg4avhby1noc0d" };
        }

        [Theory]
        [MemberData(nameof(GetAscii_TestData))]
        public void GetAscii(string unicode, int index, int count, string expected)
        {
            if (index + count == unicode.Length)
            {
                if (index == 0)
                {
                    Assert.Equal(expected, new IdnMapping().GetAscii(unicode));
                }
                Assert.Equal(expected, new IdnMapping().GetAscii(unicode, index));
            }
            Assert.Equal(expected, new IdnMapping().GetAscii(unicode, index, count));
        }

        [Theory]
        [InlineData("www.microsoft.com")]
        [InlineData("bing.com")]
        public void GetAscii_NoTranslationNeeded_ResultIsSameObjectAsInput(string input)
        {
            Assert.Same(input, new IdnMapping().GetAscii(input));
            Assert.NotSame(input, new IdnMapping().GetAscii(input.Substring(1)));
            Assert.NotSame(input, new IdnMapping().GetAscii(input.Substring(0, input.Length - 1)));
        }

        [Fact]
        public void TestGetAsciiWithDot()
        {
            string result = "";
            Exception ex = Record.Exception(()=> result = new IdnMapping().GetAscii("."));
            if (ex == null)
            {
                // Windows and OSX always throw exception. some versions of Linux succeed and others throw exception
                Assert.False(OperatingSystem.IsWindows());
                Assert.False(OperatingSystem.IsMacOS());
                Assert.Equal(".", result);
            }
            else
            {
                Assert.IsType<ArgumentException>(ex);
            }
        }

        public static IEnumerable<object[]> GetAscii_Invalid_TestData()
        {
            // Unicode is null
            yield return new object[] { null, 0, 0, typeof(ArgumentNullException) };
            yield return new object[] { null, -5, -10, typeof(ArgumentNullException) };

            // Index or count are invalid
            yield return new object[] { "abc", -1, 0, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", 0, -1, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", -5, -10, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", 2, 2, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", 4, 99, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", 3, 0, typeof(ArgumentException) };

            // An FQDN/label must not begin with a label separator (it may end with one)
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 3, 18, typeof(ArgumentException) };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 3, 8, typeof(ArgumentException) };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 3, 9, typeof(ArgumentException) };
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 11, 10, typeof(ArgumentException) };

            if (!OperatingSystem.IsWindows())  // expected platform differences, see https://github.com/dotnet/runtime/issues/17190
            {
                if (OperatingSystem.IsMacOS())
                {
                    yield return new object[] { ".", 0, 1, typeof(ArgumentException) };
                }
                yield return new object[] { "-", 0, 1, typeof(ArgumentException) };
            }
            else
            {
                yield return new object[] { ".", 0, 1, typeof(ArgumentException) };
            }

            // Null containing strings
            yield return new object[] { "\u0101\u0000", 0, 2, typeof(ArgumentException) };
            yield return new object[] { "\u0101\u0000\u0101", 0, 3, typeof(ArgumentException) };
            yield return new object[] { "\u0101\u0000\u0101\u0000", 0, 4, typeof(ArgumentException) };

            // Invalid unicode strings
            for (int i = 0; i <= 0x1F; i++)
            {
                yield return new object[] { "abc" + (char)i + "def", 0, 7, typeof(ArgumentException) };
            }

            yield return new object[] { "abc" + (char)0x7F + "def", 0, 7, typeof(ArgumentException) };
        }

        [Theory]
        [MemberData(nameof(GetAscii_Invalid_TestData))]
        public void GetAscii_Invalid(string unicode, int index, int count, Type exceptionType)
        {
            static void getAscii_Invalid(IdnMapping idnMapping, string unicode, int index, int count, Type exceptionType)
            {
                if (unicode == null || index + count == unicode.Length)
                {
                    if (unicode == null || index == 0)
                    {
                        Assert.Throws(exceptionType, () => idnMapping.GetAscii(unicode));
                    }
                    Assert.Throws(exceptionType, () => idnMapping.GetAscii(unicode, index));
                }
                Assert.Throws(exceptionType, () => idnMapping.GetAscii(unicode, index, count));
            }

            getAscii_Invalid(new IdnMapping() { UseStd3AsciiRules = false }, unicode, index, count, exceptionType);
            getAscii_Invalid(new IdnMapping() { UseStd3AsciiRules = true }, unicode, index, count, exceptionType);
        }

        [Fact]
        public void TestStringWithHyphenIn3rdAnd4thPlace()
        {
            string unicode = "r6---sn-uxanug5-hxay.gvt1.com";

            // Ensure we are not throwing on Linux because of the 3rd and 4th hyphens in the string.
            Assert.Equal(unicode, new IdnMapping().GetAscii(unicode));
        }

        [Theory]
        [MemberData(nameof(GetAscii_TestData))]
        public void TryGetAscii(string unicode, int index, int count, string expected)
        {
            var idn = new IdnMapping();
            ReadOnlySpan<char> unicodeSpan = unicode.AsSpan(index, count);

            // Test with exact size buffer
            char[] destination = new char[expected.Length];
            Assert.True(idn.TryGetAscii(unicodeSpan, destination, out int charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            // IDN names are case-insensitive; the underlying API may lowercase the output
            Assert.Equal(expected, new string(destination, 0, charsWritten), StringComparer.OrdinalIgnoreCase);

            // Test with larger buffer
            destination = new char[expected.Length + 10];
            Assert.True(idn.TryGetAscii(unicodeSpan, destination, out charsWritten));
            Assert.Equal(expected.Length, charsWritten);
            Assert.Equal(expected, new string(destination, 0, charsWritten), StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [MemberData(nameof(GetAscii_TestData))]
        public void TryGetAscii_BufferTooSmall(string unicode, int index, int count, string expected)
        {
            if (expected.Length == 0)
            {
                return;
            }

            var idn = new IdnMapping();
            ReadOnlySpan<char> unicodeSpan = unicode.AsSpan(index, count);

            // Test with buffer that is too small
            char[] destination = new char[expected.Length - 1];
            Assert.False(idn.TryGetAscii(unicodeSpan, destination, out int charsWritten));
            Assert.Equal(0, charsWritten);
        }

        [Fact]
        public void TryGetAscii_EmptyBuffer()
        {
            var idn = new IdnMapping();

            // Test with empty destination when result would be non-empty
            Assert.False(idn.TryGetAscii("abc", Span<char>.Empty, out int charsWritten));
            Assert.Equal(0, charsWritten);
        }

        [Theory]
        [InlineData("")]
        public void TryGetAscii_Empty_ThrowsArgumentException(string unicode)
        {
            var idn = new IdnMapping();
            char[] destination = new char[100];
            Assert.Throws<ArgumentException>(() => idn.TryGetAscii(unicode, destination, out _));
        }

        [Theory]
        [InlineData("\u0101\u0000")]
        [InlineData("\u0101\u0000\u0101")]
        public void TryGetAscii_NullContaining_ThrowsArgumentException(string unicode)
        {
            var idn = new IdnMapping();
            char[] destination = new char[100];
            Assert.Throws<ArgumentException>(() => idn.TryGetAscii(unicode, destination, out _));
        }

        [Theory]
        [MemberData(nameof(GetAscii_Std3Compatible_TestData))]
        public void TryGetAscii_WithFlags(string unicode, int index, int count, string expected)
        {
            // Test with UseStd3AsciiRules = true and AllowUnassigned = true
            var idnStd3 = new IdnMapping() { UseStd3AsciiRules = true, AllowUnassigned = true };
            ReadOnlySpan<char> unicodeSpan = unicode.AsSpan(index, count);
            char[] destination = new char[expected.Length + 10];

            Assert.True(idnStd3.TryGetAscii(unicodeSpan, destination, out int charsWritten));
            Assert.Equal(expected, new string(destination, 0, charsWritten), StringComparer.OrdinalIgnoreCase);

            // Test with AllowUnassigned = false (default)
            var idnNoUnassigned = new IdnMapping() { AllowUnassigned = false };
            Assert.True(idnNoUnassigned.TryGetAscii(unicodeSpan, destination, out charsWritten));
            Assert.Equal(expected, new string(destination, 0, charsWritten), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Test data compatible with UseStd3AsciiRules=true (excludes special ASCII characters).
        /// </summary>
        public static IEnumerable<object[]> GetAscii_Std3Compatible_TestData()
        {
            // Only include alphanumeric ASCII and non-ASCII test data that works with Std3 rules
            yield return new object[] { "\u0101", 0, 1, "xn--yda" };
            yield return new object[] { "\u0101\u0061\u0041", 0, 3, "xn--aa-cla" };
            yield return new object[] { "\u0061\u0101\u0062", 0, 3, "xn--ab-dla" };
            yield return new object[] { "\u0061\u0062\u0101", 0, 3, "xn--ab-ela" };
            yield return new object[] { "\uD800\uDF00\uD800\uDF01\uD800\uDF02", 0, 6, "xn--097ccd" }; // Surrogate pairs
            yield return new object[] { "\u0061\u0062\u0063", 0, 3, "\u0061\u0062\u0063" }; // ASCII only code points
            yield return new object[] { "\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067", 0, 7, "xn--d9juau41awczczp" }; // Non-ASCII only code points
            yield return new object[] { "\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 9, "xn--de-jg4avhby1noc0d" }; // ASCII and non-ASCII code points
            yield return new object[] { "\u0061\u0062\u0063.\u305D\u306E\u30B9\u30D4\u30FC\u30C9\u3067.\u30D1\u30D5\u30A3\u30FC\u0064\u0065\u30EB\u30F3\u30D0", 0, 21, "abc.xn--d9juau41awczczp.xn--de-jg4avhby1noc0d" }; // Fully qualified domain name
        }

        [Theory]
        [MemberData(nameof(GetAscii_Invalid_TestData))]
        public void TryGetAscii_Invalid(string unicode, int index, int count, Type exceptionType)
        {
            if (unicode is null)
            {
                return; // TryGetAscii takes ReadOnlySpan<char>, which can't be null
            }

            // Skip entries with invalid index/count (those test the GetAscii(string, int, int) validation, not the span content validation)
            if (index < 0 || count < 0 || index > unicode.Length || index + count > unicode.Length)
            {
                return;
            }

            // Also skip empty count tests - they test ArgumentException for empty string validation
            // but TryGetAscii span-based API doesn't have index/count overloads
            if (count == 0)
            {
                return;
            }

            string slice = unicode.Substring(index, count);
            char[] destination = new char[100];

            var idnNoStd3 = new IdnMapping() { UseStd3AsciiRules = false };
            Assert.Throws(exceptionType, () => idnNoStd3.TryGetAscii(slice, destination, out _));

            var idnStd3 = new IdnMapping() { UseStd3AsciiRules = true };
            Assert.Throws(exceptionType, () => idnStd3.TryGetAscii(slice, destination, out _));
        }

        [Fact]
        public void TryGetAscii_OverlappingBuffers_ThrowsArgumentException()
        {
            var idn = new IdnMapping();
            char[] buffer = new char[100];

            // Write unicode input to the buffer
            string unicode = "\u0101\u0062\u0063"; // "ābc"
            unicode.AsSpan().CopyTo(buffer);

            // Test overlapping: input and destination start at same location
            Assert.Throws<ArgumentException>(() => idn.TryGetAscii(buffer.AsSpan(0, unicode.Length), buffer.AsSpan(0, buffer.Length), out _));

            // Test overlapping: destination starts inside input
            Assert.Throws<ArgumentException>(() => idn.TryGetAscii(buffer.AsSpan(0, unicode.Length), buffer.AsSpan(1, buffer.Length - 1), out _));

            // Test overlapping: input starts inside destination
            unicode.AsSpan().CopyTo(buffer.AsSpan(10));
            Assert.Throws<ArgumentException>(() => idn.TryGetAscii(buffer.AsSpan(10, unicode.Length), buffer.AsSpan(0, buffer.Length), out _));
        }
    }
}
