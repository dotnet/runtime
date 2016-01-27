// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;

namespace System.Globalization
{

#if INSIDE_CLR
    using StringStringDictionary = Dictionary<string, string>;
    using StringCultureDataDictionary = Dictionary<String, CultureData>;
    using Lock = Object;
#else
    using StringStringDictionary = LowLevelDictionary<string, string>;
    using StringCultureDataDictionary = LowLevelDictionary<string, CultureData>;
#endif

    //
    // List of culture data
    // Note the we cache overrides.
    // Note that localized names (resource names) aren't available from here.
    //

    //
    // Our names are a tad confusing.
    //
    // sWindowsName -- The name that windows thinks this culture is, ie:
    //                            en-US if you pass in en-US
    //                            de-DE_phoneb if you pass in de-DE_phoneb
    //                            fj-FJ if you pass in fj (neutral, on a pre-Windows 7 machine)
    //                            fj if you pass in fj (neutral, post-Windows 7 machine)
    //
    // sRealName -- The name you used to construct the culture, in pretty form
    //                       en-US if you pass in EN-us
    //                       en if you pass in en
    //                       de-DE_phoneb if you pass in de-DE_phoneb
    //
    // sSpecificCulture -- The specific culture for this culture
    //                             en-US for en-US
    //                             en-US for en
    //                             de-DE_phoneb for alt sort
    //                             fj-FJ for fj (neutral)
    //
    // sName -- The IETF name of this culture (ie: no sort info, could be neutral)
    //                en-US if you pass in en-US
    //                en if you pass in en
    //                de-DE if you pass in de-DE_phoneb
    //
    internal partial class CultureData
    {
        const int undef = -1;
        const int LOCALE_CUSTOM_UNSPECIFIED = 0x1000;
        const int LOCALE_CUSTOM_DEFAULT = 0x0c00;

        // Override flag
        private String sRealName; // Name you passed in (ie: en-US, en, or de-DE_phoneb)
        private String sWindowsName; // Name OS thinks the object is (ie: de-DE_phoneb, or en-US (even if en was passed in))

        // Identity
        private String sName; // locale name (ie: en-us, NO sort info, but could be neutral)
        private String sParent; // Parent name (which may be a custom locale/culture)
        private String sLocalizedDisplayName; // Localized pretty name for this locale
        private String sEnglishDisplayName; // English pretty name for this locale
        private String sNativeDisplayName; // Native pretty name for this locale
        private String sSpecificCulture; // The culture name to be used in CultureInfo.CreateSpecificCulture(), en-US form if neutral, sort name if sort

        // Language
        private String sISO639Language; // ISO 639 Language Name
        private String sLocalizedLanguage; // Localized name for this language
        private String sEnglishLanguage; // English name for this language
        private String sNativeLanguage; // Native name of this language

        // Region
        private String sRegionName; // (RegionInfo)
        private String sLocalizedCountry; // localized country name
        private String sEnglishCountry; // english country name (RegionInfo)
        private String sNativeCountry; // native country name
        private String sISO3166CountryName; // ISO 3166 (RegionInfo), ie: US

        // Numbers
        private String sPositiveSign; // (user can override) positive sign
        private String sNegativeSign; // (user can override) negative sign
        private String[] saNativeDigits; // (user can override) native characters for digits 0-9
        // (nfi populates these 5, don't have to be = undef)
        private int iDigits; // (user can override) number of fractional digits
        private int iNegativeNumber; // (user can override) negative number format
        private int[] waGrouping; // (user can override) grouping of digits
        private String sDecimalSeparator; // (user can override) decimal separator
        private String sThousandSeparator; // (user can override) thousands separator
        private String sNaN; // Not a Number
        private String sPositiveInfinity; // + Infinity
        private String sNegativeInfinity; // - Infinity

        // Percent
        private int iNegativePercent = undef; // Negative Percent (0-3)
        private int iPositivePercent = undef; // Positive Percent (0-11)
        private String sPercent; // Percent (%) symbol
        private String sPerMille; // PerMille (‰) symbol

        // Currency
        private String sCurrency; // (user can override) local monetary symbol
        private String sIntlMonetarySymbol; // international monetary symbol (RegionInfo)
        // (nfi populates these 4, don't have to be = undef)
        private int iCurrencyDigits; // (user can override) # local monetary fractional digits
        private int iCurrency; // (user can override) positive currency format
        private int iNegativeCurrency; // (user can override) negative currency format
        private int[] waMonetaryGrouping; // (user can override) monetary grouping of digits
        private String sMonetaryDecimal; // (user can override) monetary decimal separator
        private String sMonetaryThousand; // (user can override) monetary thousands separator

        // Misc
        private int iMeasure = undef; // (user can override) system of measurement 0=metric, 1=US (RegionInfo)
        private String sListSeparator; // (user can override) list separator

        // Time
        private String sAM1159; // (user can override) AM designator
        private String sPM2359; // (user can override) PM designator
        private String sTimeSeparator;
        private volatile String[] saLongTimes; // (user can override) time format
        private volatile String[] saShortTimes; // short time format
        private volatile String[] saDurationFormats; // time duration format

        // Calendar specific data
        private int iFirstDayOfWeek = undef; // (user can override) first day of week (gregorian really)
        private int iFirstWeekOfYear = undef; // (user can override) first week of year (gregorian really)
        private volatile CalendarId[] waCalendars; // all available calendar type(s).  The first one is the default calendar

        // Store for specific data about each calendar
        private CalendarData[] calendars; // Store for specific calendar data

        // Text information
        private int iReadingLayout = undef; // Reading layout data
        // 0 - Left to right (eg en-US)
        // 1 - Right to left (eg arabic locales)
        // 2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
        // 3 - Vertical top to bottom with columns proceeding to the right

        // CoreCLR depends on this even though its not exposed publicly.

        private int iLanguage; // locale ID (0409) - NO sort information
        private bool bUseOverrides; // use user overrides?
        private bool bNeutral; // Flags for the culture (ie: neutral or not right now)


        // Region Name to Culture Name mapping table
        // (In future would be nice to be in registry or something)

