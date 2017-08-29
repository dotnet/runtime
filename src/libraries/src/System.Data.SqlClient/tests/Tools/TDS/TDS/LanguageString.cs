// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// Class that contains language names and can convert between enumeration and strings
    /// </summary>
    public static class LanguageString
    {
        private const string English = "us_english";
        private const string German = "Deutsch";
        private const string French = "Fran\u00E7ais";
        private const string Japanese = "\u65E5\u672C\u8A9E";
        private const string Danish = "Dansk";
        private const string Spanish = "Espa\u00F1o";
        private const string Italian = "Italiano";
        private const string Dutch = "Nederlands";
        private const string Norwegian = "Norsk";
        private const string Portuguese = "Portugu\u00EAs";
        private const string Finnish = "Suomi";
        private const string Swedish = "Svenska";
        private const string Czech = "\u010De\u0161tina";
        private const string Hungarian = "magyar";
        private const string Polish = "polski";
        private const string Romanian = "rom\u00E2n\u0103";
        private const string Croatian = "hrvatski";
        private const string Slovak = "sloven\u010Dina";
        private const string Slovenian = "slovenski";
        private const string Greek = "\u03B5\u03BB\u03BB\u03B7\u03BD\u03B9\u03BA\u03AC";
        private const string Bulgarian = "\u0431\u044A\u043B\u0433\u0430\u0440\u0441\u043A\u0438";
        private const string Russian = "\u0440\u0443\u0441\u0441\u043A\u0438\u0439";
        private const string Turkish = "T\u00FCrk\u00E7e";
        private const string BritishEnglish = "British";
        private const string Estonian = "eesti";
        private const string Latvian = "latvie\u0161u";
        private const string Lithuanian = "lietuvi\u0173";
        private const string Brazilian = "Portugu\u00EAs (Brasil)";
        private const string TraditionalChinese = "\u7E41\u9AD4\u4E2D\u6587";
        private const string Korean = "\uD55C\uAD6D\uC5B4";
        private const string SimplifiedChinese = "\u7B80\u4F53\u4E2D\u6587";
        private const string Arabic = "Arabic";
        private const string Thai = "\u0E44\u0E17\u0E22";
        private const string Bokmal = "norsk (bokm\u00E5l)";

        /// <summary>
        /// Convert a language to enumeration
        /// </summary>
        public static LanguageType ToEnum(string value)
        {
            // Check every langauge
            if (English.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.English;
            }
            else if (German.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.German;
            }
            else if (French.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.French;
            }
            else if (Japanese.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Japanese;
            }
            else if (Danish.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Danish;
            }
            else if (Spanish.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Spanish;
            }
            else if (Italian.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Italian;
            }
            else if (Dutch.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Dutch;
            }
            else if (Norwegian.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Norwegian;
            }
            else if (Portuguese.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Portuguese;
            }
            else if (Finnish.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Finnish;
            }
            else if (Swedish.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Swedish;
            }
            else if (Czech.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Czech;
            }
            else if (Hungarian.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Hungarian;
            }
            else if (Polish.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Polish;
            }
            else if (Romanian.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Romanian;
            }
            else if (Croatian.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Croatian;
            }
            else if (Slovak.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Slovak;
            }
            else if (Slovenian.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Slovenian;
            }
            else if (Greek.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Greek;
            }
            else if (Bulgarian.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Bulgarian;
            }
            else if (Russian.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Russian;
            }
            else if (Turkish.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Turkish;
            }
            else if (BritishEnglish.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.BritishEnglish;
            }
            else if (Estonian.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Estonian;
            }
            else if (Latvian.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Latvian;
            }
            else if (Lithuanian.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Lithuanian;
            }
            else if (Brazilian.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Brazilian;
            }
            else if (TraditionalChinese.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.TraditionalChinese;
            }
            else if (Korean.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Korean;
            }
            else if (SimplifiedChinese.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.SimplifiedChinese;
            }
            else if (Arabic.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Arabic;
            }
            else if (Thai.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Thai;
            }
            else if (Bokmal.Equals(value, StringComparison.CurrentCultureIgnoreCase))
            {
                return LanguageType.Bokmal;
            }

            // Unknown value
            throw new Exception("Unrecognized language string \"" + value + "\"");
        }

        /// <summary>
        /// Convert enumeration to string
        /// </summary>
        public static string ToString(LanguageType value)
        {
            // Switch through the langauges
            switch (value)
            {
                case LanguageType.English:
                    {
                        return English;
                    }
                case LanguageType.German:
                    {
                        return German;
                    }
                case LanguageType.French:
                    {
                        return French;
                    }
                case LanguageType.Japanese:
                    {
                        return Japanese;
                    }
                case LanguageType.Danish:
                    {
                        return Danish;
                    }
                case LanguageType.Spanish:
                    {
                        return Spanish;
                    }
                case LanguageType.Italian:
                    {
                        return Italian;
                    }
                case LanguageType.Dutch:
                    {
                        return Dutch;
                    }
                case LanguageType.Norwegian:
                    {
                        return Norwegian;
                    }
                case LanguageType.Portuguese:
                    {
                        return Portuguese;
                    }
                case LanguageType.Finnish:
                    {
                        return Finnish;
                    }
                case LanguageType.Swedish:
                    {
                        return Swedish;
                    }
                case LanguageType.Czech:
                    {
                        return Czech;
                    }
                case LanguageType.Hungarian:
                    {
                        return Hungarian;
                    }
                case LanguageType.Polish:
                    {
                        return Polish;
                    }
                case LanguageType.Romanian:
                    {
                        return Romanian;
                    }
                case LanguageType.Croatian:
                    {
                        return Croatian;
                    }
                case LanguageType.Slovak:
                    {
                        return Slovak;
                    }
                case LanguageType.Slovenian:
                    {
                        return Slovenian;
                    }
                case LanguageType.Greek:
                    {
                        return Greek;
                    }
                case LanguageType.Bulgarian:
                    {
                        return Bulgarian;
                    }
                case LanguageType.Russian:
                    {
                        return Russian;
                    }
                case LanguageType.Turkish:
                    {
                        return Turkish;
                    }
                case LanguageType.BritishEnglish:
                    {
                        return BritishEnglish;
                    }
                case LanguageType.Estonian:
                    {
                        return Estonian;
                    }
                case LanguageType.Latvian:
                    {
                        return Latvian;
                    }
                case LanguageType.Lithuanian:
                    {
                        return Lithuanian;
                    }
                case LanguageType.Brazilian:
                    {
                        return Brazilian;
                    }
                case LanguageType.TraditionalChinese:
                    {
                        return TraditionalChinese;
                    }
                case LanguageType.Korean:
                    {
                        return Korean;
                    }
                case LanguageType.SimplifiedChinese:
                    {
                        return SimplifiedChinese;
                    }
                case LanguageType.Arabic:
                    {
                        return Arabic;
                    }
                case LanguageType.Thai:
                    {
                        return Thai;
                    }
                case LanguageType.Bokmal:
                    {
                        return Bokmal;
                    }
            }

            // Unknown value
            throw new Exception("Unrecognized language type \"" + value.ToString() + "\"");
        }
    }
}
