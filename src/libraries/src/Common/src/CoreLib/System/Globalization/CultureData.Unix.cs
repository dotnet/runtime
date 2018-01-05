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
        const int ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY = 100; // max size of keyword or value
        const int ICU_ULOC_FULLNAME_CAPACITY = 157;           // max size of locale name
        const string ICU_COLLATION_KEYWORD = "@collation=";
        
        
        /// <summary>
        /// This method uses the sRealName field (which is initialized by the constructor before this is called) to
        /// initialize the rest of the state of CultureData based on the underlying OS globalization library.
        /// </summary>
        private unsafe bool InitCultureData()
        {
            Debug.Assert(_sRealName != null);

            Debug.Assert(!GlobalizationMode.Invariant);

            string alternateSortName = string.Empty;
            string realNameBuffer = _sRealName;

            // Basic validation
            if (realNameBuffer.Contains('@'))
            {
                return false; // don't allow ICU variants to come in directly
            }

            // Replace _ (alternate sort) with @collation= for ICU
            int index = realNameBuffer.IndexOf('_');
            if (index > 0)
            {
                if (index >= (realNameBuffer.Length - 1) // must have characters after _
                    || realNameBuffer.Substring(index + 1).Contains('_')) // only one _ allowed
                {
                    return false; // fail
                }
                alternateSortName = realNameBuffer.Substring(index + 1);
                realNameBuffer = realNameBuffer.Substring(0, index) + ICU_COLLATION_KEYWORD + alternateSortName;
            }

            // Get the locale name from ICU
            if (!GetLocaleName(realNameBuffer, out _sWindowsName))
            {
                return false; // fail
            }

            // Replace the ICU collation keyword with an _
            index = _sWindowsName.IndexOf(ICU_COLLATION_KEYWORD, StringComparison.Ordinal);
            if (index >= 0)
            {
                _sName = _sWindowsName.Substring(0, index) + "_" + alternateSortName;
            }
            else
            {
                _sName = _sWindowsName;
            }
            _sRealName = _sName;

            _iLanguage = this.ILANGUAGE;
            if (_iLanguage == 0)
            {
                _iLanguage = CultureInfo.LOCALE_CUSTOM_UNSPECIFIED;
            }

            _bNeutral = (this.SISO3166CTRYNAME.Length == 0);
            
            _sSpecificCulture = _bNeutral ? LocaleData.GetSpecificCultureName(_sRealName) : _sRealName;   
            
            // Remove the sort from sName unless custom culture
            if (index>0 && !_bNeutral && !IsCustomCultureId(_iLanguage))
            {
                _sName = _sWindowsName.Substring(0, index);
            }
            return true;
        }

        internal static bool GetLocaleName(string localeName, out string windowsName)
        {
            // Get the locale name from ICU
            StringBuilder sb = StringBuilderCache.Acquire(ICU_ULOC_FULLNAME_CAPACITY);
            if (!Interop.GlobalizationInterop.GetLocaleName(localeName, sb, sb.Capacity))
            {
                StringBuilderCache.Release(sb);
                windowsName = null;
                return false; // fail
            }

            // Success - use the locale name returned which may be different than realNameBuffer (casing)
            windowsName = StringBuilderCache.GetStringAndRelease(sb); // the name passed to subsequent ICU calls
            return true;
        }

        internal static bool GetDefaultLocaleName(out string windowsName)
        {
            // Get the default (system) locale name from ICU
            StringBuilder sb = StringBuilderCache.Acquire(ICU_ULOC_FULLNAME_CAPACITY);
            if (!Interop.GlobalizationInterop.GetDefaultLocaleName(sb, sb.Capacity))
            {
                StringBuilderCache.Release(sb);
                windowsName = null;
                return false; // fail
            }

            // Success - use the locale name returned which may be different than realNameBuffer (casing)
            windowsName = StringBuilderCache.GetStringAndRelease(sb); // the name passed to subsequent ICU calls
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
        private string GetLocaleInfo(string localeName, LocaleStringData type)
        {
            Debug.Assert(localeName != null, "[CultureData.GetLocaleInfo] Expected localeName to be not be null");

            switch (type)
            {
                case LocaleStringData.NegativeInfinitySymbol:
                    // not an equivalent in ICU; prefix the PositiveInfinitySymbol with NegativeSign
                    return GetLocaleInfo(localeName, LocaleStringData.NegativeSign) +
                        GetLocaleInfo(localeName, LocaleStringData.PositiveInfinitySymbol);
            }

            StringBuilder sb = StringBuilderCache.Acquire(ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY);

            bool result = Interop.GlobalizationInterop.GetLocaleInfoString(localeName, (uint)type, sb, sb.Capacity);
            if (!result)
            {
                // Failed, just use empty string
                StringBuilderCache.Release(sb);
                Debug.Fail("[CultureData.GetLocaleInfo(LocaleStringData)] Failed");
                return String.Empty;
            }
            return StringBuilderCache.GetStringAndRelease(sb);
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
            bool result = Interop.GlobalizationInterop.GetLocaleInfoInt(_sWindowsName, (uint)type, ref value);
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
            bool result = Interop.GlobalizationInterop.GetLocaleInfoGroupingSizes(_sWindowsName, (uint)type, ref primaryGroupingSize, ref secondaryGroupingSize);
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

        private string GetTimeFormatString()
        {
            return GetTimeFormatString(false);
        }

        private string GetTimeFormatString(bool shortFormat)
        {
            Debug.Assert(_sWindowsName != null, "[CultureData.GetTimeFormatString(bool shortFormat)] Expected _sWindowsName to be populated already");

            StringBuilder sb = StringBuilderCache.Acquire(ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY);

            bool result = Interop.GlobalizationInterop.GetLocaleTimeFormat(_sWindowsName, shortFormat, sb, sb.Capacity);
            if (!result)
            {
                // Failed, just use empty string
                StringBuilderCache.Release(sb);
                Debug.Fail("[CultureData.GetTimeFormatString(bool shortFormat)] Failed");
                return String.Empty;
            }

            return ConvertIcuTimeFormatString(StringBuilderCache.GetStringAndRelease(sb));
        }

        private int GetFirstDayOfWeek()
        {
            return this.GetLocaleInfo(LocaleNumberData.FirstDayOfWeek);
        }

        private String[] GetTimeFormats()
        {
            string format = GetTimeFormatString(false);
            return new string[] { format };
        }

        private String[] GetShortTimeFormats()
        {
            string format = GetTimeFormatString(true);
            return new string[] { format };
        }

        private static CultureData GetCultureDataFromRegionName(String regionName)
        {
            // no support to lookup by region name, other than the hard-coded list in CultureData
            return null;
        }

        private static string GetLanguageDisplayName(string cultureName)
        {
            return new CultureInfo(cultureName)._cultureData.GetLocaleInfo(cultureName, LocaleStringData.LocalizedDisplayName);
        }

        private static string GetRegionDisplayName(string isoCountryCode)
        {
            // use the fallback which is to return NativeName
            return null;
        }

        private static CultureInfo GetUserDefaultCulture()
        {
            return CultureInfo.GetUserDefaultCulture();
        }

        private static string ConvertIcuTimeFormatString(string icuFormatString)
        {
            StringBuilder sb = StringBuilderCache.Acquire(ICU_ULOC_FULLNAME_CAPACITY);
            bool amPmAdded = false;

            for (int i = 0; i < icuFormatString.Length; i++)
            {
                switch(icuFormatString[i])
                {
                    case ':':
                    case '.':
                    case 'H':
                    case 'h':
                    case 'm':
                    case 's':
                        sb.Append(icuFormatString[i]);
                        break;

                    case ' ':
                    case '\u00A0':
                        // Convert nonbreaking spaces into regular spaces
                        sb.Append(' ');
                        break;

                    case 'a': // AM/PM
                        if (!amPmAdded)
                        {
                            amPmAdded = true;
                            sb.Append("tt");
                        }
                        break;

                }
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }
        
        private static string LCIDToLocaleName(int culture)
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
            return ansiCodePage == -1 ? CultureData.Invariant.IDEFAULTANSICODEPAGE : ansiCodePage; 
        }

        private static int GetOemCodePage(string cultureName)
        {
            int oemCodePage = LocaleData.GetLocaleDataNumericPart(cultureName, LocaleDataParts.OemCodePage);
            return oemCodePage == -1 ? CultureData.Invariant.IDEFAULTOEMCODEPAGE : oemCodePage; 
        }

        private static int GetMacCodePage(string cultureName)
        {
            int macCodePage = LocaleData.GetLocaleDataNumericPart(cultureName, LocaleDataParts.MacCodePage);
            return macCodePage == -1 ? CultureData.Invariant.IDEFAULTMACCODEPAGE : macCodePage; 
        }

        private static int GetEbcdicCodePage(string cultureName)
        {
            int ebcdicCodePage = LocaleData.GetLocaleDataNumericPart(cultureName, LocaleDataParts.EbcdicCodePage);
            return ebcdicCodePage == -1 ? CultureData.Invariant.IDEFAULTEBCDICCODEPAGE : ebcdicCodePage; 
        }

        private static int GetGeoId(string cultureName)
        {
            int geoId = LocaleData.GetLocaleDataNumericPart(cultureName, LocaleDataParts.GeoId);
            return geoId == -1 ? CultureData.Invariant.IGEOID : geoId; 
        }
        
        private static int GetDigitSubstitution(string cultureName)
        {
            int digitSubstitution = LocaleData.GetLocaleDataNumericPart(cultureName, LocaleDataParts.DigitSubstitution);
            return digitSubstitution == -1 ? (int) DigitShapes.None : digitSubstitution; 
        }

        private static string GetThreeLetterWindowsLanguageName(string cultureName)
        {
            string langName = LocaleData.GetThreeLetterWindowsLangageName(cultureName);
            return langName == null ? "ZZZ" /* default lang name */ : langName; 
        }

        private static CultureInfo[] EnumCultures(CultureTypes types)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            
            if ((types & (CultureTypes.NeutralCultures | CultureTypes.SpecificCultures)) == 0)
            {
                return Array.Empty<CultureInfo>();
            }
            
            int bufferLength = Interop.GlobalizationInterop.GetLocales(null, 0);
            if (bufferLength <= 0)
            {
                return Array.Empty<CultureInfo>();
            }
            
            Char [] chars = new Char[bufferLength];
            
            bufferLength = Interop.GlobalizationInterop.GetLocales(chars, bufferLength);
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
                    CultureInfo ci = CultureInfo.GetCultureInfo(new String(chars, index, length));
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
        
        internal bool IsFramework // not applicable on Linux based systems 
        {
            get { return false; }
        }
        
        internal bool IsWin32Installed // not applicable on Linux based systems
        {
            get { return false; }
        }
        
        internal bool IsReplacementCulture // not applicable on Linux based systems
        {
            get { return false; }
        }
    }
}
