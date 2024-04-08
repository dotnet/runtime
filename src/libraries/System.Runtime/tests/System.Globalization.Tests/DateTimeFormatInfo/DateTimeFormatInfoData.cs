// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    internal static class DateTimeFormatInfoData
    {
        public static string EnUSEraName()
        {
            return PlatformDetection.IsNlsGlobalization ? "A.D." : "AD";
        }

        public static string EnUSAbbreviatedEraName()
        {
            return PlatformDetection.IsNlsGlobalization ? "AD" : "A";
        }

        public static string JaJPAbbreviatedEraName()
        {
            // For Windows<Win7 and others, the default calendar is Gregorian Calendar, AD is expected to be the Era Name
            // CLDR has the Japanese abbreviated era name for the Gregorian Calendar in English - "AD",
            // so for non-Windows machines it will be "AD".
            return PlatformDetection.IsNlsGlobalization ? "\u897F\u66A6" : "AD";
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
