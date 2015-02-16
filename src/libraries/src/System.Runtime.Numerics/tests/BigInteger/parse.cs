// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace System.Numerics.Tests
{
    public class parseTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        [OuterLoop]
        public static void RunParseToStringTests()
        {
            byte[] tempByteArray1 = new byte[0];

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
            Assert.Throws<ArgumentException>(() =>
            {
                Eval(BigInteger.Parse("1", invalid).ToString("d"), "1", String.Format("Roundtrip between Parse() and ToString() failed.  RoundTrip returned: {0}  num1: {1}", BigInteger.Parse("1").ToString("d"), "1"));
            });
            Assert.Throws<ArgumentException>(() =>
            {
                BigInteger junk;
                BigInteger.TryParse("1", invalid, null, out junk);
                Eval(junk.ToString("d"), "1", String.Format("Roundtrip between TryParse() and ToString() failed.  RoundTrip returned: {0}  num1: {1}", junk.ToString("d"), "1"));
            });

            //FormatProvider tests
            RunFormatProviderParseStrings();
        }

        private static void RunFormatProviderParseStrings()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi = MarkUp(nfi);

            //Currencies
            // ***************************
            // *** FormatProvider - Currencies
            // ***************************
            Assert.True(VerifyFormatParse("@ 12#34#56!", NumberStyles.Any, nfi, new BigInteger(123456)), " Verification Failed");
            Assert.True(VerifyFormatParse("(12#34#56!@)", NumberStyles.Any, nfi, new BigInteger(-123456)), " Verification Failed");

            //Numbers
            // ***************************
            // *** FormatProvider - Numbers
            // ***************************
            Assert.True(VerifySimpleFormatParse(">1234567", nfi, new BigInteger(1234567)), " Verification Failed");
            Assert.True(VerifySimpleFormatParse("<1234567", nfi, new BigInteger(-1234567)), " Verification Failed");
            Assert.True(VerifyFormatParse("123&4567^", NumberStyles.Any, nfi, new BigInteger(1234567)), " Verification Failed");
            Assert.True(VerifyFormatParse("123&4567^ <", NumberStyles.Any, nfi, new BigInteger(-1234567)), " Verification Failed");
        }

        public static void VerifyDefaultParse(Random random)
        {
            // BasicTests
            Assert.True(VerifyFailParseToString(null, new ArgumentNullException().GetType()), " Verification Failed");
            Assert.True(VerifyFailParseToString(String.Empty, new FormatException().GetType()), " Verification Failed");
            Assert.True(VerifyParseToString("0"), " Verification Failed");
            Assert.True(VerifyParseToString("000"), " Verification Failed");
            Assert.True(VerifyParseToString("1"), " Verification Failed");
            Assert.True(VerifyParseToString("001"), " Verification Failed");

            // SimpleNumbers - Small
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString(GetDigitSequence(1, 10, random)), " Verification Failed");
            }

            // SimpleNumbers - Large
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString(GetDigitSequence(100, 1000, random)), " Verification Failed");
            }

            // Leading White
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString("\u0009\u0009\u0009" + GetDigitSequence(1, 100, random)), " Verification Failed");
                Assert.True(VerifyParseToString("\u000A\u000A\u000A" + GetDigitSequence(1, 100, random)), " Verification Failed");
                Assert.True(VerifyParseToString("\u000B\u000B\u000B" + GetDigitSequence(1, 100, random)), " Verification Failed");
                Assert.True(VerifyParseToString("\u000C\u000C\u000C" + GetDigitSequence(1, 100, random)), " Verification Failed");
                Assert.True(VerifyParseToString("\u000D\u000D\u000D" + GetDigitSequence(1, 100, random)), " Verification Failed");
                Assert.True(VerifyParseToString("\u0020\u0020\u0020" + GetDigitSequence(1, 100, random)), " Verification Failed");
            }

            // Trailing White
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + "\u0009\u0009\u0009"), " Verification Failed");
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000A\u000A\u000A"), " Verification Failed");
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000B\u000B\u000B"), " Verification Failed");
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000C\u000C\u000C"), " Verification Failed");
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000D\u000D\u000D"), " Verification Failed");
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + "\u0020\u0020\u0020"), " Verification Failed");
            }

            // Leading Sign
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString(CultureInfo.CurrentUICulture.NumberFormat.NegativeSign + GetDigitSequence(1, 100, random)), " Verification Failed");
                Assert.True(VerifyParseToString(CultureInfo.CurrentUICulture.NumberFormat.PositiveSign + GetDigitSequence(1, 100, random)), " Verification Failed");
            }

            // Trailing Sign
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyFailParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, new FormatException().GetType()), " Verification Failed");
                Assert.True(VerifyFailParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentUICulture.NumberFormat.PositiveSign, new FormatException().GetType()), " Verification Failed");
            }

            // Parentheses
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyFailParseToString("(" + GetDigitSequence(1, 100, random) + ")", new FormatException().GetType()), " Verification Failed");
            }

            // Decimal Point - end
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyFailParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator, new FormatException().GetType()), " Verification Failed");
            }

            // Decimal Point - middle
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyFailParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator + "000", new FormatException().GetType()), " Verification Failed");
            }

            // Decimal Point - non-zero decimal
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyFailParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator + GetDigitSequence(20, 25, random), new FormatException().GetType()), " Verification Failed");
            }

            // Thousands
            for (int i = 0; i < s_samples; i++)
            {
                int[] sizes = null;
                string seperator = null;
                string digits = null;

                sizes = CultureInfo.CurrentUICulture.NumberFormat.NumberGroupSizes;
                seperator = CultureInfo.CurrentUICulture.NumberFormat.NumberGroupSeparator;
                digits = GenerateGroups(sizes, seperator, random);
                Assert.True(VerifyFailParseToString(digits, new FormatException().GetType()), " Verification Failed");
            }

            // Exponent
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyFailParseToString(GetDigitSequence(1, 100, random) + "e" + CultureInfo.CurrentUICulture.NumberFormat.PositiveSign + GetDigitSequence(1, 3, random), new FormatException().GetType()), " Verification Failed");
                Assert.True(VerifyFailParseToString(GetDigitSequence(1, 100, random) + "e" + CultureInfo.CurrentUICulture.NumberFormat.NegativeSign + GetDigitSequence(1, 3, random), new FormatException().GetType()), " Verification Failed");
            }

            // Currency Symbol
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyFailParseToString(CultureInfo.CurrentUICulture.NumberFormat.CurrencySymbol + GetDigitSequence(1, 100, random), new FormatException().GetType()), " Verification Failed");
            }

            // Hex Specifier
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyFailParseToString(GetHexDigitSequence(1, 100, random), new FormatException().GetType()), " Verification Failed");
            }

            // Invalid Chars
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyFailParseToString(GetDigitSequence(1, 50, random) + GetRandomInvalidChar(random) + GetDigitSequence(1, 50, random), new FormatException().GetType()), " Verification Failed");
            }
        }

        public static void VerifyNumberStyles(NumberStyles ns, Random random)
        {
            Assert.True(VerifyParseToString(null, ns, false, null), " Verification Failed");
            Assert.True(VerifyParseToString(String.Empty, ns, false), " Verification Failed");
            Assert.True(VerifyParseToString("0", ns, true), " Verification Failed");
            Assert.True(VerifyParseToString("000", ns, true), " Verification Failed");
            Assert.True(VerifyParseToString("1", ns, true), " Verification Failed");
            Assert.True(VerifyParseToString("001", ns, true), " Verification Failed");

            // SimpleNumbers - Small
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString(GetDigitSequence(1, 10, random), ns, true), " Verification Failed");
            }

            // SimpleNumbers - Large
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString(GetDigitSequence(100, 1000, random), ns, true), " Verification Failed");
            }

            // Leading White
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString("\u0009\u0009\u0009" + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingWhite) != 0)), " Verification Failed");
                Assert.True(VerifyParseToString("\u000A\u000A\u000A" + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingWhite) != 0)), " Verification Failed");
                Assert.True(VerifyParseToString("\u000B\u000B\u000B" + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingWhite) != 0)), " Verification Failed");
                Assert.True(VerifyParseToString("\u000C\u000C\u000C" + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingWhite) != 0)), " Verification Failed");
                Assert.True(VerifyParseToString("\u000D\u000D\u000D" + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingWhite) != 0)), " Verification Failed");
                Assert.True(VerifyParseToString("\u0020\u0020\u0020" + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingWhite) != 0)), " Verification Failed");
            }

            // Trailing White
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + "\u0009\u0009\u0009", ns, ((ns & NumberStyles.AllowTrailingWhite) != 0)), " Verification Failed");
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000A\u000A\u000A", ns, ((ns & NumberStyles.AllowTrailingWhite) != 0)), " Verification Failed");
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000B\u000B\u000B", ns, ((ns & NumberStyles.AllowTrailingWhite) != 0)), " Verification Failed");
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000C\u000C\u000C", ns, ((ns & NumberStyles.AllowTrailingWhite) != 0)), " Verification Failed");
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + "\u000D\u000D\u000D", ns, ((ns & NumberStyles.AllowTrailingWhite) != 0)), " Verification Failed");
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + "\u0020\u0020\u0020", ns, ((ns & NumberStyles.AllowTrailingWhite) != 0)), " Verification Failed");
            }

            // Leading Sign
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString(CultureInfo.CurrentUICulture.NumberFormat.NegativeSign + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingSign) != 0)), " Verification Failed");
                Assert.True(VerifyParseToString(CultureInfo.CurrentUICulture.NumberFormat.PositiveSign + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowLeadingSign) != 0)), " Verification Failed");
            }

            // Trailing Sign
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, ns, ((ns & NumberStyles.AllowTrailingSign) != 0)), " Verification Failed");
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentUICulture.NumberFormat.PositiveSign, ns, ((ns & NumberStyles.AllowTrailingSign) != 0)), " Verification Failed");
            }

            // Parentheses
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString("(" + GetDigitSequence(1, 100, random) + ")", ns, ((ns & NumberStyles.AllowParentheses) != 0)), " Verification Failed");
            }

            // Decimal Point - end
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator, ns, ((ns & NumberStyles.AllowDecimalPoint) != 0)), " Verification Failed");
            }

            // Decimal Point - middle
            for (int i = 0; i < s_samples; i++)
            {
                string digits = GetDigitSequence(1, 100, random);
                Assert.True(VerifyParseToString(digits + CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator + "000", ns, ((ns & NumberStyles.AllowDecimalPoint) != 0), digits), " Verification Failed");
            }

            // Decimal Point - non-zero decimal
            for (int i = 0; i < s_samples; i++)
            {
                string digits = GetDigitSequence(1, 100, random);
                Assert.True(VerifyParseToString(digits + CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator + GetDigitSequence(20, 25, random), ns, false, digits), " Verification Failed");
            }

            // Thousands
            for (int i = 0; i < s_samples; i++)
            {
                int[] sizes = null;
                string seperator = null;
                string digits = null;

                sizes = CultureInfo.CurrentUICulture.NumberFormat.NumberGroupSizes;
                seperator = CultureInfo.CurrentUICulture.NumberFormat.NumberGroupSeparator;
                digits = GenerateGroups(sizes, seperator, random);
                Assert.True(VerifyParseToString(digits, ns, ((ns & NumberStyles.AllowThousands) != 0)), " Verification Failed");
            }

            // Exponent
            for (int i = 0; i < s_samples; i++)
            {
                string digits = GetDigitSequence(1, 100, random);
                string exp = GetDigitSequence(1, 3, random);
                int expValue = Int32.Parse(exp);
                string zeros = new string('0', expValue);
                //Positive Exponents
                Assert.True(VerifyParseToString(digits + "e" + CultureInfo.CurrentUICulture.NumberFormat.PositiveSign + exp, ns, ((ns & NumberStyles.AllowExponent) != 0), digits + zeros), " Verification Failed");
                //Negative Exponents
                bool valid = ((ns & NumberStyles.AllowExponent) != 0);
                for (int j = digits.Length; (valid && (j > 0) && (j > digits.Length - expValue)); j--)
                {
                    if (digits[j - 1] != '0')
                    {
                        valid = false;
                    }
                }
                if (digits.Length - Int32.Parse(exp) > 0)
                {
                    Assert.True(VerifyParseToString(digits + "e" + CultureInfo.CurrentUICulture.NumberFormat.NegativeSign + exp, ns, valid, digits.Substring(0, digits.Length - Int32.Parse(exp))), " Verification Failed");
                }
                else
                {
                    Assert.True(VerifyParseToString(digits + "e" + CultureInfo.CurrentUICulture.NumberFormat.NegativeSign + exp, ns, valid, "0"), " Verification Failed");
                }
            }

            // Currency Symbol
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString(CultureInfo.CurrentUICulture.NumberFormat.CurrencySymbol + GetDigitSequence(1, 100, random), ns, ((ns & NumberStyles.AllowCurrencySymbol) != 0)), " Verification Failed");
            }

            // Hex Specifier
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString(GetHexDigitSequence(1, 15, random) + "A", ns, ((ns & NumberStyles.AllowHexSpecifier) != 0)), " Verification Failed");
            }

            // Invalid Chars
            for (int i = 0; i < s_samples; i++)
            {
                Assert.True(VerifyParseToString(GetDigitSequence(1, 100, random) + GetRandomInvalidChar(random) + GetDigitSequence(1, 10, random), ns, false), " Verification Failed");
            }
        }

        private static bool VerifyParseToString(string num1)
        {
            bool ret = true;
            BigInteger test;

            ret &= Eval(BigInteger.Parse(num1), Fix(num1.Trim()), String.Format("Roundtrip between Parse() and ToString() failed.  RoundTrip returned: {0}  num1: {1}", BigInteger.Parse(num1), Fix(num1.Trim())));
            ret &= BigInteger.TryParse(num1, out test);
            ret &= Eval(test, Fix(num1.Trim()), String.Format("Roundtrip between TryParse() and ToString() failed.  ToString returned: {0} num1: {1}", test, Fix(num1.Trim())));

            return ret;
        }
        private static bool VerifyFailParseToString(string num1, Type expect)
        {
            bool ret = true;
            BigInteger test;

            try
            {
                Eval(BigInteger.Parse(num1).ToString("d"), num1.Trim(), String.Format("Roundtrip between Parse() and ToString() failed.  RoundTrip returned: {0}  num1: {1}", BigInteger.Parse(num1).ToString("d"), num1.Trim()));
                Console.WriteLine("{0} did not throw expected exception", num1);
                ret = false;
            }
            catch (Exception e)
            {
                if (!(e.GetType() == expect))
                {
                    Console.WriteLine("Wrong exception thrown.\n" + e);
                    ret = false;
                }
            }
            ret &= Eval(!BigInteger.TryParse(num1, out test), true, String.Format("TryParse expected on fail on {0}", num1));

            return ret;
        }
        private static bool VerifyParseToString(string num1, NumberStyles ns, bool failureNotExpected)
        {
            return VerifyParseToString(num1, ns, failureNotExpected, Fix(num1.Trim(), ((ns & NumberStyles.AllowHexSpecifier) != 0), failureNotExpected));
        }
        private static bool VerifyParseToString(string num1, NumberStyles ns, bool failureNotExpected, string value)
        {
            bool ret = true;
            BigInteger test;

            if (failureNotExpected)
            {
                try
                {
                    ret &= Eval(BigInteger.Parse(num1, ns), value, String.Format("Roundtrip between Parse() and ToString() failed.  RoundTrip returned: {0}  num1: {1}", BigInteger.Parse(num1, ns), value));
                    ret &= BigInteger.TryParse(num1, ns, null, out test);
                    ret &= Eval(test, value, String.Format("Roundtrip between TryParse() and ToString() failed.  ToString returned: {0} num1: {1}", test, value));
                }
                catch (Exception e)
                {
                    ret &= Eval(false, String.Format("Got Unexpected exception parsing: {0} \n {1}", num1, e));
                }
            }
            else
            {
                try
                {
                    Eval(BigInteger.Parse(num1, ns), value, String.Format("Roundtrip between Parse() and ToString() failed.  RoundTrip returned: {0}  num1: {1}", BigInteger.Parse(num1, ns).ToString("d"), value));
                    Console.WriteLine("{0} did not throw expected exception", num1);
                    ret = false;
                }
                catch (Exception)
                {
                    //Intentionally blank
                }
                ret &= Eval(!BigInteger.TryParse(num1, ns, null, out test), true, String.Format("TryParse expected on fail on {0}", num1));
            }

            return ret;
        }
        private static bool VerifySimpleFormatParse(string num1, NumberFormatInfo nfi, BigInteger expected)
        {
            return VerifySimpleFormatParse(num1, nfi, expected, true);
        }
        private static bool VerifySimpleFormatParse(string num1, NumberFormatInfo nfi, BigInteger expected, bool failureNotExpected)
        {
            bool ret = true;
            BigInteger test;

            if (failureNotExpected)
            {
                ret &= Eval(BigInteger.Parse(num1, nfi), expected, String.Format("Roundtrip between Parse() and ToString() failed.  RoundTrip returned: {0}  num1: {1}", BigInteger.Parse(num1, nfi), expected));
                ret &= BigInteger.TryParse(num1, NumberStyles.Any, nfi, out test);
                ret &= Eval(test, expected, String.Format("Roundtrip between TryParse() and ToString() failed.  ToString returned: {0} num1: {1}", test, expected));
            }
            else
            {
                try
                {
                    Eval(BigInteger.Parse(num1, nfi), expected, String.Format("Roundtrip between Parse() and ToString() failed.  RoundTrip returned: {0}  num1: {1}", BigInteger.Parse(num1, nfi).ToString("d"), expected));
                    Console.WriteLine("{0} did not throw expected exception", num1);
                    ret = false;
                }
                catch (Exception)
                {
                    //Intentionally blank
                }
                ret &= Eval(!BigInteger.TryParse(num1, NumberStyles.Any, nfi, out test), true, String.Format("TryParse expected on fail on {0}", num1));
            }
            return ret;
        }
        private static bool VerifyFormatParse(string num1, NumberStyles ns, NumberFormatInfo nfi, BigInteger expected)
        {
            return VerifyFormatParse(num1, ns, nfi, expected, true);
        }
        private static bool VerifyFormatParse(string num1, NumberStyles ns, NumberFormatInfo nfi, BigInteger expected, bool failureNotExpected)
        {
            bool ret = true;
            BigInteger test;

            if (failureNotExpected)
            {
                ret &= Eval(BigInteger.Parse(num1, ns, nfi), expected, String.Format("Roundtrip between Parse() and ToString() failed.  RoundTrip returned: {0}  num1: {1}", BigInteger.Parse(num1, ns, nfi), expected));
                ret &= BigInteger.TryParse(num1, NumberStyles.Any, nfi, out test);
                ret &= Eval(test, expected, String.Format("Roundtrip between TryParse() and ToString() failed.  ToString returned: {0} num1: {1}", test, expected));
            }
            else
            {
                try
                {
                    Eval(BigInteger.Parse(num1, ns, nfi), expected, String.Format("Roundtrip between Parse() and ToString() failed.  RoundTrip returned: {0}  num1: {1}", BigInteger.Parse(num1, ns, nfi).ToString("d"), expected));
                    Console.WriteLine("{0} did not throw expected exception", num1);
                    ret = false;
                }
                catch (Exception)
                {
                    //Intentionally blank
                }
                ret &= Eval(!BigInteger.TryParse(num1, ns, nfi, out test), true, String.Format("TryParse expected on fail on {0}", num1));
            }
            return ret;
        }

        private static String GetDigitSequence(int min, int max, Random random)
        {
            String result = String.Empty;
            String[] digits = new String[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
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
        private static String GetHexDigitSequence(int min, int max, Random random)
        {
            String result = String.Empty;
            String[] digits = new String[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "a", "b", "c", "d", "e", "f" };
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
        private static String GetRandomInvalidChar(Random random)
        {
            Char[] digits = new Char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' };
            Char result = '5';
            while (result == '5')
            {
                result = (Char)random.Next();
                for (int i = 0; i < digits.Length; i++)
                {
                    if (result == (char)digits[i])
                        result = '5';
                }

                // Remove the comma: 'AllowThousands' NumberStyle does not enforce the GroupSizes.
                if (result == ',')
                    result = '5';
            }

            String res = new String(result, 1);
            return res;
        }
        private static String Fix(String input)
        {
            return Fix(input, false);
        }
        private static String Fix(String input, bool isHex)
        {
            return Fix(input, isHex, true);
        }
        private static String Fix(String input, bool isHex, bool failureNotExpected)
        {
            String output = input;

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
                List<Char> out2 = new List<Char>();
                for (int i = 0; i < output.Length; i++)
                {
                    if ((output[i] >= '0') & (output[i] <= '9'))
                    {
                        out2.Add(output[i]);
                    }
                }
                output = new String(out2.ToArray());
            }

            return output;
        }
        private static String ConvertHexToDecimal(string input)
        {
            char[] inArr = input.ToCharArray();
            bool isNeg = false;

            if (inArr.Length > 0)
            {
                if (Int32.Parse("0" + inArr[0], NumberStyles.AllowHexSpecifier) > 7)
                {
                    isNeg = true;
                    for (int i = 0; i < inArr.Length; i++)
                    {
                        int digit = Int32.Parse("0" + inArr[i], NumberStyles.AllowHexSpecifier);
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
                    BigInteger x2 = (Int32.Parse(new string(new char[] { inArr[i] }), NumberStyles.AllowHexSpecifier) * baseNum);
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

            String y2 = new String(number.ToArray());
            if (isNeg)
            {
                y2 = CultureInfo.CurrentUICulture.NumberFormat.NegativeSign.ToCharArray() + y2;
            }
            return y2;
        }
        private static String GenerateGroups(int[] sizes, string seperator, Random random)
        {
            List<int> total_sizes = new List<int>();
            int total;
            int num_digits = random.Next(10, 100);
            ;
            string digits = String.Empty;

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
                    continue;
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

        public static bool Eval<T>(T expected, T actual, String errorMsg)
        {
            bool retValue = expected == null ? actual == null : expected.Equals(actual);

            if (!retValue)
                return Eval(retValue, errorMsg +
                " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                " Actual:" + (null == actual ? "<null>" : actual.ToString()));

            return true;
        }
        public static bool Eval(BigInteger x, String y, String errorMsg)
        {
            bool IsPos = (x >= 0);
            bool ret = true;
            String y2 = null;

            if (!IsPos)
            {
                x = -x;
            }

            if (x == 0)
            {
                Assert.True(y.Equals("0"), " Verification Failed");
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
                y2 = new String(number.ToArray());
                //            if (y.Length > 50)
                //            {
                //                Assert.IsTrue((y2.StartsWith(y.Substring(0, 50), StringComparison.OrdinalIgnoreCase)), " Verification Failed" );
                //                Assert.IsTrue((y.Length == y2.Length), " Verification Failed" );
                //            }
                //            else
                //            {
                Assert.True(y2.Equals(y, StringComparison.OrdinalIgnoreCase), " Verification Failed");
                //            }
            }

            if (!ret)
            {
                Console.WriteLine("Error: " + errorMsg);
                Console.WriteLine("got:      " + y2);
                Console.WriteLine("expected: " + y);
            }
            return ret;
        }
        public static bool Eval(bool expression, string message)
        {
            if (!expression)
            {
                Console.WriteLine(message);
            }

            return expression;
        }
    }
}
