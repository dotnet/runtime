// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace System.Numerics.Tests
{
    public class ToStringTest
    {
        private static bool s_noZeroOut = true;

        public delegate String StringFormatter(String input, int percision, NumberFormatInfo nfi);
        private static int s_samples = 1;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunSimpleToStringTests()
        {
            String test;

            //Scenario 1: Large BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, s_random);
                Assert.True(VerifyToString(test, test), " Verification Failed");
            }

            //Scenario 2: Small BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, s_random);
                Assert.True(VerifyToString(test, test), " Verification Failed");
            }

            //Scenario 3: Large BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, s_random);
                Assert.True(VerifyToString(CultureInfo.CurrentUICulture.NumberFormat.NegativeSign + test, CultureInfo.CurrentUICulture.NumberFormat.NegativeSign + test), " Verification Failed");
            }

            //Scenario 4: Small BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, s_random);
                Assert.True(VerifyToString(CultureInfo.CurrentUICulture.NumberFormat.NegativeSign + test, CultureInfo.CurrentUICulture.NumberFormat.NegativeSign + test), " Verification Failed");
            }

            //Scenario 5: Constant values
            Assert.True(VerifyToString(CultureInfo.CurrentUICulture.NumberFormat.NegativeSign + "1", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign + "1"), " Verification Failed");
            Assert.True(VerifyToString("0", "0"), " Verification Failed");
            Assert.True(VerifyToString(Int16.MinValue.ToString(), Int16.MinValue.ToString()), " Verification Failed");
            Assert.True(VerifyToString(Int32.MinValue.ToString(), Int32.MinValue.ToString()), " Verification Failed");
            Assert.True(VerifyToString(Int64.MinValue.ToString(), Int64.MinValue.ToString()), " Verification Failed");
            Assert.True(VerifyToString(Decimal.MinValue.ToString(), Decimal.MinValue.ToString()), " Verification Failed");
            Assert.True(VerifyToString(Int16.MaxValue.ToString(), Int16.MaxValue.ToString()), " Verification Failed");
            Assert.True(VerifyToString(Int32.MaxValue.ToString(), Int32.MaxValue.ToString()), " Verification Failed");
            Assert.True(VerifyToString(Int64.MaxValue.ToString(), Int64.MaxValue.ToString()), " Verification Failed");
            Assert.True(VerifyToString(Decimal.MaxValue.ToString(), Decimal.MaxValue.ToString()), " Verification Failed");
        }

        [Fact]
        public static void RunProviderToStringTests()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi = MarkUp(nfi);

            Assert.True(RunSimpleProviderToStringTests(s_random, "", nfi, 0, DecimalFormatter), " Verification Failed");
            Assert.True(RunSimpleProviderToStringTests(s_random, "C", nfi, nfi.CurrencyDecimalDigits, CurrencyFormatter), " Verification Failed");
            Assert.True(RunSimpleProviderToStringTests(s_random, "D", nfi, 0, DecimalFormatter), " Verification Failed");
            Assert.True(RunSimpleProviderToStringTests(s_random, "E", nfi, 6, ExponentialFormatter), " Verification Failed");
            Assert.True(RunSimpleProviderToStringTests(s_random, "F", nfi, nfi.NumberDecimalDigits, FixedFormatter), " Verification Failed");
            Assert.True(RunSimpleProviderToStringTests(s_random, "G", nfi, 0, DecimalFormatter), " Verification Failed");
            Assert.True(RunSimpleProviderToStringTests(s_random, "N", nfi, nfi.NumberDecimalDigits, NumberFormatter), " Verification Failed");
            Assert.True(RunSimpleProviderToStringTests(s_random, "P", nfi, nfi.PercentDecimalDigits, PercentFormatter), " Verification Failed");
            Assert.True(RunSimpleProviderToStringTests(s_random, "X", nfi, 0, HexFormatter), " Verification Failed");
            Assert.True(RunSimpleProviderToStringTests(s_random, "R", nfi, 0, DecimalFormatter), " Verification Failed");
        }

        [Fact]
        public static void RunStandardFormatToStringTests()
        {
            String test;
            String format;

            //Currency
            Assert.True(RunStandardFormatToStringTests(s_random, "C", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, CultureInfo.CurrentUICulture.NumberFormat.CurrencyDecimalDigits, CurrencyFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "c0", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, CurrencyFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "C1", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, CurrencyFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "c2", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 2, CurrencyFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "C5", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 5, CurrencyFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "c33", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 33, CurrencyFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "C99", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 99, CurrencyFormatter), " Verification Failed");

            //Decimal
            Assert.True(RunStandardFormatToStringTests(s_random, "D", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "d0", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "D1", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "d2", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 2, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "D5", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 5, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "d33", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 33, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "D99", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 99, DecimalFormatter), " Verification Failed");

            //Exponential (note: negative percision means lower case e)
            Assert.True(RunStandardFormatToStringTests(s_random, "E", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 6, ExponentialFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "E0", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, ExponentialFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "E1", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExponentialFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "e2", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, -2, ExponentialFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "E5", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 5, ExponentialFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "e33", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, -33, ExponentialFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "E99", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 99, ExponentialFormatter), " Verification Failed");
            //test exponent of 4 digits
            test = GetDigitSequence(2000, 2000, s_random);
            Assert.True(VerifyToString(test, "E", ExponentialFormatter(test, 6, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");

            //Fixed-Point
            Assert.True(RunStandardFormatToStringTests(s_random, "f", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalDigits, FixedFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "F0", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, FixedFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "f1", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, FixedFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "F2", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 2, FixedFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "f5", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 5, FixedFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "F33", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 33, FixedFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "f99", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 99, FixedFormatter), " Verification Failed");

            //General
            Assert.True(RunStandardFormatToStringTests(s_random, "g", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "G0", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "G1", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "G2", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 2, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "g5", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 5, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "G33", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 33, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "g99", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 99, DecimalFormatter), " Verification Failed");

            //Number
            Assert.True(RunStandardFormatToStringTests(s_random, "n", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalDigits, NumberFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "N0", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, NumberFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "N1", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, NumberFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "N2", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 2, NumberFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "n5", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 5, NumberFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "N33", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 33, NumberFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "n99", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 99, NumberFormatter), " Verification Failed");

            //Percent
            Assert.True(RunStandardFormatToStringTests(s_random, "p", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, CultureInfo.CurrentUICulture.NumberFormat.PercentDecimalDigits, PercentFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "P0", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, PercentFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "P1", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, PercentFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "P2", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 2, PercentFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "p5", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 5, PercentFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "P33", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 33, PercentFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "p99", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 99, PercentFormatter), " Verification Failed");

            //Hex
            Assert.True(RunStandardFormatToStringTests(s_random, "X", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, HexFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "X0", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, HexFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "x1", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, -1, HexFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "X2", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 2, HexFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "x5", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, -5, HexFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "X33", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 33, HexFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "x99", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, -99, HexFormatter), " Verification Failed");

            //RoundTrip
            Assert.True(RunStandardFormatToStringTests(s_random, "R", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "R0", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "r1", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "R2", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 2, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "r5", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 5, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "R33", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 33, DecimalFormatter), " Verification Failed");
            Assert.True(RunStandardFormatToStringTests(s_random, "r99", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 99, DecimalFormatter), " Verification Failed");

            //other - invalid format characters
            for (int i = 0; i < s_samples; i++)
            {
                format = GetRandomInvalidFormatChar(s_random);
                test = GetDigitSequence(10, 100, s_random);
                Assert.True(VerifyToString(test, true, format, false, null, true, null), " Verification Failed");
            }
        }

        [Fact]
        public static void RunCustomFormatZeroPlaceholder()
        {
            //Zero Placeholder
            Assert.True(RunCustomFormatToStringTests(s_random, "0", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ZeroFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 4, ZeroFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, new String('0', 500), CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 500, ZeroFormatter), " Verification Failed");
        }

        [Fact]
        public static void RunCustomFormatDigitPlaceholder()
        {
            //Digit Placeholder
            Assert.True(RunCustomFormatToStringTests(s_random, "#", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, ZeroFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "####", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, ZeroFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, new String('#', 500), CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, ZeroFormatter), " Verification Failed");
        }

        [Fact]
        public static void RunCustomFormatDecimalPoint()
        {
            //Decimal Point (match required digits before and after point with percision)
            Assert.True(RunCustomFormatToStringTests(s_random, "#.#", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, DecimalPointFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "00.00", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 2, DecimalPointFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0000.0.00.0", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 4, DecimalPointFormatter), " Verification Failed");
        }

        [Fact]
        public static void RunCustomFormatThousandsSeparator()
        {
            //Thousands Seperator
            Assert.True(RunCustomFormatToStringTests(s_random, "#,#", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, ThousandsFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "00,00", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 4, ThousandsFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0000,0,00,0", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 8, ThousandsFormatter), " Verification Failed");
        }

        [Fact]
        public static void RunCustomFormatNumberScaling()
        {
            //Number Scaling (match scale factor to decimal places+3
            Assert.True(RunCustomFormatToStringTests(s_random, "#,", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 3, ScalingFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "#,,.000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 6, ScalingFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "#,,,.000000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 9, ScalingFormatter), " Verification Failed");
        }

        [Fact]
        public static void RunCustomFormatPercentSign()
        {
            //Percent Sign
            Assert.True(RunCustomFormatToStringTests(s_random, "#%", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, PercentSymbolFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "#%000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 3, PercentSymbolFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "#%000000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 6, PercentSymbolFormatter), " Verification Failed");
        }

        [Fact]
        public static void RunCustomFormatScientificNotation()
        {
            //Scientific Notation
            Assert.True(RunCustomFormatToStringTests(s_random, "0.000000E000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 6, ScientificFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0.000000E-000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 6, ScientificFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0.000000E+000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 6, SignedScientificFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0.000000e000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, -6, ScientificFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0.000000e-000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, -6, ScientificFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0.000000e+000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, -6, SignedScientificFormatter), " Verification Failed");
        }

        [Fact]
        public static void RunCustomFormatEscapeChar()
        {
            //Escape Character
            Assert.True(RunCustomFormatToStringTests(s_random, "0\\\'", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "\'")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\\\"", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "\"")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\\%", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "%")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\n", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "\n")), " Verification Failed");
        }

        [Fact]
        public static void RunCustomFormatLiterals()
        {
            //Literals
            Assert.True(RunCustomFormatToStringTests(s_random, "0\'.0\'", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, ".0")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\".0\"", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, ".0")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\',0\'", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, ",0")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\",0\"", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, ",0")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\',\'", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, ",")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\",\"", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, ",")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\'%\'", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "%")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\"%\"", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "%")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\'E+0\'", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "E+0")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\"E+0\"", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "E+0")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\'\\\'", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "\\")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "0\"\\\"", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "\\")), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "#\',\'%", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, ExtraFormatter(PercentSymbolFormatter, ",", CultureInfo.CurrentUICulture.NumberFormat.PercentSymbol.Length)), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "000\",\".000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 3, ExtraFormatter(DecimalPointFormatter, ",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator.Length + 3)), " Verification Failed");
        }

        [Fact]
        public static void RunCustomFormatSeparator()
        {
            //Seperator
            Assert.True(RunCustomFormatToStringTests(s_random, "00.00;0.00E000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 2, CombinedFormatter(DecimalPointFormatter, ScientificFormatter)), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "00.00;;0.00E000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 2, CombinedFormatter(DecimalPointFormatter, DecimalPointFormatter, ScientificFormatter, true)), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "00.00;#%00;0.00E000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 2, CombinedFormatter(DecimalPointFormatter, PercentSymbolFormatter, ScientificFormatter)), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "00000000000000000000000000000.00000000000000000000000000000;", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 29, DecimalPointFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "00000000000000000000000000000.00000000000000000000000000000;;", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 29, DecimalPointFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, ";", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 2, CombinedFormatter(EmptyFormatter, EmptyFormatter, EmptyFormatter, true)), " Verification Failed");
        }

        [Fact]
        public static void CustomFormatPerMille()
        {
            //PerMillie Symbol
            Assert.True(RunCustomFormatToStringTests(s_random, "#\u2030", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 0, PerMilleSymbolFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "#\u2030000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 3, PerMilleSymbolFormatter), " Verification Failed");
            Assert.True(RunCustomFormatToStringTests(s_random, "#\u2030000000", CultureInfo.CurrentUICulture.NumberFormat.NegativeSign, 6, PerMilleSymbolFormatter), " Verification Failed");
        }

        private static bool RunSimpleProviderToStringTests(Random random, String format, NumberFormatInfo provider, int percision, StringFormatter formatter)
        {
            bool ret = true;
            String test;
            bool hasFormat = (format != String.Empty);

            //Scenario 1: Large BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, random);
                Assert.True(VerifyToString(test, hasFormat, format, true, provider, false, formatter(test, percision, provider)), " Verification Failed");
            }

            //Scenario 2: Small BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, random);
                Assert.True(VerifyToString(test, hasFormat, format, true, provider, false, formatter(test, percision, provider)), " Verification Failed");
            }

            //Scenario 3: Large BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, random);
                Assert.True(VerifyToString(provider.NegativeSign + test, hasFormat, format, true, provider, false, formatter(provider.NegativeSign + test, percision, provider)), " Verification Failed");
            }

            //Scenario 4: Small BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, random);
                Assert.True(VerifyToString(provider.NegativeSign + test, hasFormat, format, true, provider, false, formatter(provider.NegativeSign + test, percision, provider)), " Verification Failed");
            }

            //Scenario 5: Constant values
            Assert.True(VerifyToString(provider.NegativeSign + "1", hasFormat, format, true, provider, false, formatter(provider.NegativeSign + "1", percision, provider)), " Verification Failed");
            Assert.True(VerifyToString(provider.NegativeSign + "0", hasFormat, format, true, provider, false, formatter("0", percision, provider)), " Verification Failed");
            Assert.True(VerifyToString("0", hasFormat, format, true, provider, false, formatter("0", percision, provider)), " Verification Failed");
            Assert.True(VerifyToString(Int16.MinValue.ToString("d", provider), hasFormat, format, true, provider, false, formatter(Int16.MinValue.ToString("d", provider), percision, provider)), " Verification Failed");
            Assert.True(VerifyToString(Int32.MinValue.ToString("d", provider), hasFormat, format, true, provider, false, formatter(Int32.MinValue.ToString("d", provider), percision, provider)), " Verification Failed");
            Assert.True(VerifyToString(Int64.MinValue.ToString("d", provider), hasFormat, format, true, provider, false, formatter(Int64.MinValue.ToString("d", provider), percision, provider)), " Verification Failed");
            Assert.True(VerifyToString(Int16.MaxValue.ToString("d", provider), hasFormat, format, true, provider, false, formatter(Int16.MaxValue.ToString("d", provider), percision, provider)), " Verification Failed");
            Assert.True(VerifyToString(Int32.MaxValue.ToString("d", provider), hasFormat, format, true, provider, false, formatter(Int32.MaxValue.ToString("d", provider), percision, provider)), " Verification Failed");
            Assert.True(VerifyToString(Int64.MaxValue.ToString("d", provider), hasFormat, format, true, provider, false, formatter(Int64.MaxValue.ToString("d", provider), percision, provider)), " Verification Failed");

            return ret;
        }
        private static bool RunStandardFormatToStringTests(Random random, String format, String negativeSign, int percision, StringFormatter formatter)
        {
            bool ret = true;
            String test;

            //Scenario 1: Large BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, random);
                Assert.True(VerifyToString(test, format, formatter(test, percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            }

            //Scenario 2: Small BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, random);
                Assert.True(VerifyToString(test, format, formatter(test, percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            }

            //Scenario 3: Large BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, random);
                Assert.True(VerifyToString(negativeSign + test, format, formatter(negativeSign + test, percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            }

            //Scenario 4: Small BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, random);
                Assert.True(VerifyToString(negativeSign + test, format, formatter(negativeSign + test, percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            }

            //Scenario 5: Constant values
            Assert.True(VerifyToString(negativeSign + "1", format, formatter(negativeSign + "1", percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(negativeSign + "0", format, formatter("0", percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString("0", format, formatter("0", percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Int16.MinValue.ToString(), format, formatter(Int16.MinValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Int32.MinValue.ToString(), format, formatter(Int32.MinValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Int64.MinValue.ToString(), format, formatter(Int64.MinValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Decimal.MinValue.ToString(), format, formatter(Decimal.MinValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Int16.MaxValue.ToString(), format, formatter(Int16.MaxValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Int32.MaxValue.ToString(), format, formatter(Int32.MaxValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Int64.MaxValue.ToString(), format, formatter(Int64.MaxValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Decimal.MaxValue.ToString(), format, formatter(Decimal.MaxValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");

            return ret;
        }
        private static bool RunCustomFormatToStringTests(Random random, String format, String negativeSign, int percision, StringFormatter formatter)
        {
            bool ret = true;
            String test;

            //Scenario 1: Large BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, random);
                Assert.True(VerifyToString(test, format, formatter(test, percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            }

            //Scenario 2: Small BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, random);
                Assert.True(VerifyToString(test, format, formatter(test, percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            }

            //Scenario 3: Large BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, random);
                Assert.True(VerifyToString(negativeSign + test, format, formatter(negativeSign + test, percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            }

            //Scenario 4: Small BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, random);
                Assert.True(VerifyToString(negativeSign + test, format, formatter(negativeSign + test, percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            }

            //Scenario 5: Constant values
            Assert.True(VerifyToString(negativeSign + "1", format, formatter(negativeSign + "1", percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(negativeSign + "0", format, formatter("0", percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString("0", format, formatter("0", percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Int16.MinValue.ToString(), format, formatter(Int16.MinValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Int32.MinValue.ToString(), format, formatter(Int32.MinValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Int64.MinValue.ToString(), format, formatter(Int64.MinValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Decimal.MinValue.ToString(), format, formatter(Decimal.MinValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Int16.MaxValue.ToString(), format, formatter(Int16.MaxValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Int32.MaxValue.ToString(), format, formatter(Int32.MaxValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Int64.MaxValue.ToString(), format, formatter(Int64.MaxValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");
            Assert.True(VerifyToString(Decimal.MaxValue.ToString(), format, formatter(Decimal.MaxValue.ToString(), percision, CultureInfo.CurrentUICulture.NumberFormat)), " Verification Failed");

            return ret;
        }

        private static String CurrencyFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            String pre = String.Empty;
            String post = String.Empty;

            if (input.StartsWith(nfi.NegativeSign))
            {
                input = input.Substring(nfi.NegativeSign.Length);

                switch (nfi.CurrencyNegativePattern)
                {
                    case 0:
                        pre = "(" + nfi.CurrencySymbol;
                        post = ")";
                        break;
                    case 1:
                        pre = nfi.NegativeSign + nfi.CurrencySymbol;
                        break;
                    case 2:
                        pre = nfi.CurrencySymbol + nfi.NegativeSign;
                        break;
                    case 3:
                        pre = nfi.CurrencySymbol;
                        post = nfi.NegativeSign;
                        break;
                    case 4:
                        pre = "(";
                        post = nfi.CurrencySymbol + ")";
                        break;
                    case 5:
                        pre = nfi.NegativeSign;
                        post = nfi.CurrencySymbol;
                        break;
                    case 6:
                        post = nfi.NegativeSign + nfi.CurrencySymbol;
                        break;
                    case 7:
                        post = nfi.CurrencySymbol + nfi.NegativeSign;
                        break;
                    case 8:
                        pre = nfi.NegativeSign;
                        post = " " + nfi.CurrencySymbol;
                        break;
                    case 9:
                        pre = nfi.NegativeSign + nfi.CurrencySymbol + " ";
                        break;
                    case 10:
                        post = " " + nfi.CurrencySymbol + nfi.NegativeSign;
                        break;
                    case 11:
                        pre = nfi.CurrencySymbol + " ";
                        post = nfi.NegativeSign;
                        break;
                    case 12:
                        pre = nfi.CurrencySymbol + " " + nfi.NegativeSign;
                        break;
                    case 13:
                        post = nfi.NegativeSign + " " + nfi.CurrencySymbol;
                        break;
                    case 14:
                        pre = "(" + nfi.CurrencySymbol + " ";
                        post = ")";
                        break;
                    case 15:
                        pre = "(";
                        post = " " + nfi.CurrencySymbol + ")";
                        break;
                }
            }
            else
            {
                switch (nfi.CurrencyPositivePattern)
                {
                    case 0:
                        pre = nfi.CurrencySymbol;
                        break;
                    case 1:
                        post = nfi.CurrencySymbol;
                        break;
                    case 2:
                        pre = nfi.CurrencySymbol + " ";
                        break;
                    case 3:
                        post = " " + nfi.CurrencySymbol;
                        break;
                }
            }

            return pre + GroupFormatDigits(input, nfi.CurrencyGroupSeparator, nfi.CurrencyGroupSizes, nfi.CurrencyDecimalSeparator, percision) + post;
        }
        private static String DecimalFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            String output = input;

            if (input.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }

            output = ZeroString(percision - output.Length) + output;

            if (IsNeg)
            {
                output = nfi.NegativeSign + output;
            }

            return output;
        }
        private static String ExponentialFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            Char[] temp = input.ToCharArray();
            Char[] temp2;
            String output = String.Empty;
            String digits;
            Char exp = 'E';

            if (percision < 0)
            {
                percision = -percision;
                exp = 'e';
            }
            if (input.StartsWith(nfi.NegativeSign))
            {
                temp = new String(temp, nfi.NegativeSign.Length, temp.Length - nfi.NegativeSign.Length).ToCharArray();
                IsNeg = true;
            }

            digits = (temp.Length - 1).ToString();
            digits = ZeroString(3 - digits.Length) + digits;
            temp2 = temp;
            temp = new Char[percision + 2];
            for (int j = 0; j < temp.Length; j++)
            {
                if ((j < 100) && (j < temp2.Length))
                {
                    temp[j] = temp2[j];
                }
                else
                {
                    temp[j] = '0';
                }
            }

            int i = percision + 1;
            if (temp[i] >= '5')
            {
                i--;
                while ((i >= 0) && (temp[i] == '9'))
                {
                    temp[i] = '0';
                    i--;
                };
                if (i > -1)
                {
                    temp[i]++;
                }
                else
                {
                    temp[0] = '1';
                    digits = (Int32.Parse(digits) + 1).ToString("D3");
                    i = 0;
                }
            }
            else
            {
                i--;
            }

            if (IsNeg)
            {
                output = nfi.NegativeSign;
            }
            output = output + temp[0];
            if (percision != 0)
            {
                output = output + nfi.NumberDecimalSeparator + new String(temp, 1, percision);
            }
            output = output + exp + nfi.PositiveSign + digits;

            return output;
        }
        private static String FixedFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            String output = input;

            if (input.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }

            if (IsNeg)
            {
                output = nfi.NegativeSign + output;
            }

            if (percision > 0)
            {
                output = output + nfi.NumberDecimalSeparator + ZeroString(percision);
            }

            return output;
        }
        private static String GeneralFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            String output = input;
            bool lowercase = false;
            char exp = 'E';

            if (percision < 0)
            {
                lowercase = true;
                exp = 'e';
                percision = -percision;
            }
            if (input.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
            }

            if (output.Length > percision)
            {
                int newpercision = (percision == 0 ? 0 : percision - 1);
                if (lowercase)
                {
                    newpercision = -newpercision;
                }
                output = ExponentialFormatter(input, newpercision, nfi);

                int expPlace = output.IndexOf(exp);
                if (output[expPlace + nfi.PositiveSign.Length + 1] == '0')
                {
                    output = output.Substring(0, expPlace + nfi.PositiveSign.Length + 1) + output.Substring(expPlace + nfi.PositiveSign.Length + 2);
                }
                while (output[expPlace - 1] == '0')
                {
                    output = output.Substring(0, expPlace - 1) + output.Substring(expPlace);
                    expPlace--;
                }
                if (output.Substring(expPlace - nfi.NumberDecimalSeparator.Length, nfi.NumberDecimalSeparator.Length).Equals(nfi.NumberDecimalSeparator))
                {
                    output = output.Substring(0, expPlace - nfi.NumberDecimalSeparator.Length) + output.Substring(expPlace);
                }
            }
            else
            {
                output = FixedFormatter(input, 0, nfi);
            }

            return output;
        }
        private static String NumberFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            String pre = String.Empty;
            String post = String.Empty;

            if (input.StartsWith(nfi.NegativeSign))
            {
                input = input.Substring(nfi.NegativeSign.Length);

                switch (nfi.NumberNegativePattern)
                {
                    case 0:
                        pre = "(";
                        post = ")";
                        break;
                    case 1:
                        pre = nfi.NegativeSign;
                        break;
                    case 2:
                        pre = nfi.NegativeSign + " ";
                        break;
                    case 3:
                        post = nfi.NegativeSign;
                        break;
                    case 4:
                        post = " " + nfi.NegativeSign;
                        break;
                }
            }

            return pre + GroupFormatDigits(input, nfi.NumberGroupSeparator, nfi.NumberGroupSizes, nfi.NumberDecimalSeparator, percision) + post;
        }
        private static String PercentFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            String pre = String.Empty;
            String post = String.Empty;

            if (input.StartsWith(nfi.NegativeSign))
            {
                input = input.Substring(nfi.NegativeSign.Length);

                switch (nfi.PercentNegativePattern)
                {
                    case 0:
                        pre = nfi.NegativeSign;
                        post = " " + nfi.PercentSymbol;
                        break;
                    case 1:
                        pre = nfi.NegativeSign;
                        post = nfi.PercentSymbol;
                        break;
                    case 2:
                        pre = nfi.NegativeSign + nfi.PercentSymbol;
                        break;
                    case 3:
                        pre = nfi.PercentSymbol + nfi.NegativeSign;
                        break;
                    case 4:
                        pre = nfi.PercentSymbol;
                        post = nfi.NegativeSign;
                        break;
                    case 5:
                        post = nfi.NegativeSign + nfi.PercentSymbol;
                        break;
                    case 6:
                        post = nfi.PercentSymbol + nfi.NegativeSign;
                        break;
                    case 7:
                        pre = nfi.NegativeSign + nfi.PercentSymbol + " ";
                        break;
                    case 8:
                        post = " " + nfi.PercentSymbol + nfi.NegativeSign;
                        break;
                    case 9:
                        pre = nfi.PercentSymbol + " ";
                        post = nfi.NegativeSign;
                        break;
                    case 10:
                        pre = nfi.PercentSymbol + " " + nfi.NegativeSign;
                        break;
                    case 11:
                        post = nfi.NegativeSign + " " + nfi.PercentSymbol;
                        break;
                }
            }
            else
            {
                switch (nfi.PercentPositivePattern)
                {
                    case 0:
                        post = " " + nfi.PercentSymbol;
                        break;
                    case 1:
                        post = nfi.PercentSymbol;
                        break;
                    case 2:
                        pre = nfi.PercentSymbol;
                        break;
                    case 3:
                        pre = nfi.PercentSymbol + " ";
                        break;
                }
            }

            if (input != "0")
            {
                input += "00";
            }
            return pre + GroupFormatDigits(input, nfi.PercentGroupSeparator, nfi.PercentGroupSizes, nfi.PercentDecimalSeparator, percision) + post;
        }
        private static String HexFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            bool upper = true;
            if (percision < 0)
            {
                percision = -percision;
                upper = false;
            }
            String output = ConvertDecimalToHex(input, upper, nfi);
            int typeChar = Int32.Parse(output.Substring(0, 1), NumberStyles.AllowHexSpecifier);

            if (typeChar >= 8)
            {
                output = FString(percision - output.Length, upper) + output;
            }
            else
            {
                output = ZeroString(percision - output.Length) + output;
            }

            return output;
        }
        private static String ZeroFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            String output = input;

            if (output.Equals("0"))
            {
                output = String.Empty;
            }
            if (output.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }

            if (output.Length > 50 && s_noZeroOut == false)
            {
                output = ZeroString(percision - output.Length) + output.Substring(0, 50) + ZeroString(output.Length - 50);
            }
            else
            {
                output = ZeroString(percision - output.Length) + output;
            }
            if (IsNeg)
            {
                output = nfi.NegativeSign + output;
            }

            return output;
        }
        private static String DecimalPointFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            String output = input;

            if (output.Equals("0"))
            {
                output = String.Empty;
            }
            if (output.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }

            if (output.Length > 50 && s_noZeroOut == false)
            {
                output = ZeroString(percision - output.Length) + output.Substring(0, 50) + ZeroString(output.Length - 50);
            }
            else
            {
                output = ZeroString(percision - output.Length) + output;
            }

            if (percision != 0)
            {
                output = output + nfi.NumberDecimalSeparator + ZeroString(percision);
            }
            if (IsNeg)
            {
                output = nfi.NegativeSign + output;
            }

            return output;
        }
        private static String ThousandsFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            String pre = String.Empty;

            if (input.StartsWith(nfi.NegativeSign))
            {
                input = input.Substring(nfi.NegativeSign.Length);
                pre = nfi.NegativeSign;
            }
            if (input.Equals("0"))
            {
                input = String.Empty;
            }
            input = ZeroString(percision - input.Length) + input;

            return pre + GroupFormatDigits(input, nfi.NumberGroupSeparator, nfi.NumberGroupSizes, String.Empty, 0);
        }
        private static String ScalingFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            String output = input;
            String part = String.Empty;
            bool roundUp = false;

            if (output.Equals("0"))
            {
                output = String.Empty;
            }
            if (output.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }

            if (output.Length > 50 && s_noZeroOut == false)
            {
                output = ZeroString(percision - output.Length) + output.Substring(0, 50) + ZeroString(output.Length - 50);
            }
            else
            {
                output = ZeroString(percision - output.Length) + output;
            }

            if (output.Length >= percision)
            {
                part = String.Empty;
                if (output[output.Length - 3] > '4')
                {
                    roundUp = true;
                }
                part = output.Substring(output.Length - percision, percision - 3);
                output = output.Substring(0, output.Length - percision);
                if (roundUp)
                {
                    if (part == String.Empty)
                    {
                        if (output == String.Empty)
                        {
                            output = "1";
                        }
                        else
                        {
                            output = (BigInteger.Parse(output) + 1).ToString("d");
                        }
                    }
                    else
                    {
                        part = (BigInteger.Parse(part) + 1).ToString("d" + (percision - 3));
                        if (part == "1" + ZeroString(percision - 3))
                        {
                            part = ZeroString(percision - 3);
                            if (output == String.Empty)
                            {
                                output = "1";
                            }
                            else
                            {
                                output = (BigInteger.Parse(output) + 1).ToString("d");
                            }
                        }
                    }
                }
            }
            else
            {
                output = String.Empty;
            }

            if (part != string.Empty)
            {
                output = output + nfi.NumberDecimalSeparator + part;
            }

            if (IsNeg && (output != String.Empty) && (output != (nfi.NumberDecimalSeparator + ZeroString(output.Length - nfi.NumberDecimalSeparator.Length))))
            {
                output = nfi.NegativeSign + output;
            }

            return output;
        }
        private static String PercentSymbolFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            String output = input;

            if (output.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }
            if (output.Equals("0"))
            {
                output = String.Empty;
            }
            else
            {
                output = output + "00";
            }
            if (output.Length > 50 && s_noZeroOut == false)
            {
                output = ZeroString(percision - output.Length) + output.Substring(0, 50) + ZeroString(output.Length - 50);
            }
            else
            {
                output = ZeroString(percision - output.Length) + output;
            }

            output = output.Substring(0, output.Length - percision) + nfi.PercentSymbol + output.Substring(output.Length - percision, percision);

            if (IsNeg)
            {
                output = nfi.NegativeSign + output;
            }

            return output;
        }
        private static String PerMilleSymbolFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            String output = input;

            if (output.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }
            if (output.Equals("0"))
            {
                output = String.Empty;
            }
            else
            {
                output = output + "000";
            }
            if (output.Length > 50 && s_noZeroOut == false)
            {
                output = ZeroString(percision - output.Length) + output.Substring(0, 50) + ZeroString(output.Length - 50);
            }
            else
            {
                output = ZeroString(percision - output.Length) + output;
            }

            output = output.Substring(0, output.Length - percision) + nfi.PerMilleSymbol + output.Substring(output.Length - percision, percision);

            if (IsNeg)
            {
                output = nfi.NegativeSign + output;
            }

            return output;
        }
        private static String ScientificFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            Char[] temp = input.ToCharArray();
            Char[] temp2;
            String output = String.Empty;
            String digits;
            Char exp = 'E';

            if (percision < 0)
            {
                percision = -percision;
                exp = 'e';
            }
            if (input.StartsWith(nfi.NegativeSign))
            {
                temp = new String(temp, nfi.NegativeSign.Length, temp.Length - nfi.NegativeSign.Length).ToCharArray();
                IsNeg = true;
            }

            digits = (temp.Length - 1).ToString();
            digits = ZeroString(3 - digits.Length) + digits;
            temp2 = temp;
            temp = new Char[percision + 2];
            for (int j = 0; j < temp.Length; j++)
            {
                if ((j < 50) && (j < temp2.Length))
                {
                    temp[j] = temp2[j];
                }
                else
                {
                    temp[j] = '0';
                }
            }

            int i = percision + 1;
            if (temp[i] >= '5')
            {
                i--;
                while ((i >= 0) && (temp[i] == '9'))
                {
                    temp[i] = '0';
                    i--;
                };
                if (i > -1)
                {
                    temp[i]++;
                }
                else
                {
                    temp[0] = '1';
                    digits = (Int32.Parse(digits) + 1).ToString("D3");
                    i = 0;
                }
            }
            else
            {
                i--;
            }

            if (IsNeg)
            {
                output = nfi.NegativeSign;
            }
            output = output + temp[0];
            if (percision != 0)
            {
                output = output + nfi.NumberDecimalSeparator + new String(temp, 1, percision);
            }
            output = output + exp + digits;

            return output;
        }
        private static String SignedScientificFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            Char[] temp = input.ToCharArray();
            Char[] temp2;
            String output = String.Empty;
            String digits;
            Char exp = 'E';

            if (percision < 0)
            {
                percision = -percision;
                exp = 'e';
            }
            if (input.StartsWith(nfi.NegativeSign))
            {
                temp = new String(temp, nfi.NegativeSign.Length, temp.Length - nfi.NegativeSign.Length).ToCharArray();
                IsNeg = true;
            }

            digits = (temp.Length - 1).ToString();
            digits = ZeroString(3 - digits.Length) + digits;
            temp2 = temp;
            temp = new Char[percision + 2];
            for (int j = 0; j < temp.Length; j++)
            {
                if ((j < 50) && (j < temp2.Length))
                {
                    temp[j] = temp2[j];
                }
                else
                {
                    temp[j] = '0';
                }
            }

            int i = percision + 1;
            if (temp[i] >= '5')
            {
                i--;
                while ((i >= 0) && (temp[i] == '9'))
                {
                    temp[i] = '0';
                    i--;
                };
                if (i > -1)
                {
                    temp[i]++;
                }
                else
                {
                    temp[0] = '1';
                    digits = (Int32.Parse(digits) + 1).ToString("D3");
                    i = 0;
                }
            }
            else
            {
                i--;
            }

            if (IsNeg)
            {
                output = nfi.NegativeSign;
            }
            output = output + temp[0];
            if (percision != 0)
            {
                output = output + nfi.NumberDecimalSeparator + new String(temp, 1, percision);
            }
            output = output + exp + nfi.PositiveSign + digits;

            return output;
        }
        private static String EmptyFormatter(String input, int percision, NumberFormatInfo nfi)
        {
            String temp = String.Empty;

            if (input.StartsWith(nfi.NegativeSign))
            {
                temp = nfi.NegativeSign;
            }

            return temp;
        }

        private static StringFormatter ExtraFormatter(StringFormatter formatter, String added)
        {
            return ExtraFormatter(formatter, added, 0);
        }
        private static StringFormatter ExtraFormatter(StringFormatter formatter, String added, int placesAfter)
        {
            StringFormatter sf = delegate (String input, int percision, NumberFormatInfo nfi)
            {
                String temp = formatter(input, percision, nfi);
                if (temp.Length < placesAfter)
                {
                    placesAfter = temp.Length;
                }
                temp = temp.Substring(0, temp.Length - placesAfter) + added + temp.Substring(temp.Length - placesAfter, placesAfter);
                return temp;
            };
            return sf;
        }
        private static StringFormatter CombinedFormatter(StringFormatter posFormatter, StringFormatter negFormatter)
        {
            return CombinedFormatter(posFormatter, negFormatter, posFormatter);
        }
        private static StringFormatter CombinedFormatter(StringFormatter posFormatter, StringFormatter negFormatter, StringFormatter zeroFormatter)
        {
            return CombinedFormatter(posFormatter, negFormatter, zeroFormatter, false);
        }
        private static StringFormatter CombinedFormatter(StringFormatter posFormatter, StringFormatter negFormatter, StringFormatter zeroFormatter, bool negInherited)
        {
            StringFormatter sf = delegate (String input, int percision, NumberFormatInfo nfi)
            {
                String temp = string.Empty;
                BigInteger bi = BigInteger.Parse(input);

                if (bi > 0)
                {
                    temp = posFormatter(input, percision, nfi);
                }
                if (bi == 0)
                {
                    temp = zeroFormatter(input, percision, nfi);
                }
                if (bi < 0)
                {
                    if (!negInherited)
                    {
                        input = input.Substring(nfi.NegativeSign.Length);
                    }
                    temp = negFormatter(input, percision, nfi);
                }

                return temp;
            };
            return sf;
        }

        private static bool VerifyToString(String test, String expectedResult)
        {
            return VerifyToString(test, false, null, false, null, false, expectedResult);
        }
        private static bool VerifyToString(String test, String format, String expectedResult)
        {
            return VerifyToString(test, true, format, false, null, false, expectedResult);
        }
        private static bool VerifyToString(String test, bool hasFormat, String format, bool hasProvider, IFormatProvider provider, bool expectError, String expectedResult)
        {
            bool ret = true;
            string result = null;

            try
            {
                if (hasFormat)
                {
                    if (hasProvider)
                    {
                        result = BigInteger.Parse(test, provider).ToString(format, provider);
                    }
                    else
                    {
                        result = BigInteger.Parse(test).ToString(format);
                    }
                }
                else
                {
                    if (hasProvider)
                    {
                        result = BigInteger.Parse(test, provider).ToString(provider);
                    }
                    else
                    {
                        result = BigInteger.Parse(test).ToString();
                    }
                }

                if (expectError)
                {
                    Console.WriteLine("Expected Exception not encountered.");
                    ret = false;
                }
                else
                {
                    if (expectedResult != result)
                    {
                        ret = false;
                        if (expectedResult.Length != result.Length)
                        {
                            Console.WriteLine("ToString values did not match: Expected:\r\n{0}.\r\nActual:\r\n{1}", expectedResult, result);
                        }
                        else
                        {
                            int index = expectedResult.LastIndexOf("E", StringComparison.OrdinalIgnoreCase);
                            if (index == 0)
                                throw new ArgumentException("E found at beginning of String: {0}", expectedResult);
                            if (index > 0)
                            {
                                var dig1 = (byte)expectedResult[index - 1];
                                var dig2 = (byte)result[index - 1];

                                if (dig2 == dig1 - 1 || dig2 == dig1 + 1)
                                    ret = true;
                                if (dig1 == '9' && dig2 == '0' || dig2 == '9' && dig1 == '0')
                                    ret = true;
                                if (index == 1 && (dig1 == '9' && dig2 == '1' || dig2 == '9' && dig1 == '1'))
                                    ret = true;
                            }
                        }
                    }

                    if (ret == false)
                    {
                        Console.WriteLine("ToString values did not match: Expected:\r\n{0}.\r\nActual:\r\n{1}", expectedResult, result);
                    }
                }
            }
            catch (Exception e)
            {
                if (e.GetType() == typeof(FormatException))
                {
                    if (expectError)
                    {
                        //Intentionally left blank.
                    }
                    else
                    {
                        Console.WriteLine("Unexpected Exception:" + e);
                        ret = false;
                    }
                }
                else
                {
                    Console.WriteLine("Unexpected Exception:" + e);
                    ret = false;
                }
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

            for (int i = 0; i < size; i++)
            {
                result += digits[random.Next(0, digits.Length)];
            }

            return result;
        }
        private static String GetRandomInvalidFormatChar(Random random)
        {
            Char[] digits = new Char[] { 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f', 'G', 'g', 'N', 'n', 'P', 'p', 'X', 'x', 'R', 'r' };
            Char result = 'C';
            while (result == 'C')
            {
                result = (Char)random.Next();
                for (int i = 0; i < digits.Length; i++)
                {
                    if (result < 'A')
                        result = 'C';
                    if (result > 'Z')
                        result = 'C';
                    if (result == (char)digits[i])
                        result = 'C';
                }
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
            String output = input;

            while (output.StartsWith("0") & (output.Length > 1))
            {
                output = output.Substring(1);
            }
            if (isHex)
            {
                output = ConvertHexToDecimal(output);
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

            return output;
        }
        private static String ConvertHexToDecimal(string input)
        {
            char[] inArr = input.ToCharArray();
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

            bool IsPos = (x >= 0);
            List<char> number = new List<char>();

            if (!IsPos)
            {
                x = -x;
            }

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
                if (!IsPos)
                {
                    number.Add(CultureInfo.CurrentUICulture.NumberFormat.NegativeSign.ToCharArray()[0]);
                }
                number.Reverse();
            }
            String y2 = new String(number.ToArray());
            return y2;
        }
        private static String ConvertDecimalToHex(string input, bool upper, NumberFormatInfo nfi)
        {
            String output = string.Empty;
            BigInteger bi = BigInteger.Parse(input, nfi);
            Byte[] bytes = bi.ToByteArray();
            int[] chars = new int[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[2 * i] = bytes[i] % 16;
                chars[2 * i + 1] = bytes[i] / 16;
            }

            int start = chars.Length - 1;
            if (((chars[chars.Length - 1] == 0) && (chars[chars.Length - 2] < 8)) ||
                ((chars[chars.Length - 1] == 15) && (chars[chars.Length - 2] > 7)))
            {
                start--;
            }
            for (; start >= 0; start--)
            {
                if (upper)
                {
                    output = output + chars[start].ToString("X");
                }
                else
                {
                    output = output + chars[start].ToString("x");
                }
            }

            return output;
        }
        private static String ConvertToExp(string input, int places)
        {
            Char[] temp = input.Substring(0, places + 2).ToCharArray();
            String ret = null;
            int i = places + 1;

            if (temp[i] >= '5')
            {
                i--;
                while ((i >= 0) && (temp[i] == '9'))
                {
                    temp[i] = '0';
                    i--;
                };
                if (i > -1)
                {
                    temp[i]++;
                }
                else
                {
                    temp[0] = '1';
                    input += "0";
                    i = 0;
                }
            }
            else
            {
                i--;
            }

            while ((temp[i] == '0') && (i != 0))
            {
                i--;
            }

            ret = temp[0] + CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
            ret += ((i == 0) ? "0" : new String(temp).Substring(1, i));
            ret += "E" + CultureInfo.CurrentUICulture.NumberFormat.PositiveSign + (input.Length - 1);

            return ret;
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

        private static String GroupFormatDigits(String input, String seperator, int[] sizes, String point, int places)
        {
            String output = String.Empty;
            int currentspot = input.Length - 1;
            int currentsize = 0;
            int size = 0;
            bool tempsize = false;

            if (s_noZeroOut == false && false)
            {
                while (currentspot > 49)
                {
                    size = sizes[currentsize];
                    if ((size == 0) || (size > (currentspot - 49)))
                    {
                        int zerosize = currentspot - 49;
                        output = ZeroString(zerosize) + output;
                        if (size != 0)
                        {
                            size = size - zerosize;
                        }
                        tempsize = true;
                        currentspot = 49;
                    }
                    else
                    {
                        output = seperator + ZeroString(size) + output;
                        currentspot -= size;
                    }
                    if (currentsize < sizes.Length - 1)
                    {
                        currentsize++;
                    }
                }
            }

            while (currentspot >= 0)
            {
                if (!tempsize)
                {
                    size = sizes[currentsize];
                }
                else
                {
                    tempsize = false;
                }
                if ((size == 0) || (size > currentspot))
                {
                    output = input.Substring(0, currentspot + 1) + output;
                    currentspot = -1;
                }
                else
                {
                    output = seperator + input.Substring(currentspot - size + 1, size) + output;
                    currentspot -= size;
                }
                if (currentsize < sizes.Length - 1)
                {
                    currentsize++;
                }
            }

            if (places != 0)
            {
                output += point + ZeroString(places);
            }

            return output;
        }
        private static String ZeroString(int size)
        {
            String ret = String.Empty;
            if (size >= 1)
            {
                ret = new String('0', size);
            }
            return ret;
        }
        private static String FString(int size, bool upper)
        {
            String ret = String.Empty;
            if (size >= 1)
            {
                if (upper)
                {
                    ret = new String('F', size);
                }
                else
                {
                    ret = new String('f', size);
                }
            }
            return ret;
        }

        private static NumberFormatInfo MarkUp(NumberFormatInfo nfi)
        {
            nfi.CurrencyDecimalDigits = 0;
            nfi.CurrencyDecimalSeparator = "!!";
            nfi.CurrencyGroupSeparator = "##";
            nfi.CurrencyGroupSizes = new int[] { 2 };
            nfi.CurrencyNegativePattern = 4;
            nfi.CurrencyPositivePattern = 2;
            nfi.CurrencySymbol = "@@";

            nfi.NumberDecimalDigits = 0;
            nfi.NumberDecimalSeparator = "^^";
            nfi.NumberGroupSeparator = "&&";
            nfi.NumberGroupSizes = new int[] { 4 };
            nfi.NumberNegativePattern = 4;

            nfi.PercentDecimalDigits = 0;
            nfi.PercentDecimalSeparator = "**";
            nfi.PercentGroupSeparator = "++";
            nfi.PercentGroupSizes = new int[] { 5 };
            nfi.PercentNegativePattern = 2;
            nfi.PercentPositivePattern = 2;
            nfi.PercentSymbol = "??";
            nfi.PerMilleSymbol = "~~";

            nfi.NegativeSign = "<<";
            nfi.PositiveSign = ">>";

            return nfi;
        }

        public static bool Eval<T>(T expected, T actual, String errorMsg)
        {
            bool retValue = expected == null ? actual == null : expected.Equals(actual);
            return Eval(retValue, errorMsg + " Expected:" + (null == expected ? "<null>" : expected.ToString()) + " Actual:" + (null == actual ? "<null>" : actual.ToString()));

            //if (!retValue)
            //{
            //    Eval(retValue, errorMsg + " Expected:" + (null == expected ? "<null>" : expected.ToString()) + " Actual:" + (null == actual ? "<null>" : actual.ToString()));
            //    throw new ApplicationException();
            //}


            //return true;
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
                Assert.True(y2.Equals(y, StringComparison.OrdinalIgnoreCase), " Verification Failed");
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
