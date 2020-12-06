// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using System.Text;
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
            yield return new object[] {  0x0002, new string[] { "bg" }                   , "bg-BG"         , "bul", "BGR", "bg"            , "bg-BG" };
            yield return new object[] {  0x0003, new string[] { "ca" }                   , "ca-ES"         , "cat", "CAT", "ca"            , "ca-ES" };
            yield return new object[] {  0x0004, new string[] { "zh-chs", "zh-hans" }    , "zh-CN"         , "zho", "CHS", "zh-Hans"       , "zh-CN" };
            yield return new object[] {  0x0005, new string[] { "cs" }                   , "cs-CZ"         , "ces", "CSY", "cs"            , "cs-CZ" };
            yield return new object[] {  0x0006, new string[] { "da" }                   , "da-DK"         , "dan", "DAN", "da"            , "da-DK" };
            yield return new object[] {  0x0007, new string[] { "de" }                   , "de-DE"         , "deu", "DEU", "de"            , "de-DE" };
            yield return new object[] {  0x0008, new string[] { "el" }                   , "el-GR"         , "ell", "ELL", "el"            , "el-GR" };
            yield return new object[] {  0x0009, new string[] { "en" }                   , "en-US"         , "eng", "ENU", "en"            , "en-US" };
            yield return new object[] {  0x000a, new string[] { "es" }                   , "es-ES"         , "spa", "ESN", "es"            , "es-ES" };
            yield return new object[] {  0x000b, new string[] { "fi" }                   , "fi-FI"         , "fin", "FIN", "fi"            , "fi-FI" };
            yield return new object[] {  0x000c, new string[] { "fr" }                   , "fr-FR"         , "fra", "FRA", "fr"            , "fr-FR" };
            yield return new object[] {  0x000d, new string[] { "he" }                   , "he-IL"         , "heb", "HEB", "he"            , "en-US" };
            yield return new object[] {  0x000e, new string[] { "hu" }                   , "hu-HU"         , "hun", "HUN", "hu"            , "hu-HU" };
            yield return new object[] {  0x000f, new string[] { "is" }                   , "is-IS"         , "isl", "ISL", "is"            , "is-IS" };
            yield return new object[] {  0x0010, new string[] { "it" }                   , "it-IT"         , "ita", "ITA", "it"            , "it-IT" };
            yield return new object[] {  0x0011, new string[] { "ja" }                   , "ja-JP"         , "jpn", "JPN", "ja"            , "ja-JP" };
            yield return new object[] {  0x0012, new string[] { "ko" }                   , "ko-KR"         , "kor", "KOR", "ko"            , "ko-KR" };
            yield return new object[] {  0x0013, new string[] { "nl" }                   , "nl-NL"         , "nld", "NLD", "nl"            , "nl-NL" };
            yield return new object[] {  0x0014, new string[] { "no" }                   , "nb-NO"         , "nor", "NOR", "no"            , "nb-NO" };
            yield return new object[] {  0x0015, new string[] { "pl" }                   , "pl-PL"         , "pol", "PLK", "pl"            , "pl-PL" };
            yield return new object[] {  0x0016, new string[] { "pt" }                   , "pt-BR"         , "por", "PTB", "pt"            , "pt-BR" };
            yield return new object[] {  0x0017, new string[] { "rm" }                   , "rm-CH"         , "roh", "RMC", "rm"            , "rm-CH" };
            yield return new object[] {  0x0018, new string[] { "ro" }                   , "ro-RO"         , "ron", "ROM", "ro"            , "ro-RO" };
            yield return new object[] {  0x0019, new string[] { "ru" }                   , "ru-RU"         , "rus", "RUS", "ru"            , "ru-RU" };
            yield return new object[] {  0x001a, new string[] { "hr" }                   , "hr-HR"         , "hrv", "HRV", "hr"            , "hr-HR" };
            yield return new object[] {  0x001b, new string[] { "sk" }                   , "sk-SK"         , "slk", "SKY", "sk"            , "sk-SK" };
            yield return new object[] {  0x001c, new string[] { "sq" }                   , "sq-AL"         , "sqi", "SQI", "sq"            , "sq-AL" };
            yield return new object[] {  0x001d, new string[] { "sv" }                   , "sv-SE"         , "swe", "SVE", "sv"            , "sv-SE" };
            yield return new object[] {  0x001e, new string[] { "th" }                   , "th-TH"         , "tha", "THA", "th"            , "en-US" };
            yield return new object[] {  0x001f, new string[] { "tr" }                   , "tr-TR"         , "tur", "TRK", "tr"            , "tr-TR" };
            yield return new object[] {  0x0020, new string[] { "ur" }                   , "ur-PK"         , "urd", "URD", "ur"            , "en-US" };
            yield return new object[] {  0x0021, new string[] { "id" }                   , "id-ID"         , "ind", "IND", "id"            , "id-ID" };
            yield return new object[] {  0x0022, new string[] { "uk" }                   , "uk-UA"         , "ukr", "UKR", "uk"            , "uk-UA" };
            yield return new object[] {  0x0023, new string[] { "be" }                   , "be-BY"         , "bel", "BEL", "be"            , "be-BY" };
            yield return new object[] {  0x0024, new string[] { "sl" }                   , "sl-SI"         , "slv", "SLV", "sl"            , "sl-SI" };
            yield return new object[] {  0x0025, new string[] { "et" }                   , "et-EE"         , "est", "ETI", "et"            , "et-EE" };
            yield return new object[] {  0x0026, new string[] { "lv" }                   , "lv-LV"         , "lav", "LVI", "lv"            , "lv-LV" };
            yield return new object[] {  0x0027, new string[] { "lt" }                   , "lt-LT"         , "lit", "LTH", "lt"            , "lt-LT" };
            yield return new object[] {  0x0028, new string[] { "tg" }                   , "tg-Cyrl-TJ"    , "tgk", "TAJ", "tg"            , "tg-Cyrl-TJ" };
            yield return new object[] {  0x0029, new string[] { "fa" }                   , "fa-IR"         , "fas", "FAR", "fa"            , "en-US" };
            yield return new object[] {  0x002a, new string[] { "vi" }                   , "vi-VN"         , "vie", "VIT", "vi"            , "en-US" };
            yield return new object[] {  0x002b, new string[] { "hy" }                   , "hy-AM"         , "hye", "HYE", "hy"            , "hy-AM" };
            yield return new object[] {  0x002c, new string[] { "az" }                   , "az-Latn-AZ"    , "aze", "AZE", "az"            , "az-Latn-AZ" };
            yield return new object[] {  0x002d, new string[] { "eu" }                   , "eu-ES"         , "eus", "EUQ", "eu"            , "en-US" };
            yield return new object[] {  0x002e, new string[] { "hsb" }                  , "hsb-DE"        , "hsb", "HSB", "hsb"           , "hsb-DE" };
            yield return new object[] {  0x002f, new string[] { "mk" }                   , "mk-MK"         , "mkd", "MKI", "mk"            , "mk-MK" };
            yield return new object[] {  0x0030, new string[] { "st" }                   , "st-ZA"         , "sot", "SOT", "st"            , "en-US" };
            yield return new object[] {  0x0031, new string[] { "ts" }                   , "ts-ZA"         , "tso", "TSO", "ts"            , "en-US" };
            yield return new object[] {  0x0032, new string[] { "tn" }                   , "tn-ZA"         , "tsn", "TSN", "tn"            , "tn-ZA" };
            yield return new object[] {  0x0033, new string[] { "ve" }                   , "ve-ZA"         , "ven", "ZZZ", "ve"            , "en-US" };
            yield return new object[] {  0x0034, new string[] { "xh" }                   , "xh-ZA"         , "xho", "XHO", "xh"            , "xh-ZA" };
            yield return new object[] {  0x0035, new string[] { "zu" }                   , "zu-ZA"         , "zul", "ZUL", "zu"            , "zu-ZA" };
            yield return new object[] {  0x0036, new string[] { "af" }                   , "af-ZA"         , "afr", "AFK", "af"            , "af-ZA" };
            yield return new object[] {  0x0037, new string[] { "ka" }                   , "ka-GE"         , "kat", "KAT", "ka"            , "ka-GE" };
            yield return new object[] {  0x0038, new string[] { "fo" }                   , "fo-FO"         , "fao", "FOS", "fo"            , "fo-FO" };
            yield return new object[] {  0x0039, new string[] { "hi" }                   , "hi-IN"         , "hin", "HIN", "hi"            , "en-US" };
            yield return new object[] {  0x003a, new string[] { "mt" }                   , "mt-MT"         , "mlt", "MLT", "mt"            , "mt-MT" };
            yield return new object[] {  0x003b, new string[] { "se" }                   , "se-NO"         , "sme", "SME", "se"            , "se-NO" };
            yield return new object[] {  0x003c, new string[] { "ga" }                   , "ga-IE"         , "gle", "IRE", "ga"            , "ga-IE" };
            yield return new object[] {  0x003d, new string[] { "yi" }                   , "yi-001"        , "yid", "ZZZ", "yi"            , "en-US" };
            yield return new object[] {  0x003e, new string[] { "ms" }                   , "ms-MY"         , "msa", "MSL", "ms"            , "ms-MY" };
            yield return new object[] {  0x003f, new string[] { "kk" }                   , "kk-KZ"         , "kaz", "KKZ", "kk"            , "kk-KZ" };
            yield return new object[] {  0x0040, new string[] { "ky" }                   , "ky-KG"         , "kir", "KYR", "ky"            , "ky-KG" };
            yield return new object[] {  0x0041, new string[] { "sw" }                   , "sw-KE"         , "swa", "SWK", "sw"            , "sw-KE" };
            yield return new object[] {  0x0042, new string[] { "tk" }                   , "tk-TM"         , "tuk", "TUK", "tk"            , "tk-TM" };
            yield return new object[] {  0x0043, new string[] { "uz" }                   , "uz-Latn-UZ"    , "uzb", "UZB", "uz"            , "uz-Latn-UZ" };
            yield return new object[] {  0x0044, new string[] { "tt" }                   , "tt-RU"         , "tat", "TTT", "tt"            , "tt-RU" };
            yield return new object[] {  0x0045, new string[] { "bn" }                   , "bn-BD"         , "ben", "BNB", "bn"            , "en-US" };
            yield return new object[] {  0x0046, new string[] { "pa" }                   , "pa-IN"         , "pan", "PAN", "pa"            , "en-US" };
            yield return new object[] {  0x0047, new string[] { "gu" }                   , "gu-IN"         , "guj", "GUJ", "gu"            , "en-US" };
            yield return new object[] {  0x0048, new string[] { "or" }                   , "or-IN"         , "ori", "ORI", "or"            , "en-US" };
            yield return new object[] {  0x0049, new string[] { "ta" }                   , "ta-IN"         , "tam", "TAI", "ta"            , "en-US" };
            yield return new object[] {  0x004a, new string[] { "te" }                   , "te-IN"         , "tel", "TEL", "te"            , "en-US" };
            yield return new object[] {  0x004b, new string[] { "kn" }                   , "kn-IN"         , "kan", "KDI", "kn"            , "en-US" };
            yield return new object[] {  0x004c, new string[] { "ml" }                   , "ml-IN"         , "mal", "MYM", "ml"            , "en-US" };
            yield return new object[] {  0x004d, new string[] { "as" }                   , "as-IN"         , "asm", "ASM", "as"            , "en-US" };
            yield return new object[] {  0x004e, new string[] { "mr" }                   , "mr-IN"         , "mar", "MAR", "mr"            , "en-US" };
            yield return new object[] {  0x004f, new string[] { "sa" }                   , "sa-IN"         , "san", "SAN", "sa"            , "en-US" };
            yield return new object[] {  0x0050, new string[] { "mn" }                   , "mn-MN"         , "mon", "MNN", "mn"            , "mn-MN" };
            yield return new object[] {  0x0051, new string[] { "bo" }                   , "bo-CN"         , "bod", "BOB", "bo"            , "en-US" };
            yield return new object[] {  0x0052, new string[] { "cy" }                   , "cy-GB"         , "cym", "CYM", "cy"            , "cy-GB" };
            yield return new object[] {  0x0053, new string[] { "km" }                   , "km-KH"         , "khm", "KHM", "km"            , "en-US" };
            yield return new object[] {  0x0054, new string[] { "lo" }                   , "lo-LA"         , "lao", "LAO", "lo"            , "en-US" };
            yield return new object[] {  0x0055, new string[] { "my" }                   , "my-MM"         , "mya", "MYA", "my"            , "en-US" };
            yield return new object[] {  0x0056, new string[] { "gl" }                   , "gl-ES"         , "glg", "GLC", "gl"            , "gl-ES" };
            yield return new object[] {  0x0057, new string[] { "kok" }                  , "kok-IN"        , "kok", "KNK", "kok"           , "en-US" };
            yield return new object[] {  0x0058, new string[] { "mni" }                  , "mni-IN"        , "mni", "ZZZ", "mni"           , "en-IN" };
            yield return new object[] {  0x0059, new string[] { "sd" }                   , "sd-Arab-PK"    , "snd", "SIP", "sd"            , "en-US" };
            yield return new object[] {  0x005a, new string[] { "syr" }                  , "syr-SY"        , "syr", "SYR", "syr"           , "en-US" };
            yield return new object[] {  0x005b, new string[] { "si" }                   , "si-LK"         , "sin", "SIN", "si"            , "en-US" };
            yield return new object[] {  0x005c, new string[] { "chr" }                  , "chr-Cher-US"   , "chr", "CRE", "chr"           , "en-US" };
            yield return new object[] {  0x005d, new string[] { "iu" }                   , "iu-Latn-CA"    , "iku", "IUK", "iu"            , "iu-Latn-CA" };
            yield return new object[] {  0x005e, new string[] { "am" }                   , "am-ET"         , "amh", "AMH", "am"            , "en-US" };
            yield return new object[] {  0x005f, new string[] { "tzm" }                  , "tzm-Latn-DZ"   , "tzm", "TZA", "tzm"           , "tzm-Latn-DZ" };
            yield return new object[] {  0x0060, new string[] { "ks" }                   , "ks-Arab-IN"    , "kas", "ZZZ", "ks"            , "en-US" };
            yield return new object[] {  0x0061, new string[] { "ne" }                   , "ne-NP"         , "nep", "NEP", "ne"            , "en-US" };
            yield return new object[] {  0x0062, new string[] { "fy" }                   , "fy-NL"         , "fry", "FYN", "fy"            , "fy-NL" };
            yield return new object[] {  0x0063, new string[] { "ps" }                   , "ps-AF"         , "pus", "PAS", "ps"            , "en-US" };
            yield return new object[] {  0x0064, new string[] { "fil" }                  , "fil-PH"        , "fil", "FPO", "fil"           , "fil-PH" };
            yield return new object[] {  0x0065, new string[] { "dv" }                   , "dv-MV"         , "div", "DIV", "dv"            , "en-US" };
            yield return new object[] {  0x0066, new string[] { "bin" }                  , "bin-NG"        , "bin", "ZZZ", "bin"           , "en-US" };
            yield return new object[] {  0x0067, new string[] { "ff" }                   , "ff-Latn-SN"    , "ful", "FUL", "ff"            , "ff-Latn-SN" };
            yield return new object[] {  0x0068, new string[] { "ha" }                   , "ha-Latn-NG"    , "hau", "HAU", "ha"            , "ha-Latn-NG" };
            yield return new object[] {  0x0069, new string[] { "ibb" }                  , "ibb-NG"        , "ibb", "ZZZ", "ibb"           , "en-US" };
            yield return new object[] {  0x006a, new string[] { "yo" }                   , "yo-NG"         , "yor", "YOR", "yo"            , "yo-NG" };
            yield return new object[] {  0x006b, new string[] { "quz" }                  , "quz-BO"        , ""   , "QUB", "quz"           , "quz-BO" };
            yield return new object[] {  0x006c, new string[] { "nso" }                  , "nso-ZA"        , "nso", "NSO", "nso"           , "nso-ZA" };
            yield return new object[] {  0x006d, new string[] { "ba" }                   , "ba-RU"         , "bak", "BAS", "ba"            , "ba-RU" };
            yield return new object[] {  0x006e, new string[] { "lb" }                   , "lb-LU"         , "ltz", "LBX", "lb"            , "lb-LU" };
            yield return new object[] {  0x006f, new string[] { "kl" }                   , "kl-GL"         , "kal", "KAL", "kl"            , "kl-GL" };
            yield return new object[] {  0x0070, new string[] { "ig" }                   , "ig-NG"         , "ibo", "IBO", "ig"            , "ig-NG" };
            yield return new object[] {  0x0071, new string[] { "kr" }                   , "kr-Latn-NG"    , "kau", "ZZZ", "kr"            , "en-US" };
            yield return new object[] {  0x0072, new string[] { "om" }                   , "om-ET"         , "orm", "ORM", "om"            , "en-US" };
            yield return new object[] {  0x0073, new string[] { "ti" }                   , "ti-ER"         , "tir", "TIR", "ti"            , "en-US" };
            yield return new object[] {  0x0074, new string[] { "gn" }                   , "gn-PY"         , "grn", "GRN", "gn"            , "gn-PY" };
            yield return new object[] {  0x0075, new string[] { "haw" }                  , "haw-US"        , "haw", "HAW", "haw"           , "haw-US" };
            yield return new object[] {  0x0076, new string[] { "la" }                   , "la-001"        , "lat", "ZZZ", "la"            , "en-US" };
            yield return new object[] {  0x0077, new string[] { "so" }                   , "so-SO"         , "som", "SOM", "so"            , "en-US" };
            yield return new object[] {  0x0078, new string[] { "ii" }                   , "ii-CN"         , "iii", "III", "ii"            , "en-US" };
            yield return new object[] {  0x0079, new string[] { "pap" }                  , "pap-029"       , "pap", "ZZZ", "pap"           , "en-029" };
            yield return new object[] {  0x007a, new string[] { "arn" }                  , "arn-CL"        , "arn", "MPD", "arn"           , "arn-CL" };
            yield return new object[] {  0x007c, new string[] { "moh" }                  , "moh-CA"        , "moh", "MWK", "moh"           , "en-US" };
            yield return new object[] {  0x007e, new string[] { "br" }                   , "br-FR"         , "bre", "BRE", "br"            , "br-FR" };
            yield return new object[] {  0x0080, new string[] { "ug" }                   , "ug-CN"         , "uig", "UIG", "ug"            , "en-US" };
            yield return new object[] {  0x0081, new string[] { "mi" }                   , "mi-NZ"         , "mri", "MRI", "mi"            , "mi-NZ" };
            yield return new object[] {  0x0082, new string[] { "oc" }                   , "oc-FR"         , "oci", "OCI", "oc"            , "oc-FR" };
            yield return new object[] {  0x0083, new string[] { "co" }                   , "co-FR"         , "cos", "COS", "co"            , "co-FR" };
            yield return new object[] {  0x0084, new string[] { "gsw" }                  , "gsw-CH"        , "gsw", "ZZZ", "gsw"           , "en-US" };
            yield return new object[] {  0x0085, new string[] { "sah" }                  , "sah-RU"        , "sah", "SAH", "sah"           , "sah-RU" };
            yield return new object[] {  0x0086, new string[] { "quc" }                  , "quc-Latn-GT"   , "quc", "QUT", "quc"           , "quc-Latn-GT" };
            yield return new object[] {  0x0087, new string[] { "rw" }                   , "rw-RW"         , "kin", "KIN", "rw"            , "rw-RW" };
            yield return new object[] {  0x0088, new string[] { "wo" }                   , "wo-SN"         , "wol", "WOL", "wo"            , "wo-SN" };
            yield return new object[] {  0x008c, new string[] { "prs" }                  , "prs-AF"        , ""   , "PRS", "prs"           , "en-US" };
            yield return new object[] {  0x0091, new string[] { "gd" }                   , "gd-GB"         , "gla", "GLA", "gd"            , "gd-GB" };
            yield return new object[] {  0x0092, new string[] { "ku" }                   , "ku-Arab-IQ"    , "kur", "KUR", "ku"            , "en-US" };
            yield return new object[] {  0x0401, new string[] { "ar-sa" }                , "ar-SA"         , "ara", "ARA", "ar-SA"         , "en-US" };
            yield return new object[] {  0x0402, new string[] { "bg-bg" }                , "bg-BG"         , "bul", "BGR", "bg-BG"         , "bg-BG" };
            yield return new object[] {  0x0403, new string[] { "ca-es" }                , "ca-ES"         , "cat", "CAT", "ca-ES"         , "ca-ES" };
            yield return new object[] {  0x0404, new string[] { "zh-tw" }                , "zh-TW"         , "zho", "CHT", "zh-Hant-TW"    , "zh-TW" };
            yield return new object[] {  0x0405, new string[] { "cs-cz" }                , "cs-CZ"         , "ces", "CSY", "cs-CZ"         , "cs-CZ" };
            yield return new object[] {  0x0406, new string[] { "da-dk" }                , "da-DK"         , "dan", "DAN", "da-DK"         , "da-DK" };
            yield return new object[] {  0x0407, new string[] { "de-de" }                , "de-DE"         , "deu", "DEU", "de-DE"         , "de-DE" };
            yield return new object[] {  0x0408, new string[] { "el-gr" }                , "el-GR"         , "ell", "ELL", "el-GR"         , "el-GR" };
            yield return new object[] {  0x0409, new string[] { "en-us" }                , "en-US"         , "eng", "ENU", "en-US"         , "en-US" };
            yield return new object[] {  0x040a, new string[] { "es-es_tradnl", "es-es" }, "es-ES"         , "spa", "ESP", "es-ES"         , "es-ES" };
            yield return new object[] {  0x040b, new string[] { "fi-fi" }                , "fi-FI"         , "fin", "FIN", "fi-FI"         , "fi-FI" };
            yield return new object[] {  0x040c, new string[] { "fr-fr" }                , "fr-FR"         , "fra", "FRA", "fr-FR"         , "fr-FR" };
            yield return new object[] {  0x040d, new string[] { "he-il" }                , "he-IL"         , "heb", "HEB", "he-IL"         , "en-US" };
            yield return new object[] {  0x040e, new string[] { "hu-hu" }                , "hu-HU"         , "hun", "HUN", "hu-HU"         , "hu-HU" };
            yield return new object[] {  0x040f, new string[] { "is-is" }                , "is-IS"         , "isl", "ISL", "is-IS"         , "is-IS" };
            yield return new object[] {  0x0410, new string[] { "it-it" }                , "it-IT"         , "ita", "ITA", "it-IT"         , "it-IT" };
            yield return new object[] {  0x0411, new string[] { "ja-jp" }                , "ja-JP"         , "jpn", "JPN", "ja-JP"         , "ja-JP" };
            yield return new object[] {  0x0412, new string[] { "ko-kr" }                , "ko-KR"         , "kor", "KOR", "ko-KR"         , "ko-KR" };
            yield return new object[] {  0x0413, new string[] { "nl-nl" }                , "nl-NL"         , "nld", "NLD", "nl-NL"         , "nl-NL" };
            yield return new object[] {  0x0414, new string[] { "nb-no" }                , "nb-NO"         , "nob", "NOR", "nb-NO"         , "nb-NO" };
            yield return new object[] {  0x0415, new string[] { "pl-pl" }                , "pl-PL"         , "pol", "PLK", "pl-PL"         , "pl-PL" };
            yield return new object[] {  0x0416, new string[] { "pt-br" }                , "pt-BR"         , "por", "PTB", "pt-BR"         , "pt-BR" };
            yield return new object[] {  0x0417, new string[] { "rm-ch" }                , "rm-CH"         , "roh", "RMC", "rm-CH"         , "rm-CH" };
            yield return new object[] {  0x0418, new string[] { "ro-ro" }                , "ro-RO"         , "ron", "ROM", "ro-RO"         , "ro-RO" };
            yield return new object[] {  0x0419, new string[] { "ru-ru" }                , "ru-RU"         , "rus", "RUS", "ru-RU"         , "ru-RU" };
            yield return new object[] {  0x041a, new string[] { "hr-hr" }                , "hr-HR"         , "hrv", "HRV", "hr-HR"         , "hr-HR" };
            yield return new object[] {  0x041b, new string[] { "sk-sk" }                , "sk-SK"         , "slk", "SKY", "sk-SK"         , "sk-SK" };
            yield return new object[] {  0x041c, new string[] { "sq-al" }                , "sq-AL"         , "sqi", "SQI", "sq-AL"         , "sq-AL" };
            yield return new object[] {  0x041d, new string[] { "sv-se" }                , "sv-SE"         , "swe", "SVE", "sv-SE"         , "sv-SE" };
            yield return new object[] {  0x041e, new string[] { "th-th" }                , "th-TH"         , "tha", "THA", "th-TH"         , "en-US" };
            yield return new object[] {  0x041f, new string[] { "tr-tr" }                , "tr-TR"         , "tur", "TRK", "tr-TR"         , "tr-TR" };
            yield return new object[] {  0x0420, new string[] { "ur-pk" }                , "ur-PK"         , "urd", "URD", "ur-PK"         , "en-US" };
            yield return new object[] {  0x0421, new string[] { "id-id" }                , "id-ID"         , "ind", "IND", "id-ID"         , "id-ID" };
            yield return new object[] {  0x0422, new string[] { "uk-ua" }                , "uk-UA"         , "ukr", "UKR", "uk-UA"         , "uk-UA" };
            yield return new object[] {  0x0423, new string[] { "be-by" }                , "be-BY"         , "bel", "BEL", "be-BY"         , "be-BY" };
            yield return new object[] {  0x0424, new string[] { "sl-si" }                , "sl-SI"         , "slv", "SLV", "sl-SI"         , "sl-SI" };
            yield return new object[] {  0x0425, new string[] { "et-ee" }                , "et-EE"         , "est", "ETI", "et-EE"         , "et-EE" };
            yield return new object[] {  0x0426, new string[] { "lv-lv" }                , "lv-LV"         , "lav", "LVI", "lv-LV"         , "lv-LV" };
            yield return new object[] {  0x0427, new string[] { "lt-lt" }                , "lt-LT"         , "lit", "LTH", "lt-LT"         , "lt-LT" };
            yield return new object[] {  0x0428, new string[] { "tg-cyrl-tj" }           , "tg-Cyrl-TJ"    , "tgk", "TAJ", "tg-Cyrl-TJ"    , "tg-Cyrl-TJ" };
            yield return new object[] {  0x0429, new string[] { "fa-ir" }                , "fa-IR"         , "fas", "FAR", "fa-IR"         , "en-US" };
            yield return new object[] {  0x042a, new string[] { "vi-vn" }                , "vi-VN"         , "vie", "VIT", "vi-VN"         , "en-US" };
            yield return new object[] {  0x042b, new string[] { "hy-am" }                , "hy-AM"         , "hye", "HYE", "hy-AM"         , "hy-AM" };
            yield return new object[] {  0x042c, new string[] { "az-latn-az" }           , "az-Latn-AZ"    , "aze", "AZE", "az-Latn-AZ"    , "az-Latn-AZ" };
            yield return new object[] {  0x042d, new string[] { "eu-es" }                , "eu-ES"         , "eus", "EUQ", "eu-ES"         , "en-US" };
            yield return new object[] {  0x042e, new string[] { "hsb-de" }               , "hsb-DE"        , "hsb", "HSB", "hsb-DE"        , "hsb-DE" };
            yield return new object[] {  0x042f, new string[] { "mk-mk" }                , "mk-MK"         , "mkd", "MKI", "mk-MK"         , "mk-MK" };
            yield return new object[] {  0x0430, new string[] { "st-za" }                , "st-ZA"         , "sot", "SOT", "st-ZA"         , "en-US" };
            yield return new object[] {  0x0431, new string[] { "ts-za" }                , "ts-ZA"         , "tso", "TSO", "ts-ZA"         , "en-US" };
            yield return new object[] {  0x0432, new string[] { "tn-za" }                , "tn-ZA"         , "tsn", "TSN", "tn-ZA"         , "tn-ZA" };
            yield return new object[] {  0x0433, new string[] { "ve-za" }                , "ve-ZA"         , "ven", "ZZZ", "ve-ZA"         , "en-US" };
            yield return new object[] {  0x0434, new string[] { "xh-za" }                , "xh-ZA"         , "xho", "XHO", "xh-ZA"         , "xh-ZA" };
            yield return new object[] {  0x0435, new string[] { "zu-za" }                , "zu-ZA"         , "zul", "ZUL", "zu-ZA"         , "zu-ZA" };
            yield return new object[] {  0x0436, new string[] { "af-za" }                , "af-ZA"         , "afr", "AFK", "af-ZA"         , "af-ZA" };
            yield return new object[] {  0x0437, new string[] { "ka-ge" }                , "ka-GE"         , "kat", "KAT", "ka-GE"         , "ka-GE" };
            yield return new object[] {  0x0438, new string[] { "fo-fo" }                , "fo-FO"         , "fao", "FOS", "fo-FO"         , "fo-FO" };
            yield return new object[] {  0x0439, new string[] { "hi-in" }                , "hi-IN"         , "hin", "HIN", "hi-IN"         , "en-US" };
            yield return new object[] {  0x043a, new string[] { "mt-mt" }                , "mt-MT"         , "mlt", "MLT", "mt-MT"         , "mt-MT" };
            yield return new object[] {  0x043b, new string[] { "se-no" }                , "se-NO"         , "sme", "SME", "se-NO"         , "se-NO" };
            yield return new object[] {  0x043d, new string[] { "yi-001" }               , "yi-001"        , "yid", "ZZZ", "yi-001"        , "en-US" };
            yield return new object[] {  0x043e, new string[] { "ms-my" }                , "ms-MY"         , "msa", "MSL", "ms-MY"         , "ms-MY" };
            yield return new object[] {  0x043f, new string[] { "kk-kz" }                , "kk-KZ"         , "kaz", "KKZ", "kk-KZ"         , "kk-KZ" };
            yield return new object[] {  0x0440, new string[] { "ky-kg" }                , "ky-KG"         , "kir", "KYR", "ky-KG"         , "ky-KG" };
            yield return new object[] {  0x0441, new string[] { "sw-ke" }                , "sw-KE"         , "swa", "SWK", "sw-KE"         , "sw-KE" };
            yield return new object[] {  0x0442, new string[] { "tk-tm" }                , "tk-TM"         , "tuk", "TUK", "tk-TM"         , "tk-TM" };
            yield return new object[] {  0x0443, new string[] { "uz-latn-uz" }           , "uz-Latn-UZ"    , "uzb", "UZB", "uz-Latn-UZ"    , "uz-Latn-UZ" };
            yield return new object[] {  0x0444, new string[] { "tt-ru" }                , "tt-RU"         , "tat", "TTT", "tt-RU"         , "tt-RU" };
            yield return new object[] {  0x0445, new string[] { "bn-in" }                , "bn-IN"         , "ben", "BNG", "bn-IN"         , "en-US" };
            yield return new object[] {  0x0446, new string[] { "pa-in" }                , "pa-IN"         , "pan", "PAN", "pa-IN"         , "en-US" };
            yield return new object[] {  0x0447, new string[] { "gu-in" }                , "gu-IN"         , "guj", "GUJ", "gu-IN"         , "en-US" };
            yield return new object[] {  0x0448, new string[] { "or-in" }                , "or-IN"         , "ori", "ORI", "or-IN"         , "en-US" };
            yield return new object[] {  0x0449, new string[] { "ta-in" }                , "ta-IN"         , "tam", "TAI", "ta-IN"         , "en-US" };
            yield return new object[] {  0x044a, new string[] { "te-in" }                , "te-IN"         , "tel", "TEL", "te-IN"         , "en-US" };
            yield return new object[] {  0x044b, new string[] { "kn-in" }                , "kn-IN"         , "kan", "KDI", "kn-IN"         , "en-US" };
            yield return new object[] {  0x044c, new string[] { "ml-in" }                , "ml-IN"         , "mal", "MYM", "ml-IN"         , "en-US" };
            yield return new object[] {  0x044d, new string[] { "as-in" }                , "as-IN"         , "asm", "ASM", "as-IN"         , "en-US" };
            yield return new object[] {  0x044e, new string[] { "mr-in" }                , "mr-IN"         , "mar", "MAR", "mr-IN"         , "en-US" };
            yield return new object[] {  0x044f, new string[] { "sa-in" }                , "sa-IN"         , "san", "SAN", "sa-IN"         , "en-US" };
            yield return new object[] {  0x0450, new string[] { "mn-mn" }                , "mn-MN"         , "mon", "MNN", "mn-MN"         , "mn-MN" };
            yield return new object[] {  0x0451, new string[] { "bo-cn" }                , "bo-CN"         , "bod", "BOB", "bo-CN"         , "en-US" };
            yield return new object[] {  0x0452, new string[] { "cy-gb" }                , "cy-GB"         , "cym", "CYM", "cy-GB"         , "cy-GB" };
            yield return new object[] {  0x0453, new string[] { "km-kh" }                , "km-KH"         , "khm", "KHM", "km-KH"         , "en-US" };
            yield return new object[] {  0x0454, new string[] { "lo-la" }                , "lo-LA"         , "lao", "LAO", "lo-LA"         , "en-US" };
            yield return new object[] {  0x0455, new string[] { "my-mm" }                , "my-MM"         , "mya", "MYA", "my-MM"         , "en-US" };
            yield return new object[] {  0x0456, new string[] { "gl-es" }                , "gl-ES"         , "glg", "GLC", "gl-ES"         , "gl-ES" };
            yield return new object[] {  0x0457, new string[] { "kok-in" }               , "kok-IN"        , "kok", "KNK", "kok-IN"        , "en-US" };
            yield return new object[] {  0x0458, new string[] { "mni-in" }               , "mni-IN"        , "mni", "ZZZ", "mni-IN"        , "en-IN" };
            yield return new object[] {  0x0459, new string[] { "sd-deva-in" }           , "sd-Deva-IN"    , "snd", "ZZZ", "sd-Deva-IN"    , "en-IN" };
            yield return new object[] {  0x045a, new string[] { "syr-sy" }               , "syr-SY"        , "syr", "SYR", "syr-SY"        , "en-US" };
            yield return new object[] {  0x045b, new string[] { "si-lk" }                , "si-LK"         , "sin", "SIN", "si-LK"         , "en-US" };
            yield return new object[] {  0x045c, new string[] { "chr-cher-us" }          , "chr-Cher-US"   , "chr", "CRE", "chr-Cher-US"   , "en-US" };
            yield return new object[] {  0x045d, new string[] { "iu-cans-ca" }           , "iu-Cans-CA"    , "iku", "IUS", "iu-Cans-CA"    , "en-US" };
            yield return new object[] {  0x045e, new string[] { "am-et" }                , "am-ET"         , "amh", "AMH", "am-ET"         , "en-US" };
            yield return new object[] {  0x045f, new string[] { "tzm-arab-ma" }          , "tzm-Arab-MA"   , "tzm", "ZZZ", "tzm-Arab-MA"   , "en-US" };
            yield return new object[] {  0x0460, new string[] { "ks-arab" }              , "ks-Arab-IN"    , "kas", "ZZZ", "ks-Arab"       , "en-US" };
            yield return new object[] {  0x0461, new string[] { "ne-np" }                , "ne-NP"         , "nep", "NEP", "ne-NP"         , "en-US" };
            yield return new object[] {  0x0462, new string[] { "fy-nl" }                , "fy-NL"         , "fry", "FYN", "fy-NL"         , "fy-NL" };
            yield return new object[] {  0x0463, new string[] { "ps-af" }                , "ps-AF"         , "pus", "PAS", "ps-AF"         , "en-US" };
            yield return new object[] {  0x0464, new string[] { "fil-ph" }               , "fil-PH"        , "fil", "FPO", "fil-PH"        , "fil-PH" };
            yield return new object[] {  0x0465, new string[] { "dv-mv" }                , "dv-MV"         , "div", "DIV", "dv-MV"         , "en-US" };
            yield return new object[] {  0x0466, new string[] { "bin-ng" }               , "bin-NG"        , "bin", "ZZZ", "bin-NG"        , "en-US" };
            yield return new object[] {  0x0467, new string[] { "ff-ng", "ff-latn-ng" }  , "ff-Latn-NG"    , "ful", "ZZZ", "ff-Latn-NG"    , "" };
            yield return new object[] {  0x0468, new string[] { "ha-latn-ng" }           , "ha-Latn-NG"    , "hau", "HAU", "ha-Latn-NG"    , "ha-Latn-NG" };
            yield return new object[] {  0x0469, new string[] { "ibb-ng" }               , "ibb-NG"        , "ibb", "ZZZ", "ibb-NG"        , "en-US" };
            yield return new object[] {  0x046a, new string[] { "yo-ng" }                , "yo-NG"         , "yor", "YOR", "yo-NG"         , "yo-NG" };
            yield return new object[] {  0x046b, new string[] { "quz-bo" }               , "quz-BO"        , ""   , "QUB", "quz-BO"        , "quz-BO" };
            yield return new object[] {  0x046c, new string[] { "nso-za" }               , "nso-ZA"        , "nso", "NSO", "nso-ZA"        , "nso-ZA" };
            yield return new object[] {  0x046d, new string[] { "ba-ru" }                , "ba-RU"         , "bak", "BAS", "ba-RU"         , "ba-RU" };
            yield return new object[] {  0x046e, new string[] { "lb-lu" }                , "lb-LU"         , "ltz", "LBX", "lb-LU"         , "lb-LU" };
            yield return new object[] {  0x046f, new string[] { "kl-gl" }                , "kl-GL"         , "kal", "KAL", "kl-GL"         , "kl-GL" };
            yield return new object[] {  0x0470, new string[] { "ig-ng" }                , "ig-NG"         , "ibo", "IBO", "ig-NG"         , "ig-NG" };
            yield return new object[] {  0x0471, new string[] { "kr-ng", "kr-latn-ng" }  , "kr-Latn-NG"    , "kau", "ZZZ", "kr-Latn-NG"    , "" };
            yield return new object[] {  0x0472, new string[] { "om-et" }                , "om-ET"         , "orm", "ORM", "om-ET"         , "en-US" };
            yield return new object[] {  0x0473, new string[] { "ti-et" }                , "ti-ET"         , "tir", "TIE", "ti-ET"         , "en-US" };
            yield return new object[] {  0x0474, new string[] { "gn-py" }                , "gn-PY"         , "grn", "GRN", "gn-PY"         , "gn-PY" };
            yield return new object[] {  0x0475, new string[] { "haw-us" }               , "haw-US"        , "haw", "HAW", "haw-US"        , "haw-US" };
            yield return new object[] {  0x0476, new string[] { "la-001" }               , "la-001"        , "lat", "ZZZ", "la-001"        , "en-US" };
            yield return new object[] {  0x0477, new string[] { "so-so" }                , "so-SO"         , "som", "SOM", "so-SO"         , "en-US" };
            yield return new object[] {  0x0478, new string[] { "ii-cn" }                , "ii-CN"         , "iii", "III", "ii-CN"         , "en-US" };
            yield return new object[] {  0x0479, new string[] { "pap-029" }              , "pap-029"       , "pap", "ZZZ", "pap-029"       , "en-029" };
            yield return new object[] {  0x047a, new string[] { "arn-cl" }               , "arn-CL"        , "arn", "MPD", "arn-CL"        , "arn-CL" };
            yield return new object[] {  0x047c, new string[] { "moh-ca" }               , "moh-CA"        , "moh", "MWK", "moh-CA"        , "en-US" };
            yield return new object[] {  0x047e, new string[] { "br-fr" }                , "br-FR"         , "bre", "BRE", "br-FR"         , "br-FR" };
            yield return new object[] {  0x0480, new string[] { "ug-cn" }                , "ug-CN"         , "uig", "UIG", "ug-CN"         , "en-US" };
            yield return new object[] {  0x0481, new string[] { "mi-nz" }                , "mi-NZ"         , "mri", "MRI", "mi-NZ"         , "mi-NZ" };
            yield return new object[] {  0x0482, new string[] { "oc-fr" }                , "oc-FR"         , "oci", "OCI", "oc-FR"         , "oc-FR" };
            yield return new object[] {  0x0483, new string[] { "co-fr" }                , "co-FR"         , "cos", "COS", "co-FR"         , "co-FR" };
            yield return new object[] {  0x0484, new string[] { "gsw-fr" }               , "gsw-FR"        , "gsw", "GSW", "gsw-FR"        , "gsw-FR" };
            yield return new object[] {  0x0485, new string[] { "sah-ru" }               , "sah-RU"        , "sah", "SAH", "sah-RU"        , "sah-RU" };
            yield return new object[] {  0x0486, new string[] { "quc-latn-gt" }          , "quc-Latn-GT"   , "quc", "QUT", "quc-Latn-GT"   , "quc-Latn-GT" };
            yield return new object[] {  0x0487, new string[] { "rw-rw" }                , "rw-RW"         , "kin", "KIN", "rw-RW"         , "rw-RW" };
            yield return new object[] {  0x0488, new string[] { "wo-sn" }                , "wo-SN"         , "wol", "WOL", "wo-SN"         , "wo-SN" };
            yield return new object[] {  0x048c, new string[] { "prs-af" }               , "prs-AF"        , ""   , "PRS", "prs-AF"        , "en-US" };
            yield return new object[] {  0x0491, new string[] { "gd-gb" }                , "gd-GB"         , "gla", "GLA", "gd-GB"         , "gd-GB" };
            yield return new object[] {  0x0492, new string[] { "ku-arab-iq" }           , "ku-Arab-IQ"    , "kur", "KUR", "ku-Arab-IQ"    , "en-US" };
            yield return new object[] {  0x0501, new string[] { "qps-ploc" }             , "qps-ploc"      , ""   , "ENU", "qps-ploc"      , "qps-ploc" };
            yield return new object[] {  0x05fe, new string[] { "qps-ploca" }            , "qps-ploca"     , ""   , "JPN", "qps-ploca"     , "qps-ploca" };
            yield return new object[] {  0x0801, new string[] { "ar-iq" }                , "ar-IQ"         , "ara", "ARI", "ar-IQ"         , "en-US" };
            yield return new object[] {  0x0803, new string[] { "ca-es-valencia" }       , "ca-ES-valencia", "cat", "VAL", "ca-ES-valencia", "ca-ES" };
            yield return new object[] {  0x0804, new string[] { "zh-cn" }                , "zh-CN"         , "zho", "CHS", "zh-Hans-CN"    , "zh-CN" };
            yield return new object[] {  0x0807, new string[] { "de-ch" }                , "de-CH"         , "deu", "DES", "de-CH"         , "de-CH" };
            yield return new object[] {  0x0809, new string[] { "en-gb" }                , "en-GB"         , "eng", "ENG", "en-GB"         , "en-GB" };
            yield return new object[] {  0x080a, new string[] { "es-mx" }                , "es-MX"         , "spa", "ESM", "es-MX"         , "es-MX" };
            yield return new object[] {  0x080c, new string[] { "fr-be" }                , "fr-BE"         , "fra", "FRB", "fr-BE"         , "fr-BE" };
            yield return new object[] {  0x0810, new string[] { "it-ch" }                , "it-CH"         , "ita", "ITS", "it-CH"         , "it-CH" };
            yield return new object[] {  0x0813, new string[] { "nl-be" }                , "nl-BE"         , "nld", "NLB", "nl-BE"         , "nl-BE" };
            yield return new object[] {  0x0814, new string[] { "nn-no" }                , "nn-NO"         , "nno", "NON", "nn-NO"         , "nn-NO" };
            yield return new object[] {  0x0816, new string[] { "pt-pt" }                , "pt-PT"         , "por", "PTG", "pt-PT"         , "pt-PT" };
            yield return new object[] {  0x0818, new string[] { "ro-md" }                , "ro-MD"         , "ron", "ROD", "ro-MD"         , "en-US" };
            yield return new object[] {  0x0819, new string[] { "ru-md" }                , "ru-MD"         , "rus", "RUM", "ru-MD"         , "en-US" };
            yield return new object[] {  0x081a, new string[] { "sr-latn-cs" }           , "sr-Latn-CS"    , "srp", "SRL", "sr-Latn-CS"    , "sr-Latn-CS" };
            yield return new object[] {  0x081d, new string[] { "sv-fi" }                , "sv-FI"         , "swe", "SVF", "sv-FI"         , "sv-FI" };
            yield return new object[] {  0x0820, new string[] { "ur-in" }                , "ur-IN"         , "urd", "URI", "ur-IN"         , "en-US" };
            yield return new object[] {  0x082c, new string[] { "az-cyrl-az" }           , "az-Cyrl-AZ"    , "aze", "AZC", "az-Cyrl-AZ"    , "az-Cyrl-AZ" };
            yield return new object[] {  0x082e, new string[] { "dsb-de" }               , "dsb-DE"        , "dsb", "DSB", "dsb-DE"        , "dsb-DE" };
            yield return new object[] {  0x0832, new string[] { "tn-bw" }                , "tn-BW"         , "tsn", "TSB", "tn-BW"         , "tn-BW" };
            yield return new object[] {  0x083b, new string[] { "se-se" }                , "se-SE"         , "sme", "SMF", "se-SE"         , "se-SE" };
            yield return new object[] {  0x083c, new string[] { "ga-ie" }                , "ga-IE"         , "gle", "IRE", "ga-IE"         , "ga-IE" };
            yield return new object[] {  0x083e, new string[] { "ms-bn" }                , "ms-BN"         , "msa", "MSB", "ms-BN"         , "ms-BN" };
            yield return new object[] {  0x0843, new string[] { "uz-cyrl-uz" }           , "uz-Cyrl-UZ"    , "uzb", "UZC", "uz-Cyrl-UZ"    , "uz-Cyrl-UZ" };
            yield return new object[] {  0x0845, new string[] { "bn-bd" }                , "bn-BD"         , "ben", "BNB", "bn-BD"         , "en-US" };
            yield return new object[] {  0x0846, new string[] { "pa-arab-pk" }           , "pa-Arab-PK"    , "pan", "PAP", "pa-Arab-PK"    , "en-US" };
            yield return new object[] {  0x0849, new string[] { "ta-lk" }                , "ta-LK"         , "tam", "TAM", "ta-LK"         , "en-US" };
            yield return new object[] {  0x0850, new string[] { "mn-mong-cn" }           , "mn-Mong-CN"    , "mon", "MNG", "mn-Mong-CN"    , "mn-Mong-CN" };
            yield return new object[] {  0x0859, new string[] { "sd-arab-pk" }           , "sd-Arab-PK"    , "snd", "SIP", "sd-Arab-PK"    , "en-US" };
            yield return new object[] {  0x085d, new string[] { "iu-latn-ca" }           , "iu-Latn-CA"    , "iku", "IUK", "iu-Latn-CA"    , "iu-Latn-CA" };
            yield return new object[] {  0x085f, new string[] { "tzm-latn-dz" }          , "tzm-Latn-DZ"   , "tzm", "TZA", "tzm-Latn-DZ"   , "tzm-Latn-DZ" };
            yield return new object[] {  0x0860, new string[] { "ks-deva-in" }           , "ks-Deva-IN"    , "kas", "ZZZ", "ks-Deva-IN"    , "en-IN" };
            yield return new object[] {  0x0861, new string[] { "ne-in" }                , "ne-IN"         , "nep", "NEI", "ne-IN"         , "en-US" };
            yield return new object[] {  0x0867, new string[] { "ff-latn-sn" }           , "ff-Latn-SN"    , "ful", "FUL", "ff-Latn-SN"    , "ff-Latn-SN" };
            yield return new object[] {  0x086b, new string[] { "quz-ec" }               , "quz-EC"        , ""   , "QUE", "quz-EC"        , "quz-EC" };
            yield return new object[] {  0x0873, new string[] { "ti-er" }                , "ti-ER"         , "tir", "TIR", "ti-ER"         , "en-US" };
            yield return new object[] {  0x0901, new string[] { "qps-latn-x-sh" }        , "qps-Latn-x-sh" , ""   , "ENJ", "qps-Latn-x-sh" , "en-JM" };
            yield return new object[] {  0x09ff, new string[] { "qps-plocm" }            , "qps-plocm"     , ""   , "ARA", "qps-plocm"     , "en-US" };
            yield return new object[] {  0x0c01, new string[] { "ar-eg" }                , "ar-EG"         , "ara", "ARE", "ar-EG"         , "en-US" };
            yield return new object[] {  0x0c04, new string[] { "zh-hk" }                , "zh-HK"         , "zho", "ZHH", "zh-Hant-HK"    , "zh-HK" };
            yield return new object[] {  0x0c07, new string[] { "de-at" }                , "de-AT"         , "deu", "DEA", "de-AT"         , "de-AT" };
            yield return new object[] {  0x0c09, new string[] { "en-au" }                , "en-AU"         , "eng", "ENA", "en-AU"         , "en-AU" };
            yield return new object[] {  0x0c0a, new string[] { "es-es" }                , "es-ES"         , "spa", "ESN", "es-ES"         , "es-ES" };
            yield return new object[] {  0x0c0c, new string[] { "fr-ca" }                , "fr-CA"         , "fra", "FRC", "fr-CA"         , "fr-CA" };
            yield return new object[] {  0x0c1a, new string[] { "sr-cyrl-cs" }           , "sr-Cyrl-CS"    , "srp", "SRB", "sr-Cyrl-CS"    , "sr-Cyrl-CS" };
            yield return new object[] {  0x0c3b, new string[] { "se-fi" }                , "se-FI"         , "sme", "SMG", "se-FI"         , "se-FI" };
            yield return new object[] {  0x0c50, new string[] { "mn-mong-mn" }           , "mn-Mong-MN"    , "mon", "MNM", "mn-Mong-MN"    , "mn-Mong-MN" };
            yield return new object[] {  0x0c51, new string[] { "dz-bt" }                , "dz-BT"         , "dzo", "ZZZ", "dz-BT"         , "en-US" };
            yield return new object[] {  0x0c6b, new string[] { "quz-pe" }               , "quz-PE"        , ""   , "QUP", "quz-PE"        , "quz-PE" };
            yield return new object[] {  0x1001, new string[] { "ar-ly" }                , "ar-LY"         , "ara", "ARL", "ar-LY"         , "en-US" };
            yield return new object[] {  0x1004, new string[] { "zh-sg" }                , "zh-SG"         , "zho", "ZHI", "zh-Hans-SG"    , "zh-SG" };
            yield return new object[] {  0x1007, new string[] { "de-lu" }                , "de-LU"         , "deu", "DEL", "de-LU"         , "de-LU" };
            yield return new object[] {  0x1009, new string[] { "en-ca" }                , "en-CA"         , "eng", "ENC", "en-CA"         , "en-CA" };
            yield return new object[] {  0x100a, new string[] { "es-gt" }                , "es-GT"         , "spa", "ESG", "es-GT"         , "es-GT" };
            yield return new object[] {  0x100c, new string[] { "fr-ch" }                , "fr-CH"         , "fra", "FRS", "fr-CH"         , "fr-CH" };
            yield return new object[] {  0x101a, new string[] { "hr-ba" }                , "hr-BA"         , "hrv", "HRB", "hr-BA"         , "hr-BA" };
            yield return new object[] {  0x103b, new string[] { "smj-no" }               , "smj-NO"        , "smj", "SMJ", "smj-NO"        , "smj-NO" };
            yield return new object[] {  0x105f, new string[] { "tzm-tfng-ma" }          , "tzm-Tfng-MA"   , "tzm", "TZM", "tzm-Tfng-MA"   , "fr-FR" };
            yield return new object[] {  0x1401, new string[] { "ar-dz" }                , "ar-DZ"         , "ara", "ARG", "ar-DZ"         , "fr-FR" };
            yield return new object[] {  0x1404, new string[] { "zh-mo" }                , "zh-MO"         , "zho", "ZHM", "zh-Hant-MO"    , "zh-MO" };
            yield return new object[] {  0x1407, new string[] { "de-li" }                , "de-LI"         , "deu", "DEC", "de-LI"         , "de-LI" };
            yield return new object[] {  0x1409, new string[] { "en-nz" }                , "en-NZ"         , "eng", "ENZ", "en-NZ"         , "en-NZ" };
            yield return new object[] {  0x140a, new string[] { "es-cr" }                , "es-CR"         , "spa", "ESC", "es-CR"         , "es-CR" };
            yield return new object[] {  0x140c, new string[] { "fr-lu" }                , "fr-LU"         , "fra", "FRL", "fr-LU"         , "fr-LU" };
            yield return new object[] {  0x141a, new string[] { "bs-latn-ba" }           , "bs-Latn-BA"    , "bos", "BSB", "bs-Latn-BA"    , "bs-Latn-BA" };
            yield return new object[] {  0x143b, new string[] { "smj-se" }               , "smj-SE"        , "smj", "SMK", "smj-SE"        , "smj-SE" };
            yield return new object[] {  0x1801, new string[] { "ar-ma" }                , "ar-MA"         , "ara", "ARM", "ar-MA"         , "fr-FR" };
            yield return new object[] {  0x1809, new string[] { "en-ie" }                , "en-IE"         , "eng", "ENI", "en-IE"         , "en-IE" };
            yield return new object[] {  0x180a, new string[] { "es-pa" }                , "es-PA"         , "spa", "ESA", "es-PA"         , "es-PA" };
            yield return new object[] {  0x180c, new string[] { "fr-mc" }                , "fr-MC"         , "fra", "FRM", "fr-MC"         , "fr-MC" };
            yield return new object[] {  0x181a, new string[] { "sr-latn-ba" }           , "sr-Latn-BA"    , "srp", "SRS", "sr-Latn-BA"    , "sr-Latn-BA" };
            yield return new object[] {  0x183b, new string[] { "sma-no" }               , "sma-NO"        , "sma", "SMA", "sma-NO"        , "sma-NO" };
            yield return new object[] {  0x1c01, new string[] { "ar-tn" }                , "ar-TN"         , "ara", "ART", "ar-TN"         , "fr-FR" };
            yield return new object[] {  0x1c09, new string[] { "en-za" }                , "en-ZA"         , "eng", "ENS", "en-ZA"         , "en-ZA" };
            yield return new object[] {  0x1c0a, new string[] { "es-do" }                , "es-DO"         , "spa", "ESD", "es-DO"         , "es-DO" };
            yield return new object[] {  0x1c0c, new string[] { "fr-029" }               , "fr-029"        , "fra", "ZZZ", "fr-029"        , "fr-FR" };
            yield return new object[] {  0x1c1a, new string[] { "sr-cyrl-ba" }           , "sr-Cyrl-BA"    , "srp", "SRN", "sr-Cyrl-BA"    , "sr-Cyrl-BA" };
            yield return new object[] {  0x1c3b, new string[] { "sma-se" }               , "sma-SE"        , "sma", "SMB", "sma-SE"        , "sma-SE" };
            yield return new object[] {  0x2001, new string[] { "ar-om" }                , "ar-OM"         , "ara", "ARO", "ar-OM"         , "en-US" };
            yield return new object[] {  0x2009, new string[] { "en-jm" }                , "en-JM"         , "eng", "ENJ", "en-JM"         , "en-JM" };
            yield return new object[] {  0x200a, new string[] { "es-ve" }                , "es-VE"         , "spa", "ESV", "es-VE"         , "es-VE" };
            yield return new object[] {  0x200c, new string[] { "fr-re" }                , "fr-RE"         , "fra", "FRR", "fr-RE"         , "en-US" };
            yield return new object[] {  0x201a, new string[] { "bs-cyrl-ba" }           , "bs-Cyrl-BA"    , "bos", "BSC", "bs-Cyrl-BA"    , "bs-Cyrl-BA" };
            yield return new object[] {  0x203b, new string[] { "sms-fi" }               , "sms-FI"        , "sms", "SMS", "sms-FI"        , "sms-FI" };
            yield return new object[] {  0x2401, new string[] { "ar-ye" }                , "ar-YE"         , "ara", "ARY", "ar-YE"         , "en-US" };
            yield return new object[] {  0x2409, new string[] { "en-029" }               , "en-029"        , "eng", "ENB", "en-029"        , "en-029" };
            yield return new object[] {  0x240a, new string[] { "es-co" }                , "es-CO"         , "spa", "ESO", "es-CO"         , "es-CO" };
            yield return new object[] {  0x240c, new string[] { "fr-cd" }                , "fr-CD"         , "fra", "FRD", "fr-CD"         , "en-US" };
            yield return new object[] {  0x241a, new string[] { "sr-latn-rs" }           , "sr-Latn-RS"    , "srp", "SRM", "sr-Latn-RS"    , "sr-Latn-RS" };
            yield return new object[] {  0x243b, new string[] { "smn-fi" }               , "smn-FI"        , "smn", "SMN", "smn-FI"        , "smn-FI" };
            yield return new object[] {  0x2801, new string[] { "ar-sy" }                , "ar-SY"         , "ara", "ARS", "ar-SY"         , "en-US" };
            yield return new object[] {  0x2809, new string[] { "en-bz" }                , "en-BZ"         , "eng", "ENL", "en-BZ"         , "en-BZ" };
            yield return new object[] {  0x280a, new string[] { "es-pe" }                , "es-PE"         , "spa", "ESR", "es-PE"         , "es-PE" };
            yield return new object[] {  0x280c, new string[] { "fr-sn" }                , "fr-SN"         , "fra", "FRN", "fr-SN"         , "en-US" };
            yield return new object[] {  0x281a, new string[] { "sr-cyrl-rs" }           , "sr-Cyrl-RS"    , "srp", "SRO", "sr-Cyrl-RS"    , "sr-Cyrl-RS" };
            yield return new object[] {  0x2c01, new string[] { "ar-jo" }                , "ar-JO"         , "ara", "ARJ", "ar-JO"         , "en-US" };
            yield return new object[] {  0x2c09, new string[] { "en-tt" }                , "en-TT"         , "eng", "ENT", "en-TT"         , "en-TT" };
            yield return new object[] {  0x2c0a, new string[] { "es-ar" }                , "es-AR"         , "spa", "ESS", "es-AR"         , "es-AR" };
            yield return new object[] {  0x2c0c, new string[] { "fr-cm" }                , "fr-CM"         , "fra", "FRE", "fr-CM"         , "en-US" };
            yield return new object[] {  0x2c1a, new string[] { "sr-latn-me" }           , "sr-Latn-ME"    , "srp", "SRP", "sr-Latn-ME"    , "sr-Latn-ME" };
            yield return new object[] {  0x3001, new string[] { "ar-lb" }                , "ar-LB"         , "ara", "ARB", "ar-LB"         , "en-US" };
            yield return new object[] {  0x3009, new string[] { "en-zw" }                , "en-ZW"         , "eng", "ENW", "en-ZW"         , "en-ZW" };
            yield return new object[] {  0x300a, new string[] { "es-ec" }                , "es-EC"         , "spa", "ESF", "es-EC"         , "es-EC" };
            yield return new object[] {  0x300c, new string[] { "fr-ci" }                , "fr-CI"         , "fra", "FRI", "fr-CI"         , "en-US" };
            yield return new object[] {  0x301a, new string[] { "sr-cyrl-me" }           , "sr-Cyrl-ME"    , "srp", "SRQ", "sr-Cyrl-ME"    , "sr-Cyrl-ME" };
            yield return new object[] {  0x3401, new string[] { "ar-kw" }                , "ar-KW"         , "ara", "ARK", "ar-KW"         , "en-US" };
            yield return new object[] {  0x3409, new string[] { "en-ph" }                , "en-PH"         , "eng", "ENP", "en-PH"         , "en-PH" };
            yield return new object[] {  0x340a, new string[] { "es-cl" }                , "es-CL"         , "spa", "ESL", "es-CL"         , "es-CL" };
            yield return new object[] {  0x340c, new string[] { "fr-ml" }                , "fr-ML"         , "fra", "FRF", "fr-ML"         , "en-US" };
            yield return new object[] {  0x3801, new string[] { "ar-ae" }                , "ar-AE"         , "ara", "ARU", "ar-AE"         , "en-US" };
            yield return new object[] {  0x3809, new string[] { "en-id" }                , "en-ID"         , "eng", "ZZZ", "en-ID"         , "en-US" };
            yield return new object[] {  0x380a, new string[] { "es-uy" }                , "es-UY"         , "spa", "ESY", "es-UY"         , "es-UY" };
            yield return new object[] {  0x380c, new string[] { "fr-ma" }                , "fr-MA"         , "fra", "FRO", "fr-MA"         , "en-US" };
            yield return new object[] {  0x3c01, new string[] { "ar-bh" }                , "ar-BH"         , "ara", "ARH", "ar-BH"         , "en-US" };
            yield return new object[] {  0x3c09, new string[] { "en-hk" }                , "en-HK"         , "eng", "ENH", "en-HK"         , "en-US" };
            yield return new object[] {  0x3c0a, new string[] { "es-py" }                , "es-PY"         , "spa", "ESZ", "es-PY"         , "es-PY" };
            yield return new object[] {  0x3c0c, new string[] { "fr-ht" }                , "fr-HT"         , "fra", "FRH", "fr-HT"         , "en-US" };
            yield return new object[] {  0x4001, new string[] { "ar-qa" }                , "ar-QA"         , "ara", "ARQ", "ar-QA"         , "en-US" };
            yield return new object[] {  0x4009, new string[] { "en-in" }                , "en-IN"         , "eng", "ENN", "en-IN"         , "en-IN" };
            yield return new object[] {  0x400a, new string[] { "es-bo" }                , "es-BO"         , "spa", "ESB", "es-BO"         , "es-BO" };
            yield return new object[] {  0x4409, new string[] { "en-my" }                , "en-MY"         , "eng", "ENM", "en-MY"         , "en-MY" };
            yield return new object[] {  0x440a, new string[] { "es-sv" }                , "es-SV"         , "spa", "ESE", "es-SV"         , "es-SV" };
            yield return new object[] {  0x4809, new string[] { "en-sg" }                , "en-SG"         , "eng", "ENE", "en-SG"         , "en-SG" };
            yield return new object[] {  0x480a, new string[] { "es-hn" }                , "es-HN"         , "spa", "ESH", "es-HN"         , "es-HN" };
            yield return new object[] {  0x4c0a, new string[] { "es-ni" }                , "es-NI"         , "spa", "ESI", "es-NI"         , "es-NI" };
            yield return new object[] {  0x500a, new string[] { "es-pr" }                , "es-PR"         , "spa", "ESU", "es-PR"         , "es-PR" };
            yield return new object[] {  0x540a, new string[] { "es-us" }                , "es-US"         , "spa", "EST", "es-US"         , "es-US" };
            yield return new object[] {  0x580a, new string[] { "es-419" }               , "es-419"        , "spa", "ESJ", "es-419"        , "en-US" };
            yield return new object[] {  0x5c0a, new string[] { "es-cu" }                , "es-CU"         , "spa", "ESK", "es-CU"         , "en-US" };
            yield return new object[] {  0x641a, new string[] { "bs-cyrl" }              , "bs-Cyrl-BA"    , "bos", "BSC", "bs-Cyrl"       , "bs-Cyrl-BA" };
            yield return new object[] {  0x681a, new string[] { "bs-latn" }              , "bs-Latn-BA"    , "bos", "BSB", "bs-Latn"       , "bs-Latn-BA" };
            yield return new object[] {  0x6c1a, new string[] { "sr-cyrl" }              , "sr-Cyrl-RS"    , "srp", "SRO", "sr-Cyrl"       , "sr-Cyrl-RS" };
            yield return new object[] {  0x701a, new string[] { "sr-latn" }              , "sr-Latn-RS"    , "srp", "SRM", "sr-Latn"       , "sr-Latn-RS" };
            yield return new object[] {  0x703b, new string[] { "smn" }                  , "smn-FI"        , "smn", "SMN", "smn"           , "smn-FI" };
            yield return new object[] {  0x742c, new string[] { "az-cyrl" }              , "az-Cyrl-AZ"    , "aze", "AZC", "az-Cyrl"       , "az-Cyrl-AZ" };
            yield return new object[] {  0x743b, new string[] { "sms" }                  , "sms-FI"        , "sms", "SMS", "sms"           , "sms-FI" };
            yield return new object[] {  0x7804, new string[] { "zh" }                   , "zh-CN"         , "zho", "CHS", "zh"            , "zh-CN" };
            yield return new object[] {  0x7814, new string[] { "nn" }                   , "nn-NO"         , "nno", "NON", "nn"            , "nn-NO" };
            yield return new object[] {  0x781a, new string[] { "bs" }                   , "bs-Latn-BA"    , "bos", "BSB", "bs"            , "bs-Latn-BA" };
            yield return new object[] {  0x782c, new string[] { "az-latn" }              , "az-Latn-AZ"    , "aze", "AZE", "az-Latn"       , "az-Latn-AZ" };
            yield return new object[] {  0x783b, new string[] { "sma" }                  , "sma-SE"        , "sma", "SMB", "sma"           , "sma-SE" };
            yield return new object[] {  0x7843, new string[] { "uz-cyrl" }              , "uz-Cyrl-UZ"    , "uzb", "UZC", "uz-Cyrl"       , "uz-Cyrl-UZ" };
            yield return new object[] {  0x7850, new string[] { "mn-cyrl" }              , "mn-MN"         , "mon", "MNN", "mn-Cyrl"       , "mn-MN" };
            yield return new object[] {  0x785d, new string[] { "iu-cans" }              , "iu-Cans-CA"    , "iku", "IUS", "iu-Cans"       , "en-US" };
            yield return new object[] {  0x785f, new string[] { "tzm-tfng" }             , "tzm-Tfng-MA"   , "tzm", "TZM", "tzm-Tfng"      , "fr-FR" };
            yield return new object[] {  0x7c04, new string[] { "zh-cht", "zh-hant" }    , "zh-HK"         , "zho", "ZHH", "zh-Hant"       , "zh-HK" };
            yield return new object[] {  0x7c14, new string[] { "nb" }                   , "nb-NO"         , "nob", "NOR", "nb"            , "nb-NO" };
            yield return new object[] {  0x7c1a, new string[] { "sr" }                   , "sr-Latn-RS"    , "srp", "SRM", "sr"            , "sr-Latn-RS" };
            yield return new object[] {  0x7c28, new string[] { "tg-cyrl" }              , "tg-Cyrl-TJ"    , "tgk", "TAJ", "tg-Cyrl"       , "tg-Cyrl-TJ" };
            yield return new object[] {  0x7c2e, new string[] { "dsb" }                  , "dsb-DE"        , "dsb", "DSB", "dsb"           , "dsb-DE" };
            yield return new object[] {  0x7c3b, new string[] { "smj" }                  , "smj-SE"        , "smj", "SMK", "smj"           , "smj-SE" };
            yield return new object[] {  0x7c43, new string[] { "uz-latn" }              , "uz-Latn-UZ"    , "uzb", "UZB", "uz-Latn"       , "uz-Latn-UZ" };
            yield return new object[] {  0x7c46, new string[] { "pa-arab" }              , "pa-Arab-PK"    , "pan", "PAP", "pa-Arab"       , "en-US" };
            yield return new object[] {  0x7c50, new string[] { "mn-mong" }              , "mn-Mong-CN"    , "mon", "MNG", "mn-Mong"       , "mn-Mong-CN" };
            yield return new object[] {  0x7c59, new string[] { "sd-arab" }              , "sd-Arab-PK"    , "snd", "SIP", "sd-Arab"       , "en-US" };
            yield return new object[] {  0x7c5c, new string[] { "chr-cher" }             , "chr-Cher-US"   , "chr", "CRE", "chr-Cher"      , "en-US" };
            yield return new object[] {  0x7c5d, new string[] { "iu-latn" }              , "iu-Latn-CA"    , "iku", "IUK", "iu-Latn"       , "iu-Latn-CA" };
            yield return new object[] {  0x7c5f, new string[] { "tzm-latn" }             , "tzm-Latn-DZ"   , "tzm", "TZA", "tzm-Latn"      , "tzm-Latn-DZ" };
            yield return new object[] {  0x7c67, new string[] { "ff-latn" }              , "ff-Latn-SN"    , "ful", "FUL", "ff-Latn"       , "ff-Latn-SN" };
            yield return new object[] {  0x7c68, new string[] { "ha-latn" }              , "ha-Latn-NG"    , "hau", "HAU", "ha-Latn"       , "ha-Latn-NG" };
            yield return new object[] {  0x7c86, new string[] { "quc-latn" }             , "quc-Latn-GT"   , "quc", "QUT", "quc-Latn"      , "quc-Latn-GT" };
            yield return new object[] {  0x7c92, new string[] { "ku-arab" }              , "ku-Arab-IQ"    , "kur", "KUR", "ku-Arab"       , "en-US" };
            yield return new object[] { 0x1007f, new string[] { "x-iv_mathan", "x-iv" }  , ""              , ""   , "IVL", "x-IV"          , "" };
            yield return new object[] { 0x10407, new string[] { "de-de_phoneb", "de-de" }, "de-DE"         , "deu", "DEU", "de-DE"         , "de-DE" };
            yield return new object[] { 0x1040e, new string[] { "hu-hu_technl", "hu-hu" }, "hu-HU"         , "hun", "HUN", "hu-HU"         , "hu-HU" };
            yield return new object[] { 0x10437, new string[] { "ka-ge_modern", "ka-ge" }, "ka-GE"         , "kat", "KAT", "ka-GE"         , "ka-GE" };
            yield return new object[] { 0x20804, new string[] { "zh-cn_stroke", "zh-cn" }, "zh-CN"         , "zho", "CHS", "zh-Hans-CN"    , "zh-CN" };
            yield return new object[] { 0x21004, new string[] { "zh-sg_stroke", "zh-sg" }, "zh-SG"         , "zho", "ZHI", "zh-Hans-SG"    , "zh-SG" };
            yield return new object[] { 0x21404, new string[] { "zh-mo_stroke", "zh-mo" }, "zh-MO"         , "zho", "ZHM", "zh-Hant-MO"    , "zh-MO" };
            yield return new object[] { 0x30404, new string[] { "zh-tw_pronun", "zh-tw" }, "zh-TW"         , "zho", "CHT", "zh-Hant-TW"    , "zh-TW" };
            yield return new object[] { 0x40404, new string[] { "zh-tw_radstr", "zh-tw" }, "zh-TW"         , "zho", "CHT", "zh-Hant-TW"    , "zh-TW" };
            yield return new object[] { 0x40411, new string[] { "ja-jp_radstr", "ja-jp" }, "ja-JP"         , "jpn", "JPN", "ja-JP"         , "ja-JP" };
            yield return new object[] { 0x40c04, new string[] { "zh-hk_radstr", "zh-hk" }, "zh-HK"         , "zho", "ZHH", "zh-Hant-HK"    , "zh-HK" };
            yield return new object[] { 0x41404, new string[] { "zh-mo_radstr", "zh-mo" }, "zh-MO"         , "zho", "ZHM", "zh-Hant-MO"    , "zh-MO" };
            yield return new object[] { 0x50804, new string[] { "zh-cn_phoneb", "zh-cn" }, "zh-CN"         , "zho", "CHS", "zh-Hans-CN"    , "zh-CN" };
            yield return new object[] { 0x51004, new string[] { "zh-sg_phoneb", "zh-sg" }, "zh-SG"         , "zho", "ZHI", "zh-Hans-SG"    , "zh-SG" };
        }                               
                                        
        [Theory]                        
        [MemberData(nameof(CultureInfo_TestData))]
        public void LcidTest(int lcid, string[] cultureNames, string specificCultureName, string threeLetterISOLanguageName, string threeLetterWindowsLanguageName, string alternativeCultureName, string consoleUICultureName)
        {                               
            _ = alternativeCultureName; 

            CultureInfo ci = new CultureInfo(lcid);
            Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(lcid, ci.LCID);
            Assert.True(ci.UseUserOverride, "UseUserOverride for lcid created culture expected to be true");
            Assert.False(ci.IsReadOnly, "IsReadOnly for lcid created culture expected to be false");
            Assert.Equal(threeLetterISOLanguageName, ci.ThreeLetterISOLanguageName);
            Assert.Equal(threeLetterWindowsLanguageName, ci.ThreeLetterWindowsLanguageName);

            ci = new CultureInfo(cultureNames[0]);
            Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(lcid, ci.LCID);
            Assert.True(ci.UseUserOverride, "UseUserOverride for named created culture expected to be true");
            Assert.False(ci.IsReadOnly, "IsReadOnly for named created culture expected to be false");

            ci = new CultureInfo(lcid, false);
            Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(lcid, ci.LCID);
            Assert.False(ci.UseUserOverride, "UseUserOverride with false user override culture expected to be false");
            Assert.False(ci.IsReadOnly, "IsReadOnly with false user override culture expected to be false");

            ci = CultureInfo.GetCultureInfo(lcid);
            Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(lcid, ci.LCID);
            Assert.False(ci.UseUserOverride, "UseUserOverride with Culture created by GetCultureInfo and lcid expected to be false");
            Assert.True(ci.IsReadOnly, "IsReadOnly with Culture created by GetCultureInfo and lcid expected to be true");

            ci = CultureInfo.GetCultureInfo(cultureNames[0]);
            Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(lcid, ci.LCID);
            Assert.False(ci.UseUserOverride, "UseUserOverride with Culture created by GetCultureInfo and name expected to be false");
            Assert.True(ci.IsReadOnly, "IsReadOnly with Culture created by GetCultureInfo and name expected to be true");

            ci = CultureInfo.GetCultureInfo(cultureNames[0], "");
            Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(lcid, ci.LCID);
            Assert.False(ci.UseUserOverride, "UseUserOverride with Culture created by GetCultureInfo and sort name expected to be false");
            Assert.True(ci.IsReadOnly, "IsReadOnly with Culture created by GetCultureInfo and sort name expected to be true");
            Assert.Equal(CultureInfo.InvariantCulture.TextInfo, ci.TextInfo);
            Assert.Equal(CultureInfo.InvariantCulture.CompareInfo, ci.CompareInfo);

            ci = CultureInfo.CreateSpecificCulture(cultureNames[0]);
            Assert.Equal(specificCultureName, ci.Name);

            ci = CultureInfo.GetCultureInfoByIetfLanguageTag(cultureNames[0]);
            Assert.Contains(ci.Name, cultureNames, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(ci.Name, ci.IetfLanguageTag);
            Assert.Equal(lcid, ci.KeyboardLayoutId);

            Assert.Equal(consoleUICultureName, ci.GetConsoleFallbackUICulture().Name);
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
        public void GetCulturesTest(int lcid, string[] cultureNames, string specificCultureName, string threeLetterISOLanguageName, string threeLetterWindowsLanguageName, string alternativeCultureName, string consoleUICultureName)
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
