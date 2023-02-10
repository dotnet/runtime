// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Reflection;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Globalization.Tests
{
    public class HybridModeTests
    {

        public static IEnumerable<object[]> Cultures_TestData()
        {
            yield return new object[] { "en-US" };
           /* yield return new object[] { "ja-JP" };
            yield return new object[] { "fr-FR" };
            yield return new object[] { "tr-TR" };
            yield return new object[] { "" };*/
        }

        //private static readonly string[] s_cultureNames = new string[] { "en-US", "ja-JP", "fr-FR", "tr-TR", "" };

        //[ConditionalTheory(nameof(PredefinedCulturesOnlyIsDisabled))]
        [Theory]
        [MemberData(nameof(Cultures_TestData))]
        public void TestCultureData(string cultureName)
        {
            CultureInfo ci = new CultureInfo(cultureName);
          /*  bool invariant = (bool) typeof(object).Assembly.GetType("System.Globalization.GlobalizationMode").GetProperty("InvariantGlobalization", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            bool hybrid = (bool) typeof(object).Assembly.GetType("System.Globalization.GlobalizationMode").GetProperty("HybridGlobalization", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            System.Console.WriteLine("Globalization mode is IsInvariantGlobalization: " + invariant);
            System.Console.WriteLine("Globalization mode is HybridGlobalization: " + hybrid);*/
            // Add here test 
           // System.Console.WriteLine("Globalization mode is IsInvariantGlobalization: " + PlatformDetection.IsInvariantGlobalization);
           // System.Console.WriteLine("Globalization mode is IsHybridGlobalization: " + PlatformDetection.IsHybridGlobalization);
            System.Console.WriteLine("Globalization mode log EnglishName: " + ci.EnglishName);
           // CultureInfo myTestCulture = new CultureInfo(name);
            //Assert.Equal(expected, myTestCulture.EnglishName);
            //
            // DateTimeInfo
            //

            /*Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedDayNames, ci.DateTimeFormat.AbbreviatedDayNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthGenitiveNames, ci.DateTimeFormat.AbbreviatedMonthGenitiveNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames, ci.DateTimeFormat.AbbreviatedMonthNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.AMDesignator, ci.DateTimeFormat.AMDesignator);
            Assert.True(ci.DateTimeFormat.Calendar is GregorianCalendar);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.CalendarWeekRule);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.DateSeparator, ci.DateTimeFormat.DateSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.DayNames, ci.DateTimeFormat.DayNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.FirstDayOfWeek, ci.DateTimeFormat.FirstDayOfWeek);

            for (DayOfWeek dow = DayOfWeek.Sunday; dow < DayOfWeek.Saturday; dow++)
                Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedDayName(dow), ci.DateTimeFormat.GetAbbreviatedDayName(dow));
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedEraName(1), ci.DateTimeFormat.GetAbbreviatedEraName(1));

            for (int i = 1; i <= 12; i++)
                Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(i), ci.DateTimeFormat.GetAbbreviatedMonthName(i));

            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetAllDateTimePatterns(), ci.DateTimeFormat.GetAllDateTimePatterns());

            for (DayOfWeek dow = DayOfWeek.Sunday; dow < DayOfWeek.Saturday; dow++)
                Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetDayName(dow), ci.DateTimeFormat.GetDayName(dow));

            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetEra(CultureInfo.InvariantCulture.DateTimeFormat.GetEraName(1)), ci.DateTimeFormat.GetEra(ci.DateTimeFormat.GetEraName(1)));

            for (int i = 1; i <= 12; i++)
                Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(i), ci.DateTimeFormat.GetMonthName(i));
            for (DayOfWeek dow = DayOfWeek.Sunday; dow < DayOfWeek.Saturday; dow++)
                Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.GetShortestDayName(dow), ci.DateTimeFormat.GetShortestDayName(dow));

            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.LongDatePattern, ci.DateTimeFormat.LongDatePattern);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.LongTimePattern, ci.DateTimeFormat.LongTimePattern);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.MonthDayPattern, ci.DateTimeFormat.MonthDayPattern);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.MonthGenitiveNames, ci.DateTimeFormat.MonthGenitiveNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.MonthNames, ci.DateTimeFormat.MonthNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.NativeCalendarName, ci.DateTimeFormat.NativeCalendarName);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.PMDesignator, ci.DateTimeFormat.PMDesignator);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.RFC1123Pattern, ci.DateTimeFormat.RFC1123Pattern);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern, ci.DateTimeFormat.ShortDatePattern);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.ShortestDayNames, ci.DateTimeFormat.ShortestDayNames);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.ShortTimePattern, ci.DateTimeFormat.ShortTimePattern);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.TimeSeparator, ci.DateTimeFormat.TimeSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.DateTimeFormat.YearMonthPattern, ci.DateTimeFormat.YearMonthPattern);

            //
            // Culture data
            //

            Assert.True(ci.Calendar is GregorianCalendar);

            CultureTypes ct = ci.Name == "" ? CultureInfo.InvariantCulture.CultureTypes : CultureInfo.InvariantCulture.CultureTypes | CultureTypes.UserCustomCulture;
            Assert.Equal(ct, ci.CultureTypes);
            Assert.Equal(CultureInfo.InvariantCulture.NativeName, ci.DisplayName);
            Assert.Equal(CultureInfo.InvariantCulture.EnglishName, ci.EnglishName);
            Assert.Equal(CultureInfo.InvariantCulture.GetConsoleFallbackUICulture(), ci.GetConsoleFallbackUICulture());
            Assert.Equal(cultureName, ci.IetfLanguageTag);
            Assert.Equal(CultureInfo.InvariantCulture.IsNeutralCulture, ci.IsNeutralCulture);
            Assert.Equal(CultureInfo.InvariantCulture.KeyboardLayoutId, ci.KeyboardLayoutId);
            Assert.Equal(ci.Name == "" ? 0x7F : 0x1000, ci.LCID);
            Assert.Equal(cultureName, ci.Name);
            Assert.Equal(CultureInfo.InvariantCulture.NativeName, ci.NativeName);
            Assert.Equal(1, ci.OptionalCalendars.Length);
            Assert.True(ci.OptionalCalendars[0] is GregorianCalendar);
            Assert.Equal(CultureInfo.InvariantCulture.Parent, ci.Parent);
            Assert.Equal(CultureInfo.InvariantCulture.ThreeLetterISOLanguageName, ci.ThreeLetterISOLanguageName);
            Assert.Equal(CultureInfo.InvariantCulture.ThreeLetterWindowsLanguageName, ci.ThreeLetterWindowsLanguageName);
            Assert.Equal(CultureInfo.InvariantCulture.TwoLetterISOLanguageName, ci.TwoLetterISOLanguageName);
            Assert.Equal(ci.Name == "" ? false : true, ci.UseUserOverride);

            //
            // Culture Creations
            //
            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentCulture);
            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CurrentUICulture);
            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.InstalledUICulture);
            Assert.Equal(CultureInfo.InvariantCulture, CultureInfo.CreateSpecificCulture("en"));
            Assert.Equal(ci, CultureInfo.GetCultureInfo(cultureName).Clone());
            Assert.Equal(ci, CultureInfo.GetCultureInfoByIetfLanguageTag(cultureName));

            //
            // NumberFormatInfo
            //

            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencyDecimalDigits, ci.NumberFormat.CurrencyDecimalDigits);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencyDecimalSeparator, ci.NumberFormat.CurrencyDecimalSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencyGroupSeparator, ci.NumberFormat.CurrencyGroupSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencyGroupSizes, ci.NumberFormat.CurrencyGroupSizes);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencyNegativePattern, ci.NumberFormat.CurrencyNegativePattern);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencyPositivePattern, ci.NumberFormat.CurrencyPositivePattern);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.CurrencySymbol, ci.NumberFormat.CurrencySymbol);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.DigitSubstitution, ci.NumberFormat.DigitSubstitution);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NaNSymbol, ci.NumberFormat.NaNSymbol);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NativeDigits, ci.NumberFormat.NativeDigits);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NegativeInfinitySymbol, ci.NumberFormat.NegativeInfinitySymbol);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NegativeSign, ci.NumberFormat.NegativeSign);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalDigits, ci.NumberFormat.NumberDecimalDigits);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator, ci.NumberFormat.NumberDecimalSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NumberGroupSeparator, ci.NumberFormat.NumberGroupSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NumberGroupSizes, ci.NumberFormat.NumberGroupSizes);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NumberNegativePattern, ci.NumberFormat.NumberNegativePattern);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentDecimalDigits, ci.NumberFormat.PercentDecimalDigits);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentDecimalSeparator, ci.NumberFormat.PercentDecimalSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentGroupSeparator, ci.NumberFormat.PercentGroupSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentGroupSizes, ci.NumberFormat.PercentGroupSizes);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentNegativePattern, ci.NumberFormat.PercentNegativePattern);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentPositivePattern, ci.NumberFormat.PercentPositivePattern);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PercentSymbol, ci.NumberFormat.PercentSymbol);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PerMilleSymbol, ci.NumberFormat.PerMilleSymbol);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PositiveInfinitySymbol, ci.NumberFormat.PositiveInfinitySymbol);
            Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PositiveSign, ci.NumberFormat.PositiveSign);

            //
            // TextInfo
            //

            Assert.Equal(CultureInfo.InvariantCulture.TextInfo.ANSICodePage, ci.TextInfo.ANSICodePage);
            Assert.Equal(cultureName, ci.TextInfo.CultureName);
            Assert.Equal(CultureInfo.InvariantCulture.TextInfo.EBCDICCodePage, ci.TextInfo.EBCDICCodePage);
            Assert.Equal(CultureInfo.InvariantCulture.TextInfo.IsRightToLeft, ci.TextInfo.IsRightToLeft);
            Assert.Equal(ci.Name == "" ? 0x7F : 0x1000, ci.TextInfo.LCID);
            Assert.Equal(CultureInfo.InvariantCulture.TextInfo.ListSeparator, ci.TextInfo.ListSeparator);
            Assert.Equal(CultureInfo.InvariantCulture.TextInfo.MacCodePage, ci.TextInfo.MacCodePage);
            Assert.Equal(CultureInfo.InvariantCulture.TextInfo.OEMCodePage, ci.TextInfo.OEMCodePage);

            //
            // CompareInfo
            //
            Assert.Equal(ci.Name == "" ? 0x7F : 0x1000, ci.CompareInfo.LCID);
            Assert.True(cultureName.Equals(ci.CompareInfo.Name, StringComparison.OrdinalIgnoreCase));*/
        }
    }
}
