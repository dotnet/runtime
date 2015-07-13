// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoAll
    {
        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public void TestAllCultures()
        {
            Assert.True(EnumSystemLocalesEx(EnumLocales, LOCALE_WINDOWS, IntPtr.Zero, IntPtr.Zero), "EnumSystemLocalesEx has failed");

            foreach (CultureInfo ci in cultures)
            {
                Validate(ci);
            }
        }

        private void Validate(CultureInfo ci)
        {
            string val = GetLocaleInfo(ci, LOCALE_SENGLISHDISPLAYNAME);
            Assert.True(ci.EnglishName.Equals(val), String.Format("Expected {0} and got {1}", ci.EnglishName, val));

            // si-LK has some special case when running on Win7 so we just ignore this one
            val = ci.Name.Length == 0 ? "Invariant Language (Invariant Country)" : GetLocaleInfo(ci, LOCALE_SNATIVEDISPLAYNAME);
            Assert.True(ci.Name.Equals("si-LK", StringComparison.OrdinalIgnoreCase) || ci.NativeName.Equals(val), String.Format("Expected {0} and got {1}", val, ci.NativeName));

            // zh-Hans and zh-Hant has different behavior on different platform
            val = GetLocaleInfo(ci, LOCALE_SPARENT);
            Assert.True(val.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase) || val.Equals("zh-Hant", StringComparison.OrdinalIgnoreCase) ||
                          ci.Parent.Name.Equals(val), String.Format("Expected {0} and got {1} as parent of {2}", val, ci.Parent.Name, ci));

            val = GetLocaleInfo(ci, LOCALE_SISO639LANGNAME);
            Assert.True(ci.TwoLetterISOLanguageName.Equals(val), String.Format("Expected {0} and got {1}", val, ci.TwoLetterISOLanguageName));

            ValidateDTFI(ci);
            ValidateNFI(ci);
            ValidateRegionInfo(ci);
        }

        private void ValidateDTFI(CultureInfo ci)
        {
            DateTimeFormatInfo dtfi = ci.DateTimeFormat;
            Calendar cal = dtfi.Calendar;
            int calId = GetCalendarId(cal);

            Assert.Equal<string>(GetDayNames(ci, calId, CAL_SABBREVDAYNAME1), dtfi.AbbreviatedDayNames);
            Assert.Equal<string>(GetDayNames(ci, calId, CAL_SDAYNAME1), dtfi.DayNames);
            Assert.Equal<string>(GetMonthNames(ci, calId, CAL_SMONTHNAME1), dtfi.MonthNames);
            Assert.Equal<string>(GetMonthNames(ci, calId, CAL_SABBREVMONTHNAME1), dtfi.AbbreviatedMonthNames);
            Assert.Equal<string>(GetMonthNames(ci, calId, CAL_SMONTHNAME1 | LOCALE_RETURN_GENITIVE_NAMES), dtfi.MonthGenitiveNames);
            Assert.Equal<string>(GetMonthNames(ci, calId, CAL_SABBREVMONTHNAME1 | LOCALE_RETURN_GENITIVE_NAMES), dtfi.AbbreviatedMonthGenitiveNames);
            Assert.Equal<string>(GetDayNames(ci, calId, CAL_SSHORTESTDAYNAME1), dtfi.ShortestDayNames);

            Assert.True(GetLocaleInfo(ci, LOCALE_S1159).Equals(dtfi.AMDesignator, StringComparison.OrdinalIgnoreCase), String.Format("Failed with AMDesignator for culture {0}", ci));
            Assert.True(GetLocaleInfo(ci, LOCALE_S2359).Equals(dtfi.PMDesignator, StringComparison.OrdinalIgnoreCase), String.Format("Failed with PMDesignator for culture {0}", ci));

            Assert.True(GetDefaultcalendar(ci) == calId, String.Format("Default calendar not matching for culture {0}. we got {1} and expected {2}", ci, GetDefaultcalendar(ci), calId));

            int dayOfWeek = ConvertFirstDayOfWeekMonToSun(GetLocaleInfoAsInt(ci, LOCALE_IFIRSTDAYOFWEEK));
            Assert.True(dayOfWeek == (int)dtfi.FirstDayOfWeek, String.Format("FirstDayOfWeek is not matching for culture {0}. we got {1} and expected {2}", ci, dayOfWeek, dtfi.FirstDayOfWeek));
            Assert.True(GetLocaleInfoAsInt(ci, LOCALE_IFIRSTWEEKOFYEAR) == (int)dtfi.CalendarWeekRule, String.Format("CalendarWeekRule is not matching for culture {0}. we got {1} and expected {2}", ci, GetLocaleInfoAsInt(ci, LOCALE_IFIRSTWEEKOFYEAR), dtfi.CalendarWeekRule));
            string monthDay = GetCalendarInfo(ci, calId, CAL_SMONTHDAY, true);
            Assert.True(monthDay == dtfi.MonthDayPattern, String.Format("MonthDayPattern is not matching for culture {0}. we got '{1}' and expected '{2}'", ci, monthDay, dtfi.MonthDayPattern));
            string rfc1123Pattern = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";
            Assert.True(rfc1123Pattern == dtfi.RFC1123Pattern, String.Format("RFC1123Pattern is not matching for culture {0}. we got '{1}' and expected '{2}'", ci, dtfi.RFC1123Pattern, rfc1123Pattern));
            string sortableDateTimePattern = "yyyy'-'MM'-'dd'T'HH':'mm':'ss";
            Assert.True(sortableDateTimePattern == dtfi.SortableDateTimePattern, String.Format("SortableDateTimePattern is not matching for culture {0}. we got '{1}' and expected '{2}'", ci, dtfi.SortableDateTimePattern, sortableDateTimePattern));
            string universalSortableDateTimePattern = "yyyy'-'MM'-'dd HH':'mm':'ss'Z'";
            Assert.True(universalSortableDateTimePattern == dtfi.UniversalSortableDateTimePattern, String.Format("SortableDateTimePattern is not matching for culture {0}. we got '{1}' and expected '{2}'", ci, dtfi.UniversalSortableDateTimePattern, universalSortableDateTimePattern));

            string longDatePattern1 = GetCalendarInfo(ci, calId, CAL_SLONGDATE)[0];
            string longDatePattern2 = ReescapeWin32String(GetLocaleInfo(ci, LOCALE_SLONGDATE));
            string longTimePattern1 = GetTimeFormats(ci, 0)[0];
            string longTimePattern2 = ReescapeWin32String(GetLocaleInfo(ci, LOCALE_STIMEFORMAT));
            string fullDateTimePattern = longDatePattern1 + " " + longTimePattern1;
            string fullDateTimePattern1 = longDatePattern2 + " " + longTimePattern2;
            Assert.True(fullDateTimePattern == dtfi.FullDateTimePattern || fullDateTimePattern1 == dtfi.FullDateTimePattern, String.Format("FullDateTimePattern is not matching for culture {0}. we got '{1}' or '{2}' and expected '{3}'", ci, fullDateTimePattern, fullDateTimePattern1, dtfi.FullDateTimePattern));
            Assert.True(longDatePattern1 == dtfi.LongDatePattern || longDatePattern2 == dtfi.LongDatePattern, String.Format("LongDatePattern is not matching for culture {0}. we got '{1}' or '{2}' and expected '{3}'", ci, longDatePattern1, longDatePattern2, dtfi.LongDatePattern));
            Assert.True(longTimePattern1 == dtfi.LongTimePattern || longTimePattern2 == dtfi.LongTimePattern, String.Format("LongTimePattern is not matching for culture {0}. we got '{1}' or '{2}' and expected '{3}'", ci, longTimePattern1, longTimePattern1, dtfi.LongTimePattern));

            string shortTimePattern1 = GetTimeFormats(ci, TIME_NOSECONDS)[0];
            string shortTimePattern2 = ReescapeWin32String(GetLocaleInfo(ci, LOCALE_SSHORTTIME));
            Assert.True(shortTimePattern1 == dtfi.ShortTimePattern || shortTimePattern2 == dtfi.ShortTimePattern, String.Format("ShortTimePattern is not matching for culture {0}. we got '{1}' or '{2}' and expected '{3}'", ci, shortTimePattern1, shortTimePattern1, dtfi.ShortTimePattern));

            string shortDatePattern1 = GetCalendarInfo(ci, calId, CAL_SSHORTDATE)[0];
            string shortDatePattern2 = ReescapeWin32String(GetLocaleInfo(ci, LOCALE_SSHORTDATE));
            Assert.True(shortDatePattern1 == dtfi.ShortDatePattern || shortDatePattern2 == dtfi.ShortDatePattern, String.Format("LongDatePattern is not matching for culture {0}. we got '{1}' or '{2}' and expected '{3}'", ci, shortDatePattern1, shortDatePattern2, dtfi.ShortDatePattern));

            string yearMonthPattern1 = GetCalendarInfo(ci, calId, CAL_SYEARMONTH)[0];
            string yearMonthPattern2 = ReescapeWin32String(GetLocaleInfo(ci, LOCALE_SYEARMONTH));
            Assert.True(yearMonthPattern1 == dtfi.YearMonthPattern || yearMonthPattern2 == dtfi.YearMonthPattern, String.Format("YearMonthPattern is not matching for culture {0}. we got '{1}' or '{2}' and expected '{3}'", ci, yearMonthPattern1, yearMonthPattern2, dtfi.YearMonthPattern));

            string[] eraNames = GetCalendarInfo(ci, calId, CAL_SERASTRING);
            Assert.True(eraNames[0].Equals(dtfi.GetEraName(1), StringComparison.OrdinalIgnoreCase), String.Format("Era 1 Name with culture {0} and calendar {1} is wrong. got {2} and expected {3}", ci, cal, dtfi.GetEraName(1), eraNames[0]));

            if (cal is JapaneseCalendar)
            {
                Assert.True(eraNames[1].Equals(dtfi.GetEraName(2), StringComparison.OrdinalIgnoreCase), String.Format("Era 2 Name with culture {0} and calendar {1} is wrong. got {2} and expected {3}", ci, cal, dtfi.GetEraName(2), eraNames[1]));
                Assert.True(eraNames[2].Equals(dtfi.GetEraName(3), StringComparison.OrdinalIgnoreCase), String.Format("Era 3 Name with culture {0} and calendar {1} is wrong. got {2} and expected {3}", ci, cal, dtfi.GetEraName(3), eraNames[2]));
                Assert.True(eraNames[3].Equals(dtfi.GetEraName(4), StringComparison.OrdinalIgnoreCase), String.Format("Era 4 Name with culture {0} and calendar {1} is wrong. got {2} and expected {3}", ci, cal, dtfi.GetEraName(4), eraNames[3]));
            }

            string[] abbreviatedEraNames = GetCalendarInfo(ci, calId, CAL_SABBREVERASTRING);
            Assert.True(abbreviatedEraNames[0].Equals(dtfi.GetAbbreviatedEraName(1), StringComparison.OrdinalIgnoreCase), String.Format("Abbreviated Era 1 Name with culture {0} and calendar {1} is wrong. got {2} and expected {3}", ci, cal, dtfi.GetAbbreviatedEraName(1), abbreviatedEraNames[0]));

            if (cal is JapaneseCalendar)
            {
                Assert.True(abbreviatedEraNames[1].Equals(dtfi.GetAbbreviatedEraName(2), StringComparison.OrdinalIgnoreCase), String.Format("Abbreviated Era 1 Name with culture {0} and calendar {1} is wrong. got {2} and expected {3}", ci, cal, dtfi.GetAbbreviatedEraName(2), abbreviatedEraNames[1]));
                Assert.True(abbreviatedEraNames[2].Equals(dtfi.GetAbbreviatedEraName(3), StringComparison.OrdinalIgnoreCase), String.Format("Abbreviated Era 1 Name with culture {0} and calendar {1} is wrong. got {2} and expected {3}", ci, cal, dtfi.GetAbbreviatedEraName(3), abbreviatedEraNames[2]));
                Assert.True(abbreviatedEraNames[3].Equals(dtfi.GetAbbreviatedEraName(4), StringComparison.OrdinalIgnoreCase), String.Format("Abbreviated Era 1 Name with culture {0} and calendar {1} is wrong. got {2} and expected {3}", ci, cal, dtfi.GetAbbreviatedEraName(4), abbreviatedEraNames[3]));
            }
        }

        private void ValidateNFI(CultureInfo ci)
        {
            NumberFormatInfo nfi = ci.NumberFormat;
            string val = GetLocaleInfo(ci, LOCALE_SPOSITIVESIGN);
            if (String.IsNullOrEmpty(val)) val = "+";
            Assert.True(val.Equals(nfi.PositiveSign), String.Format("Wrong PositiveSign with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.PositiveSign));

            val = GetLocaleInfo(ci, LOCALE_SNEGATIVESIGN);
            Assert.True(val.Equals(nfi.NegativeSign), String.Format("Wrong NegativeSign with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.NegativeSign));

            val = GetLocaleInfo(ci, LOCALE_SDECIMAL);
            Assert.True(val.Equals(nfi.NumberDecimalSeparator), String.Format("Wrong NumberDecimalSeparator with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.NumberDecimalSeparator));
            Assert.True(val.Equals(nfi.PercentDecimalSeparator), String.Format("Wrong PercentDecimalSeparator with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.PercentDecimalSeparator));

            val = GetLocaleInfo(ci, LOCALE_STHOUSAND);
            Assert.True(val.Equals(nfi.NumberGroupSeparator), String.Format("Wrong NumberGroupSeparator with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.NumberGroupSeparator));
            Assert.True(val.Equals(nfi.PercentGroupSeparator), String.Format("Wrong PercentGroupSeparator with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.PercentGroupSeparator));

            val = GetLocaleInfo(ci, LOCALE_SMONTHOUSANDSEP);
            Assert.True(val.Equals(nfi.CurrencyGroupSeparator), String.Format("Wrong CurrencyGroupSeparator with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.CurrencyGroupSeparator));

            val = GetLocaleInfo(ci, LOCALE_SMONDECIMALSEP);
            Assert.True(val.Equals(nfi.CurrencyDecimalSeparator), String.Format("Wrong CurrencyDecimalSeparator with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.CurrencyDecimalSeparator));

            val = GetLocaleInfo(ci, LOCALE_SCURRENCY);
            Assert.True(val.Equals(nfi.CurrencySymbol), String.Format("Wrong CurrencySymbol with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.CurrencySymbol));

            int decValue = GetLocaleInfoAsInt(ci, LOCALE_IDIGITS);
            Assert.True(decValue == nfi.NumberDecimalDigits, String.Format("Wrong NumberDecimalDigits with culturer '{0}'. got '{1}' and expected '{2}' ", ci, decValue, nfi.NumberDecimalDigits));
            Assert.True(decValue == nfi.PercentDecimalDigits, String.Format("Wrong PercentDecimalDigits with culturer '{0}'. got '{1}' and expected '{2}' ", ci, decValue, nfi.PercentDecimalDigits));

            decValue = GetLocaleInfoAsInt(ci, LOCALE_ICURRDIGITS);
            Assert.True(decValue == nfi.CurrencyDecimalDigits, String.Format("Wrong CurrencyDecimalDigits with culturer '{0}'. got '{1}' and expected '{2}' ", ci, decValue, nfi.CurrencyDecimalDigits));

            decValue = GetLocaleInfoAsInt(ci, LOCALE_ICURRENCY);
            Assert.True(decValue == nfi.CurrencyPositivePattern, String.Format("Wrong CurrencyPositivePattern with culturer '{0}'. got '{1}' and expected '{2}' ", ci, decValue, nfi.CurrencyPositivePattern));

            decValue = GetLocaleInfoAsInt(ci, LOCALE_INEGCURR);
            Assert.True(decValue == nfi.CurrencyNegativePattern, String.Format("Wrong CurrencyNegativePattern with culturer '{0}'. got '{1}' and expected '{2}' ", ci, decValue, nfi.CurrencyNegativePattern));

            decValue = GetLocaleInfoAsInt(ci, LOCALE_INEGNUMBER);
            Assert.True(decValue == nfi.NumberNegativePattern, String.Format("Wrong NumberNegativePattern with culturer '{0}'. got '{1}' and expected '{2}' ", ci, decValue, nfi.NumberNegativePattern));

            val = GetLocaleInfo(ci, LOCALE_SMONGROUPING);
            Assert.Equal<int>(ConvertWin32GroupString(val), nfi.CurrencyGroupSizes);

            val = GetLocaleInfo(ci, LOCALE_SNAN);
            Assert.True(val.Equals(nfi.NaNSymbol), String.Format("Wrong NaNSymbol with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.NaNSymbol));

            val = GetLocaleInfo(ci, LOCALE_SNEGINFINITY);
            Assert.True(val.Equals(nfi.NegativeInfinitySymbol), String.Format("Wrong NegativeInfinitySymbol with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.NegativeInfinitySymbol));

            val = GetLocaleInfo(ci, LOCALE_SGROUPING);
            Assert.Equal<int>(ConvertWin32GroupString(val), nfi.NumberGroupSizes);
            Assert.Equal<int>(ConvertWin32GroupString(val), nfi.PercentGroupSizes);

            decValue = GetLocaleInfoAsInt(ci, LOCALE_INEGATIVEPERCENT);
            Assert.True(decValue == nfi.PercentNegativePattern, String.Format("Wrong PercentNegativePattern with culturer '{0}'. got '{1}' and expected '{2}' ", ci, decValue, nfi.PercentNegativePattern));

            decValue = GetLocaleInfoAsInt(ci, LOCALE_IPOSITIVEPERCENT);
            Assert.True(decValue == nfi.PercentPositivePattern, String.Format("Wrong PercentPositivePattern with culturer '{0}'. got '{1}' and expected '{2}' ", ci, decValue, nfi.PercentPositivePattern));

            val = GetLocaleInfo(ci, LOCALE_SPERCENT);
            Assert.True(val.Equals(nfi.PercentSymbol), String.Format("Wrong PercentSymbol with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.PercentSymbol));

            val = GetLocaleInfo(ci, LOCALE_SPERMILLE);
            Assert.True(val.Equals(nfi.PerMilleSymbol), String.Format("Wrong PerMilleSymbol with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.PerMilleSymbol));

            val = GetLocaleInfo(ci, LOCALE_SPOSINFINITY);
            Assert.True(val.Equals(nfi.PositiveInfinitySymbol), String.Format("Wrong PositiveInfinitySymbol with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, nfi.PositiveInfinitySymbol));
        }

        private void ValidateRegionInfo(CultureInfo ci)
        {
            if (ci.Name.Length == 0) // no region for invariant
                return;

            RegionInfo ri = new RegionInfo(ci.Name);
            string val = GetLocaleInfo(ci, LOCALE_SCURRENCY);
            Assert.True(val.Equals(ri.CurrencySymbol), String.Format("Wrong CurrencySymbol with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, ri.CurrencySymbol));

            val = GetLocaleInfo(ci, LOCALE_SENGLISHCOUNTRYNAME);
            Assert.True(val.Equals(ri.EnglishName), String.Format("Wrong EnglishName with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, ri.EnglishName));

            int decValue = GetLocaleInfoAsInt(ci, LOCALE_IMEASURE);
            Assert.True((decValue == 0) == ri.IsMetric, String.Format("Wrong IsMetric with culturer '{0}'. got '{1}' and expected '{2}' ", ci, decValue, ri.IsMetric));

            val = GetLocaleInfo(ci, LOCALE_SINTLSYMBOL);
            Assert.True(val.Equals(ri.ISOCurrencySymbol), String.Format("Wrong ISOCurrencySymbol with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, ri.ISOCurrencySymbol));

            val = GetLocaleInfo(ci, LOCALE_SISO3166CTRYNAME);
            Assert.True(val.Equals(ri.Name, StringComparison.OrdinalIgnoreCase) || ci.Name.Equals(ri.Name, StringComparison.OrdinalIgnoreCase), String.Format("Wrong Name with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, ri.Name));
            Assert.True(val.Equals(ri.TwoLetterISORegionName, StringComparison.OrdinalIgnoreCase), String.Format("Wrong TwoLetterISORegionName with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, ri.TwoLetterISORegionName));

            val = GetLocaleInfo(ci, LOCALE_SNATIVECOUNTRYNAME);
            Assert.True(val.Equals(ri.NativeName, StringComparison.OrdinalIgnoreCase), String.Format("Wrong NativeName with culturer '{0}'. got '{1}' and expected '{2}' ", ci, val, ri.NativeName));
        }

        private int[] ConvertWin32GroupString(String win32Str)
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
            if (!EnumTimeFormatsEx(EnumTimeFormats, ci.Name, flags, IntPtr.Zero))
            {
                Assert.True(false, String.Format("EnumTimeFormatsEx failed with culture {0} and flags {1}", ci, flags));
            }

            return _timePatterns.ToArray();
        }

        internal String ReescapeWin32String(String str)
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
                Assert.True(false, String.Format("Got a calendar {0} which we cannot map its Id", cal));
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
            if (GetLocaleInfoEx(ci.Name, lctype, sb, 400) <= 0)
            {
                Assert.True(false, String.Format("GetLocaleInfoEx failed when calling with lctype {0} and culture {1}", lctype, ci));
            }
            return sb.ToString();
        }

        private string GetCalendarInfo(CultureInfo ci, int calendar, uint calType, bool throwInFail)
        {
            if (GetCalendarInfoEx(ci.Name, calendar, IntPtr.Zero, calType, sb, 400, IntPtr.Zero) <= 0)
            {
                if (throwInFail)
                    Assert.True(false, String.Format("GetCalendarInfoEx failed when calling with caltype {0} and culture {1} and calendar Id {2}", calType, ci, calendar));
                else
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
            if (!EnumCalendarInfoExEx(EnumCalendarsCallback, ci.Name, ENUM_ALL_CALENDARS, null, CAL_ICALINTVALUE, IntPtr.Zero))
            {
                Assert.True(false, "EnumCalendarInfoExEx has been failed.");
            }

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

            if (!EnumCalendarInfoExEx(EnumCalendarInfoCallback, ci.Name, (uint)calId, null, calType, IntPtr.Zero))
            {
                Assert.True(false, "EnumCalendarInfoExEx has been failed in GetCalendarInfo.");
            }

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
            if (GetLocaleInfoEx(ci.Name, lcType | LOCALE_RETURN_NUMBER, ref data, sizeof(int)) <= 0)
                Assert.True(false, String.Format("GetLocaleInfoEx failed with culture {0} and lcType {1}.", ci, lcType));

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

        [DllImport("api-ms-win-core-localization-l1-2-0.dll", CharSet = CharSet.Unicode)]
        internal extern static int GetLocaleInfoEx(string lpLocaleName, uint LCType, StringBuilder data, int cchData);

        [DllImport("api-ms-win-core-localization-l1-2-0.dll", CharSet = CharSet.Unicode)]
        internal extern static int GetLocaleInfoEx(string lpLocaleName, uint LCType, ref int data, int cchData);

        [DllImport("api-ms-win-core-localization-l1-2-1.dll", CharSet = CharSet.Unicode)]
        internal extern static bool EnumSystemLocalesEx(EnumLocalesProcEx lpLocaleEnumProcEx, uint dwFlags, IntPtr lParam, IntPtr reserved);

        [DllImport("api-ms-win-core-localization-l1-2-0.dll", CharSet = CharSet.Unicode)]
        internal extern static int GetCalendarInfoEx(string lpLocaleName, int Calendar, IntPtr lpReserved, uint CalType, StringBuilder lpCalData, int cchData, IntPtr lpValue);

        [DllImport("api-ms-win-core-localization-l1-2-1.dll", CharSet = CharSet.Unicode)]
        internal extern static int GetCalendarInfoEx(string lpLocaleName, int Calendar, IntPtr lpReserved, uint CalType, StringBuilder lpCalData, int cchData, ref uint lpValue);

        [DllImport("api-ms-win-core-localization-l2-1-0.dll", CharSet = CharSet.Unicode)]
        internal extern static bool EnumCalendarInfoExEx(EnumCalendarInfoProcExEx pCalInfoEnumProcExEx, string lpLocaleName, uint Calendar, string lpReserved, uint CalType, IntPtr lParam);

        [DllImport("api-ms-win-core-localization-l2-1-0.dll", CharSet = CharSet.Unicode)]
        internal extern static bool EnumTimeFormatsEx(EnumTimeFormatsProcEx lpTimeFmtEnumProcEx, string lpLocaleName, uint dwFlags, IntPtr lParam);
    }
}