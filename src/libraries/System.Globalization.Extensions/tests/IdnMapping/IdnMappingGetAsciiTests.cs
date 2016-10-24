// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
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
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // expected platform differences, see https://github.com/dotnet/corefx/issues/8242
                {
                    if ((c >= 'A' && c <= 'Z'))
                    {
                        yield return new object[] { ascii, 0, 1, ascii.ToLower() };
                    }
                    else if (c != '-')
                    {
                        yield return new object[] { ascii, 0, 1, ascii };
                    }
                }
                else
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

        [Fact]
        public void TestGetAsciiWithDot()
        {
            string result = "";
            Exception ex = Record.Exception(()=> result = new IdnMapping().GetAscii("."));
            if (ex == null)
            {
                // Windows and OSX always throw exception. some versions of Linux succeed and others throw exception
                Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
                Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
                Assert.Equal(result, ".");
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

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))  // expected platform differences, see https://github.com/dotnet/corefx/issues/8242
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
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
            GetAscii_Invalid(new IdnMapping() { UseStd3AsciiRules = false }, unicode, index, count, exceptionType);
            GetAscii_Invalid(new IdnMapping() { UseStd3AsciiRules = true }, unicode, index, count, exceptionType);
        }

        public static void GetAscii_Invalid(IdnMapping idnMapping, string unicode, int index, int count, Type exceptionType)
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
    }
}
