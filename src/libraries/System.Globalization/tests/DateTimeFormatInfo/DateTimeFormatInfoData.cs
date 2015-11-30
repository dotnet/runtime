// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    internal static class DateTimeFormatInfoData
    {
        private static readonly bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly int s_WindowsVersion = GetWindowsVersion();
        private static readonly bool s_isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static string GetEraName(CultureInfo cultureInfo)
        {
            if (string.Equals(cultureInfo.Name, "en-US", StringComparison.OrdinalIgnoreCase))
            {
                return s_isWindows ? "A.D." : "AD";
            }
            if (string.Equals(cultureInfo.Name, "fr-FR", StringComparison.OrdinalIgnoreCase))
            {
                return "ap. J.-C.";
            }

            throw GetCultureNotSupportedException(cultureInfo);
        }

        public static string GetAbbreviatedEraName(CultureInfo cultureInfo)
        {
            if (string.Equals(cultureInfo.Name, "en-US", StringComparison.OrdinalIgnoreCase))
            {
                return s_isWindows ? "AD" : "A";
            }
            if (string.Equals(cultureInfo.Name, "ja-JP", StringComparison.OrdinalIgnoreCase) &&
                cultureInfo.Calendar.GetType() == typeof(GregorianCalendar))
            {
                //For Windows<Win7 and others, the default calendar is Gregorian Calendar, AD is expected to be the Era Name
                // CLDR has the Japanese abbreviated era name for the Gregorian Calendar in English - "AD",
                // so for non-Windows machines it will be "AD".
                return s_isWindows ? "\u897F\u66A6" : "AD";
            }

            throw GetCultureNotSupportedException(cultureInfo);
        }

        internal static string[] GetDayNames(CultureInfo cultureInfo)
        {
            if (string.Equals(cultureInfo.Name, "en-US", StringComparison.OrdinalIgnoreCase))
            {
                return new string[]
                {
                    "Sunday",
                    "Monday",
                    "Tuesday",
                    "Wednesday",
                    "Thursday",
                    "Friday",
                    "Saturday"
                };
            }
            if (string.Equals(cultureInfo.Name, "fr-FR", StringComparison.OrdinalIgnoreCase))
            {
                string[] dayNames = new string[]
                {
                    "dimanche",
                    "lundi",
                    "mardi",
                    "mercredi",
                    "jeudi",
                    "vendredi",
                    "samedi"
                };

                if (s_isOSX)
                {
                    CapitalizeStrings(dayNames);
                }
                return dayNames;
            }

            throw GetCultureNotSupportedException(cultureInfo);
        }

        internal static string[] GetAbbreviatedDayNames(CultureInfo cultureInfo)
        {
            if (string.Equals(cultureInfo.Name, "en-US", StringComparison.OrdinalIgnoreCase))
            {
                return new string[]
                {
                    "Sun",
                    "Mon",
                    "Tue",
                    "Wed",
                    "Thu",
                    "Fri",
                    "Sat"
                };
            }
            if (string.Equals(cultureInfo.Name, "fr-FR", StringComparison.OrdinalIgnoreCase))
            {
                string[] dayNames = new string[]
                {
                    "dim.",
                    "lun.",
                    "mar.",
                    "mer.",
                    "jeu.",
                    "ven.",
                    "sam."
                };

                if (s_isOSX)
                {
                    CapitalizeStrings(dayNames);
                }
                return dayNames;
            }

            throw GetCultureNotSupportedException(cultureInfo);
        }

        private static void CapitalizeStrings(string[] strings)
        {
            for (int i = 0; i < strings.Length; i++)
            {
                strings[i] = strings[i].Substring(0, 1).ToUpper() + strings[i].Substring(1);
            }
        }

        internal static CalendarWeekRule GetCalendarWeekRule(CultureInfo cultureInfo)
        {
            if (string.Equals(cultureInfo.Name, "en-US", StringComparison.OrdinalIgnoreCase))
            {
                return CalendarWeekRule.FirstDay;
            }
            if (string.Equals(cultureInfo.Name, "br-FR", StringComparison.OrdinalIgnoreCase))
            {
                if (s_isWindows && s_WindowsVersion < 10)
                {
                    return CalendarWeekRule.FirstFullWeek;
                }
                else
                {
                    return CalendarWeekRule.FirstFourDayWeek;
                }
            }

            throw GetCultureNotSupportedException(cultureInfo);
        }

        public static Exception GetCultureNotSupportedException(CultureInfo cultureInfo)
        {
            return new NotSupportedException(string.Format("The culture '{0}' with calendar '{1}' is not supported.",
                cultureInfo.Name,
                cultureInfo.Calendar.GetType().Name));
        }

        public static int GetWindowsVersion()
        {
            if (s_isWindows)
            {
                RTL_OSVERSIONINFOEX osvi = new RTL_OSVERSIONINFOEX();
                osvi.dwOSVersionInfoSize = (uint)Marshal.SizeOf(osvi);
                Assert.Equal(0, RtlGetVersion(out osvi));
                return (int)osvi.dwMajorVersion;
            }

            return -1;
        }

        [DllImport("ntdll.dll")]
        private static extern int RtlGetVersion(out RTL_OSVERSIONINFOEX lpVersionInformation);

        [StructLayout(LayoutKind.Sequential)]
        internal struct RTL_OSVERSIONINFOEX
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string szCSDVersion;
        }
    }
}
