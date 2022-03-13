// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using System.Text;
using System.Diagnostics;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoAll
    {
        [PlatformSpecific(TestPlatforms.Windows)] // P/Invoke to Win32 function
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNlsGlobalization))]
        public void TestAllCultures_Nls()
        {
            Assert.True(EnumSystemLocalesEx(EnumLocales, LOCALE_WINDOWS, IntPtr.Zero, IntPtr.Zero), "EnumSystemLocalesEx has failed");

            Assert.All(cultures, Validate);
        }

        private void Validate(CultureInfo ci)
        {
            Assert.Equal(ci.EnglishName, GetLocaleInfo(ci, LOCALE_SENGLISHDISPLAYNAME));

            // si-LK has some special case when running on Win7 so we just ignore this one
            if (!ci.Name.Equals("si-LK", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal(ci.Name.Length == 0 ? "Invariant Language (Invariant Country)" : GetLocaleInfo(ci, LOCALE_SNATIVEDISPLAYNAME), ci.NativeName);
            }

            // zh-Hans and zh-Hant has different behavior on different platform
            Assert.Contains(GetLocaleInfo(ci, LOCALE_SPARENT), new[] { "zh-Hans", "zh-Hant", ci.Parent.Name }, StringComparer.OrdinalIgnoreCase);

            Assert.Equal(ci.TwoLetterISOLanguageName, GetLocaleInfo(ci, LOCALE_SISO639LANGNAME));

            ValidateDTFI(ci);
            ValidateNFI(ci);
            ValidateRegionInfo(ci);
        }

        private void ValidateDTFI(CultureInfo ci)
        {
            DateTimeFormatInfo dtfi = ci.DateTimeFormat;
            Calendar cal = dtfi.Calendar;
            int calId = GetCalendarId(cal);

            Assert.Equal(GetDayNames(ci, calId, CAL_SABBREVDAYNAME1), dtfi.AbbreviatedDayNames);
            Assert.Equal(GetDayNames(ci, calId, CAL_SDAYNAME1), dtfi.DayNames);
            Assert.Equal(GetMonthNames(ci, calId, CAL_SMONTHNAME1), dtfi.MonthNames);
            Assert.Equal(GetMonthNames(ci, calId, CAL_SABBREVMONTHNAME1), dtfi.AbbreviatedMonthNames);
            Assert.Equal(GetMonthNames(ci, calId, CAL_SMONTHNAME1 | LOCALE_RETURN_GENITIVE_NAMES), dtfi.MonthGenitiveNames);
            Assert.Equal(GetMonthNames(ci, calId, CAL_SABBREVMONTHNAME1 | LOCALE_RETURN_GENITIVE_NAMES), dtfi.AbbreviatedMonthGenitiveNames);
            Assert.Equal(GetDayNames(ci, calId, CAL_SSHORTESTDAYNAME1), dtfi.ShortestDayNames);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_S1159), dtfi.AMDesignator, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_S2359), dtfi.PMDesignator, StringComparer.OrdinalIgnoreCase);

            Assert.Equal(calId, GetDefaultcalendar(ci));

            Assert.Equal((int)dtfi.FirstDayOfWeek, ConvertFirstDayOfWeekMonToSun(GetLocaleInfoAsInt(ci, LOCALE_IFIRSTDAYOFWEEK)));
            Assert.Equal((int)dtfi.CalendarWeekRule, GetLocaleInfoAsInt(ci, LOCALE_IFIRSTWEEKOFYEAR));
            Assert.Equal(dtfi.MonthDayPattern, GetCalendarInfo(ci, calId, CAL_SMONTHDAY, true));
            Assert.Equal("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'", dtfi.RFC1123Pattern);
            Assert.Equal("yyyy'-'MM'-'dd'T'HH':'mm':'ss", dtfi.SortableDateTimePattern);
            Assert.Equal("yyyy'-'MM'-'dd HH':'mm':'ss'Z'", dtfi.UniversalSortableDateTimePattern);

            string longDatePattern1 = GetCalendarInfo(ci, calId, CAL_SLONGDATE)[0];
            string longDatePattern2 = ReescapeWin32String(GetLocaleInfo(ci, LOCALE_SLONGDATE));
            string longTimePattern1 = GetTimeFormats(ci, 0)[0];
            string longTimePattern2 = ReescapeWin32String(GetLocaleInfo(ci, LOCALE_STIMEFORMAT));
            string fullDateTimePattern = longDatePattern1 + " " + longTimePattern1;
            string fullDateTimePattern1 = longDatePattern2 + " " + longTimePattern2;
            Assert.Contains(dtfi.FullDateTimePattern, new[] { fullDateTimePattern, fullDateTimePattern1 });
            Assert.Contains(dtfi.LongDatePattern, new[] { longDatePattern1, longDatePattern2 });
            Assert.Contains(dtfi.LongTimePattern, new[] { longTimePattern1, longTimePattern2 });

            Assert.Contains(dtfi.ShortTimePattern, new[] { GetTimeFormats(ci, TIME_NOSECONDS)[0], ReescapeWin32String(GetLocaleInfo(ci, LOCALE_SSHORTTIME)) });
            Assert.Contains(dtfi.ShortDatePattern, new[] { GetCalendarInfo(ci, calId, CAL_SSHORTDATE)[0], ReescapeWin32String(GetLocaleInfo(ci, LOCALE_SSHORTDATE)) });

            Assert.Contains(dtfi.YearMonthPattern, new[] { GetCalendarInfo(ci, calId, CAL_SYEARMONTH)[0], ReescapeWin32String(GetLocaleInfo(ci, LOCALE_SYEARMONTH)) });

            int eraNameIndex = 1;
            Assert.All(GetCalendarInfo(ci, calId, CAL_SERASTRING), eraName => Assert.Equal(dtfi.GetEraName(eraNameIndex++), eraName, StringComparer.OrdinalIgnoreCase));
            eraNameIndex = 1;
            Assert.All(GetCalendarInfo(ci, calId, CAL_SABBREVERASTRING), eraName => Assert.Equal(dtfi.GetAbbreviatedEraName(eraNameIndex++), eraName, StringComparer.OrdinalIgnoreCase));
        }

        private void ValidateNFI(CultureInfo ci)
        {
            NumberFormatInfo nfi = ci.NumberFormat;

            Assert.Equal(string.IsNullOrEmpty(GetLocaleInfo(ci, LOCALE_SPOSITIVESIGN)) ? "+" : GetLocaleInfo(ci, LOCALE_SPOSITIVESIGN), nfi.PositiveSign);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_SNEGATIVESIGN), nfi.NegativeSign);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_SDECIMAL), nfi.NumberDecimalSeparator);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_SDECIMAL), nfi.PercentDecimalSeparator);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_STHOUSAND), nfi.NumberGroupSeparator);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_STHOUSAND), nfi.PercentGroupSeparator);

            Assert.Equal(GetLocaleInfo(ci, LOCALE_SMONTHOUSANDSEP), nfi.CurrencyGroupSeparator);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_SMONDECIMALSEP), nfi.CurrencyDecimalSeparator);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_SCURRENCY), nfi.CurrencySymbol);

            Assert.Equal(GetLocaleInfoAsInt(ci, LOCALE_IDIGITS), nfi.NumberDecimalDigits);
            Assert.Equal(GetLocaleInfoAsInt(ci, LOCALE_IDIGITS), nfi.PercentDecimalDigits);

            Assert.Equal(GetLocaleInfoAsInt(ci, LOCALE_ICURRDIGITS), nfi.CurrencyDecimalDigits);
            Assert.Equal(GetLocaleInfoAsInt(ci, LOCALE_ICURRENCY), nfi.CurrencyPositivePattern);
            Assert.Equal(GetLocaleInfoAsInt(ci, LOCALE_INEGCURR), nfi.CurrencyNegativePattern);
            Assert.Equal(GetLocaleInfoAsInt(ci, LOCALE_INEGNUMBER), nfi.NumberNegativePattern);

            Assert.Equal(ConvertWin32GroupString(GetLocaleInfo(ci, LOCALE_SMONGROUPING)), nfi.CurrencyGroupSizes);

            Assert.Equal(GetLocaleInfo(ci, LOCALE_SNAN), nfi.NaNSymbol);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_SNEGINFINITY), nfi.NegativeInfinitySymbol);

            Assert.Equal(ConvertWin32GroupString(GetLocaleInfo(ci, LOCALE_SGROUPING)), nfi.NumberGroupSizes);
            Assert.Equal(ConvertWin32GroupString(GetLocaleInfo(ci, LOCALE_SGROUPING)), nfi.PercentGroupSizes);

            Assert.Equal(GetLocaleInfoAsInt(ci, LOCALE_INEGATIVEPERCENT), nfi.PercentNegativePattern);
            Assert.Equal(GetLocaleInfoAsInt(ci, LOCALE_IPOSITIVEPERCENT), nfi.PercentPositivePattern);

            Assert.Equal(GetLocaleInfo(ci, LOCALE_SPERCENT), nfi.PercentSymbol);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_SPERMILLE), nfi.PerMilleSymbol);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_SPOSINFINITY), nfi.PositiveInfinitySymbol);
        }

        private void ValidateRegionInfo(CultureInfo ci)
        {
            if (ci.Name.Length == 0) // no region for invariant
                return;

            RegionInfo ri = new RegionInfo(ci.Name);

            Assert.Equal(GetLocaleInfo(ci, LOCALE_SCURRENCY), ri.CurrencySymbol);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_SENGLISHCOUNTRYNAME), ri.EnglishName);
            Assert.Equal(GetLocaleInfoAsInt(ci, LOCALE_IMEASURE) == 0, ri.IsMetric);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_SINTLSYMBOL), ri.ISOCurrencySymbol);
            Assert.True(ci.Name.Equals(ri.Name, StringComparison.OrdinalIgnoreCase) || // Desktop usese culture name as region name
                        ri.Name.Equals(GetLocaleInfo(ci, LOCALE_SISO3166CTRYNAME), StringComparison.OrdinalIgnoreCase)); // netcore uses 2 letter ISO for region name
            Assert.Equal(GetLocaleInfo(ci, LOCALE_SISO3166CTRYNAME), ri.TwoLetterISORegionName, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(GetLocaleInfo(ci, LOCALE_SNATIVECOUNTRYNAME), ri.NativeName, StringComparer.OrdinalIgnoreCase);
        }

        private int[] ConvertWin32GroupString(string win32Str)
        {
            // None of these cases make any sense
            if (win32Str == null || win32Str.Length == 0)
            {
                return (new int[] { 3 });
            }

            if (win32Str[0] == '0')
            {
                return (new int[] { 0 });
            }

            // Since its in n;n;n;n;n format, we can always get the length quickly
            int[] values;
            if (win32Str[win32Str.Length - 1] == '0')
            {
                // Trailing 0 gets dropped. 1;0 -> 1
                values = new int[(win32Str.Length / 2)];
            }
            else
            {
                // Need extra space for trailing zero 1 -> 1;0
                values = new int[(win32Str.Length / 2) + 2];
                values[values.Length - 1] = 0;
            }

            int i;
            int j;
            for (i = 0, j = 0; i < win32Str.Length && j < values.Length; i += 2, j++)
            {
                // Note that this # shouldn't ever be zero, 'cause 0 is only at end
                // But we'll test because its registry that could be anything
                if (win32Str[i] < '1' || win32Str[i] > '9')
                    return new int[] { 3 };

                values[j] = (int)(win32Str[i] - '0');
            }

            return (values);
        }

        private List<string> _timePatterns;
        private bool EnumTimeFormats(string lpTimeFormatString, IntPtr lParam)
        {
            _timePatterns.Add(ReescapeWin32String(lpTimeFormatString));
            return true;
        }

        private string[] GetTimeFormats(CultureInfo ci, uint flags)
        {
            _timePatterns = new List<string>();
            Assert.True(EnumTimeFormatsEx(EnumTimeFormats, ci.Name, flags, IntPtr.Zero), string.Format("EnumTimeFormatsEx failed with culture {0} and flags {1}", ci, flags));

            return _timePatterns.ToArray();
        }

        internal string ReescapeWin32String(string str)
        {
            // If we don't have data, then don't try anything
            if (str == null)
                return null;

            StringBuilder result = null;

            bool inQuote = false;
            for (int i = 0; i < str.Length; i++)
            {
                // Look for quote
                if (str[i] == '\'')
                {
                    // Already in quote?
                    if (inQuote)
                    {
                        // See another single quote.  Is this '' of 'fred''s' or '''', or is it an ending quote?
                        if (i + 1 < str.Length && str[i + 1] == '\'')
                        {
                            // Found another ', so we have ''.  Need to add \' instead.
                            // 1st make sure we have our stringbuilder
                            if (result == null)
                                result = new StringBuilder(str, 0, i, str.Length * 2);

                            // Append a \' and keep going (so we don't turn off quote mode)
                            result.Append("\\'");
                            i++;
                            continue;
                        }

                        // Turning off quote mode, fall through to add it
                        inQuote = false;
                    }
                    else
                    {
                        // Found beginning quote, fall through to add it
                        inQuote = true;
                    }
                }
                // Is there a single \ character?
                else if (str[i] == '\\')
                {
                    // Found a \, need to change it to \\
                    // 1st make sure we have our stringbuilder
                    if (result == null)
                        result = new StringBuilder(str, 0, i, str.Length * 2);

                    // Append our \\ to the string & continue
                    result.Append("\\\\");
                    continue;
                }

                // If we have a builder we need to add our character
                if (result != null)
                    result.Append(str[i]);
            }

            // Unchanged string? , just return input string
            if (result == null)
                return str;

            // String changed, need to use the builder
            return result.ToString();
        }


        private string[] GetMonthNames(CultureInfo ci, int calendar, uint calType)
        {
            string[] names = new string[13];
            for (uint i = 0; i < 13; i++)
            {
                names[i] = GetCalendarInfo(ci, calendar, calType + i, false);
            }

            return names;
        }

        private int ConvertFirstDayOfWeekMonToSun(int iTemp)
        {
            // Convert Mon-Sun to Sun-Sat format
            iTemp++;
            if (iTemp > 6)
            {
                // Wrap Sunday and convert invalid data to Sunday
                iTemp = 0;
            }
            return iTemp;
        }

        private string[] GetDayNames(CultureInfo ci, int calendar, uint calType)
        {
            string[] names = new string[7];
            for (uint i = 1; i < 7; i++)
            {
                names[i] = GetCalendarInfo(ci, calendar, calType + i - 1, true);
            }
            names[0] = GetCalendarInfo(ci, calendar, calType + 6, true);

            return names;
        }

        private int GetCalendarId(Calendar cal)
        {
            int calId = 0;

            if (cal is System.Globalization.GregorianCalendar)
            {
                calId = (int)(cal as GregorianCalendar).CalendarType;
            }
            else if (cal is System.Globalization.JapaneseCalendar)
            {
                calId = CAL_JAPAN;
            }
            else if (cal is System.Globalization.TaiwanCalendar)
            {
                calId = CAL_TAIWAN;
            }
            else if (cal is System.Globalization.KoreanCalendar)
            {
                calId = CAL_KOREA;
            }
            else if (cal is System.Globalization.HijriCalendar)
            {
                calId = CAL_HIJRI;
            }
            else if (cal is System.Globalization.ThaiBuddhistCalendar)
            {
                calId = CAL_THAI;
            }
            else if (cal is System.Globalization.HebrewCalendar)
            {
                calId = CAL_HEBREW;
            }
            else if (cal is System.Globalization.UmAlQuraCalendar)
            {
                calId = CAL_UMALQURA;
            }
            else if (cal is System.Globalization.PersianCalendar)
            {
                calId = CAL_PERSIAN;
            }
            else
            {
                throw new KeyNotFoundException(string.Format("Got a calendar {0} which we cannot map its Id", cal));
            }

            return calId;
        }

        internal bool EnumLocales(string name, uint dwFlags, IntPtr param)
        {
            CultureInfo ci = new CultureInfo(name);
            if (!ci.IsNeutralCulture)
                cultures.Add(ci);
            return true;
        }

        private string GetLocaleInfo(CultureInfo ci, uint lctype)
        {
            Assert.True(GetLocaleInfoEx(ci.Name, lctype, sb, 400) > 0, string.Format("GetLocaleInfoEx failed when calling with lctype {0} and culture {1}", lctype, ci));
            return sb.ToString();
        }

        private string GetCalendarInfo(CultureInfo ci, int calendar, uint calType, bool throwInFail)
        {
            if (GetCalendarInfoEx(ci.Name, calendar, IntPtr.Zero, calType, sb, 400, IntPtr.Zero) <= 0)
            {
                Assert.False(throwInFail, string.Format("GetCalendarInfoEx failed when calling with caltype {0} and culture {1} and calendar Id {2}", calType, ci, calendar));
                return "";
            }
            return ReescapeWin32String(sb.ToString());
        }


        private List<int> _optionalCals = new List<int>();
        private bool EnumCalendarsCallback(string lpCalendarInfoString, int calendar, string pReserved, IntPtr lParam)
        {
            _optionalCals.Add(calendar);
            return true;
        }

        private int[] GetOptionalCalendars(CultureInfo ci)
        {
            _optionalCals = new List<int>();
            Assert.True(EnumCalendarInfoExEx(EnumCalendarsCallback, ci.Name, ENUM_ALL_CALENDARS, null, CAL_ICALINTVALUE, IntPtr.Zero), "EnumCalendarInfoExEx has been failed.");

            return _optionalCals.ToArray();
        }

        private List<string> _calPatterns;
        private bool EnumCalendarInfoCallback(string lpCalendarInfoString, int calendar, string pReserved, IntPtr lParam)
        {
            _calPatterns.Add(ReescapeWin32String(lpCalendarInfoString));
            return true;
        }

        private string[] GetCalendarInfo(CultureInfo ci, int calId, uint calType)
        {
            _calPatterns = new List<string>();

            Assert.True(EnumCalendarInfoExEx(EnumCalendarInfoCallback, ci.Name, (uint)calId, null, calType, IntPtr.Zero), "EnumCalendarInfoExEx has been failed in GetCalendarInfo.");

            return _calPatterns.ToArray();
        }

        private int GetDefaultcalendar(CultureInfo ci)
        {
            int calId = GetLocaleInfoAsInt(ci, LOCALE_ICALENDARTYPE);
            if (calId != 0)
                return calId;

            int[] cals = GetOptionalCalendars(ci);
            Assert.True(cals.Length > 0);
            return cals[0];
        }

        private int GetLocaleInfoAsInt(CultureInfo ci, uint lcType)
        {
            int data = 0;
            Assert.True(GetLocaleInfoEx(ci.Name, lcType | LOCALE_RETURN_NUMBER, ref data, sizeof(int)) > 0, string.Format("GetLocaleInfoEx failed with culture {0} and lcType {1}.", ci, lcType));

            return data;
        }

        internal delegate bool EnumLocalesProcEx([MarshalAs(UnmanagedType.LPWStr)] string name, uint dwFlags, IntPtr param);
        internal delegate bool EnumCalendarInfoProcExEx([MarshalAs(UnmanagedType.LPWStr)] string lpCalendarInfoString, int Calendar, string lpReserved, IntPtr lParam);
        internal delegate bool EnumTimeFormatsProcEx([MarshalAs(UnmanagedType.LPWStr)] string lpTimeFormatString, IntPtr lParam);

        internal static StringBuilder sb = new StringBuilder(400);
        internal static List<CultureInfo> cultures = new List<CultureInfo>();

        internal const uint LOCALE_WINDOWS = 0x00000001;
        internal const uint LOCALE_SENGLISHDISPLAYNAME = 0x00000072;
        internal const uint LOCALE_SNATIVEDISPLAYNAME = 0x00000073;
        internal const uint LOCALE_SPARENT = 0x0000006d;
        internal const uint LOCALE_SISO639LANGNAME = 0x00000059;
        internal const uint LOCALE_S1159 = 0x00000028;   // AM designator, eg "AM"
        internal const uint LOCALE_S2359 = 0x00000029; // PM designator, eg "PM"
        internal const uint LOCALE_ICALENDARTYPE = 0x00001009;
        internal const uint LOCALE_RETURN_NUMBER = 0x20000000;
        internal const uint LOCALE_IFIRSTWEEKOFYEAR = 0x0000100D;
        internal const uint LOCALE_IFIRSTDAYOFWEEK = 0x0000100C;
        internal const uint LOCALE_SLONGDATE = 0x00000020;
        internal const uint LOCALE_STIMEFORMAT = 0x00001003;
        internal const uint LOCALE_RETURN_GENITIVE_NAMES = 0x10000000;
        internal const uint LOCALE_SSHORTDATE = 0x0000001F;
        internal const uint LOCALE_SSHORTTIME = 0x00000079;
        internal const uint LOCALE_SYEARMONTH = 0x00001006;
        internal const uint LOCALE_SPOSITIVESIGN = 0x00000050;   // positive sign
        internal const uint LOCALE_SNEGATIVESIGN = 0x00000051;   // negative sign
        internal const uint LOCALE_SDECIMAL = 0x0000000E;
        internal const uint LOCALE_STHOUSAND = 0x0000000F;
        internal const uint LOCALE_SMONTHOUSANDSEP = 0x00000017;
        internal const uint LOCALE_SMONDECIMALSEP = 0x00000016;
        internal const uint LOCALE_SCURRENCY = 0x00000014;
        internal const uint LOCALE_IDIGITS = 0x00000011;
        internal const uint LOCALE_ICURRDIGITS = 0x00000019;
        internal const uint LOCALE_ICURRENCY = 0x0000001B;
        internal const uint LOCALE_INEGCURR = 0x0000001C;
        internal const uint LOCALE_INEGNUMBER = 0x00001010;
        internal const uint LOCALE_SMONGROUPING = 0x00000018;
        internal const uint LOCALE_SNAN = 0x00000069;
        internal const uint LOCALE_SNEGINFINITY = 0x0000006b;   // - Infinity
        internal const uint LOCALE_SGROUPING = 0x00000010;
        internal const uint LOCALE_INEGATIVEPERCENT = 0x00000074;
        internal const uint LOCALE_IPOSITIVEPERCENT = 0x00000075;
        internal const uint LOCALE_SPERCENT = 0x00000076;
        internal const uint LOCALE_SPERMILLE = 0x00000077;
        internal const uint LOCALE_SPOSINFINITY = 0x0000006a;
        internal const uint LOCALE_SENGLISHCOUNTRYNAME = 0x00001002;
        internal const uint LOCALE_IMEASURE = 0x0000000D;
        internal const uint LOCALE_SINTLSYMBOL = 0x00000015;
        internal const uint LOCALE_SISO3166CTRYNAME = 0x0000005A;
        internal const uint LOCALE_SNATIVECOUNTRYNAME = 0x00000008;

        internal const uint CAL_SABBREVDAYNAME1 = 0x0000000e;
        internal const uint CAL_SMONTHNAME1 = 0x00000015;
        internal const uint CAL_SABBREVMONTHNAME1 = 0x00000022;
        internal const uint CAL_ICALINTVALUE = 0x00000001;
        internal const uint CAL_SDAYNAME1 = 0x00000007;
        internal const uint CAL_SLONGDATE = 0x00000006;
        internal const uint CAL_SMONTHDAY = 0x00000038;
        internal const uint CAL_SSHORTDATE = 0x00000005;
        internal const uint CAL_SSHORTESTDAYNAME1 = 0x00000031;
        internal const uint CAL_SYEARMONTH = 0x0000002f;
        internal const uint CAL_SERASTRING = 0x00000004;
        internal const uint CAL_SABBREVERASTRING = 0x00000039;
        internal const uint ENUM_ALL_CALENDARS = 0xffffffff;

        internal const uint TIME_NOSECONDS = 0x00000002;

        internal const int CAL_JAPAN = 3;     // Japanese Emperor Era calendar
        internal const int CAL_TAIWAN = 4;     // Taiwan Era calendar
        internal const int CAL_KOREA = 5;     // Korean Tangun Era calendar
        internal const int CAL_HIJRI = 6;     // Hijri (Arabic Lunar) calendar
        internal const int CAL_THAI = 7;     // Thai calendar
        internal const int CAL_HEBREW = 8;     // Hebrew (Lunar) calendar
        internal const int CAL_PERSIAN = 22;
        internal const int CAL_UMALQURA = 23;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetLocaleInfoEx(string lpLocaleName, uint LCType, StringBuilder data, int cchData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetLocaleInfoEx(string lpLocaleName, uint LCType, ref int data, int cchData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool EnumSystemLocalesEx(EnumLocalesProcEx lpLocaleEnumProcEx, uint dwFlags, IntPtr lParam, IntPtr reserved);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetCalendarInfoEx(string lpLocaleName, int Calendar, IntPtr lpReserved, uint CalType, StringBuilder lpCalData, int cchData, IntPtr lpValue);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetCalendarInfoEx(string lpLocaleName, int Calendar, IntPtr lpReserved, uint CalType, StringBuilder lpCalData, int cchData, ref uint lpValue);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool EnumCalendarInfoExEx(EnumCalendarInfoProcExEx pCalInfoEnumProcExEx, string lpLocaleName, uint Calendar, string lpReserved, uint CalType, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool EnumTimeFormatsEx(EnumTimeFormatsProcEx lpTimeFmtEnumProcEx, string lpLocaleName, uint dwFlags, IntPtr lParam);

        public static IEnumerable<object[]> CultureInfo_TestData()
        {
            yield return new object[] { 0x0001, new [] { "ar" }, "ar-SA", "ara", "ARA", "ar", "en-US" };
            yield return new object[] { 0x0002, new [] { "bg" }, "bg-BG", "bul", "BGR", "bg", "bg-BG" };
            yield return new object[] { 0x0003, new [] { "ca" }, "ca-ES", "cat", "CAT", "ca", "ca-ES" };
            yield return new object[] { 0x0004, new [] { "zh-chs", "zh-hans" }, "zh-CN", "zho", "CHS", "zh-Hans", "zh-CN", true };
            yield return new object[] { 0x0005, new [] { "cs" }, "cs-CZ", "ces", "CSY", "cs", "cs-CZ" };
            yield return new object[] { 0x0006, new [] { "da" }, "da-DK", "dan", "DAN", "da", "da-DK" };
            yield return new object[] { 0x0007, new [] { "de" }, "de-DE", "deu", "DEU", "de", "de-DE" };
            yield return new object[] { 0x0008, new [] { "el" }, "el-GR", "ell", "ELL", "el", "el-GR" };
            yield return new object[] { 0x0009, new [] { "en" }, "en-US", "eng", "ENU", "en", "en-US" };
            yield return new object[] { 0x000a, new [] { "es" }, "es-ES", "spa", "ESP", "es", "es-ES" };
            yield return new object[] { 0x000b, new [] { "fi" }, "fi-FI", "fin", "FIN", "fi", "fi-FI" };
            yield return new object[] { 0x000c, new [] { "fr" }, "fr-FR", "fra", "FRA", "fr", "fr-FR" };
            yield return new object[] { 0x000d, new [] { "he" }, "he-IL", "heb", "HEB", "he", "en-US" };
            yield return new object[] { 0x000e, new [] { "hu" }, "hu-HU", "hun", "HUN", "hu", "hu-HU" };
            yield return new object[] { 0x0010, new [] { "it" }, "it-IT", "ita", "ITA", "it", "it-IT" };
            yield return new object[] { 0x0011, new [] { "ja" }, "ja-JP", "jpn", "JPN", "ja", "ja-JP" };
            yield return new object[] { 0x0012, new [] { "ko" }, "ko-KR", "kor", "KOR", "ko", "ko-KR" };
            yield return new object[] { 0x0013, new [] { "nl" }, "nl-NL", "nld", "NLD", "nl", "nl-NL" };
            yield return new object[] { 0x0015, new [] { "pl" }, "pl-PL", "pol", "PLK", "pl", "pl-PL" };
            yield return new object[] { 0x0016, new [] { "pt" }, "pt-BR", "por", "PTB", "pt", "pt-BR" };
            yield return new object[] { 0x0018, new [] { "ro" }, "ro-RO", "ron", "ROM", "ro", "ro-RO" };
            yield return new object[] { 0x0019, new [] { "ru" }, "ru-RU", "rus", "RUS", "ru", "ru-RU" };
            yield return new object[] { 0x001a, new [] { "hr" }, "hr-HR", "hrv", "HRV", "hr", "hr-HR" };
            yield return new object[] { 0x001b, new [] { "sk" }, "sk-SK", "slk", "SKY", "sk", "sk-SK" };
            yield return new object[] { 0x001d, new [] { "sv" }, "sv-SE", "swe", "SVE", "sv", "sv-SE" };
            yield return new object[] { 0x001e, new [] { "th" }, "th-TH", "tha", "THA", "th", "en-US" };
            yield return new object[] { 0x001f, new [] { "tr" }, "tr-TR", "tur", "TRK", "tr", "tr-TR" };
            yield return new object[] { 0x0021, new [] { "id" }, "id-ID", "ind", "IND", "id", "id-ID" };
            yield return new object[] { 0x0022, new [] { "uk" }, "uk-UA", "ukr", "UKR", "uk", "uk-UA" };
            yield return new object[] { 0x0024, new [] { "sl" }, "sl-SI", "slv", "SLV", "sl", "sl-SI" };
            yield return new object[] { 0x0025, new [] { "et" }, "et-EE", "est", "ETI", "et", "et-EE" };
            yield return new object[] { 0x0026, new [] { "lv" }, "lv-LV", "lav", "LVI", "lv", "lv-LV" };
            yield return new object[] { 0x0027, new [] { "lt" }, "lt-LT", "lit", "LTH", "lt", "lt-LT" };
            yield return new object[] { 0x0029, new [] { "fa" }, "fa-IR", "fas", "FAR", "fa", "en-US" };
            yield return new object[] { 0x002a, new [] { "vi" }, "vi-VN", "vie", "VIT", "vi", "en-US" };
            yield return new object[] { 0x0039, new [] { "hi" }, "hi-IN", "hin", "HIN", "hi", "en-US" };
            yield return new object[] { 0x003e, new [] { "ms" }, "ms-MY", "msa", "MSL", "ms", "ms-MY" };
            yield return new object[] { 0x0041, new [] { "sw" }, "sw-KE", "swa", "SWK", "sw", "sw-KE" };
            yield return new object[] { 0x0047, new [] { "gu" }, "gu-IN", "guj", "GUJ", "gu", "en-US" };
            yield return new object[] { 0x004a, new [] { "te" }, "te-IN", "tel", "TEL", "te", "en-US" };
            yield return new object[] { 0x004b, new [] { "kn" }, "kn-IN", "kan", "KDI", "kn", "en-US" };
            yield return new object[] { 0x004e, new [] { "mr" }, "mr-IN", "mar", "MAR", "mr", "en-US" };
            yield return new object[] { 0x005e, new [] { "am" }, "am-ET", "amh", "AMH", "am", "en-US" };
            yield return new object[] { 0x0401, new [] { "ar-sa" }, "ar-SA", "ara", "ARA", "ar-SA", "en-US" };
            yield return new object[] { 0x0402, new [] { "bg-bg" }, "bg-BG", "bul", "BGR", "bg-BG", "bg-BG" };
            yield return new object[] { 0x0403, new [] { "ca-es" }, "ca-ES", "cat", "CAT", "ca-ES", "ca-ES" };
            yield return new object[] { 0x0404, new [] { "zh-tw" }, "zh-TW", "zho", "CHT", "zh-Hant-TW", "zh-TW" };
            yield return new object[] { 0x0405, new [] { "cs-cz" }, "cs-CZ", "ces", "CSY", "cs-CZ", "cs-CZ" };
            yield return new object[] { 0x0406, new [] { "da-dk" }, "da-DK", "dan", "DAN", "da-DK", "da-DK" };
            yield return new object[] { 0x0407, new [] { "de-de" }, "de-DE", "deu", "DEU", "de-DE", "de-DE" };
            yield return new object[] { 0x0408, new [] { "el-gr" }, "el-GR", "ell", "ELL", "el-GR", "el-GR" };
            yield return new object[] { 0x0409, new [] { "en-us" }, "en-US", "eng", "ENU", "en-US", "en-US" };
            yield return new object[] { 0x040b, new [] { "fi-fi" }, "fi-FI", "fin", "FIN", "fi-FI", "fi-FI" };
            yield return new object[] { 0x040c, new [] { "fr-fr" }, "fr-FR", "fra", "FRA", "fr-FR", "fr-FR" };
            yield return new object[] { 0x040d, new [] { "he-il" }, "he-IL", "heb", "HEB", "he-IL", "en-US" };
            yield return new object[] { 0x040e, new [] { "hu-hu" }, "hu-HU", "hun", "HUN", "hu-HU", "hu-HU" };
            yield return new object[] { 0x0410, new [] { "it-it" }, "it-IT", "ita", "ITA", "it-IT", "it-IT" };
            yield return new object[] { 0x0411, new [] { "ja-jp" }, "ja-JP", "jpn", "JPN", "ja-JP", "ja-JP" };
            yield return new object[] { 0x0412, new [] { "ko-kr" }, "ko-KR", "kor", "KOR", "ko-KR", "ko-KR" };
            yield return new object[] { 0x0413, new [] { "nl-nl" }, "nl-NL", "nld", "NLD", "nl-NL", "nl-NL" };
            yield return new object[] { 0x0415, new [] { "pl-pl" }, "pl-PL", "pol", "PLK", "pl-PL", "pl-PL" };
            yield return new object[] { 0x0416, new [] { "pt-br" }, "pt-BR", "por", "PTB", "pt-BR", "pt-BR" };
            yield return new object[] { 0x0418, new [] { "ro-ro" }, "ro-RO", "ron", "ROM", "ro-RO", "ro-RO" };
            yield return new object[] { 0x0419, new [] { "ru-ru" }, "ru-RU", "rus", "RUS", "ru-RU", "ru-RU" };
            yield return new object[] { 0x041a, new [] { "hr-hr" }, "hr-HR", "hrv", "HRV", "hr-HR", "hr-HR" };
            yield return new object[] { 0x041b, new [] { "sk-sk" }, "sk-SK", "slk", "SKY", "sk-SK", "sk-SK" };
            yield return new object[] { 0x041d, new [] { "sv-se" }, "sv-SE", "swe", "SVE", "sv-SE", "sv-SE" };
            yield return new object[] { 0x041e, new [] { "th-th" }, "th-TH", "tha", "THA", "th-TH", "en-US" };
            yield return new object[] { 0x041f, new [] { "tr-tr" }, "tr-TR", "tur", "TRK", "tr-TR", "tr-TR" };
            yield return new object[] { 0x0421, new [] { "id-id" }, "id-ID", "ind", "IND", "id-ID", "id-ID" };
            yield return new object[] { 0x0422, new [] { "uk-ua" }, "uk-UA", "ukr", "UKR", "uk-UA", "uk-UA" };
            yield return new object[] { 0x0424, new [] { "sl-si" }, "sl-SI", "slv", "SLV", "sl-SI", "sl-SI" };
            yield return new object[] { 0x0425, new [] { "et-ee" }, "et-EE", "est", "ETI", "et-EE", "et-EE" };
            yield return new object[] { 0x0426, new [] { "lv-lv" }, "lv-LV", "lav", "LVI", "lv-LV", "lv-LV" };
            yield return new object[] { 0x0427, new [] { "lt-lt" }, "lt-LT", "lit", "LTH", "lt-LT", "lt-LT" };
            yield return new object[] { 0x0429, new [] { "fa-ir" }, "fa-IR", "fas", "FAR", "fa-IR", "en-US" };
            yield return new object[] { 0x042a, new [] { "vi-vn" }, "vi-VN", "vie", "VIT", "vi-VN", "en-US" };
            yield return new object[] { 0x0439, new [] { "hi-in" }, "hi-IN", "hin", "HIN", "hi-IN", "en-US" };
            yield return new object[] { 0x0441, new [] { "sw-ke" }, "sw-KE", "swa", "SWK", "sw-KE", "sw-KE" };
            yield return new object[] { 0x0447, new [] { "gu-in" }, "gu-IN", "guj", "GUJ", "gu-IN", "en-US" };
            yield return new object[] { 0x044a, new [] { "te-in" }, "te-IN", "tel", "TEL", "te-IN", "en-US" };
            yield return new object[] { 0x044b, new [] { "kn-in" }, "kn-IN", "kan", "KDI", "kn-IN", "en-US" };
            yield return new object[] { 0x044e, new [] { "mr-in" }, "mr-IN", "mar", "MAR", "mr-IN", "en-US" };
            yield return new object[] { 0x045e, new [] { "am-et" }, "am-ET", "amh", "AMH", "am-ET", "en-US" };
            yield return new object[] { 0x0464, new [] { "fil-ph" }, "fil-PH", "fil", "FPO", "fil-PH", "fil-PH" };
            yield return new object[] { 0x0804, new [] { "zh-cn" }, "zh-CN", "zho", "CHS", "zh-Hans-CN", "zh-CN" };
            yield return new object[] { 0x0807, new [] { "de-ch" }, "de-CH", "deu", "DES", "de-CH", "de-CH" };
            yield return new object[] { 0x0809, new [] { "en-gb" }, "en-GB", "eng", "ENG", "en-GB", "en-GB" };
            yield return new object[] { 0x080a, new [] { "es-mx" }, "es-MX", "spa", "ESM", "es-MX", "es-MX" };
            yield return new object[] { 0x080c, new [] { "fr-be" }, "fr-BE", "fra", "FRB", "fr-BE", "fr-BE" };
            yield return new object[] { 0x0810, new [] { "it-ch" }, "it-CH", "ita", "ITS", "it-CH", "it-CH" };
            yield return new object[] { 0x0813, new [] { "nl-be" }, "nl-BE", "nld", "NLB", "nl-BE", "nl-BE" };
            yield return new object[] { 0x0816, new [] { "pt-pt" }, "pt-PT", "por", "PTG", "pt-PT", "pt-PT" };
            yield return new object[] { 0x0c04, new [] { "zh-hk" }, "zh-HK", "zho", "ZHH", "zh-Hant-HK", "zh-HK" };
            yield return new object[] { 0x0c07, new [] { "de-at" }, "de-AT", "deu", "DEA", "de-AT", "de-AT" };
            yield return new object[] { 0x0c09, new [] { "en-au" }, "en-AU", "eng", "ENA", "en-AU", "en-AU" };
            yield return new object[] { 0x0c0a, new [] { "es-es" }, "es-ES", "spa", "ESN", "es-ES", "es-ES" };
            yield return new object[] { 0x0c0c, new [] { "fr-ca" }, "fr-CA", "fra", "FRC", "fr-CA", "fr-CA" };
            yield return new object[] { 0x1004, new [] { "zh-sg" }, "zh-SG", "zho", "ZHI", "zh-Hans-SG", "zh-SG" };
            yield return new object[] { 0x1007, new [] { "de-lu" }, "de-LU", "deu", "DEL", "de-LU", "de-LU" };
            yield return new object[] { 0x1009, new [] { "en-ca" }, "en-CA", "eng", "ENC", "en-CA", "en-CA" };
            yield return new object[] { 0x100c, new [] { "fr-ch" }, "fr-CH", "fra", "FRS", "fr-CH", "fr-CH" };
            yield return new object[] { 0x1407, new [] { "de-li" }, "de-LI", "deu", "DEC", "de-LI", "de-LI" };
            yield return new object[] { 0x1409, new [] { "en-nz" }, "en-NZ", "eng", "ENZ", "en-NZ", "en-NZ" };
            yield return new object[] { 0x1809, new [] { "en-ie" }, "en-IE", "eng", "ENI", "en-IE", "en-IE" };
            yield return new object[] { 0x1c09, new [] { "en-za" }, "en-ZA", "eng", "ENS", "en-ZA", "en-ZA" };
            yield return new object[] { 0x2009, new [] { "en-jm" }, "en-JM", "eng", "ENJ", "en-JM", "en-JM" };
            yield return new object[] { 0x241a, new [] { "sr-latn-rs" }, "sr-Latn-RS", "srp", "SRM", "sr-Latn-RS", "sr-Latn-RS" };
            yield return new object[] { 0x2809, new [] { "en-bz" }, "en-BZ", "eng", "ENL", "en-BZ", "en-BZ" };
            yield return new object[] { 0x281a, new [] { "sr-cyrl-rs" }, "sr-Cyrl-RS", "srp", "SRO", "sr-Cyrl-RS", "sr-Cyrl-RS" };
            yield return new object[] { 0x2c09, new [] { "en-tt" }, "en-TT", "eng", "ENT", "en-TT", "en-TT" };
            yield return new object[] { 0x3009, new [] { "en-zw" }, "en-ZW", "eng", "ENW", "en-ZW", "en-ZW" };
            yield return new object[] { 0x3409, new [] { "en-ph" }, "en-PH", "eng", "ENP", "en-PH", "en-PH" };
            yield return new object[] { 0x4009, new [] { "en-in" }, "en-IN", "eng", "ENN", "en-IN", "en-IN" };
            yield return new object[] { 0x4809, new [] { "en-sg" }, "en-SG", "eng", "ENE", "en-SG", "en-SG" };
            yield return new object[] { 0x6c1a, new [] { "sr-cyrl" }, "sr-Cyrl-RS", "srp", "SRO", "sr-Cyrl", "sr-Cyrl-RS" };
            yield return new object[] { 0x701a, new [] { "sr-latn" }, "sr-Latn-RS", "srp", "SRM", "sr-Latn", "sr-Latn-RS" };
            yield return new object[] { 0x7804, new [] { "zh" }, "zh-CN", "zho", "CHS", "zh", "zh-CN" };
            yield return new object[] { 0x7c04, new [] { "zh-cht", "zh-hant" }, "zh-HK", "zho", "CHT", "zh-Hant", "zh-HK", true };
            yield return new object[] { 0x7c1a, new [] { "sr" }, "sr-Latn-RS", "srp", "SRB", "sr", "sr-Latn-RS" };
            yield return new object[] { 0x10407, new [] { "de-de_phoneb", "de-de" }, "de-DE", "deu", "DEU", "de-DE", "de-DE", true };
            yield return new object[] { 0x1040e, new [] { "hu-hu_technl", "hu-hu" }, "hu-HU", "hun", "HUN", "hu-HU", "hu-HU", true };
            yield return new object[] { 0x20804, new [] { "zh-cn_stroke", "zh-cn" }, "zh-CN", "zho", "CHS", "zh-Hans-CN", "zh-CN", true };
            yield return new object[] { 0x21004, new [] { "zh-sg_stroke", "zh-sg" }, "zh-SG", "zho", "ZHI", "zh-Hans-SG", "zh-SG", true };
            yield return new object[] { 0x30404, new [] { "zh-tw_pronun", "zh-tw" }, "zh-TW", "zho", "CHT", "zh-Hant-TW", "zh-TW", true };
            yield return new object[] { 0x40404, new [] { "zh-tw_radstr", "zh-tw" }, "zh-TW", "zho", "CHT", "zh-Hant-TW", "zh-TW", true };
            yield return new object[] { 0x40411, new [] { "ja-jp_radstr", "ja-jp" }, "ja-JP", "jpn", "JPN", "ja-JP", "ja-JP", true };
            yield return new object[] { 0x40c04, new [] { "zh-hk_radstr", "zh-hk" }, "zh-HK", "zho", "ZHH", "zh-Hant-HK", "zh-HK", true };
        }

        [Theory]
        [MemberData(nameof(CultureInfo_TestData))]
        public void LcidTest(int lcid, string[] cultureNames, string specificCultureName, string threeLetterISOLanguageName, string threeLetterWindowsLanguageName, string alternativeCultureName, string consoleUICultureName, bool expectToThrowOnBrowser = false)
        {
            if (!expectToThrowOnBrowser || PlatformDetection.IsNotBrowser)
            {
                _ = alternativeCultureName;

                CultureInfo ci = new CultureInfo(lcid);
                Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);

                Assert.True(lcid == ci.LCID || (ushort)lcid == (ushort)ci.LCID);
                Assert.True(ci.UseUserOverride, "UseUserOverride for lcid created culture expected to be true");
                Assert.False(ci.IsReadOnly, "IsReadOnly for lcid created culture expected to be false");
                if (ci.ThreeLetterISOLanguageName != "")
                {
                    Assert.Equal(threeLetterISOLanguageName, ci.ThreeLetterISOLanguageName);
                }
                if (ci.ThreeLetterWindowsLanguageName != "ZZZ")
                {
                    Assert.True((threeLetterWindowsLanguageName == ci.ThreeLetterWindowsLanguageName) || (threeLetterWindowsLanguageName == "CHT" && ci.ThreeLetterWindowsLanguageName == "ZHH"));
                }
                ci = new CultureInfo(cultureNames[0]);
                Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);
                Assert.True(lcid == ci.LCID || (ushort)lcid == (ushort)ci.LCID);
                Assert.True(ci.UseUserOverride, "UseUserOverride for named created culture expected to be true");
                Assert.False(ci.IsReadOnly, "IsReadOnly for named created culture expected to be false");

                ci = new CultureInfo(lcid, false);
                Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);
                Assert.True(lcid == ci.LCID || (ushort)lcid == (ushort)ci.LCID);
                Assert.False(ci.UseUserOverride, "UseUserOverride with false user override culture expected to be false");
                Assert.False(ci.IsReadOnly, "IsReadOnly with false user override culture expected to be false");

                ci = CultureInfo.GetCultureInfo(lcid);
                Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);
                Assert.True(lcid == ci.LCID || (ushort)lcid == (ushort)ci.LCID);
                Assert.False(ci.UseUserOverride, "UseUserOverride with Culture created by GetCultureInfo and lcid expected to be false");
                Assert.True(ci.IsReadOnly, "IsReadOnly with Culture created by GetCultureInfo and lcid expected to be true");

                ci = CultureInfo.GetCultureInfo(cultureNames[0]);
                Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);
                Assert.True(lcid == ci.LCID || (ushort)lcid == (ushort)ci.LCID);
                Assert.False(ci.UseUserOverride, "UseUserOverride with Culture created by GetCultureInfo and name expected to be false");
                Assert.True(ci.IsReadOnly, "IsReadOnly with Culture created by GetCultureInfo and name expected to be true");

                ci = CultureInfo.GetCultureInfo(cultureNames[0], "");
                Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);
                Assert.True(lcid == ci.LCID || (ushort)lcid == (ushort)ci.LCID);
                Assert.False(ci.UseUserOverride, "UseUserOverride with Culture created by GetCultureInfo and sort name expected to be false");
                Assert.True(ci.IsReadOnly, "IsReadOnly with Culture created by GetCultureInfo and sort name expected to be true");
                Assert.Equal(CultureInfo.InvariantCulture.TextInfo, ci.TextInfo);
                Assert.Equal(CultureInfo.InvariantCulture.CompareInfo, ci.CompareInfo);

                ci = CultureInfo.CreateSpecificCulture(cultureNames[0]);
                TestCultureName(specificCultureName, ci.Name);

                // CultureInfo.GetCultureInfoByIetfLanguageTag doesn't support alternative sort LCID's.
                if (lcid <= 0xffff && lcid != 0x040a)
                {
                    ci = CultureInfo.GetCultureInfoByIetfLanguageTag(cultureNames[0]);
                    Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);
                    TestCultureName(ci.Name, ci.IetfLanguageTag);
                    Assert.True(lcid == ci.KeyboardLayoutId || (ushort)lcid == (ushort)ci.KeyboardLayoutId);
                }

                if (ci.GetConsoleFallbackUICulture().Name != "")
                {
                    Assert.Equal(consoleUICultureName, ci.GetConsoleFallbackUICulture().Name);
                }
            }
            else
            {
                AssertExtensions.Throws<CultureNotFoundException>(() => new CultureInfo(lcid));
            }

        }

        private static string[] hans = new[] { "zh-CN", "zh-CHS", "zh-Hans" };
        private static string[] hant = new[] { "zh-HK", "zh-CHT", "zh-Hant" };

        private static void TestCultureName(string left, string right)
        {
            if (hans.Contains(left, StringComparer.OrdinalIgnoreCase))
            {
                Assert.Contains(right, hans, StringComparer.OrdinalIgnoreCase);
            }
            else if (hant.Contains(left, StringComparer.OrdinalIgnoreCase))
            {
                Assert.Contains(right, hant, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                Assert.Equal(left, right, ignoreCase: true);
            }
        }

        [Fact]
        public void InstalledUICultureTest()
        {
            var c1 = CultureInfo.InstalledUICulture;
            var c2 = CultureInfo.InstalledUICulture;

            // we cannot expect the value we get for InstalledUICulture without reading the OS.
            // instead we test ensuring the value doesn't change if we requested it multiple times.
            Assert.Equal(c1.Name, c2.Name);
        }

        [Theory]
        [MemberData(nameof(CultureInfo_TestData))]
        public void GetCulturesTest(int lcid, string[] cultureNames, string specificCultureName, string threeLetterISOLanguageName, string threeLetterWindowsLanguageName, string alternativeCultureName, string consoleUICultureName, bool expectToThrowOnBrowser = false)
        {
            if (!expectToThrowOnBrowser || PlatformDetection.IsNotBrowser)
            {
                _ = lcid;
                _ = specificCultureName;
                _ = threeLetterISOLanguageName;
                _ = threeLetterWindowsLanguageName;
                _ = consoleUICultureName;

                bool found = false;
                Assert.All(CultureInfo.GetCultures(CultureTypes.NeutralCultures),
                        c => Assert.True( (c.IsNeutralCulture && ((c.CultureTypes & CultureTypes.NeutralCultures) != 0)) || c.Equals(CultureInfo.InvariantCulture)));
                found = CultureInfo.GetCultures(CultureTypes.NeutralCultures).Any(c => cultureNames.Contains(c.Name, StringComparer.OrdinalIgnoreCase) ||
                                                                                    c.Name.Equals(alternativeCultureName, StringComparison.OrdinalIgnoreCase));
                Assert.All(CultureInfo.GetCultures(CultureTypes.SpecificCultures), c => Assert.True(!c.IsNeutralCulture && ((c.CultureTypes & CultureTypes.SpecificCultures) != 0)));
                if (!found)
                {
                    found = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Any(c => cultureNames.Contains(c.Name, StringComparer.OrdinalIgnoreCase) ||
                                                                                        c.Name.Equals(alternativeCultureName, StringComparison.OrdinalIgnoreCase));
                }

                Assert.True(found, $"Expected to find the culture {cultureNames[0]} in the enumerated list");
            }
            else
            {
                AssertExtensions.Throws<CultureNotFoundException>(() => new CultureInfo(lcid));
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ClearCachedDataTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo ci = CultureInfo.GetCultureInfo("ja-JP");
                Assert.True((object) ci == (object) CultureInfo.GetCultureInfo("ja-JP"), "Expected getting same object reference");
                ci.ClearCachedData();
                Assert.False((object) ci == (object) CultureInfo.GetCultureInfo("ja-JP"), "expected to get a new object reference");
            }).Dispose();
        }

        [Fact]
        public void CultureNotFoundExceptionTest()
        {
            AssertExtensions.Throws<CultureNotFoundException>("name", () => new CultureInfo("!@#$%^&*()"));
            AssertExtensions.Throws<CultureNotFoundException>("name", () => new CultureInfo("This is invalid culture"));
            AssertExtensions.Throws<CultureNotFoundException>("name", () => new CultureInfo("longCulture" + new string('a', 100)));
            AssertExtensions.Throws<CultureNotFoundException>("culture", () => new CultureInfo(0x1000));

            CultureNotFoundException e = AssertExtensions.Throws<CultureNotFoundException>("name", () => new CultureInfo("This is invalid culture"));
            Assert.Equal("This is invalid culture", e.InvalidCultureName);

            e = AssertExtensions.Throws<CultureNotFoundException>("culture", () => new CultureInfo(0x1000));
            Assert.Equal(0x1000, e.InvalidCultureId);
        }
    }
}
