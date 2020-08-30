// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Globalization
{
    internal partial class CultureData
    {
        // ICU constants
        private const int ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY = 100; // max size of keyword or value
        private const int ICU_ULOC_FULLNAME_CAPACITY = 157;           // max size of locale name

        internal static unsafe bool GetLocaleName(string localeName, out string? windowsName)
        {
            // Get the locale name from ICU
            char* buffer = stackalloc char[ICU_ULOC_FULLNAME_CAPACITY];
            if (!Interop.Globalization.GetLocaleName(localeName, buffer, ICU_ULOC_FULLNAME_CAPACITY))
            {
                windowsName = null;
                return false; // fail
            }

            // Success - use the locale name returned which may be different than realNameBuffer (casing)
            windowsName = new string(buffer); // the name passed to subsequent ICU calls
            return true;
        }

        internal static unsafe bool GetDefaultLocaleName([NotNullWhen(true)] out string? windowsName)
        {
            // Get the default (system) locale name from ICU
            char* buffer = stackalloc char[ICU_ULOC_FULLNAME_CAPACITY];
            if (!Interop.Globalization.GetDefaultLocaleName(buffer, ICU_ULOC_FULLNAME_CAPACITY))
            {
                windowsName = null;
                return false; // fail
            }

            // Success - use the locale name returned which may be different than realNameBuffer (casing)
            windowsName = new string(buffer); // the name passed to subsequent ICU calls
            return true;
        }

        private string IcuGetLocaleInfo(LocaleStringData type)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            Debug.Assert(_sWindowsName != null, "[CultureData.IcuGetLocaleInfo] Expected _sWindowsName to be populated already");
            return IcuGetLocaleInfo(_sWindowsName, type);
        }

        // For LOCALE_SPARENT we need the option of using the "real" name (forcing neutral names) instead of the
        // "windows" name, which can be specific for downlevel (< windows 7) os's.
        private unsafe string IcuGetLocaleInfo(string localeName, LocaleStringData type)
        {
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(localeName != null, "[CultureData.IcuGetLocaleInfo] Expected localeName to be not be null");

            switch (type)
            {
                case LocaleStringData.NegativeInfinitySymbol:
                    // not an equivalent in ICU; prefix the PositiveInfinitySymbol with NegativeSign
                    return IcuGetLocaleInfo(localeName, LocaleStringData.NegativeSign) +
                        IcuGetLocaleInfo(localeName, LocaleStringData.PositiveInfinitySymbol);
            }

            char* buffer = stackalloc char[ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY];
            bool result = Interop.Globalization.GetLocaleInfoString(localeName, (uint)type, buffer, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY);
            if (!result)
            {
                // Failed, just use empty string
                Debug.Fail("[CultureData.IcuGetLocaleInfo(LocaleStringData)] Failed");
                return string.Empty;
            }

            return new string(buffer);
        }

        private int IcuGetLocaleInfo(LocaleNumberData type)
        {
            Debug.Assert(!GlobalizationMode.UseNls);

            Debug.Assert(_sWindowsName != null, "[CultureData.IcuGetLocaleInfo(LocaleNumberData)] Expected _sWindowsName to be populated already");

            switch (type)
            {
                case LocaleNumberData.CalendarType:
                    // returning 0 will cause the first supported calendar to be returned, which is the preferred calendar
                    return 0;
            }


            int value = 0;
            bool result = Interop.Globalization.GetLocaleInfoInt(_sWindowsName, (uint)type, ref value);
            if (!result)
            {
                // Failed, just use 0
                Debug.Fail("[CultureData.IcuGetLocaleInfo(LocaleNumberData)] failed");
            }

            return value;
        }

        private int[] IcuGetLocaleInfo(LocaleGroupingData type)
        {
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(_sWindowsName != null, "[CultureData.IcuGetLocaleInfo(LocaleGroupingData)] Expected _sWindowsName to be populated already");

            int primaryGroupingSize = 0;
            int secondaryGroupingSize = 0;
            bool result = Interop.Globalization.GetLocaleInfoGroupingSizes(_sWindowsName, (uint)type, ref primaryGroupingSize, ref secondaryGroupingSize);
            if (!result)
            {
                Debug.Fail("[CultureData.IcuGetLocaleInfo(LocaleGroupingData type)] failed");
            }

            if (secondaryGroupingSize == 0)
            {
                return new int[] { primaryGroupingSize };
            }

            return new int[] { primaryGroupingSize, secondaryGroupingSize };
        }

        private string IcuGetTimeFormatString() => IcuGetTimeFormatString(shortFormat: false);

        private unsafe string IcuGetTimeFormatString(bool shortFormat)
        {
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(_sWindowsName != null, "[CultureData.GetTimeFormatString(bool shortFormat)] Expected _sWindowsName to be populated already");

            char* buffer = stackalloc char[ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY];

            bool result = Interop.Globalization.GetLocaleTimeFormat(_sWindowsName, shortFormat, buffer, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY);
            if (!result)
            {
                // Failed, just use empty string
                Debug.Fail("[CultureData.GetTimeFormatString(bool shortFormat)] Failed");
                return string.Empty;
            }

            var span = new ReadOnlySpan<char>(buffer, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY);
            return ConvertIcuTimeFormatString(span.Slice(0, span.IndexOf('\0')));
        }

        private static CultureData? IcuGetCultureDataFromRegionName(string? regionName)
        {
            // no support to lookup by region name, other than the hard-coded list in CultureData
            return null;
        }

        private static string IcuGetLanguageDisplayName(string cultureName)
        {
            return new CultureInfo(cultureName)._cultureData.IcuGetLocaleInfo(cultureName, LocaleStringData.LocalizedDisplayName);
        }

        private static string? IcuGetRegionDisplayName()
        {
            // use the fallback which is to return NativeName
            return null;
        }

        private static string ConvertIcuTimeFormatString(ReadOnlySpan<char> icuFormatString)
        {
            Debug.Assert(icuFormatString.Length < ICU_ULOC_FULLNAME_CAPACITY);
            Span<char> result = stackalloc char[ICU_ULOC_FULLNAME_CAPACITY];

            bool amPmAdded = false;
            int resultPos = 0;

            for (int i = 0; i < icuFormatString.Length; i++)
            {
                switch (icuFormatString[i])
                {
                    case '\'':
                        result[resultPos++] = icuFormatString[i++];
                        while (i < icuFormatString.Length)
                        {
                            char current = icuFormatString[i];
                            result[resultPos++] = current;
                            if (current == '\'')
                            {
                                break;
                            }
                            i++;
                        }
                        break;

                    case ':':
                    case '.':
                    case 'H':
                    case 'h':
                    case 'm':
                    case 's':
                        result[resultPos++] = icuFormatString[i];
                        break;

                    case ' ':
                    case '\u00A0':
                        // Convert nonbreaking spaces into regular spaces
                        result[resultPos++] = ' ';
                        break;

                    case 'a': // AM/PM
                        if (!amPmAdded)
                        {
                            amPmAdded = true;
                            result[resultPos++] = 't';
                            result[resultPos++] = 't';
                        }
                        break;

                }
            }

            return result.Slice(0, resultPos).ToString();
        }

        private static int IcuLocaleNameToLCID(string cultureName)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            int lcid = IcuLocaleData.GetLocaleDataNumericPart(cultureName, IcuLocaleDataParts.Lcid);
            return lcid == -1 ? CultureInfo.LOCALE_CUSTOM_UNSPECIFIED : lcid;
        }

        private static int IcuGetGeoId(string cultureName)
        {
            Debug.Assert(!GlobalizationMode.UseNls);
            int geoId = IcuLocaleData.GetLocaleDataNumericPart(cultureName, IcuLocaleDataParts.GeoId);
            return geoId == -1 ? CultureData.Invariant.GeoId : geoId;
        }

        private static int IcuGetDigitSubstitution(string cultureName)
        {
            Debug.Assert(!GlobalizationMode.UseNls);
            int digitSubstitution = IcuLocaleData.GetLocaleDataNumericPart(cultureName, IcuLocaleDataParts.DigitSubstitution);
            return digitSubstitution == -1 ? (int) DigitShapes.None : digitSubstitution;
        }

        private static string IcuGetThreeLetterWindowsLanguageName(string cultureName)
        {
            Debug.Assert(!GlobalizationMode.UseNls);
            return IcuLocaleData.GetThreeLetterWindowsLanguageName(cultureName) ?? "ZZZ" /* default lang name */;
        }

        private static CultureInfo[] IcuEnumCultures(CultureTypes types)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            if ((types & (CultureTypes.NeutralCultures | CultureTypes.SpecificCultures)) == 0)
            {
                return Array.Empty<CultureInfo>();
            }

            int bufferLength = Interop.Globalization.GetLocales(null, 0);
            if (bufferLength <= 0)
            {
                return Array.Empty<CultureInfo>();
            }

            char [] chars = new char[bufferLength];

            bufferLength = Interop.Globalization.GetLocales(chars, bufferLength);
            if (bufferLength <= 0)
            {
                return Array.Empty<CultureInfo>();
            }

            bool enumNeutrals   = (types & CultureTypes.NeutralCultures) != 0;
            bool enumSpecificss = (types & CultureTypes.SpecificCultures) != 0;

            List<CultureInfo> list = new List<CultureInfo>();
            if (enumNeutrals)
            {
                list.Add(CultureInfo.InvariantCulture);
            }

            int index = 0;
            while (index < bufferLength)
            {
                int length = (int) chars[index++];
                if (index + length <= bufferLength)
                {
                    CultureInfo ci = CultureInfo.GetCultureInfo(new string(chars, index, length));
                    if ((enumNeutrals && ci.IsNeutralCulture) || (enumSpecificss && !ci.IsNeutralCulture))
                    {
                        list.Add(ci);
                    }
                }

                index += length;
            }

            return list.ToArray();
        }

        private static string IcuGetConsoleFallbackName(string cultureName)
        {
            Debug.Assert(!GlobalizationMode.UseNls);
            return IcuLocaleData.GetConsoleUICulture(cultureName);
        }
    }
}
