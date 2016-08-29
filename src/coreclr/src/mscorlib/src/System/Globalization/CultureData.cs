// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
#if !FEATURE_CORECLR
    using System.Reflection;
    using System.Resources;
#endif
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
    using System.Security;

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

    // StructLayout is needed here otherwise compiler can re-arrange the fields.
    // We have to keep this in-sync with the definition in comnlsinfo.h
    //
    // WARNING WARNING WARNING
    //
    // WARNING: Anything changed here also needs to be updated on the native side (object.h see type CultureDataBaseObject)
    // WARNING: The type loader will rearrange class member offsets so the mscorwks!CultureDataBaseObject
    // WARNING: must be manually structured to match the true loaded class layout
    //
    [FriendAccessAllowed]
    internal class CultureData
    {
        const int undef = -1;

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
        private int iGeoId = undef; // GeoId
        private String sLocalizedCountry; // localized country name
        private String sEnglishCountry; // english country name (RegionInfo)
        private String sNativeCountry; // native country name
        private String sISO3166CountryName; // ISO 3166 (RegionInfo), ie: US

        // Numbers
        private String sPositiveSign; // (user can override) positive sign
        private String sNegativeSign; // (user can override) negative sign
        private String[] saNativeDigits; // (user can override) native characters for digits 0-9
        // (nfi populates these 5, don't have to be = undef)
        private int iDigitSubstitution; // (user can override) Digit substitution 0=context, 1=none/arabic, 2=Native/national (2 seems to be unused)
        private int iLeadingZeros; // (user can override) leading zeros 0 = no leading zeros, 1 = leading zeros
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
        private String sEnglishCurrency; // English name for this currency
        private String sNativeCurrency; // Native name for this currency
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
        //        private int    iPaperSize               ; // default paper size (RegionInfo)

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
        private volatile int[] waCalendars; // all available calendar type(s).  The first one is the default calendar

        // Store for specific data about each calendar
        private CalendarData[] calendars; // Store for specific calendar data

        // Text information
        private int iReadingLayout = undef; // Reading layout data
        // 0 - Left to right (eg en-US)
        // 1 - Right to left (eg arabic locales)
        // 2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
        // 3 - Vertical top to bottom with columns proceeding to the right

        private String sTextInfo; // Text info name to use for custom
        private String sCompareInfo; // Compare info name (including sorting key) to use if custom
        private String sScripts; // Typical Scripts for this locale (latn;cyrl; etc)

        private int iDefaultAnsiCodePage = undef; // default ansi code page ID (ACP)
        private int iDefaultOemCodePage = undef; // default oem code page ID (OCP or OEM)
        private int iDefaultMacCodePage = undef; // default macintosh code page
        private int iDefaultEbcdicCodePage = undef; // default EBCDIC code page

        // These are desktop only, not coreclr
        private int    iLanguage; // locale ID (0409) - NO sort information
        private String sAbbrevLang; // abbreviated language name (Windows Language Name) ex: ENU
        private String sAbbrevCountry; // abbreviated country name (RegionInfo) (Windows Region Name) ex: USA
        private String sISO639Language2; // 3 char ISO 639 lang name 2 ex: eng
        private String sISO3166CountryName2; // 3 char ISO 3166 country name 2 2(RegionInfo) ex: USA (ISO)
        private int    iInputLanguageHandle=undef;// input language handle
        private String sConsoleFallbackName; // The culture name for the console fallback UI culture
        private String sKeyboardsToInstall; // Keyboard installation string.
        private String fontSignature; // Font signature (16 WORDS)

        // The bools all need to be in one spot
        private bool bUseOverrides; // use user overrides?
        private bool bNeutral; // Flags for the culture (ie: neutral or not right now)
        private bool bWin32Installed; // Flags indicate if the culture is Win32 installed
        private bool bFramework; // Flags for indicate if the culture is one of Whidbey cultures

        // Region Name to Culture Name mapping table
        // (In future would be nice to be in registry or something)

        //Using a property so we avoid creating the dictionary untill we need it
        private static Dictionary<string, string> RegionNames
        {
            get 
            {
                if (s_RegionNames == null)
                {
                    var regionNames = new Dictionary<string, string> {
			{ "029", "en-029" },
			{ "AE",  "ar-AE" },
			{ "AF",  "prs-AF" },
			{ "AL",  "sq-AL" },
			{ "AM",  "hy-AM" },
			{ "AR",  "es-AR" },
			{ "AT",  "de-AT" },
			{ "AU",  "en-AU" },
			{ "AZ",  "az-Cyrl-AZ" },
			{ "BA",  "bs-Latn-BA" },
			{ "BD",  "bn-BD" },
			{ "BE",  "nl-BE" },
			{ "BG",  "bg-BG" },
			{ "BH",  "ar-BH" },
			{ "BN",  "ms-BN" },
			{ "BO",  "es-BO" },
			{ "BR",  "pt-BR" },
			{ "BY",  "be-BY" },
			{ "BZ",  "en-BZ" },
			{ "CA",  "en-CA" },
			{ "CH",  "it-CH" },
			{ "CL",  "es-CL" },
			{ "CN",  "zh-CN" },
			{ "CO",  "es-CO" },
			{ "CR",  "es-CR" },
			{ "CS",  "sr-Cyrl-CS" },
			{ "CZ",  "cs-CZ" },
			{ "DE",  "de-DE" },
			{ "DK",  "da-DK" },
			{ "DO",  "es-DO" },
			{ "DZ",  "ar-DZ" },
			{ "EC",  "es-EC" },
			{ "EE",  "et-EE" },
			{ "EG",  "ar-EG" },
			{ "ES",  "es-ES" },
			{ "ET",  "am-ET" },
			{ "FI",  "fi-FI" },
			{ "FO",  "fo-FO" },
			{ "FR",  "fr-FR" },
			{ "GB",  "en-GB" },
			{ "GE",  "ka-GE" },
			{ "GL",  "kl-GL" },
			{ "GR",  "el-GR" },
			{ "GT",  "es-GT" },
			{ "HK",  "zh-HK" },
			{ "HN",  "es-HN" },
			{ "HR",  "hr-HR" },
			{ "HU",  "hu-HU" },
			{ "ID",  "id-ID" },
			{ "IE",  "en-IE" },
			{ "IL",  "he-IL" },
			{ "IN",  "hi-IN" },
			{ "IQ",  "ar-IQ" },
			{ "IR",  "fa-IR" },
			{ "IS",  "is-IS" },
			{ "IT",  "it-IT" },
			{ "IV",  "" },
			{ "JM",  "en-JM" },
			{ "JO",  "ar-JO" },
			{ "JP",  "ja-JP" },
			{ "KE",  "sw-KE" },
			{ "KG",  "ky-KG" },
			{ "KH",  "km-KH" },
			{ "KR",  "ko-KR" },
			{ "KW",  "ar-KW" },
			{ "KZ",  "kk-KZ" },
			{ "LA",  "lo-LA" },
			{ "LB",  "ar-LB" },
			{ "LI",  "de-LI" },
			{ "LK",  "si-LK" },
			{ "LT",  "lt-LT" },
			{ "LU",  "lb-LU" },
			{ "LV",  "lv-LV" },
			{ "LY",  "ar-LY" },
			{ "MA",  "ar-MA" },
			{ "MC",  "fr-MC" },
			{ "ME",  "sr-Latn-ME" },
			{ "MK",  "mk-MK" },
			{ "MN",  "mn-MN" },
			{ "MO",  "zh-MO" },
			{ "MT",  "mt-MT" },
			{ "MV",  "dv-MV" },
			{ "MX",  "es-MX" },
			{ "MY",  "ms-MY" },
			{ "NG",  "ig-NG" },
			{ "NI",  "es-NI" },
			{ "NL",  "nl-NL" },
			{ "NO",  "nn-NO" },
			{ "NP",  "ne-NP" },
			{ "NZ",  "en-NZ" },
			{ "OM",  "ar-OM" },
			{ "PA",  "es-PA" },
			{ "PE",  "es-PE" },
			{ "PH",  "en-PH" },
			{ "PK",  "ur-PK" },
			{ "PL",  "pl-PL" },
			{ "PR",  "es-PR" },
			{ "PT",  "pt-PT" },
			{ "PY",  "es-PY" },
			{ "QA",  "ar-QA" },
			{ "RO",  "ro-RO" },
			{ "RS",  "sr-Latn-RS" },
			{ "RU",  "ru-RU" },
			{ "RW",  "rw-RW" },
			{ "SA",  "ar-SA" },
			{ "SE",  "sv-SE" },
			{ "SG",  "zh-SG" },
			{ "SI",  "sl-SI" },
			{ "SK",  "sk-SK" },
			{ "SN",  "wo-SN" },
			{ "SV",  "es-SV" },
			{ "SY",  "ar-SY" },
			{ "TH",  "th-TH" },
			{ "TJ",  "tg-Cyrl-TJ" },
			{ "TM",  "tk-TM" },
			{ "TN",  "ar-TN" },
			{ "TR",  "tr-TR" },
			{ "TT",  "en-TT" },
			{ "TW",  "zh-TW" },
			{ "UA",  "uk-UA" },
			{ "US",  "en-US" },
			{ "UY",  "es-UY" },
			{ "UZ",  "uz-Cyrl-UZ" },
			{ "VE",  "es-VE" },
			{ "VN",  "vi-VN" },
			{ "YE",  "ar-YE" },
			{ "ZA",  "af-ZA" },
			{ "ZW",  "en-ZW" }
		    };
		    s_RegionNames = regionNames;
		}
                return s_RegionNames;
            }
        }
        private volatile static Dictionary<string, string> s_RegionNames;

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

		    // Call the native code to get the value of bWin32Installed.
		    // For versions <= Vista, we set this to false for compatibility with v2.
		    // For Windows 7, the flag is true.
		    invariant.bUseOverrides = false;
		    invariant.sRealName = "";

		    // Ask the native code to fill it out for us, we only need the field IsWin32Installed
		    nativeInitCultureData(invariant);

		    // Basics
		    // Note that we override the resources since this IS NOT supposed to change (by definition)
		    invariant.bUseOverrides = false;
		    invariant.sRealName = "";                     // Name you passed in (ie: en-US, en, or de-DE_phoneb)
		    invariant.sWindowsName = "";                     // Name OS thinks the object is (ie: de-DE_phoneb, or en-US (even if en was passed in))

		    // Identity
		    invariant.sName = "";                     // locale name (ie: en-us)
		    invariant.sParent = "";                     // Parent name (which may be a custom locale/culture)
		    invariant.bNeutral = false;                   // Flags for the culture (ie: neutral or not right now)
		    // Don't set invariant.bWin32Installed, we used nativeInitCultureData for that.
		    invariant.bFramework = true;

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
		    // Unused for now:
		    //            invariant.iCountry              =1;                      // country code (RegionInfo)
		    invariant.iGeoId = 244;                    // GeoId (Windows Only)
		    invariant.sEnglishCountry = "Invariant Country";    // english country name (RegionInfo)
		    invariant.sNativeCountry = "Invariant Country";    // native country name (Windows Only)
		    invariant.sISO3166CountryName = "IV";                   // (RegionInfo), ie: US

		    // Numbers
		    invariant.sPositiveSign = "+";                    // positive sign
		    invariant.sNegativeSign = "-";                    // negative sign
		    invariant.saNativeDigits = new String[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }; // native characters for digits 0-9
		    invariant.iDigitSubstitution = 1;                      // Digit substitution 0=context, 1=none/arabic, 2=Native/national (2 seems to be unused) (Windows Only)
		    invariant.iLeadingZeros = 1;                      // leading zeros 0=no leading zeros, 1=leading zeros
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
		    invariant.sCurrency = "\x00a4";                // local monetary symbol "¤: for international monetary symbol
		    invariant.sIntlMonetarySymbol = "XDR";                  // international monetary symbol (RegionInfo)
		    invariant.sEnglishCurrency = "International Monetary Fund"; // English name for this currency (Windows Only)
		    invariant.sNativeCurrency = "International Monetary Fund"; // Native name for this currency (Windows Only)
		    invariant.iCurrencyDigits = 2;                      // # local monetary fractional digits
		    invariant.iCurrency = 0;                      // positive currency format
		    invariant.iNegativeCurrency = 0;                      // negative currency format
		    invariant.waMonetaryGrouping = new int[] { 3 };          // monetary grouping of digits
		    invariant.sMonetaryDecimal = ".";                    // monetary decimal separator
		    invariant.sMonetaryThousand = ",";                    // monetary thousands separator

		    // Misc
		    invariant.iMeasure = 0;                      // system of measurement 0=metric, 1=US (RegionInfo)
		    invariant.sListSeparator = ",";                    // list separator
		    // Unused for now:
		    //            invariant.iPaperSize            =9;                      // default paper size (RegionInfo)
		    //            invariant.waFontSignature       ="\x0002\x0000\x0000\x0000\x0000\x0000\x0000\x8000\x0001\x0000\x0000\x8000\x0001\x0000\x0000\x8000"; // Font signature (16 WORDS) (Windows Only)

		    // Time
		    invariant.sAM1159 = "AM";                   // AM designator
		    invariant.sPM2359 = "PM";                   // PM designator
		    invariant.saLongTimes = new String[] { "HH:mm:ss" };                             // time format
		    invariant.saShortTimes = new String[] { "HH:mm", "hh:mm tt", "H:mm", "h:mm tt" }; // short time format
		    invariant.saDurationFormats = new String[] { "HH:mm:ss" };                             // time duration format

		    // Calendar specific data
		    invariant.iFirstDayOfWeek = 0;                      // first day of week
		    invariant.iFirstWeekOfYear = 0;                      // first week of year
		    invariant.waCalendars = new int[] { (int)CalendarId.GREGORIAN };       // all available calendar type(s).  The first one is the default calendar

		    // Store for specific data about each calendar
		    invariant.calendars = new CalendarData[CalendarData.MAX_CALENDARS];
		    invariant.calendars[0] = CalendarData.Invariant;

		    // Text information
		    invariant.iReadingLayout = 0;                      // Reading Layout = RTL

		    invariant.sTextInfo = "";                     // Text info name to use for custom
		    invariant.sCompareInfo = "";                     // Compare info name (including sorting key) to use if custom
		    invariant.sScripts = "Latn;";                // Typical Scripts for this locale (latn,cyrl, etc)

		    invariant.iLanguage = 0x007f;                 // locale ID (0409) - NO sort information
		    invariant.iDefaultAnsiCodePage = 1252;                   // default ansi code page ID (ACP)
		    invariant.iDefaultOemCodePage = 437;                    // default oem code page ID (OCP or OEM)
		    invariant.iDefaultMacCodePage = 10000;                  // default macintosh code page
		    invariant.iDefaultEbcdicCodePage = 037;                    // default EBCDIC code page
		    invariant.sAbbrevLang = "IVL";                  // abbreviated language name (Windows Language Name)
		    invariant.sAbbrevCountry = "IVC";                  // abbreviated country name (RegionInfo) (Windows Region Name)
		    invariant.sISO639Language2 = "ivl";                  // 3 char ISO 639 lang name 2
		    invariant.sISO3166CountryName2 = "ivc";                  // 3 char ISO 3166 country name 2 2(RegionInfo)
		    invariant.iInputLanguageHandle = 0x007f;                 // input language handle
		    invariant.sConsoleFallbackName = "";                     // The culture name for the console fallback UI culture
		    invariant.sKeyboardsToInstall = "0409:00000409";        // Keyboard installation string.
		    // Remember it
                    s_Invariant = invariant;
                }
                return s_Invariant;
            }
        }
        private volatile static CultureData s_Invariant;


