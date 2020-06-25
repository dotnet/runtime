// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    internal static class DateTimeFormatInfoData
    {
        // When running in Windows and CultureInfo is using user overrides, we honor those
        // overrides for the default user UI culture instead of using the ones from ICU.
        private static bool ShouldUseNlsTestData(bool useUserOverrides, string cultureName) =>
            PlatformDetection.IsNlsGlobalization ||
            (useUserOverrides &&
                PlatformDetection.IsWindows &&
                CultureInfo.InstalledUICulture.Name == cultureName);

        public static string EnUSEraName(bool useUserOverrides)
        {
            return ShouldUseNlsTestData(useUserOverrides, "en-US") ? "A.D." : "AD";
        }

        public static string EnUSAbbreviatedEraName(bool useUserOverrides)
        {
            return ShouldUseNlsTestData(useUserOverrides, "en-US") ? "AD" : "A";
        }

        public static string JaJPAbbreviatedEraName(bool useUserOverrides)
        {
            // For Windows<Win7 and others, the default calendar is Gregorian Calendar, AD is expected to be the Era Name
            // CLDR has the Japanese abbreviated era name for the Gregorian Calendar in English - "AD",
            // so for non-Windows machines it will be "AD".
            return ShouldUseNlsTestData(useUserOverrides, "ja-JP") ? "\u897F\u66A6" : "AD";
        }

        public static string[] FrFRDayNames()
        {
            return new string[] { "dimanche", "lundi", "mardi", "mercredi", "jeudi", "vendredi", "samedi" };
        }

        public static string[] FrFRAbbreviatedDayNames()
        {
            return new string[] { "dim.", "lun.", "mar.", "mer.", "jeu.", "ven.", "sam." };
        }


        public static CalendarWeekRule BrFRCalendarWeekRule()
        {
            if (PlatformDetection.IsWindows7)
            {
                return CalendarWeekRule.FirstDay;
            }

            if (PlatformDetection.IsWindows && PlatformDetection.WindowsVersion < 10)
            {
                return CalendarWeekRule.FirstFullWeek;
            }

            return CalendarWeekRule.FirstFourDayWeek;
        }

        public static Exception GetCultureNotSupportedException(CultureInfo cultureInfo)
        {
            return new NotSupportedException(string.Format("The culture '{0}' with calendar '{1}' is not supported.",
                cultureInfo.Name,
                cultureInfo.Calendar.GetType().Name));
        }
    }
}
