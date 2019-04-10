// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace System.Globalization
{
#if CORERT
    #pragma warning restore nullable
    using StringStringDictionary = LowLevelDictionary<string, string>;
    using StringCultureDataDictionary = LowLevelDictionary<string, CultureData>;
    using LcidToCultureNameDictionary = LowLevelDictionary<int, string>;
#else
    using StringStringDictionary = Dictionary<string, string>;
    using StringCultureDataDictionary = Dictionary<string, CultureData>;
    using LcidToCultureNameDictionary = Dictionary<int, string>;
#endif

    /// <summary>
    /// List of culture data
    /// Note the we cache overrides.
    /// Note that localized names (resource names) aren't available from here.
    /// </summary>
    /// <remarks>
    /// Our names are a tad confusing.
    ///
    /// sWindowsName -- The name that windows thinks this culture is, ie:
    ///                            en-US if you pass in en-US
    ///                            de-DE_phoneb if you pass in de-DE_phoneb
    ///                            fj-FJ if you pass in fj (neutral, on a pre-Windows 7 machine)
    ///                            fj if you pass in fj (neutral, post-Windows 7 machine)
    ///
    /// sRealName -- The name you used to construct the culture, in pretty form
    ///                       en-US if you pass in EN-us
    ///                       en if you pass in en
    ///                       de-DE_phoneb if you pass in de-DE_phoneb
    ///
    /// sSpecificCulture -- The specific culture for this culture
    ///                             en-US for en-US
    ///                             en-US for en
    ///                             de-DE_phoneb for alt sort
    ///                             fj-FJ for fj (neutral)
    ///
    /// sName -- The IETF name of this culture (ie: no sort info, could be neutral)
    ///                en-US if you pass in en-US
    ///                en if you pass in en
    ///                de-DE if you pass in de-DE_phoneb
    /// </remarks>
    internal partial class CultureData
    {
        private const int LocaleNameMaxLength = 85;
        private const int undef = -1;

        // Override flag
        private string _sRealName = null!; // Name you passed in (ie: en-US, en, or de-DE_phoneb). Initialized by helper called during initialization.
        private string? _sWindowsName; // Name OS thinks the object is (ie: de-DE_phoneb, or en-US (even if en was passed in))

        // Identity
        private string? _sName; // locale name (ie: en-us, NO sort info, but could be neutral)
        private string? _sParent; // Parent name (which may be a custom locale/culture)
        private string? _sLocalizedDisplayName; // Localized pretty name for this locale
        private string? _sEnglishDisplayName; // English pretty name for this locale
        private string? _sNativeDisplayName; // Native pretty name for this locale
        private string? _sSpecificCulture; // The culture name to be used in CultureInfo.CreateSpecificCulture(), en-US form if neutral, sort name if sort

        // Language
        private string? _sISO639Language; // ISO 639 Language Name
        private string? _sISO639Language2; // ISO 639 Language Name
        private string? _sLocalizedLanguage; // Localized name for this language
        private string? _sEnglishLanguage; // English name for this language
        private string? _sNativeLanguage; // Native name of this language
        private string? _sAbbrevLang; // abbreviated language name (Windows Language Name) ex: ENU
        private string? _sConsoleFallbackName; // The culture name for the console fallback UI culture
        private int    _iInputLanguageHandle=undef;// input language handle

        // Region
        private string? _sRegionName; // (RegionInfo)
        private string? _sLocalizedCountry; // localized country name
        private string? _sEnglishCountry; // english country name (RegionInfo)
        private string? _sNativeCountry; // native country name
        private string? _sISO3166CountryName; // ISO 3166 (RegionInfo), ie: US
        private string? _sISO3166CountryName2; // 3 char ISO 3166 country name 2 2(RegionInfo) ex: USA (ISO)
        private int    _iGeoId = undef; // GeoId

        // Numbers
        private string? _sPositiveSign; // (user can override) positive sign
        private string? _sNegativeSign; // (user can override) negative sign
        // (nfi populates these 5, don't have to be = undef)
        private int _iDigits; // (user can override) number of fractional digits
        private int _iNegativeNumber; // (user can override) negative number format
        private int[]? _waGrouping; // (user can override) grouping of digits
        private string? _sDecimalSeparator; // (user can override) decimal separator
        private string? _sThousandSeparator; // (user can override) thousands separator
        private string? _sNaN; // Not a Number
        private string? _sPositiveInfinity; // + Infinity
        private string? _sNegativeInfinity; // - Infinity

        // Percent
        private int _iNegativePercent = undef; // Negative Percent (0-3)
        private int _iPositivePercent = undef; // Positive Percent (0-11)
        private string? _sPercent; // Percent (%) symbol
        private string? _sPerMille; // PerMille symbol

        // Currency
        private string? _sCurrency; // (user can override) local monetary symbol
        private string? _sIntlMonetarySymbol; // international monetary symbol (RegionInfo)
        private string? _sEnglishCurrency; // English name for this currency
        private string? _sNativeCurrency; // Native name for this currency
        // (nfi populates these 4, don't have to be = undef)
        private int _iCurrencyDigits; // (user can override) # local monetary fractional digits
        private int _iCurrency; // (user can override) positive currency format
        private int _iNegativeCurrency; // (user can override) negative currency format
        private int[]? _waMonetaryGrouping; // (user can override) monetary grouping of digits
        private string? _sMonetaryDecimal; // (user can override) monetary decimal separator
        private string? _sMonetaryThousand; // (user can override) monetary thousands separator

        // Misc
        private int _iMeasure = undef; // (user can override) system of measurement 0=metric, 1=US (RegionInfo)
        private string? _sListSeparator; // (user can override) list separator

        // Time
        private string? _sAM1159; // (user can override) AM designator
        private string? _sPM2359; // (user can override) PM designator
        private string? _sTimeSeparator;
        private volatile string[]? _saLongTimes; // (user can override) time format
        private volatile string[]? _saShortTimes; // short time format
        private volatile string[]? _saDurationFormats; // time duration format

        // Calendar specific data
        private int _iFirstDayOfWeek = undef; // (user can override) first day of week (gregorian really)
        private int _iFirstWeekOfYear = undef; // (user can override) first week of year (gregorian really)
        private volatile CalendarId[]? _waCalendars; // all available calendar type(s).  The first one is the default calendar

        // Store for specific data about each calendar
        private CalendarData?[]? _calendars; // Store for specific calendar data

        // Text information
        private int _iReadingLayout = undef; // Reading layout data
        // 0 - Left to right (eg en-US)
        // 1 - Right to left (eg arabic locales)
        // 2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
        // 3 - Vertical top to bottom with columns proceeding to the right

        // CoreCLR depends on this even though its not exposed publicly.

        private int _iDefaultAnsiCodePage = undef; // default ansi code page ID (ACP)
        private int _iDefaultOemCodePage = undef; // default oem code page ID (OCP or OEM)
        private int _iDefaultMacCodePage = undef; // default macintosh code page
        private int _iDefaultEbcdicCodePage = undef; // default EBCDIC code page

        private int _iLanguage; // locale ID (0409) - NO sort information
        private bool _bUseOverrides; // use user overrides?
        private bool _bNeutral; // Flags for the culture (ie: neutral or not right now)

        /// <summary>
        /// Region Name to Culture Name mapping table
        /// </summary>
        /// <remarks>
        /// Using a property so we avoid creating the dictionary until we need it
        /// </remarks>
        private static StringStringDictionary RegionNames
        {
            get
            {
                if (s_regionNames == null)
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

                    s_regionNames = regionNames;
                }

                return s_regionNames!;
            }
        }

        // Cache of regions we've already looked up
        private static volatile StringCultureDataDictionary? s_cachedRegions;
        private static volatile StringStringDictionary? s_regionNames;

        internal static CultureData? GetCultureDataForRegion(string? cultureName, bool useUserOverride)
        {
            // First do a shortcut for Invariant
            if (string.IsNullOrEmpty(cultureName))
            {
                return CultureData.Invariant;
            }

            // First check if GetCultureData() can find it (ie: its a real culture)
            CultureData? retVal = GetCultureData(cultureName, useUserOverride);
            if (retVal != null && !retVal.IsNeutralCulture)
            {
                return retVal;
            }

            // Not a specific culture, perhaps it's region-only name
            // (Remember this isn't a core clr path where that's not supported)

            // If it was neutral remember that so that RegionInfo() can throw the right exception
            CultureData? neutral = retVal;

            // Try the hash table next
            string hashName = AnsiToLower(useUserOverride ? cultureName : cultureName + '*');
            StringCultureDataDictionary? tempHashTable = s_cachedRegions;

            if (tempHashTable == null)
            {
                // No table yet, make a new one
                tempHashTable = new StringCultureDataDictionary();
            }
            else
            {
                // Check the hash table
                lock (s_lock)
                {
                    tempHashTable.TryGetValue(hashName, out retVal);
                }
                if (retVal != null)
                {
                    return retVal;
                }
            }

            // Not found in the hash table, look it up the hard way

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
            if (!GlobalizationMode.Invariant && (retVal == null || (retVal.IsNeutralCulture == true)))
            {
                retVal = GetCultureDataFromRegionName(cultureName);
            }

            // If we found one we can use, then cache it for next time
            if (retVal != null && (retVal.IsNeutralCulture == false))
            {
                // first add it to the cache
                lock (s_lock)
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

        // Clear our internal caches
        internal static void ClearCachedData()
        {
            s_cachedCultures = null;
            s_cachedRegions = null;
        }

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
                throw new ArgumentOutOfRangeException(nameof(types),
                              SR.Format(SR.ArgumentOutOfRange_Range, CultureTypes.NeutralCultures, CultureTypes.FrameworkCultures));
            }

            // We have deprecated CultureTypes.FrameworkCultures.
            // When this enum is used, we will enumerate Whidbey framework cultures (for compatibility).

            // We have deprecated CultureTypes.WindowsOnlyCultures.
            // When this enum is used, we will return an empty array for this enum.
            if ((types & CultureTypes.WindowsOnlyCultures) != 0)
            {
                // Remove the enum as it is an no-op.
                types &= (~CultureTypes.WindowsOnlyCultures);
            }

            if (GlobalizationMode.Invariant)
            {
                // in invariant mode we always return invariant culture only from the enumeration
                return new CultureInfo[] { new CultureInfo("") };
            }

#pragma warning restore 618
            return EnumCultures(types);
        }

        private static CultureData CreateCultureWithInvariantData()
        {
            // Make a new culturedata
            CultureData invariant = new CultureData();

            // Basics
            // Note that we override the resources since this IS NOT supposed to change (by definition)
            invariant._bUseOverrides = false;
            invariant._sRealName = "";                     // Name you passed in (ie: en-US, en, or de-DE_phoneb)
            invariant._sWindowsName = "";                     // Name OS thinks the object is (ie: de-DE_phoneb, or en-US (even if en was passed in))

            // Identity
            invariant._sName = "";                     // locale name (ie: en-us)
            invariant._sParent = "";                     // Parent name (which may be a custom locale/culture)
            invariant._bNeutral = false;                   // Flags for the culture (ie: neutral or not right now)
            invariant._sEnglishDisplayName = "Invariant Language (Invariant Country)"; // English pretty name for this locale
            invariant._sNativeDisplayName = "Invariant Language (Invariant Country)";  // Native pretty name for this locale
            invariant._sSpecificCulture = "";                     // The culture name to be used in CultureInfo.CreateSpecificCulture()

            // Language
            invariant._sISO639Language = "iv";                   // ISO 639 Language Name
            invariant._sISO639Language2 = "ivl";                  // 3 char ISO 639 lang name 2
            invariant._sLocalizedLanguage = "Invariant Language";   // Display name for this Language
            invariant._sEnglishLanguage = "Invariant Language";   // English name for this language
            invariant._sNativeLanguage = "Invariant Language";   // Native name of this language
            invariant._sAbbrevLang = "IVL";                  // abbreviated language name (Windows Language Name)
            invariant._sConsoleFallbackName = "";            // The culture name for the console fallback UI culture
            invariant._iInputLanguageHandle = 0x07F;         // input language handle

            // Region
            invariant._sRegionName = "IV";                    // (RegionInfo)
            invariant._sEnglishCountry = "Invariant Country"; // english country name (RegionInfo)
            invariant._sNativeCountry = "Invariant Country";  // native country name (Windows Only)
            invariant._sISO3166CountryName = "IV";            // (RegionInfo), ie: US
            invariant._sISO3166CountryName2 = "ivc";          // 3 char ISO 3166 country name 2 2(RegionInfo)
            invariant._iGeoId = 244;                          // GeoId (Windows Only)

            // Numbers
            invariant._sPositiveSign = "+";                    // positive sign
            invariant._sNegativeSign = "-";                    // negative sign
            invariant._iDigits = 2;                      // number of fractional digits
            invariant._iNegativeNumber = 1;                      // negative number format
            invariant._waGrouping = new int[] { 3 };          // grouping of digits
            invariant._sDecimalSeparator = ".";                    // decimal separator
            invariant._sThousandSeparator = ",";                    // thousands separator
            invariant._sNaN = "NaN";                  // Not a Number
            invariant._sPositiveInfinity = "Infinity";             // + Infinity
            invariant._sNegativeInfinity = "-Infinity";            // - Infinity

            // Percent
            invariant._iNegativePercent = 0;                      // Negative Percent (0-3)
            invariant._iPositivePercent = 0;                      // Positive Percent (0-11)
            invariant._sPercent = "%";                    // Percent (%) symbol
            invariant._sPerMille = "\x2030";               // PerMille symbol

            // Currency
            invariant._sCurrency = "\x00a4";                // local monetary symbol: for international monetary symbol
            invariant._sIntlMonetarySymbol = "XDR";                  // international monetary symbol (RegionInfo)
            invariant._sEnglishCurrency = "International Monetary Fund"; // English name for this currency (Windows Only)
            invariant._sNativeCurrency = "International Monetary Fund"; // Native name for this currency (Windows Only)
            invariant._iCurrencyDigits = 2;                      // # local monetary fractional digits
            invariant._iCurrency = 0;                      // positive currency format
            invariant._iNegativeCurrency = 0;                      // negative currency format
            invariant._waMonetaryGrouping = new int[] { 3 };          // monetary grouping of digits
            invariant._sMonetaryDecimal = ".";                    // monetary decimal separator
            invariant._sMonetaryThousand = ",";                    // monetary thousands separator

            // Misc
            invariant._iMeasure = 0;                      // system of measurement 0=metric, 1=US (RegionInfo)
            invariant._sListSeparator = ",";                    // list separator

            // Time
            invariant._sTimeSeparator = ":";
            invariant._sAM1159 = "AM";                   // AM designator
            invariant._sPM2359 = "PM";                   // PM designator
            invariant._saLongTimes = new string[] { "HH:mm:ss" };                             // time format
            invariant._saShortTimes = new string[] { "HH:mm", "hh:mm tt", "H:mm", "h:mm tt" }; // short time format
            invariant._saDurationFormats = new string[] { "HH:mm:ss" };                             // time duration format


            // Calendar specific data
            invariant._iFirstDayOfWeek = 0;                      // first day of week
            invariant._iFirstWeekOfYear = 0;                      // first week of year
            invariant._waCalendars = new CalendarId[] { CalendarId.GREGORIAN };       // all available calendar type(s).  The first one is the default calendar

            // Store for specific data about each calendar
            invariant._calendars = new CalendarData[CalendarData.MAX_CALENDARS];
            invariant._calendars[0] = CalendarData.Invariant;

            // Text information
            invariant._iReadingLayout = 0;

            // These are desktop only, not coreclr

            invariant._iLanguage = CultureInfo.LOCALE_INVARIANT;   // locale ID (0409) - NO sort information
            invariant._iDefaultAnsiCodePage = 1252;         // default ansi code page ID (ACP)
            invariant._iDefaultOemCodePage = 437;           // default oem code page ID (OCP or OEM)
            invariant._iDefaultMacCodePage = 10000;         // default macintosh code page
            invariant._iDefaultEbcdicCodePage = 037;        // default EBCDIC code page

            if (GlobalizationMode.Invariant)
            {
                invariant._sLocalizedDisplayName = invariant._sNativeDisplayName;
                invariant._sLocalizedCountry = invariant._sNativeCountry;
            }

            return invariant;
        }

        /// <summary>
        /// Build our invariant information
        /// We need an invariant instance, which we build hard-coded
        /// </summary>
        internal static CultureData Invariant
        {
            get
            {
                if (s_Invariant == null)
                {
                    // Remember it
                    s_Invariant = CreateCultureWithInvariantData();
                }
                return s_Invariant;
            }
        }
        private volatile static CultureData? s_Invariant;

        // Cache of cultures we've already looked up
        private static volatile StringCultureDataDictionary? s_cachedCultures;
        private static readonly object s_lock = new object();

        internal static CultureData? GetCultureData(string? cultureName, bool useUserOverride)
        {
            // First do a shortcut for Invariant
            if (string.IsNullOrEmpty(cultureName))
            {
                return CultureData.Invariant;
            }

            // Try the hash table first
            string hashName = AnsiToLower(useUserOverride ? cultureName : cultureName + '*');
            StringCultureDataDictionary? tempHashTable = s_cachedCultures;
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
                lock (s_lock)
                {
                    ret = tempHashTable.TryGetValue(hashName, out retVal);
                }
                if (ret && retVal != null)
                {
                    return retVal;
                }
            }

            // Not found in the hash table, need to see if we can build one that works for us
            CultureData? culture = CreateCultureData(cultureName, useUserOverride);
            if (culture == null)
            {
                return null;
            }

            // Found one, add it to the cache
            lock (s_lock)
            {
                tempHashTable[hashName] = culture;
            }

            // Copy the hashtable to the corresponding member variables.  This will potentially overwrite
            // new tables simultaneously created by a new thread, but maximizes thread safety.
            s_cachedCultures = tempHashTable;

            return culture;
        }

        private static string NormalizeCultureName(string name, out bool isNeutralName)
        {
            isNeutralName = true;
            int i = 0;

            if (name.Length > LocaleNameMaxLength)
            {
                // Theoretically we shouldn't hit this exception.
                throw new ArgumentException(SR.Format(SR.Argument_InvalidId, nameof(name)));
            }

            Span<char> normalizedName = stackalloc char[name.Length];

            bool changed = false;

            while (i < name.Length && name[i] != '-' && name[i] != '_')
            {
                if (name[i] >= 'A' && name[i] <= 'Z')
                {
                    // lowercase characters before '-'
                    normalizedName[i] = (char) (((int)name[i]) + 0x20);
                    changed = true;
                }
                else
                {
                    normalizedName[i] = name[i];
                }
                i++;
            }

            if (i < name.Length)
            {
                // this is not perfect to detect the non neutral cultures but it is good enough when we are running in invariant mode
                isNeutralName = false;
            }

            while (i < name.Length)
            {
                if (name[i] >= 'a' && name[i] <= 'z')
                {
                    normalizedName[i] = (char) (((int)name[i]) - 0x20);
                    changed = true;
                }
                else
                {
                    normalizedName[i] = name[i];
                }
                i++;
            }

            if (changed)
            {
                return new string(normalizedName);
            }

            return name;
        }

        private static CultureData? CreateCultureData(string cultureName, bool useUserOverride)
        {
            if (GlobalizationMode.Invariant)
            {
                if (cultureName.Length > LocaleNameMaxLength || !CultureInfo.VerifyCultureName(cultureName, false))
                {
                    return null;
                }
                CultureData cd = CreateCultureWithInvariantData();
                cd._bUseOverrides = useUserOverride;
                cd._sName = NormalizeCultureName(cultureName, out cd._bNeutral);
                cd._sRealName = cd._sName;
                cd._sWindowsName = cd._sName;
                cd._iLanguage = CultureInfo.LOCALE_CUSTOM_UNSPECIFIED;

                return cd;
            }

            CultureData culture = new CultureData();
            culture._bUseOverrides = useUserOverride;
            culture._sRealName = cultureName;

            // Ask native code if that one's real
            if (!culture.InitCultureData() && !culture.InitCompatibilityCultureData())
            {
                return null;
            }

            return culture;
        }

        private bool InitCompatibilityCultureData()
        {
            // for compatibility handle the deprecated ids: zh-chs, zh-cht
            string cultureName = _sRealName!;

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

            _sRealName = fallbackCultureName;
            if (!InitCultureData())
            {
                return false;
            }

            // fixup our data
            _sName = realCultureName; // the name that goes back to the user
            _sParent = fallbackCultureName;

            return true;
        }

        /// We'd rather people use the named version since this doesn't allow custom locales
        internal static CultureData GetCultureData(int culture, bool bUseUserOverride)
        {
            string? localeName = null;
            CultureData? retVal = null;

            if (culture == CultureInfo.LOCALE_INVARIANT)
            {
                return Invariant;
            }

            if (GlobalizationMode.Invariant)
            {
                // LCID is not supported in the InvariantMode
                throw new CultureNotFoundException(nameof(culture), culture, SR.Argument_CultureNotSupported);
            }

            // Convert the lcid to a name, then use that
            // Note that this will return neutral names (unlike Vista native API)
            localeName = LCIDToLocaleName(culture);

            if (!string.IsNullOrEmpty(localeName))
            {
                // Valid name, use it
                retVal = GetCultureData(localeName, bUseUserOverride);
            }

            // If not successful, throw
            if (retVal == null)
            {
                throw new CultureNotFoundException(nameof(culture), culture, SR.Argument_CultureNotSupported);
            }

            // Return the one we found
            return retVal;
        }

        /// <summary>
        /// The real name used to construct the locale (ie: de-DE_phoneb)
        /// </summary>
        internal string CultureName
        {
            get
            {
                Debug.Assert(_sRealName != null, "[CultureData.CultureName] Expected _sRealName to be populated by already");
                // since windows doesn't know about zh-CHS and zh-CHT,
                // we leave sRealName == zh-Hanx but we still need to
                // pretend that it was zh-CHX.
                switch (_sName)
                {
                    case "zh-CHS":
                    case "zh-CHT":
                        return _sName!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/34273
                }
                return _sRealName!;
            }
        }

        /// <summary>
        /// Are overrides enabled?
        /// </summary>
        internal bool UseUserOverride => _bUseOverrides;

        /// <summary>
        /// locale name (ie: de-DE, NO sort information)
        /// </summary>
        internal string Name => _sName ?? string.Empty;

        // Parent name (which may be a custom locale/culture)
        internal string ParentName
        {
            get
            {
                if (_sParent == null)
                {
                    // Ask using the real name, so that we get parents of neutrals
                    _sParent = GetLocaleInfo(_sRealName!, LocaleStringData.ParentName);
                }
                return _sParent;
            }
        }

        // Localized pretty name for this locale (ie: Inglis (estados Unitos))
        internal string DisplayName
        {
            get
            {
                if (_sLocalizedDisplayName == null)
                {
                    if (IsSupplementalCustomCulture)
                    {
                        if (IsNeutralCulture)
                        {
                            _sLocalizedDisplayName = NativeLanguageName;
                        }
                        else
                        {
                            _sLocalizedDisplayName = NativeName;
                        }
                    }
                    else
                    {
                        try
                        {
                            const string ZH_CHT = "zh-CHT";
                            const string ZH_CHS = "zh-CHS";

                            if (Name.Equals(ZH_CHT, StringComparison.OrdinalIgnoreCase))
                            {
                                _sLocalizedDisplayName = GetLanguageDisplayName("zh-Hant");
                            }
                            else if (Name.Equals(ZH_CHS, StringComparison.OrdinalIgnoreCase))
                            {
                                _sLocalizedDisplayName = GetLanguageDisplayName("zh-Hans");
                            }
                            else
                            {
                                _sLocalizedDisplayName = GetLanguageDisplayName(Name);
                            }
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                    }
                    // If it hasn't been found (Windows 8 and up), fallback to the system
                    if (string.IsNullOrEmpty(_sLocalizedDisplayName))
                    {
                        // If its neutral use the language name
                        if (IsNeutralCulture)
                        {
                            _sLocalizedDisplayName = LocalizedLanguageName;
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
                                _sLocalizedDisplayName = NativeName;
                            }
                            else
                            {
                                _sLocalizedDisplayName = GetLocaleInfo(LocaleStringData.LocalizedDisplayName);
                            }
                        }
                    }
                }

                return _sLocalizedDisplayName;
            }
        }

        /// <summary>
        /// English pretty name for this locale (ie: English (United States))
        /// </summary>
        internal string EnglishName
        {
            get
            {
                if (_sEnglishDisplayName == null)
                {
                    // If its neutral use the language name
                    if (IsNeutralCulture)
                    {
                        _sEnglishDisplayName = EnglishLanguageName;
                        // differentiate the legacy display names
                        switch (_sName)
                        {
                            case "zh-CHS":
                            case "zh-CHT":
                                _sEnglishDisplayName += " Legacy";
                                break;
                        }
                    }
                    else
                    {
                        _sEnglishDisplayName = GetLocaleInfo(LocaleStringData.EnglishDisplayName);

                        // if it isn't found build one:
                        if (string.IsNullOrEmpty(_sEnglishDisplayName))
                        {
                            // Our existing names mostly look like:
                            // "English" + "United States" -> "English (United States)"
                            // "Azeri (Latin)" + "Azerbaijan" -> "Azeri (Latin, Azerbaijan)"
                            if (EnglishLanguageName[EnglishLanguageName.Length - 1] == ')')
                            {
                                // "Azeri (Latin)" + "Azerbaijan" -> "Azeri (Latin, Azerbaijan)"
                                _sEnglishDisplayName = string.Concat(
                                    EnglishLanguageName.AsSpan(0, _sEnglishLanguage!.Length - 1),
                                    ", ",
                                    EnglishCountryName,
                                    ")");
                            }
                            else
                            {
                                // "English" + "United States" -> "English (United States)"
                                _sEnglishDisplayName = EnglishLanguageName + " (" + EnglishCountryName + ")";
                            }
                        }
                    }
                }
                return _sEnglishDisplayName;
            }
        }

        /// <summary>
        /// Native pretty name for this locale (ie: Deutsch (Deutschland))
        /// </summary>
        internal string NativeName
        {
            get
            {
                if (_sNativeDisplayName == null)
                {
                    // If its neutral use the language name
                    if (IsNeutralCulture)
                    {
                        _sNativeDisplayName = NativeLanguageName;
                        // differentiate the legacy display names
                        switch (_sName)
                        {
                            case "zh-CHS":
                                _sNativeDisplayName += " \u65E7\u7248";
                                break;
                            case "zh-CHT":
                                _sNativeDisplayName += " \u820A\u7248";
                                break;
                        }
                    }
                    else
                    {
                        _sNativeDisplayName = GetLocaleInfo(LocaleStringData.NativeDisplayName);

                        // if it isn't found build one:
                        if (string.IsNullOrEmpty(_sNativeDisplayName))
                        {
                            // These should primarily be "Deutsch (Deutschland)" type names
                            _sNativeDisplayName = NativeLanguageName + " (" + NativeCountryName + ")";
                        }
                    }
                }
                return _sNativeDisplayName;
            }
        }

        /// <summary>
        /// The culture name to be used in CultureInfo.CreateSpecificCulture()
        /// </summary>
        internal string SpecificCultureName
        {
            get
            {
                // This got populated during the culture initialization
                Debug.Assert(_sSpecificCulture != null, "[CultureData.SpecificCultureName] Expected this.sSpecificCulture to be populated by culture data initialization already");
                return _sSpecificCulture;
            }
        }

        /// <summary>
        /// iso 639 language name, ie: en
        /// </summary>
        internal string TwoLetterISOLanguageName
        {
            get
            {
                if (_sISO639Language == null)
                {
                    _sISO639Language = GetLocaleInfo(LocaleStringData.Iso639LanguageTwoLetterName);
                }
                return _sISO639Language;
            }
        }

        /// <summary>
        /// iso 639 language name, ie: eng
        /// </summary>
        internal string ThreeLetterISOLanguageName
        {
            get
            {
                if (_sISO639Language2 == null)
                {
                    _sISO639Language2 = GetLocaleInfo(LocaleStringData.Iso639LanguageThreeLetterName);
                }
                return _sISO639Language2;
            }
        }

        /// <summary>
        /// abbreviated windows language name (ie: enu) (non-standard, avoid this)
        /// </summary>
        internal string ThreeLetterWindowsLanguageName
        {
            get
            {
                if (_sAbbrevLang == null)
                {
                    _sAbbrevLang = GetThreeLetterWindowsLanguageName(_sRealName!);
                }
                return _sAbbrevLang;
            }
        }

        /// <summary>
        /// Localized name for this language (Windows Only) ie: Inglis
        /// This is only valid for Windows 8 and higher neutrals:
        /// </summary>
        private string LocalizedLanguageName
        {
            get
            {
                if (_sLocalizedLanguage == null)
                {
                    // Usually the UI culture shouldn't be different than what we got from WinRT except
                    // if DefaultThreadCurrentUICulture was set
                    CultureInfo ci;

                    if (CultureInfo.DefaultThreadCurrentUICulture != null &&
                        ((ci = GetUserDefaultCulture()) != null) &&
                        !CultureInfo.DefaultThreadCurrentUICulture!.Name.Equals(ci.Name))
                    {
                        _sLocalizedLanguage = NativeLanguageName;
                    }
                    else
                    {
                        _sLocalizedLanguage = GetLocaleInfo(LocaleStringData.LocalizedLanguageName);
                    }
                }

                return _sLocalizedLanguage;
            }
        }

        /// <summary>
        /// English name for this language (Windows Only) ie: German
        /// </summary>
        private string EnglishLanguageName
        {
            get
            {
                if (_sEnglishLanguage == null)
                {
                    _sEnglishLanguage = GetLocaleInfo(LocaleStringData.EnglishLanguageName);
                }
                return _sEnglishLanguage;
            }
        }

        /// <summary>
        /// Native name of this language (Windows Only) ie: Deutsch
        /// </summary>
        private string NativeLanguageName
        {
            get
            {
                if (_sNativeLanguage == null)
                {
                    _sNativeLanguage = GetLocaleInfo(LocaleStringData.NativeLanguageName);
                }
                return _sNativeLanguage;
            }
        }

        /// <summary>
        /// region name (eg US)
        /// </summary>
        internal string RegionName
        {
            get
            {
                if (_sRegionName == null)
                {
                    _sRegionName = GetLocaleInfo(LocaleStringData.Iso3166CountryName);
                }
                return _sRegionName;
            }
        }

        internal int GeoId
        {
            get
            {
                if (_iGeoId == undef)
                {
                    _iGeoId = GetGeoId(_sRealName!);
                }
                return _iGeoId;
            }
        }

        /// <summary>
        /// localized name for the country
        /// </summary>
        internal string LocalizedCountryName
        {
            get
            {
                if (_sLocalizedCountry == null)
                {
                    try
                    {
                        _sLocalizedCountry = GetRegionDisplayName(TwoLetterISOCountryName);
                    }
                    catch (Exception)
                    {
                        // do nothing. we'll fallback
                    }

                    if (_sLocalizedCountry == null)
                    {
                        _sLocalizedCountry = NativeCountryName;
                    }
                }
                return _sLocalizedCountry;
            }
        }

        /// <summary>
        /// english country name (RegionInfo) ie: Germany
        /// </summary>
        internal string EnglishCountryName
        {
            get
            {
                if (_sEnglishCountry == null)
                {
                    _sEnglishCountry = GetLocaleInfo(LocaleStringData.EnglishCountryName);
                }
                return _sEnglishCountry;
            }
        }

        /// <summary>
        /// native country name (RegionInfo) ie: Deutschland
        /// </summary>
        internal string NativeCountryName
        {
            get
            {
                if (_sNativeCountry == null)
                {
                    _sNativeCountry = GetLocaleInfo(LocaleStringData.NativeCountryName);
                }
                return _sNativeCountry;
            }
        }

        /// <summary>
        /// ISO 3166 Country Name
        /// </summary>
        internal string TwoLetterISOCountryName
        {
            get
            {
                if (_sISO3166CountryName == null)
                {
                    _sISO3166CountryName = GetLocaleInfo(LocaleStringData.Iso3166CountryName);
                }
                return _sISO3166CountryName;
            }
        }

        /// <summary>
        /// 3 letter ISO 3166 country code
        /// </summary>
        internal string ThreeLetterISOCountryName
        {
            get
            {
                if (_sISO3166CountryName2 == null)
                {
                    _sISO3166CountryName2 = GetLocaleInfo(LocaleStringData.Iso3166CountryName2);
                }
                return _sISO3166CountryName2;
            }
        }

        internal int KeyboardLayoutId
        {
            get
            {
                if (_iInputLanguageHandle == undef)
                {
                    if (IsSupplementalCustomCulture)
                    {
                        _iInputLanguageHandle = 0x0409;
                    }
                    else
                    {
                        // Input Language is same as LCID for built-in cultures
                        _iInputLanguageHandle = LCID;
                    }
                }
                return _iInputLanguageHandle;
            }
        }

        /// <summary>
        /// Console fallback name (ie: locale to use for console apps for unicode-only locales)
        /// </summary>
        internal string SCONSOLEFALLBACKNAME
        {
            get
            {
                if (_sConsoleFallbackName == null)
                {
                    _sConsoleFallbackName = GetConsoleFallbackName(_sRealName!);
                }
                return _sConsoleFallbackName;
            }
        }

        /// <summary>
        /// (user can override) grouping of digits
        /// </summary>
        internal int[] NumberGroupSizes
        {
            get
            {
                if (_waGrouping == null)
                {
                    _waGrouping = GetLocaleInfo(LocaleGroupingData.Digit);
                }
                return _waGrouping;
            }
        }

        /// <summary>
        /// Not a Number
        /// </summary>
        private string NaNSymbol
        {
            get
            {
                if (_sNaN == null)
                {
                    _sNaN = GetLocaleInfo(LocaleStringData.NaNSymbol);
                }
                return _sNaN;
            }
        }

        /// <summary>
        /// + Infinity
        /// </summary>
        private string PositiveInfinitySymbol
        {
            get
            {
                if (_sPositiveInfinity == null)
                {
                    _sPositiveInfinity = GetLocaleInfo(LocaleStringData.PositiveInfinitySymbol);
                }
                return _sPositiveInfinity;
            }
        }

        /// <summary>
        /// - Infinity
        /// </summary>
        private string NegativeInfinitySymbol
        {
            get
            {
                if (_sNegativeInfinity == null)
                {
                    _sNegativeInfinity = GetLocaleInfo(LocaleStringData.NegativeInfinitySymbol);
                }
                return _sNegativeInfinity;
            }
        }

        /// <summary>
        /// Negative Percent (0-3)
        /// </summary>
        private int PercentNegativePattern
        {
            get
            {
                if (_iNegativePercent == undef)
                {
                    // Note that <= Windows Vista this is synthesized by native code
                    _iNegativePercent = GetLocaleInfo(LocaleNumberData.NegativePercentFormat);
                }
                return _iNegativePercent;
            }
        }

        /// <summary>
        /// Positive Percent (0-11)
        /// </summary>
        private int PercentPositivePattern
        {
            get
            {
                if (_iPositivePercent == undef)
                {
                    // Note that <= Windows Vista this is synthesized by native code
                    _iPositivePercent = GetLocaleInfo(LocaleNumberData.PositivePercentFormat);
                }
                return _iPositivePercent;
            }
        }

        /// <summary>
        /// Percent (%) symbol
        /// </summary>
        private string PercentSymbol
        {
            get
            {
                if (_sPercent == null)
                {
                    _sPercent = GetLocaleInfo(LocaleStringData.PercentSymbol);
                }
                return _sPercent;
            }
        }

        /// <summary>
        /// PerMille symbol
        /// </summary>
        private string PerMilleSymbol
        {
            get
            {
                if (_sPerMille == null)
                {
                    _sPerMille = GetLocaleInfo(LocaleStringData.PerMilleSymbol);
                }
                return _sPerMille;
            }
        }

        /// <summary>
        /// (user can override) local monetary symbol, eg: $
        /// </summary>
        internal string CurrencySymbol
        {
            get
            {
                if (_sCurrency == null)
                {
                    _sCurrency = GetLocaleInfo(LocaleStringData.MonetarySymbol);
                }
                return _sCurrency;
            }
        }

        /// <summary>
        /// international monetary symbol (RegionInfo), eg: USD
        /// </summary>
        internal string ISOCurrencySymbol
        {
            get
            {
                if (_sIntlMonetarySymbol == null)
                {
                    _sIntlMonetarySymbol = GetLocaleInfo(LocaleStringData.Iso4217MonetarySymbol);
                }
                return _sIntlMonetarySymbol;
            }
        }

        /// <summary>
        /// English name for this currency (RegionInfo), eg: US Dollar
        /// </summary>
        internal string CurrencyEnglishName
        {
            get
            {
                if (_sEnglishCurrency == null)
                {
                    _sEnglishCurrency = GetLocaleInfo(LocaleStringData.CurrencyEnglishName);
                }
                return _sEnglishCurrency;
            }
        }

        /// <summary>
        /// Native name for this currency (RegionInfo), eg: Schweiz Frank
        /// </summary>
        internal string CurrencyNativeName
        {
            get
            {
                if (_sNativeCurrency == null)
                {
                    _sNativeCurrency = GetLocaleInfo(LocaleStringData.CurrencyNativeName);
                }
                return _sNativeCurrency;
            }
        }


        /// <summary>
        /// (user can override) monetary grouping of digits
        /// </summary>
        internal int[] CurrencyGroupSizes
        {
            get
            {
                if (_waMonetaryGrouping == null)
                {
                    _waMonetaryGrouping = GetLocaleInfo(LocaleGroupingData.Monetary);
                }
                return _waMonetaryGrouping;
            }
        }

        /// <summary>
        /// (user can override) system of measurement 0=metric, 1=US (RegionInfo)
        /// </summary>
        internal int MeasurementSystem
        {
            get
            {
                if (_iMeasure == undef)
                {
                    _iMeasure = GetLocaleInfo(LocaleNumberData.MeasurementSystem);
                }
                return _iMeasure;
            }
        }

        /// <summary>
        /// (user can override) list Separator
        /// </summary>
        internal string ListSeparator
        {
            get
            {
                if (_sListSeparator == null)
                {
                    _sListSeparator = GetLocaleInfo(LocaleStringData.ListSeparator);
                }
                return _sListSeparator;
            }
        }

        /// <summary>
        /// (user can override) AM designator
        /// </summary>
        internal string AMDesignator
        {
            get
            {
                if (_sAM1159 == null)
                {
                    _sAM1159 = GetLocaleInfo(LocaleStringData.AMDesignator);
                }
                return _sAM1159;
            }
        }

        /// <summary>
        /// (user can override) PM designator
        /// </summary>
        internal string PMDesignator
        {
            get
            {
                if (_sPM2359 == null)
                {
                    _sPM2359 = GetLocaleInfo(LocaleStringData.PMDesignator);
                }
                return _sPM2359;
            }
        }

        /// <summary>
        /// (user can override) time format
        /// </summary>
        internal string[] LongTimes
        {
            get
            {
                if (_saLongTimes == null)
                {
                    Debug.Assert(!GlobalizationMode.Invariant);

                    string[]? longTimes = GetTimeFormats();
                    if (longTimes == null || longTimes.Length == 0)
                    {
                        _saLongTimes = Invariant._saLongTimes!;
                    }
                    else
                    {
                        _saLongTimes = longTimes;
                    }
                }
                return _saLongTimes;
            }
        }

        /// <summary>
        /// short time format
        /// Short times (derived from long times format)
        /// </summary>
        internal string[] ShortTimes
        {
            get
            {
                if (_saShortTimes == null)
                {
                    Debug.Assert(!GlobalizationMode.Invariant);

                    // Try to get the short times from the OS/culture.dll
                    string[]? shortTimes = GetShortTimeFormats();

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
                    _saShortTimes = shortTimes;
                }
                return _saShortTimes;
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
            string[] longTimes = LongTimes;
            string[] shortTimes = new string[longTimes.Length];

            for (int i = 0; i < longTimes.Length; i++)
            {
                shortTimes[i] = StripSecondsFromPattern(longTimes[i]);
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

                        time = string.Concat(time.AsSpan(0, j), sep, time.AsSpan(endIndex));
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
            bool shouldEscape = false;
            containsSpace = false;
            for (; index < time.Length; index++)
            {
                switch (time[index])
                {
                    case '\'':
                        shouldEscape = !shouldEscape;
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
                        if (shouldEscape)
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
        internal int FirstDayOfWeek
        {
            get
            {
                if (_iFirstDayOfWeek == undef)
                {
                    _iFirstDayOfWeek = GetFirstDayOfWeek();
                }
                return _iFirstDayOfWeek;
            }
        }

        // (user can override) first week of year
        internal int CalendarWeekRule
        {
            get
            {
                if (_iFirstWeekOfYear == undef)
                {
                    _iFirstWeekOfYear = GetLocaleInfo(LocaleNumberData.FirstWeekOfYear);
                }
                return _iFirstWeekOfYear;
            }
        }

        /// <summary>
        /// (user can override default only) short date format
        /// </summary>
        internal string[] ShortDates(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saShortDates;
        }

        /// <summary>
        /// (user can override default only) long date format
        /// </summary>
        internal string[] LongDates(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saLongDates;
        }

        /// <summary>
        /// (user can override) date year/month format.
        /// </summary>
        internal string[] YearMonths(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saYearMonths;
        }

        internal string[] DayNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saDayNames;
        }

        internal string[] AbbreviatedDayNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saAbbrevDayNames;
        }

        internal string[] SuperShortDayNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saSuperShortDayNames;
        }

        internal string[] MonthNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saMonthNames;
        }

        internal string[] GenitiveMonthNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saMonthGenitiveNames;
        }

        internal string[] AbbreviatedMonthNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saAbbrevMonthNames;
        }

        internal string[] AbbreviatedGenitiveMonthNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saAbbrevMonthGenitiveNames;
        }

        /// <remarks>>
        /// Note: This only applies to Hebrew, and it basically adds a "1" to the 6th month name
        /// the non-leap names skip the 7th name in the normal month name array
        /// </remarks>
        internal string[] LeapYearMonthNames(CalendarId calendarId)
        {
            return GetCalendar(calendarId).saLeapYearMonthNames;
        }

        internal string MonthDay(CalendarId calendarId)
        {
            return GetCalendar(calendarId).sMonthDay;
        }

        /// <summary>
        /// All available calendar type(s). The first one is the default calendar.
        /// </summary>
        internal CalendarId[] CalendarIds
        {
            get
            {
                if (_waCalendars == null)
                {
                    // We pass in an array of ints, and native side fills it up with count calendars.
                    // We then have to copy that list to a new array of the right size.
                    // Default calendar should be first
                    CalendarId[] calendars = new CalendarId[23];
                    Debug.Assert(_sWindowsName != null, "[CultureData.CalendarIds] Expected _sWindowsName to be populated by already");
                    int count = CalendarData.GetCalendars(_sWindowsName, _bUseOverrides, calendars);

                    // See if we had a calendar to add.
                    if (count == 0)
                    {
                        // Failed for some reason, just grab Gregorian from Invariant
                        _waCalendars = Invariant._waCalendars!;
                    }
                    else
                    {
                        // The OS may not return calendar 4 for zh-TW, but we've always allowed it.
                        // TODO: Is this hack necessary long-term?
                        if (_sWindowsName == "zh-TW")
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
                        Array.Copy(calendars, 0, temp, 0, count);

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

                        _waCalendars = temp;
                    }
                }

                return _waCalendars;
            }
        }

        /// <summary>
        /// Native calendar names. Index of optional calendar - 1, empty if
        /// no optional calendar at that number
        /// </summary>
        internal string CalendarName(CalendarId calendarId)
        {
            return GetCalendar(calendarId).sNativeName;
        }

        internal CalendarData GetCalendar(CalendarId calendarId)
        {
            Debug.Assert(calendarId > 0 && calendarId <= CalendarId.LAST_CALENDAR,
                "[CultureData.GetCalendar] Expect calendarId to be in a valid range");

            // arrays are 0 based, calendarIds are 1 based
            int calendarIndex = (int)calendarId - 1;

            // Have to have calendars
            if (_calendars == null)
            {
                _calendars = new CalendarData[CalendarData.MAX_CALENDARS];
            }

            // we need the following local variable to avoid returning null
            // when another thread creates a new array of CalendarData (above)
            // right after we insert the newly created CalendarData (below)
            CalendarData? calendarData = _calendars[calendarIndex];
            // Make sure that calendar has data
            if (calendarData == null)
            {
                Debug.Assert(_sWindowsName != null, "[CultureData.GetCalendar] Expected _sWindowsName to be populated by already");
                calendarData = new CalendarData(_sWindowsName, calendarId, UseUserOverride);
                _calendars[calendarIndex] = calendarData;
            }

            return calendarData;
        }

        internal bool IsRightToLeft
        {
            get
            {
                // Returns one of the following 4 reading layout values:
                // 0 - Left to right (eg en-US)
                // 1 - Right to left (eg arabic locales)
                // 2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
                // 3 - Vertical top to bottom with columns proceeding to the right
                return ReadingLayout == 1;
            }
        }

        /// <summary>
        /// Returns one of the following 4 reading layout values:
        /// 0 - Left to right (eg en-US)
        /// 1 - Right to left (eg arabic locales)
        /// 2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
        /// 3 - Vertical top to bottom with columns proceeding to the right
        /// </summary>
        private int ReadingLayout
        {
            get
            {
                if (_iReadingLayout == undef)
                {
                    Debug.Assert(_sRealName != null, "[CultureData.IsRightToLeft] Expected _sRealName to be populated by already");
                    _iReadingLayout = GetLocaleInfo(LocaleNumberData.ReadingLayout);
                }

                return (_iReadingLayout);
            }
        }

        /// <summary>
        /// // Text info name to use for text information
        /// The TextInfo name never includes that alternate sort and is always specific
        /// For customs, it uses the SortLocale (since the textinfo is not exposed in Win7)
        /// en -> en-US
        /// en-US -> en-US
        /// fj (custom neutral) -> en-US (assuming that en-US is the sort locale for fj)
        /// fj_FJ (custom specific) -> en-US (assuming that en-US is the sort locale for fj-FJ)
        /// es-ES_tradnl -> es-ES
        /// </summary>
        internal string TextInfoName
        {
            get
            {
                // Note: Custom cultures might point at another culture's textinfo, however windows knows how
                // to redirect it to the desired textinfo culture, so this is OK.
                Debug.Assert(_sRealName != null, "[CultureData.TextInfoName] Expected _sRealName to be populated by already");
                return _sRealName;
            }
        }

        /// <summary>
        /// Compare info name (including sorting key) to use if custom
        /// </summary>
        internal string SortName
        {
            get
            {
                Debug.Assert(_sRealName != null, "[CultureData.SortName] Expected _sRealName to be populated by already");
                return _sRealName;
            }
        }

        internal bool IsSupplementalCustomCulture => IsCustomCultureId(LCID);

        /// <summary>
        /// Default ansi code page ID (ACP)
        /// </summary>
        internal int ANSICodePage
        {
            get
            {
                if (_iDefaultAnsiCodePage == undef)
                {
                    _iDefaultAnsiCodePage = GetAnsiCodePage(_sRealName!);
                }
                return _iDefaultAnsiCodePage;
            }
        }

        /// <summary>
        /// Default oem code page ID (OCP or OEM).
        /// </summary>
        internal int OEMCodePage
        {
            get
            {
                if (_iDefaultOemCodePage == undef)
                {
                    _iDefaultOemCodePage = GetOemCodePage(_sRealName!);
                }
                return _iDefaultOemCodePage;
            }
        }

        /// <summary>
        /// Default macintosh code page.
        /// </summary>
        internal int MacCodePage
        {
            get
            {
                if (_iDefaultMacCodePage == undef)
                {
                    _iDefaultMacCodePage = GetMacCodePage(_sRealName!);
                }
                return _iDefaultMacCodePage;
            }
        }

        /// <summary>
        /// Default EBCDIC code page.
        /// </summary>
        internal int EBCDICCodePage
        {
            get
            {
                if (_iDefaultEbcdicCodePage == undef)
                {
                    _iDefaultEbcdicCodePage = GetEbcdicCodePage(_sRealName!);
                }
                return _iDefaultEbcdicCodePage;
            }
        }

        internal int LCID
        {
            get
            {
                if (_iLanguage == 0)
                {
                    Debug.Assert(_sRealName != null, "[CultureData.LCID] Expected this.sRealName to be populated already");
                    _iLanguage = LocaleNameToLCID(_sRealName);
                }
                return _iLanguage;
            }
        }

        internal bool IsNeutralCulture
        {
            get
            {
                // InitCultureData told us if we're neutral or not
                return _bNeutral;
            }
        }

        internal bool IsInvariantCulture
        {
            get
            {
                return string.IsNullOrEmpty(Name);
            }
        }

        /// <summary>
        /// Get an instance of our default calendar
        /// </summary>
        internal Calendar DefaultCalendar
        {
            get
            {
                if (GlobalizationMode.Invariant)
                {
                    return CultureInfo.GetCalendarInstance(CalendarIds[0]);
                }

                CalendarId defaultCalId = (CalendarId)GetLocaleInfo(LocaleNumberData.CalendarType);

                if (defaultCalId == 0)
                {
                    defaultCalId = CalendarIds[0];
                }

                return CultureInfo.GetCalendarInstance(defaultCalId);
            }
        }

        /// <summary>
        /// All of our era names
        /// </summary>
        internal string[] EraNames(CalendarId calendarId)
        {
            Debug.Assert(calendarId > 0, "[CultureData.saEraNames] Expected Calendar.ID > 0");
            return GetCalendar(calendarId).saEraNames;
        }

        internal string[] AbbrevEraNames(CalendarId calendarId)
        {
            Debug.Assert(calendarId > 0, "[CultureData.saAbbrevEraNames] Expected Calendar.ID > 0");
            return GetCalendar(calendarId).saAbbrevEraNames;
        }

        internal string[] AbbreviatedEnglishEraNames(CalendarId calendarId)
        {
            Debug.Assert(calendarId > 0, "[CultureData.saAbbrevEraNames] Expected Calendar.ID > 0");
            return GetCalendar(calendarId).saAbbrevEnglishEraNames;
        }

        /// <summary>
        /// Time separator (derived from time format)
        /// </summary>
        internal string TimeSeparator
        {
            get
            {
                if (_sTimeSeparator == null)
                {
                    string? longTimeFormat = GetTimeFormatString();
                    if (string.IsNullOrEmpty(longTimeFormat))
                    {
                        longTimeFormat = LongTimes[0];
                    }

                    // Compute STIME from time format
                    _sTimeSeparator = GetTimeSeparator(longTimeFormat);
                }
                return _sTimeSeparator;
            }
        }

        /// <summary>
        /// Date separator (derived from short date format)
        /// </summary>
        internal string DateSeparator(CalendarId calendarId)
        {
            if (calendarId == CalendarId.JAPAN && !LocalAppContextSwitches.EnforceLegacyJapaneseDateParsing)
            {
                // The date separator is derived from the default short date pattern. So far this pattern is using
                // '/' as date separator when using the Japanese calendar which make the formatting and parsing work fine.
                // changing the default pattern is likely will happen in the near future which can easily break formatting
                // and parsing.
                // We are forcing here the date separator to '/' to ensure the parsing is not going to break when changing
                // the default short date pattern. The application still can override this in the code by DateTimeFormatInfo.DateSeparartor.
                return "/";
            }

            return GetDateSeparator(ShortDates(calendarId)[0]);
        }

        /// <summary>
        /// Unescape a NLS style quote string
        ///
        /// This removes single quotes:
        ///      'fred' -> fred
        ///      'fred -> fred
        ///      fred' -> fred
        ///      fred's -> freds
        ///
        /// This removes the first \ of escaped characters:
        ///      fred\'s -> fred's
        ///      a\\b -> a\b
        ///      a\b -> ab
        ///
        /// We don't build the stringbuilder unless we find a ' or a \.  If we find a ' or a \, we
        /// always build a stringbuilder because we need to remove the ' or \.
        /// </summary>
        private static string UnescapeNlsString(string str, int start, int end)
        {
            Debug.Assert(str != null);
            Debug.Assert(start >= 0);
            Debug.Assert(end >= 0);
            StringBuilder? result = null;

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
            {
                return str.Substring(start, end - start + 1);
            }

            return result.ToString();
        }

        /// <summary>
        /// Time format separator (ie: : in 12:39:00)
        /// We calculate this from the provided time format
        /// </summary>
        private static string GetTimeSeparator(string format)
        {
            //  Find the time separator so that we can pretend we know TimeSeparator.
            return GetSeparator(format, "Hhms");
        }

        /// <summary>
        /// Date format separator (ie: / in 9/1/03)
        /// We calculate this from the provided short date
        /// </summary>
        private static string GetDateSeparator(string format)
        {
            //  Find the date separator so that we can pretend we know DateSeparator.
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

            return string.Empty;
        }

        private static int IndexOfTimePart(string format, int startIndex, string timeParts)
        {
            Debug.Assert(startIndex >= 0, "startIndex cannot be negative");
            Debug.Assert(timeParts.IndexOfAny(new char[] { '\'', '\\' }) == -1, "timeParts cannot include quote characters");
            bool inQuote = false;
            for (int i = startIndex; i < format.Length; ++i)
            {
                // See if we have a time Part
                if (!inQuote && timeParts.Contains(format[i]))
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

        internal static bool IsCustomCultureId(int cultureId)
        {
            return cultureId == CultureInfo.LOCALE_CUSTOM_DEFAULT || cultureId == CultureInfo.LOCALE_CUSTOM_UNSPECIFIED;
        }

        internal void GetNFIValues(NumberFormatInfo nfi)
        {
            if (GlobalizationMode.Invariant || IsInvariantCulture)
            {
                nfi._positiveSign = _sPositiveSign!;
                nfi._negativeSign = _sNegativeSign!;

                nfi._numberGroupSeparator = _sThousandSeparator!;
                nfi._numberDecimalSeparator = _sDecimalSeparator!;
                nfi._numberDecimalDigits = _iDigits;
                nfi._numberNegativePattern = _iNegativeNumber;

                nfi._currencySymbol = _sCurrency!;
                nfi._currencyGroupSeparator = _sMonetaryThousand!;
                nfi._currencyDecimalSeparator = _sMonetaryDecimal!;
                nfi._currencyDecimalDigits = _iCurrencyDigits;
                nfi._currencyNegativePattern = _iNegativeCurrency;
                nfi._currencyPositivePattern = _iCurrency;
            }
            else
            {
                Debug.Assert(_sWindowsName != null, "[CultureData.GetNFIValues] Expected _sWindowsName to be populated by already");
                // String values
                nfi._positiveSign = GetLocaleInfo(LocaleStringData.PositiveSign);
                nfi._negativeSign = GetLocaleInfo(LocaleStringData.NegativeSign);

                nfi._numberDecimalSeparator = GetLocaleInfo(LocaleStringData.DecimalSeparator);
                nfi._numberGroupSeparator = GetLocaleInfo(LocaleStringData.ThousandSeparator);
                nfi._currencyGroupSeparator = GetLocaleInfo(LocaleStringData.MonetaryThousandSeparator);
                nfi._currencyDecimalSeparator = GetLocaleInfo(LocaleStringData.MonetaryDecimalSeparator);
                nfi._currencySymbol = GetLocaleInfo(LocaleStringData.MonetarySymbol);

                // Numeric values
                nfi._numberDecimalDigits = GetLocaleInfo(LocaleNumberData.FractionalDigitsCount);
                nfi._currencyDecimalDigits = GetLocaleInfo(LocaleNumberData.MonetaryFractionalDigitsCount);
                nfi._currencyPositivePattern = GetLocaleInfo(LocaleNumberData.PositiveMonetaryNumberFormat);
                nfi._currencyNegativePattern = GetLocaleInfo(LocaleNumberData.NegativeMonetaryNumberFormat);
                nfi._numberNegativePattern = GetLocaleInfo(LocaleNumberData.NegativeNumberFormat);

                // LOCALE_SNATIVEDIGITS (array of 10 single character strings).
                string digits = GetLocaleInfo(LocaleStringData.Digits);
                nfi._nativeDigits = new string[10];
                for (int i = 0; i < nfi._nativeDigits.Length; i++)
                {
                    nfi._nativeDigits[i] = char.ToString(digits[i]);
                }

                Debug.Assert(_sRealName != null);
                nfi._digitSubstitution = GetDigitSubstitution(_sRealName);
            }

            // Gather additional data
            nfi._numberGroupSizes = NumberGroupSizes;
            nfi._currencyGroupSizes = CurrencyGroupSizes;

            // prefer the cached value since these do not have user overrides
            nfi._percentNegativePattern = PercentNegativePattern;
            nfi._percentPositivePattern = PercentPositivePattern;
            nfi._percentSymbol = PercentSymbol;
            nfi._perMilleSymbol = PerMilleSymbol;

            nfi._negativeInfinitySymbol = NegativeInfinitySymbol;
            nfi._positiveInfinitySymbol = PositiveInfinitySymbol;
            nfi._nanSymbol = NaNSymbol;

            // We don't have percent values, so use the number values
            nfi._percentDecimalDigits = nfi._numberDecimalDigits;
            nfi._percentDecimalSeparator = nfi._numberDecimalSeparator;
            nfi._percentGroupSizes = nfi._numberGroupSizes;
            nfi._percentGroupSeparator = nfi._numberGroupSeparator;

            // Clean up a few odd values

            // Windows usually returns an empty positive sign, but we like it to be "+"
            if (nfi._positiveSign == null || nfi._positiveSign.Length == 0)
            {
                nfi._positiveSign = "+";
            }

            // Special case for Italian.  The currency decimal separator in the control panel is the empty string. When the user
            // specifies C4 as the currency format, this results in the number apparently getting multiplied by 10000 because the
            // decimal point doesn't show up.  We'll just hack this here because our default currency format will never use nfi.
            if (nfi._currencyDecimalSeparator == null || nfi._currencyDecimalSeparator.Length == 0)
            {
                nfi._currencyDecimalSeparator = nfi._numberDecimalSeparator;
            }
        }

        /// <remarks>
        /// This is ONLY used for caching names and shouldn't be used for anything else
        /// </remarks>
        internal static string AnsiToLower(string testString) => TextInfo.ToLowerAsciiInvariant(testString);

        /// <remarks>
        /// The numeric values of the enum members match their Win32 counterparts.  The CultureData Win32 PAL implementation
        /// takes a dependency on this fact, in order to prevent having to construct a mapping from internal values to LCTypes.
        /// </remarks>
        private enum LocaleStringData : uint
        {
            /// <summary>localized name of locale, eg "German (Germany)" in UI language (corresponds to LOCALE_SLOCALIZEDDISPLAYNAME)</summary>
            LocalizedDisplayName = 0x00000002,
            /// <summary>Display name (language + country usually) in English, eg "German (Germany)" (corresponds to LOCALE_SENGLISHDISPLAYNAME)</summary>
            EnglishDisplayName = 0x00000072,
            /// <summary>Display name in native locale language, eg "Deutsch (Deutschland) (corresponds to LOCALE_SNATIVEDISPLAYNAME)</summary>
            NativeDisplayName = 0x00000073,
            /// <summary>Language Display Name for a language, eg "German" in UI language (corresponds to LOCALE_SLOCALIZEDLANGUAGENAME)</summary>
            LocalizedLanguageName = 0x0000006f,
            /// <summary>English name of language, eg "German" (corresponds to LOCALE_SENGLISHLANGUAGENAME)</summary>
            EnglishLanguageName = 0x00001001,
            /// <summary>native name of language, eg "Deutsch" (corresponds to LOCALE_SNATIVELANGUAGENAME)</summary>
            NativeLanguageName = 0x00000004,
            /// <summary>localized name of country, eg "Germany" in UI language (corresponds to LOCALE_SLOCALIZEDCOUNTRYNAME)</summary>
            LocalizedCountryName = 0x00000006,
            /// <summary>English name of country, eg "Germany" (corresponds to LOCALE_SENGLISHCOUNTRYNAME)</summary>
            EnglishCountryName = 0x00001002,
            /// <summary>native name of country, eg "Deutschland" (corresponds to LOCALE_SNATIVECOUNTRYNAME)</summary>
            NativeCountryName = 0x00000008,
            /// <summary>abbreviated language name (corresponds to LOCALE_SABBREVLANGNAME)</summary>
            AbbreviatedWindowsLanguageName = 0x00000003,
            /// <summary>list item separator (corresponds to LOCALE_SLIST)</summary>
            ListSeparator = 0x0000000C,
            /// <summary>decimal separator (corresponds to LOCALE_SDECIMAL)</summary>
            DecimalSeparator = 0x0000000E,
            /// <summary>thousand separator (corresponds to LOCALE_STHOUSAND)</summary>
            ThousandSeparator = 0x0000000F,
            /// <summary>digit grouping (corresponds to LOCALE_SGROUPING)</summary>
            Digits = 0x00000013,
            /// <summary>local monetary symbol (corresponds to LOCALE_SCURRENCY)</summary>
            MonetarySymbol = 0x00000014,
            /// <summary>English currency name (corresponds to LOCALE_SENGCURRNAME)</summary>
            CurrencyEnglishName = 0x00001007,
            /// <summary>Native currency name (corresponds to LOCALE_SNATIVECURRNAME)</summary>
            CurrencyNativeName = 0x00001008,
            /// <summary>uintl monetary symbol (corresponds to LOCALE_SINTLSYMBOL)</summary>
            Iso4217MonetarySymbol = 0x00000015,
            /// <summary>monetary decimal separator (corresponds to LOCALE_SMONDECIMALSEP)</summary>
            MonetaryDecimalSeparator = 0x00000016,
            /// <summary>monetary thousand separator (corresponds to LOCALE_SMONTHOUSANDSEP)</summary>
            MonetaryThousandSeparator = 0x00000017,
            /// <summary>AM designator (corresponds to LOCALE_S1159)</summary>
            AMDesignator = 0x00000028,
            /// <summary>PM designator (corresponds to LOCALE_S2359)</summary>
            PMDesignator = 0x00000029,
            /// <summary>positive sign (corresponds to LOCALE_SPOSITIVESIGN)</summary>
            PositiveSign = 0x00000050,
            /// <summary>negative sign (corresponds to LOCALE_SNEGATIVESIGN)</summary>
            NegativeSign = 0x00000051,
            /// <summary>ISO abbreviated language name (corresponds to LOCALE_SISO639LANGNAME)</summary>
            Iso639LanguageTwoLetterName = 0x00000059,
            /// <summary>ISO abbreviated country name (corresponds to LOCALE_SISO639LANGNAME2)</summary>
            Iso639LanguageThreeLetterName = 0x00000067,
            /// <summary>ISO abbreviated language name (corresponds to LOCALE_SISO639LANGNAME)</summary>
            Iso639LanguageName = 0x00000059,
            /// <summary>ISO abbreviated country name (corresponds to LOCALE_SISO3166CTRYNAME)</summary>
            Iso3166CountryName = 0x0000005A,
            /// <summary>3 letter ISO country code (corresponds to LOCALE_SISO3166CTRYNAME2)</summary>
            Iso3166CountryName2 = 0x00000068,   // 3 character ISO country name
            /// <summary>Not a Number (corresponds to LOCALE_SNAN)</summary>
            NaNSymbol = 0x00000069,
            /// <summary>+ Infinity (corresponds to LOCALE_SPOSINFINITY)</summary>
            PositiveInfinitySymbol = 0x0000006a,
            /// <summary>- Infinity (corresponds to LOCALE_SNEGINFINITY)</summary>
            NegativeInfinitySymbol = 0x0000006b,
            /// <summary>Fallback name for resources (corresponds to LOCALE_SPARENT)</summary>
            ParentName = 0x0000006d,
            /// <summary>Fallback name for within the console (corresponds to LOCALE_SCONSOLEFALLBACKNAME)</summary>
            ConsoleFallbackName = 0x0000006e,
            /// <summary>Returns the percent symbol (corresponds to LOCALE_SPERCENT)</summary>
            PercentSymbol = 0x00000076,
            /// <summary>Returns the permille (U+2030) symbol (corresponds to LOCALE_SPERMILLE)</summary>
            PerMilleSymbol = 0x00000077
        }

        /// <remarks>
        /// The numeric values of the enum members match their Win32 counterparts.  The CultureData Win32 PAL implementation
        /// takes a dependency on this fact, in order to prevent having to construct a mapping from internal values to LCTypes.
        /// </remarks>
        private enum LocaleGroupingData : uint
        {
            /// <summary>digit grouping (corresponds to LOCALE_SGROUPING)</summary>
            Digit = 0x00000010,
            /// <summary>monetary grouping (corresponds to LOCALE_SMONGROUPING)</summary>
            Monetary = 0x00000018,
        }

        /// <remarks>
        /// The numeric values of the enum members match their Win32 counterparts.  The CultureData Win32 PAL implementation
        /// takes a dependency on this fact, in order to prevent having to construct a mapping from internal values to LCTypes.
        /// </remarks>
        private enum LocaleNumberData : uint
        {
            /// <summary>language id (corresponds to LOCALE_ILANGUAGE)</summary>
            LanguageId = 0x00000001,
            /// <summary>geographical location id, (corresponds to LOCALE_IGEOID)</summary>
            GeoId = 0x0000005B,
            /// <summary>0 = context, 1 = none, 2 = national (corresponds to LOCALE_IDIGITSUBSTITUTION)</summary>
            DigitSubstitution = 0x00001014,
            /// <summary>0 = metric, 1 = US (corresponds to LOCALE_IMEASURE)</summary>
            MeasurementSystem = 0x0000000D,
            /// <summary>number of fractional digits (corresponds to LOCALE_IDIGITS)</summary>
            FractionalDigitsCount = 0x00000011,
            /// <summary>negative number mode (corresponds to LOCALE_INEGNUMBER)</summary>
            NegativeNumberFormat = 0x00001010,
            /// <summary># local monetary digits (corresponds to LOCALE_ICURRDIGITS)</summary>
            MonetaryFractionalDigitsCount = 0x00000019,
            /// <summary>positive currency mode (corresponds to LOCALE_ICURRENCY)</summary>
            PositiveMonetaryNumberFormat = 0x0000001B,
            /// <summary>negative currency mode (corresponds to LOCALE_INEGCURR)</summary>
            NegativeMonetaryNumberFormat = 0x0000001C,
            /// <summary>type of calendar specifier (corresponds to LOCALE_ICALENDARTYPE)</summary>
            CalendarType = 0x00001009,
            /// <summary>first day of week specifier (corresponds to LOCALE_IFIRSTDAYOFWEEK)</summary>
            FirstDayOfWeek = 0x0000100C,
            /// <summary>first week of year specifier (corresponds to LOCALE_IFIRSTWEEKOFYEAR)</summary>
            FirstWeekOfYear = 0x0000100D,
            /// <summary>
            /// Returns one of the following 4 reading layout values:
            ///  0 - Left to right (eg en-US)
            ///  1 - Right to left (eg arabic locales)
            ///  2 - Vertical top to bottom with columns to the left and also left to right (ja-JP locales)
            ///  3 - Vertical top to bottom with columns proceeding to the right
            /// (corresponds to LOCALE_IREADINGLAYOUT)
            /// </summary>
            ReadingLayout = 0x00000070,
            /// <summary>Returns 0-11 for the negative percent format (corresponds to LOCALE_INEGATIVEPERCENT)</summary>
            NegativePercentFormat = 0x00000074,
            /// <summary>Returns 0-3 for the positive percent format (corresponds to LOCALE_IPOSITIVEPERCENT)</summary>
            PositivePercentFormat = 0x00000075,
            /// <summary>default ansi code page (corresponds to LOCALE_IDEFAULTCODEPAGE)</summary>
            OemCodePage = 0x0000000B,
            /// <summary>default ansi code page (corresponds to LOCALE_IDEFAULTANSICODEPAGE)</summary>
            AnsiCodePage = 0x00001004,
            /// <summary>default mac code page (corresponds to LOCALE_IDEFAULTMACCODEPAGE)</summary>
            MacCodePage = 0x00001011,
            /// <summary>default ebcdic code page (corresponds to LOCALE_IDEFAULTEBCDICCODEPAGE)</summary>
            EbcdicCodePage = 0x00001012,
        }
    }
}