#if !FEATURE_CORECLR
        internal static volatile ResourceSet MscorlibResourceSet;
#endif

#if !FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        private static bool IsResourcePresent(String resourceKey)
        {
            if (MscorlibResourceSet == null)
            {
                MscorlibResourceSet = new ResourceSet(typeof(Environment).Assembly.GetManifestResourceStream("mscorlib.resources"));
            }
            return MscorlibResourceSet.GetString(resourceKey) != null;
        }
#endif

        ///////////////
        // Constructors //
        ///////////////
        // Cache of cultures we've already looked up
        private static volatile Dictionary<String, CultureData> s_cachedCultures;

        [FriendAccessAllowed]
        internal static CultureData GetCultureData(String cultureName, bool useUserOverride)
        {
            // First do a shortcut for Invariant
            if (String.IsNullOrEmpty(cultureName))
            {
                return CultureData.Invariant;
            }

            // Try the hash table first
            String hashName = AnsiToLower(useUserOverride ? cultureName : cultureName + '*');
            Dictionary<String, CultureData> tempHashTable = s_cachedCultures;
            if (tempHashTable == null)
            {
                // No table yet, make a new one
                tempHashTable = new Dictionary<String, CultureData>();
            }
            else
            {
                // Check the hash table
                CultureData retVal;
                lock (((ICollection)tempHashTable).SyncRoot)
                {
                    tempHashTable.TryGetValue(hashName, out retVal);
                }
                if (retVal != null)
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
            lock (((ICollection)tempHashTable).SyncRoot)
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
#if !FEATURE_CORECLR
                if (culture.InitCompatibilityCultureData() == false
                 && culture.InitLegacyAlternateSortData() == false)
#endif
                {
                    return null;
                }
            }

            return culture;
        }

        private bool InitCultureData()
        {
            if (nativeInitCultureData(this) == false)
            {
                return false;
            }

#if !FEATURE_CORECLR
            if (CultureInfo.IsTaiwanSku)
            {
                TreatTaiwanParentChainAsHavingTaiwanAsSpecific();
            }
#endif
            return true;
        }

#if !FEATURE_CORECLR
        [System.Security.SecuritySafeCritical]
        private void TreatTaiwanParentChainAsHavingTaiwanAsSpecific()
        {
            if (IsNeutralInParentChainOfTaiwan() && IsOsPriorToWin7() && !IsReplacementCulture)
            {
                // force population of fields that should have information that is
                // different than zh-TW:
                string s = SNATIVELANGUAGE;
                s = SENGLISHLANGUAGE;
                s = SLOCALIZEDLANGUAGE;
                s = STEXTINFO;
                s = SCOMPAREINFO;
                s = FONTSIGNATURE;
                int i = IDEFAULTANSICODEPAGE;
                i = IDEFAULTOEMCODEPAGE;
                i = IDEFAULTMACCODEPAGE;

                this.sSpecificCulture = "zh-TW";
                this.sWindowsName = "zh-TW";
            }
        }

        private bool IsNeutralInParentChainOfTaiwan()
        {
            return this.sRealName == "zh" || this.sRealName == "zh-Hant";
  }

        static readonly Version s_win7Version = new Version(6, 1);
        static private bool IsOsPriorToWin7()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                   Environment.OSVersion.Version < s_win7Version;
        }
        static private bool IsOsWin7OrPrior()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                Environment.OSVersion.Version < new Version(6, 2); // Win7 is 6.1.Build.Revision so we have to check for anything less than 6.2
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
            this.bFramework = true;

            return true;
        }

        private bool InitLegacyAlternateSortData()
        {
            if (!CompareInfo.IsLegacy20SortingBehaviorRequested)
            {
                return false;
            }

            // For V2 compatibility, handle deprecated alternate sorts
            string cultureName = this.sRealName;

            switch (AnsiToLower(cultureName))
            {
                case "ko-kr_unicod":
                    cultureName = "ko-KR_unicod";
                    this.sRealName = "ko-KR";
                    this.iLanguage = 0x00010412;
                    break;
                case "ja-jp_unicod":
                    cultureName = "ja-JP_unicod";
                    this.sRealName = "ja-JP";
                    this.iLanguage = 0x00010411;
                    break;
                case "zh-hk_stroke":
                    cultureName = "zh-HK_stroke";
                    this.sRealName = "zh-HK";
                    this.iLanguage = 0x00020c04;
                    break;
                default:
                    return false;
            }

            if (nativeInitCultureData(this) == false)
            {
                return false;
            }

            this.sRealName = cultureName;
            this.sCompareInfo = cultureName;
            this.bFramework = true;

            return true;
        }

#if FEATURE_WIN32_REGISTRY
        private static String s_RegionKey = @"System\CurrentControlSet\Control\Nls\RegionMapping";
#endif // FEATURE_WIN32_REGISTRY

