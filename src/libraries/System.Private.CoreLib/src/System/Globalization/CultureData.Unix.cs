// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.Globalization
{
    internal partial class CultureData
    {
        // ICU constants
        private const int ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY = 100; // max size of keyword or value
        private const int ICU_ULOC_FULLNAME_CAPACITY = 157;           // max size of locale name
        private const string ICU_COLLATION_KEYWORD = "@collation=";

        /// <summary>
        /// This method uses the sRealName field (which is initialized by the constructor before this is called) to
        /// initialize the rest of the state of CultureData based on the underlying OS globalization library.
        /// </summary>
        private unsafe bool InitCultureData()
        {
            Debug.Assert(_sRealName != null);

            Debug.Assert(!GlobalizationMode.Invariant);

            string realNameBuffer = _sRealName;

            // Basic validation
            if (realNameBuffer.Contains('@'))
            {
                return false; // don't allow ICU variants to come in directly
            }

            // Replace _ (alternate sort) with @collation= for ICU
            ReadOnlySpan<char> alternateSortName = default;
            int index = realNameBuffer.IndexOf('_');
            if (index > 0)
            {
                if (index >= (realNameBuffer.Length - 1) // must have characters after _
                    || realNameBuffer.IndexOf('_', index + 1) >= 0) // only one _ allowed
                {
                    return false; // fail
                }
                alternateSortName = realNameBuffer.AsSpan(index + 1);
                realNameBuffer = string.Concat(realNameBuffer.AsSpan(0, index), ICU_COLLATION_KEYWORD, alternateSortName);
            }

            // Get the locale name from ICU
            if (!GetLocaleName(realNameBuffer, out _sWindowsName))
            {
                return false; // fail
            }

            // Replace the ICU collation keyword with an _
            Debug.Assert(_sWindowsName != null);
            index = _sWindowsName.IndexOf(ICU_COLLATION_KEYWORD, StringComparison.Ordinal);
            if (index >= 0)
            {
                _sName = string.Concat(_sWindowsName.AsSpan(0, index), "_", alternateSortName);
            }
            else
            {
                _sName = _sWindowsName;
            }
            _sRealName = _sName;

            _iLanguage = LCID;
            if (_iLanguage == 0)
            {
                _iLanguage = CultureInfo.LOCALE_CUSTOM_UNSPECIFIED;
            }

            _bNeutral = TwoLetterISOCountryName.Length == 0;

            _sSpecificCulture = _bNeutral ? LocaleData.GetSpecificCultureName(_sRealName) : _sRealName;

            // Remove the sort from sName unless custom culture
            if (index > 0 && !_bNeutral && !IsCustomCultureId(_iLanguage))
            {
                _sName = _sWindowsName.Substring(0, index);
            }
            return true;
        }

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

        internal static unsafe bool GetDefaultLocaleName(out string? windowsName)
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

        private string GetLocaleInfo(LocaleStringData type)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(_sWindowsName != null, "[CultureData.GetLocaleInfo] Expected _sWindowsName to be populated already");
            return GetLocaleInfo(_sWindowsName, type);
        }

        // For LOCALE_SPARENT we need the option of using the "real" name (forcing neutral names) instead of the
        // "windows" name, which can be specific for downlevel (< windows 7) os's.
        private unsafe string GetLocaleInfo(string localeName, LocaleStringData type)
        {
            Debug.Assert(localeName != null, "[CultureData.GetLocaleInfo] Expected localeName to be not be null");

            switch (type)
            {
                case LocaleStringData.NegativeInfinitySymbol:
                    // not an equivalent in ICU; prefix the PositiveInfinitySymbol with NegativeSign
                    return GetLocaleInfo(localeName, LocaleStringData.NegativeSign) +
                        GetLocaleInfo(localeName, LocaleStringData.PositiveInfinitySymbol);
            }

            char* buffer = stackalloc char[ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY];
            bool result = Interop.Globalization.GetLocaleInfoString(localeName, (uint)type, buffer, ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY);
            if (!result)
            {
                // Failed, just use empty string
                Debug.Fail("[CultureData.GetLocaleInfo(LocaleStringData)] Failed");
                return string.Empty;
            }

            return new string(buffer);
        }

        private int GetLocaleInfo(LocaleNumberData type)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(_sWindowsName != null, "[CultureData.GetLocaleInfo(LocaleNumberData)] Expected _sWindowsName to be populated already");

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
                Debug.Fail("[CultureData.GetLocaleInfo(LocaleNumberData)] failed");
            }

            return value;
        }

        private int[] GetLocaleInfo(LocaleGroupingData type)
        {
            Debug.Assert(_sWindowsName != null, "[CultureData.GetLocaleInfo(LocaleGroupingData)] Expected _sWindowsName to be populated already");

            int primaryGroupingSize = 0;
            int secondaryGroupingSize = 0;
            bool result = Interop.Globalization.GetLocaleInfoGroupingSizes(_sWindowsName, (uint)type, ref primaryGroupingSize, ref secondaryGroupingSize);
            if (!result)
            {
                Debug.Fail("[CultureData.GetLocaleInfo(LocaleGroupingData type)] failed");
            }

            if (secondaryGroupingSize == 0)
            {
                return new int[] { primaryGroupingSize };
            }

            return new int[] { primaryGroupingSize, secondaryGroupingSize };
        }

        private string GetTimeFormatString() => GetTimeFormatString(shortFormat: false);

        private unsafe string GetTimeFormatString(bool shortFormat)
        {
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

        private int GetFirstDayOfWeek() => GetLocaleInfo(LocaleNumberData.FirstDayOfWeek);

        private string[] GetTimeFormats()
        {
            string format = GetTimeFormatString(false);
            return new string[] { format };
        }

        private string[] GetShortTimeFormats()
        {
            string format = GetTimeFormatString(true);
            return new string[] { format };
        }

        private static CultureData? GetCultureDataFromRegionName(string? regionName)
        {
            // no support to lookup by region name, other than the hard-coded list in CultureData
            return null;
        }

        private static string GetLanguageDisplayName(string cultureName)
        {
            return new CultureInfo(cultureName)._cultureData.GetLocaleInfo(cultureName, LocaleStringData.LocalizedDisplayName);
        }

        private static string? GetRegionDisplayName(string? isoCountryCode)
        {
            // use the fallback which is to return NativeName
            return null;
        }

        private static CultureInfo GetUserDefaultCulture()
        {
            return CultureInfo.GetUserDefaultCulture();
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
                            char current = icuFormatString[i++];
                            result[resultPos++] = current;
                            if (current == '\'')
                            {
                                break;
                            }
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

        private static string? LCIDToLocaleName(int culture)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            return LocaleData.LCIDToLocaleName(culture);
        }

        private static int LocaleNameToLCID(string cultureName)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            int lcid = LocaleData.GetLocaleDataNumericPart(cultureName, LocaleDataParts.Lcid);
            return lcid == -1 ? CultureInfo.LOCALE_CUSTOM_UNSPECIFIED : lcid;
        }

        private static int GetAnsiCodePage(string cultureName)
        {
            int ansiCodePage = LocaleData.GetLocaleDataNumericPart(cultureName, LocaleDataParts.AnsiCodePage);
            return ansiCodePage == -1 ? CultureData.Invariant.ANSICodePage : ansiCodePage;
        }

        private static int GetOemCodePage(string cultureName)
        {
            int oemCodePage = LocaleData.GetLocaleDataNumericPart(cultureName, LocaleDataParts.OemCodePage);
            return oemCodePage == -1 ? CultureData.Invariant.OEMCodePage : oemCodePage;
        }

        private static int GetMacCodePage(string cultureName)
        {
            int macCodePage = LocaleData.GetLocaleDataNumericPart(cultureName, LocaleDataParts.MacCodePage);
            return macCodePage == -1 ? CultureData.Invariant.MacCodePage : macCodePage;
        }

        private static int GetEbcdicCodePage(string cultureName)
        {
            int ebcdicCodePage = LocaleData.GetLocaleDataNumericPart(cultureName, LocaleDataParts.EbcdicCodePage);
            return ebcdicCodePage == -1 ? CultureData.Invariant.EBCDICCodePage : ebcdicCodePage;
        }

        private static int GetGeoId(string cultureName)
        {
            int geoId = LocaleData.GetLocaleDataNumericPart(cultureName, LocaleDataParts.GeoId);
            return geoId == -1 ? CultureData.Invariant.GeoId : geoId;
        }

        private static int GetDigitSubstitution(string cultureName)
        {
            int digitSubstitution = LocaleData.GetLocaleDataNumericPart(cultureName, LocaleDataParts.DigitSubstitution);
            return digitSubstitution == -1 ? (int) DigitShapes.None : digitSubstitution;
        }

        private static string GetThreeLetterWindowsLanguageName(string cultureName)
        {
            return LocaleData.GetThreeLetterWindowsLanguageName(cultureName) ?? "ZZZ" /* default lang name */;
        }

        private static CultureInfo[] EnumCultures(CultureTypes types)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

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

        private static string GetConsoleFallbackName(string cultureName)
        {
            return LocaleData.GetConsoleUICulture(cultureName);
        }

        internal bool IsWin32Installed => false;

        internal bool IsReplacementCulture => false;
    }
}
