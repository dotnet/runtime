// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Numerics.Tests
{
    public class ToStringTest
    {
        private static bool s_noZeroOut = true;

        public delegate string StringFormatter(string input, int precision, NumberFormatInfo nfi);
        private static int s_samples = 1;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunSimpleToStringTests()
        {
            string test;

            // Scenario 1: Large BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, s_random);
                VerifyToString(test, test);
            }

            // Scenario 2: Small BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, s_random);
                VerifyToString(test, test);
            }

            // Scenario 3: Large BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, s_random);
                VerifyToString(CultureInfo.CurrentCulture.NumberFormat.NegativeSign + test, CultureInfo.CurrentCulture.NumberFormat.NegativeSign + test);
            }

            // Scenario 4: Small BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, s_random);
                VerifyToString(CultureInfo.CurrentCulture.NumberFormat.NegativeSign + test, CultureInfo.CurrentCulture.NumberFormat.NegativeSign + test);
            }

            // Scenario 5: Constant values
            VerifyToString(CultureInfo.CurrentCulture.NumberFormat.NegativeSign + "1", CultureInfo.CurrentCulture.NumberFormat.NegativeSign + "1");
            VerifyToString("0", "0");
            VerifyToString(Int16.MinValue.ToString(), Int16.MinValue.ToString());
            VerifyToString(Int32.MinValue.ToString(), Int32.MinValue.ToString());
            VerifyToString(Int64.MinValue.ToString(), Int64.MinValue.ToString());
            VerifyToString(Decimal.MinValue.ToString(), Decimal.MinValue.ToString());
            VerifyToString(Int16.MaxValue.ToString(), Int16.MaxValue.ToString());
            VerifyToString(Int32.MaxValue.ToString(), Int32.MaxValue.ToString());
            VerifyToString(Int64.MaxValue.ToString(), Int64.MaxValue.ToString());
            VerifyToString(Decimal.MaxValue.ToString(), Decimal.MaxValue.ToString());
        }

        [Fact]
        public static void RunProviderToStringTests()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi = MarkUp(nfi);

            RunSimpleProviderToStringTests(s_random, "", nfi, 0, DecimalFormatter);
            RunSimpleProviderToStringTests(s_random, "C", nfi, nfi.CurrencyDecimalDigits, CurrencyFormatter);
            RunSimpleProviderToStringTests(s_random, "D", nfi, 0, DecimalFormatter);
            RunSimpleProviderToStringTests(s_random, "E", nfi, 6, ExponentialFormatter);
            RunSimpleProviderToStringTests(s_random, "F", nfi, nfi.NumberDecimalDigits, FixedFormatter);
            RunSimpleProviderToStringTests(s_random, "G", nfi, 0, DecimalFormatter);
            RunSimpleProviderToStringTests(s_random, "N", nfi, nfi.NumberDecimalDigits, NumberFormatter);
            RunSimpleProviderToStringTests(s_random, "P", nfi, nfi.PercentDecimalDigits, PercentFormatter);
            RunSimpleProviderToStringTests(s_random, "X", nfi, 0, HexFormatter);
            RunSimpleProviderToStringTests(s_random, "R", nfi, 0, DecimalFormatter);
        }

        [Fact]
        public static void RunStandardFormatToStringTests()
        {
            string test;
            string format;

            long intMaxPlus1 = ((long)int.MaxValue + 1);
            string intMaxPlus1String = intMaxPlus1.ToString();
            // Currency
            RunStandardFormatToStringTests_Helper(s_random, "C", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalDigits, CurrencyFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "c0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, CurrencyFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "C1", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, CurrencyFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "c2", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 2, CurrencyFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "C5", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 5, CurrencyFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "c33", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 33, CurrencyFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "C99", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 99, CurrencyFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "C100", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 100, CurrencyFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "C" + intMaxPlus1String, CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 101, CurrencyFormatter, true);

            // Decimal
            RunStandardFormatToStringTests_Helper(s_random, "D", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "d0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "D1", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "D0000001", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "d2", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 2, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "D5", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 5, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "d33", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 33, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "D99", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 99, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "D100", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 100, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "D" + intMaxPlus1String, CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 101, DecimalFormatter, true);
            RunStandardFormatToStringTests_Helper(s_random, "D\0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "D4\0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 4, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "D4\0Z", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 4, DecimalFormatter);

            // Exponential (note: negative precision means lower case e)
            RunStandardFormatToStringTests_Helper(s_random, "E", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 6, ExponentialFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "E0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, ExponentialFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "E1", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExponentialFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "e2", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, -2, ExponentialFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "E5", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 5, ExponentialFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "e33", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, -33, ExponentialFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "E99", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 99, ExponentialFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "E100", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 100, ExponentialFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "E" + intMaxPlus1String, CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 101, ExponentialFormatter, true);

            // Test exponent of 4 digits
            test = GetDigitSequence(2000, 2000, s_random);
            VerifyToString(test, "E", ExponentialFormatter(test, 6, CultureInfo.CurrentCulture.NumberFormat));

            // Fixed-Point
            RunStandardFormatToStringTests_Helper(s_random, "f", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalDigits, FixedFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "F0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, FixedFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "f1", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, FixedFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "F2", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 2, FixedFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "f5", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 5, FixedFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "F33", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 33, FixedFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "f99", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 99, FixedFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "f100", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 100, FixedFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "f" + intMaxPlus1String, CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 101, FixedFormatter, true);

            // General
            RunStandardFormatToStringTests_Helper(s_random, "g", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "G0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "G1", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "G2", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 2, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "g5", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 5, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "G33", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 33, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "g99", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 99, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "g100", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 100, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "g" + intMaxPlus1String, CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 101, DecimalFormatter, true);

            // Number
            RunStandardFormatToStringTests_Helper(s_random, "n", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalDigits, NumberFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "N0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, NumberFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "N1", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, NumberFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "N2", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 2, NumberFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "n5", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 5, NumberFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "N33", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 33, NumberFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "n99", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 99, NumberFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "n100", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 100, NumberFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "n" + intMaxPlus1String, CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 101, NumberFormatter, true);

            // Percent
            RunStandardFormatToStringTests_Helper(s_random, "p", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, CultureInfo.CurrentCulture.NumberFormat.PercentDecimalDigits, PercentFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "P0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, PercentFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "P1", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, PercentFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "P2", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 2, PercentFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "p5", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 5, PercentFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "P33", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 33, PercentFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "p99", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 99, PercentFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "p100", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 100, PercentFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "p" + intMaxPlus1String, CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 101, PercentFormatter, true);

            // Hex
            RunStandardFormatToStringTests_Helper(s_random, "X", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, HexFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "X0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, HexFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "x1", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, -1, HexFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "X2", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 2, HexFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "x5", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, -5, HexFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "X33", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 33, HexFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "x99", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, -99, HexFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "x100", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, -100, HexFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "x" + intMaxPlus1String, CultureInfo.CurrentCulture.NumberFormat.NegativeSign, -101, HexFormatter, true);

            // RoundTrip
            RunStandardFormatToStringTests_Helper(s_random, "R", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "R0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "r1", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "R2", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 2, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "r5", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 5, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "R33", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 33, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "r99", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 99, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "r100", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 100, DecimalFormatter);
            RunStandardFormatToStringTests_Helper(s_random, "r" + intMaxPlus1String, CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 101, DecimalFormatter, true);

            // Other - invalid format characters
            for (int i = 0; i < s_samples; i++)
            {
                format = GetRandomInvalidFormatChar(s_random);
                test = GetDigitSequence(10, 100, s_random);
                VerifyToString(test, format, null, true, null);
            }
        }

        [Fact]
        [SkipOnCoreClr("Long running tests: https://github.com/dotnet/runtime/issues/11980", TestPlatforms.Linux, ~RuntimeConfiguration.Release)]
        public static void RunRegionSpecificStandardFormatToStringTests()
        {
            CultureInfo[] cultures = new CultureInfo[] { new CultureInfo("en-US"), new CultureInfo("en-GB"), new CultureInfo("fr-CA"),
                                                            new CultureInfo("ar-SA"), new CultureInfo("de-DE"), new CultureInfo("he-IL"),
                                                            new CultureInfo("ru-RU"), new CultureInfo("zh-CN") };
            long intMaxPlus1 = ((long)int.MaxValue + 1);
            string intMaxPlus1String = intMaxPlus1.ToString();
            foreach (CultureInfo culture in cultures)
            {
                // Set CurrentCulture to simulate different locales
                using (new ThreadCultureChange(culture))
                {
                    // Currency
                    RunStandardFormatToStringTests_Helper(s_random, "C", culture.NumberFormat.NegativeSign, culture.NumberFormat.CurrencyDecimalDigits, CurrencyFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "c0", culture.NumberFormat.NegativeSign, 0, CurrencyFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "C1", culture.NumberFormat.NegativeSign, 1, CurrencyFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "c2", culture.NumberFormat.NegativeSign, 2, CurrencyFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "C5", culture.NumberFormat.NegativeSign, 5, CurrencyFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "c33", culture.NumberFormat.NegativeSign, 33, CurrencyFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "C99", culture.NumberFormat.NegativeSign, 99, CurrencyFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "C100", culture.NumberFormat.NegativeSign, 100, CurrencyFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "C" + intMaxPlus1String, culture.NumberFormat.NegativeSign, 101, CurrencyFormatter, true);

                    // Decimal
                    RunStandardFormatToStringTests_Helper(s_random, "D", culture.NumberFormat.NegativeSign, 0, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "d0", culture.NumberFormat.NegativeSign, 0, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "D1", culture.NumberFormat.NegativeSign, 1, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "d2", culture.NumberFormat.NegativeSign, 2, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "D5", culture.NumberFormat.NegativeSign, 5, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "d33", culture.NumberFormat.NegativeSign, 33, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "D99", culture.NumberFormat.NegativeSign, 99, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "D100", culture.NumberFormat.NegativeSign, 100, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "D" + intMaxPlus1String, culture.NumberFormat.NegativeSign, 101, DecimalFormatter, true);

                    // Exponential (note: negative precision means lower case e)
                    RunStandardFormatToStringTests_Helper(s_random, "E", culture.NumberFormat.NegativeSign, 6, ExponentialFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "E0", culture.NumberFormat.NegativeSign, 0, ExponentialFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "E1", culture.NumberFormat.NegativeSign, 1, ExponentialFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "e2", culture.NumberFormat.NegativeSign, -2, ExponentialFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "E5", culture.NumberFormat.NegativeSign, 5, ExponentialFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "e33", culture.NumberFormat.NegativeSign, -33, ExponentialFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "E99", culture.NumberFormat.NegativeSign, 99, ExponentialFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "E100", culture.NumberFormat.NegativeSign, 100, ExponentialFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "E" + intMaxPlus1String, culture.NumberFormat.NegativeSign, 101, ExponentialFormatter, true);

                    // Fixed-Point
                    RunStandardFormatToStringTests_Helper(s_random, "f", culture.NumberFormat.NegativeSign, culture.NumberFormat.NumberDecimalDigits, FixedFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "F0", culture.NumberFormat.NegativeSign, 0, FixedFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "f1", culture.NumberFormat.NegativeSign, 1, FixedFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "F2", culture.NumberFormat.NegativeSign, 2, FixedFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "f5", culture.NumberFormat.NegativeSign, 5, FixedFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "F33", culture.NumberFormat.NegativeSign, 33, FixedFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "f99", culture.NumberFormat.NegativeSign, 99, FixedFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "f100", culture.NumberFormat.NegativeSign, 100, FixedFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "f" + intMaxPlus1String, culture.NumberFormat.NegativeSign, 101, FixedFormatter, true);

                    // General
                    RunStandardFormatToStringTests_Helper(s_random, "g", culture.NumberFormat.NegativeSign, 0, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "G0", culture.NumberFormat.NegativeSign, 0, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "G1", culture.NumberFormat.NegativeSign, 1, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "G2", culture.NumberFormat.NegativeSign, 2, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "g5", culture.NumberFormat.NegativeSign, 5, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "G33", culture.NumberFormat.NegativeSign, 33, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "g99", culture.NumberFormat.NegativeSign, 99, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "g100", culture.NumberFormat.NegativeSign, 100, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "g" + intMaxPlus1String, culture.NumberFormat.NegativeSign, 101, DecimalFormatter, true);

                    // Number
                    RunStandardFormatToStringTests_Helper(s_random, "n", culture.NumberFormat.NegativeSign, culture.NumberFormat.NumberDecimalDigits, NumberFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "N0", culture.NumberFormat.NegativeSign, 0, NumberFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "N1", culture.NumberFormat.NegativeSign, 1, NumberFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "N2", culture.NumberFormat.NegativeSign, 2, NumberFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "n5", culture.NumberFormat.NegativeSign, 5, NumberFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "N33", culture.NumberFormat.NegativeSign, 33, NumberFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "n99", culture.NumberFormat.NegativeSign, 99, NumberFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "n100", culture.NumberFormat.NegativeSign, 100, NumberFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "n" + intMaxPlus1String, culture.NumberFormat.NegativeSign, 101, NumberFormatter, true);

                    // Percent
                    RunStandardFormatToStringTests_Helper(s_random, "p", culture.NumberFormat.NegativeSign, culture.NumberFormat.PercentDecimalDigits, PercentFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "P0", culture.NumberFormat.NegativeSign, 0, PercentFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "P1", culture.NumberFormat.NegativeSign, 1, PercentFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "P2", culture.NumberFormat.NegativeSign, 2, PercentFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "p5", culture.NumberFormat.NegativeSign, 5, PercentFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "P33", culture.NumberFormat.NegativeSign, 33, PercentFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "p99", culture.NumberFormat.NegativeSign, 99, PercentFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "p100", culture.NumberFormat.NegativeSign, 100, PercentFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "p" + intMaxPlus1String, culture.NumberFormat.NegativeSign, 101, PercentFormatter, true);

                    // Hex
                    RunStandardFormatToStringTests_Helper(s_random, "X", culture.NumberFormat.NegativeSign, 0, HexFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "X0", culture.NumberFormat.NegativeSign, 0, HexFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "x1", culture.NumberFormat.NegativeSign, -1, HexFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "X2", culture.NumberFormat.NegativeSign, 2, HexFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "x5", culture.NumberFormat.NegativeSign, -5, HexFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "X33", culture.NumberFormat.NegativeSign, 33, HexFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "x99", culture.NumberFormat.NegativeSign, -99, HexFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "x100", culture.NumberFormat.NegativeSign, -100, HexFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "x" + intMaxPlus1String, culture.NumberFormat.NegativeSign, -101, HexFormatter, true);

                    // RoundTrip
                    RunStandardFormatToStringTests_Helper(s_random, "R", culture.NumberFormat.NegativeSign, 0, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "R0", culture.NumberFormat.NegativeSign, 0, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "r1", culture.NumberFormat.NegativeSign, 1, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "R2", culture.NumberFormat.NegativeSign, 2, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "r5", culture.NumberFormat.NegativeSign, 5, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "R33", culture.NumberFormat.NegativeSign, 33, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "r99", culture.NumberFormat.NegativeSign, 99, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "r100", culture.NumberFormat.NegativeSign, 100, DecimalFormatter);
                    RunStandardFormatToStringTests_Helper(s_random, "r" + intMaxPlus1String, culture.NumberFormat.NegativeSign, 101, DecimalFormatter, true);
                }
            }
        }

        [Fact]
        public static void RunCustomFormatZeroPlaceholder()
        {
            // Zero Placeholder
            RunCustomFormatToStringTests(s_random, "0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ZeroFormatter);
            RunCustomFormatToStringTests(s_random, "0000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 4, ZeroFormatter);
            RunCustomFormatToStringTests(s_random, new string('0', 500), CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 500, ZeroFormatter);
        }

        [Fact]
        public static void RunCustomFormatDigitPlaceholder()
        {
            // Digit Placeholder
            RunCustomFormatToStringTests(s_random, "#", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, ZeroFormatter);
            RunCustomFormatToStringTests(s_random, "####", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, ZeroFormatter);
            RunCustomFormatToStringTests(s_random, new string('#', 500), CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, ZeroFormatter);
        }

        [Fact]
        public static void RunCustomFormatDecimalPoint()
        {
            // Decimal Point (match required digits before and after point with precision)
            RunCustomFormatToStringTests(s_random, "#.#", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, DecimalPointFormatter);
            RunCustomFormatToStringTests(s_random, "00.00", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 2, DecimalPointFormatter);
            RunCustomFormatToStringTests(s_random, "0000.0.00.0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 4, DecimalPointFormatter);
        }

        [Fact]
        public static void RunCustomFormatThousandsSeparator()
        {
            // Thousands Separator
            RunCustomFormatToStringTests(s_random, "#,#", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, ThousandsFormatter);
            RunCustomFormatToStringTests(s_random, "00,00", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 4, ThousandsFormatter);
            RunCustomFormatToStringTests(s_random, "0000,0,00,0", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 8, ThousandsFormatter);
        }

        [Fact]
        public static void RunCustomFormatNumberScaling()
        {
            // Number Scaling (match scale factor to decimal places+3
            RunCustomFormatToStringTests(s_random, "#,", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 3, ScalingFormatter);
            RunCustomFormatToStringTests(s_random, "#,,.000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 6, ScalingFormatter);
            RunCustomFormatToStringTests(s_random, "#,,,.000000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 9, ScalingFormatter);
        }

        [Fact]
        public static void RunCustomFormatPercentSign()
        {
            // Percent Sign
            RunCustomFormatToStringTests(s_random, "#%", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, PercentSymbolFormatter);
            RunCustomFormatToStringTests(s_random, "#%000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 3, PercentSymbolFormatter);
            RunCustomFormatToStringTests(s_random, "#%000000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 6, PercentSymbolFormatter);
        }

        [Fact]
        public static void RunCustomFormatScientificNotation()
        {
            // Scientific Notation
            RunCustomFormatToStringTests(s_random, "0.000000E000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 6, ScientificFormatter);
            RunCustomFormatToStringTests(s_random, "0.000000E-000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 6, ScientificFormatter);
            RunCustomFormatToStringTests(s_random, "0.000000E+000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 6, SignedScientificFormatter);
            RunCustomFormatToStringTests(s_random, "0.000000e000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, -6, ScientificFormatter);
            RunCustomFormatToStringTests(s_random, "0.000000e-000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, -6, ScientificFormatter);
            RunCustomFormatToStringTests(s_random, "0.000000e+000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, -6, SignedScientificFormatter);
        }

        [Fact]
        public static void RunCustomFormatEscapeChar()
        {
            // Escape Character
            RunCustomFormatToStringTests(s_random, "0\\\'", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "\'"));
            RunCustomFormatToStringTests(s_random, "0\\\"", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "\""));
            RunCustomFormatToStringTests(s_random, "0\\%", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "%"));
            RunCustomFormatToStringTests(s_random, "0\n", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "\n"));
        }

        [Fact]
        public static void RunCustomFormatLiterals()
        {
            // Literals
            RunCustomFormatToStringTests(s_random, "0\'.0\'", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, ".0"));
            RunCustomFormatToStringTests(s_random, "0\".0\"", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, ".0"));
            RunCustomFormatToStringTests(s_random, "0\',0\'", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, ",0"));
            RunCustomFormatToStringTests(s_random, "0\",0\"", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, ",0"));
            RunCustomFormatToStringTests(s_random, "0\',\'", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, ","));
            RunCustomFormatToStringTests(s_random, "0\",\"", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, ","));
            RunCustomFormatToStringTests(s_random, "0\'%\'", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "%"));
            RunCustomFormatToStringTests(s_random, "0\"%\"", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "%"));
            RunCustomFormatToStringTests(s_random, "0\'E+0\'", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "E+0"));
            RunCustomFormatToStringTests(s_random, "0\"E+0\"", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "E+0"));
            RunCustomFormatToStringTests(s_random, "0\'\\\'", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "\\"));
            RunCustomFormatToStringTests(s_random, "0\"\\\"", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 1, ExtraFormatter(ZeroFormatter, "\\"));
            RunCustomFormatToStringTests(s_random, "#\',\'%", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, ExtraFormatter(PercentSymbolFormatter, ",", CultureInfo.CurrentCulture.NumberFormat.PercentSymbol.Length));
            RunCustomFormatToStringTests(s_random, "000\",\".000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 3, ExtraFormatter(DecimalPointFormatter, ",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator.Length + 3));
        }

        [Fact]
        public static void RunCustomFormatSeparator()
        {
            // Separator
            RunCustomFormatToStringTests(s_random, "00.00;0.00E000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 2, CombinedFormatter(DecimalPointFormatter, ScientificFormatter));
            RunCustomFormatToStringTests(s_random, "00.00;;0.00E000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 2, CombinedFormatter(DecimalPointFormatter, DecimalPointFormatter, ScientificFormatter, true));
            RunCustomFormatToStringTests(s_random, "00.00;#%00;0.00E000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 2, CombinedFormatter(DecimalPointFormatter, PercentSymbolFormatter, ScientificFormatter));
            RunCustomFormatToStringTests(s_random, "00000000000000000000000000000.00000000000000000000000000000;", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 29, DecimalPointFormatter);
            RunCustomFormatToStringTests(s_random, "00000000000000000000000000000.00000000000000000000000000000;;", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 29, DecimalPointFormatter);
            RunCustomFormatToStringTests(s_random, ";", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 2, CombinedFormatter(EmptyFormatter, EmptyFormatter, EmptyFormatter, true));
        }

        [Fact]
        public static void CustomFormatPerMille()
        {
            // PerMillie Symbol
            RunCustomFormatToStringTests(s_random, "#\u2030", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 0, PerMilleSymbolFormatter);
            RunCustomFormatToStringTests(s_random, "#\u2030000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 3, PerMilleSymbolFormatter);
            RunCustomFormatToStringTests(s_random, "#\u2030000000", CultureInfo.CurrentCulture.NumberFormat.NegativeSign, 6, PerMilleSymbolFormatter);
        }

        private static void RunSimpleProviderToStringTests(Random random, string format, NumberFormatInfo provider, int precision, StringFormatter formatter)
        {
            string test;

            // Scenario 1: Large BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, random);
                VerifyToString(test, format, provider, false, formatter(test, precision, provider));
            }

            // Scenario 2: Small BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, random);
                VerifyToString(test, format, provider, false, formatter(test, precision, provider));
            }

            // Scenario 3: Large BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, random);
                VerifyToString(provider.NegativeSign + test, format, provider, false, formatter(provider.NegativeSign + test, precision, provider));
            }

            // Scenario 4: Small BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, random);
                VerifyToString(provider.NegativeSign + test, format, provider, false, formatter(provider.NegativeSign + test, precision, provider));
            }

            // Scenario 5: Constant values
            VerifyToString(provider.NegativeSign + "1", format, provider, false, formatter(provider.NegativeSign + "1", precision, provider));
            VerifyToString(provider.NegativeSign + "0", format, provider, false, formatter("0", precision, provider));
            VerifyToString("0", format, provider, false, formatter("0", precision, provider));
            VerifyToString(Int16.MinValue.ToString("d", provider), format, provider, false, formatter(Int16.MinValue.ToString("d", provider), precision, provider));
            VerifyToString(Int32.MinValue.ToString("d", provider), format, provider, false, formatter(Int32.MinValue.ToString("d", provider), precision, provider));
            VerifyToString(Int64.MinValue.ToString("d", provider), format, provider, false, formatter(Int64.MinValue.ToString("d", provider), precision, provider));
            VerifyToString(Int16.MaxValue.ToString("d", provider), format, provider, false, formatter(Int16.MaxValue.ToString("d", provider), precision, provider));
            VerifyToString(Int32.MaxValue.ToString("d", provider), format, provider, false, formatter(Int32.MaxValue.ToString("d", provider), precision, provider));
            VerifyToString(Int64.MaxValue.ToString("d", provider), format, provider, false, formatter(Int64.MaxValue.ToString("d", provider), precision, provider));
        }

        private static void RunStandardFormatToStringTests_Helper(Random random, string format, string negativeSign, int precision, StringFormatter formatter, bool expectError = false)
        {
            string test;

            // Scenario 1: Large BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, random);
                VerifyToString(test, format, formatter(test, precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            }

            // Scenario 2: Small BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, random);
                VerifyToString(test, format, formatter(test, precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            }

            // Scenario 3: Large BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, random);
                VerifyToString(negativeSign + test, format, formatter(negativeSign + test, precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            }

            // Scenario 4: Small BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, random);
                VerifyToString(negativeSign + test, format, formatter(negativeSign + test, precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            }

            // Scenario 5: Constant values
            VerifyToString(negativeSign + "1", format, formatter(negativeSign + "1", precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            VerifyToString(negativeSign + "0", format, formatter("0", precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            VerifyToString("0", format, formatter("0", precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            VerifyToString(Int16.MinValue.ToString(), format, formatter(Int16.MinValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            VerifyToString(Int32.MinValue.ToString(), format, formatter(Int32.MinValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            VerifyToString(Int64.MinValue.ToString(), format, formatter(Int64.MinValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            VerifyToString(Decimal.MinValue.ToString(), format, formatter(Decimal.MinValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            VerifyToString(Int16.MaxValue.ToString(), format, formatter(Int16.MaxValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            VerifyToString(Int32.MaxValue.ToString(), format, formatter(Int32.MaxValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            VerifyToString(Int64.MaxValue.ToString(), format, formatter(Int64.MaxValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
            VerifyToString(Decimal.MaxValue.ToString(), format, formatter(Decimal.MaxValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat), expectError);
        }

        private static void RunCustomFormatToStringTests(Random random, string format, string negativeSign, int precision, StringFormatter formatter)
        {
            string test;

            // Scenario 1: Large BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, random);
                VerifyToString(test, format, formatter(test, precision, CultureInfo.CurrentCulture.NumberFormat));
            }

            // Scenario 2: Small BigInteger - positive
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, random);
                VerifyToString(test, format, formatter(test, precision, CultureInfo.CurrentCulture.NumberFormat));
            }

            // Scenario 3: Large BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(100, 1000, random);
                VerifyToString(negativeSign + test, format, formatter(negativeSign + test, precision, CultureInfo.CurrentCulture.NumberFormat));
            }

            // Scenario 4: Small BigInteger - negative
            for (int i = 0; i < s_samples; i++)
            {
                test = GetDigitSequence(1, 20, random);
                VerifyToString(negativeSign + test, format, formatter(negativeSign + test, precision, CultureInfo.CurrentCulture.NumberFormat));
            }

            // Scenario 5: Constant values
            VerifyToString(negativeSign + "1", format, formatter(negativeSign + "1", precision, CultureInfo.CurrentCulture.NumberFormat));
            VerifyToString(negativeSign + "0", format, formatter("0", precision, CultureInfo.CurrentCulture.NumberFormat));
            VerifyToString("0", format, formatter("0", precision, CultureInfo.CurrentCulture.NumberFormat));
            VerifyToString(Int16.MinValue.ToString(), format, formatter(Int16.MinValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat));
            VerifyToString(Int32.MinValue.ToString(), format, formatter(Int32.MinValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat));
            VerifyToString(Int64.MinValue.ToString(), format, formatter(Int64.MinValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat));
            VerifyToString(Decimal.MinValue.ToString(), format, formatter(Decimal.MinValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat));
            VerifyToString(Int16.MaxValue.ToString(), format, formatter(Int16.MaxValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat));
            VerifyToString(Int32.MaxValue.ToString(), format, formatter(Int32.MaxValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat));
            VerifyToString(Int64.MaxValue.ToString(), format, formatter(Int64.MaxValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat));
            VerifyToString(Decimal.MaxValue.ToString(), format, formatter(Decimal.MaxValue.ToString(), precision, CultureInfo.CurrentCulture.NumberFormat));
        }

        private static string CurrencyFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            string pre = string.Empty;
            string post = string.Empty;

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

            return pre + GroupFormatDigits(input, nfi.CurrencyGroupSeparator, nfi.CurrencyGroupSizes, nfi.CurrencyDecimalSeparator, precision) + post;
        }

        private static string DecimalFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            string output = input;

            if (input.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }

            output = ZeroString(precision - output.Length) + output;

            if (IsNeg)
            {
                output = nfi.NegativeSign + output;
            }

            return output;
        }

        private static string ExponentialFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            char[] temp = input.ToCharArray();
            char[] temp2;
            string output = string.Empty;
            string digits;
            char exp = 'E';

            if (precision < 0)
            {
                precision = -precision;
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
            temp = new char[precision + 2];
            for (int j = 0; j < temp.Length; j++)
            {
                if ((j < temp2.Length))
                {
                    temp[j] = temp2[j];
                }
                else
                {
                    temp[j] = '0';
                }
            }

            int i = precision + 1;
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
            if (precision != 0)
            {
                output = output + nfi.NumberDecimalSeparator + new string(temp, 1, precision);
            }
            output = output + exp + nfi.PositiveSign + digits;

            return output;
        }

        private static string FixedFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            string output = input;

            if (input.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }

            if (IsNeg)
            {
                output = nfi.NegativeSign + output;
            }

            if (precision > 0)
            {
                output = output + nfi.NumberDecimalSeparator + ZeroString(precision);
            }

            return output;
        }

        private static string GeneralFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            string output = input;
            bool lowercase = false;
            char exp = 'E';

            if (precision < 0)
            {
                lowercase = true;
                exp = 'e';
                precision = -precision;
            }
            if (input.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
            }

            if (output.Length > precision)
            {
                int newprecision = (precision == 0 ? 0 : precision - 1);
                if (lowercase)
                {
                    newprecision = -newprecision;
                }
                output = ExponentialFormatter(input, newprecision, nfi);

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

        private static string NumberFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            string pre = string.Empty;
            string post = string.Empty;

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

            return pre + GroupFormatDigits(input, nfi.NumberGroupSeparator, nfi.NumberGroupSizes, nfi.NumberDecimalSeparator, precision) + post;
        }

        private static string PercentFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            string pre = string.Empty;
            string post = string.Empty;

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
            return pre + GroupFormatDigits(input, nfi.PercentGroupSeparator, nfi.PercentGroupSizes, nfi.PercentDecimalSeparator, precision) + post;
        }

        private static string HexFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            bool upper = true;
            if (precision < 0)
            {
                precision = -precision;
                upper = false;
            }
            string output = ConvertDecimalToHex(input, upper, nfi);
            int typeChar = int.Parse(output.Substring(0, 1), NumberStyles.AllowHexSpecifier);

            if (typeChar >= 8)
            {
                output = FString(precision - output.Length, upper) + output;
            }
            else
            {
                output = ZeroString(precision - output.Length) + output;
            }

            return output;
        }

        private static string ZeroFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            string output = input;

            if (output.Equals("0"))
            {
                output = string.Empty;
            }
            if (output.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }

            if (output.Length > 50 && s_noZeroOut == false)
            {
                output = ZeroString(precision - output.Length) + output.Substring(0, 50) + ZeroString(output.Length - 50);
            }
            else
            {
                output = ZeroString(precision - output.Length) + output;
            }
            if (IsNeg)
            {
                output = nfi.NegativeSign + output;
            }

            return output;
        }

        private static string DecimalPointFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            string output = input;

            if (output.Equals("0"))
            {
                output = string.Empty;
            }
            if (output.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }

            if (output.Length > 50 && s_noZeroOut == false)
            {
                output = ZeroString(precision - output.Length) + output.Substring(0, 50) + ZeroString(output.Length - 50);
            }
            else
            {
                output = ZeroString(precision - output.Length) + output;
            }

            if (precision != 0)
            {
                output = output + nfi.NumberDecimalSeparator + ZeroString(precision);
            }
            if (IsNeg)
            {
                output = nfi.NegativeSign + output;
            }

            return output;
        }

        private static string ThousandsFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            string pre = string.Empty;

            if (input.StartsWith(nfi.NegativeSign))
            {
                input = input.Substring(nfi.NegativeSign.Length);
                pre = nfi.NegativeSign;
            }
            if (input.Equals("0"))
            {
                input = string.Empty;
            }
            input = ZeroString(precision - input.Length) + input;

            return pre + GroupFormatDigits(input, nfi.NumberGroupSeparator, nfi.NumberGroupSizes, string.Empty, 0);
        }

        private static string ScalingFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            string output = input;
            string part = string.Empty;
            bool roundUp = false;

            if (output.Equals("0"))
            {
                output = string.Empty;
            }
            if (output.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }

            if (output.Length > 50 && s_noZeroOut == false)
            {
                output = ZeroString(precision - output.Length) + output.Substring(0, 50) + ZeroString(output.Length - 50);
            }
            else
            {
                output = ZeroString(precision - output.Length) + output;
            }

            if (output.Length >= precision)
            {
                part = string.Empty;
                if (output[output.Length - 3] > '4')
                {
                    roundUp = true;
                }
                part = output.Substring(output.Length - precision, precision - 3);
                output = output.Substring(0, output.Length - precision);
                if (roundUp)
                {
                    if (part == string.Empty)
                    {
                        if (output == string.Empty)
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
                        part = (BigInteger.Parse(part) + 1).ToString("d" + (precision - 3));
                        if (part == "1" + ZeroString(precision - 3))
                        {
                            part = ZeroString(precision - 3);
                            if (output == string.Empty)
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
                output = string.Empty;
            }

            if (part != string.Empty)
            {
                output = output + nfi.NumberDecimalSeparator + part;
            }

            if (IsNeg && (output != string.Empty) && (output != (nfi.NumberDecimalSeparator + ZeroString(output.Length - nfi.NumberDecimalSeparator.Length))))
            {
                output = nfi.NegativeSign + output;
            }

            return output;
        }

        private static string PercentSymbolFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            string output = input;

            if (output.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }
            if (output.Equals("0"))
            {
                output = string.Empty;
            }
            else
            {
                output = output + "00";
            }
            if (output.Length > 50 && s_noZeroOut == false)
            {
                output = ZeroString(precision - output.Length) + output.Substring(0, 50) + ZeroString(output.Length - 50);
            }
            else
            {
                output = ZeroString(precision - output.Length) + output;
            }

            output = output.Substring(0, output.Length - precision) + nfi.PercentSymbol + output.Substring(output.Length - precision, precision);

            if (IsNeg)
            {
                output = nfi.NegativeSign + output;
            }

            return output;
        }

        private static string PerMilleSymbolFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            string output = input;

            if (output.StartsWith(nfi.NegativeSign))
            {
                output = output.Substring(nfi.NegativeSign.Length);
                IsNeg = true;
            }
            if (output.Equals("0"))
            {
                output = string.Empty;
            }
            else
            {
                output = output + "000";
            }
            if (output.Length > 50 && s_noZeroOut == false)
            {
                output = ZeroString(precision - output.Length) + output.Substring(0, 50) + ZeroString(output.Length - 50);
            }
            else
            {
                output = ZeroString(precision - output.Length) + output;
            }

            output = output.Substring(0, output.Length - precision) + nfi.PerMilleSymbol + output.Substring(output.Length - precision, precision);

            if (IsNeg)
            {
                output = nfi.NegativeSign + output;
            }

            return output;
        }

        private static string ScientificFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            char[] temp = input.ToCharArray();
            char[] temp2;
            string output = string.Empty;
            string digits;
            char exp = 'E';

            if (precision < 0)
            {
                precision = -precision;
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
            temp = new char[precision + 2];
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

            int i = precision + 1;
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
            if (precision != 0)
            {
                output = output + nfi.NumberDecimalSeparator + new string(temp, 1, precision);
            }
            output = output + exp + digits;

            return output;
        }

        private static string SignedScientificFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            bool IsNeg = false;
            char[] temp = input.ToCharArray();
            char[] temp2;
            string output = string.Empty;
            string digits;
            char exp = 'E';

            if (precision < 0)
            {
                precision = -precision;
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
            temp = new char[precision + 2];
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

            int i = precision + 1;
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
            if (precision != 0)
            {
                output = output + nfi.NumberDecimalSeparator + new string(temp, 1, precision);
            }
            output = output + exp + nfi.PositiveSign + digits;

            return output;
        }

        private static string EmptyFormatter(string input, int precision, NumberFormatInfo nfi)
        {
            string temp = string.Empty;

            if (input.StartsWith(nfi.NegativeSign))
            {
                temp = nfi.NegativeSign;
            }

            return temp;
        }

        private static StringFormatter ExtraFormatter(StringFormatter formatter, string added)
        {
            return ExtraFormatter(formatter, added, 0);
        }

        private static StringFormatter ExtraFormatter(StringFormatter formatter, string added, int placesAfter)
        {
            StringFormatter sf = delegate (string input, int precision, NumberFormatInfo nfi)
            {
                string temp = formatter(input, precision, nfi);
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
            StringFormatter sf = delegate (string input, int precision, NumberFormatInfo nfi)
            {
                string temp = string.Empty;
                BigInteger bi = BigInteger.Parse(input);

                if (bi > 0)
                {
                    temp = posFormatter(input, precision, nfi);
                }
                if (bi == 0)
                {
                    temp = zeroFormatter(input, precision, nfi);
                }
                if (bi < 0)
                {
                    if (!negInherited)
                    {
                        input = input.Substring(nfi.NegativeSign.Length);
                    }
                    temp = negFormatter(input, precision, nfi);
                }

                return temp;
            };
            return sf;
        }

        private static void VerifyToString(string test, string expectedResult)
        {
            VerifyToString(test, format: null, provider: null, expectError: false, expectedResult: expectedResult);
        }

        private static void VerifyToString(string test, string format, string expectedResult, bool expectError = false)
        {
            VerifyToString(test, format: format, provider: null, expectError, expectedResult: expectedResult);
        }

        private static void VerifyToString(string test, string format, IFormatProvider provider, bool expectError, string expectedResult)
        {
            bool hasFormat = !string.IsNullOrEmpty(format);
            bool hasProvider = provider != null;
            string result = null;

            try
            {
                if (hasFormat)
                {
                    result = hasProvider ? BigInteger.Parse(test, provider).ToString(format, provider) :
                                           BigInteger.Parse(test).ToString(format);
                }
                else
                {
                    result = hasProvider ? BigInteger.Parse(test, provider).ToString(provider) :
                                           BigInteger.Parse(test).ToString();
                }

                Assert.False(expectError, "Expected exception not encountered.");

                VerifyExpectedStringResult(expectedResult, result);
            }
            catch (FormatException)
            {
                Assert.True(expectError);
            }

            VerifyTryFormat(test, format, provider, expectError, expectedResult);
        }

        private static void VerifyExpectedStringResult(string expectedResult, string result)
        {
            if (expectedResult != result)
            {
                Assert.Equal(expectedResult.Length, result.Length);

                int index = expectedResult.LastIndexOf("E", StringComparison.OrdinalIgnoreCase);
                Assert.False(index == 0, "'E' found at beginning of expectedResult");

                bool equal = false;
                if (index > 0)
                {
                    var dig1 = (byte)expectedResult[index - 1];
                    var dig2 = (byte)result[index - 1];

                    equal |= (dig2 == dig1 - 1 || dig2 == dig1 + 1);
                    equal |= (dig1 == '9' && dig2 == '0' || dig2 == '9' && dig1 == '0');
                    equal |= (index == 1 && (dig1 == '9' && dig2 == '1' || dig2 == '9' && dig1 == '1'));
                }

                Assert.True(equal);
            }
        }

        static void VerifyTryFormat(string test, string format, IFormatProvider provider, bool expectError, string expectedResult)
        {
            try
            {
                BigInteger bi = BigInteger.Parse(test, provider);

                char[] destination = expectedResult != null ? new char[expectedResult.Length] : Array.Empty<char>();
                Assert.True(bi.TryFormat(destination, out int charsWritten, format, provider));
                Assert.False(expectError);

                VerifyExpectedStringResult(expectedResult, new string(destination, 0, charsWritten));

                if (expectedResult.Length > 0)
                {
                    Assert.False(bi.TryFormat(new char[expectedResult.Length - 1], out charsWritten, format, provider));
                    Assert.Equal(0, charsWritten);
                }
            }
            catch (FormatException)
            {
                Assert.True(expectError);
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

            for (int i = 0; i < size; i++)
            {
                result += digits[random.Next(0, digits.Length)];
            }

            return result;
        }
        private static string GetRandomInvalidFormatChar(Random random)
        {
            char[] digits = new char[] { 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f', 'G', 'g', 'N', 'n', 'P', 'p', 'X', 'x', 'R', 'r' };
            char result = 'C';
            while (result == 'C')
            {
                result = unchecked((char)random.Next());
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
            return new string(result, 1);
        }

        private static string Fix(string input)
        {
            return Fix(input, false);
        }

        private static string Fix(string input, bool isHex)
        {
            string output = input;

            while (output.StartsWith("0") & (output.Length > 1))
            {
                output = output.Substring(1);
            }
            if (isHex)
            {
                output = ConvertHexToDecimal(output);
            }
            List<char> out2 = new List<char>();
            for (int i = 0; i < output.Length; i++)
            {
                if (char.IsAsciiDigit(output[i]))
                {
                    out2.Add(output[i]);
                }
            }
            output = new string(out2.ToArray());

            return output;
        }

        private static string ConvertHexToDecimal(string input)
        {
            char[] inArr = input.ToCharArray();
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
                    number.Add(CultureInfo.CurrentCulture.NumberFormat.NegativeSign.ToCharArray()[0]);
                }
                number.Reverse();
            }
            string y2 = new string(number.ToArray());
            return y2;
        }

        private static string ConvertDecimalToHex(string input, bool upper, NumberFormatInfo nfi)
        {
            string output = string.Empty;
            BigInteger bi = BigInteger.Parse(input, nfi);
            byte[] bytes = bi.ToByteArray();
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
                    output = $"{output}{chars[start]:X}";
                }
                else
                {
                    output = $"{output}{chars[start]:x}";
                }
            }

            return output;
        }

        private static string ConvertToExp(string input, int places)
        {
            char[] temp = input.Substring(0, places + 2).ToCharArray();
            string ret = null;
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

            ret = temp[0] + CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            ret += ((i == 0) ? "0" : new String(temp).Substring(1, i));
            ret += "E" + CultureInfo.CurrentCulture.NumberFormat.PositiveSign + (input.Length - 1);

            return ret;
        }

        private static string GenerateGroups(int[] sizes, string separator, Random random)
        {
            List<int> total_sizes = new List<int>();
            int total;
            int num_digits = random.Next(10, 100);
            ;
            string digits = string.Empty;

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
                    digits += GetDigitSequence(group_size + 1, group_size + 1, random).Substring(1);
                }
                num_digits -= group_size;
                if (num_digits > 0)
                {
                    digits += separator;
                }
            }

            return digits;
        }

        private static string GroupFormatDigits(string input, string separator, int[] sizes, string point, int places)
        {
            string output = string.Empty;
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
                        output = separator + ZeroString(size) + output;
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
                    output = separator + input.Substring(currentspot - size + 1, size) + output;
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

        private static string ZeroString(int size)
        {
            return size >= 1 ? new string('0', size) : string.Empty;
        }

        private static string FString(int size, bool upper)
        {
            string ret = string.Empty;
            if (size >= 1)
            {
                if (upper)
                {
                    ret = new string('F', size);
                }
                else
                {
                    ret = new string('f', size);
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
    }
}