#endif // !FEATURE_CORECLR
        // Cache of regions we've already looked up
        private static volatile Dictionary<String, CultureData> s_cachedRegions;

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
            Dictionary<String, CultureData> tempHashTable = s_cachedRegions;
            if (tempHashTable == null)
            {
                // No table yet, make a new one
                tempHashTable = new Dictionary<String, CultureData>();
            }
            else
            {
                // Check the hash table
                lock (((ICollection)tempHashTable).SyncRoot)
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
#if !FEATURE_CORECLR
#if FEATURE_WIN32_REGISTRY
            // First try the registry in case there are overrides of our table
            try
            {
                // Open in read-only mode.
                // Use InternalOpenSubKey so that we avoid the security check.
                Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.InternalOpenSubKey(s_RegionKey, false);

                if (key != null)
                {
                    try
                    {
                        Object value = key.InternalGetValue(cultureName, null, false, false);

                        if (value != null)
                        {
                            // Get the name of the locale to try.
                            String specificForRegion = value.ToString();

                            // See if it's real
                            retVal = GetCultureData(specificForRegion, useUserOverride);
                        }
                    }
                    finally
                    {
                        key.Close();
                    }
                }
            }
            // If this fails for any reason, we'll just ignore it, likely it just isn't there.
            catch (ObjectDisposedException) { }
            catch (ArgumentException) { }
#endif // FEATURE_WIN32_REGISTRY
#endif // !FEATURE_CORECLR

            // If not a valid mapping from the registry we'll have to try the hard coded table
            if (retVal == null || (retVal.IsNeutralCulture == true))
            {
                // Not a valid mapping, try the hard coded table
                if (RegionNames.ContainsKey(cultureName))
                {
                    // Make sure we can get culture data for it
                    retVal = GetCultureData(RegionNames[cultureName], useUserOverride);
                }
            }

            // If not found in the hard coded table we'll have to find a culture that works for us
            if (retVal == null || (retVal.IsNeutralCulture == true))
            {
                // Not found in the hard coded table, need to see if we can find a culture that works for us
                // Not a real culture name, see if it matches a region name
                // (we just return the first culture we match)
                CultureInfo[] specifics = SpecificCultures;
                for (int i = 0; i < specifics.Length; i++)
                {
                    if (String.Compare(specifics[i].m_cultureData.SREGIONNAME, cultureName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        // Matched, use this culture
                        retVal = specifics[i].m_cultureData;
                        break;
                    }
                }
            }

            // If we found one we can use, then cash it for next time
            if (retVal != null && (retVal.IsNeutralCulture == false))
            {
                // first add it to the cache
                lock (((ICollection)tempHashTable).SyncRoot)
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

#if FEATURE_USE_LCID
        // Obtain locale name from LCID
        // NOTE: This will get neutral names, unlike the OS API
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern String LCIDToLocaleName(int lcid);

        // We'd rather people use the named version since this doesn't allow custom locales
        internal static CultureData GetCultureData(int culture, bool bUseUserOverride)
        {
            String localeName = null;
            CultureData retVal = null;

#if !FEATURE_CORECLR
            // If V2 legacy sort is requested, then provide deprecated alternate sorts
            if (CompareInfo.IsLegacy20SortingBehaviorRequested)
            {
                switch (culture)
                {
                    case 0x00010412:
                        localeName = "ko-KR_unicod";
                        break;
                    case 0x00010411:
                        localeName = "ja-JP_unicod";
                        break;
                    case 0x00020c04:
                        localeName = "zh-HK_stroke";
                        break;
                }
            }
#endif

            if (localeName == null)
            {
                // Convert the lcid to a name, then use that
                // Note that this'll return neutral names (unlike Vista native API)
                localeName = LCIDToLocaleName(culture);
            }

            // If its not valid, then throw
            if (String.IsNullOrEmpty(localeName))
            {
                // Could be valid for Invariant
                if (culture == 0x007f)
                    return Invariant;
            }
            else
            {
#if !FEATURE_CORECLR
                switch (localeName)
                {
                    // for compatibility with Whidbey, when requesting
                    // a locale from LCID, return the old localeName
                    case "zh-Hans":
                        localeName = "zh-CHS";
                        break;
                    case "zh-Hant":
                        localeName = "zh-CHT";
                        break;
                }
#endif
                // Valid name, use it
                retVal = GetCultureData(localeName, bUseUserOverride);
            }

            // If not successful, throw
            if (retVal == null)
                throw new CultureNotFoundException(
                    "culture", culture, Environment.GetResourceString("Argument_CultureNotSupported"));

            // Return the one we found
            return retVal;
        }
#endif

        // Clear our internal caches
        internal static void ClearCachedData()
        {
            s_cachedCultures = null;
            s_cachedRegions = null;
            s_replacementCultureNames = null;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static CultureInfo[] GetCultures(CultureTypes types)
        {
            // Disable  warning 618: System.Globalization.CultureTypes.FrameworkCultures' is obsolete
#pragma warning disable 618
            // Validate flags
            if ((int)types <= 0 || ((int)types & (int)~(CultureTypes.NeutralCultures | CultureTypes.SpecificCultures |
                                                            CultureTypes.InstalledWin32Cultures | CultureTypes.UserCustomCulture |
                                                            CultureTypes.ReplacementCultures | CultureTypes.WindowsOnlyCultures |
                                                            CultureTypes.FrameworkCultures)) != 0)
            {
                throw new ArgumentOutOfRangeException(
                                "types",
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Environment.GetResourceString("ArgumentOutOfRange_Range"), CultureTypes.NeutralCultures, CultureTypes.FrameworkCultures));
            }

            //
            // CHANGE FROM Whidbey
            //
            // We have deprecated CultureTypes.FrameworkCultures.
            // When this enum is used, we will enumerate Whidbey framework cultures (for compatibility).
            //

            // We have deprecated CultureTypes.WindowsOnlyCultures.
            // When this enum is used, we will return an empty array for this enum.
            if ((types & CultureTypes.WindowsOnlyCultures) != 0)
            {
                // Remove the enum as it is an no-op.
                types &= (~CultureTypes.WindowsOnlyCultures);
            }

            String[] cultureNames = null;

            //
            // Call nativeEnumCultureNames() to get a string array of culture names based on the specified
            // enumeration type.
            //
            // nativeEnumCultureNames is a QCall.  We need to use a reference to return the string array
            // allocated from the QCall.  That ref has to be wrapped as object handle.
            // See vm\qcall.h for details in QCall.
            //

            if (nativeEnumCultureNames((int)types, JitHelpers.GetObjectHandleOnStack(ref cultureNames)) == 0)
            {
                return new CultureInfo[0];
            }

            int arrayLength = cultureNames.Length;

            if ((types & (CultureTypes.NeutralCultures | CultureTypes.FrameworkCultures)) != 0) // add zh-CHT and zh-CHS
            {
                arrayLength += 2;
            }

            CultureInfo[] cultures = new CultureInfo[arrayLength];

            for (int i = 0; i < cultureNames.Length; i++)
            {
                cultures[i] = new CultureInfo(cultureNames[i]);
            }

            if ((types & (CultureTypes.NeutralCultures | CultureTypes.FrameworkCultures)) != 0) // add zh-CHT and zh-CHS
            {
                Contract.Assert(arrayLength == cultureNames.Length + 2, "CultureData.nativeEnumCultureNames() Incorrect array size");
                cultures[cultureNames.Length] = new CultureInfo("zh-CHS");
                cultures[cultureNames.Length + 1] = new CultureInfo("zh-CHT");
            }

#pragma warning restore 618

            return cultures;
        }

        internal static volatile CultureInfo[] specificCultures;

        private static CultureInfo[] SpecificCultures
        {
            get
            {
                if (specificCultures == null)
                    specificCultures = GetCultures(CultureTypes.SpecificCultures);

                return specificCultures;
            }
        }

        internal bool IsReplacementCulture
        {
            get
            {
                return IsReplacementCultureName(this.SNAME);
            }
        }

        internal static volatile String[] s_replacementCultureNames;

        ////////////////////////////////////////////////////////////////////////
        //
        // Cache for the known replacement cultures.
        // This is used by CultureInfo.CultureType to check if a culture is a
        // replacement culture.
        //
        ////////////////////////////////////////////////////////////////////////


        [System.Security.SecuritySafeCritical]  // auto-generated
        private static bool IsReplacementCultureName(String name)
        {
            Contract.Assert(name != null, "IsReplacementCultureName(): name should not be null");
            String[] replacementCultureNames = s_replacementCultureNames;
            if (replacementCultureNames == null)
            {
                if (nativeEnumCultureNames((int)CultureTypes.ReplacementCultures, JitHelpers.GetObjectHandleOnStack(ref replacementCultureNames)) == 0)
                {
                    return false;
                }

                // Even if we don't have any replacement cultures, the returned replacementCultureNames will still an empty string array, not null.
                Contract.Assert(name != null, "IsReplacementCultureName(): replacementCultureNames should not be null");
                Array.Sort(replacementCultureNames);
                s_replacementCultureNames = replacementCultureNames;
            }
            return Array.BinarySearch(replacementCultureNames, name) >= 0;
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
                Contract.Assert(this.sRealName != null, "[CultureData.CultureName] Expected this.sRealName to be populated by COMNlsInfo::nativeInitCultureData already");
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
                //                Contract.Assert(this.sName != null,
                //                    "[CultureData.SNAME] Expected this.sName to be populated by COMNlsInfo::nativeInitCultureData already");
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
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sParent == null)
                {
                    // Ask using the real name, so that we get parents of neutrals
                    this.sParent = DoGetLocaleInfo(this.sRealName, LOCALE_SPARENT);

#if !FEATURE_CORECLR
                    // for compatibility, the chain should be:
                    // zh-CN -> zh-CHS -> zh-Hans -> zh
                    // zh-TW -> zh-CHT -> zh-Hant -> zh
                    Contract.Assert(this.sName != "zh-CHS" && this.sName != "zh-CHT",
                                    "sParent should have been initialized for zh-CHS and zh-CHT when they were constructed, otherwise we get recursion");
                    switch (this.sParent)
                    {
                        case "zh-Hans":
                            this.sParent = "zh-CHS";
                            break;
                        case "zh-Hant":
                            this.sParent = "zh-CHT";
                            break;
                    }
#endif

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
#if !FEATURE_CORECLR
                    String resourceKey = "Globalization.ci_" + this.sName;
                    if (IsResourcePresent(resourceKey))
                    {
                        this.sLocalizedDisplayName = Environment.GetResourceString(resourceKey);
                    }
#endif
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
                            // We have to make the neutral distinction in case the OS returns a specific name
                            if (CultureInfo.UserDefaultUICulture.Name.Equals(Thread.CurrentThread.CurrentUICulture.Name))
                            {
                                this.sLocalizedDisplayName = DoGetLocaleInfo(LOCALE_SLOCALIZEDDISPLAYNAME);
                            }
                            if (String.IsNullOrEmpty(this.sLocalizedDisplayName))
                            {
                                this.sLocalizedDisplayName = this.SNATIVEDISPLAYNAME;
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
#if !FEATURE_CORECLR
                        // differentiate the legacy display names
                        switch (this.sName)
                        {
                            case "zh-CHS":
                            case "zh-CHT":
                                this.sEnglishDisplayName += " Legacy";
                                break;
                        }
#endif

                    }
                    else
                    {
                        this.sEnglishDisplayName = DoGetLocaleInfo(LOCALE_SENGLISHDISPLAYNAME);

                        // if it isn't found build one:
                        if (String.IsNullOrEmpty(this.sEnglishDisplayName))
                        {
                            // Our existing names mostly look like:
                            // "English" + "United States" -> "English (United States)"
                            // "Azeri (Latin)" + "Azerbaijan" -> "Azeri (Latin, Azerbaijan)"
                            if (this.SENGLISHLANGUAGE.EndsWith(')'))
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
#if !FEATURE_CORECLR
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
#endif
                    }
                    else
                    {
#if !FEATURE_CORECLR
                        if (IsIncorrectNativeLanguageForSinhala())
                        {
                            // work around bug in Windows 7 for native name of Sinhala
                            this.sNativeDisplayName ="\x0dc3\x0dd2\x0d82\x0dc4\x0dbd (\x0DC1\x0DCA\x200D\x0DBB\x0DD3\x0020\x0DBD\x0D82\x0D9A\x0DCF)";
                        }
                        else
#endif
                        {
                            this.sNativeDisplayName = DoGetLocaleInfo(LOCALE_SNATIVEDISPLAYNAME);
                        }

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

        // The culture name to be used in CultureInfo.CreateSpecificCulture()
        internal String SSPECIFICCULTURE
        {
            get
            {
                // This got populated when ComNlsInfo::nativeInitCultureData told us we had a culture
                Contract.Assert(this.sSpecificCulture != null, "[CultureData.SSPECIFICCULTURE] Expected this.sSpecificCulture to be populated by COMNlsInfo::nativeInitCultureData already");
                return this.sSpecificCulture;
            }
        }

        /////////////
        // Language //
        /////////////

        // iso 639 language name, ie: en
        internal String SISO639LANGNAME
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sISO639Language == null)
                {
                    this.sISO639Language = DoGetLocaleInfo(LOCALE_SISO639LANGNAME);
                }
                return this.sISO639Language;
            }
        }

        // iso 639 language name, ie: eng
        internal String SISO639LANGNAME2
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sISO639Language2 == null)
                {
                    this.sISO639Language2 = DoGetLocaleInfo(LOCALE_SISO639LANGNAME2);
                }
                return this.sISO639Language2;
            }
        }

        // abbreviated windows language name (ie: enu) (non-standard, avoid this)
        internal String SABBREVLANGNAME
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sAbbrevLang == null)
                {
                    this.sAbbrevLang = DoGetLocaleInfo(LOCALE_SABBREVLANGNAME);
                }
                return this.sAbbrevLang;
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
                    if (CultureInfo.UserDefaultUICulture.Name.Equals(Thread.CurrentThread.CurrentUICulture.Name))
                    {
                        this.sLocalizedLanguage = DoGetLocaleInfo(LOCALE_SLOCALIZEDLANGUAGENAME);
                    }
                    // Some OS's might not have this resource or LCTYPE
                    if (String.IsNullOrEmpty(this.sLocalizedLanguage))
                    {
                        this.sLocalizedLanguage = SNATIVELANGUAGE;
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
                    this.sEnglishLanguage = DoGetLocaleInfo(LOCALE_SENGLISHLANGUAGENAME);
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
#if !FEATURE_CORECLR
                    if (IsIncorrectNativeLanguageForSinhala())
                    {
                        this.sNativeLanguage = "\x0dc3\x0dd2\x0d82\x0dc4\x0dbd";
                    }
                    else
#endif
                    {
                        this.sNativeLanguage = DoGetLocaleInfo(LOCALE_SNATIVELANGUAGENAME);
                    }
                }
                return this.sNativeLanguage;
            }
        }