        //Using a property so we avoid creating the dictionary untill we need it
        private static StringStringDictionary RegionNames
        {
            get
            {
                if (s_RegionNames == null)
                {
                    StringStringDictionary regionNames = new StringStringDictionary(211 /* prime */);

                    regionNames.Add("029", "en-029");
                    regionNames.Add("AE", "ar-AE");
                    regionNames.Add("AF", "prs-AF");
                    regionNames.Add("AL", "sq-AL");
                    regionNames.Add("AM", "hy-AM");
                    regionNames.Add("AR", "es-AR");
                    regionNames.Add("AT", "de-AT");
                    regionNames.Add("AU", "en-AU");
                    regionNames.Add("AZ", "az-Cyrl-AZ");
                    regionNames.Add("BA", "bs-Latn-BA");
                    regionNames.Add("BD", "bn-BD");
                    regionNames.Add("BE", "nl-BE");
                    regionNames.Add("BG", "bg-BG");
                    regionNames.Add("BH", "ar-BH");
                    regionNames.Add("BN", "ms-BN");
                    regionNames.Add("BO", "es-BO");
                    regionNames.Add("BR", "pt-BR");
                    regionNames.Add("BY", "be-BY");
                    regionNames.Add("BZ", "en-BZ");
                    regionNames.Add("CA", "en-CA");
                    regionNames.Add("CH", "it-CH");
                    regionNames.Add("CL", "es-CL");
                    regionNames.Add("CN", "zh-CN");
                    regionNames.Add("CO", "es-CO");
                    regionNames.Add("CR", "es-CR");
                    regionNames.Add("CS", "sr-Cyrl-CS");
                    regionNames.Add("CZ", "cs-CZ");
                    regionNames.Add("DE", "de-DE");
                    regionNames.Add("DK", "da-DK");
                    regionNames.Add("DO", "es-DO");
                    regionNames.Add("DZ", "ar-DZ");
                    regionNames.Add("EC", "es-EC");
                    regionNames.Add("EE", "et-EE");
                    regionNames.Add("EG", "ar-EG");
                    regionNames.Add("ES", "es-ES");
                    regionNames.Add("ET", "am-ET");
                    regionNames.Add("FI", "fi-FI");
                    regionNames.Add("FO", "fo-FO");
                    regionNames.Add("FR", "fr-FR");
                    regionNames.Add("GB", "en-GB");
                    regionNames.Add("GE", "ka-GE");
                    regionNames.Add("GL", "kl-GL");
                    regionNames.Add("GR", "el-GR");
                    regionNames.Add("GT", "es-GT");
                    regionNames.Add("HK", "zh-HK");
                    regionNames.Add("HN", "es-HN");
                    regionNames.Add("HR", "hr-HR");
                    regionNames.Add("HU", "hu-HU");
                    regionNames.Add("ID", "id-ID");
                    regionNames.Add("IE", "en-IE");
                    regionNames.Add("IL", "he-IL");
                    regionNames.Add("IN", "hi-IN");
                    regionNames.Add("IQ", "ar-IQ");
                    regionNames.Add("IR", "fa-IR");
                    regionNames.Add("IS", "is-IS");
                    regionNames.Add("IT", "it-IT");
                    regionNames.Add("IV", "");
                    regionNames.Add("JM", "en-JM");
                    regionNames.Add("JO", "ar-JO");
                    regionNames.Add("JP", "ja-JP");
                    regionNames.Add("KE", "sw-KE");
                    regionNames.Add("KG", "ky-KG");
                    regionNames.Add("KH", "km-KH");
                    regionNames.Add("KR", "ko-KR");
                    regionNames.Add("KW", "ar-KW");
                    regionNames.Add("KZ", "kk-KZ");
                    regionNames.Add("LA", "lo-LA");
                    regionNames.Add("LB", "ar-LB");
                    regionNames.Add("LI", "de-LI");
                    regionNames.Add("LK", "si-LK");
                    regionNames.Add("LT", "lt-LT");
                    regionNames.Add("LU", "lb-LU");
                    regionNames.Add("LV", "lv-LV");
                    regionNames.Add("LY", "ar-LY");
                    regionNames.Add("MA", "ar-MA");
                    regionNames.Add("MC", "fr-MC");
                    regionNames.Add("ME", "sr-Latn-ME");
                    regionNames.Add("MK", "mk-MK");
                    regionNames.Add("MN", "mn-MN");
                    regionNames.Add("MO", "zh-MO");
                    regionNames.Add("MT", "mt-MT");
                    regionNames.Add("MV", "dv-MV");
                    regionNames.Add("MX", "es-MX");
                    regionNames.Add("MY", "ms-MY");
                    regionNames.Add("NG", "ig-NG");
                    regionNames.Add("NI", "es-NI");
                    regionNames.Add("NL", "nl-NL");
                    regionNames.Add("NO", "nn-NO");
                    regionNames.Add("NP", "ne-NP");
                    regionNames.Add("NZ", "en-NZ");
                    regionNames.Add("OM", "ar-OM");
                    regionNames.Add("PA", "es-PA");
                    regionNames.Add("PE", "es-PE");
                    regionNames.Add("PH", "en-PH");
                    regionNames.Add("PK", "ur-PK");
                    regionNames.Add("PL", "pl-PL");
                    regionNames.Add("PR", "es-PR");
                    regionNames.Add("PT", "pt-PT");
                    regionNames.Add("PY", "es-PY");
                    regionNames.Add("QA", "ar-QA");
                    regionNames.Add("RO", "ro-RO");
                    regionNames.Add("RS", "sr-Latn-RS");
                    regionNames.Add("RU", "ru-RU");
                    regionNames.Add("RW", "rw-RW");
                    regionNames.Add("SA", "ar-SA");
                    regionNames.Add("SE", "sv-SE");
                    regionNames.Add("SG", "zh-SG");
                    regionNames.Add("SI", "sl-SI");
                    regionNames.Add("SK", "sk-SK");
                    regionNames.Add("SN", "wo-SN");
                    regionNames.Add("SV", "es-SV");
                    regionNames.Add("SY", "ar-SY");
                    regionNames.Add("TH", "th-TH");
                    regionNames.Add("TJ", "tg-Cyrl-TJ");
                    regionNames.Add("TM", "tk-TM");
                    regionNames.Add("TN", "ar-TN");
                    regionNames.Add("TR", "tr-TR");
                    regionNames.Add("TT", "en-TT");
                    regionNames.Add("TW", "zh-TW");
                    regionNames.Add("UA", "uk-UA");
                    regionNames.Add("US", "en-US");
                    regionNames.Add("UY", "es-UY");
                    regionNames.Add("UZ", "uz-Cyrl-UZ");
                    regionNames.Add("VE", "es-VE");
                    regionNames.Add("VN", "vi-VN");
                    regionNames.Add("YE", "ar-YE");
                    regionNames.Add("ZA", "af-ZA");
                    regionNames.Add("ZW", "en-ZW");

                    s_RegionNames = regionNames;
                }

                return s_RegionNames;
            }
        }

        // Cache of regions we've already looked up
        private static volatile StringCultureDataDictionary s_cachedRegions;
        private static volatile StringStringDictionary s_RegionNames;

        [System.Security.SecurityCritical]  // auto-generated
        internal static CultureData GetCultureDataForRegion(String cultureName, bool useUserOverride)
        {
            // First do a shortcut for Invariant
            if (String.IsNullOrEmpty(cultureName))
            {
                return CultureData.Invariant;
            }

            //
            // First check if GetCultureData() can find it (ie: its a real culture)
            //
            CultureData retVal = GetCultureData(cultureName, useUserOverride);
            if (retVal != null && (retVal.IsNeutralCulture == false)) return retVal;

            //
            // Not a specific culture, perhaps it's region-only name
            // (Remember this isn't a core clr path where that's not supported)
            //

            // If it was neutral remember that so that RegionInfo() can throw the right exception
            CultureData neutral = retVal;

            // Try the hash table next
            String hashName = AnsiToLower(useUserOverride ? cultureName : cultureName + '*');
            StringCultureDataDictionary tempHashTable = s_cachedRegions;
            if (tempHashTable == null)
            {
                // No table yet, make a new one
                tempHashTable = new StringCultureDataDictionary();
            }
            else
            {
                // Check the hash table
                lock (m_lock)
                {
                    tempHashTable.TryGetValue(hashName, out retVal);
                }
                if (retVal != null)
                {
                    return retVal;
                }
            }

            //
            // Not found in the hash table, look it up the hard way
            //

            // If not a valid mapping from the registry we'll have to try the hard coded table
            if (retVal == null || (retVal.IsNeutralCulture == true))
            {
                // Not a valid mapping, try the hard coded table
                string name;
                if (RegionNames.TryGetValue(cultureName, out name))
                {
                    // Make sure we can get culture data for it
                    retVal = GetCultureData(name, useUserOverride);
                }
            }

            // If not found in the hard coded table we'll have to find a culture that works for us
            if (retVal == null || (retVal.IsNeutralCulture == true))
            {
                retVal = GetCultureDataFromRegionName(cultureName);
            }

            // If we found one we can use, then cache it for next time
            if (retVal != null && (retVal.IsNeutralCulture == false))
            {
                // first add it to the cache
                lock (m_lock)
                {
                    tempHashTable[hashName] = retVal;
                }

                // Copy the hashtable to the corresponding member variables.  This will potentially overwrite
                // new tables simultaneously created by a new thread, but maximizes thread safety.
                s_cachedRegions = tempHashTable;
            }
            else
            {
                // Unable to find a matching culture/region, return null or neutral
                // (regionInfo throws a more specific exception on neutrals)
                retVal = neutral;
            }

            // Return the found culture to use, null, or the neutral culture.
            return retVal;
        }


