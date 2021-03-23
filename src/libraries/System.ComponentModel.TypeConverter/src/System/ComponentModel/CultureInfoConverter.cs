// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert <see cref='System.Globalization.CultureInfo'/>
    /// objects to and from various other representations.
    /// </summary>
    public class CultureInfoConverter : TypeConverter
    {
        private StandardValuesCollection _values;

        /// <summary>
        /// Retrieves the "default" name for our culture.
        /// </summary>
        private string DefaultCultureString => SR.CultureInfoConverterDefaultCultureString;

        private const string DefaultInvariantCultureString = "(Default)";

        /// <summary>
        /// Retrieves the Name for a input CultureInfo.
        /// </summary>
        protected virtual string GetCultureName(CultureInfo culture)
        {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            return culture.Name;
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object in the given
        /// source type to a System.Globalization.CultureInfo object using the specified context.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object to
        /// the given destination type using the context.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the specified value object to a <see cref='System.Globalization.CultureInfo'/>
        /// object.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            // Only when GetCultureName returns culture.Name, we use CultureInfoMapper
            // (Since CultureInfoMapper will transfer Culture.DisplayName to Culture.Name).
            // Otherwise, we just keep the value unchanged.
            if (value is string text)
            {
                if (GetCultureName(CultureInfo.InvariantCulture).Equals(string.Empty))
                {
                    text = CultureInfoMapper.GetCultureInfoName((string)value);
                }

                string defaultCultureString = DefaultCultureString;
                if (culture != null && culture.Equals(CultureInfo.InvariantCulture))
                {
                    defaultCultureString = DefaultInvariantCultureString;
                }

                // Look for the default culture info.
                CultureInfo retVal = null;
                if (string.IsNullOrEmpty(text) || string.Equals(text, defaultCultureString, StringComparison.Ordinal))
                {
                    retVal = CultureInfo.InvariantCulture;
                }

                // Now look in our set of installed cultures.
                if (retVal == null)
                {
                    foreach (CultureInfo info in GetStandardValues(context))
                    {
                        if (info != null && string.Equals(GetCultureName(info), text, StringComparison.Ordinal))
                        {
                            retVal = info;
                            break;
                        }
                    }
                }

                // Now try to create a new culture info from this value.
                if (retVal == null)
                {
                    try
                    {
                        retVal = new CultureInfo(text);
                    }
                    catch
                    {
                    }
                }

                // Finally, try to find a partial match.
                if (retVal == null)
                {
                    foreach (CultureInfo info in _values)
                    {
                        if (info != null && GetCultureName(info).StartsWith(text, StringComparison.CurrentCulture))
                        {
                            retVal = info;
                            break;
                        }
                    }
                }

                // No good. We can't support it.
                if (retVal == null)
                {
                    throw new ArgumentException(SR.Format(SR.CultureInfoConverterInvalidCulture, (string)value), nameof(value));
                }

                return retVal;
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Converts the given value object to the specified destination type.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                string defaultCultureString = DefaultCultureString;
                if (culture != null && culture.Equals(CultureInfo.InvariantCulture))
                {
                    defaultCultureString = DefaultInvariantCultureString;
                }

                if (value == null || value == CultureInfo.InvariantCulture)
                {
                    return defaultCultureString;
                }
                if (value is CultureInfo c)
                {
                    return GetCultureName(c);
                }
            }
            if (destinationType == typeof(InstanceDescriptor) && value is CultureInfo cultureValue)
            {
                return new InstanceDescriptor(
                    typeof(CultureInfo).GetConstructor(new Type[] { typeof(string) }),
                    new object[] { cultureValue.Name }
                );
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Gets a collection of standard values collection for a System.Globalization.CultureInfo
        /// object using the specified context.
        /// </summary>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (_values == null)
            {
                CultureInfo[] installedCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures | CultureTypes.NeutralCultures);
                int invariantIndex = Array.IndexOf(installedCultures, CultureInfo.InvariantCulture);

                CultureInfo[] array;
                if (invariantIndex != -1)
                {
                    Debug.Assert(invariantIndex >= 0 && invariantIndex < installedCultures.Length);
                    installedCultures[invariantIndex] = null;
                    array = new CultureInfo[installedCultures.Length];
                }
                else
                {
                    array = new CultureInfo[installedCultures.Length + 1];
                }

                Array.Copy(installedCultures, array, installedCultures.Length);
                Array.Sort(array, new CultureComparer(this));
                Debug.Assert(array[0] == null);
                if (array[0] == null)
                {
                    //we replace null with the real default culture because there are code paths
                    // where the propgrid will send values from this returned array directly -- instead
                    // of converting it to a string and then back to a value (which this relied on).
                    array[0] = CultureInfo.InvariantCulture; //null isn't the value here -- invariantculture is.
                }

                _values = new StandardValuesCollection(array);
            }

            return _values;
        }

        /// <summary>
        /// Gets a value indicating whether the list of standard values returned from
        /// System.ComponentModel.CultureInfoConverter.GetStandardValues is an exclusive list.
        /// </summary>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;

        /// <summary>
        /// Gets a value indicating whether this object supports a standard set
        /// of values that can be picked from a list using the specified context.
        /// </summary>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

        /// <summary>
        /// IComparer object used for sorting CultureInfos
        /// WARNING:  If you change where null is positioned, then you must fix CultureConverter.GetStandardValues!
        /// </summary>
        private sealed class CultureComparer : IComparer
        {
            private readonly CultureInfoConverter _converter;

            public CultureComparer(CultureInfoConverter cultureConverter)
            {
                Debug.Assert(cultureConverter != null);
                _converter = cultureConverter;
            }

            public int Compare(object item1, object item2)
            {
                if (item1 == null)
                {
                    // If both are null, then they are equal.
                    if (item2 == null)
                    {
                        return 0;
                    }

                    // Otherwise, item1 is null, but item2 is valid (greater).
                    return -1;
                }

                if (item2 == null)
                {
                    // item2 is null, so item 1 is greater.
                    return 1;
                }

                string itemName1 = _converter.GetCultureName(((CultureInfo)item1));
                string itemName2 = _converter.GetCultureName(((CultureInfo)item2));

                CompareInfo compInfo = (CultureInfo.CurrentCulture).CompareInfo;
                return compInfo.Compare(itemName1, itemName2, CompareOptions.StringSort);
            }
        }

        private static class CultureInfoMapper
        {
            /// Dictionary of CultureInfo.DisplayName, CultureInfo.Name for cultures that have changed DisplayName over releases.
            /// This is to workaround an issue with CultureInfoConverter that serializes DisplayName (fixing it would introduce breaking changes).
            private static readonly Dictionary<string, string> s_cultureInfoNameMap = CreateMap();

            private static Dictionary<string, string> CreateMap()
            {
                const int Count = 274;
                var result = new Dictionary<string, string>(Count)
                {
                    {"Afrikaans", "af"},
                    {"Afrikaans (South Africa)", "af-ZA"},
                    {"Albanian", "sq"},
                    {"Albanian (Albania)", "sq-AL"},
                    {"Alsatian (France)", "gsw-FR"},
                    {"Amharic (Ethiopia)", "am-ET"},
                    {"Arabic", "ar"},
                    {"Arabic (Algeria)", "ar-DZ"},
                    {"Arabic (Bahrain)", "ar-BH"},
                    {"Arabic (Egypt)", "ar-EG"},
                    {"Arabic (Iraq)", "ar-IQ"},
                    {"Arabic (Jordan)", "ar-JO"},
                    {"Arabic (Kuwait)", "ar-KW"},
                    {"Arabic (Lebanon)", "ar-LB"},
                    {"Arabic (Libya)", "ar-LY"},
                    {"Arabic (Morocco)", "ar-MA"},
                    {"Arabic (Oman)", "ar-OM"},
                    {"Arabic (Qatar)", "ar-QA"},
                    {"Arabic (Saudi Arabia)", "ar-SA"},
                    {"Arabic (Syria)", "ar-SY"},
                    {"Arabic (Tunisia)", "ar-TN"},
                    {"Arabic (U.A.E.)", "ar-AE"},
                    {"Arabic (Yemen)", "ar-YE"},
                    {"Armenian", "hy"},
                    {"Armenian (Armenia)", "hy-AM"},
                    {"Assamese (India)", "as-IN"},
                    {"Azeri", "az"},
                    {"Azeri (Cyrillic, Azerbaijan)", "az-Cyrl-AZ"},
                    {"Azeri (Latin, Azerbaijan)", "az-Latn-AZ"},
                    {"Bashkir (Russia)", "ba-RU"},
                    {"Basque", "eu"},
                    {"Basque (Basque)", "eu-ES"},
                    {"Belarusian", "be"},
                    {"Belarusian (Belarus)", "be-BY"},
                    {"Bengali (Bangladesh)", "bn-BD"},
                    {"Bengali (India)", "bn-IN"},
                    {"Bosnian (Cyrillic, Bosnia and Herzegovina)", "bs-Cyrl-BA"},
                    {"Bosnian (Latin, Bosnia and Herzegovina)", "bs-Latn-BA"},
                    {"Breton (France)", "br-FR"},
                    {"Bulgarian", "bg"},
                    {"Bulgarian (Bulgaria)", "bg-BG"},
                    {"Catalan", "ca"},
                    {"Catalan (Catalan)", "ca-ES"},
                    {"Chinese (Hong Kong S.A.R.)", "zh-HK"},
                    {"Chinese (Macao S.A.R.)", "zh-MO"},
                    {"Chinese (People's Republic of China)", "zh-CN"},
                    {"Chinese (Simplified)", "zh-CHS"},
                    {"Chinese (Singapore)", "zh-SG"},
                    {"Chinese (Taiwan)", "zh-TW"},
                    {"Chinese (Traditional)", "zh-CHT"},
                    {"Corsican (France)", "co-FR"},
                    {"Croatian", "hr"},
                    {"Croatian (Croatia)", "hr-HR"},
                    {"Croatian (Latin, Bosnia and Herzegovina)", "hr-BA"},
                    {"Czech", "cs"},
                    {"Czech (Czech Republic)", "cs-CZ"},
                    {"Danish", "da"},
                    {"Danish (Denmark)", "da-DK"},
                    {"Dari (Afghanistan)", "prs-AF"},
                    {"Divehi", "dv"},
                    {"Divehi (Maldives)", "dv-MV"},
                    {"Dutch", "nl"},
                    {"Dutch (Belgium)", "nl-BE"},
                    {"Dutch (Netherlands)", "nl-NL"},
                    {"English", "en"},
                    {"English (Australia)", "en-AU"},
                    {"English (Belize)", "en-BZ"},
                    {"English (Canada)", "en-CA"},
                    {"English (Caribbean)", "en-029"},
                    {"English (India)", "en-IN"},
                    {"English (Ireland)", "en-IE"},
                    {"English (Jamaica)", "en-JM"},
                    {"English (Malaysia)", "en-MY"},
                    {"English (New Zealand)", "en-NZ"},
                    {"English (Republic of the Philippines)", "en-PH"},
                    {"English (Singapore)", "en-SG"},
                    {"English (South Africa)", "en-ZA"},
                    {"English (Trinidad and Tobago)", "en-TT"},
                    {"English (United Kingdom)", "en-GB"},
                    {"English (United States)", "en-US"},
                    {"English (Zimbabwe)", "en-ZW"},
                    {"Estonian", "et"},
                    {"Estonian (Estonia)", "et-EE"},
                    {"Faroese", "fo"},
                    {"Faroese (Faroe Islands)", "fo-FO"},
                    {"Filipino (Philippines)", "fil-PH"},
                    {"Finnish", "fi"},
                    {"Finnish (Finland)", "fi-FI"},
                    {"French", "fr"},
                    {"French (Belgium)", "fr-BE"},
                    {"French (Canada)", "fr-CA"},
                    {"French (France)", "fr-FR"},
                    {"French (Luxembourg)", "fr-LU"},
                    {"French (Principality of Monaco)", "fr-MC"},
                    {"French (Switzerland)", "fr-CH"},
                    {"Frisian (Netherlands)", "fy-NL"},
                    {"Galician", "gl"},
                    {"Galician (Galician)", "gl-ES"},
                    {"Georgian", "ka"},
                    {"Georgian (Georgia)", "ka-GE"},
                    {"German", "de"},
                    {"German (Austria)", "de-AT"},
                    {"German (Germany)", "de-DE"},
                    {"German (Liechtenstein)", "de-LI"},
                    {"German (Luxembourg)", "de-LU"},
                    {"German (Switzerland)", "de-CH"},
                    {"Greek", "el"},
                    {"Greek (Greece)", "el-GR"},
                    {"Greenlandic (Greenland)", "kl-GL"},
                    {"Gujarati", "gu"},
                    {"Gujarati (India)", "gu-IN"},
                    {"Hausa (Latin, Nigeria)", "ha-Latn-NG"},
                    {"Hebrew", "he"},
                    {"Hebrew (Israel)", "he-IL"},
                    {"Hindi", "hi"},
                    {"Hindi (India)", "hi-IN"},
                    {"Hungarian", "hu"},
                    {"Hungarian (Hungary)", "hu-HU"},
                    {"Icelandic", "is"},
                    {"Icelandic (Iceland)", "is-IS"},
                    {"Igbo (Nigeria)", "ig-NG"},
                    {"Indonesian", "id"},
                    {"Indonesian (Indonesia)", "id-ID"},
                    {"Inuktitut (Latin, Canada)", "iu-Latn-CA"},
                    {"Inuktitut (Syllabics, Canada)", "iu-Cans-CA"},
                    {"Invariant Language (Invariant Country)", ""},
                    {"Irish (Ireland)", "ga-IE"},
                    {"isiXhosa (South Africa)", "xh-ZA"},
                    {"isiZulu (South Africa)", "zu-ZA"},
                    {"Italian", "it"},
                    {"Italian (Italy)", "it-IT"},
                    {"Italian (Switzerland)", "it-CH"},
                    {"Japanese", "ja"},
                    {"Japanese (Japan)", "ja-JP"},
                    {"K'iche (Guatemala)", "qut-GT"},
                    {"Kannada", "kn"},
                    {"Kannada (India)", "kn-IN"},
                    {"Kazakh", "kk"},
                    {"Kazakh (Kazakhstan)", "kk-KZ"},
                    {"Khmer (Cambodia)", "km-KH"},
                    {"Kinyarwanda (Rwanda)", "rw-RW"},
                    {"Kiswahili", "sw"},
                    {"Kiswahili (Kenya)", "sw-KE"},
                    {"Konkani", "kok"},
                    {"Konkani (India)", "kok-IN"},
                    {"Korean", "ko"},
                    {"Korean (Korea)", "ko-KR"},
                    {"Kyrgyz", "ky"},
                    {"Kyrgyz (Kyrgyzstan)", "ky-KG"},
                    {"Lao (Lao P.D.R.)", "lo-LA"},
                    {"Latvian", "lv"},
                    {"Latvian (Latvia)", "lv-LV"},
                    {"Lithuanian", "lt"},
                    {"Lithuanian (Lithuania)", "lt-LT"},
                    {"Lower Sorbian (Germany)", "dsb-DE"},
                    {"Luxembourgish (Luxembourg)", "lb-LU"},
                    {"Macedonian", "mk"},
                    {"Macedonian (North Macedonia)", "mk-MK"},
                    {"Malay", "ms"},
                    {"Malay (Brunei Darussalam)", "ms-BN"},
                    {"Malay (Malaysia)", "ms-MY"},
                    {"Malayalam (India)", "ml-IN"},
                    {"Maltese (Malta)", "mt-MT"},
                    {"Maori (New Zealand)", "mi-NZ"},
                    {"Mapudungun (Chile)", "arn-CL"},
                    {"Marathi", "mr"},
                    {"Marathi (India)", "mr-IN"},
                    {"Mohawk (Mohawk)", "moh-CA"},
                    {"Mongolian", "mn"},
                    {"Mongolian (Cyrillic, Mongolia)", "mn-MN"},
                    {"Mongolian (Traditional Mongolian, PRC)", "mn-Mong-CN"},
                    {"Nepali (Nepal)", "ne-NP"},
                    {"Norwegian", "no"},
                    {"Norwegian, Bokm\u00E5l (Norway)", "nb-NO"},
                    {"Norwegian, Nynorsk (Norway)", "nn-NO"},
                    {"Occitan (France)", "oc-FR"},
                    {"Oriya (India)", "or-IN"},
                    {"Pashto (Afghanistan)", "ps-AF"},
                    {"Persian", "fa"},
                    {"Persian (Iran)", "fa-IR"},
                    {"Polish", "pl"},
                    {"Polish (Poland)", "pl-PL"},
                    {"Portuguese", "pt"},
                    {"Portuguese (Brazil)", "pt-BR"},
                    {"Portuguese (Portugal)", "pt-PT"},
                    {"Punjabi", "pa"},
                    {"Punjabi (India)", "pa-IN"},
                    {"Quechua (Bolivia)", "quz-BO"},
                    {"Quechua (Ecuador)", "quz-EC"},
                    {"Quechua (Peru)", "quz-PE"},
                    {"Romanian", "ro"},
                    {"Romanian (Romania)", "ro-RO"},
                    {"Romansh (Switzerland)", "rm-CH"},
                    {"Russian", "ru"},
                    {"Russian (Russia)", "ru-RU"},
                    {"Sami, Inari (Finland)", "smn-FI"},
                    {"Sami, Lule (Norway)", "smj-NO"},
                    {"Sami, Lule (Sweden)", "smj-SE"},
                    {"Sami, Northern (Finland)", "se-FI"},
                    {"Sami, Northern (Norway)", "se-NO"},
                    {"Sami, Northern (Sweden)", "se-SE"},
                    {"Sami, Skolt (Finland)", "sms-FI"},
                    {"Sami, Southern (Norway)", "sma-NO"},
                    {"Sami, Southern (Sweden)", "sma-SE"},
                    {"Sanskrit", "sa"},
                    {"Sanskrit (India)", "sa-IN"},
                    {"Serbian", "sr"},
                    {"Serbian (Cyrillic, Bosnia and Herzegovina)", "sr-Cyrl-BA"},
                    {"Serbian (Cyrillic, Serbia)", "sr-Cyrl-CS"},
                    {"Serbian (Latin, Bosnia and Herzegovina)", "sr-Latn-BA"},
                    {"Serbian (Latin, Serbia)", "sr-Latn-CS"},
                    {"Sesotho sa Leboa (South Africa)", "nso-ZA"},
                    {"Setswana (South Africa)", "tn-ZA"},
                    {"Sinhala (Sri Lanka)", "si-LK"},
                    {"Slovak", "sk"},
                    {"Slovak (Slovakia)", "sk-SK"},
                    {"Slovenian", "sl"},
                    {"Slovenian (Slovenia)", "sl-SI"},
                    {"Spanish", "es"},
                    {"Spanish (Argentina)", "es-AR"},
                    {"Spanish (Bolivia)", "es-BO"},
                    {"Spanish (Chile)", "es-CL"},
                    {"Spanish (Colombia)", "es-CO"},
                    {"Spanish (Costa Rica)", "es-CR"},
                    {"Spanish (Dominican Republic)", "es-DO"},
                    {"Spanish (Ecuador)", "es-EC"},
                    {"Spanish (El Salvador)", "es-SV"},
                    {"Spanish (Guatemala)", "es-GT"},
                    {"Spanish (Honduras)", "es-HN"},
                    {"Spanish (Mexico)", "es-MX"},
                    {"Spanish (Nicaragua)", "es-NI"},
                    {"Spanish (Panama)", "es-PA"},
                    {"Spanish (Paraguay)", "es-PY"},
                    {"Spanish (Peru)", "es-PE"},
                    {"Spanish (Puerto Rico)", "es-PR"},
                    {"Spanish (Spain)", "es-ES"},
                    {"Spanish (United States)", "es-US"},
                    {"Spanish (Uruguay)", "es-UY"},
                    {"Spanish (Venezuela)", "es-VE"},
                    {"Swedish", "sv"},
                    {"Swedish (Finland)", "sv-FI"},
                    {"Swedish (Sweden)", "sv-SE"},
                    {"Syriac", "syr"},
                    {"Syriac (Syria)", "syr-SY"},
                    {"Tajik (Cyrillic, Tajikistan)", "tg-Cyrl-TJ"},
                    {"Tamazight (Latin, Algeria)", "tzm-Latn-DZ"},
                    {"Tamil", "ta"},
                    {"Tamil (India)", "ta-IN"},
                    {"Tatar", "tt"},
                    {"Tatar (Russia)", "tt-RU"},
                    {"Telugu", "te"},
                    {"Telugu (India)", "te-IN"},
                    {"Thai", "th"},
                    {"Thai (Thailand)", "th-TH"},
                    {"Tibetan (PRC)", "bo-CN"},
                    {"Turkish", "tr"},
                    {"Turkish (Turkey)", "tr-TR"},
                    {"Turkmen (Turkmenistan)", "tk-TM"},
                    {"Uighur (PRC)", "ug-CN"},
                    {"Ukrainian", "uk"},
                    {"Ukrainian (Ukraine)", "uk-UA"},
                    {"Upper Sorbian (Germany)", "hsb-DE"},
                    {"Urdu", "ur"},
                    {"Urdu (Islamic Republic of Pakistan)", "ur-PK"},
                    {"Uzbek", "uz"},
                    {"Uzbek (Cyrillic, Uzbekistan)", "uz-Cyrl-UZ"},
                    {"Uzbek (Latin, Uzbekistan)", "uz-Latn-UZ"},
                    {"Vietnamese", "vi"},
                    {"Vietnamese (Vietnam)", "vi-VN"},
                    {"Welsh (United Kingdom)", "cy-GB"},
                    {"Wolof (Senegal)", "wo-SN"},
                    {"Yakut (Russia)", "sah-RU"},
                    {"Yi (PRC)", "ii-CN"},
                    {"Yoruba (Nigeria)", "yo-NG"}
                };

                Debug.Assert(result.Count == Count);
                return result;
            }

            public static string GetCultureInfoName(string cultureInfoDisplayName)
            {
                return s_cultureInfoNameMap.TryGetValue(cultureInfoDisplayName, out string name) ?
                    name :
                    cultureInfoDisplayName;
            }
        }
    }
}