#if !FEATURE_CORECLR
        private bool IsIncorrectNativeLanguageForSinhala()
        {
            return IsOsWin7OrPrior() 
                && (sName == "si-LK" || sName == "si")
                && !IsReplacementCulture;
        }
#endif

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
                    this.sRegionName = DoGetLocaleInfo(LOCALE_SISO3166CTRYNAME);
                }
                return this.sRegionName;
            }
        }

        // (user can override) country code (RegionInfo)
        internal int ICOUNTRY
        {
            get
            {
                return DoGetLocaleInfoInt(LOCALE_ICOUNTRY);
            }
        }

        // GeoId
        internal int IGEOID
        {
            get
            {
                if (this.iGeoId == undef)
                {
                    this.iGeoId = DoGetLocaleInfoInt(LOCALE_IGEOID);
                }
                return this.iGeoId;
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
#if !FEATURE_CORECLR
                    String resourceKey = "Globalization.ri_" + this.SREGIONNAME;
                    if (IsResourcePresent(resourceKey))
                    {
                        this.sLocalizedCountry = Environment.GetResourceString(resourceKey);
                    }
#endif
                    // If it hasn't been found (Windows 8 and up), fallback to the system
                    if (String.IsNullOrEmpty(this.sLocalizedCountry))
                    {
                        // We have to make the neutral distinction in case the OS returns a specific name
                        if (CultureInfo.UserDefaultUICulture.Name.Equals(Thread.CurrentThread.CurrentUICulture.Name))
                        {
                            this.sLocalizedCountry = DoGetLocaleInfo(LOCALE_SLOCALIZEDCOUNTRYNAME);
                        }
                        if (String.IsNullOrEmpty(this.sLocalizedDisplayName))
                        {
                            this.sLocalizedCountry = SNATIVECOUNTRY;
                        }
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
                    this.sEnglishCountry = DoGetLocaleInfo(LOCALE_SENGLISHCOUNTRYNAME);
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
                    this.sNativeCountry = DoGetLocaleInfo(LOCALE_SNATIVECOUNTRYNAME);
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
                    this.sISO3166CountryName = DoGetLocaleInfo(LOCALE_SISO3166CTRYNAME);
                }
                return this.sISO3166CountryName;
            }
        }

        // ISO 3166 Country Name
        internal String SISO3166CTRYNAME2
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sISO3166CountryName2 == null)
                {
                    this.sISO3166CountryName2 = DoGetLocaleInfo(LOCALE_SISO3166CTRYNAME2);
                }
                return this.sISO3166CountryName2;
            }
        }

        // abbreviated Country Name (windows version, non-standard, avoid)
        internal String SABBREVCTRYNAME
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sAbbrevCountry == null)
                {
                    this.sAbbrevCountry = DoGetLocaleInfo(LOCALE_SABBREVCTRYNAME);
                }
                return this.sAbbrevCountry;
            }
        }

        // Default Country
        private int IDEFAULTCOUNTRY
        {
            get
            {
                return DoGetLocaleInfoInt(LOCALE_IDEFAULTCOUNTRY);
            }
        }

        // Console fallback name (ie: locale to use for console apps for unicode-only locales)
        internal int IINPUTLANGUAGEHANDLE
        {
            get
            {
                if (this.iInputLanguageHandle == undef)
                {
                    if (IsSupplementalCustomCulture)
                    {
                        this.iInputLanguageHandle = 0x0409;
                    }
                    else
                    {
                        // Input Language is same as LCID for built-in cultures
                        this.iInputLanguageHandle = this.ILANGUAGE;
                    }
                }
                return this.iInputLanguageHandle;
            }
        }

        // Console fallback name (ie: locale to use for console apps for unicode-only locales)
        internal String SCONSOLEFALLBACKNAME
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sConsoleFallbackName == null)
                {
                    string consoleFallbackName = DoGetLocaleInfo(LOCALE_SCONSOLEFALLBACKNAME);
                    if (consoleFallbackName == "es-ES_tradnl")
                    {
                        consoleFallbackName = "es-ES";
                    }
                    this.sConsoleFallbackName = consoleFallbackName;
                }
                return this.sConsoleFallbackName;
            }
        }

        /////////////
        // Numbers //
        ////////////

        //                internal String sPositiveSign            ; // (user can override) positive sign
        //                internal String sNegativeSign            ; // (user can override) negative sign
        //                internal String[] saNativeDigits         ; // (user can override) native characters for digits 0-9
        //                internal int iDigitSubstitution       ; // (user can override) Digit substitution 0=context, 1=none/arabic, 2=Native/national (2 seems to be unused) (Windows Only)
        //                internal int iDigits                  ; // (user can override) number of fractional digits
        //                internal int iNegativeNumber          ; // (user can override) negative number format

        // Leading zeroes
        private bool ILEADINGZEROS
        {
            get
            {
                return (DoGetLocaleInfoInt(LOCALE_ILZERO) == 1);
            }
        }


        // (user can override) grouping of digits
        internal int[] WAGROUPING
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.waGrouping == null || UseUserOverride)
                {
                    this.waGrouping = ConvertWin32GroupString(DoGetLocaleInfo(LOCALE_SGROUPING));
                }
                return this.waGrouping;
            }
        }


        //                internal String sDecimalSeparator        ; // (user can override) decimal separator
        //                internal String sThousandSeparator       ; // (user can override) thousands separator

        // Not a Number
        internal String SNAN
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sNaN == null)
                {
                    this.sNaN = DoGetLocaleInfo(LOCALE_SNAN);
                }
                return this.sNaN;
            }
        }

        // + Infinity
        internal String SPOSINFINITY
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sPositiveInfinity == null)
                {
                    this.sPositiveInfinity = DoGetLocaleInfo(LOCALE_SPOSINFINITY);
                }
                return this.sPositiveInfinity;
            }
        }

        // - Infinity
        internal String SNEGINFINITY
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sNegativeInfinity == null)
                {
                    this.sNegativeInfinity = DoGetLocaleInfo(LOCALE_SNEGINFINITY);
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
                    this.iNegativePercent = DoGetLocaleInfoInt(LOCALE_INEGATIVEPERCENT);
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
                    this.iPositivePercent = DoGetLocaleInfoInt(LOCALE_IPOSITIVEPERCENT);
                }
                return this.iPositivePercent;
            }
        }

        // Percent (%) symbol
        internal String SPERCENT
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sPercent == null)
                {
                    // Note that <= Windows Vista this is synthesized by native code
                    this.sPercent = DoGetLocaleInfo(LOCALE_SPERCENT);
                }
                return this.sPercent;
            }
        }

        // PerMille (‰) symbol
        internal String SPERMILLE
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sPerMille == null)
                {
                    // Note that <= Windows Vista this is synthesized by native code
                    this.sPerMille = DoGetLocaleInfo(LOCALE_SPERMILLE);
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
                if (this.sCurrency == null || UseUserOverride)
                {
                    this.sCurrency = DoGetLocaleInfo(LOCALE_SCURRENCY);
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
                    this.sIntlMonetarySymbol = DoGetLocaleInfo(LOCALE_SINTLSYMBOL);
                }
                return this.sIntlMonetarySymbol;
            }
        }

        // English name for this currency (RegionInfo), eg: US Dollar
        internal String SENGLISHCURRENCY
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sEnglishCurrency == null)
                {
                    this.sEnglishCurrency = DoGetLocaleInfo(LOCALE_SENGCURRNAME);
                }
                return this.sEnglishCurrency;
            }
        }

        // Native name for this currency (RegionInfo), eg: Schweiz Frank
        internal String SNATIVECURRENCY
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sNativeCurrency == null)
                {
                    this.sNativeCurrency = DoGetLocaleInfo(LOCALE_SNATIVECURRNAME);
                }
                return this.sNativeCurrency;
            }
        }

        //                internal int iCurrencyDigits          ; // (user can override) # local monetary fractional digits
        //                internal int iCurrency                ; // (user can override) positive currency format
        //                internal int iNegativeCurrency        ; // (user can override) negative currency format

        // (user can override) monetary grouping of digits
        internal int[] WAMONGROUPING
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.waMonetaryGrouping == null || UseUserOverride)
                {
                    this.waMonetaryGrouping = ConvertWin32GroupString(DoGetLocaleInfo(LOCALE_SMONGROUPING));
                }
                return this.waMonetaryGrouping;
            }
        }

        //                internal String sMonetaryDecimal         ; // (user can override) monetary decimal separator
        //                internal String sMonetaryThousand        ; // (user can override) monetary thousands separator

        /////////
        // Misc //
        /////////

        // (user can override) system of measurement 0=metric, 1=US (RegionInfo)
        internal int IMEASURE
        {
            get
            {
                if (this.iMeasure == undef || UseUserOverride)
                {
                    this.iMeasure = DoGetLocaleInfoInt(LOCALE_IMEASURE);
                }
                return this.iMeasure;
            }
        }

        // (user can override) list Separator
        internal String SLIST
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sListSeparator == null || UseUserOverride)
                {
                    this.sListSeparator = DoGetLocaleInfo(LOCALE_SLIST);
                }
                return this.sListSeparator;
            }
        }

        // Paper size
        private int IPAPERSIZE
        {
            get
            {
                return DoGetLocaleInfoInt(LOCALE_IPAPERSIZE);
            }
        }

        ////////////////////////////
        // Calendar/Time (Gregorian) //
        ////////////////////////////

        // (user can override) AM designator
        internal String SAM1159
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sAM1159 == null || UseUserOverride)
                {
                    this.sAM1159 = DoGetLocaleInfo(LOCALE_S1159);
                }
                return this.sAM1159;
            }
        }

        // (user can override) PM designator
        internal String SPM2359
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.sPM2359 == null || UseUserOverride)
                {
                    this.sPM2359 = DoGetLocaleInfo(LOCALE_S2359);
                }
                return this.sPM2359;
            }
        }

        // (user can override) time format
        internal String[] LongTimes
        {
            get
            {
                if (this.saLongTimes == null || UseUserOverride)
                {
                    String[] longTimes = DoEnumTimeFormats();
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
        internal String[] ShortTimes
        {
            get
            {
                if (this.saShortTimes == null || UseUserOverride)
                {
                    // Try to get the short times from the OS/culture.dll
                    String[] shortTimes = DoEnumShortTimeFormats();

                    if (shortTimes == null || shortTimes.Length == 0)
                    {
                        //
                        // If we couldn't find short times, then compute them from long times
                        // (eg: CORECLR on < Win7 OS & fallback for missing culture.dll)
                        //
                        shortTimes = DeriveShortTimesFromLong();
                    }

                    // Found short times, use them
                    this.saShortTimes = shortTimes;
                }
                return this.saShortTimes;
            }
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
                        StringBuilder sb = new StringBuilder(time.Substring(0, j));
                        if (containsSpace)
                        {
                            sb.Append(' ');
                        }
                        sb.Append(time.Substring(endIndex));
                        time = sb.ToString();
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

        // time duration format
        internal String[] SADURATION
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (this.saDurationFormats == null)
                {
                    String durationFormat = DoGetLocaleInfo(LOCALE_SDURATION);
                    this.saDurationFormats = new String[] { ReescapeWin32String(durationFormat) };
                }
                return this.saDurationFormats;
            }
        }

        // (user can override) first day of week
        internal int IFIRSTDAYOFWEEK
        {
            get
            {
                if (this.iFirstDayOfWeek == undef || UseUserOverride)
                {
                    // Have to convert it from windows to .Net formats
                    this.iFirstDayOfWeek = ConvertFirstDayOfWeekMonToSun(DoGetLocaleInfoInt(LOCALE_IFIRSTDAYOFWEEK));
                }
                return this.iFirstDayOfWeek;
            }
        }

        // (user can override) first week of year
        internal int IFIRSTWEEKOFYEAR
        {
            get
            {
                if (this.iFirstWeekOfYear == undef || UseUserOverride)
                {
                    this.iFirstWeekOfYear = DoGetLocaleInfoInt(LOCALE_IFIRSTWEEKOFYEAR);
                }
                return this.iFirstWeekOfYear;
            }
        }

        // (user can override default only) short date format
        internal String[] ShortDates(int calendarId)
        {
            return GetCalendar(calendarId).saShortDates;
        }

        // (user can override default only) long date format
        internal String[] LongDates(int calendarId)
        {
            return GetCalendar(calendarId).saLongDates;
        }

        // (user can override) date year/month format.
        internal String[] YearMonths(int calendarId)
        {
            return GetCalendar(calendarId).saYearMonths;
        }

        // day names
        internal string[] DayNames(int calendarId)
        {
            return GetCalendar(calendarId).saDayNames;
        }

        // abbreviated day names
        internal string[] AbbreviatedDayNames(int calendarId)
        {
            // Get abbreviated day names for this calendar from the OS if necessary
            return GetCalendar(calendarId).saAbbrevDayNames;
        }

        // The super short day names
        internal string[] SuperShortDayNames(int calendarId)
        {
            return GetCalendar(calendarId).saSuperShortDayNames;
        }

        // month names
        internal string[] MonthNames(int calendarId)
        {
            return GetCalendar(calendarId).saMonthNames;
        }

        // Genitive month names
        internal string[] GenitiveMonthNames(int calendarId)
        {
            return GetCalendar(calendarId).saMonthGenitiveNames;
        }

        // month names
        internal string[] AbbreviatedMonthNames(int calendarId)
        {
            return GetCalendar(calendarId).saAbbrevMonthNames;
        }

        // Genitive month names
        internal string[] AbbreviatedGenitiveMonthNames(int calendarId)
        {
            return GetCalendar(calendarId).saAbbrevMonthGenitiveNames;
        }

        // Leap year month names
        // Note: This only applies to Hebrew, and it basically adds a "1" to the 6th month name
        // the non-leap names skip the 7th name in the normal month name array
        internal string[] LeapYearMonthNames(int calendarId)
        {
            return GetCalendar(calendarId).saLeapYearMonthNames;
        }

        // month/day format (single string, no override)
        internal String MonthDay(int calendarId)
        {
            return GetCalendar(calendarId).sMonthDay;
        }



        /////////////
        // Calendars //
        /////////////

        // all available calendar type(s), The first one is the default calendar.
        internal int[] CalendarIds
        {
            get
            {
                if (this.waCalendars == null)
                {
                    // We pass in an array of ints, and native side fills it up with count calendars.
                    // We then have to copy that list to a new array of the right size.
                    // Default calendar should be first
                    int[] calendarInts = new int[23];
                    Contract.Assert(this.sWindowsName != null, "[CultureData.CalendarIds] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
                    int count = CalendarData.nativeGetCalendars(this.sWindowsName, this.bUseOverrides, calendarInts);

                    // See if we had a calendar to add.
                    if (count == 0)
                    {
                        // Failed for some reason, just grab Gregorian from Invariant
                        this.waCalendars = Invariant.waCalendars;
                    }
                    else
                    {
                        // The OS may not return calendar 4 for zh-TW, but we've always allowed it.
                        if (this.sWindowsName == "zh-TW")
                        {
                            bool found = false;

                            // Do we need to insert calendar 4?
                            for (int i = 0; i < count; i++)
                            {
                                // Stop if we found calendar four
                                if (calendarInts[i] == Calendar.CAL_TAIWAN)
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
                                Array.Copy(calendarInts, 1, calendarInts, 2, 23 - 1 - 1);
                                calendarInts[1] = Calendar.CAL_TAIWAN;
                            }
                        }

                        // It worked, remember the list
                        int[] temp = new int[count];
                        Array.Copy(calendarInts, temp, count);

                        // Want 1st calendar to be default
                        // Prior to Vista the enumeration didn't have default calendar first
                        // Only a coreclr concern, culture.dll does the right thing.
#if FEATURE_CORECLR
                        if (temp.Length > 1)
                        {
                            int i = DoGetLocaleInfoInt(LOCALE_ICALENDARTYPE);
                            if (temp[1] == i)
                            {
                                temp[1] = temp[0];
                                temp[0] = i;
                            }
                        }
#endif

                        this.waCalendars = temp;
                    }
                }

                return this.waCalendars;
            }
        }

        // Native calendar names.  index of optional calendar - 1, empty if no optional calendar at that number
        internal String CalendarName(int calendarId)
        {
            // Get the calendar
            return GetCalendar(calendarId).sNativeName;
        }

        internal CalendarData GetCalendar(int calendarId)
        {
            Contract.Assert(calendarId > 0 && calendarId <= CalendarData.MAX_CALENDARS,
                "[CultureData.GetCalendar] Expect calendarId to be in a valid range");

            // arrays are 0 based, calendarIds are 1 based
            int calendarIndex = calendarId - 1;

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
            if (calendarData == null || UseUserOverride)
            {
                Contract.Assert(this.sWindowsName != null, "[CultureData.GetCalendar] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
                calendarData = new CalendarData(this.sWindowsName, calendarId, this.UseUserOverride);
#if !FEATURE_CORECLR
                //Work around issue where Win7 data for MonthDay contains invalid two sets of data separated by semicolon
                //even though MonthDay is not enumerated
                if (IsOsWin7OrPrior() && !IsSupplementalCustomCulture && !IsReplacementCulture)
                {
                    calendarData.FixupWin7MonthDaySemicolonBug();
                }
#endif
                calendars[calendarIndex] = calendarData;
            }

            return calendarData;
        }

        internal int CurrentEra(int calendarId)
        {
            return GetCalendar(calendarId).iCurrentEra;
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
                    Contract.Assert(this.sRealName != null, "[CultureData.IsRightToLeft] Expected this.sRealName to be populated by COMNlsInfo::nativeInitCultureData already");
                    this.iReadingLayout = DoGetLocaleInfoInt(LOCALE_IREADINGLAYOUT);
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
            [System.Security.SecuritySafeCritical]
            get
            {
                if (this.sTextInfo == null)
                {
                    // LOCALE_SSORTLOCALE is broken in Win7 for Alt sorts.
                    // It is also not supported downlevel without culture.dll.
                    if (IsNeutralCulture || IsSupplementalCustomCulture)
                    {
                        string sortLocale = DoGetLocaleInfo(LOCALE_SSORTLOCALE);
                        this.sTextInfo = GetCultureData(sortLocale, bUseOverrides).SNAME;
                    }

                    if (this.sTextInfo == null)
                    {
                        this.sTextInfo = this.SNAME; // removes alternate sort
                    }
                }

                return this.sTextInfo;
            }
        }

        // Compare info name (including sorting key) to use if custom
        internal String SCOMPAREINFO
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (this.sCompareInfo == null)
                {
                    // LOCALE_SSORTLOCALE is broken in Win7 for Alt sorts.
                    // It is also not supported downlevel without culture.dll.
                    // We really only need it for the custom locale case though
                    // since for all other cases, it is the same as sWindowsName
                    if (IsSupplementalCustomCulture)
                    {
                        this.sCompareInfo = DoGetLocaleInfo(LOCALE_SSORTLOCALE);
                    }

                    if (this.sCompareInfo == null)
                    {
                        this.sCompareInfo = this.sWindowsName;
                    }
                }

                return this.sCompareInfo;
            }
        }

        internal bool IsSupplementalCustomCulture
        {
            get
            {
                return IsCustomCultureId(this.ILANGUAGE);
            }
        }

        // Typical Scripts for this locale (latn;cyrl; etc)

        private String SSCRIPTS
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (this.sScripts == null)
                {
                    this.sScripts = DoGetLocaleInfo(LOCALE_SSCRIPTS);
                }
                return this.sScripts;
            }
        }

        private String SOPENTYPELANGUAGETAG
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return DoGetLocaleInfo(LOCALE_SOPENTYPELANGUAGETAG);
            }
        }

        private String FONTSIGNATURE
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (this.fontSignature == null)
                {
                    this.fontSignature = DoGetLocaleInfo(LOCALE_FONTSIGNATURE);
                }
                return this.fontSignature;
            }
        }

        private String SKEYBOARDSTOINSTALL
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                return DoGetLocaleInfo(LOCALE_SKEYBOARDSTOINSTALL);
            }
        }


        internal int IDEFAULTANSICODEPAGE   // default ansi code page ID (ACP)
        {
            get
            {
                if (this.iDefaultAnsiCodePage == undef)
                {
                    this.iDefaultAnsiCodePage = DoGetLocaleInfoInt(LOCALE_IDEFAULTANSICODEPAGE);
                }
                return this.iDefaultAnsiCodePage;
            }
        }

        internal int IDEFAULTOEMCODEPAGE   // default oem code page ID (OCP or OEM)
        {
            get
            {
                if (this.iDefaultOemCodePage == undef)
                {
                    this.iDefaultOemCodePage = DoGetLocaleInfoInt(LOCALE_IDEFAULTCODEPAGE);
                }
                return this.iDefaultOemCodePage;
            }
        }

        internal int IDEFAULTMACCODEPAGE   // default macintosh code page
        {
            get
            {
                if (this.iDefaultMacCodePage == undef)
                {
                    this.iDefaultMacCodePage = DoGetLocaleInfoInt(LOCALE_IDEFAULTMACCODEPAGE);
                }
                return this.iDefaultMacCodePage;
            }
        }

        internal int IDEFAULTEBCDICCODEPAGE   // default EBCDIC code page
        {
            get
            {
                if (this.iDefaultEbcdicCodePage == undef)
                {
                    this.iDefaultEbcdicCodePage = DoGetLocaleInfoInt(LOCALE_IDEFAULTEBCDICCODEPAGE);
                }
                return this.iDefaultEbcdicCodePage;
            }
        }

        // Obtain locale name from LCID
        // NOTE: This will get neutral names, unlike the OS API
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int LocaleNameToLCID(String localeName);

        // These are desktop only, not coreclr
        // locale ID (0409), including sort information
        internal int ILANGUAGE
        {
            get
            {
                if (this.iLanguage == 0)
                {
                    Contract.Assert(this.sRealName != null, "[CultureData.ILANGUAGE] Expected this.sRealName to be populated by COMNlsInfo::nativeInitCultureData already");
                    this.iLanguage = LocaleNameToLCID(this.sRealName);
                }
                return this.iLanguage;
            }
        }

        internal bool IsWin32Installed
        {
            get { return this.bWin32Installed; }
        }

        internal bool IsFramework
        {
            get { return this.bFramework; }
        }

        ////////////////////
        // Derived properties //
        ////////////////////

        internal bool IsNeutralCulture
        {
            get
            {
                // NlsInfo::nativeInitCultureData told us if we're neutral or not
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
                int defaultCalId = DoGetLocaleInfoInt(LOCALE_ICALENDARTYPE);
                if (defaultCalId == 0)
                {
                    defaultCalId = this.CalendarIds[0];
                }

                return CultureInfo.GetCalendarInstance(defaultCalId);
            }
        }

        // All of our era names
        internal String[] EraNames(int calendarId)
        {
            Contract.Assert(calendarId > 0, "[CultureData.saEraNames] Expected Calendar.ID > 0");

            return this.GetCalendar(calendarId).saEraNames;
        }

        internal String[] AbbrevEraNames(int calendarId)
        {
            Contract.Assert(calendarId > 0, "[CultureData.saAbbrevEraNames] Expected Calendar.ID > 0");

            return this.GetCalendar(calendarId).saAbbrevEraNames;
        }

        internal String[] AbbreviatedEnglishEraNames(int calendarId)
        {
            Contract.Assert(calendarId > 0, "[CultureData.saAbbrevEraNames] Expected Calendar.ID > 0");

            return this.GetCalendar(calendarId).saAbbrevEnglishEraNames;
        }

        // String array DEFAULTS
        // Note: GetDTFIOverrideValues does the user overrides for these, so we don't have to.


        // Time separator (derived from time format)
        internal String TimeSeparator
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (sTimeSeparator == null || UseUserOverride)
                {
                    string longTimeFormat = ReescapeWin32String(DoGetLocaleInfo(LOCALE_STIMEFORMAT));
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
        internal String DateSeparator(int calendarId)
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
        static private String UnescapeNlsString(String str, int start, int end)
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

        ////////////////////////////////////////////////////////////////////////////
        //
        // Reescape a Win32 style quote string as a NLS+ style quoted string
        //
        // This is also the escaping style used by custom culture data files
        //
        // NLS+ uses \ to escape the next character, whether in a quoted string or
        // not, so we always have to change \ to \\.
        //
        // NLS+ uses \' to escape a quote inside a quoted string so we have to change
        // '' to \' (if inside a quoted string)
        //
        // We don't build the stringbuilder unless we find something to change
        ////////////////////////////////////////////////////////////////////////////
        static internal String ReescapeWin32String(String str)
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

        static internal String[] ReescapeWin32Strings(String[] array)
        {
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = ReescapeWin32String(array[i]);
                }
            }

            return array;
        }

        // NOTE: this method is used through reflection by System.Globalization.CultureXmlParser.ReadDateElement()
        // and breaking changes here will not show up at build time, only at run time.
        static private String GetTimeSeparator(String format)
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

        // NOTE: this method is used through reflection by System.Globalization.CultureXmlParser.ReadDateElement()
        // and breaking changes here will not show up at build time, only at run time.
        static private String GetDateSeparator(String format)
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

        [System.Security.SecurityCritical]
        string DoGetLocaleInfo(uint lctype)
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.DoGetLocaleInfo] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
            return DoGetLocaleInfo(this.sWindowsName, lctype);
        }

        // For LOCALE_SPARENT we need the option of using the "real" name (forcing neutral names) instead of the
        // "windows" name, which can be specific for downlevel (< windows 7) os's.
        [System.Security.SecurityCritical]  // auto-generated
        string DoGetLocaleInfo(string localeName, uint lctype)
        {
            // Fix lctype if we don't want overrides
            if (!UseUserOverride)
            {
                lctype |= LOCALE_NOUSEROVERRIDE;
            }

            // Ask OS for data
            Contract.Assert(localeName != null, "[CultureData.DoGetLocaleInfo] Expected localeName to be not be null");
            string result = CultureInfo.nativeGetLocaleInfoEx(localeName, lctype);
            if (result == null)
            {
                // Failed, just use empty string
                result = String.Empty;
            }

            return result;
        }

        int DoGetLocaleInfoInt(uint lctype)
        {
            // Fix lctype if we don't want overrides
            if (!UseUserOverride)
            {
                lctype |= LOCALE_NOUSEROVERRIDE;
            }

            // Ask OS for data, note that we presume it returns success, so we have to know that
            // sWindowsName is valid before calling.
            Contract.Assert(this.sWindowsName != null, "[CultureData.DoGetLocaleInfoInt] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
            int result = CultureInfo.nativeGetLocaleInfoExInt(this.sWindowsName, lctype);

            return result;
        }

        String[] DoEnumTimeFormats()
        {
            // Note that this gets overrides for us all the time
            Contract.Assert(this.sWindowsName != null, "[CultureData.DoEnumTimeFormats] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
            String[] result = ReescapeWin32Strings(nativeEnumTimeFormats(this.sWindowsName, 0, UseUserOverride));

            return result;
        }

        String[] DoEnumShortTimeFormats()
        {
            // Note that this gets overrides for us all the time
            Contract.Assert(this.sWindowsName != null, "[CultureData.DoEnumShortTimeFormats] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
            String[] result = ReescapeWin32Strings(nativeEnumTimeFormats(this.sWindowsName, TIME_NOSECONDS, UseUserOverride));

            return result;
        }

        /////////////////
        // Static Helpers //
        ////////////////
        internal static bool IsCustomCultureId(int cultureId)
        {
            if (cultureId == CultureInfo.LOCALE_CUSTOM_DEFAULT || cultureId == CultureInfo.LOCALE_CUSTOM_UNSPECIFIED)
                return true;

            return false;
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // Parameters:
        //      calendarValueOnly   Retrieve the values which are affected by the calendar change of DTFI.
        //                          This will cause values like longTimePattern not be retrieved since it is
        //                          not affected by the Calendar property in DTFI.
        //
        ////////////////////////////////////////////////////////////////////////////
        [System.Security.SecurityCritical]  // auto-generated
        internal void GetNFIValues(NumberFormatInfo nfi)
        {
            if (this.IsInvariantCulture)
            {
                nfi.positiveSign = this.sPositiveSign;
                nfi.negativeSign = this.sNegativeSign;

                nfi.nativeDigits = this.saNativeDigits;
                nfi.digitSubstitution = this.iDigitSubstitution;

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
                //
                // We don't have information for the following four.  All cultures use
                // the same value of the number formatting values.
                //
                // PercentDecimalDigits
                // PercentDecimalSeparator
                // PercentGroupSize
                // PercentGroupSeparator
                //

                //
                // Ask native side for our data.
                //
                Contract.Assert(this.sWindowsName != null, "[CultureData.GetNFIValues] Expected this.sWindowsName to be populated by COMNlsInfo::nativeInitCultureData already");
                CultureData.nativeGetNumberFormatInfoValues(this.sWindowsName, nfi, UseUserOverride);
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
            //decimal point doesn't show up.  We'll just workaround this here because our default currency format will never use nfi.
            if (nfi.currencyDecimalSeparator == null || nfi.currencyDecimalSeparator.Length == 0)
            {
                nfi.currencyDecimalSeparator = nfi.numberDecimalSeparator;
            }

#if !FEATURE_CORECLR
            if ((932 == this.IDEFAULTANSICODEPAGE) ||
               (949 == this.IDEFAULTANSICODEPAGE))
            {
                // Legacy behavior for cultures that use Japanese/Korean default ANSI code pages
                // Note that this is a code point, not a character.  On Japanese/Korean machines this
                // will be rendered as their currency symbol, not rendered as a "\"
                nfi.ansiCurrencySymbol = "\x5c";
            }
#endif // !FEATURE_CORECLR
        }

        static private int ConvertFirstDayOfWeekMonToSun(int iTemp)
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

        // If we get a group from windows, then its in 3;0 format with the 0 backwards
        // of how NLS+ uses it (ie: if the string has a 0, then the int[] shouldn't and vice versa)
        // EXCEPT in the case where the list only contains 0 in which NLS and NLS+ have the same meaning.
        static private int[] ConvertWin32GroupString(String win32Str)
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

        // LCTYPES for GetLocaleInfo
        private const uint LOCALE_NOUSEROVERRIDE = 0x80000000;   // do not use user overrides
        private const uint LOCALE_RETURN_NUMBER = 0x20000000;   // return number instead of string

        // Modifier for genitive names
        private const uint LOCALE_RETURN_GENITIVE_NAMES = 0x10000000;   //Flag to return the Genitive forms of month names

        //
        //  The following LCTypes are mutually exclusive in that they may NOT
        //  be used in combination with each other.
        //

        //
        // These are the various forms of the name of the locale:
        //
        private const uint LOCALE_SLOCALIZEDDISPLAYNAME = 0x00000002;   // localized name of locale, eg "German (Germany)" in UI language
        private const uint LOCALE_SENGLISHDISPLAYNAME = 0x00000072;   // Display name (language + country usually) in English, eg "German (Germany)"
        private const uint LOCALE_SNATIVEDISPLAYNAME = 0x00000073;   // Display name in native locale language, eg "Deutsch (Deutschland)

        private const uint LOCALE_SLOCALIZEDLANGUAGENAME = 0x0000006f;   // Language Display Name for a language, eg "German" in UI language
        private const uint LOCALE_SENGLISHLANGUAGENAME = 0x00001001;   // English name of language, eg "German"
        private const uint LOCALE_SNATIVELANGUAGENAME = 0x00000004;   // native name of language, eg "Deutsch"

        private const uint LOCALE_SLOCALIZEDCOUNTRYNAME = 0x00000006;   // localized name of country, eg "Germany" in UI language
        private const uint LOCALE_SENGLISHCOUNTRYNAME = 0x00001002;   // English name of country, eg "Germany"
        private const uint LOCALE_SNATIVECOUNTRYNAME = 0x00000008;   // native name of country, eg "Deutschland"


        //        private const uint LOCALE_ILANGUAGE              =0x00000001;   // language id // Don't use, use NewApis::LocaleNameToLCID instead (GetLocaleInfo doesn't return neutrals)

        //        private const uint LOCALE_SLANGUAGE              =LOCALE_SLOCALIZEDDISPLAYNAME;   // localized name of language (use LOCALE_SLOCALIZEDDISPLAYNAME instead)
        //        private const uint LOCALE_SENGLANGUAGE           =LOCALE_SENGLISHLANGUAGENAME;   // English name of language (use LOCALE_SENGLISHLANGUAGENAME instead)
        private const uint LOCALE_SABBREVLANGNAME = 0x00000003;   // abbreviated language name
        //        private const uint LOCALE_SNATIVELANGNAME        =LOCALE_SNATIVELANGUAGENAME;   // native name of language (use LOCALE_SNATIVELANGUAGENAME instead)

        private const uint LOCALE_ICOUNTRY = 0x00000005;   // country code
        //        private const uint LOCALE_SCOUNTRY               =LOCALE_SLOCALIZEDCOUNTRYNAME;   // localized name of country (use LOCALE_SLOCALIZEDCOUNTRYNAME instead)
        //        private const uint LOCALE_SENGCOUNTRY            =LOCALE_SENGLISHCOUNTRYNAME;   // English name of country (use LOCALE_SENGLISHCOUNTRYNAME instead)
        private const uint LOCALE_SABBREVCTRYNAME = 0x00000007;   // abbreviated country name
        //        private const uint LOCALE_SNATIVECTRYNAME        =LOCALE_SNATIVECOUNTRYNAME;   // native name of country ( use LOCALE_SNATIVECOUNTRYNAME instead)
        private const uint LOCALE_IGEOID = 0x0000005B;   // geographical location id

        private const uint LOCALE_IDEFAULTLANGUAGE = 0x00000009;   // default language id
        private const uint LOCALE_IDEFAULTCOUNTRY = 0x0000000A;   // default country code
        private const uint LOCALE_IDEFAULTCODEPAGE = 0x0000000B;   // default oem code page
        private const uint LOCALE_IDEFAULTANSICODEPAGE = 0x00001004;   // default ansi code page
        private const uint LOCALE_IDEFAULTMACCODEPAGE = 0x00001011;   // default mac code page

        private const uint LOCALE_SLIST = 0x0000000C;   // list item separator
        private const uint LOCALE_IMEASURE = 0x0000000D;   // 0 = metric, 1 = US

        private const uint LOCALE_SDECIMAL = 0x0000000E;   // decimal separator
        private const uint LOCALE_STHOUSAND = 0x0000000F;   // thousand separator
        private const uint LOCALE_SGROUPING = 0x00000010;   // digit grouping
        private const uint LOCALE_IDIGITS = 0x00000011;   // number of fractional digits
        private const uint LOCALE_ILZERO = 0x00000012;   // leading zeros for decimal
        private const uint LOCALE_INEGNUMBER = 0x00001010;   // negative number mode
        private const uint LOCALE_SNATIVEDIGITS = 0x00000013;   // native digits for 0-9

        private const uint LOCALE_SCURRENCY = 0x00000014;   // local monetary symbol
        private const uint LOCALE_SINTLSYMBOL = 0x00000015;   // uintl monetary symbol
        private const uint LOCALE_SMONDECIMALSEP = 0x00000016;   // monetary decimal separator
        private const uint LOCALE_SMONTHOUSANDSEP = 0x00000017;   // monetary thousand separator
        private const uint LOCALE_SMONGROUPING = 0x00000018;   // monetary grouping
        private const uint LOCALE_ICURRDIGITS = 0x00000019;   // # local monetary digits
        private const uint LOCALE_IINTLCURRDIGITS = 0x0000001A;   // # uintl monetary digits
        private const uint LOCALE_ICURRENCY = 0x0000001B;   // positive currency mode
        private const uint LOCALE_INEGCURR = 0x0000001C;   // negative currency mode

        private const uint LOCALE_SDATE = 0x0000001D;   // date separator (derived from LOCALE_SSHORTDATE, use that instead)
        private const uint LOCALE_STIME = 0x0000001E;   // time separator (derived from LOCALE_STIMEFORMAT, use that instead)
        private const uint LOCALE_SSHORTDATE = 0x0000001F;   // short date format string
        private const uint LOCALE_SLONGDATE = 0x00000020;   // long date format string
        private const uint LOCALE_STIMEFORMAT = 0x00001003;   // time format string
        private const uint LOCALE_IDATE = 0x00000021;   // short date format ordering (derived from LOCALE_SSHORTDATE, use that instead)
        private const uint LOCALE_ILDATE = 0x00000022;   // long date format ordering (derived from LOCALE_SLONGDATE, use that instead)
        private const uint LOCALE_ITIME = 0x00000023;   // time format specifier (derived from LOCALE_STIMEFORMAT, use that instead)
        private const uint LOCALE_ITIMEMARKPOSN = 0x00001005;   // time marker position (derived from LOCALE_STIMEFORMAT, use that instead)
        private const uint LOCALE_ICENTURY = 0x00000024;   // century format specifier (short date, LOCALE_SSHORTDATE is preferred)
        private const uint LOCALE_ITLZERO = 0x00000025;   // leading zeros in time field (derived from LOCALE_STIMEFORMAT, use that instead)
        private const uint LOCALE_IDAYLZERO = 0x00000026;   // leading zeros in day field (short date, LOCALE_SSHORTDATE is preferred)
        private const uint LOCALE_IMONLZERO = 0x00000027;   // leading zeros in month field (short date, LOCALE_SSHORTDATE is preferred)
        private const uint LOCALE_S1159 = 0x00000028;   // AM designator
        private const uint LOCALE_S2359 = 0x00000029;   // PM designator

        private const uint LOCALE_ICALENDARTYPE = 0x00001009;   // type of calendar specifier
        private const uint LOCALE_IOPTIONALCALENDAR = 0x0000100B;   // additional calendar types specifier
        private const uint LOCALE_IFIRSTDAYOFWEEK = 0x0000100C;   // first day of week specifier
        private const uint LOCALE_IFIRSTWEEKOFYEAR = 0x0000100D;   // first week of year specifier

        private const uint LOCALE_SDAYNAME1 = 0x0000002A;   // long name for Monday
        private const uint LOCALE_SDAYNAME2 = 0x0000002B;   // long name for Tuesday
        private const uint LOCALE_SDAYNAME3 = 0x0000002C;   // long name for Wednesday
        private const uint LOCALE_SDAYNAME4 = 0x0000002D;   // long name for Thursday
        private const uint LOCALE_SDAYNAME5 = 0x0000002E;   // long name for Friday
        private const uint LOCALE_SDAYNAME6 = 0x0000002F;   // long name for Saturday
        private const uint LOCALE_SDAYNAME7 = 0x00000030;   // long name for Sunday
        private const uint LOCALE_SABBREVDAYNAME1 = 0x00000031;   // abbreviated name for Monday
        private const uint LOCALE_SABBREVDAYNAME2 = 0x00000032;   // abbreviated name for Tuesday
        private const uint LOCALE_SABBREVDAYNAME3 = 0x00000033;   // abbreviated name for Wednesday
        private const uint LOCALE_SABBREVDAYNAME4 = 0x00000034;   // abbreviated name for Thursday
        private const uint LOCALE_SABBREVDAYNAME5 = 0x00000035;   // abbreviated name for Friday
        private const uint LOCALE_SABBREVDAYNAME6 = 0x00000036;   // abbreviated name for Saturday
        private const uint LOCALE_SABBREVDAYNAME7 = 0x00000037;   // abbreviated name for Sunday
        private const uint LOCALE_SMONTHNAME1 = 0x00000038;   // long name for January
        private const uint LOCALE_SMONTHNAME2 = 0x00000039;   // long name for February
        private const uint LOCALE_SMONTHNAME3 = 0x0000003A;   // long name for March
        private const uint LOCALE_SMONTHNAME4 = 0x0000003B;   // long name for April
        private const uint LOCALE_SMONTHNAME5 = 0x0000003C;   // long name for May
        private const uint LOCALE_SMONTHNAME6 = 0x0000003D;   // long name for June
        private const uint LOCALE_SMONTHNAME7 = 0x0000003E;   // long name for July
        private const uint LOCALE_SMONTHNAME8 = 0x0000003F;   // long name for August
        private const uint LOCALE_SMONTHNAME9 = 0x00000040;   // long name for September
        private const uint LOCALE_SMONTHNAME10 = 0x00000041;   // long name for October
        private const uint LOCALE_SMONTHNAME11 = 0x00000042;   // long name for November
        private const uint LOCALE_SMONTHNAME12 = 0x00000043;   // long name for December
        private const uint LOCALE_SMONTHNAME13 = 0x0000100E;   // long name for 13th month (if exists)
        private const uint LOCALE_SABBREVMONTHNAME1 = 0x00000044;   // abbreviated name for January
        private const uint LOCALE_SABBREVMONTHNAME2 = 0x00000045;   // abbreviated name for February
        private const uint LOCALE_SABBREVMONTHNAME3 = 0x00000046;   // abbreviated name for March
        private const uint LOCALE_SABBREVMONTHNAME4 = 0x00000047;   // abbreviated name for April
        private const uint LOCALE_SABBREVMONTHNAME5 = 0x00000048;   // abbreviated name for May
        private const uint LOCALE_SABBREVMONTHNAME6 = 0x00000049;   // abbreviated name for June
        private const uint LOCALE_SABBREVMONTHNAME7 = 0x0000004A;   // abbreviated name for July
        private const uint LOCALE_SABBREVMONTHNAME8 = 0x0000004B;   // abbreviated name for August
        private const uint LOCALE_SABBREVMONTHNAME9 = 0x0000004C;   // abbreviated name for September
        private const uint LOCALE_SABBREVMONTHNAME10 = 0x0000004D;   // abbreviated name for October
        private const uint LOCALE_SABBREVMONTHNAME11 = 0x0000004E;   // abbreviated name for November
        private const uint LOCALE_SABBREVMONTHNAME12 = 0x0000004F;   // abbreviated name for December
        private const uint LOCALE_SABBREVMONTHNAME13 = 0x0000100F;   // abbreviated name for 13th month (if exists)

        private const uint LOCALE_SPOSITIVESIGN = 0x00000050;   // positive sign
        private const uint LOCALE_SNEGATIVESIGN = 0x00000051;   // negative sign
        private const uint LOCALE_IPOSSIGNPOSN = 0x00000052;   // positive sign position (derived from INEGCURR)
        private const uint LOCALE_INEGSIGNPOSN = 0x00000053;   // negative sign position (derived from INEGCURR)
        private const uint LOCALE_IPOSSYMPRECEDES = 0x00000054;   // mon sym precedes pos amt (derived from ICURRENCY)
        private const uint LOCALE_IPOSSEPBYSPACE = 0x00000055;   // mon sym sep by space from pos amt (derived from ICURRENCY)
        private const uint LOCALE_INEGSYMPRECEDES = 0x00000056;   // mon sym precedes neg amt (derived from INEGCURR)
        private const uint LOCALE_INEGSEPBYSPACE = 0x00000057;   // mon sym sep by space from neg amt (derived from INEGCURR)

        private const uint LOCALE_FONTSIGNATURE = 0x00000058;   // font signature
        private const uint LOCALE_SISO639LANGNAME = 0x00000059;   // ISO abbreviated language name
        private const uint LOCALE_SISO3166CTRYNAME = 0x0000005A;   // ISO abbreviated country name

        private const uint LOCALE_IDEFAULTEBCDICCODEPAGE = 0x00001012;   // default ebcdic code page
        private const uint LOCALE_IPAPERSIZE = 0x0000100A;   // 1 = letter, 5 = legal, 8 = a3, 9 = a4
        private const uint LOCALE_SENGCURRNAME = 0x00001007;   // english name of currency
        private const uint LOCALE_SNATIVECURRNAME = 0x00001008;   // native name of currency
        private const uint LOCALE_SYEARMONTH = 0x00001006;   // year month format string
        private const uint LOCALE_SSORTNAME = 0x00001013;   // sort name
        private const uint LOCALE_IDIGITSUBSTITUTION = 0x00001014;   // 0 = context, 1 = none, 2 = national

        private const uint LOCALE_SNAME = 0x0000005c;   // locale name (with sort info) (ie: de-DE_phoneb)
        private const uint LOCALE_SDURATION = 0x0000005d;   // time duration format
        private const uint LOCALE_SKEYBOARDSTOINSTALL = 0x0000005e;   // (windows only) keyboards to install
        private const uint LOCALE_SSHORTESTDAYNAME1 = 0x00000060;   // Shortest day name for Monday
        private const uint LOCALE_SSHORTESTDAYNAME2 = 0x00000061;   // Shortest day name for Tuesday
        private const uint LOCALE_SSHORTESTDAYNAME3 = 0x00000062;   // Shortest day name for Wednesday
        private const uint LOCALE_SSHORTESTDAYNAME4 = 0x00000063;   // Shortest day name for Thursday
        private const uint LOCALE_SSHORTESTDAYNAME5 = 0x00000064;   // Shortest day name for Friday
        private const uint LOCALE_SSHORTESTDAYNAME6 = 0x00000065;   // Shortest day name for Saturday
        private const uint LOCALE_SSHORTESTDAYNAME7 = 0x00000066;   // Shortest day name for Sunday
        private const uint LOCALE_SISO639LANGNAME2 = 0x00000067;   // 3 character ISO abbreviated language name
        private const uint LOCALE_SISO3166CTRYNAME2 = 0x00000068;   // 3 character ISO country name
        private const uint LOCALE_SNAN = 0x00000069;   // Not a Number
        private const uint LOCALE_SPOSINFINITY = 0x0000006a;   // + Infinity
        private const uint LOCALE_SNEGINFINITY = 0x0000006b;   // - Infinity
        private const uint LOCALE_SSCRIPTS = 0x0000006c;   // Typical scripts in the locale
        private const uint LOCALE_SPARENT = 0x0000006d;   // Fallback name for resources
        private const uint LOCALE_SCONSOLEFALLBACKNAME = 0x0000006e;   // Fallback name for within the console
        //        private const uint LOCALE_SLANGDISPLAYNAME       =LOCALE_SLOCALIZEDLANGUAGENAME;   // Language Display Name for a language (use LOCALE_SLOCALIZEDLANGUAGENAME instead)

        // Windows 7 LCTYPES
        private const uint LOCALE_IREADINGLAYOUT = 0x00000070;   // Returns one of the following 4 reading layout values:
        // 0 - Left to right (eg en-US)
        // 1 - Right to left (eg arabic locales)
        // 2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
        // 3 - Vertical top to bottom with columns proceeding to the right
        private const uint LOCALE_INEUTRAL = 0x00000071;   // Returns 0 for specific cultures, 1 for neutral cultures.
        private const uint LOCALE_INEGATIVEPERCENT = 0x00000074;   // Returns 0-11 for the negative percent format
        private const uint LOCALE_IPOSITIVEPERCENT = 0x00000075;   // Returns 0-3 for the positive percent formatIPOSITIVEPERCENT
        private const uint LOCALE_SPERCENT = 0x00000076;   // Returns the percent symbol
        private const uint LOCALE_SPERMILLE = 0x00000077;   // Returns the permille (U+2030) symbol
        private const uint LOCALE_SMONTHDAY = 0x00000078;   // Returns the preferred month/day format
        private const uint LOCALE_SSHORTTIME = 0x00000079;   // Returns the preferred short time format (ie: no seconds, just h:mm)
        private const uint LOCALE_SOPENTYPELANGUAGETAG = 0x0000007a;   // Open type language tag, eg: "latn" or "dflt"
        private const uint LOCALE_SSORTLOCALE = 0x0000007b;   // Name of locale to use for sorting/collation/casing behavior.

        // Time formats enumerations
        internal const uint TIME_NOSECONDS = 0x00000002;   // Don't use seconds (get short time format for enumtimeformats on win7+)

        // Get our initial minimal culture data (name, parent, etc.)
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool nativeInitCultureData(CultureData cultureData);

        // Grab the NumberFormatInfo data
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool nativeGetNumberFormatInfoValues(String localeName, NumberFormatInfo nfi, bool useUserOverride);

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern String[] nativeEnumTimeFormats(String localeName, uint dwFlags, bool useUserOverride);

        [System.Security.SecurityCritical]  // auto-generated
        [SuppressUnmanagedCodeSecurityAttribute()]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern int nativeEnumCultureNames(int cultureTypes, ObjectHandleOnStack retStringArray);
    }
}