        /////////////////////////////////////////////////////////////////////////
        // Build our invariant information
        //
        // We need an invariant instance, which we build hard-coded
        /////////////////////////////////////////////////////////////////////////
        internal static CultureData Invariant
        {
            get
            {
                if (s_Invariant == null)
                {
                    // Make a new culturedata
                    CultureData invariant = new CultureData();

                    // Basics
                    // Note that we override the resources since this IS NOT supposed to change (by definition)
                    invariant.bUseOverrides = false;
                    invariant.sRealName = "";                     // Name you passed in (ie: en-US, en, or de-DE_phoneb)
                    invariant.sWindowsName = "";                     // Name OS thinks the object is (ie: de-DE_phoneb, or en-US (even if en was passed in))

                    // Identity
                    invariant.sName = "";                     // locale name (ie: en-us)
                    invariant.sParent = "";                     // Parent name (which may be a custom locale/culture)
                    invariant.bNeutral = false;                   // Flags for the culture (ie: neutral or not right now)
                    invariant.sEnglishDisplayName = "Invariant Language (Invariant Country)"; // English pretty name for this locale
                    invariant.sNativeDisplayName = "Invariant Language (Invariant Country)";  // Native pretty name for this locale
                    invariant.sSpecificCulture = "";                     // The culture name to be used in CultureInfo.CreateSpecificCulture()

                    // Language
                    invariant.sISO639Language = "iv";                   // ISO 639 Language Name
                    invariant.sLocalizedLanguage = "Invariant Language";   // Display name for this Language
                    invariant.sEnglishLanguage = "Invariant Language";   // English name for this language
                    invariant.sNativeLanguage = "Invariant Language";   // Native name of this language

                    // Region
                    invariant.sRegionName = "IV";                   // (RegionInfo)
                    invariant.sEnglishCountry = "Invariant Country";    // english country name (RegionInfo)
                    invariant.sNativeCountry = "Invariant Country";    // native country name (Windows Only)
                    invariant.sISO3166CountryName = "IV";                   // (RegionInfo), ie: US

                    // Numbers
                    invariant.sPositiveSign = "+";                    // positive sign
                    invariant.sNegativeSign = "-";                    // negative sign
                    invariant.saNativeDigits = new String[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }; // native characters for digits 0-9
                    invariant.iDigits = 2;                      // number of fractional digits
                    invariant.iNegativeNumber = 1;                      // negative number format
                    invariant.waGrouping = new int[] { 3 };          // grouping of digits
                    invariant.sDecimalSeparator = ".";                    // decimal separator
                    invariant.sThousandSeparator = ",";                    // thousands separator
                    invariant.sNaN = "NaN";                  // Not a Number
                    invariant.sPositiveInfinity = "Infinity";             // + Infinity
                    invariant.sNegativeInfinity = "-Infinity";            // - Infinity

                    // Percent
                    invariant.iNegativePercent = 0;                      // Negative Percent (0-3)
                    invariant.iPositivePercent = 0;                      // Positive Percent (0-11)
                    invariant.sPercent = "%";                    // Percent (%) symbol
                    invariant.sPerMille = "\x2030";               // PerMille(‰) symbol

                    // Currency
                    invariant.sCurrency = "\x00a4";         // local monetary symbol "¤: for international monetary symbol
                    invariant.sIntlMonetarySymbol = "XDR";                  // international monetary symbol (RegionInfo)
                    invariant.iCurrencyDigits = 2;                      // # local monetary fractional digits
                    invariant.iCurrency = 0;                      // positive currency format
                    invariant.iNegativeCurrency = 0;                      // negative currency format
                    invariant.waMonetaryGrouping = new int[] { 3 };          // monetary grouping of digits
                    invariant.sMonetaryDecimal = ".";                    // monetary decimal separator
                    invariant.sMonetaryThousand = ",";                    // monetary thousands separator

                    // Misc
                    invariant.iMeasure = 0;                      // system of measurement 0=metric, 1=US (RegionInfo)
                    invariant.sListSeparator = ",";                    // list separator

                    // Time
                    invariant.sAM1159 = "AM";                   // AM designator
                    invariant.sPM2359 = "PM";                   // PM designator
                    invariant.saLongTimes = new String[] { "HH:mm:ss" };                             // time format
                    invariant.saShortTimes = new String[] { "HH:mm", "hh:mm tt", "H:mm", "h:mm tt" }; // short time format
                    invariant.saDurationFormats = new String[] { "HH:mm:ss" };                             // time duration format

                    // Calendar specific data
                    invariant.iFirstDayOfWeek = 0;                      // first day of week
                    invariant.iFirstWeekOfYear = 0;                      // first week of year
                    invariant.waCalendars = new CalendarId[] { CalendarId.GREGORIAN };       // all available calendar type(s).  The first one is the default calendar

                    // Store for specific data about each calendar
                    invariant.calendars = new CalendarData[CalendarData.MAX_CALENDARS];
                    invariant.calendars[0] = CalendarData.Invariant;

                    // Text information
                    invariant.iReadingLayout = 0;

                    // Language
                    invariant.iLanguage = 0x007f;                 // locale ID (0409) - NO sort information

                    // Remember it
                    s_Invariant = invariant;
                }
                return s_Invariant;
            }
        }
        private volatile static CultureData s_Invariant;

        ///////////////
        // Constructors //
        ///////////////
        // Cache of cultures we've already looked up
        private static volatile StringCultureDataDictionary s_cachedCultures;
        private static readonly Lock m_lock = new Lock();

        internal static CultureData GetCultureData(String cultureName, bool useUserOverride)
        {
            // First do a shortcut for Invariant
            if (String.IsNullOrEmpty(cultureName))
            {
                return CultureData.Invariant;
            }

            // Try the hash table first
            String hashName = AnsiToLower(useUserOverride ? cultureName : cultureName + '*');
            StringCultureDataDictionary tempHashTable = s_cachedCultures;
            if (tempHashTable == null)
            {
                // No table yet, make a new one
                tempHashTable = new StringCultureDataDictionary();
            }
            else
            {
                // Check the hash table
                bool ret;
                CultureData retVal;
                lock (m_lock)
                {
                    ret = tempHashTable.TryGetValue(hashName, out retVal);
                }
                if (ret && retVal != null)
                {
                    return retVal;
                }
            }

            // Not found in the hash table, need to see if we can build one that works for us
            CultureData culture = CreateCultureData(cultureName, useUserOverride);
            if (culture == null)
            {
                return null;
            }

            // Found one, add it to the cache
            lock (m_lock)
            {
                tempHashTable[hashName] = culture;
            }

            // Copy the hashtable to the corresponding member variables.  This will potentially overwrite
            // new tables simultaneously created by a new thread, but maximizes thread safety.
            s_cachedCultures = tempHashTable;

            return culture;
        }

        private static CultureData CreateCultureData(string cultureName, bool useUserOverride)
        {
            CultureData culture = new CultureData();
            culture.bUseOverrides = useUserOverride;
            culture.sRealName = cultureName;

            // Ask native code if that one's real
            if (culture.InitCultureData() == false)
            {
                if (culture.InitCompatibilityCultureData() == false)
                {
                    return null;
                }
            }

            return culture;
        }

        private bool InitCompatibilityCultureData()
        {
            // for compatibility handle the deprecated ids: zh-chs, zh-cht
            string cultureName = this.sRealName;

            string fallbackCultureName;
            string realCultureName;
            switch (AnsiToLower(cultureName))
            {
                case "zh-chs":
                    fallbackCultureName = "zh-Hans";
                    realCultureName = "zh-CHS";
                    break;
                case "zh-cht":
                    fallbackCultureName = "zh-Hant";
                    realCultureName = "zh-CHT";
                    break;
                default:
                    return false;
            }

            this.sRealName = fallbackCultureName;
            if (InitCultureData() == false)
            {
                return false;
            }
            // fixup our data
            this.sName = realCultureName; // the name that goes back to the user
            this.sParent = fallbackCultureName;

            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  All the accessors
        //
        //  Accessors for our data object items
        //
        ////////////////////////////////////////////////////////////////////////

        ///////////
        // Identity //
        ///////////

        // The real name used to construct the locale (ie: de-DE_phoneb)
        internal String CultureName
        {
            get
            {
                Contract.Assert(this.sRealName != null, "[CultureData.CultureName] Expected this.sRealName to be populated by already");
                // since windows doesn't know about zh-CHS and zh-CHT,
                // we leave sRealName == zh-Hanx but we still need to
                // pretend that it was zh-CHX.
                switch (this.sName)
                {
                    case "zh-CHS":
                    case "zh-CHT":
                        return this.sName;
                }
                return this.sRealName;
            }
        }

        // Are overrides enabled?
        internal bool UseUserOverride
        {
            get
            {
                return this.bUseOverrides;
            }
        }

        // locale name (ie: de-DE, NO sort information)
        internal String SNAME
        {
            get
            {
                if (this.sName == null)
                {
                    this.sName = String.Empty;
                }
                return this.sName;
            }
        }

        // Parent name (which may be a custom locale/culture)
        internal String SPARENT
        {
            get
            {
                if (this.sParent == null)
                {
                    // Ask using the real name, so that we get parents of neutrals
                    this.sParent = GetLocaleInfo(this.sRealName, LocaleStringData.ParentName);
                }
                return this.sParent;
            }
        }

        // Localized pretty name for this locale (ie: Inglis (estados Unitos))
        internal String SLOCALIZEDDISPLAYNAME
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sLocalizedDisplayName == null)
                {
                    if (this.IsSupplementalCustomCulture)
                    {
                        if (this.IsNeutralCulture)
                        {
                            this.sLocalizedDisplayName = this.SNATIVELANGUAGE;
                        }
                        else
                        {
                            this.sLocalizedDisplayName = this.SNATIVEDISPLAYNAME;
                        }
                    }
                    else
                    {
                        try
                        {
                            const string ZH_CHT = "zh-CHT";
                            const string ZH_CHS = "zh-CHS";

                            if (SNAME.Equals(ZH_CHT, StringComparison.OrdinalIgnoreCase))
                            {
                                this.sLocalizedDisplayName = GetLanguageDisplayName("zh-Hant");
                            }
                            else if (SNAME.Equals(ZH_CHS, StringComparison.OrdinalIgnoreCase)) 
                            {
                                this.sLocalizedDisplayName = GetLanguageDisplayName("zh-Hans");
                            }
                            else
                            {
                                this.sLocalizedDisplayName = GetLanguageDisplayName(SNAME);
                            }
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                    }
                    // If it hasn't been found (Windows 8 and up), fallback to the system
                    if (String.IsNullOrEmpty(this.sLocalizedDisplayName))
                    {
                        // If its neutral use the language name
                        if (this.IsNeutralCulture)
                        {
                            this.sLocalizedDisplayName = this.SLOCALIZEDLANGUAGE;
                        }
                        else
                        {
                            // Usually the UI culture shouldn't be different than what we got from WinRT except 
                            // if DefaultThreadCurrentUICulture was set
                            CultureInfo ci;

                            if (CultureInfo.DefaultThreadCurrentUICulture != null &&
                                ((ci = GetUserDefaultCulture()) != null) &&
                                !CultureInfo.DefaultThreadCurrentUICulture.Name.Equals(ci.Name))
                            {
                                this.sLocalizedDisplayName = this.SNATIVEDISPLAYNAME;
                            }
                            else
                            {
                                this.sLocalizedDisplayName = GetLocaleInfo(LocaleStringData.LocalizedDisplayName);
                            }
                        }
                    }
                }

                return this.sLocalizedDisplayName;
            }
        }

