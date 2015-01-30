using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    internal partial class CultureData
    {       
        /// <summary>
        /// Check with the OS to see if this is a valid culture.
        /// If so we populate a limited number of fields.  If its not valid we return false.
        ///
        /// The fields we populate:
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
        ///
        /// bNeutral -- TRUE if it is a neutral locale
        ///
        /// For a neutral we just populate the neutral name, but we leave the windows name pointing to the
        /// windows locale that's going to provide data for us.
        /// </summary>
        private unsafe bool InitCultureData()
        {
            // TODO: Implement this fully.
            sWindowsName = "";
            sRealName = "";
            sSpecificCulture = "";
            sName = "";
            bNeutral = false;

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
                    return "\u00A4";
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
            throw new NotImplementedException();
        }

        private static CultureInfo GetUserDefaultCulture()
        {
            // TODO: Implement this fully.
            return CultureInfo.InvariantCulture;
        }

        private static bool IsCustomCultureId(int cultureId)
        {
           // TODO: Implement this fully.
            throw new NotImplementedException();
        }

        // PAL methods end here.
    }
}