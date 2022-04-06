// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.RegularExpressions
{
    // The main function of RegexCharClass is as a builder to turn ranges, characters and
    // Unicode categories into a single string.  This string is used as a black box
    // representation of a character class by the rest of Regex.  The format is as follows.
    //
    // Char index   Use
    //      0       Flags - currently this only holds the "negate" flag
    //      1       length of the string representing the "set" portion, e.g. [a-z0-9] only has a "set"
    //      2       length of the string representing the "category" portion, e.g. [\p{Lu}] only has a "category"
    //      3...m   The set.  These are a series of ranges which define the characters included in the set.
    //              To determine if a given character is in the set, we binary search over this set of ranges
    //              and see where the character should go.  Based on whether the ending index is odd or even,
    //              we know if the character is in the set.
    //      m+1...n The categories.  This is a list of UnicodeCategory enum values which describe categories
    //              included in this class.

    /// <summary>Provides the "set of Unicode chars" functionality used by the regexp engine.</summary>
    internal sealed partial class RegexCharClass
    {
        // Constants
        internal const int FlagsIndex = 0;
        internal const int SetLengthIndex = 1;
        internal const int CategoryLengthIndex = 2;
        internal const int SetStartIndex = 3; // must be odd for subsequent logic to work

        private const string NullCharString = "\0";
        private const char NullChar = '\0';
        internal const char LastChar = '\uFFFF';

        internal const short SpaceConst = 100;
        private const short NotSpaceConst = -100;

        private const string InternalRegexIgnoreCase = "__InternalRegexIgnoreCase__";
        private const string Space = "\x64";
        private const string NotSpace = "\uFF9C";
        private const string Word = "\u0000\u0002\u0004\u0005\u0003\u0001\u0006\u0009\u0013\u0000";
        private const string NotWord = "\u0000\uFFFE\uFFFC\uFFFB\uFFFD\uFFFF\uFFFA\uFFF7\uFFED\u0000";

        internal const string SpaceClass = "\u0000\u0000\u0001\u0064";
        internal const string NotSpaceClass = "\u0001\u0000\u0001\u0064";
        internal const string WordClass = "\u0000\u0000\u000A\u0000\u0002\u0004\u0005\u0003\u0001\u0006\u0009\u0013\u0000";
        internal const string NotWordClass = "\u0001\u0000\u000A\u0000\u0002\u0004\u0005\u0003\u0001\u0006\u0009\u0013\u0000";
        internal const string DigitClass = "\u0000\u0000\u0001\u0009";
        internal const string NotDigitClass = "\u0000\u0000\u0001\uFFF7";

        private const string ECMASpaceSet = "\u0009\u000E\u0020\u0021";
        private const string NotECMASpaceSet = "\0\u0009\u000E\u0020\u0021";
        private const string ECMAWordSet = "\u0030\u003A\u0041\u005B\u005F\u0060\u0061\u007B\u0130\u0131";
        private const string NotECMAWordSet = "\0\u0030\u003A\u0041\u005B\u005F\u0060\u0061\u007B\u0130\u0131";
        private const string ECMADigitSet = "\u0030\u003A";
        private const string NotECMADigitSet = "\0\u0030\u003A";

        internal const string ECMASpaceClass = "\x00\x04\x00" + ECMASpaceSet;
        internal const string NotECMASpaceClass = "\x01\x04\x00" + ECMASpaceSet;
        internal const string ECMAWordClass = "\x00\x0A\x00" + ECMAWordSet;
        internal const string NotECMAWordClass = "\x01\x0A\x00" + ECMAWordSet;
        internal const string ECMADigitClass = "\x00\x02\x00" + ECMADigitSet;
        internal const string NotECMADigitClass = "\x01\x02\x00" + ECMADigitSet;

        internal const string AnyClass = "\x00\x01\x00\x00";
        private const string EmptyClass = "\x00\x00\x00";

        // UnicodeCategory is zero based, so we add one to each value and subtract it off later
        private const int DefinedCategoriesCapacity = 38;
        private static readonly Dictionary<string, string> s_definedCategories = new Dictionary<string, string>(DefinedCategoriesCapacity)
        {
            // Others
            { "Cc", "\u000F" }, // UnicodeCategory.Control + 1
            { "Cf", "\u0010" }, // UnicodeCategory.Format + 1
            { "Cn", "\u001E" }, // UnicodeCategory.OtherNotAssigned + 1
            { "Co", "\u0012" }, // UnicodeCategory.PrivateUse + 1
            { "Cs", "\u0011" }, // UnicodeCategory.Surrogate + 1
            { "C", "\u0000\u000F\u0010\u001E\u0012\u0011\u0000" },

            // Letters
            { "Ll", "\u0002" }, // UnicodeCategory.LowercaseLetter + 1
            { "Lm", "\u0004" }, // UnicodeCategory.ModifierLetter + 1
            { "Lo", "\u0005" }, // UnicodeCategory.OtherLetter + 1
            { "Lt", "\u0003" }, // UnicodeCategory.TitlecaseLetter + 1
            { "Lu", "\u0001" }, // UnicodeCategory.UppercaseLetter + 1
            { "L", "\u0000\u0002\u0004\u0005\u0003\u0001\u0000" },

            // InternalRegexIgnoreCase = {LowercaseLetter} OR {TitlecaseLetter} OR {UppercaseLetter}
            // !!!This category should only ever be used in conjunction with RegexOptions.IgnoreCase code paths!!!
            { "__InternalRegexIgnoreCase__", "\u0000\u0002\u0003\u0001\u0000" },

            // Marks
            { "Mc", "\u0007" }, // UnicodeCategory.SpacingCombiningMark + 1
            { "Me", "\u0008" }, // UnicodeCategory.EnclosingMark + 1
            { "Mn", "\u0006" }, // UnicodeCategory.NonSpacingMark + 1
            { "M", "\u0000\u0007\u0008\u0006\u0000" },

            // Numbers
            { "Nd", "\u0009" }, // UnicodeCategory.DecimalDigitNumber + 1
            { "Nl", "\u000A" }, // UnicodeCategory.LetterNumber + 1
            { "No", "\u000B" }, // UnicodeCategory.OtherNumber + 1
            { "N", "\u0000\u0009\u000A\u000B\u0000" },

            // Punctuation
            { "Pc", "\u0013" }, // UnicodeCategory.ConnectorPunctuation + 1
            { "Pd", "\u0014" }, // UnicodeCategory.DashPunctuation + 1
            { "Pe", "\u0016" }, // UnicodeCategory.ClosePunctuation + 1
            { "Po", "\u0019" }, // UnicodeCategory.OtherPunctuation + 1
            { "Ps", "\u0015" }, // UnicodeCategory.OpenPunctuation + 1
            { "Pf", "\u0018" }, // UnicodeCategory.FinalQuotePunctuation + 1
            { "Pi", "\u0017" }, // UnicodeCategory.InitialQuotePunctuation + 1
            { "P", "\u0000\u0013\u0014\u0016\u0019\u0015\u0018\u0017\u0000" },

            // Symbols
            { "Sc", "\u001B" }, // UnicodeCategory.CurrencySymbol + 1
            { "Sk", "\u001C" }, // UnicodeCategory.ModifierSymbol + 1
            { "Sm", "\u001A" }, // UnicodeCategory.MathSymbol + 1
            { "So", "\u001D" }, // UnicodeCategory.OtherSymbol + 1
            { "S", "\u0000\u001B\u001C\u001A\u001D\u0000" },

            // Separators
            { "Zl", "\u000D" }, // UnicodeCategory.LineSeparator + 1
            { "Zp", "\u000E" }, // UnicodeCategory.ParagraphSeparator + 1
            { "Zs", "\u000C" }, // UnicodeCategory.SpaceSeparator + 1
            { "Z", "\u0000\u000D\u000E\u000C\u0000" },
        };

        /*
         *   The property table contains all the block definitions defined in the
         *   XML schema spec (http://www.w3.org/TR/2001/PR-xmlschema-2-20010316/#charcter-classes), Unicode 4.0 spec (www.unicode.org),
         *   and Perl 5.6 (see Programming Perl, 3rd edition page 167).   Three blocks defined by Perl (and here) may
         *   not be in the Unicode: IsHighPrivateUseSurrogates, IsHighSurrogates, and IsLowSurrogates.
         *
        **/
        // Has to be sorted by the first column
        private static readonly string[][] s_propTable = {
            new[] {"IsAlphabeticPresentationForms",         "\uFB00\uFB50"},
            new[] {"IsArabic",                              "\u0600\u0700"},
            new[] {"IsArabicPresentationForms-A",           "\uFB50\uFE00"},
            new[] {"IsArabicPresentationForms-B",           "\uFE70\uFF00"},
            new[] {"IsArmenian",                            "\u0530\u0590"},
            new[] {"IsArrows",                              "\u2190\u2200"},
            new[] {"IsBasicLatin",                          "\u0000\u0080"},
            new[] {"IsBengali",                             "\u0980\u0A00"},
            new[] {"IsBlockElements",                       "\u2580\u25A0"},
            new[] {"IsBopomofo",                            "\u3100\u3130"},
            new[] {"IsBopomofoExtended",                    "\u31A0\u31C0"},
            new[] {"IsBoxDrawing",                          "\u2500\u2580"},
            new[] {"IsBraillePatterns",                     "\u2800\u2900"},
            new[] {"IsBuhid",                               "\u1740\u1760"},
            new[] {"IsCJKCompatibility",                    "\u3300\u3400"},
            new[] {"IsCJKCompatibilityForms",               "\uFE30\uFE50"},
            new[] {"IsCJKCompatibilityIdeographs",          "\uF900\uFB00"},
            new[] {"IsCJKRadicalsSupplement",               "\u2E80\u2F00"},
            new[] {"IsCJKSymbolsandPunctuation",            "\u3000\u3040"},
            new[] {"IsCJKUnifiedIdeographs",                "\u4E00\uA000"},
            new[] {"IsCJKUnifiedIdeographsExtensionA",      "\u3400\u4DC0"},
            new[] {"IsCherokee",                            "\u13A0\u1400"},
            new[] {"IsCombiningDiacriticalMarks",           "\u0300\u0370"},
            new[] {"IsCombiningDiacriticalMarksforSymbols", "\u20D0\u2100"},
            new[] {"IsCombiningHalfMarks",                  "\uFE20\uFE30"},
            new[] {"IsCombiningMarksforSymbols",            "\u20D0\u2100"},
            new[] {"IsControlPictures",                     "\u2400\u2440"},
            new[] {"IsCurrencySymbols",                     "\u20A0\u20D0"},
            new[] {"IsCyrillic",                            "\u0400\u0500"},
            new[] {"IsCyrillicSupplement",                  "\u0500\u0530"},
            new[] {"IsDevanagari",                          "\u0900\u0980"},
            new[] {"IsDingbats",                            "\u2700\u27C0"},
            new[] {"IsEnclosedAlphanumerics",               "\u2460\u2500"},
            new[] {"IsEnclosedCJKLettersandMonths",         "\u3200\u3300"},
            new[] {"IsEthiopic",                            "\u1200\u1380"},
            new[] {"IsGeneralPunctuation",                  "\u2000\u2070"},
            new[] {"IsGeometricShapes",                     "\u25A0\u2600"},
            new[] {"IsGeorgian",                            "\u10A0\u1100"},
            new[] {"IsGreek",                               "\u0370\u0400"},
            new[] {"IsGreekExtended",                       "\u1F00\u2000"},
            new[] {"IsGreekandCoptic",                      "\u0370\u0400"},
            new[] {"IsGujarati",                            "\u0A80\u0B00"},
            new[] {"IsGurmukhi",                            "\u0A00\u0A80"},
            new[] {"IsHalfwidthandFullwidthForms",          "\uFF00\uFFF0"},
            new[] {"IsHangulCompatibilityJamo",             "\u3130\u3190"},
            new[] {"IsHangulJamo",                          "\u1100\u1200"},
            new[] {"IsHangulSyllables",                     "\uAC00\uD7B0"},
            new[] {"IsHanunoo",                             "\u1720\u1740"},
            new[] {"IsHebrew",                              "\u0590\u0600"},
            new[] {"IsHighPrivateUseSurrogates",            "\uDB80\uDC00"},
            new[] {"IsHighSurrogates",                      "\uD800\uDB80"},
            new[] {"IsHiragana",                            "\u3040\u30A0"},
            new[] {"IsIPAExtensions",                       "\u0250\u02B0"},
            new[] {"IsIdeographicDescriptionCharacters",    "\u2FF0\u3000"},
            new[] {"IsKanbun",                              "\u3190\u31A0"},
            new[] {"IsKangxiRadicals",                      "\u2F00\u2FE0"},
            new[] {"IsKannada",                             "\u0C80\u0D00"},
            new[] {"IsKatakana",                            "\u30A0\u3100"},
            new[] {"IsKatakanaPhoneticExtensions",          "\u31F0\u3200"},
            new[] {"IsKhmer",                               "\u1780\u1800"},
            new[] {"IsKhmerSymbols",                        "\u19E0\u1A00"},
            new[] {"IsLao",                                 "\u0E80\u0F00"},
            new[] {"IsLatin-1Supplement",                   "\u0080\u0100"},
            new[] {"IsLatinExtended-A",                     "\u0100\u0180"},
            new[] {"IsLatinExtended-B",                     "\u0180\u0250"},
            new[] {"IsLatinExtendedAdditional",             "\u1E00\u1F00"},
            new[] {"IsLetterlikeSymbols",                   "\u2100\u2150"},
            new[] {"IsLimbu",                               "\u1900\u1950"},
            new[] {"IsLowSurrogates",                       "\uDC00\uE000"},
            new[] {"IsMalayalam",                           "\u0D00\u0D80"},
            new[] {"IsMathematicalOperators",               "\u2200\u2300"},
            new[] {"IsMiscellaneousMathematicalSymbols-A",  "\u27C0\u27F0"},
            new[] {"IsMiscellaneousMathematicalSymbols-B",  "\u2980\u2A00"},
            new[] {"IsMiscellaneousSymbols",                "\u2600\u2700"},
            new[] {"IsMiscellaneousSymbolsandArrows",       "\u2B00\u2C00"},
            new[] {"IsMiscellaneousTechnical",              "\u2300\u2400"},
            new[] {"IsMongolian",                           "\u1800\u18B0"},
            new[] {"IsMyanmar",                             "\u1000\u10A0"},
            new[] {"IsNumberForms",                         "\u2150\u2190"},
            new[] {"IsOgham",                               "\u1680\u16A0"},
            new[] {"IsOpticalCharacterRecognition",         "\u2440\u2460"},
            new[] {"IsOriya",                               "\u0B00\u0B80"},
            new[] {"IsPhoneticExtensions",                  "\u1D00\u1D80"},
            new[] {"IsPrivateUse",                          "\uE000\uF900"},
            new[] {"IsPrivateUseArea",                      "\uE000\uF900"},
            new[] {"IsRunic",                               "\u16A0\u1700"},
            new[] {"IsSinhala",                             "\u0D80\u0E00"},
            new[] {"IsSmallFormVariants",                   "\uFE50\uFE70"},
            new[] {"IsSpacingModifierLetters",              "\u02B0\u0300"},
            new[] {"IsSpecials",                            "\uFFF0"},
            new[] {"IsSuperscriptsandSubscripts",           "\u2070\u20A0"},
            new[] {"IsSupplementalArrows-A",                "\u27F0\u2800"},
            new[] {"IsSupplementalArrows-B",                "\u2900\u2980"},
            new[] {"IsSupplementalMathematicalOperators",   "\u2A00\u2B00"},
            new[] {"IsSyriac",                              "\u0700\u0750"},
            new[] {"IsTagalog",                             "\u1700\u1720"},
            new[] {"IsTagbanwa",                            "\u1760\u1780"},
            new[] {"IsTaiLe",                               "\u1950\u1980"},
            new[] {"IsTamil",                               "\u0B80\u0C00"},
            new[] {"IsTelugu",                              "\u0C00\u0C80"},
            new[] {"IsThaana",                              "\u0780\u07C0"},
            new[] {"IsThai",                                "\u0E00\u0E80"},
            new[] {"IsTibetan",                             "\u0F00\u1000"},
            new[] {"IsUnifiedCanadianAboriginalSyllabics",  "\u1400\u1680"},
            new[] {"IsVariationSelectors",                  "\uFE00\uFE10"},
            new[] {"IsYiRadicals",                          "\uA490\uA4D0"},
            new[] {"IsYiSyllables",                         "\uA000\uA490"},
            new[] {"IsYijingHexagramSymbols",               "\u4DC0\u4E00"},
            new[] {"_xmlC", /* Name Char              */    "\u002D\u002F\u0030\u003B\u0041\u005B\u005F\u0060\u0061\u007B\u00B7\u00B8\u00C0\u00D7\u00D8\u00F7\u00F8\u0132\u0134\u013F\u0141\u0149\u014A\u017F\u0180\u01C4\u01CD\u01F1\u01F4\u01F6\u01FA\u0218\u0250\u02A9\u02BB\u02C2\u02D0\u02D2\u0300\u0346\u0360\u0362\u0386\u038B\u038C\u038D\u038E\u03A2\u03A3\u03CF\u03D0\u03D7\u03DA\u03DB\u03DC\u03DD\u03DE\u03DF\u03E0\u03E1\u03E2\u03F4\u0401\u040D\u040E\u0450\u0451\u045D\u045E\u0482\u0483\u0487\u0490\u04C5\u04C7\u04C9\u04CB\u04CD\u04D0\u04EC\u04EE\u04F6\u04F8\u04FA\u0531\u0557\u0559\u055A\u0561\u0587\u0591\u05A2\u05A3\u05BA\u05BB\u05BE\u05BF\u05C0\u05C1\u05C3\u05C4\u05C5\u05D0\u05EB\u05F0\u05F3\u0621\u063B\u0640\u0653\u0660\u066A\u0670\u06B8\u06BA\u06BF\u06C0\u06CF\u06D0\u06D4\u06D5\u06E9\u06EA\u06EE\u06F0\u06FA\u0901\u0904\u0905\u093A\u093C\u094E\u0951\u0955\u0958\u0964\u0966\u0970\u0981\u0984\u0985\u098D\u098F\u0991\u0993\u09A9\u09AA\u09B1\u09B2\u09B3\u09B6\u09BA\u09BC\u09BD\u09BE\u09C5\u09C7\u09C9\u09CB\u09CE\u09D7\u09D8\u09DC"
                +"\u09DE\u09DF\u09E4\u09E6\u09F2\u0A02\u0A03\u0A05\u0A0B\u0A0F\u0A11\u0A13\u0A29\u0A2A\u0A31\u0A32\u0A34\u0A35\u0A37\u0A38\u0A3A\u0A3C\u0A3D\u0A3E\u0A43\u0A47\u0A49\u0A4B\u0A4E\u0A59\u0A5D\u0A5E\u0A5F\u0A66\u0A75\u0A81\u0A84\u0A85\u0A8C\u0A8D\u0A8E\u0A8F\u0A92\u0A93\u0AA9\u0AAA\u0AB1\u0AB2\u0AB4\u0AB5\u0ABA\u0ABC\u0AC6\u0AC7\u0ACA\u0ACB\u0ACE\u0AE0\u0AE1\u0AE6\u0AF0\u0B01\u0B04\u0B05\u0B0D\u0B0F\u0B11\u0B13\u0B29\u0B2A\u0B31\u0B32\u0B34\u0B36\u0B3A\u0B3C\u0B44\u0B47\u0B49\u0B4B\u0B4E\u0B56\u0B58\u0B5C\u0B5E\u0B5F\u0B62\u0B66\u0B70\u0B82\u0B84\u0B85\u0B8B\u0B8E\u0B91\u0B92\u0B96\u0B99\u0B9B\u0B9C\u0B9D\u0B9E\u0BA0\u0BA3\u0BA5\u0BA8\u0BAB\u0BAE\u0BB6\u0BB7\u0BBA\u0BBE\u0BC3\u0BC6\u0BC9\u0BCA\u0BCE\u0BD7\u0BD8\u0BE7\u0BF0\u0C01\u0C04\u0C05\u0C0D\u0C0E\u0C11\u0C12\u0C29\u0C2A\u0C34\u0C35\u0C3A\u0C3E\u0C45\u0C46\u0C49\u0C4A\u0C4E\u0C55\u0C57\u0C60\u0C62\u0C66\u0C70\u0C82\u0C84\u0C85\u0C8D\u0C8E\u0C91\u0C92\u0CA9\u0CAA\u0CB4\u0CB5\u0CBA\u0CBE\u0CC5\u0CC6\u0CC9\u0CCA\u0CCE\u0CD5\u0CD7\u0CDE\u0CDF\u0CE0\u0CE2"
                +"\u0CE6\u0CF0\u0D02\u0D04\u0D05\u0D0D\u0D0E\u0D11\u0D12\u0D29\u0D2A\u0D3A\u0D3E\u0D44\u0D46\u0D49\u0D4A\u0D4E\u0D57\u0D58\u0D60\u0D62\u0D66\u0D70\u0E01\u0E2F\u0E30\u0E3B\u0E40\u0E4F\u0E50\u0E5A\u0E81\u0E83\u0E84\u0E85\u0E87\u0E89\u0E8A\u0E8B\u0E8D\u0E8E\u0E94\u0E98\u0E99\u0EA0\u0EA1\u0EA4\u0EA5\u0EA6\u0EA7\u0EA8\u0EAA\u0EAC\u0EAD\u0EAF\u0EB0\u0EBA\u0EBB\u0EBE\u0EC0\u0EC5\u0EC6\u0EC7\u0EC8\u0ECE\u0ED0\u0EDA\u0F18\u0F1A\u0F20\u0F2A\u0F35\u0F36\u0F37\u0F38\u0F39\u0F3A\u0F3E\u0F48\u0F49\u0F6A\u0F71\u0F85\u0F86\u0F8C\u0F90\u0F96\u0F97\u0F98\u0F99\u0FAE\u0FB1\u0FB8\u0FB9\u0FBA\u10A0\u10C6\u10D0\u10F7\u1100\u1101\u1102\u1104\u1105\u1108\u1109\u110A\u110B\u110D\u110E\u1113\u113C\u113D\u113E\u113F\u1140\u1141\u114C\u114D\u114E\u114F\u1150\u1151\u1154\u1156\u1159\u115A\u115F\u1162\u1163\u1164\u1165\u1166\u1167\u1168\u1169\u116A\u116D\u116F\u1172\u1174\u1175\u1176\u119E\u119F\u11A8\u11A9\u11AB\u11AC\u11AE\u11B0\u11B7\u11B9\u11BA\u11BB\u11BC\u11C3\u11EB\u11EC\u11F0\u11F1\u11F9\u11FA\u1E00\u1E9C\u1EA0\u1EFA\u1F00"
                +"\u1F16\u1F18\u1F1E\u1F20\u1F46\u1F48\u1F4E\u1F50\u1F58\u1F59\u1F5A\u1F5B\u1F5C\u1F5D\u1F5E\u1F5F\u1F7E\u1F80\u1FB5\u1FB6\u1FBD\u1FBE\u1FBF\u1FC2\u1FC5\u1FC6\u1FCD\u1FD0\u1FD4\u1FD6\u1FDC\u1FE0\u1FED\u1FF2\u1FF5\u1FF6\u1FFD\u20D0\u20DD\u20E1\u20E2\u2126\u2127\u212A\u212C\u212E\u212F\u2180\u2183\u3005\u3006\u3007\u3008\u3021\u3030\u3031\u3036\u3041\u3095\u3099\u309B\u309D\u309F\u30A1\u30FB\u30FC\u30FF\u3105\u312D\u4E00\u9FA6\uAC00\uD7A4"},
            new[] {"_xmlD",                                 "\u0030\u003A\u0660\u066A\u06F0\u06FA\u0966\u0970\u09E6\u09F0\u0A66\u0A70\u0AE6\u0AF0\u0B66\u0B70\u0BE7\u0BF0\u0C66\u0C70\u0CE6\u0CF0\u0D66\u0D70\u0E50\u0E5A\u0ED0\u0EDA\u0F20\u0F2A\u1040\u104A\u1369\u1372\u17E0\u17EA\u1810\u181A\uFF10\uFF1A"},
            new[] {"_xmlI", /* Start Name Char       */     "\u003A\u003B\u0041\u005B\u005F\u0060\u0061\u007B\u00C0\u00D7\u00D8\u00F7\u00F8\u0132\u0134\u013F\u0141\u0149\u014A\u017F\u0180\u01C4\u01CD\u01F1\u01F4\u01F6\u01FA\u0218\u0250\u02A9\u02BB\u02C2\u0386\u0387\u0388\u038B\u038C\u038D\u038E\u03A2\u03A3\u03CF\u03D0\u03D7\u03DA\u03DB\u03DC\u03DD\u03DE\u03DF\u03E0\u03E1\u03E2\u03F4\u0401\u040D\u040E\u0450\u0451\u045D\u045E\u0482\u0490\u04C5\u04C7\u04C9\u04CB\u04CD\u04D0\u04EC\u04EE\u04F6\u04F8\u04FA\u0531\u0557\u0559\u055A\u0561\u0587\u05D0\u05EB\u05F0\u05F3\u0621\u063B\u0641\u064B\u0671\u06B8\u06BA\u06BF\u06C0\u06CF\u06D0\u06D4\u06D5\u06D6\u06E5\u06E7\u0905\u093A\u093D\u093E\u0958\u0962\u0985\u098D\u098F\u0991\u0993\u09A9\u09AA\u09B1\u09B2\u09B3\u09B6\u09BA\u09DC\u09DE\u09DF\u09E2\u09F0\u09F2\u0A05\u0A0B\u0A0F\u0A11\u0A13\u0A29\u0A2A\u0A31\u0A32\u0A34\u0A35\u0A37\u0A38\u0A3A\u0A59\u0A5D\u0A5E\u0A5F\u0A72\u0A75\u0A85\u0A8C\u0A8D\u0A8E\u0A8F\u0A92\u0A93\u0AA9\u0AAA\u0AB1\u0AB2\u0AB4\u0AB5\u0ABA\u0ABD\u0ABE\u0AE0\u0AE1\u0B05\u0B0D\u0B0F"
                +"\u0B11\u0B13\u0B29\u0B2A\u0B31\u0B32\u0B34\u0B36\u0B3A\u0B3D\u0B3E\u0B5C\u0B5E\u0B5F\u0B62\u0B85\u0B8B\u0B8E\u0B91\u0B92\u0B96\u0B99\u0B9B\u0B9C\u0B9D\u0B9E\u0BA0\u0BA3\u0BA5\u0BA8\u0BAB\u0BAE\u0BB6\u0BB7\u0BBA\u0C05\u0C0D\u0C0E\u0C11\u0C12\u0C29\u0C2A\u0C34\u0C35\u0C3A\u0C60\u0C62\u0C85\u0C8D\u0C8E\u0C91\u0C92\u0CA9\u0CAA\u0CB4\u0CB5\u0CBA\u0CDE\u0CDF\u0CE0\u0CE2\u0D05\u0D0D\u0D0E\u0D11\u0D12\u0D29\u0D2A\u0D3A\u0D60\u0D62\u0E01\u0E2F\u0E30\u0E31\u0E32\u0E34\u0E40\u0E46\u0E81\u0E83\u0E84\u0E85\u0E87\u0E89\u0E8A\u0E8B\u0E8D\u0E8E\u0E94\u0E98\u0E99\u0EA0\u0EA1\u0EA4\u0EA5\u0EA6\u0EA7\u0EA8\u0EAA\u0EAC\u0EAD\u0EAF\u0EB0\u0EB1\u0EB2\u0EB4\u0EBD\u0EBE\u0EC0\u0EC5\u0F40\u0F48\u0F49\u0F6A\u10A0\u10C6\u10D0\u10F7\u1100\u1101\u1102\u1104\u1105\u1108\u1109\u110A\u110B\u110D\u110E\u1113\u113C\u113D\u113E\u113F\u1140\u1141\u114C\u114D\u114E\u114F\u1150\u1151\u1154\u1156\u1159\u115A\u115F\u1162\u1163\u1164\u1165\u1166\u1167\u1168\u1169\u116A\u116D\u116F\u1172\u1174\u1175\u1176\u119E\u119F\u11A8\u11A9\u11AB\u11AC"
                +"\u11AE\u11B0\u11B7\u11B9\u11BA\u11BB\u11BC\u11C3\u11EB\u11EC\u11F0\u11F1\u11F9\u11FA\u1E00\u1E9C\u1EA0\u1EFA\u1F00\u1F16\u1F18\u1F1E\u1F20\u1F46\u1F48\u1F4E\u1F50\u1F58\u1F59\u1F5A\u1F5B\u1F5C\u1F5D\u1F5E\u1F5F\u1F7E\u1F80\u1FB5\u1FB6\u1FBD\u1FBE\u1FBF\u1FC2\u1FC5\u1FC6\u1FCD\u1FD0\u1FD4\u1FD6\u1FDC\u1FE0\u1FED\u1FF2\u1FF5\u1FF6\u1FFD\u2126\u2127\u212A\u212C\u212E\u212F\u2180\u2183\u3007\u3008\u3021\u302A\u3041\u3095\u30A1\u30FB\u3105\u312D\u4E00\u9FA6\uAC00\uD7A4"},
            new[] {"_xmlW",                                 "\u0024\u0025\u002B\u002C\u0030\u003A\u003C\u003F\u0041\u005B\u005E\u005F\u0060\u007B\u007C\u007D\u007E\u007F\u00A2\u00AB\u00AC\u00AD\u00AE\u00B7\u00B8\u00BB\u00BC\u00BF\u00C0\u0221\u0222\u0234\u0250\u02AE\u02B0\u02EF\u0300\u0350\u0360\u0370\u0374\u0376\u037A\u037B\u0384\u0387\u0388\u038B\u038C\u038D\u038E\u03A2\u03A3\u03CF\u03D0\u03F7\u0400\u0487\u0488\u04CF\u04D0\u04F6\u04F8\u04FA\u0500\u0510\u0531\u0557\u0559\u055A\u0561\u0588\u0591\u05A2\u05A3\u05BA\u05BB\u05BE\u05BF\u05C0\u05C1\u05C3\u05C4\u05C5\u05D0\u05EB\u05F0\u05F3\u0621\u063B\u0640\u0656\u0660\u066A\u066E\u06D4\u06D5\u06DD\u06DE\u06EE\u06F0\u06FF\u0710\u072D\u0730\u074B\u0780\u07B2\u0901\u0904\u0905\u093A\u093C\u094E\u0950\u0955\u0958\u0964\u0966\u0970\u0981\u0984\u0985\u098D\u098F\u0991\u0993\u09A9\u09AA\u09B1\u09B2\u09B3\u09B6\u09BA\u09BC\u09BD\u09BE\u09C5\u09C7\u09C9\u09CB\u09CE\u09D7\u09D8\u09DC\u09DE\u09DF\u09E4\u09E6\u09FB\u0A02\u0A03\u0A05\u0A0B\u0A0F\u0A11\u0A13\u0A29\u0A2A\u0A31\u0A32\u0A34\u0A35"
                +"\u0A37\u0A38\u0A3A\u0A3C\u0A3D\u0A3E\u0A43\u0A47\u0A49\u0A4B\u0A4E\u0A59\u0A5D\u0A5E\u0A5F\u0A66\u0A75\u0A81\u0A84\u0A85\u0A8C\u0A8D\u0A8E\u0A8F\u0A92\u0A93\u0AA9\u0AAA\u0AB1\u0AB2\u0AB4\u0AB5\u0ABA\u0ABC\u0AC6\u0AC7\u0ACA\u0ACB\u0ACE\u0AD0\u0AD1\u0AE0\u0AE1\u0AE6\u0AF0\u0B01\u0B04\u0B05\u0B0D\u0B0F\u0B11\u0B13\u0B29\u0B2A\u0B31\u0B32\u0B34\u0B36\u0B3A\u0B3C\u0B44\u0B47\u0B49\u0B4B\u0B4E\u0B56\u0B58\u0B5C\u0B5E\u0B5F\u0B62\u0B66\u0B71\u0B82\u0B84\u0B85\u0B8B\u0B8E\u0B91\u0B92\u0B96\u0B99\u0B9B\u0B9C\u0B9D\u0B9E\u0BA0\u0BA3\u0BA5\u0BA8\u0BAB\u0BAE\u0BB6\u0BB7\u0BBA\u0BBE\u0BC3\u0BC6\u0BC9\u0BCA\u0BCE\u0BD7\u0BD8\u0BE7\u0BF3\u0C01\u0C04\u0C05\u0C0D\u0C0E\u0C11\u0C12\u0C29\u0C2A\u0C34\u0C35\u0C3A\u0C3E\u0C45\u0C46\u0C49\u0C4A\u0C4E\u0C55\u0C57\u0C60\u0C62\u0C66\u0C70\u0C82\u0C84\u0C85\u0C8D\u0C8E\u0C91\u0C92\u0CA9\u0CAA\u0CB4\u0CB5\u0CBA\u0CBE\u0CC5\u0CC6\u0CC9\u0CCA\u0CCE\u0CD5\u0CD7\u0CDE\u0CDF\u0CE0\u0CE2\u0CE6\u0CF0\u0D02\u0D04\u0D05\u0D0D\u0D0E\u0D11\u0D12\u0D29\u0D2A\u0D3A\u0D3E\u0D44\u0D46\u0D49"
                +"\u0D4A\u0D4E\u0D57\u0D58\u0D60\u0D62\u0D66\u0D70\u0D82\u0D84\u0D85\u0D97\u0D9A\u0DB2\u0DB3\u0DBC\u0DBD\u0DBE\u0DC0\u0DC7\u0DCA\u0DCB\u0DCF\u0DD5\u0DD6\u0DD7\u0DD8\u0DE0\u0DF2\u0DF4\u0E01\u0E3B\u0E3F\u0E4F\u0E50\u0E5A\u0E81\u0E83\u0E84\u0E85\u0E87\u0E89\u0E8A\u0E8B\u0E8D\u0E8E\u0E94\u0E98\u0E99\u0EA0\u0EA1\u0EA4\u0EA5\u0EA6\u0EA7\u0EA8\u0EAA\u0EAC\u0EAD\u0EBA\u0EBB\u0EBE\u0EC0\u0EC5\u0EC6\u0EC7\u0EC8\u0ECE\u0ED0\u0EDA\u0EDC\u0EDE\u0F00\u0F04\u0F13\u0F3A\u0F3E\u0F48\u0F49\u0F6B\u0F71\u0F85\u0F86\u0F8C\u0F90\u0F98\u0F99\u0FBD\u0FBE\u0FCD\u0FCF\u0FD0\u1000\u1022\u1023\u1028\u1029\u102B\u102C\u1033\u1036\u103A\u1040\u104A\u1050\u105A\u10A0\u10C6\u10D0\u10F9\u1100\u115A\u115F\u11A3\u11A8\u11FA\u1200\u1207\u1208\u1247\u1248\u1249\u124A\u124E\u1250\u1257\u1258\u1259\u125A\u125E\u1260\u1287\u1288\u1289\u128A\u128E\u1290\u12AF\u12B0\u12B1\u12B2\u12B6\u12B8\u12BF\u12C0\u12C1\u12C2\u12C6\u12C8\u12CF\u12D0\u12D7\u12D8\u12EF\u12F0\u130F\u1310\u1311\u1312\u1316\u1318\u131F\u1320\u1347\u1348\u135B\u1369\u137D\u13A0"
                +"\u13F5\u1401\u166D\u166F\u1677\u1681\u169B\u16A0\u16EB\u16EE\u16F1\u1700\u170D\u170E\u1715\u1720\u1735\u1740\u1754\u1760\u176D\u176E\u1771\u1772\u1774\u1780\u17D4\u17D7\u17D8\u17DB\u17DD\u17E0\u17EA\u180B\u180E\u1810\u181A\u1820\u1878\u1880\u18AA\u1E00\u1E9C\u1EA0\u1EFA\u1F00\u1F16\u1F18\u1F1E\u1F20\u1F46\u1F48\u1F4E\u1F50\u1F58\u1F59\u1F5A\u1F5B\u1F5C\u1F5D\u1F5E\u1F5F\u1F7E\u1F80\u1FB5\u1FB6\u1FC5\u1FC6\u1FD4\u1FD6\u1FDC\u1FDD\u1FF0\u1FF2\u1FF5\u1FF6\u1FFF\u2044\u2045\u2052\u2053\u2070\u2072\u2074\u207D\u207F\u208D\u20A0\u20B2\u20D0\u20EB\u2100\u213B\u213D\u214C\u2153\u2184\u2190\u2329\u232B\u23B4\u23B7\u23CF\u2400\u2427\u2440\u244B\u2460\u24FF\u2500\u2614\u2616\u2618\u2619\u267E\u2680\u268A\u2701\u2705\u2706\u270A\u270C\u2728\u2729\u274C\u274D\u274E\u274F\u2753\u2756\u2757\u2758\u275F\u2761\u2768\u2776\u2795\u2798\u27B0\u27B1\u27BF\u27D0\u27E6\u27F0\u2983\u2999\u29D8\u29DC\u29FC\u29FE\u2B00\u2E80\u2E9A\u2E9B\u2EF4\u2F00\u2FD6\u2FF0\u2FFC\u3004\u3008\u3012\u3014\u3020\u3030\u3031\u303D\u303E\u3040"
                +"\u3041\u3097\u3099\u30A0\u30A1\u30FB\u30FC\u3100\u3105\u312D\u3131\u318F\u3190\u31B8\u31F0\u321D\u3220\u3244\u3251\u327C\u327F\u32CC\u32D0\u32FF\u3300\u3377\u337B\u33DE\u33E0\u33FF\u3400\u4DB6\u4E00\u9FA6\uA000\uA48D\uA490\uA4C7\uAC00\uD7A4\uF900\uFA2E\uFA30\uFA6B\uFB00\uFB07\uFB13\uFB18\uFB1D\uFB37\uFB38\uFB3D\uFB3E\uFB3F\uFB40\uFB42\uFB43\uFB45\uFB46\uFBB2\uFBD3\uFD3E\uFD50\uFD90\uFD92\uFDC8\uFDF0\uFDFD\uFE00\uFE10\uFE20\uFE24\uFE62\uFE63\uFE64\uFE67\uFE69\uFE6A\uFE70\uFE75\uFE76\uFEFD\uFF04\uFF05\uFF0B\uFF0C\uFF10\uFF1A\uFF1C\uFF1F\uFF21\uFF3B\uFF3E\uFF3F\uFF40\uFF5B\uFF5C\uFF5D\uFF5E\uFF5F\uFF66\uFFBF\uFFC2\uFFC8\uFFCA\uFFD0\uFFD2\uFFD8\uFFDA\uFFDD\uFFE0\uFFE7\uFFE8\uFFEF\uFFFC\uFFFE"},
        };

        private List<(char First, char Last)>? _rangelist;
        private StringBuilder? _categories;
        private RegexCharClass? _subtractor;
        private bool _negate;

#if DEBUG
        static RegexCharClass()
        {
            // Make sure the initial capacity for s_definedCategories is correct
            Debug.Assert(
                s_definedCategories.Count == DefinedCategoriesCapacity,
                $"Expected (s_definedCategories.Count): {s_definedCategories.Count}, Actual (DefinedCategoriesCapacity): {DefinedCategoriesCapacity}");

            // Make sure the s_propTable is correctly ordered
            int len = s_propTable.Length;
            for (int i = 0; i < len - 1; i++)
                Debug.Assert(string.Compare(s_propTable[i][0], s_propTable[i + 1][0], StringComparison.Ordinal) < 0, $"RegexCharClass s_propTable is out of order at ({s_propTable[i][0]}, {s_propTable[i + 1][0]})");

        }
#endif

        /// <summary>
        /// Creates an empty character class.
        /// </summary>
        public RegexCharClass()
        {
        }

        private RegexCharClass(bool negate, List<(char First, char Last)>? ranges, StringBuilder? categories, RegexCharClass? subtraction)
        {
            _rangelist = ranges;
            _categories = categories;
            _negate = negate;
            _subtractor = subtraction;
        }

        public bool CanMerge => !_negate && _subtractor == null;

        public bool Negate
        {
            set { _negate = value; }
        }

        public void AddChar(char c) => AddRange(c, c);

        /// <summary>
        /// Adds a regex char class
        /// </summary>
        public void AddCharClass(RegexCharClass cc)
        {
            Debug.Assert(cc.CanMerge && CanMerge, "Both character classes added together must be able to merge");

            int ccRangeCount = cc._rangelist?.Count ?? 0;

            if (ccRangeCount != 0)
            {
                EnsureRangeList().AddRange(cc._rangelist!);
            }

            if (cc._categories != null)
            {
                EnsureCategories().Append(cc._categories);
            }
        }

        /// <summary>Adds a regex char class if the classes are mergeable.</summary>
        public bool TryAddCharClass(RegexCharClass cc)
        {
            if (cc.CanMerge && CanMerge)
            {
                AddCharClass(cc);
                return true;
            }

            return false;
        }

        private StringBuilder EnsureCategories() =>
            _categories ??= new StringBuilder();

        private List<(char First, char Last)> EnsureRangeList() =>
            _rangelist ??= new List<(char First, char Last)>(6);

        /// <summary>
        /// Adds a set (specified by its string representation) to the class.
        /// </summary>
        private void AddSet(ReadOnlySpan<char> set)
        {
            if (set.Length == 0)
            {
                return;
            }

            List<(char First, char Last)> rangeList = EnsureRangeList();

            int i;
            for (i = 0; i < set.Length - 1; i += 2)
            {
                rangeList.Add((set[i], (char)(set[i + 1] - 1)));
            }

            if (i < set.Length)
            {
                rangeList.Add((set[i], LastChar));
            }
        }

        public void AddSubtraction(RegexCharClass sub)
        {
            Debug.Assert(_subtractor == null, "Can't add two subtractions to a char class. ");
            _subtractor = sub;
        }

        /// <summary>
        /// Adds a single range of characters to the class.
        /// </summary>
        public void AddRange(char first, char last) =>
            EnsureRangeList().Add((first, last));

        public void AddCategoryFromName(string categoryName, bool invert, bool caseInsensitive, string pattern, int currentPos)
        {
            if (s_definedCategories.TryGetValue(categoryName, out string? category) &&
                !categoryName.Equals(InternalRegexIgnoreCase))
            {
                if (caseInsensitive && (categoryName.Equals("Ll") || categoryName.Equals("Lu") || categoryName.Equals("Lt")))
                {
                    // when RegexOptions.IgnoreCase is specified then {Ll}, {Lu}, and {Lt} cases should all match
                    category = s_definedCategories[InternalRegexIgnoreCase];
                }

                StringBuilder categories = EnsureCategories();
                if (invert)
                {
                    // Negate category
                    for (int i = 0; i < category.Length; i++)
                    {
                        short ch = (short)category[i];
                        categories.Append((char)-ch);
                    }
                }
                else
                {
                    categories.Append(category);
                }
            }
            else
            {
                AddSet(SetFromProperty(categoryName, invert, pattern, currentPos));
            }
        }

        private void AddCategory(string category) => EnsureCategories().Append(category);

        /// <summary>
        /// Adds to the class any case-equivalence versions of characters already
        /// in the class. Used for case-insensitivity.
        /// </summary>
        public void AddCaseEquivalences(CultureInfo culture)
        {
            List<(char First, char Last)>? rangeList = _rangelist;
            if (rangeList != null)
            {
                int count = rangeList.Count;
                for (int i = 0; i < count; i++)
                {
                    (char First, char Last) range = rangeList[i];
                    if (range.First == range.Last)
                    {
                        if (RegexCaseEquivalences.TryFindCaseEquivalencesForCharWithIBehavior(range.First, culture, out ReadOnlySpan<char> equivalences))
                        {
                            foreach (char equivalence in equivalences)
                            {
                                AddChar(equivalence);
                            }
                        }
                    }
                    else
                    {
                        AddCaseEquivalenceRange(range.First, range.Last, culture);
                    }
                }
            }
        }

        /// <summary>
        /// For a single range that's in the set, adds any additional ranges
        /// necessary to ensure that lowercase equivalents are also included.
        /// </summary>
        private void AddCaseEquivalenceRange(char chMin, char chMax, CultureInfo culture)
        {
            for (int i = chMin; i <= chMax; i++)
            {
                if (RegexCaseEquivalences.TryFindCaseEquivalencesForCharWithIBehavior((char)i, culture, out ReadOnlySpan<char> equivalences))
                {
                    foreach (char equivalence in equivalences)
                    {
                        AddChar(equivalence);
                    }
                }
            }
        }

        public void AddWord(bool ecma, bool negate)
        {
            if (ecma)
            {
                AddSet((negate ? NotECMAWordSet : ECMAWordSet).AsSpan());
            }
            else
            {
                AddCategory(negate ? NotWord : Word);
            }
        }

        public void AddSpace(bool ecma, bool negate)
        {
            if (ecma)
            {
                AddSet((negate ? NotECMASpaceSet : ECMASpaceSet).AsSpan());
            }
            else
            {
                AddCategory(negate ? NotSpace : Space);
            }
        }

        public void AddDigit(bool ecma, bool negate, string pattern, int currentPos)
        {
            if (ecma)
            {
                AddSet((negate ? NotECMADigitSet : ECMADigitSet).AsSpan());
            }
            else
            {
                AddCategoryFromName("Nd", negate, caseInsensitive: false, pattern, currentPos);
            }
        }

        public static string ConvertOldStringsToClass(string set, string category)
        {
            bool startsWithNulls = set.Length >= 2 && set[0] == '\0' && set[1] == '\0';
            int strLength = SetStartIndex + set.Length + category.Length;
            if (startsWithNulls)
            {
                strLength -= 2;
            }

#if REGEXGENERATOR
            return StringExtensions.Create
#else
            return string.Create
#endif
                (strLength, (set, category, startsWithNulls), static (span, state) =>
            {
                int index;

                if (state.startsWithNulls)
                {
                    span[FlagsIndex] = (char)0x1;
                    span[SetLengthIndex] = (char)(state.set.Length - 2);
                    span[CategoryLengthIndex] = (char)state.category.Length;
                    state.set.AsSpan(2).CopyTo(span.Slice(SetStartIndex));
                    index = SetStartIndex + state.set.Length - 2;
                }
                else
                {
                    span[FlagsIndex] = '\0';
                    span[SetLengthIndex] = (char)state.set.Length;
                    span[CategoryLengthIndex] = (char)state.category.Length;
                    state.set.AsSpan().CopyTo(span.Slice(SetStartIndex));
                    index = SetStartIndex + state.set.Length;
                }

                state.category.AsSpan().CopyTo(span.Slice(index));
            });
        }

        /// <summary>
        /// Returns the char
        /// </summary>
        public static char SingletonChar(string set)
        {
            Debug.Assert(IsSingleton(set) || IsSingletonInverse(set), "Tried to get the singleton char out of a non singleton character class");
            return set[SetStartIndex];
        }

        public static bool IsMergeable(string charClass) =>
            charClass != null &&
            !IsNegated(charClass) &&
            !IsSubtraction(charClass);

        public static bool IsEmpty(string charClass) =>
            charClass[CategoryLengthIndex] == 0 &&
            charClass[SetLengthIndex] == 0 &&
            !IsNegated(charClass) &&
            !IsSubtraction(charClass);

        /// <summary><c>true</c> if the set contains a single character only</summary>
        /// <remarks>
        /// This will happen not only from character classes manually written to contain a single character,
        /// but much more frequently by the implementation/parser itself, e.g. when looking for \n as part of
        /// finding the end of a line, when processing an alternation like "hello|hithere" where the first
        /// character of both options is the same, etc.
        /// </remarks>
        public static bool IsSingleton(string set) =>
            set[CategoryLengthIndex] == 0 &&
            set[SetLengthIndex] == 2 &&
            !IsNegated(set) &&
            !IsSubtraction(set) &&
            (set[SetStartIndex] == LastChar || set[SetStartIndex] + 1 == set[SetStartIndex + 1]);

        public static bool IsSingletonInverse(string set) =>
            set[CategoryLengthIndex] == 0 &&
            set[SetLengthIndex] == 2 &&
            IsNegated(set) &&
            !IsSubtraction(set) &&
            (set[SetStartIndex] == LastChar || set[SetStartIndex] + 1 == set[SetStartIndex + 1]);

        /// <summary>Gets whether the set contains nothing other than a single UnicodeCategory (it may be negated).</summary>
        /// <param name="set">The set to examine.</param>
        /// <param name="category">The single category if there was one.</param>
        /// <param name="negated">true if the single category is a not match.</param>
        /// <returns>true if a single category could be obtained; otherwise, false.</returns>
        public static bool TryGetSingleUnicodeCategory(string set, out UnicodeCategory category, out bool negated)
        {
            if (set[CategoryLengthIndex] == 1 &&
                set[SetLengthIndex] == 0 &&
                !IsSubtraction(set))
            {
                short c = (short)set[SetStartIndex];

                if (c > 0)
                {
                    if (c != SpaceConst)
                    {
                        category = (UnicodeCategory)(c - 1);
                        negated = IsNegated(set);
                        return true;
                    }
                }
                else if (c < 0)
                {
                    if (c != NotSpaceConst)
                    {
                        category = (UnicodeCategory)(-1 - c);
                        negated = !IsNegated(set);
                        return true;
                    }
                }
            }

            category = default;
            negated = false;
            return false;
        }

        /// <summary>Attempts to get a single range stored in the set.</summary>
        /// <param name="set">The set.</param>
        /// <param name="lowInclusive">The inclusive lower-bound of the range, if available.</param>
        /// <param name="highInclusive">The inclusive upper-bound of the range, if available.</param>
        /// <returns>true if the set contained a single range; otherwise, false.</returns>
        /// <remarks>
        /// <paramref name="lowInclusive"/> and <paramref name="highInclusive"/> will be equal if the
        /// range is a singleton or singleton inverse. The range will need to be negated by the caller
        /// if <see cref="IsNegated(string)"/> is true.
        /// </remarks>
        public static bool TryGetSingleRange(string set, out char lowInclusive, out char highInclusive)
        {
            if (set[CategoryLengthIndex] == 0 && // must not have any categories
                set.Length == SetStartIndex + set[SetLengthIndex]) // and no subtraction
            {
                switch ((int)set[SetLengthIndex])
                {
                    case 1:
                        lowInclusive = set[SetStartIndex];
                        highInclusive = LastChar;
                        return true;

                    case 2:
                        lowInclusive = set[SetStartIndex];
                        highInclusive = (char)(set[SetStartIndex + 1] - 1);
                        return true;
                }
            }

            lowInclusive = highInclusive = '\0';
            return false;
        }

        /// <summary>Gets all of the characters in the specified set, storing them into the provided span.</summary>
        /// <param name="set">The character class.</param>
        /// <param name="chars">The span into which the chars should be stored.</param>
        /// <returns>
        /// The number of stored chars.  If they won't all fit, 0 is returned.
        /// If 0 is returned, no assumptions can be made about the characters.
        /// </returns>
        /// <remarks>
        /// Only considers character classes that only contain sets (no categories)
        /// and no subtraction... just simple sets containing starting/ending pairs.
        /// The returned characters may be negated: if IsNegated(set) is false, then
        /// the returned characters are the only ones that match; if it returns true,
        /// then the returned characters are the only ones that don't match.
        /// </remarks>
        public static int GetSetChars(string set, Span<char> chars)
        {
            // We get the characters by enumerating the set portion, so we validate that it's
            // set up to enable that, e.g. no categories.
            if (!CanEasilyEnumerateSetContents(set))
            {
                return 0;
            }

            // Iterate through the pairs of ranges, storing each value in each range
            // into the supplied span.  If they all won't fit, we give up and return 0.
            // Otherwise we return the number found.  Note that we don't bother to handle
            // the corner case where the last range's upper bound is LastChar (\uFFFF),
            // based on it a) complicating things, and b) it being really unlikely to
            // be part of a small set.
            int setLength = set[SetLengthIndex];
            int count = 0;
            for (int i = SetStartIndex; i < SetStartIndex + setLength; i += 2)
            {
                int curSetEnd = set[i + 1];
                for (int c = set[i]; c < curSetEnd; c++)
                {
                    if (count >= chars.Length)
                    {
                        return 0;
                    }

                    chars[count++] = (char)c;
                }
            }

            return count;
        }

        /// <summary>
        /// Determines whether two sets may overlap.
        /// </summary>
        /// <returns>false if the two sets do not overlap; true if they may.</returns>
        /// <remarks>
        /// If the method returns false, the caller can be sure the sets do not overlap.
        /// If the method returns true, it's still possible the sets don't overlap.
        /// </remarks>
        public static bool MayOverlap(string set1, string set2)
        {
            // If the sets are identical, there's obviously overlap.
            if (set1 == set2)
            {
                return true;
            }

            // If either set is all-inclusive, there's overlap by definition (unless
            // the other set is empty, but that's so rare it's not worth checking.)
            if (set1 == AnyClass || set2 == AnyClass)
            {
                return true;
            }

            // If one set is negated and the other one isn't, we're in one of two situations:
            // - The remainder of the sets are identical, in which case these are inverses of
            //   each other, and they don't overlap.
            // - The remainder of the sets aren't identical, in which case there's very likely
            //   overlap, and it's not worth spending more time investigating.
            bool set1Negated = IsNegated(set1);
            bool set2Negated = IsNegated(set2);
            if (set1Negated != set2Negated)
            {
                return !set1.AsSpan(1).SequenceEqual(set2.AsSpan(1));
            }

            // If the sets are negated, since they're not equal, there's almost certainly overlap.
            Debug.Assert(set1Negated == set2Negated);
            if (set1Negated)
            {
                return true;
            }

            // Special-case some known, common classes that don't overlap.
            if (KnownDistinctSets(set1, set2) ||
                KnownDistinctSets(set2, set1))
            {
                return false;
            }

            // If set2 can be easily enumerated (e.g. no unicode categories), then enumerate it and
            // check if any of its members are in set1.  Otherwise, the same for set1.
            if (CanEasilyEnumerateSetContents(set2))
            {
                return MayOverlapByEnumeration(set1, set2);
            }
            else if (CanEasilyEnumerateSetContents(set1))
            {
                return MayOverlapByEnumeration(set2, set1);
            }

            // Assume that everything else might overlap.  In the future if it proved impactful, we could be more accurate here,
            // at the exense of more computation time.
            return true;

            static bool KnownDistinctSets(string set1, string set2) =>
                (set1 == SpaceClass || set1 == ECMASpaceClass) &&
                (set2 == DigitClass || set2 == WordClass || set2 == ECMADigitClass || set2 == ECMAWordClass);

            static bool MayOverlapByEnumeration(string set1, string set2)
            {
                Debug.Assert(!IsNegated(set1) && !IsNegated(set2));
                for (int i = SetStartIndex; i < SetStartIndex + set2[SetLengthIndex]; i += 2)
                {
                    int curSetEnd = set2[i + 1];
                    for (int c = set2[i]; c < curSetEnd; c++)
                    {
                        if (CharInClass((char)c, set1))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>Gets whether the specified character participates in case conversion.</summary>
        /// <remarks>
        /// This method is used to perform operations as if they were case-sensitive even if they're
        /// specified as being case-insensitive.  Such a reduction can be applied when the only character
        /// that would lower-case to the one being searched for / compared against is that character itself.
        /// </remarks>
        public static bool ParticipatesInCaseConversion(int comparison)
        {
            Debug.Assert((uint)comparison <= char.MaxValue);

            switch (char.GetUnicodeCategory((char)comparison))
            {
                case UnicodeCategory.ClosePunctuation:
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.Control:
                case UnicodeCategory.DashPunctuation:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.FinalQuotePunctuation:
                case UnicodeCategory.InitialQuotePunctuation:
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.OtherNumber:
                case UnicodeCategory.OtherPunctuation:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.SpaceSeparator:
                    // All chars in these categories meet the criteria that the only way
                    // `char.ToLower(toTest, AnyCulture) == charInAboveCategory` is when
                    // toTest == charInAboveCategory.
                    return false;

                default:
                    // We don't know (without testing the character against every other
                    // character), so assume it does.
                    return true;
            }
        }

        /// <summary>Gets whether the specified span participates in case conversion.</summary>
        /// <remarks>The span participates in case conversion if any of its characters do.</remarks>
        public static bool ParticipatesInCaseConversion(ReadOnlySpan<char> s)
        {
            foreach (char c in s)
            {
                if (ParticipatesInCaseConversion(c))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Gets whether the specified span contains only ASCII.</summary>
        public static bool IsAscii(ReadOnlySpan<char> s) // TODO https://github.com/dotnet/runtime/issues/28230: Replace once Ascii is available
        {
            foreach (char c in s)
            {
                if (c >= 128)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Gets whether we can iterate through the set list pairs in order to completely enumerate the set's contents.</summary>
        /// <remarks>This may enumerate negated characters if the set is negated.</remarks>
        private static bool CanEasilyEnumerateSetContents(string set) =>
            set.Length > SetStartIndex &&
            set[SetLengthIndex] > 0 &&
            set[SetLengthIndex] % 2 == 0 &&
            set[CategoryLengthIndex] == 0 &&
            !IsSubtraction(set);

        /// <summary>Provides results from <see cref="Analyze"/>.</summary>
        internal struct CharClassAnalysisResults
        {
            /// <summary>true if we know for sure that the set contains only ASCII values; otherwise, false.</summary>
            public bool ContainsOnlyAscii;
            /// <summary>true if we know for sure that the set doesn't contain any ASCII values; otherwise, false.</summary>
            public bool ContainsNoAscii;
            /// <summary>true if we know for sure that all ASCII values are in the set; otherwise, false.</summary>
            public bool AllAsciiContained;
            /// <summary>true if we know for sure that all non-ASCII values are in the set; otherwise, false.</summary>
            public bool AllNonAsciiContained;
            /// <summary>The exclusive upper bound. Only valid if <see cref="ContainsOnlyAscii"/> is true.</summary>
            public int UpperBoundExclusiveIfContainsOnlyAscii;
        }

        /// <summary>Analyzes the set to determine some basic properties that can be used to optimize usage.</summary>
        internal static CharClassAnalysisResults Analyze(string set)
        {
            if (!CanEasilyEnumerateSetContents(set))
            {
                // We can't make any strong claims about the set.
                return default;
            }

#if DEBUG
            for (int i = SetStartIndex; i < set.Length - 1; i += 2)
            {
                Debug.Assert(set[i] < set[i + 1]);
            }
#endif

            if (IsNegated(set))
            {
                // We're negated: if the upper bound of the range is ASCII, that means everything
                // above it is actually included, meaning all non-ASCII are in the class.
                // Similarly if the lower bound is non-ASCII, that means in a negated world
                // everything ASCII is included.
                return new CharClassAnalysisResults
                {
                    AllNonAsciiContained = set[set.Length - 1] < 128,
                    AllAsciiContained = set[SetStartIndex] >= 128,
                    ContainsNoAscii = false,
                    ContainsOnlyAscii = false
                };
            }

            // If the upper bound is ASCII, that means everything included in the class is ASCII.
            // Similarly if the lower bound is non-ASCII, that means no ASCII is in the class.
            return new CharClassAnalysisResults
            {
                AllNonAsciiContained = false,
                AllAsciiContained = false,
                ContainsOnlyAscii = set[set.Length - 1] <= 128,
                ContainsNoAscii = set[SetStartIndex] >= 128,
                UpperBoundExclusiveIfContainsOnlyAscii = set[set.Length - 1],
            };
        }

        internal static bool IsSubtraction(string charClass) =>
            charClass.Length > SetStartIndex +
            charClass[CategoryLengthIndex] +
            charClass[SetLengthIndex];

        internal static bool IsNegated(string set) => set[FlagsIndex] == 1;

        internal static bool IsNegated(string set, int setOffset) => set[FlagsIndex + setOffset] == 1;

        public static bool IsECMAWordChar(char ch) =>
            // According to ECMA-262, \s, \S, ., ^, and $ use Unicode-based interpretations of
            // whitespace and newline, while \d, \D\, \w, \W, \b, and \B use ASCII-only
            // interpretations of digit, word character, and word boundary.  In other words,
            // no special treatment of Unicode ZERO WIDTH NON-JOINER (ZWNJ U+200C) and
            // ZERO WIDTH JOINER (ZWJ U+200D) is required for ECMA word boundaries.
            ((((uint)ch - 'A') & ~0x20) < 26) || // ASCII letter
            (((uint)ch - '0') < 10) || // digit
            ch == '_' || // underscore
            ch == '\u0130'; // latin capital letter I with dot above

        /// <summary>16 bytes, representing the chars 0 through 127, with a 1 for a bit where that char is a word char.</summary>
        private static ReadOnlySpan<byte> WordCharAsciiLookup => new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x03,
            0xFE, 0xFF, 0xFF, 0x87, 0xFE, 0xFF, 0xFF, 0x07
        };

        /// <summary>Determines whether a character is considered a word character for the purposes of testing the \w set.</summary>
        public static bool IsWordChar(char ch)
        {
            // This is the same as IsBoundaryWordChar, except that IsBoundaryWordChar also
            // returns true for \u200c and \u200d.

            // Fast lookup in our lookup table for ASCII characters.  This is purely an optimization, and has the
            // behavior as if we fell through to the switch below (which was actually used to produce the lookup table).
            ReadOnlySpan<byte> asciiLookup = WordCharAsciiLookup;
            int chDiv8 = ch >> 3;
            if ((uint)chDiv8 < (uint)asciiLookup.Length)
            {
                return (asciiLookup[chDiv8] & (1 << (ch & 0x7))) != 0;
            }

            // For non-ASCII, fall back to checking the Unicode category.
            switch (CharUnicodeInfo.GetUnicodeCategory(ch))
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.ConnectorPunctuation:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>Determines whether a character is considered a word character for the purposes of testing a word character boundary.</summary>
        public static bool IsBoundaryWordChar(char ch)
        {
            // According to UTS#18 Unicode Regular Expressions (http://www.unicode.org/reports/tr18/)
            // RL 1.4 Simple Word Boundaries  The class of <word_character> includes all Alphabetic
            // values from the Unicode character database, from UnicodeData.txt [UData], plus the U+200C
            // ZERO WIDTH NON-JOINER and U+200D ZERO WIDTH JOINER.

            // Fast lookup in our lookup table for ASCII characters.  This is purely an optimization, and has the
            // behavior as if we fell through to the switch below (which was actually used to produce the lookup table).
            ReadOnlySpan<byte> asciiLookup = WordCharAsciiLookup;
            int chDiv8 = ch >> 3;
            if ((uint)chDiv8 < (uint)asciiLookup.Length)
            {
                return (asciiLookup[chDiv8] & (1 << (ch & 0x7))) != 0;
            }

            // For non-ASCII, fall back to checking the Unicode category.
            switch (CharUnicodeInfo.GetUnicodeCategory(ch))
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.ConnectorPunctuation:
                    return true;

                default:
                    const char ZeroWidthNonJoiner = '\u200C', ZeroWidthJoiner = '\u200D';
                    return ch == ZeroWidthJoiner | ch == ZeroWidthNonJoiner;
            }
        }

        /// <summary>Determines whether the 'a' and 'b' values differ by only a single bit, setting that bit in 'mask'.</summary>
        /// <remarks>This isn't specific to RegexCharClass; it's just a convenient place to host it.</remarks>
        public static bool DifferByOneBit(char a, char b, out int mask)
        {
            mask = a ^ b;
            return mask != 0 && (mask & (mask - 1)) == 0;
        }

        /// <summary>Determines a character's membership in a character class (via the string representation of the class).</summary>
        /// <param name="ch">The character.</param>
        /// <param name="set">The string representation of the character class.</param>
        /// <param name="asciiLazyCache">A lazily-populated cache for ASCII results stored in a 256-bit array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CharInClass(char ch, string set, ref uint[]? asciiLazyCache)
        {
            // The uint[] contains 8 ints, or 256 bits.  These are laid out as pairs, where the first bit in the pair
            // says whether the second bit in the pair has already been computed.  Once a value is computed, it's never
            // changed, so since Int32s are written/read atomically, we can trust the value bit if we see that the known bit
            // has been set.  If the known bit hasn't been set, then we proceed to look it up, and then swap in the result.
            const int CacheArrayLength = 8;
            Debug.Assert(asciiLazyCache is null || asciiLazyCache.Length == CacheArrayLength, "set lookup should be able to store two bits for each of the first 128 characters");

            // If the value is ASCII and already has an answer for this value, use it.
            if (asciiLazyCache is uint[] cache)
            {
                int index = ch >> 4;
                if ((uint)index < (uint)cache.Length)
                {
                    Debug.Assert(ch < 128);
                    uint current = cache[index];
                    uint bit = 1u << ((ch & 0xF) << 1);
                    if ((current & bit) != 0)
                    {
                        return (current & (bit << 1)) != 0;
                    }
                }
            }

            // For ASCII, lazily initialize. For non-ASCII, just compute the value.
            return ch < 128 ?
                InitializeValue(ch, set, ref asciiLazyCache) :
                CharInClassRecursive(ch, set, 0);

            static bool InitializeValue(char ch, string set, ref uint[]? asciiLazyCache)
            {
                // (After warm-up, we should find ourselves rarely getting here.)
                Debug.Assert(ch < 128);

                // Compute the result and determine which bits to write back to the array and "or" the bits back in a thread-safe manner.
                bool isInClass = CharInClass(ch, set);
                uint bitsToSet = 1u << ((ch & 0xF) << 1);
                if (isInClass)
                {
                    bitsToSet |= bitsToSet << 1;
                }

                uint[]? cache = asciiLazyCache ?? Interlocked.CompareExchange(ref asciiLazyCache, new uint[CacheArrayLength], null) ?? asciiLazyCache;
#if REGEXGENERATOR
                InterlockedExtensions.Or(ref cache[ch >> 4], bitsToSet);
#else
                Interlocked.Or(ref cache[ch >> 4], bitsToSet);
#endif

                // Return the computed value.
                return isInClass;
            }
        }

        /// <summary>
        /// Determines a character's membership in a character class (via the string representation of the class).
        /// </summary>
        public static bool CharInClass(char ch, string set) =>
            CharInClassRecursive(ch, set, 0);

        private static bool CharInClassRecursive(char ch, string set, int start)
        {
            int setLength = set[start + SetLengthIndex];
            int categoryLength = set[start + CategoryLengthIndex];
            int endPosition = start + SetStartIndex + setLength + categoryLength;

            bool inClass = CharInClassInternal(ch, set, start, setLength, categoryLength);

            // Note that we apply the negation *before* performing the subtraction.  This is because
            // the negation only applies to the first char class, not the entire subtraction.
            if (IsNegated(set, start))
            {
                inClass = !inClass;
            }

            // Subtract if necessary
            if (inClass && set.Length > endPosition)
            {
                inClass = !CharInClassRecursive(ch, set, endPosition);
            }

            return inClass;
        }

        /// <summary>
        /// Determines a character's membership in a character class (via the
        /// string representation of the class).
        /// </summary>
        private static bool CharInClassInternal(char ch, string set, int start, int setLength, int categoryLength)
        {
            int min = start + SetStartIndex;
            int max = min + setLength;

            while (min != max)
            {
                int mid = (min + max) >> 1;
                if (ch < set[mid])
                {
                    max = mid;
                }
                else
                {
                    min = mid + 1;
                }
            }

            // The starting position of the set within the character class determines
            // whether what an odd or even ending position means.  If the start is odd,
            // an *even* ending position means the character was in the set.  With recursive
            // subtractions in the mix, the starting position = start+SetStartIndex.  Since we know that
            // SetStartIndex is odd, we can simplify it out of the equation.  But if it changes we need to
            // reverse this check.
            Debug.Assert((SetStartIndex & 0x1) == 1, "If SetStartIndex is not odd, the calculation below this will be reversed");
            if ((min & 0x1) == (start & 0x1))
            {
                return true;
            }

            if (categoryLength == 0)
            {
                return false;
            }

            return CharInCategory(ch, set, start, setLength, categoryLength);
        }

        private static bool CharInCategory(char ch, string set, int start, int setLength, int categoryLength)
        {
            UnicodeCategory chcategory = char.GetUnicodeCategory(ch);

            int i = start + SetStartIndex + setLength;
            int end = i + categoryLength;
            while (i < end)
            {
                int curcat = (short)set[i];

                if (curcat == 0)
                {
                    // zero is our marker for a group of categories - treated as a unit
                    if (CharInCategoryGroup(chcategory, set, ref i))
                    {
                        return true;
                    }
                }
                else if (curcat > 0)
                {
                    // greater than zero is a positive case

                    if (curcat == SpaceConst)
                    {
                        if (char.IsWhiteSpace(ch))
                        {
                            return true;
                        }
                    }
                    else if (chcategory == (UnicodeCategory)(curcat - 1))
                    {
                        return true;
                    }
                }
                else
                {
                    // less than zero is a negative case
                    if (curcat == NotSpaceConst)
                    {
                        if (!char.IsWhiteSpace(ch))
                        {
                            return true;
                        }
                    }
                    else if (chcategory != (UnicodeCategory)(-1 - curcat))
                    {
                        return true;
                    }
                }

                i++;
            }

            return false;
        }

        /// <summary>
        /// This is used for categories which are composed of other categories - L, N, Z, W...
        /// These groups need special treatment when they are negated
        /// </summary>
        private static bool CharInCategoryGroup(UnicodeCategory chcategory, string category, ref int i)
        {
            int pos = i + 1;
            int curcat = (short)category[pos];

            bool result;

            if (curcat > 0)
            {
                // positive case - the character must be in ANY of the categories in the group
                result = false;
                for (; curcat != 0; curcat = (short)category[pos])
                {
                    pos++;
                    if (!result && chcategory == (UnicodeCategory)(curcat - 1))
                    {
                        result = true;
                    }
                }
            }
            else
            {
                // negative case - the character must be in NONE of the categories in the group
                result = true;
                for (; curcat != 0; curcat = (short)category[pos])
                {
                    pos++;
                    if (result && chcategory == (UnicodeCategory)(-1 - curcat))
                    {
                        result = false;
                    }
                }
            }

            i = pos;
            return result;
        }

        public static RegexCharClass Parse(string charClass) => ParseRecursive(charClass, 0);

        private static RegexCharClass ParseRecursive(string charClass, int start)
        {
            int setLength = charClass[start + SetLengthIndex];
            int categoryLength = charClass[start + CategoryLengthIndex];
            int endPosition = start + SetStartIndex + setLength + categoryLength;

            int i = start + SetStartIndex;
            int end = i + setLength;

            List<(char First, char Last)>? ranges = ComputeRanges(charClass.AsSpan(start));

            RegexCharClass? sub = null;
            if (charClass.Length > endPosition)
            {
                sub = ParseRecursive(charClass, endPosition);
            }

            StringBuilder? categoriesBuilder = null;
            if (categoryLength > 0)
            {
                categoriesBuilder = new StringBuilder().Append(charClass.AsSpan(end, categoryLength));
            }

            return new RegexCharClass(IsNegated(charClass, start), ranges, categoriesBuilder, sub);
        }

        /// <summary>Computes a list of all of the character ranges in the set string.</summary>
        public static List<(char First, char Last)>? ComputeRanges(ReadOnlySpan<char> set)
        {
            int setLength = set[SetLengthIndex];
            int i = SetStartIndex;
            int end = i + setLength;

            List<(char First, char Last)>? ranges = null;
            if (setLength > 0)
            {
                ranges = new List<(char First, char Last)>(setLength);
                while (i < end)
                {
                    char first = set[i];
                    i++;

                    char last = i < end ? (char)(set[i] - 1) : LastChar;
                    i++;

                    ranges.Add((first, last));
                }
            }

            return ranges;
        }

        /// <summary>Creates a set string for a single character.</summary>
        /// <param name="c">The character for which to create the set.</param>
        /// <returns>The create set string.</returns>
        public static string OneToStringClass(char c)
            => CharsToStringClass(stackalloc char[1] { c });

        internal static unsafe string CharsToStringClass(ReadOnlySpan<char> chars)
        {
#if DEBUG
            // Make sure they're all sorted with no duplicates
            for (int index = 0; index < chars.Length - 1; index++)
            {
                Debug.Assert(chars[index] < chars[index + 1]);
            }
#endif

            // If there aren't any chars, just return an empty class.
            if (chars.Length == 0)
            {
                return EmptyClass;
            }

            if (chars.Length == 2)
            {
                switch (chars[0], chars[1])
                {
                    case ('A', 'a'): case ('a', 'A'): return "\0\x0004\0ABab";
                    case ('B', 'b'): case ('b', 'B'): return "\0\x0004\0BCbc";
                    case ('C', 'c'): case ('c', 'C'): return "\0\x0004\0CDcd";
                    case ('D', 'd'): case ('d', 'D'): return "\0\x0004\0DEde";
                    case ('E', 'e'): case ('e', 'E'): return "\0\x0004\0EFef";
                    case ('F', 'f'): case ('f', 'F'): return "\0\x0004\0FGfg";
                    case ('G', 'g'): case ('g', 'G'): return "\0\x0004\0GHgh";
                    case ('H', 'h'): case ('h', 'H'): return "\0\x0004\0HIhi";
                    // 'I' and 'i' are missing since depending on the cultuure they may
                    // have additional mappings.
                    case ('J', 'j'): case ('j', 'J'): return "\0\x0004\0JKjk";
                    // 'K' and 'k' are missing since their mapping also includes Kelvin K.
                    case ('L', 'l'): case ('l', 'L'): return "\0\x0004\0LMlm";
                    case ('M', 'm'): case ('m', 'M'): return "\0\x0004\0MNmn";
                    case ('N', 'n'): case ('n', 'N'): return "\0\x0004\0NOno";
                    case ('O', 'o'): case ('o', 'O'): return "\0\x0004\0OPop";
                    case ('P', 'p'): case ('p', 'P'): return "\0\x0004\0PQpq";
                    case ('Q', 'q'): case ('q', 'Q'): return "\0\x0004\0QRqr";
                    case ('R', 'r'): case ('r', 'R'): return "\0\x0004\0RSrs";
                    case ('S', 's'): case ('s', 'S'): return "\0\x0004\0STst";
                    case ('T', 't'): case ('t', 'T'): return "\0\x0004\0TUtu";
                    case ('U', 'u'): case ('u', 'U'): return "\0\x0004\0UVuv";
                    case ('V', 'v'): case ('v', 'V'): return "\0\x0004\0VWvw";
                    case ('W', 'w'): case ('w', 'W'): return "\0\x0004\0WXwx";
                    case ('X', 'x'): case ('x', 'X'): return "\0\x0004\0XYxy";
                    case ('Y', 'y'): case ('y', 'Y'): return "\0\x0004\0YZyz";
                    case ('Z', 'z'): case ('z', 'Z'): return "\0\x0004\0Z[z{";
                }
            }

            // Count how many characters there actually are.  All but the very last possible
            // char value will have two characters, one for the inclusive beginning of range
            // and one for the exclusive end of range.
            int count = chars.Length * 2;
            if (chars[chars.Length - 1] == LastChar)
            {
                count--;
            }

            // Get the pointer/length of the span to be able to pass it into string.Create.
            fixed (char* charsPtr = chars)
            {
#if REGEXGENERATOR
                return StringExtensions.Create(
#else
                return string.Create(
#endif
                    SetStartIndex + count, ((IntPtr)charsPtr, chars.Length), static (span, state) =>
                {
                    // Reconstruct the span now that we're inside of the lambda.
                    ReadOnlySpan<char> chars = new ReadOnlySpan<char>((char*)state.Item1, state.Length);

                    // Fill in the set string
                    span[FlagsIndex] = (char)0;
                    span[CategoryLengthIndex] = (char)0;
                    span[SetLengthIndex] = (char)(span.Length - SetStartIndex);
                    int i = SetStartIndex;
                    foreach (char c in chars)
                    {
                        span[i++] = c;
                        if (c != LastChar)
                        {
                            span[i++] = (char)(c + 1);
                        }
                    }
                    Debug.Assert(i == span.Length);
                });
            }
        }

        /// <summary>
        /// Constructs the string representation of the class.
        /// </summary>
        public string ToStringClass()
        {
            var vsb = new ValueStringBuilder(stackalloc char[256]);
            ToStringClass(ref vsb);
            return vsb.ToString();
        }

        private void ToStringClass(ref ValueStringBuilder vsb)
        {
            Canonicalize();

            int initialLength = vsb.Length;
            int categoriesLength = _categories?.Length ?? 0;
            Span<char> headerSpan = vsb.AppendSpan(SetStartIndex);
            headerSpan[FlagsIndex] = (char)(_negate ? 1 : 0);
            headerSpan[SetLengthIndex] = '\0'; // (will be replaced once we know how long a range we've added)
            headerSpan[CategoryLengthIndex] = (char)categoriesLength;

            // Append ranges
            List<(char First, char Last)>? rangelist = _rangelist;
            if (rangelist != null)
            {
                for (int i = 0; i < rangelist.Count; i++)
                {
                    (char First, char Last) currentRange = rangelist[i];
                    vsb.Append(currentRange.First);
                    if (currentRange.Last != LastChar)
                    {
                        vsb.Append((char)(currentRange.Last + 1));
                    }
                }
            }

            // Update the range length.  The ValueStringBuilder may have already had some
            // contents (if this is a subtactor), so we need to offset by the initial length.
            vsb[initialLength + SetLengthIndex] = (char)(vsb.Length - initialLength - SetStartIndex);

            // Append categories
            if (categoriesLength != 0)
            {
                foreach (ReadOnlyMemory<char> chunk in _categories!.GetChunks())
                {
                    vsb.Append(chunk.Span);
                }
            }

            // Append a subtractor if there is one.
            _subtractor?.ToStringClass(ref vsb);
        }

        /// <summary>
        /// Logic to reduce a character class to a unique, sorted form.
        /// </summary>
        private void Canonicalize()
        {
            List<(char First, char Last)>? rangelist = _rangelist;
            if (rangelist != null)
            {
                // Find and eliminate overlapping or abutting ranges.
                if (rangelist.Count > 1)
                {
                    rangelist.Sort((x, y) => x.First.CompareTo(y.First));

                    bool done = false;
                    int j = 0;

                    for (int i = 1; ; i++)
                    {
                        char last;
                        for (last = rangelist[j].Last; ; i++)
                        {
                            if (i == rangelist.Count || last == LastChar)
                            {
                                done = true;
                                break;
                            }

                            (char First, char Last) currentRange;
                            if ((currentRange = rangelist[i]).First > last + 1)
                            {
                                break;
                            }

                            if (last < currentRange.Last)
                            {
                                last = currentRange.Last;
                            }
                        }

                        rangelist[j] = (rangelist[j].First, last);

                        j++;

                        if (done)
                        {
                            break;
                        }

                        if (j < i)
                        {
                            rangelist[j] = rangelist[i];
                        }
                    }

                    rangelist.RemoveRange(j, rangelist.Count - j);
                }

                // If the class now represents a single negated range, but does so by including every
                // other character, invert it to produce a normalized form with a single range.  This
                // is valuable for subsequent optimizations in most of the engines.
                if (!_negate &&
                    _subtractor is null &&
                    (_categories is null || _categories.Length == 0))
                {
                    if (rangelist.Count == 2)
                    {
                        // There are two ranges in the list.  See if there's one missing range between them.
                        // Such a range might be as small as a single character.
                        if (rangelist[0].First == 0 &&
                            rangelist[1].Last == LastChar &&
                            rangelist[0].Last < rangelist[1].First - 1)
                        {
                            rangelist[0] = ((char)(rangelist[0].Last + 1), (char)(rangelist[1].First - 1));
                            rangelist.RemoveAt(1);
                            _negate = true;
                        }
                    }
                    else if (rangelist.Count == 1)
                    {
                        if (rangelist[0].First == 0)
                        {
                            // There's only one range in the list.  Does it include everything but the last char?
                            if (rangelist[0].Last == LastChar - 1)
                            {
                                rangelist[0] = (LastChar, LastChar);
                                _negate = true;
                            }
                        }
                        else if (rangelist[0].First == 1)
                        {
                            // Or everything but the first char?
                            if (rangelist[0].Last == LastChar)
                            {
                                rangelist[0] = ('\0', '\0');
                                _negate = true;
                            }
                        }
                    }
                }
            }
        }

        private static ReadOnlySpan<char> SetFromProperty(string capname, bool invert, string pattern, int currentPos)
        {
            int min = 0;
            int max = s_propTable.Length;
            while (min != max)
            {
                int mid = (min + max) / 2;
                int res = string.Compare(capname, s_propTable[mid][0], StringComparison.Ordinal);
                if (res < 0)
                {
                    max = mid;
                }
                else if (res > 0)
                {
                    min = mid + 1;
                }
                else
                {
                    string set = s_propTable[mid][1];
                    Debug.Assert(!string.IsNullOrEmpty(set), "Found a null/empty element in RegexCharClass prop table");
                    return
                        !invert ? set.AsSpan() :
                        set[0] == NullChar ? set.AsSpan(1) :
                        (NullCharString + set).AsSpan();
                }
            }

            throw new RegexParseException(RegexParseError.UnrecognizedUnicodeProperty, currentPos,
                SR.Format(SR.MakeException, pattern, currentPos, SR.Format(SR.UnrecognizedUnicodeProperty, capname)));
        }

        public static readonly string[] CategoryIdToName = PopulateCategoryIdToName();

        private static string[] PopulateCategoryIdToName()
        {
            // Populate category reverse lookup used for diagnostic output

            var temp = new List<KeyValuePair<string, string>>(s_definedCategories);
            temp.RemoveAll(kvp => kvp.Value.Length != 1);
            temp.Sort((kvp1, kvp2) => ((short)kvp1.Value[0]).CompareTo((short)kvp2.Value[0]));
            return temp.ConvertAll(kvp => kvp.Key).ToArray();
        }

        /// <summary>
        /// Produces a human-readable description for a set string.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public static string DescribeSet(string set)
        {
            int setLength = set[SetLengthIndex];
            int categoryLength = set[CategoryLengthIndex];
            int endPosition = SetStartIndex + setLength + categoryLength;

            var desc = new StringBuilder();

            desc.Append('[');

            int index = SetStartIndex;
            char ch1;
            char ch2;

            if (IsNegated(set))
            {
                desc.Append('^');
            }

            while (index < SetStartIndex + set[SetLengthIndex])
            {
                ch1 = set[index];
                ch2 = index + 1 < set.Length ?
                    (char)(set[index + 1] - 1) :
                    LastChar;

                desc.Append(DescribeChar(ch1));

                if (ch2 != ch1)
                {
                    if (ch1 + 1 != ch2)
                    {
                        desc.Append('-');
                    }

                    desc.Append(DescribeChar(ch2));
                }
                index += 2;
            }

            while (index < SetStartIndex + set[SetLengthIndex] + set[CategoryLengthIndex])
            {
                ch1 = set[index];
                if (ch1 == 0)
                {
                    bool found = false;

                    const char GroupChar = (char)0;
                    int lastindex = set.IndexOf(GroupChar, index + 1);
                    if (lastindex != -1)
                    {
                        string group = set.Substring(index, lastindex - index + 1);

                        foreach (KeyValuePair<string, string> kvp in s_definedCategories)
                        {
                            if (group.Equals(kvp.Value))
                            {
                                desc.Append((short)set[index + 1] > 0 ? "\\p{" : "\\P{").Append(kvp.Key).Append('}');
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            if (group.Equals(Word))
                            {
                                desc.Append("\\w");
                            }
                            else if (group.Equals(NotWord))
                            {
                                desc.Append("\\W");
                            }
                            else
                            {
                                // TODO: The code is incorrectly handling pretty-printing groups like \P{P}.
                            }
                        }

                        index = lastindex;
                    }
                }
                else
                {
                    desc.Append(DescribeCategory(ch1));
                }

                index++;
            }

            if (set.Length > endPosition)
            {
                desc.Append('-').Append(DescribeSet(set.Substring(endPosition)));
            }

            return desc.Append(']').ToString();
        }

        /// <summary>Produces a human-readable description for a single character.</summary>
        [ExcludeFromCodeCoverage]
        public static string DescribeChar(char ch) =>
            ch switch
            {
                '\a' => "\\a",
                '\b' => "\\b",
                '\t' => "\\t",
                '\r' => "\\r",
                '\v' => "\\v",
                '\f' => "\\f",
                '\n' => "\\n",
                '\\' => "\\\\",
                >= ' ' and <= '~' => ch.ToString(),
                _ => $"\\u{(uint)ch:X4}"
            };

        [ExcludeFromCodeCoverage]
        private static string DescribeCategory(char ch) =>
            (short)ch switch
            {
                SpaceConst => @"\s",
                NotSpaceConst => @"\S",
                (short)(UnicodeCategory.DecimalDigitNumber + 1) => @"\d",
                -(short)(UnicodeCategory.DecimalDigitNumber + 1) => @"\D",
                < 0 => $"\\P{{{CategoryIdToName[-(short)ch - 1]}}}",
                _ => $"\\p{{{CategoryIdToName[ch - 1]}}}",
            };
    }
}
