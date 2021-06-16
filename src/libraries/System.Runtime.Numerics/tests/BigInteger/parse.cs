// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Tests;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Numerics.Tests
{
    public class parseTest
    {
        private static readonly int s_samples = 10;
        private static readonly Random s_random = new Random(100);

        // Invariant culture is commonly used for (de-)serialization and similar to en-US
        // Ukrainian (Ukraine) added to catch regressions (https://github.com/dotnet/runtime/issues/14545)
        // Current cultue to get additional value out of glob/loc test runs
        public static IEnumerable<object[]> Cultures
        {
            get
            {
                yield return new object[] { CultureInfo.InvariantCulture };
                yield return new object[] { new CultureInfo("uk-UA") };

                if (CultureInfo.CurrentCulture.ToString() != "uk-UA")
                    yield return new object[] { CultureInfo.CurrentCulture };
            }
        }

        [Theory]
        [MemberData(nameof(Cultures))]
        [OuterLoop]
        public static void RunParseToStringTests(CultureInfo culture)
        {
            byte[] tempByteArray1 = new byte[0];
            using (new ThreadCultureChange(culture))
            {
                //default style
                VerifyDefaultParse(s_random);

                //single NumberStyles
                VerifyNumberStyles(NumberStyles.None, s_random);
                VerifyNumberStyles(NumberStyles.AllowLeadingWhite, s_random);
                VerifyNumberStyles(NumberStyles.AllowTrailingWhite, s_random);
                VerifyNumberStyles(NumberStyles.AllowLeadingSign, s_random);
                VerifyNumberStyles(NumberStyles.AllowTrailingSign, s_random);
                VerifyNumberStyles(NumberStyles.AllowParentheses, s_random);
                VerifyNumberStyles(NumberStyles.AllowDecimalPoint, s_random);
                VerifyNumberStyles(NumberStyles.AllowThousands, s_random);
                VerifyNumberStyles(NumberStyles.AllowExponent, s_random);
                VerifyNumberStyles(NumberStyles.AllowCurrencySymbol, s_random);
                VerifyNumberStyles(NumberStyles.AllowHexSpecifier, s_random);

                //composite NumberStyles
                VerifyNumberStyles(NumberStyles.Integer, s_random);
                VerifyNumberStyles(NumberStyles.HexNumber, s_random);
                VerifyNumberStyles(NumberStyles.Number, s_random);
                VerifyNumberStyles(NumberStyles.Float, s_random);
                VerifyNumberStyles(NumberStyles.Currency, s_random);
                VerifyNumberStyles(NumberStyles.Any, s_random);

                //invalid number style
                // ******InvalidNumberStyles
                NumberStyles invalid = (NumberStyles)0x7c00;
                AssertExtensions.Throws<ArgumentException>(null, () =>
                {
                    BigInteger.Parse("1", invalid).ToString("d");
                });
                AssertExtensions.Throws<ArgumentException>(null, () =>
                {
                    BigInteger junk;
                    BigInteger.TryParse("1", invalid, null, out junk);
                    Assert.Equal("1", junk.ToString("d"));
                });

                //FormatProvider tests
                RunFormatProviderParseStrings();
            }
        }

        [Theory]
        [InlineData("123456789", 0, 9, "123456789")]
        [InlineData("123456789", 0, 1, "1")]
        [InlineData("123456789", 1, 3, "234")]
        [InlineData("123456789", 8, 1, "9")]
        [InlineData("123456789abc", 8, 1, "9")]
        [InlineData("1\03456789", 0, 1, "1")]
        [InlineData("1\03456789", 0, 2, "1")]
        [InlineData("123456789\0", 0, 10, "123456789")]
        public void Parse_Subspan_Success(string input, int offset, int length, string expected)
        {
            Eval(BigInteger.Parse(input.AsSpan(offset, length)), expected);
            Assert.True(BigInteger.TryParse(input.AsSpan(offset, length), out BigInteger test));
            Eval(test, expected);
        }

        [Fact]
        public void Parse_EmptySubspan_Fails()
        {
            Assert.False(BigInteger.TryParse("12345".AsSpan(0, 0), out BigInteger result));
            Assert.Equal(0, result);
        }

        private static void RunFormatProviderParseStrings()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi = MarkUp(nfi);

            //Currencies
            // ***************************
            // *** FormatProvider - Currencies
            // ***************************
            VerifyFormatParse("@ 12#34#56!", NumberStyles.Any, nfi, new BigInteger(123456));
            VerifyFormatParse("(12#34#56!@)", NumberStyles.Any, nfi, new BigInteger(-123456));

            //Numbers
            // ***************************
            // *** FormatProvider - Numbers
            // ***************************
            VerifySimpleFormatParse(">1234567", nfi, new BigInteger(1234567));
            VerifySimpleFormatParse("<1234567", nfi, new BigInteger(-1234567));
            VerifyFormatParse("123&4567^", NumberStyles.Any, nfi, new BigInteger(1234567));
            VerifyFormatParse("123&4567^ <", NumberStyles.Any, nfi, new BigInteger(-1234567));
        }

        private static bool NoGrouping(int[] sizes) => sizes.Length == 0 || (sizes.Length == 1 && sizes[0] == 0);

        private static void VerifyDefaultParse(Random random)
        {
            // BasicTests
            VerifyFailParseToString(null, typeof(ArgumentNullException));
            VerifyFailParseToString(string.Empty, typeof(FormatException));
            VerifyParseToString("0");
            VerifyParseToString("000");
            VerifyParseToString("1");
            VerifyParseToString("001");

            // SimpleNumbers - Small
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString(GetDigitSequence(1, 10, random));
            }

            // SimpleNumbers - Large
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString(GetDigitSequence(100, 1000, random));
            }

            // Leading White
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString("\u0009\u0009\u0009" + GetDigitSequence(1, 100, random));
                VerifyParseToString("\u000A\u000A\u000A" + GetDigitSequence(1, 100, random));
                VerifyParseToString("\u000B\u000B\u000B" + GetDigitSequence(1, 100, random));
                VerifyParseToString("\u000C\u000C\u000C" + GetDigitSequence(1, 100, random));
                VerifyParseToString("\u000D\u000D\u000D" + GetDigitSequence(1, 100, random));
                VerifyParseToString("\u0020\u0020\u0020" + GetDigitSequence(1, 100, random));
            }

            // Trailing White
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString(GetDigitSequence(1, 100, random) + "\u0009\u0009\u0009");
                VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000A\u000A\u000A");
                VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000B\u000B\u000B");
                VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000C\u000C\u000C");
                VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000D\u000D\u000D");
                VerifyParseToString(GetDigitSequence(1, 100, random) + "\u0020\u0020\u0020");
            }

            // Leading Sign
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString(CultureInfo.CurrentCulture.NumberFormat.NegativeSign + GetDigitSequence(1, 100, random));
                VerifyParseToString(CultureInfo.CurrentCulture.NumberFormat.PositiveSign + GetDigitSequence(1, 100, random));
            }

            // Trailing Sign
            for (int i = 0; i < s_samples; i++)
            {
                VerifyFailParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentCulture.NumberFormat.NegativeSign, typeof(FormatException));
                VerifyFailParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentCulture.NumberFormat.PositiveSign, typeof(FormatException));
            }

            // Parentheses
            for (int i = 0; i < s_samples; i++)
            {
                VerifyFailParseToString("(" + GetDigitSequence(1, 100, random) + ")", typeof(FormatException));
            }

            // Decimal Point - end
            for (int i = 0; i < s_samples; i++)
            {
                VerifyFailParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, typeof(FormatException));
            }

            // Decimal Point - middle
            for (int i = 0; i < s_samples; i++)
            {
                VerifyFailParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "000", typeof(FormatException));
            }

            // Decimal Point - non-zero decimal
            for (int i = 0; i < s_samples; i++)
            {
                VerifyFailParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + GetDigitSequence(20, 25, random), typeof(FormatException));
            }

            // Thousands
            for (int i = 0; i < s_samples; i++)
            {
                int[] sizes = null;
                string seperator = null;
                string digits = null;

                sizes = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSizes;
                seperator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
                digits = GenerateGroups(sizes, seperator, random);
                if (NoGrouping(sizes))
                {
                    VerifyParseToString(digits);
                }
                else
                {
                    VerifyFailParseToString(digits, typeof(FormatException));
                }
            }

            // Exponent
            for (int i = 0; i < s_samples; i++)
            {
                VerifyFailParseToString(GetDigitSequence(1, 100, random) + "e" + CultureInfo.CurrentCulture.NumberFormat.PositiveSign + GetDigitSequence(1, 3, random), typeof(FormatException));
                VerifyFailParseToString(GetDigitSequence(1, 100, random) + "e" + CultureInfo.CurrentCulture.NumberFormat.NegativeSign + GetDigitSequence(1, 3, random), typeof(FormatException));
            }

            // Currency Symbol
            for (int i = 0; i < s_samples; i++)
            {
                VerifyFailParseToString(CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol + GetDigitSequence(1, 100, random), typeof(FormatException));
            }

            // Hex Specifier
            for (int i = 0; i < s_samples; i++)
            {
                VerifyFailParseToString(GetHexDigitSequence(1, 100, random), typeof(FormatException));
            }

            // Invalid Chars
            for (int i = 0; i < s_samples; i++)
            {
                VerifyFailParseToString(GetDigitSequence(1, 50, random) + GetRandomInvalidChar(random) + GetDigitSequence(1, 50, random), typeof(FormatException));
            }
        }

        private static void VerifyNumberStyles(NumberStyles ns, Random random)
        {
            VerifyParseToString(null, ns, false, null);
            VerifyParseToString(string.Empty, ns, false);
            VerifyParseToString("0", ns, true);
            VerifyParseToString("000", ns, true);
            VerifyParseToString("1", ns, true);
            VerifyParseToString("001", ns, true);

            // SimpleNumbers - Small
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString(GetDigitSequence(1, 10, random), ns, true);
            }

            // SimpleNumbers - Large
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString(GetDigitSequence(100, 1000, random), ns, true);
            }

            // Leading White
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString("\u0009\u0009\u0009" + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingWhite) != 0));
                VerifyParseToString("\u000A\u000A\u000A" + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingWhite) != 0));
                VerifyParseToString("\u000B\u000B\u000B" + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingWhite) != 0));
                VerifyParseToString("\u000C\u000C\u000C" + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingWhite) != 0));
                VerifyParseToString("\u000D\u000D\u000D" + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingWhite) != 0));
                VerifyParseToString("\u0020\u0020\u0020" + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingWhite) != 0));
            }

            // Trailing White
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString(GetDigitSequence(1, 100, random) + "\u0009\u0009\u0009", ns, FailureNotExpectedForTrailingWhite(ns, false));
                VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000A\u000A\u000A", ns, FailureNotExpectedForTrailingWhite(ns, false));
                VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000B\u000B\u000B", ns, FailureNotExpectedForTrailingWhite(ns, false));
                VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000C\u000C\u000C", ns, FailureNotExpectedForTrailingWhite(ns, false));
                VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000D\u000D\u000D", ns, FailureNotExpectedForTrailingWhite(ns, false));
                VerifyParseToString(GetDigitSequence(1, 100, random) + "\u0020\u0020\u0020", ns, FailureNotExpectedForTrailingWhite(ns, true));
            }

            // Leading Sign
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString(CultureInfo.CurrentCulture.NumberFormat.NegativeSign + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingSign) != 0));
                VerifyParseToString(CultureInfo.CurrentCulture.NumberFormat.PositiveSign + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingSign) != 0));
            }

            // Trailing Sign
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentCulture.NumberFormat.NegativeSign, ns, ((ns & NumberStyles.AllowTrailingSign) != 0));
                VerifyParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentCulture.NumberFormat.PositiveSign, ns, ((ns & NumberStyles.AllowTrailingSign) != 0));
            }

            // Parentheses
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString("(" + GetDigitSequence(1, 100, random) + ")", ns, ((ns & NumberStyles.AllowParentheses) != 0));
            }

            // Decimal Point - end
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, ns, ((ns & NumberStyles.AllowDecimalPoint) != 0));
            }

            // Decimal Point - middle
            for (int i = 0; i < s_samples; i++)
            {
                string digits = GetDigitSequence(1, 100, random);
                VerifyParseToString(digits + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + "000", ns, ((ns & NumberStyles.AllowDecimalPoint) != 0), digits);
            }

            // Decimal Point - non-zero decimal
            for (int i = 0; i < s_samples; i++)
            {
                string digits = GetDigitSequence(1, 100, random);
                VerifyParseToString(digits + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator + GetDigitSequence(20, 25, random), ns, false, digits);
            }

            // Thousands
            for (int i = 0; i < s_samples; i++)
            {
                int[] sizes = null;
                string seperator = null;
                string digits = null;

                sizes = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSizes;
                seperator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
                digits = GenerateGroups(sizes, seperator, random);
                VerifyParseToString(digits, ns, NoGrouping(sizes) || ((ns & NumberStyles.AllowThousands) != 0));
            }

            // Exponent
            for (int i = 0; i < s_samples; i++)
            {
                string digits = GetDigitSequence(1, 100, random);
                string exp = GetDigitSequence(1, 3, random);
                int expValue = int.Parse(exp);
                string zeros = new string('0', expValue);
                //Positive Exponents
                VerifyParseToString(digits + "e" + CultureInfo.CurrentCulture.NumberFormat.PositiveSign + exp, ns, ((ns & NumberStyles.AllowExponent) != 0), digits + zeros);
                //Negative Exponents
                bool valid = ((ns & NumberStyles.AllowExponent) != 0);
                for (int j = digits.Length; (valid && (j > 0) && (j > digits.Length - expValue)); j--)
                {
                    if (digits[j - 1] != '0')
                    {
                        valid = false;
                    }
                }
                if (digits.Length - int.Parse(exp) > 0)
                {
                    VerifyParseToString(digits + "e" + CultureInfo.CurrentCulture.NumberFormat.NegativeSign + exp, ns, valid, digits.Substring(0, digits.Length - int.Parse(exp)));
                }
                else
                {
                    VerifyParseToString(digits + "e" + CultureInfo.CurrentCulture.NumberFormat.NegativeSign + exp, ns, valid, "0");
                }
            }

            // Currency Symbol
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString(CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowCurrencySymbol) != 0));
            }

            // Hex Specifier
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString(GetHexDigitSequence(1, 15, random) + "A", ns, ((ns & NumberStyles.AllowHexSpecifier) != 0));
            }

            // Invalid Chars
            for (int i = 0; i < s_samples; i++)
            {
                VerifyParseToString(GetDigitSequence(1, 100, random) + GetRandomInvalidChar(random) + GetDigitSequence(1, 10, random), ns, false);
            }
        }

        private static void VerifyParseToString(string num1)
        {
            BigInteger test;

            Eval(BigInteger.Parse(num1), Fix(num1.Trim()));
            Assert.True(BigInteger.TryParse(num1, out test));
            Eval(test, Fix(num1.Trim()));
        }

        private static void VerifyFailParseToString(string num1, Type expectedExceptionType)
        {
            BigInteger test;
            Assert.False(BigInteger.TryParse(num1, out test), string.Format("Expected TryParse to fail on {0}", num1));
            if (num1 == null)
            {
                Assert.Throws<ArgumentNullException>(() => { BigInteger.Parse(num1).ToString("d"); });
            }
            else
            {
                Assert.Throws<FormatException>(() => { BigInteger.Parse(num1).ToString("d"); });
            }
        }

        private static void VerifyParseToString(string num1, NumberStyles ns, bool failureNotExpected)
        {
            VerifyParseToString(num1, ns, failureNotExpected, Fix(num1.Trim(), ((ns & NumberStyles.AllowHexSpecifier) != 0), failureNotExpected));
        }

        static void VerifyParseSpanToString(string num1, NumberStyles ns, bool failureNotExpected, string expected)
        {
            if (failureNotExpected)
            {
                Eval(BigInteger.Parse(num1.AsSpan(), ns), expected);

                Assert.True(BigInteger.TryParse(num1.AsSpan(), ns, provider: null, out BigInteger test));
                Eval(test, expected);

                if (ns == NumberStyles.Integer)
                {
                    Assert.True(BigInteger.TryParse(num1.AsSpan(), out test));
                    Eval(test, expected);
                }
            }
            else
            {
                Assert.Throws<FormatException>(() => { BigInteger.Parse(num1.AsSpan(), ns); });

                Assert.False(BigInteger.TryParse(num1.AsSpan(), ns, provider: null, out BigInteger test));

                if (ns == NumberStyles.Integer)
                {
                    Assert.False(BigInteger.TryParse(num1.AsSpan(), out test));
                }
            }
        }

        private static void VerifyParseToString(string num1, NumberStyles ns, bool failureNotExpected, string expected)
        {
            BigInteger test;

            if (failureNotExpected)
            {
                Eval(BigInteger.Parse(num1, ns), expected);
                Assert.True(BigInteger.TryParse(num1, ns, null, out test));
                Eval(test, expected);
            }
            else
            {
                if (num1 == null)
                {
                    Assert.Throws<ArgumentNullException>(() => { BigInteger.Parse(num1, ns); });
                }
                else
                {
                    Assert.Throws<FormatException>(() => { BigInteger.Parse(num1, ns); });
                }
                Assert.False(BigInteger.TryParse(num1, ns, null, out test), string.Format("Expected TryParse to fail on {0}", num1));
            }

            if (num1 != null)
            {
                VerifyParseSpanToString(num1, ns, failureNotExpected, expected);
            }
        }

        static void VerifySimpleFormatParseSpan(string num1, NumberFormatInfo nfi, BigInteger expected, bool failureExpected)
        {
            if (!failureExpected)
            {
                Assert.Equal(expected, BigInteger.Parse(num1.AsSpan(), provider: nfi));
                Assert.True(BigInteger.TryParse(num1.AsSpan(), NumberStyles.Any, nfi, out BigInteger test));
                Assert.Equal(expected, test);
            }
            else
            {
                Assert.Throws<FormatException>(() => { BigInteger.Parse(num1.AsSpan(), provider: nfi); });
                Assert.False(BigInteger.TryParse(num1.AsSpan(), NumberStyles.Any, nfi, out BigInteger test), string.Format("Expected TryParse to fail on {0}", num1));
            }
        }

        private static void VerifySimpleFormatParse(string num1, NumberFormatInfo nfi, BigInteger expected, bool failureExpected = false)
        {
            BigInteger test;

            if (!failureExpected)
            {
                Assert.Equal(expected, BigInteger.Parse(num1, nfi));
                Assert.True(BigInteger.TryParse(num1, NumberStyles.Any, nfi, out test));
                Assert.Equal(expected, test);
            }
            else
            {
                Assert.Throws<FormatException>(() => { BigInteger.Parse(num1, nfi); });
                Assert.False(BigInteger.TryParse(num1, NumberStyles.Any, nfi, out test), string.Format("Expected TryParse to fail on {0}", num1));
            }

            if (num1 != null)
            {
                VerifySimpleFormatParseSpan(num1, nfi, expected, failureExpected);
            }
        }

        static void VerifyFormatParseSpan(string num1, NumberStyles ns, NumberFormatInfo nfi, BigInteger expected, bool failureExpected)
        {
            if (!failureExpected)
            {
                Assert.Equal(expected, BigInteger.Parse(num1.AsSpan(), ns, nfi));
                Assert.True(BigInteger.TryParse(num1.AsSpan(), NumberStyles.Any, nfi, out BigInteger test));
                Assert.Equal(expected, test);
            }
            else
            {
                Assert.Throws<FormatException>(() => { BigInteger.Parse(num1.AsSpan(), ns, nfi); });
                Assert.False(BigInteger.TryParse(num1.AsSpan(), ns, nfi, out BigInteger test), string.Format("Expected TryParse to fail on {0}", num1));
            }
        }

        private static void VerifyFormatParse(string num1, NumberStyles ns, NumberFormatInfo nfi, BigInteger expected, bool failureExpected = false)
        {
            BigInteger test;

            if (!failureExpected)
            {
                Assert.Equal(expected, BigInteger.Parse(num1, ns, nfi));
                Assert.True(BigInteger.TryParse(num1, NumberStyles.Any, nfi, out test));
                Assert.Equal(expected, test);
            }
            else
            {
                Assert.Throws<FormatException>(() => { BigInteger.Parse(num1, ns, nfi); });
                Assert.False(BigInteger.TryParse(num1, ns, nfi, out test), string.Format("Expected TryParse to fail on {0}", num1));
            }

            if (num1 != null)
            {
                VerifyFormatParseSpan(num1, ns, nfi, expected, failureExpected);
            }
        }

        private static string GetDigitSequence(int min, int max, Random random)
        {
            string result = string.Empty;
            string[] digits = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            int size = random.Next(min, max);

            for (int i = 0; i < size; i++)
            {
                result += digits[random.Next(0, digits.Length)];
                if (i == 0)
                {
                    while (result == "0")
                    {
                        result = digits[random.Next(0, digits.Length)];
                    }
                }
            }

            return result;
        }

        private static string GetHexDigitSequence(int min, int max, Random random)
        {
            string result = string.Empty;
            string[] digits = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
            int size = random.Next(min, max);
            bool hasHexCharacter = false;

            while (!hasHexCharacter)
            {
                for (int i = 0; i < size; i++)
                {
                    int j = random.Next(0, digits.Length);
                    result += digits[j];
                    if (j > 9)
                    {
                        hasHexCharacter = true;
                    }
                }
            }

            return result;
        }

        private static string GetRandomInvalidChar(Random random)
        {
            char[] digits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' };
            char result = '5';
            while (result == '5')
            {
                result = unchecked((char)random.Next());
                for (int i = 0; i < digits.Length; i++)
                {
                    if (result == (char)digits[i])
                    {
                        result = '5';
                    }
                }

                // Remove the comma: 'AllowThousands' NumberStyle does not enforce the GroupSizes.
                if (result == ',')
                {
                    result = '5';
                }
            }

            string res = new string(result, 1);
            return res;
        }

        private static string Fix(string input)
        {
            return Fix(input, false);
        }

        private static string Fix(string input, bool isHex)
        {
            return Fix(input, isHex, true);
        }

        private static string Fix(string input, bool isHex, bool failureNotExpected)
        {
            string output = input;

            if (failureNotExpected)
            {
                if (isHex)
                {
                    output = ConvertHexToDecimal(output);
                }
                while (output.StartsWith("0") & (output.Length > 1))
                {
                    output = output.Substring(1);
                }
                List<char> out2 = new List<char>();
                for (int i = 0; i < output.Length; i++)
                {
                    if ((output[i] >= '0') & (output[i] <= '9'))
                    {
                        out2.Add(output[i]);
                    }
                }
                output = new string(out2.ToArray());
            }

            return output;
        }

        private static string ConvertHexToDecimal(string input)
        {
            char[] inArr = input.ToCharArray();
            bool isNeg = false;

            if (inArr.Length > 0)
            {
                if (int.Parse("0" + inArr[0], NumberStyles.AllowHexSpecifier) > 7)
                {
                    isNeg = true;
                    for (int i = 0; i < inArr.Length; i++)
                    {
                        int digit = int.Parse("0" + inArr[i], NumberStyles.AllowHexSpecifier);
                        digit = 15 - digit;
                        inArr[i] = digit.ToString("x")[0];
                    }
                }
            }

            BigInteger x = 0;
            BigInteger baseNum = 1;
            for (int i = inArr.Length - 1; i >= 0; i--)
            {
                try
                {
                    BigInteger x2 = (int.Parse(new string(new char[] { inArr[i] }), NumberStyles.AllowHexSpecifier) * baseNum);
                    x = x + x2;
                }
                catch (FormatException)
                {
                    // left blank char is not a hex character;
                }
                baseNum = baseNum * 16;
            }
            if (isNeg)
            {
                x = x + 1;
            }

            List<char> number = new List<char>();
            if (x == 0)
            {
                number.Add('0');
            }
            else
            {
                while (x > 0)
                {
                    number.Add((x % 10).ToString().ToCharArray()[0]);
                    x = x / 10;
                }
                number.Reverse();
            }

            string y2 = new string(number.ToArray());
            if (isNeg)
            {
                y2 = CultureInfo.CurrentCulture.NumberFormat.NegativeSign.ToCharArray() + y2;
            }
            return y2;
        }

        private static string GenerateGroups(int[] sizes, string seperator, Random random)
        {
            List<int> total_sizes = new List<int>();
            int total;
            int num_digits = random.Next(10, 100);
            string digits = string.Empty;

            if (NoGrouping(sizes))
            {
                return GetDigitSequence(1, 100, random);
            }

            total = 0;
            total_sizes.Add(0);
            for (int j = 0; ((j < (sizes.Length - 1)) && (total < 101)); j++)
            {
                total += sizes[j];
                total_sizes.Add(total);
            }
            if (total < 101)
            {
                if (sizes[sizes.Length - 1] == 0)
                {
                    total_sizes.Add(101);
                }
                else
                {
                    while (total < 101)
                    {
                        total += sizes[sizes.Length - 1];
                        total_sizes.Add(total);
                    }
                }
            }

            bool first = true;
            for (int j = total_sizes.Count - 1; j > 0; j--)
            {
                if ((first) && (total_sizes[j] >= num_digits))
                {
                    continue;
                }
                int group_size = num_digits - total_sizes[j - 1];
                if (first)
                {
                    digits += GetDigitSequence(group_size, group_size, random);
                    first = false;
                }
                else
                {
                    //Generate an extra character since the first character of GetDigitSequence is non-zero.
                    digits += GetDigitSequence(group_size + 1, group_size + 1, random).Substring(1);
                }
                num_digits -= group_size;
                if (num_digits > 0)
                {
                    digits += seperator;
                }
            }

            return digits;
        }

        private static NumberFormatInfo MarkUp(NumberFormatInfo nfi)
        {
            nfi.CurrencyDecimalDigits = 0;
            nfi.CurrencyDecimalSeparator = "!";
            nfi.CurrencyGroupSeparator = "#";
            nfi.CurrencyGroupSizes = new int[] { 2 };
            nfi.CurrencyNegativePattern = 4;
            nfi.CurrencyPositivePattern = 2;
            nfi.CurrencySymbol = "@";

            nfi.NumberDecimalDigits = 0;
            nfi.NumberDecimalSeparator = "^";
            nfi.NumberGroupSeparator = "&";
            nfi.NumberGroupSizes = new int[] { 4 };
            nfi.NumberNegativePattern = 4;

            nfi.PercentDecimalDigits = 0;
            nfi.PercentDecimalSeparator = "*";
            nfi.PercentGroupSeparator = "+";
            nfi.PercentGroupSizes = new int[] { 5 };
            nfi.PercentNegativePattern = 2;
            nfi.PercentPositivePattern = 2;
            nfi.PercentSymbol = "?";
            nfi.PerMilleSymbol = "~";

            nfi.NegativeSign = "<";
            nfi.PositiveSign = ">";

            return nfi;
        }

        // We need to account for cultures like fr-FR and uk-UA that use the no-break space (NBSP, 0xA0)
        // character as the group separator. Because NBSP cannot be (easily) entered by the end user we
        // accept regular spaces (SP, 0x20) as group separators for those cultures which means that
        // trailing SP characters will be interpreted as group separators rather than whitespace.
        //
        // See also System.Globalization.FormatProvider+Number.MatchChars(char*, char*)
        private static bool FailureNotExpectedForTrailingWhite(NumberStyles ns, bool spaceOnlyTrail)
        {
            if (spaceOnlyTrail && (ns & NumberStyles.AllowThousands) != 0)
            {
                if ((ns & NumberStyles.AllowCurrencySymbol) != 0)
                {
                    if (CultureInfo.CurrentCulture.NumberFormat.CurrencyGroupSeparator == "\u00A0")
                        return true;
                }
                else
                {
                    if (CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator == "\u00A0")
                        return true;
                }
            }

            return (ns & NumberStyles.AllowTrailingWhite) != 0;
        }

        private static void Eval(BigInteger x, string expected)
        {
            bool IsPos = (x >= 0);
            if (!IsPos)
            {
                x = -x;
            }

            if (x == 0)
            {
                Assert.Equal("0", expected);
            }
            else
            {
                List<char> number = new List<char>();
                while (x > 0)
                {
                    number.Add((x % 10).ToString().ToCharArray()[0]);
                    x = x / 10;
                }
                number.Reverse();
                string actual = new string(number.ToArray());

                Assert.Equal(expected, actual);
            }
        }
    }
}
