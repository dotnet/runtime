// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Xunit;

namespace Microsoft.Framework.WebEncoders
{
    public class UnicodeRangesTests
    {
        [Fact]
        public void Range_None()
        {
            UnicodeRange range = UnicodeRanges.None;
            Assert.NotNull(range);

            // Test 1: the range should be empty
            Assert.Equal(0, range.FirstCodePoint);
            Assert.Equal(0, range.Length);
        }

        [Fact]
        public void Range_All()
        {
            Range_Unicode('\u0000', '\uFFFF', "All");
        }

        [Theory]
        [InlineData('\u0000', '\u007F', "BasicLatin")]
        [InlineData('\u0080', '\u00FF', "Latin1Supplement")]
        [InlineData('\u0100', '\u017F', "LatinExtendedA")]
        [InlineData('\u0180', '\u024F', "LatinExtendedB")]
        [InlineData('\u0250', '\u02AF', "IpaExtensions")]
        [InlineData('\u02B0', '\u02FF', "SpacingModifierLetters")]
        [InlineData('\u0300', '\u036F', "CombiningDiacriticalMarks")]
        [InlineData('\u0370', '\u03FF', "GreekandCoptic")]
        [InlineData('\u0400', '\u04FF', "Cyrillic")]
        [InlineData('\u0500', '\u052F', "CyrillicSupplement")]
        [InlineData('\u0530', '\u058F', "Armenian")]
        [InlineData('\u0590', '\u05FF', "Hebrew")]
        [InlineData('\u0600', '\u06FF', "Arabic")]
        [InlineData('\u0700', '\u074F', "Syriac")]
        [InlineData('\u0750', '\u077F', "ArabicSupplement")]
        [InlineData('\u0780', '\u07BF', "Thaana")]
        [InlineData('\u07C0', '\u07FF', "NKo")]
        [InlineData('\u0800', '\u083F', "Samaritan")]
        [InlineData('\u0840', '\u085F', "Mandaic")]
        [InlineData('\u08A0', '\u08FF', "ArabicExtendedA")]
        [InlineData('\u0900', '\u097F', "Devanagari")]
        [InlineData('\u0980', '\u09FF', "Bengali")]
        [InlineData('\u0A00', '\u0A7F', "Gurmukhi")]
        [InlineData('\u0A80', '\u0AFF', "Gujarati")]
        [InlineData('\u0B00', '\u0B7F', "Oriya")]
        [InlineData('\u0B80', '\u0BFF', "Tamil")]
        [InlineData('\u0C00', '\u0C7F', "Telugu")]
        [InlineData('\u0C80', '\u0CFF', "Kannada")]
        [InlineData('\u0D00', '\u0D7F', "Malayalam")]
        [InlineData('\u0D80', '\u0DFF', "Sinhala")]
        [InlineData('\u0E00', '\u0E7F', "Thai")]
        [InlineData('\u0E80', '\u0EFF', "Lao")]
        [InlineData('\u0F00', '\u0FFF', "Tibetan")]
        [InlineData('\u1000', '\u109F', "Myanmar")]
        [InlineData('\u10A0', '\u10FF', "Georgian")]
        [InlineData('\u1100', '\u11FF', "HangulJamo")]
        [InlineData('\u1200', '\u137F', "Ethiopic")]
        [InlineData('\u1380', '\u139F', "EthiopicSupplement")]
        [InlineData('\u13A0', '\u13FF', "Cherokee")]
        [InlineData('\u1400', '\u167F', "UnifiedCanadianAboriginalSyllabics")]
        [InlineData('\u1680', '\u169F', "Ogham")]
        [InlineData('\u16A0', '\u16FF', "Runic")]
        [InlineData('\u1700', '\u171F', "Tagalog")]
        [InlineData('\u1720', '\u173F', "Hanunoo")]
        [InlineData('\u1740', '\u175F', "Buhid")]
        [InlineData('\u1760', '\u177F', "Tagbanwa")]
        [InlineData('\u1780', '\u17FF', "Khmer")]
        [InlineData('\u1800', '\u18AF', "Mongolian")]
        [InlineData('\u18B0', '\u18FF', "UnifiedCanadianAboriginalSyllabicsExtended")]
        [InlineData('\u1900', '\u194F', "Limbu")]
        [InlineData('\u1950', '\u197F', "TaiLe")]
        [InlineData('\u1980', '\u19DF', "NewTaiLue")]
        [InlineData('\u19E0', '\u19FF', "KhmerSymbols")]
        [InlineData('\u1A00', '\u1A1F', "Buginese")]
        [InlineData('\u1A20', '\u1AAF', "TaiTham")]
        [InlineData('\u1AB0', '\u1AFF', "CombiningDiacriticalMarksExtended")]
        [InlineData('\u1B00', '\u1B7F', "Balinese")]
        [InlineData('\u1B80', '\u1BBF', "Sundanese")]
        [InlineData('\u1BC0', '\u1BFF', "Batak")]
        [InlineData('\u1C00', '\u1C4F', "Lepcha")]
        [InlineData('\u1C50', '\u1C7F', "OlChiki")]
        [InlineData('\u1CC0', '\u1CCF', "SundaneseSupplement")]
        [InlineData('\u1CD0', '\u1CFF', "VedicExtensions")]
        [InlineData('\u1D00', '\u1D7F', "PhoneticExtensions")]
        [InlineData('\u1D80', '\u1DBF', "PhoneticExtensionsSupplement")]
        [InlineData('\u1DC0', '\u1DFF', "CombiningDiacriticalMarksSupplement")]
        [InlineData('\u1E00', '\u1EFF', "LatinExtendedAdditional")]
        [InlineData('\u1F00', '\u1FFF', "GreekExtended")]
        [InlineData('\u2000', '\u206F', "GeneralPunctuation")]
        [InlineData('\u2070', '\u209F', "SuperscriptsandSubscripts")]
        [InlineData('\u20A0', '\u20CF', "CurrencySymbols")]
        [InlineData('\u20D0', '\u20FF', "CombiningDiacriticalMarksforSymbols")]
        [InlineData('\u2100', '\u214F', "LetterlikeSymbols")]
        [InlineData('\u2150', '\u218F', "NumberForms")]
        [InlineData('\u2190', '\u21FF', "Arrows")]
        [InlineData('\u2200', '\u22FF', "MathematicalOperators")]
        [InlineData('\u2300', '\u23FF', "MiscellaneousTechnical")]
        [InlineData('\u2400', '\u243F', "ControlPictures")]
        [InlineData('\u2440', '\u245F', "OpticalCharacterRecognition")]
        [InlineData('\u2460', '\u24FF', "EnclosedAlphanumerics")]
        [InlineData('\u2500', '\u257F', "BoxDrawing")]
        [InlineData('\u2580', '\u259F', "BlockElements")]
        [InlineData('\u25A0', '\u25FF', "GeometricShapes")]
        [InlineData('\u2600', '\u26FF', "MiscellaneousSymbols")]
        [InlineData('\u2700', '\u27BF', "Dingbats")]
        [InlineData('\u27C0', '\u27EF', "MiscellaneousMathematicalSymbolsA")]
        [InlineData('\u27F0', '\u27FF', "SupplementalArrowsA")]
        [InlineData('\u2800', '\u28FF', "BraillePatterns")]
        [InlineData('\u2900', '\u297F', "SupplementalArrowsB")]
        [InlineData('\u2980', '\u29FF', "MiscellaneousMathematicalSymbolsB")]
        [InlineData('\u2A00', '\u2AFF', "SupplementalMathematicalOperators")]
        [InlineData('\u2B00', '\u2BFF', "MiscellaneousSymbolsandArrows")]
        [InlineData('\u2C00', '\u2C5F', "Glagolitic")]
        [InlineData('\u2C60', '\u2C7F', "LatinExtendedC")]
        [InlineData('\u2C80', '\u2CFF', "Coptic")]
        [InlineData('\u2D00', '\u2D2F', "GeorgianSupplement")]
        [InlineData('\u2D30', '\u2D7F', "Tifinagh")]
        [InlineData('\u2D80', '\u2DDF', "EthiopicExtended")]
        [InlineData('\u2DE0', '\u2DFF', "CyrillicExtendedA")]
        [InlineData('\u2E00', '\u2E7F', "SupplementalPunctuation")]
        [InlineData('\u2E80', '\u2EFF', "CjkRadicalsSupplement")]
        [InlineData('\u2F00', '\u2FDF', "KangxiRadicals")]
        [InlineData('\u2FF0', '\u2FFF', "IdeographicDescriptionCharacters")]
        [InlineData('\u3000', '\u303F', "CjkSymbolsandPunctuation")]
        [InlineData('\u3040', '\u309F', "Hiragana")]
        [InlineData('\u30A0', '\u30FF', "Katakana")]
        [InlineData('\u3100', '\u312F', "Bopomofo")]
        [InlineData('\u3130', '\u318F', "HangulCompatibilityJamo")]
        [InlineData('\u3190', '\u319F', "Kanbun")]
        [InlineData('\u31A0', '\u31BF', "BopomofoExtended")]
        [InlineData('\u31C0', '\u31EF', "CjkStrokes")]
        [InlineData('\u31F0', '\u31FF', "KatakanaPhoneticExtensions")]
        [InlineData('\u3200', '\u32FF', "EnclosedCjkLettersandMonths")]
        [InlineData('\u3300', '\u33FF', "CjkCompatibility")]
        [InlineData('\u3400', '\u4DBF', "CjkUnifiedIdeographsExtensionA")]
        [InlineData('\u4DC0', '\u4DFF', "YijingHexagramSymbols")]
        [InlineData('\u4E00', '\u9FFF', "CjkUnifiedIdeographs")]
        [InlineData('\uA000', '\uA48F', "YiSyllables")]
        [InlineData('\uA490', '\uA4CF', "YiRadicals")]
        [InlineData('\uA4D0', '\uA4FF', "Lisu")]
        [InlineData('\uA500', '\uA63F', "Vai")]
        [InlineData('\uA640', '\uA69F', "CyrillicExtendedB")]
        [InlineData('\uA6A0', '\uA6FF', "Bamum")]
        [InlineData('\uA700', '\uA71F', "ModifierToneLetters")]
        [InlineData('\uA720', '\uA7FF', "LatinExtendedD")]
        [InlineData('\uA800', '\uA82F', "SylotiNagri")]
        [InlineData('\uA830', '\uA83F', "CommonIndicNumberForms")]
        [InlineData('\uA840', '\uA87F', "Phagspa")]
        [InlineData('\uA880', '\uA8DF', "Saurashtra")]
        [InlineData('\uA8E0', '\uA8FF', "DevanagariExtended")]
        [InlineData('\uA900', '\uA92F', "KayahLi")]
        [InlineData('\uA930', '\uA95F', "Rejang")]
        [InlineData('\uA960', '\uA97F', "HangulJamoExtendedA")]
        [InlineData('\uA980', '\uA9DF', "Javanese")]
        [InlineData('\uA9E0', '\uA9FF', "MyanmarExtendedB")]
        [InlineData('\uAA00', '\uAA5F', "Cham")]
        [InlineData('\uAA60', '\uAA7F', "MyanmarExtendedA")]
        [InlineData('\uAA80', '\uAADF', "TaiViet")]
        [InlineData('\uAAE0', '\uAAFF', "MeeteiMayekExtensions")]
        [InlineData('\uAB00', '\uAB2F', "EthiopicExtendedA")]
        [InlineData('\uAB30', '\uAB6F', "LatinExtendedE")]
        [InlineData('\uAB70', '\uABBF', "CherokeeSupplement")]
        [InlineData('\uABC0', '\uABFF', "MeeteiMayek")]
        [InlineData('\uAC00', '\uD7AF', "HangulSyllables")]
        [InlineData('\uD7B0', '\uD7FF', "HangulJamoExtendedB")]
        [InlineData('\uF900', '\uFAFF', "CjkCompatibilityIdeographs")]
        [InlineData('\uFB00', '\uFB4F', "AlphabeticPresentationForms")]
        [InlineData('\uFB50', '\uFDFF', "ArabicPresentationFormsA")]
        [InlineData('\uFE00', '\uFE0F', "VariationSelectors")]
        [InlineData('\uFE10', '\uFE1F', "VerticalForms")]
        [InlineData('\uFE20', '\uFE2F', "CombiningHalfMarks")]
        [InlineData('\uFE30', '\uFE4F', "CjkCompatibilityForms")]
        [InlineData('\uFE50', '\uFE6F', "SmallFormVariants")]
        [InlineData('\uFE70', '\uFEFF', "ArabicPresentationFormsB")]
        [InlineData('\uFF00', '\uFFEF', "HalfwidthandFullwidthForms")]
        [InlineData('\uFFF0', '\uFFFF', "Specials")]
        public void Range_Unicode(ushort first, ushort last, string blockName)
        {
            Assert.Equal(0x0, first & 0xF); // first char in any block should be U+nnn0
            Assert.Equal(0xF, last & 0xF); // last char in any block should be U+nnnF
            Assert.True(first < last); // code point ranges should be ordered

            var propInfo = typeof(UnicodeRanges).GetRuntimeProperty(blockName);
            Assert.NotNull(propInfo);

            UnicodeRange range = (UnicodeRange)propInfo.GetValue(null);
            Assert.NotNull(range);

            // Test 1: the range should span the range first..last
            Assert.Equal(first, range.FirstCodePoint);
            Assert.Equal(last, range.FirstCodePoint + range.Length - 1);
        }
    }
}