        // English pretty name for this locale (ie: English (United States))
        internal String SENGDISPLAYNAME
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sEnglishDisplayName == null)
                {
                    // If its neutral use the language name
                    if (this.IsNeutralCulture)
                    {
                        this.sEnglishDisplayName = this.SENGLISHLANGUAGE;
                        // differentiate the legacy display names
                        switch (this.sName)
                        {
                            case "zh-CHS":
                            case "zh-CHT":
                                this.sEnglishDisplayName += " Legacy";
                                break;
                        }
                    }
                    else
                    {
                        this.sEnglishDisplayName = GetLocaleInfo(LocaleStringData.EnglishDisplayName);

                        // if it isn't found build one:
                        if (String.IsNullOrEmpty(this.sEnglishDisplayName))
                        {
                            // Our existing names mostly look like:
                            // "English" + "United States" -> "English (United States)"
                            // "Azeri (Latin)" + "Azerbaijan" -> "Azeri (Latin, Azerbaijan)"
                            if (this.SENGLISHLANGUAGE[this.SENGLISHLANGUAGE.Length - 1] == ')')
                            {
                                // "Azeri (Latin)" + "Azerbaijan" -> "Azeri (Latin, Azerbaijan)"
                                this.sEnglishDisplayName =
                                    this.SENGLISHLANGUAGE.Substring(0, this.sEnglishLanguage.Length - 1) +
                                    ", " + this.SENGCOUNTRY + ")";
                            }
                            else
                            {
                                // "English" + "United States" -> "English (United States)"
                                this.sEnglishDisplayName = this.SENGLISHLANGUAGE + " (" + this.SENGCOUNTRY + ")";
                            }
                        }
                    }
                }
                return this.sEnglishDisplayName;
            }
        }

        // Native pretty name for this locale (ie: Deutsch (Deutschland))
        internal String SNATIVEDISPLAYNAME
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sNativeDisplayName == null)
                {
                    // If its neutral use the language name
                    if (this.IsNeutralCulture)
                    {
                        this.sNativeDisplayName = this.SNATIVELANGUAGE;
                        // differentiate the legacy display names
                        switch (this.sName)
                        {
                            case "zh-CHS":
                                this.sNativeDisplayName += " \u65E7\u7248";
                                break;
                            case "zh-CHT":
                                this.sNativeDisplayName += " \u820A\u7248";
                                break;
                        }
                    }
                    else
                    {
                        this.sNativeDisplayName = GetLocaleInfo(LocaleStringData.NativeDisplayName);

                        // if it isn't found build one:
                        if (String.IsNullOrEmpty(this.sNativeDisplayName))
                        {
                            // These should primarily be "Deutsch (Deutschland)" type names
                            this.sNativeDisplayName = this.SNATIVELANGUAGE + " (" + this.SNATIVECOUNTRY + ")";
                        }
                    }
                }
                return this.sNativeDisplayName;
            }
        }

        /////////////
        // Language //
        /////////////

        // iso 639 language name, ie: en
        internal String SISO639LANGNAME
        {
            get
            {
                if (this.sISO639Language == null)
                {
                    this.sISO639Language = GetLocaleInfo(LocaleStringData.Iso639LanguageName);
                }
                return this.sISO639Language;
            }
        }

        // Localized name for this language (Windows Only) ie: Inglis
        // This is only valid for Windows 8 and higher neutrals:
        internal String SLOCALIZEDLANGUAGE
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sLocalizedLanguage == null)
                {
                    // Usually the UI culture shouldn't be different than what we got from WinRT except 
                    // if DefaultThreadCurrentUICulture was set
                    CultureInfo ci;

                    if (CultureInfo.DefaultThreadCurrentUICulture != null &&
                        ((ci = GetUserDefaultCulture()) != null) &&
                        !CultureInfo.DefaultThreadCurrentUICulture.Name.Equals(ci.Name))
                    {
                        this.sLocalizedLanguage = SNATIVELANGUAGE;
                    }
                    else
                    {
                        this.sLocalizedLanguage = GetLocaleInfo(LocaleStringData.LocalizedLanguageName);
                    }
                }

                return this.sLocalizedLanguage;
            }
        }

        // English name for this language (Windows Only) ie: German
        internal String SENGLISHLANGUAGE
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sEnglishLanguage == null)
                {
                    this.sEnglishLanguage = GetLocaleInfo(LocaleStringData.EnglishLanguageName);
                }
                return this.sEnglishLanguage;
            }
        }

        // Native name of this language (Windows Only) ie: Deutsch
        internal String SNATIVELANGUAGE
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sNativeLanguage == null)
                {
                    this.sNativeLanguage = GetLocaleInfo(LocaleStringData.NativeLanguageName);
                }
                return this.sNativeLanguage;
            }
        }

        ///////////
        // Region //
        ///////////

        // region name (eg US)
        internal String SREGIONNAME
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sRegionName == null)
                {
                    this.sRegionName = GetLocaleInfo(LocaleStringData.Iso3166CountryName);
                }
                return this.sRegionName;
            }
        }


        // localized name for the country
        internal string SLOCALIZEDCOUNTRY
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sLocalizedCountry == null)
                {
                    try
                    {
                        this.sLocalizedCountry = GetRegionDisplayName(SISO3166CTRYNAME);
                    }
                    catch (Exception)
                    {
                        // do nothing. we'll fallback 
                    }

                    if (this.sLocalizedCountry == null)
                    {
                        this.sLocalizedCountry = SNATIVECOUNTRY;
                    }
                }
                return this.sLocalizedCountry;
            }
        }

        // english country name (RegionInfo) ie: Germany
        internal String SENGCOUNTRY
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sEnglishCountry == null)
                {
                    this.sEnglishCountry = GetLocaleInfo(LocaleStringData.EnglishCountryName);
                }
                return this.sEnglishCountry;
            }
        }

        // native country name (RegionInfo) ie: Deutschland
        internal String SNATIVECOUNTRY
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sNativeCountry == null)
                {
                    this.sNativeCountry = GetLocaleInfo(LocaleStringData.NativeCountryName);
                }
                return this.sNativeCountry;
            }
        }

        // ISO 3166 Country Name
        internal String SISO3166CTRYNAME
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sISO3166CountryName == null)
                {
                    this.sISO3166CountryName = GetLocaleInfo(LocaleStringData.Iso3166CountryName);
                }
                return this.sISO3166CountryName;
            }
        }

        /////////////
        // Numbers //
        ////////////

        //                internal String sPositiveSign            ; // (user can override) positive sign
        //                internal String sNegativeSign            ; // (user can override) negative sign
        //                internal String[] saNativeDigits         ; // (user can override) native characters for digits 0-9
        //                internal int iDigits                  ; // (user can override) number of fractional digits
        //                internal int iNegativeNumber          ; // (user can override) negative number format



        // (user can override) grouping of digits
        internal int[] WAGROUPING
        {
            get
            {
                if (this.waGrouping == null)
                {
                    this.waGrouping = GetLocaleInfo(LocaleGroupingData.Digit);
                }
                return this.waGrouping;
            }
        }


        //                internal String sDecimalSeparator        ; // (user can override) decimal separator
        //                internal String sThousandSeparator       ; // (user can override) thousands separator

        // Not a Number
        internal String SNAN
        {
            get
            {
                if (this.sNaN == null)
                {
                    this.sNaN = GetLocaleInfo(LocaleStringData.NaNSymbol);
                }
                return this.sNaN;
            }
        }

        // + Infinity
        internal String SPOSINFINITY
        {
            get
            {
                if (this.sPositiveInfinity == null)
                {
                    this.sPositiveInfinity = GetLocaleInfo(LocaleStringData.PositiveInfinitySymbol);
                }
                return this.sPositiveInfinity;
            }
        }

        // - Infinity
        internal String SNEGINFINITY
        {
            get
            {
                if (this.sNegativeInfinity == null)
                {
                    this.sNegativeInfinity = GetLocaleInfo(LocaleStringData.NegativeInfinitySymbol);
                }
                return this.sNegativeInfinity;
            }
        }


        ////////////
        // Percent //
        ///////////

        // Negative Percent (0-3)
        internal int INEGATIVEPERCENT
        {
            get
            {
                if (this.iNegativePercent == undef)
                {
                    // Note that <= Windows Vista this is synthesized by native code
                    this.iNegativePercent = GetLocaleInfo(LocaleNumberData.NegativePercentFormat);
                }
                return this.iNegativePercent;
            }
        }

        // Positive Percent (0-11)
        internal int IPOSITIVEPERCENT
        {
            get
            {
                if (this.iPositivePercent == undef)
                {
                    // Note that <= Windows Vista this is synthesized by native code
                    this.iPositivePercent = GetLocaleInfo(LocaleNumberData.PositivePercentFormat);
                }
                return this.iPositivePercent;
            }
        }

        // Percent (%) symbol
        internal String SPERCENT
        {
            get
            {
                if (this.sPercent == null)
                {
                    this.sPercent = GetLocaleInfo(LocaleStringData.PercentSymbol);
                }
                return this.sPercent;
            }
        }

        // PerMille (‰) symbol
        internal String SPERMILLE
        {
            get
            {
                if (this.sPerMille == null)
                {
                    this.sPerMille = GetLocaleInfo(LocaleStringData.PerMilleSymbol);
                }
                return this.sPerMille;
            }
        }

        /////////////
        // Currency //
        /////////////

        // (user can override) local monetary symbol, eg: $
        internal String SCURRENCY
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sCurrency == null)
                {
                    this.sCurrency = GetLocaleInfo(LocaleStringData.MonetarySymbol);
                }
                return this.sCurrency;
            }
        }

        // international monetary symbol (RegionInfo), eg: USD
        internal String SINTLSYMBOL
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sIntlMonetarySymbol == null)
                {
                    this.sIntlMonetarySymbol = GetLocaleInfo(LocaleStringData.Iso4217MonetarySymbol);
                }
                return this.sIntlMonetarySymbol;
            }
        }

        //                internal int iCurrencyDigits          ; // (user can override) # local monetary fractional digits
        //                internal int iCurrency                ; // (user can override) positive currency format
        //                internal int iNegativeCurrency        ; // (user can override) negative currency format

        // (user can override) monetary grouping of digits
        internal int[] WAMONGROUPING
        {
            get
            {
                if (this.waMonetaryGrouping == null)
                {
                    this.waMonetaryGrouping = GetLocaleInfo(LocaleGroupingData.Monetary);
                }
                return this.waMonetaryGrouping;
            }
        }

        // (user can override) system of measurement 0=metric, 1=US (RegionInfo)
        internal int IMEASURE
        {
            get
            {
                if (this.iMeasure == undef)
                {
                    this.iMeasure = GetLocaleInfo(LocaleNumberData.MeasurementSystem);
                }
                return this.iMeasure;
            }
        }

        // (user can override) list Separator
        internal String SLIST
        {
            get
            {
                if (this.sListSeparator == null)
                {
                    this.sListSeparator = GetLocaleInfo(LocaleStringData.ListSeparator);
                }
                return this.sListSeparator;
            }
        }


        ////////////////////////////
        // Calendar/Time (Gregorian) //
        ////////////////////////////

        // (user can override) AM designator
        internal String SAM1159
        {
            get
            {
                if (this.sAM1159 == null)
                {
                    this.sAM1159 = GetLocaleInfo(LocaleStringData.AMDesignator);
                }
                return this.sAM1159;
            }
        }

        // (user can override) PM designator
        internal String SPM2359
        {
            get
            {
                if (this.sPM2359 == null)
                {
                    this.sPM2359 = GetLocaleInfo(LocaleStringData.PMDesignator);
                }
                return this.sPM2359;
            }
        }

        // (user can override) time format
        internal String[] LongTimes
        {
            get
            {
                if (this.saLongTimes == null)
                {
                    String[] longTimes = GetTimeFormats();
                    if (longTimes == null || longTimes.Length == 0)
                    {
                        this.saLongTimes = Invariant.saLongTimes;
                    }
                    else
                    {
                        this.saLongTimes = longTimes;
                    }
                }
                return this.saLongTimes;
            }
        }

        // short time format
        // Short times (derived from long times format)
        // TODO: NLS Arrowhead -  On Windows 7 we should have short times so this isn't necessary
        internal String[] ShortTimes
        {
            get
            {
                if (this.saShortTimes == null)
                {
                    // Try to get the short times from the OS/culture.dll
                    String[] shortTimes = null;
                    shortTimes = GetShortTimeFormats();

                    if (shortTimes == null || shortTimes.Length == 0)
                    {
                        //
                        // If we couldn't find short times, then compute them from long times
                        // (eg: CORECLR on < Win7 OS & fallback for missing culture.dll)
                        //
                        shortTimes = DeriveShortTimesFromLong();
                    }

                    /* The above logic doesn't make sense on Mac, since the OS can provide us a "short time pattern".
                     * currently this is the 4th element in the array returned by LongTimes.  We'll add this to our array
                     * if it doesn't exist.
                     */
                    shortTimes = AdjustShortTimesForMac(shortTimes);

                    // Found short times, use them
                    this.saShortTimes = shortTimes;
                }
                return this.saShortTimes;
            }
        }

        private string[] AdjustShortTimesForMac(string[] shortTimes)
        {
            return shortTimes;
        }

        private string[] DeriveShortTimesFromLong()
        {
            // Our logic is to look for h,H,m,s,t.  If we find an s, then we check the string
            // between it and the previous marker, if any.  If its a short, unescaped separator,
            // then we don't retain that part.
            // We then check after the ss and remove anything before the next h,H,m,t...
            string[] shortTimes = new string[LongTimes.Length];

            for (int i = 0; i < LongTimes.Length; i++)
            {
                shortTimes[i] = StripSecondsFromPattern(LongTimes[i]);
            }
            return shortTimes;
        }

        private static string StripSecondsFromPattern(string time)
        {
            bool bEscape = false;
            int iLastToken = -1;

            // Find the seconds
            for (int j = 0; j < time.Length; j++)
            {
                // Change escape mode?
                if (time[j] == '\'')
                {
                    // Continue
                    bEscape = !bEscape;
                    continue;
                }

                // See if there was a single \
                if (time[j] == '\\')
                {
                    // Skip next char
                    j++;
                    continue;
                }

                if (bEscape)
                {
                    continue;
                }

                switch (time[j])
                {
                    // Check for seconds
                    case 's':
                        // Found seconds, see if there was something unescaped and short between
                        // the last marker and the seconds.  Windows says separator can be a
                        // maximum of three characters (without null)
                        // If 1st or last characters were ', then ignore it
                        if ((j - iLastToken) <= 4 && (j - iLastToken) > 1 &&
                            (time[iLastToken + 1] != '\'') &&
                            (time[j - 1] != '\''))
                        {
                            // There was something there we want to remember
                            if (iLastToken >= 0)
                            {
                                j = iLastToken + 1;
                            }
                        }

                        bool containsSpace;
                        int endIndex = GetIndexOfNextTokenAfterSeconds(time, j, out containsSpace);

                        string sep;

                        if (containsSpace)
                        {
                            sep = " ";
                        }
                        else
                        {
                            sep = "";
                        }

                        time = time.Substring(0, j) + sep + time.Substring(endIndex);
                        break;
                    case 'm':
                    case 'H':
                    case 'h':
                        iLastToken = j;
                        break;
                }
            }
            return time;
        }

        private static int GetIndexOfNextTokenAfterSeconds(string time, int index, out bool containsSpace)
        {
            bool bEscape = false;
            containsSpace = false;
            for (; index < time.Length; index++)
            {
                switch (time[index])
                {
                    case '\'':
                        bEscape = !bEscape;
                        continue;
                    case '\\':
                        index++;
                        if (time[index] == ' ')
                        {
                            containsSpace = true;
                        }
                        continue;
                    case ' ':
                        containsSpace = true;
                        break;
                    case 't':
                    case 'm':
                    case 'H':
                    case 'h':
                        if (bEscape)
                        {
                            continue;
                        }
                        return index;
                }
            }
            containsSpace = false;
            return index;
        }

        // (user can override) first day of week
        internal int IFIRSTDAYOFWEEK
        {
            get
            {
                if (this.iFirstDayOfWeek == undef)
                {
                    this.iFirstDayOfWeek = GetFirstDayOfWeek();
                }
                return this.iFirstDayOfWeek;
            }
        }

        // (user can override) first week of year
        internal int IFIRSTWEEKOFYEAR
        {
            get
            {
                if (this.iFirstWeekOfYear == undef)
                {
                    this.iFirstWeekOfYear = GetLocaleInfo(LocaleNumberData.FirstWeekOfYear);
                }
                return this.iFirstWeekOfYear;
            }
        }

        // (user can override default only) short date format
        internal String[] ShortDates(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saShortDates;
        }

        // (user can override default only) long date format
        internal String[] LongDates(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saLongDates;
        }

        // (user can override) date year/month format.
        internal String[] YearMonths(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saYearMonths;
        }

        // day names
        internal string[] DayNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saDayNames;
        }

        // abbreviated day names
        internal string[] AbbreviatedDayNames(CalendarId calendarId)
        {
            // Get abbreviated day names for this calendar from the OS if necessary
            return GetCalendar(calendarId).saAbbrevDayNames;
        }

        // The super short day names
        internal string[] SuperShortDayNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saSuperShortDayNames;
        }

        // month names
        internal string[] MonthNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saMonthNames;
        }

        // Genitive month names
        internal string[] GenitiveMonthNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saMonthGenitiveNames;
        }

        // month names
        internal string[] AbbreviatedMonthNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saAbbrevMonthNames;
        }

        // Genitive month names
        internal string[] AbbreviatedGenitiveMonthNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saAbbrevMonthGenitiveNames;
        }

        // Leap year month names
        // Note: This only applies to Hebrew, and it basically adds a "1" to the 6th month name
        // the non-leap names skip the 7th name in the normal month name array
        internal string[] LeapYearMonthNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saLeapYearMonthNames;
        }

        // month/day format (single string, no override)
        internal String MonthDay(CalendarId calendarId)
        {
            return GetCalendar(calendarId).sMonthDay;
        }



        /////////////
        // Calendars //
        /////////////

        // all available calendar type(s), The first one is the default calendar.
        internal CalendarId[] CalendarIds
        {
            get
            {
                if (this.waCalendars == null)
                {
                    // We pass in an array of ints, and native side fills it up with count calendars.
                    // We then have to copy that list to a new array of the right size.
                    // Default calendar should be first
                    CalendarId[] calendars = new CalendarId[23];
                    Contract.Assert(this.sWindowsName != null, "[CultureData.CalendarIds] Expected this.sWindowsName to be populated by already");
                    int count = CalendarData.GetCalendars(this.sWindowsName, this.bUseOverrides, calendars);

                    // See if we had a calendar to add.
                    if (count == 0)
                    {
                        // Failed for some reason, just grab Gregorian from Invariant
                        this.waCalendars = Invariant.waCalendars;
                    }
                    else
                    {
                        // The OS may not return calendar 4 for zh-TW, but we've always allowed it.
                        // TODO: Is this necessary long-term?
                        if (this.sWindowsName == "zh-TW")
                        {
                            bool found = false;

                            // Do we need to insert calendar 4?
                            for (int i = 0; i < count; i++)
                            {
                                // Stop if we found calendar four
                                if (calendars[i] == CalendarId.TAIWAN)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            // If not found then insert it
                            if (!found)
                            {
                                // Insert it as the 2nd calendar
                                count++;
                                // Copy them from the 2nd position to the end, -1 for skipping 1st, -1 for one being added.
                                Array.Copy(calendars, 1, calendars, 2, 23 - 1 - 1);
                                calendars[1] = CalendarId.TAIWAN;
                            }
                        }

                        // It worked, remember the list
                        CalendarId[] temp = new CalendarId[count];
                        Array.Copy(calendars, temp, count);

                        // Want 1st calendar to be default
                        // Prior to Vista the enumeration didn't have default calendar first
                        if (temp.Length > 1)
                        {
                            CalendarId i = (CalendarId)GetLocaleInfo(LocaleNumberData.CalendarType);
                            if (temp[1] == i)
                            {
                                temp[1] = temp[0];
                                temp[0] = i;
                            }
                        }

                        this.waCalendars = temp;
                    }
                }

                return this.waCalendars;
            }
        }

        internal CalendarData GetCalendar(CalendarId calendarId)
        {
            Contract.Assert(calendarId > 0 && calendarId <= CalendarId.LAST_CALENDAR,
                "[CultureData.GetCalendar] Expect calendarId to be in a valid range");

            // arrays are 0 based, calendarIds are 1 based
            int calendarIndex = (int)calendarId - 1;

            // Have to have calendars
            if (calendars == null)
            {
                calendars = new CalendarData[CalendarData.MAX_CALENDARS];
            }

            // we need the following local variable to avoid returning null
            // when another thread creates a new array of CalendarData (above)
            // right after we insert the newly created CalendarData (below)
            CalendarData calendarData = calendars[calendarIndex];
            // Make sure that calendar has data
            if (calendarData == null)
            {
                Contract.Assert(this.sWindowsName != null, "[CultureData.GetCalendar] Expected this.sWindowsName to be populated by already");
                calendarData = new CalendarData(this.sWindowsName, calendarId, this.UseUserOverride);
                calendars[calendarIndex] = calendarData;
            }

            return calendarData;
        }

        ///////////////////
        // Text Information //
        ///////////////////

        // IsRightToLeft
        internal bool IsRightToLeft
        {
            get
            {
                // Returns one of the following 4 reading layout values:
                // 0 - Left to right (eg en-US)
                // 1 - Right to left (eg arabic locales)
                // 2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
                // 3 - Vertical top to bottom with columns proceeding to the right
                return (this.IREADINGLAYOUT == 1);
            }
        }

        // IREADINGLAYOUT
        // Returns one of the following 4 reading layout values:
        // 0 - Left to right (eg en-US)
        // 1 - Right to left (eg arabic locales)
        // 2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
        // 3 - Vertical top to bottom with columns proceeding to the right
        //
        // If exposed as a public API, we'd have an enum with those 4 values
        private int IREADINGLAYOUT
        {
            get
            {
                if (this.iReadingLayout == undef)
                {
                    Contract.Assert(this.sRealName != null, "[CultureData.IsRightToLeft] Expected this.sRealName to be populated by already");
                    this.iReadingLayout = GetLocaleInfo(LocaleNumberData.ReadingLayout);
                }

                return (this.iReadingLayout);
            }
        }

        // The TextInfo name never includes that alternate sort and is always specific
        // For customs, it uses the SortLocale (since the textinfo is not exposed in Win7)
        // en -> en-US
        // en-US -> en-US
        // fj (custom neutral) -> en-US (assuming that en-US is the sort locale for fj)
        // fj_FJ (custom specific) -> en-US (assuming that en-US is the sort locale for fj-FJ)
        // es-ES_tradnl -> es-ES
        internal String STEXTINFO               // Text info name to use for text information
        {
            get
            {
                // Note: Custom cultures might point at another culture's textinfo, however windows knows how
                // to redirect it to the desired textinfo culture, so this is OK.
                Contract.Assert(this.sRealName != null, "[CultureData.STEXTINFO] Expected this.sRealName to be populated by already");
                return (this.sRealName);
            }
        }

        // Compare info name (including sorting key) to use if custom
        internal String SCOMPAREINFO
        {
            get
            {
                Contract.Assert(this.sRealName != null, "[CultureData.SCOMPAREINFO] Expected this.sRealName to be populated by already");
                return (this.sRealName);
            }
        }

        internal bool IsSupplementalCustomCulture
        {
            get
            {
                return IsCustomCultureId(this.ILANGUAGE);
            }
        }

        internal int ILANGUAGE
        {
            get
            {
                return this.iLanguage;
            }
        }

        internal bool IsNeutralCulture
        {
            get
            {
                // InitCultureData told us if we're neutral or not
                return this.bNeutral;
            }
        }

        internal bool IsInvariantCulture
        {
            get
            {
                return String.IsNullOrEmpty(this.SNAME);
            }
        }

        // Get an instance of our default calendar
        internal Calendar DefaultCalendar
        {
            get
            {
                CalendarId defaultCalId = (CalendarId) GetLocaleInfo(LocaleNumberData.CalendarType);

                if (defaultCalId == 0)
                {
                    defaultCalId = this.CalendarIds[0];
                }

                return CultureInfo.GetCalendarInstance(defaultCalId);
            }
        }

        // All of our era names
        internal String[] EraNames(CalendarId calendarId)
        {
            Contract.Assert(calendarId > 0, "[CultureData.saEraNames] Expected Calendar.ID > 0");

            return this.GetCalendar(calendarId).saEraNames;
        }

        internal String[] AbbrevEraNames(CalendarId calendarId)
        {
            Contract.Assert(calendarId > 0, "[CultureData.saAbbrevEraNames] Expected Calendar.ID > 0");

            return this.GetCalendar(calendarId).saAbbrevEraNames;
        }

        internal String[] AbbreviatedEnglishEraNames(CalendarId calendarId)
        {
            Contract.Assert(calendarId > 0, "[CultureData.saAbbrevEraNames] Expected Calendar.ID > 0");

            return this.GetCalendar(calendarId).saAbbrevEnglishEraNames;
        }

        //// String array DEFAULTS
        //// Note: GetDTFIOverrideValues does the user overrides for these, so we don't have to.


        // Time separator (derived from time format)
        internal String TimeSeparator
        {
            get
            {
                if (sTimeSeparator == null)
                {
                    string longTimeFormat = GetTimeFormatString();
                    if (String.IsNullOrEmpty(longTimeFormat))
                    {
                        longTimeFormat = LongTimes[0];
                    }

                    // Compute STIME from time format
                    sTimeSeparator = GetTimeSeparator(longTimeFormat);
                }
                return sTimeSeparator;
            }
        }

        // Date separator (derived from short date format)
        internal String DateSeparator(CalendarId calendarId)
        {
            return GetDateSeparator(ShortDates(calendarId)[0]);
        }

        //////////////////////////////////////
        // Helper Functions to get derived properties //
        //////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////
        //
        // Unescape a NLS style quote string
        //
        // This removes single quotes:
        //      'fred' -> fred
        //      'fred -> fred
        //      fred' -> fred
        //      fred's -> freds
        //
        // This removes the first \ of escaped characters:
        //      fred\'s -> fred's
        //      a\\b -> a\b
        //      a\b -> ab
        //
        // We don't build the stringbuilder unless we find a ' or a \.  If we find a ' or a \, we
        // always build a stringbuilder because we need to remove the ' or \.
        //
        ////////////////////////////////////////////////////////////////////////////
        private static String UnescapeNlsString(String str, int start, int end)
        {
            Contract.Requires(str != null);
            Contract.Requires(start >= 0);
            Contract.Requires(end >= 0);
            StringBuilder result = null;

            for (int i = start; i < str.Length && i <= end; i++)
            {
                switch (str[i])
                {
                    case '\'':
                        if (result == null)
                        {
                            result = new StringBuilder(str, start, i - start, str.Length);
                        }
                        break;
                    case '\\':
                        if (result == null)
                        {
                            result = new StringBuilder(str, start, i - start, str.Length);
                        }
                        ++i;
                        if (i < str.Length)
                        {
                            result.Append(str[i]);
                        }
                        break;
                    default:
                        if (result != null)
                        {
                            result.Append(str[i]);
                        }
                        break;
                }
            }

            if (result == null)
                return (str.Substring(start, end - start + 1));

            return (result.ToString());
        }

        private static String GetTimeSeparator(String format)
        {
            // Time format separator (ie: : in 12:39:00)
            //
            // We calculate this from the provided time format
            //

            //
            //  Find the time separator so that we can pretend we know STIME.
            //
            return GetSeparator(format, "Hhms");
        }

        private static String GetDateSeparator(String format)
        {
            // Date format separator (ie: / in 9/1/03)
            //
            // We calculate this from the provided short date
            //

            //
            //  Find the date separator so that we can pretend we know SDATE.
            //
            return GetSeparator(format, "dyM");
        }

        private static string GetSeparator(string format, string timeParts)
        {
            int index = IndexOfTimePart(format, 0, timeParts);

            if (index != -1)
            {
                // Found a time part, find out when it changes
                char cTimePart = format[index];

                do
                {
                    index++;
                } while (index < format.Length && format[index] == cTimePart);

                int separatorStart = index;

                // Now we need to find the end of the separator
                if (separatorStart < format.Length)
                {
                    int separatorEnd = IndexOfTimePart(format, separatorStart, timeParts);
                    if (separatorEnd != -1)
                    {
                        // From [separatorStart, count) is our string, except we need to unescape
                        return UnescapeNlsString(format, separatorStart, separatorEnd - 1);
                    }
                }
            }

            return String.Empty;
        }

        private static int IndexOfTimePart(string format, int startIndex, string timeParts)
        {
            Contract.Assert(startIndex >= 0, "startIndex cannot be negative");
            Contract.Assert(timeParts.IndexOfAny(new char[] { '\'', '\\' }) == -1, "timeParts cannot include quote characters");
            bool inQuote = false;
            for (int i = startIndex; i < format.Length; ++i)
            {
                // See if we have a time Part
                if (!inQuote && timeParts.IndexOf(format[i]) != -1)
                {
                    return i;
                }
                switch (format[i])
                {
                    case '\\':
                        if (i + 1 < format.Length)
                        {
                            ++i;
                            switch (format[i])
                            {
                                case '\'':
                                case '\\':
                                    break;
                                default:
                                    --i; //backup since we will move over this next
                                    break;
                            }
                        }
                        break;
                    case '\'':
                        inQuote = !inQuote;
                        break;
                }
            }

            return -1;
        }

        private static bool IsCustomCultureId(int cultureId)
        {
            return (cultureId == LOCALE_CUSTOM_DEFAULT || cultureId == LOCALE_CUSTOM_UNSPECIFIED);
        }

        internal void GetNFIValues(NumberFormatInfo nfi)
        {
            if (this.IsInvariantCulture)
            {
                nfi.positiveSign = this.sPositiveSign;
                nfi.negativeSign = this.sNegativeSign;

                nfi.numberGroupSeparator = this.sThousandSeparator;
                nfi.numberDecimalSeparator = this.sDecimalSeparator;
                nfi.numberDecimalDigits = this.iDigits;
                nfi.numberNegativePattern = this.iNegativeNumber;

                nfi.currencySymbol = this.sCurrency;
                nfi.currencyGroupSeparator = this.sMonetaryThousand;
                nfi.currencyDecimalSeparator = this.sMonetaryDecimal;
                nfi.currencyDecimalDigits = this.iCurrencyDigits;
                nfi.currencyNegativePattern = this.iNegativeCurrency;
                nfi.currencyPositivePattern = this.iCurrency;
            }
            else
            {
                Contract.Assert(this.sWindowsName != null, "[CultureData.GetNFIValues] Expected this.sWindowsName to be populated by already");
                // String values
                nfi.positiveSign = GetLocaleInfo(LocaleStringData.PositiveSign);
                nfi.negativeSign = GetLocaleInfo(LocaleStringData.NegativeSign);

                nfi.numberDecimalSeparator = GetLocaleInfo(LocaleStringData.DecimalSeparator);
                nfi.numberGroupSeparator = GetLocaleInfo(LocaleStringData.ThousandSeparator);
                nfi.currencyGroupSeparator = GetLocaleInfo(LocaleStringData.MonetaryThousandSeparator);
                nfi.currencyDecimalSeparator = GetLocaleInfo(LocaleStringData.MonetaryDecimalSeparator);
                nfi.currencySymbol = GetLocaleInfo(LocaleStringData.MonetarySymbol);

                // Numeric values
                nfi.numberDecimalDigits = GetLocaleInfo(LocaleNumberData.FractionalDigitsCount);
                nfi.currencyDecimalDigits = GetLocaleInfo(LocaleNumberData.MonetaryFractionalDigitsCount);
                nfi.currencyPositivePattern = GetLocaleInfo(LocaleNumberData.PositiveMonetaryNumberFormat);
                nfi.currencyNegativePattern = GetLocaleInfo(LocaleNumberData.NegativeMonetaryNumberFormat);
                nfi.numberNegativePattern = GetLocaleInfo(LocaleNumberData.NegativeNumberFormat);

                // LOCALE_SNATIVEDIGITS (array of 10 single character strings).
                string digits = GetLocaleInfo(LocaleStringData.Digits);
                nfi.nativeDigits = new string[10];
                for (int i = 0; i < nfi.nativeDigits.Length; i++)
                {
                    nfi.nativeDigits[i] = new string(digits[i], 1);
                }
            }

            //
            // Gather additional data
            //
            nfi.numberGroupSizes = this.WAGROUPING;
            nfi.currencyGroupSizes = this.WAMONGROUPING;

            // prefer the cached value since these do not have user overrides
            nfi.percentNegativePattern = this.INEGATIVEPERCENT;
            nfi.percentPositivePattern = this.IPOSITIVEPERCENT;
            nfi.percentSymbol = this.SPERCENT;
            nfi.perMilleSymbol = this.SPERMILLE;

            nfi.negativeInfinitySymbol = this.SNEGINFINITY;
            nfi.positiveInfinitySymbol = this.SPOSINFINITY;
            nfi.nanSymbol = this.SNAN;

            //
            // We don't have percent values, so use the number values
            //
            nfi.percentDecimalDigits = nfi.numberDecimalDigits;
            nfi.percentDecimalSeparator = nfi.numberDecimalSeparator;
            nfi.percentGroupSizes = nfi.numberGroupSizes;
            nfi.percentGroupSeparator = nfi.numberGroupSeparator;

            //
            // Clean up a few odd values
            //

            // Windows usually returns an empty positive sign, but we like it to be "+"
            if (nfi.positiveSign == null || nfi.positiveSign.Length == 0) nfi.positiveSign = "+";

            //Special case for Italian.  The currency decimal separator in the control panel is the empty string. When the user
            //specifies C4 as the currency format, this results in the number apparently getting multiplied by 10000 because the
            //decimal point doesn't show up.  Our default currency format will never use nfi.
            if (nfi.currencyDecimalSeparator == null || nfi.currencyDecimalSeparator.Length == 0)
            {
                nfi.currencyDecimalSeparator = nfi.numberDecimalSeparator;
            }
        }

        // Helper
        // This is ONLY used for caching names and shouldn't be used for anything else
        internal static string AnsiToLower(string testString)
        {
            StringBuilder sb = new StringBuilder(testString.Length);

            for (int ich = 0; ich < testString.Length; ich++)
            {
                char ch = testString[ich];
                sb.Append(ch <= 'Z' && ch >= 'A' ? (char)(ch - 'A' + 'a') : ch);
            }

            return (sb.ToString());
        }

        /// <remarks>
        /// The numeric values of the enum members match their Win32 counterparts.  The CultureData Win32 PAL implementation
        /// takes a dependency on this fact, in order to prevent having to construct a mapping from internal values to LCTypes.
        /// </remarks>
        private enum LocaleStringData : uint
        {
            /// <summary>localized name of locale, eg "German (Germany)" in UI language (coresponds to LOCALE_SLOCALIZEDDISPLAYNAME)</summary>
            LocalizedDisplayName = 0x00000002,
            /// <summary>Display name (language + country usually) in English, eg "German (Germany)" (coresponds to LOCALE_SENGLISHDISPLAYNAME)</summary>
            EnglishDisplayName = 0x00000072,
            /// <summary>Display name in native locale language, eg "Deutsch (Deutschland) (coresponds to LOCALE_SNATIVEDISPLAYNAME)</summary>
            NativeDisplayName = 0x00000073,
            /// <summary>Language Display Name for a language, eg "German" in UI language (coresponds to LOCALE_SLOCALIZEDLANGUAGENAME)</summary>
            LocalizedLanguageName = 0x0000006f,
            /// <summary>English name of language, eg "German" (coresponds to LOCALE_SENGLISHLANGUAGENAME)</summary>
            EnglishLanguageName = 0x00001001,
            /// <summary>native name of language, eg "Deutsch" (coresponds to LOCALE_SNATIVELANGUAGENAME)</summary>
            NativeLanguageName = 0x00000004,
            /// <summary>English name of country, eg "Germany" (coresponds to LOCALE_SENGLISHCOUNTRYNAME)</summary>
            EnglishCountryName = 0x00001002,
            /// <summary>native name of country, eg "Deutschland" (coresponds to LOCALE_SNATIVECOUNTRYNAME)</summary>
            NativeCountryName = 0x00000008,
            /// <summary>list item separator (coresponds to LOCALE_SLIST)</summary>
            ListSeparator = 0x0000000C,
            /// <summary>decimal separator (coresponds to LOCALE_SDECIMAL)</summary>
            DecimalSeparator = 0x0000000E,
            /// <summary>thousand separator (coresponds to LOCALE_STHOUSAND)</summary>
            ThousandSeparator = 0x0000000F,
            /// <summary>digit grouping (coresponds to LOCALE_SGROUPING)</summary>
            Digits = 0x00000013,
            /// <summary>local monetary symbol (coresponds to LOCALE_SCURRENCY)</summary>
            MonetarySymbol = 0x00000014,
            /// <summary>uintl monetary symbol (coresponds to LOCALE_SINTLSYMBOL)</summary>
            Iso4217MonetarySymbol = 0x00000015,
            /// <summary>monetary decimal separator (coresponds to LOCALE_SMONDECIMALSEP)</summary>
            MonetaryDecimalSeparator = 0x00000016,
            /// <summary>monetary thousand separator (coresponds to LOCALE_SMONTHOUSANDSEP)</summary>
            MonetaryThousandSeparator = 0x00000017,
            /// <summary>AM designator (coresponds to LOCALE_S1159)</summary>
            AMDesignator = 0x00000028,
            /// <summary>PM designator (coresponds to LOCALE_S2359)</summary>
            PMDesignator = 0x00000029,
            /// <summary>positive sign (coresponds to LOCALE_SPOSITIVESIGN)</summary>
            PositiveSign = 0x00000050,
            /// <summary>negative sign (coresponds to LOCALE_SNEGATIVESIGN)</summary>
            NegativeSign = 0x00000051,
            /// <summary>ISO abbreviated language name (coresponds to LOCALE_SISO639LANGNAME)</summary>
            Iso639LanguageName = 0x00000059,
            /// <summary>ISO abbreviated country name (coresponds to LOCALE_SISO3166CTRYNAME)</summary>
            Iso3166CountryName = 0x0000005A,
            /// <summary>Not a Number (coresponds to LOCALE_SNAN)</summary>
            NaNSymbol = 0x00000069,
            /// <summary>+ Infinity (coresponds to LOCALE_SPOSINFINITY)</summary>
            PositiveInfinitySymbol = 0x0000006a,
            /// <summary>- Infinity (coresponds to LOCALE_SNEGINFINITY)</summary>
            NegativeInfinitySymbol = 0x0000006b,
            /// <summary>Fallback name for resources (coresponds to LOCALE_SPARENT)</summary>
            ParentName = 0x0000006d,
            /// <summary>Returns the percent symbol (coresponds to LOCALE_SPERCENT)</summary>
            PercentSymbol = 0x00000076,
            /// <summary>Returns the permille (U+2030) symbol (coresponds to LOCALE_SPERMILLE)</summary>
            PerMilleSymbol = 0x00000077
        }

        /// <remarks>
        /// The numeric values of the enum members match their Win32 counterparts.  The CultureData Win32 PAL implementation
        /// takes a dependency on this fact, in order to prevent having to construct a mapping from internal values to LCTypes.
        /// </remarks>
        private enum LocaleGroupingData : uint
        {
            /// <summary>digit grouping (coresponds to LOCALE_SGROUPING)</summary>
            Digit = 0x00000010,
            /// <summary>monetary grouping (coresponds to LOCALE_SMONGROUPING)</summary>
            Monetary = 0x00000018,
        }

        /// <remarks>
        /// The numeric values of the enum members match their Win32 counterparts.  The CultureData Win32 PAL implementation
        /// takes a dependency on this fact, in order to prevent having to construct a mapping from internal values to LCTypes.
        /// </remarks>
        private enum LocaleNumberData : uint
        {
            /// <summary>language id (coresponds to LOCALE_ILANGUAGE)</summary>
            LanguageId = 0x00000001,
            /// <summary>0 = metric, 1 = US (coresponds to LOCALE_IMEASURE)</summary>
            MeasurementSystem = 0x0000000D,
            /// <summary>number of fractional digits (coresponds to LOCALE_IDIGITS)</summary>
            FractionalDigitsCount = 0x00000011,
            /// <summary>negative number mode (coresponds to LOCALE_INEGNUMBER)</summary>
            NegativeNumberFormat = 0x00001010,
            /// <summary># local monetary digits (coresponds to LOCALE_ICURRDIGITS)</summary>
            MonetaryFractionalDigitsCount = 0x00000019,
            /// <summary>positive currency mode (coresponds to LOCALE_ICURRENCY)</summary>
            PositiveMonetaryNumberFormat = 0x0000001B,
            /// <summary>negative currency mode (coresponds to LOCALE_INEGCURR)</summary>
            NegativeMonetaryNumberFormat = 0x0000001C,
            /// <summary>type of calendar specifier (coresponds to LOCALE_ICALENDARTYPE)</summary>
            CalendarType = 0x00001009,
            /// <summary>first day of week specifier (coresponds to LOCALE_IFIRSTDAYOFWEEK)</summary>
            FirstDayOfWeek = 0x0000100C,
            /// <summary>first week of year specifier (coresponds to LOCALE_IFIRSTWEEKOFYEAR)</summary>
            FirstWeekOfYear = 0x0000100D,
            /// <summary>
            /// Returns one of the following 4 reading layout values:
            ///  0 - Left to right (eg en-US)
            ///  1 - Right to left (eg arabic locales)
            ///  2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
            ///  3 - Vertical top to bottom with columns proceeding to the right
            /// (coresponds to LOCALE_IREADINGLAYOUT)
            /// </summary>
            ReadingLayout = 0x00000070,
            /// <summary>Returns 0-11 for the negative percent format (coresponds to LOCALE_INEGATIVEPERCENT)</summary>
            NegativePercentFormat = 0x00000074,
            /// <summary>Returns 0-3 for the positive percent format (coresponds to LOCALE_IPOSITIVEPERCENT)</summary>
            PositivePercentFormat = 0x00000075
        }
    }
}
