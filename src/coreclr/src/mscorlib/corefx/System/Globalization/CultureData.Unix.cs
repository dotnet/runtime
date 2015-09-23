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
        // Win32 constants
        const string LOCALE_NAME_SYSTEM_DEFAULT = @"!x-sys-default-locale";
        
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
            Contract.Assert(this.sRealName != null);

            string alternateSortName = string.Empty;
            string realNameBuffer = null;
            int index;

            bool useSystemDefault = (this.sRealName == LOCALE_NAME_SYSTEM_DEFAULT);
            if (!useSystemDefault) //ICU uses null to obtain the default (system) locale
            {
                realNameBuffer = this.sRealName;

                // Basic validation
                if (realNameBuffer.Contains("@"))
                {
                    return false; // don't allow ICU variants to come in directly
                }

                // Replace _ (alternate sort) with @collation= for ICU
                index = realNameBuffer.IndexOf('_');
                if (index > 0)
                {
                    if (index >= (realNameBuffer.Length - 1) // must have characters after _
                        || realNameBuffer.Substring(index + 1).Contains("_")) // only one _ allowed
                    {
                        return false; // fail
                    }
                    alternateSortName = realNameBuffer.Substring(index + 1);
                    realNameBuffer = realNameBuffer.Substring(0, index) + ICU_COLLATION_KEYWORD + alternateSortName;
                }
            }

            // Get the locale name from ICU
            StringBuilder sb = StringBuilderCache.Acquire(ICU_ULOC_FULLNAME_CAPACITY);
            if (!Interop.GlobalizationInterop.GetLocaleName(realNameBuffer, sb, sb.Capacity))
            {
                StringBuilderCache.Release(sb);
                return false; // fail
            }

            // Success - use the locale name returned which may be different than realNameBuffer (casing)
            this.sWindowsName = StringBuilderCache.GetStringAndRelease(sb); // the name passed to subsequent ICU calls

            // Replace the ICU collation keyword with an _
            index = realNameBuffer.IndexOf(ICU_COLLATION_KEYWORD);
            if (index >= 0)
            {
                this.sName = this.sWindowsName.Substring(0, index) + "_" + alternateSortName;
            }
            else
            {
                this.sName = this.sWindowsName;
            }

            this.sRealName = this.sName;
            this.sSpecificCulture = this.sRealName; // we don't attempt to find a non-neutral locale if a neutral is passed in (unlike win32)

            this.iLanguage = this.ILANGUAGE;
            if (this.iLanguage == 0)
            {
                if (useSystemDefault)
                {
                    this.iLanguage = LOCALE_CUSTOM_DEFAULT;
                }
                else
                {
                    this.iLanguage = LOCALE_CUSTOM_UNSPECIFIED;
                }
            }

            this.bNeutral = (this.SISO3166CTRYNAME.Length == 0);

            // Remove the sort from sName unless custom culture
            if (!this.bNeutral)
            {
                if (!IsCustomCultureId(this.iLanguage))
                {
                    this.sName = this.sWindowsName.Substring(0, index);
                }
            }

            return true;
        }
 
        private string GetLocaleInfo(LocaleStringData type)
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.GetLocaleInfo] Expected this.sWindowsName to be populated already");
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
                Contract.Assert(false, "[CultureData.GetLocaleInfo(LocaleStringData)] Failed");
                return String.Empty;
            }
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        private int GetLocaleInfo(LocaleNumberData type)
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.GetLocaleInfo(LocaleNumberData)] Expected this.sWindowsName to be populated already");

            switch (type)
            {
                case LocaleNumberData.CalendarType:
                    // returning 0 will cause the first supported calendar to be returned, which is the preferred calendar
                    return 0;
            }
            

            int value = 0;
            bool result = Interop.GlobalizationInterop.GetLocaleInfoInt(this.sWindowsName, (uint)type, ref value);
            if (!result)
            {
                // Failed, just use 0
                Contract.Assert(false, "[CultureData.GetLocaleInfo(LocaleNumberData)] failed");
            }

            return value;
        }

        private int[] GetLocaleInfo(LocaleGroupingData type)
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.GetLocaleInfo(LocaleGroupingData)] Expected this.sWindowsName to be populated already");

            int primaryGroupingSize = 0;
            int secondaryGroupingSize = 0;
            bool result = Interop.GlobalizationInterop.GetLocaleInfoGroupingSizes(this.sWindowsName, (uint)type, ref primaryGroupingSize, ref secondaryGroupingSize);
            if (!result)
            {
                Contract.Assert(false, "[CultureData.GetLocaleInfo(LocaleGroupingData type)] failed");
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
            Contract.Assert(this.sWindowsName != null, "[CultureData.GetTimeFormatString(bool shortFormat)] Expected this.sWindowsName to be populated already");

            StringBuilder sb = StringBuilderCache.Acquire(ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY);

            bool result = Interop.GlobalizationInterop.GetLocaleTimeFormat(this.sWindowsName, shortFormat, sb, sb.Capacity);
            if (!result)
            {
                // Failed, just use empty string
                StringBuilderCache.Release(sb);
                Contract.Assert(false, "[CultureData.GetTimeFormatString(bool shortFormat)] Failed");
                return String.Empty;
            }
            return StringBuilderCache.GetStringAndRelease(sb);
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
            return new CultureInfo(cultureName).m_cultureData.GetLocaleInfo(cultureName, LocaleStringData.LocalizedDisplayName);
        }

        private static string GetRegionDisplayName(string isoCountryCode)
        {
            // use the fallback which is to return NativeName
            return null;
        }

        private static CultureInfo GetUserDefaultCulture()
        {
            return new CultureInfo(LOCALE_NAME_SYSTEM_DEFAULT);
        }
    }
}
