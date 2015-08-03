//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    internal partial class CultureData
    {
        const string LOCALE_NAME_SYSTEM_DEFAULT = @"!x-sys-default-locale";

        //ICU constants
        const int ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY = 100; // max size of keyword or value
        const int ICU_ULOC_FULLNAME_CAPACITY = 157;           // max size of locale name
        const int ICU_U_UNSUPPORTED_ERROR = 16;               // unknown enum value

        /// <summary>
        /// This method uses the sRealName field (which is initialized by the constructor before this is called) to
        /// initialize the rest of the state of CultureData based on the underlying OS globalization library.
        /// </summary>
        private unsafe bool InitCultureData()
        {
            Contract.Assert(this.sRealName != null);

            int result = 0;
            string realNameBuffer;
            if (this.sRealName == LOCALE_NAME_SYSTEM_DEFAULT)
            {
                realNameBuffer = null; //ICU uses null to obtain the default (system) locale
            }
            else
            {
                realNameBuffer = this.sRealName;
            }

            StringBuilder sb = StringBuilderCache.Acquire(ICU_ULOC_FULLNAME_CAPACITY);
            result = Interop.GlobalizationInterop.GetLocaleName(realNameBuffer, sb, sb.Capacity);

            if (result > 0)
            {
                return false; // fail
            }

            // Success, so use the locale name returned
            this.sRealName = sb.ToString();
            realNameBuffer = this.sRealName;

            this.sWindowsName = realNameBuffer;
            this.sName = this.sWindowsName;

            this.sSpecificCulture = this.sWindowsName; // we don't attempt to find a non-neutral locale if a neutral is passed in (unlike win32)

            this.iLanguage = this.ILANGUAGE;
            this.bNeutral = this.SISO3166CTRYNAME.Length == 0;


            return true;
        }
 
        private string GetLocaleInfo(LocaleStringData type)
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.GetLocaleInfo] Expected this.sWindowsName to be populated by already");
            return GetLocaleInfo(this.sWindowsName, type);
        }

        // For LOCALE_SPARENT we need the option of using the "real" name (forcing neutral names) instead of the
        // "windows" name, which can be specific for downlevel (< windows 7) os's.
        private string GetLocaleInfo(string localeName, LocaleStringData type)
        {
            Contract.Assert(localeName != null, "[CultureData.GetLocaleInfo] Expected localeName to be not be null");

            switch (type)
            {
                case LocaleStringData.NegativeInfinitySymbol:
                    // not an equivalent in ICU; should we remove support for this property?
                    return string.Format("{0}{1}",
                        GetLocaleInfo(localeName, LocaleStringData.NegativeSign),
                        GetLocaleInfo(localeName, LocaleStringData.PositiveInfinitySymbol));

                case LocaleStringData.ParentName:
                    // TODO: implement
                    return "";
            }

            StringBuilder sb = StringBuilderCache.Acquire(ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY);

            int result = Interop.GlobalizationInterop.GetLocaleInfoString(localeName, (uint)type, sb, sb.Capacity);
            if (result > 0)
            {
                if (result == ICU_U_UNSUPPORTED_ERROR)
                {
                    Contract.Assert(false, "[CultureData.GetLocaleInfo(LocaleStringData)] Unmatched case");
                    throw new NotImplementedException();
                }

                // Failed, just use empty string
                return String.Empty;
            }
            return sb.ToString();
        }

        private int GetLocaleInfo(LocaleNumberData type)
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.GetLocaleInfo(LocaleNumberData)] Expected this.sWindowsName to be populated by already");

            int value = 0;
            int result = Interop.GlobalizationInterop.GetLocaleInfoInt(this.sWindowsName, (uint)type, ref value);
            if (result > 0)
            {
                if (result == ICU_U_UNSUPPORTED_ERROR)
                {
                    Contract.Assert(false, "[CultureData.GetLocaleInfo(LocaleNumberData)] Unmatched case");
                    throw new NotImplementedException();
                }
                // Failed, just use 0
            }
            return value;
        }

        private int[] GetLocaleInfo(LocaleGroupingData type)
        {
            // TODO: Implement this fully.
            switch (type)
            {
                case LocaleGroupingData.Digit:
                    return new int[] { 3 };
                case LocaleGroupingData.Monetary:
                    return new int[] { 3 };
                default:
                    Contract.Assert(false, "Unmatched case in GetLocaleInfo(LocaleGroupingData)");
                    throw new NotImplementedException();
            }
        }

        private string GetTimeFormatString()
        {
            // TODO: Implement this fully.
            return "HH:mm:ss";
        }

        private int GetFirstDayOfWeek()
        {
            // TODO: Implement this fully.
            return 0;
        }

        private String[] GetTimeFormats()
        {
            // TODO: Implement this fully.
            return new string[] { "HH:mm:ss" };
        }

        private String[] GetShortTimeFormats()
        {
            // TODO: Implement this fully.
            return new string[] { "HH:mm", "hh:mm tt", "H:mm", "h:mm tt" };
        }

        // Enumerate all system cultures and then try to find out which culture has 
        // region name match the requested region name
        private static CultureData GetCultureDataFromRegionName(String regionName)
        {
            // TODO: Implement this fully.
            if (regionName == "")
            {
                return CultureInfo.InvariantCulture.m_cultureData;
            }

            throw new NotImplementedException();
        }

        private static string GetLanguageDisplayName(string cultureName)
        {
            return new CultureInfo(cultureName).m_cultureData.GetLocaleInfo(cultureName, LocaleStringData.LocalizedDisplayName);
        }

        private static string GetRegionDisplayName(string isoCountryCode)
        {
            // TODO: Implement this fully.
            return "";
        }

        private static CultureInfo GetUserDefaultCulture()
        {
            return new CultureInfo(LOCALE_NAME_SYSTEM_DEFAULT);
        }

        private static bool IsCustomCultureId(int cultureId)
        {
            // TODO: Implement this fully.
            return false;
        }
    }
}
