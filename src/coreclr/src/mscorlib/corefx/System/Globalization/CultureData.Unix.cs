using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    internal partial class CultureData
    {       
        /// <summary>
        /// This method uses the sRealName field (which is initialized by the constructor before this is called) to
        /// initialize the rest of the state of CultureData based on the underlying OS globalization library.
        /// </summary>
        private unsafe bool InitCultureData()
        {
            Contract.Assert(this.sRealName != null);

            // This is a bit of misnomer, since it doesn't have anything to do with Windows.  Instead, this is the
            // identifier the underlying OS uses for the culture.
            this.sWindowsName = AnsiToLower(this.sRealName);

            // For now, just use all of the Invariant's data
            // TODO: Implement this fully.
            CultureData invariant = CultureData.Invariant;

            // Identity
            this.sName = invariant.sName;
            this.sParent = invariant.sParent;
            this.bNeutral = invariant.bNeutral;
            this.sEnglishDisplayName = invariant.sEnglishDisplayName;
            this.sNativeDisplayName = invariant.sNativeDisplayName;
            this.sSpecificCulture = invariant.sSpecificCulture;

            // Language
            this.sISO639Language = invariant.sISO639Language;
            this.sLocalizedLanguage = invariant.sLocalizedLanguage;
            this.sEnglishLanguage = invariant.sEnglishLanguage;
            this.sNativeLanguage = invariant.sNativeLanguage;

            // Region
            this.sRegionName = invariant.sRegionName;
            this.sEnglishCountry = invariant.sEnglishCountry;
            this.sNativeCountry = invariant.sNativeCountry;
            this.sISO3166CountryName = invariant.sISO3166CountryName;

            // Numbers
            this.sPositiveSign = invariant.sPositiveSign;
            this.sNegativeSign = invariant.sNegativeSign;
            this.saNativeDigits = invariant.saNativeDigits;
            this.iDigits = invariant.iDigits;
            this.iNegativeNumber = invariant.iNegativeNumber;
            this.waGrouping = invariant.waGrouping;
            this.sDecimalSeparator = invariant.sDecimalSeparator;
            this.sThousandSeparator = invariant.sThousandSeparator;
            this.sNaN = invariant.sNaN;
            this.sPositiveInfinity = invariant.sPositiveInfinity;
            this.sNegativeInfinity = invariant.sNegativeInfinity;

            // Percent
            this.iNegativePercent = invariant.iNegativePercent;
            this.iPositivePercent = invariant.iPositivePercent;
            this.sPercent = invariant.sPercent;
            this.sPerMille = invariant.sPerMille;

            // Currency
            this.sCurrency = invariant.sCurrency;
            this.sIntlMonetarySymbol = invariant.sIntlMonetarySymbol;
            this.iCurrencyDigits = invariant.iCurrencyDigits;
            this.iCurrency = invariant.iCurrency;
            this.iNegativeCurrency = invariant.iNegativeCurrency;
            this.waMonetaryGrouping = invariant.waMonetaryGrouping;
            this.sMonetaryDecimal = invariant.sMonetaryDecimal;
            this.sMonetaryThousand = invariant.sMonetaryThousand;

            // Misc
            this.iMeasure = invariant.iMeasure;
            this.sListSeparator = invariant.sListSeparator;

            // Time
            this.sAM1159 = invariant.sAM1159;
            this.sPM2359 = invariant.sPM2359;
            this.saLongTimes = invariant.saLongTimes;
            this.saShortTimes = invariant.saShortTimes;
            this.saDurationFormats = invariant.saDurationFormats;

            // Calendar specific data
            this.iFirstDayOfWeek = invariant.iFirstDayOfWeek;
            this.iFirstWeekOfYear = invariant.iFirstWeekOfYear;
            this.waCalendars = invariant.waCalendars;

            // Store for specific data about each calendar
            this.calendars = invariant.calendars;

            // Text information
            this.iReadingLayout = invariant.iReadingLayout;

            return true;
        }

        private string GetLocaleInfo(LocaleStringData type)
        {
            // TODO: Implement this fully.
            return GetLocaleInfo("", type);
        }

        // For LOCALE_SPARENT we need the option of using the "real" name (forcing neutral names) instead of the
        // "windows" name, which can be specific for downlevel (< windows 7) os's.
        private string GetLocaleInfo(string localeName, LocaleStringData type)
        {                        
            // TODO: Implement this fully.            
            switch(type)
            {
                case LocaleStringData.LocalizedDisplayName:
                    return "Invariant Language (Invariant Country)";
                case LocaleStringData.EnglishDisplayName:
                    return "Invariant Language (Invariant Country)";
                case LocaleStringData.NativeDisplayName:
                    return "Invariant Language (Invariant Country)";
                case LocaleStringData.LocalizedLanguageName:
                    return "Invariant Language";
                case LocaleStringData.EnglishLanguageName:
                    return "Invariant Language";
                case LocaleStringData.NativeLanguageName:
                    return "Invariant Language";
                case LocaleStringData.EnglishCountryName:
                    return "Invariant Country";
                case LocaleStringData.NativeCountryName:
                    return "Invariant Country";
                case LocaleStringData.ListSeparator:
                    return ",";
                case LocaleStringData.DecimalSeparator:
                    return ".";
                case LocaleStringData.ThousandSeparator:
                    return ",";
                case LocaleStringData.Digits:
                    return "3;0";
                case LocaleStringData.MonetarySymbol:
                    return "$"; // TODO: CoreFX #846 Restore to the original value "\x00a4"
                case LocaleStringData.Iso4217MonetarySymbol:
                    return "XDR";
                case LocaleStringData.MonetaryDecimalSeparator:
                    return ".";
                case LocaleStringData.MonetaryThousandSeparator:
                    return ",";
                case LocaleStringData.AMDesignator:
                    return "AM";
                case LocaleStringData.PMDesignator:
                    return "PM";
                case LocaleStringData.PositiveSign:
                    return "+";
                case LocaleStringData.NegativeSign:
                    return "-";
                case LocaleStringData.Iso639LanguageName:
                    return "iv";
                case LocaleStringData.Iso3166CountryName:
                    return "IV";
                case LocaleStringData.NaNSymbol:
                    return "NaN";
                case LocaleStringData.PositiveInfinitySymbol:
                    return "Infinity";
                case LocaleStringData.NegativeInfinitySymbol:
                    return "-Infinity";
                case LocaleStringData.ParentName:
                    return "";
                case LocaleStringData.PercentSymbol:
                    return "%";
                case LocaleStringData.PerMilleSymbol:
                    return "\u2030";
                default:
                    Contract.Assert(false, "Unmatched case in GetLocaleInfo(LocaleStringData)");
                    throw new NotImplementedException();
            }
        }

        private int GetLocaleInfo(LocaleNumberData type)
        {
            // TODO: Implement this fully.
            switch (type)
            {
                case LocaleNumberData.LanguageId:
                    return 127;
                case LocaleNumberData.MeasurementSystem:
                    return 0;
                case LocaleNumberData.FractionalDigitsCount:
                    return 2;
                case LocaleNumberData.NegativeNumberFormat:
                    return 1;
                case LocaleNumberData.MonetaryFractionalDigitsCount:
                    return 2;
                case LocaleNumberData.PositiveMonetaryNumberFormat:
                    return 0;
                case LocaleNumberData.NegativeMonetaryNumberFormat:
                    return 0;
                case LocaleNumberData.CalendarType:
                    return 1;
                case LocaleNumberData.FirstWeekOfYear:
                    return 0;
                case LocaleNumberData.ReadingLayout:
                    return 0;
                case LocaleNumberData.NegativePercentFormat:
                    return 0;
                case LocaleNumberData.PositivePercentFormat:
                    return 0;
                default:
                    Contract.Assert(false, "Unmatched case in GetLocaleInfo(LocaleNumberData)");
                    throw new NotImplementedException();
            }
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
            // TODO: Implement this fully.
            if (cultureName == "")
            {
                return "Invariant Language";
            }

            throw new NotImplementedException();
        }

        private static string GetRegionDisplayName(string isoCountryCode)
        {
            // TODO: Implement this fully.
            return "";
        }

        private static CultureInfo GetUserDefaultCulture()
        {
            // TODO: Implement this fully.
            return CultureInfo.InvariantCulture;
        }

        private static bool IsCustomCultureId(int cultureId)
        {
            // TODO: Implement this fully.
            return false;
        }

        // PAL methods end here.
    }
}