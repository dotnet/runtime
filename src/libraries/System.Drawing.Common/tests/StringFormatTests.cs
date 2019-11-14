// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Text;
using Xunit;

namespace System.Drawing.Tests
{
    public class StringFormatTests
    {
        private const int RandomLanguageCode = 10;
        private const int EnglishLanguageCode = 2057;

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Default()
        {
            using (var format = new StringFormat())
            {
                Assert.Equal(StringAlignment.Near, format.Alignment);
                Assert.Equal(0, format.DigitSubstitutionLanguage);
                Assert.Equal(StringDigitSubstitute.User, format.DigitSubstitutionMethod);
                Assert.Equal((StringFormatFlags)0, format.FormatFlags);
                Assert.Equal(HotkeyPrefix.None, format.HotkeyPrefix);
                Assert.Equal(StringAlignment.Near, format.LineAlignment);
                Assert.Equal(StringTrimming.Character, format.Trimming);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(StringFormatFlags.DirectionRightToLeft | StringFormatFlags.DirectionVertical)]
        [InlineData((StringFormatFlags)(-1))]
        public void Ctor_Options(StringFormatFlags options)
        {
            using (var format = new StringFormat(options))
            {
                Assert.Equal(StringAlignment.Near, format.Alignment);
                Assert.Equal(0, format.DigitSubstitutionLanguage);
                Assert.Equal(StringDigitSubstitute.User, format.DigitSubstitutionMethod);
                Assert.Equal(options, format.FormatFlags);
                Assert.Equal(HotkeyPrefix.None, format.HotkeyPrefix);
                Assert.Equal(StringAlignment.Near, format.LineAlignment);
                Assert.Equal(StringTrimming.Character, format.Trimming);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(StringFormatFlags.DirectionRightToLeft | StringFormatFlags.DirectionVertical, RandomLanguageCode)]
        [InlineData(StringFormatFlags.NoClip, EnglishLanguageCode)]
        [InlineData((StringFormatFlags)(-1), -1)]
        public void Ctor_Options_Language(StringFormatFlags options, int language)
        {
            using (var format = new StringFormat(options, language))
            {
                Assert.Equal(StringAlignment.Near, format.Alignment);
                Assert.Equal(0, format.DigitSubstitutionLanguage);
                Assert.Equal(StringDigitSubstitute.User, format.DigitSubstitutionMethod);
                Assert.Equal(options, format.FormatFlags);
                Assert.Equal(HotkeyPrefix.None, format.HotkeyPrefix);
                Assert.Equal(StringAlignment.Near, format.LineAlignment);
                Assert.Equal(StringTrimming.Character, format.Trimming);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Format()
        {
            using (var original = new StringFormat(StringFormatFlags.NoClip, EnglishLanguageCode))
            using (var format = new StringFormat(original))
            {
                Assert.Equal(StringAlignment.Near, format.Alignment);
                Assert.Equal(0, format.DigitSubstitutionLanguage);
                Assert.Equal(StringDigitSubstitute.User, format.DigitSubstitutionMethod);
                Assert.Equal(StringFormatFlags.NoClip, format.FormatFlags);
                Assert.Equal(HotkeyPrefix.None, format.HotkeyPrefix);
                Assert.Equal(StringAlignment.Near, format.LineAlignment);
                Assert.Equal(StringTrimming.Character, format.Trimming);

                // The new format is a clone.
                original.FormatFlags = StringFormatFlags.NoFontFallback;
                Assert.Equal(StringFormatFlags.NoClip, format.FormatFlags);
            }
        }

        [Fact]
        public void Ctor_NullFormat_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("format", () => new StringFormat(null));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_DisposedFormat_ThrowsArgumentException()
        {
            var format = new StringFormat();
            format.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => new StringFormat(format));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Dispose_MultipleTimes_Success()
        {
            var format = new StringFormat();
            format.Dispose();
            format.Dispose();
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clone_Valid_Success()
        {
            using (var original = new StringFormat(StringFormatFlags.NoClip, EnglishLanguageCode))
            using (StringFormat format = Assert.IsType<StringFormat>(original.Clone()))
            {
                Assert.Equal(StringAlignment.Near, format.Alignment);
                Assert.Equal(0, format.DigitSubstitutionLanguage);
                Assert.Equal(StringDigitSubstitute.User, format.DigitSubstitutionMethod);
                Assert.Equal(StringFormatFlags.NoClip, format.FormatFlags);
                Assert.Equal(HotkeyPrefix.None, format.HotkeyPrefix);
                Assert.Equal(StringAlignment.Near, format.LineAlignment);
                Assert.Equal(StringTrimming.Character, format.Trimming);

                // The new format is a clone.
                original.FormatFlags = StringFormatFlags.NoFontFallback;
                Assert.Equal(StringFormatFlags.NoClip, format.FormatFlags);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clone_Disposed_ThrowsArgumentException()
        {
            var format = new StringFormat();
            format.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => format.Clone());
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0, StringDigitSubstitute.None, 0)]
        [InlineData(EnglishLanguageCode, StringDigitSubstitute.Traditional, EnglishLanguageCode)]
        [InlineData(int.MaxValue, StringDigitSubstitute.Traditional + 1, 65535)]
        [InlineData(-1, StringDigitSubstitute.User - 1, 65535)]
        public void SetDigitSubstitution_Invoke_SetsProperties(int language, StringDigitSubstitute substitute, int expectedLanguage)
        {
            using (var format = new StringFormat())
            {
                format.SetDigitSubstitution(language, substitute);
                Assert.Equal(expectedLanguage, format.DigitSubstitutionLanguage);
                Assert.Equal(substitute, format.DigitSubstitutionMethod);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetDigitSubstitution_Disposed_ThrowsArgumentException()
        {
            var format = new StringFormat();
            format.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => format.SetDigitSubstitution(0, StringDigitSubstitute.None));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0, new float[0])]
        [InlineData(10, new float[] { 1, 2.3f, 4, float.PositiveInfinity, float.NaN })]
        public void SetTabStops_GetTabStops_ReturnsExpected(float firstTabOffset, float[] tabStops)
        {
            using (var format = new StringFormat())
            {
                format.SetTabStops(firstTabOffset, tabStops);

                Assert.Equal(tabStops, format.GetTabStops(out float actualFirstTabOffset));
                Assert.Equal(firstTabOffset, actualFirstTabOffset);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetTabStops_NullTabStops_ThrowsNullReferenceException()
        {
            using (var format = new StringFormat())
            {
                Assert.Throws<NullReferenceException>(() => format.SetTabStops(0, null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetTabStops_NegativeFirstTabOffset_ThrowsArgumentException()
        {
            using (var format = new StringFormat())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => format.SetTabStops(-1, new float[0]));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetTabStops_NegativeInfinityInTabStops_ThrowsNotImplementedException()
        {
            using (var format = new StringFormat())
            {
                Assert.Throws<NotImplementedException>(() => format.SetTabStops(0, new float[] { float.NegativeInfinity }));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetTabStops_Disposed_ThrowsArgumentException()
        {
            var format = new StringFormat();
            format.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => format.SetTabStops(0, new float[0]));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetTabStops_Disposed_ThrowsArgumentException()
        {
            var format = new StringFormat();
            format.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => format.GetTabStops(out float firstTabOffset));
        }

        public static IEnumerable<object[]> SetMeasurableCharacterRanges_TestData()
        {
            yield return new object[] { new CharacterRange[0] };
            yield return new object[] { new CharacterRange[] { new CharacterRange(1, 2) } };
            yield return new object[] { new CharacterRange[] { new CharacterRange(-1, -1) } };
            yield return new object[] { new CharacterRange[32] };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(SetMeasurableCharacterRanges_TestData))]
        public void SetMeasurableCharacterRanges_Valid_Success(CharacterRange[] ranges)
        {
            using (var format = new StringFormat())
            {
                format.SetMeasurableCharacterRanges(ranges);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetMeasurableCharacterRanges_NullRanges_ThrowsNullReferenceException()
        {
            using (var format = new StringFormat())
            {
                Assert.Throws<NullReferenceException>(() => format.SetMeasurableCharacterRanges(null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetMeasurableCharacterRanges_RangesTooLarge_ThrowsOverflowException()
        {
            using (var format = new StringFormat())
            {
                Assert.Throws<OverflowException>(() => format.SetMeasurableCharacterRanges(new CharacterRange[33]));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetMeasurableCharacterRanges_Disposed_ThrowsArgumentException()
        {
            var format = new StringFormat();
            format.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => format.SetMeasurableCharacterRanges(new CharacterRange[0]));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(StringAlignment.Center)]
        [InlineData(StringAlignment.Far)]
        [InlineData(StringAlignment.Near)]
        public void Alignment_SetValid_GetReturnsExpected(StringAlignment alignment)
        {
            using (var format = new StringFormat { Alignment = alignment })
            {
                Assert.Equal(alignment, format.Alignment);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(StringAlignment.Near - 1)]
        [InlineData(StringAlignment.Far + 1)]
        public void Alignment_SetInvalid_ThrowsInvalidEnumArgumentException(StringAlignment alignment)
        {
            using (var format = new StringFormat())
            {
                Assert.ThrowsAny<ArgumentException>(() => format.Alignment = alignment);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Alignment_GetSetWhenDisposed_ThrowsArgumentException()
        {
            var format = new StringFormat();
            format.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => format.Alignment);
            AssertExtensions.Throws<ArgumentException>(null, () => format.Alignment = StringAlignment.Center);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DigitSubstitutionMethod_GetSetWhenDisposed_ThrowsArgumentException()
        {
            var format = new StringFormat();
            format.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => format.DigitSubstitutionMethod);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DigitSubstitutionLanguage_GetSetWhenDisposed_ThrowsArgumentException()
        {
            var format = new StringFormat();
            format.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => format.DigitSubstitutionLanguage);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(StringFormatFlags.DirectionRightToLeft)]
        [InlineData((StringFormatFlags)int.MinValue)]
        [InlineData((StringFormatFlags)int.MaxValue)]
        public void FormatFlags_Set_GetReturnsExpected(StringFormatFlags formatFlags)
        {
            using (var format = new StringFormat { FormatFlags = formatFlags })
            {
                Assert.Equal(formatFlags, format.FormatFlags);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FormatFlags_GetSetWhenDisposed_ThrowsArgumentException()
        {
            var format = new StringFormat();
            format.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => format.FormatFlags);
            AssertExtensions.Throws<ArgumentException>(null, () => format.FormatFlags = StringFormatFlags.NoClip);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(StringAlignment.Center)]
        [InlineData(StringAlignment.Far)]
        [InlineData(StringAlignment.Near)]
        public void LineAlignment_SetValid_GetReturnsExpected(StringAlignment alignment)
        {
            using (var format = new StringFormat { LineAlignment = alignment })
            {
                Assert.Equal(alignment, format.LineAlignment);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(StringAlignment.Near - 1)]
        [InlineData(StringAlignment.Far + 1)]
        public void LineAlignment_SetInvalid_ThrowsInvalidEnumArgumentException(StringAlignment alignment)
        {
            using (var format = new StringFormat())
            {
                Assert.ThrowsAny<ArgumentException>(() => format.LineAlignment = alignment);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void LineAlignment_GetSetWhenDisposed_ThrowsArgumentException()
        {
            var format = new StringFormat();
            format.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => format.LineAlignment);
            AssertExtensions.Throws<ArgumentException>(null, () => format.LineAlignment = StringAlignment.Center);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(HotkeyPrefix.Hide)]
        [InlineData(HotkeyPrefix.None)]
        [InlineData(HotkeyPrefix.Show)]
        public void HotKeyPrefix_SetValid_GetReturnsExpected(HotkeyPrefix prefix)
        {
            using (var format = new StringFormat { HotkeyPrefix = prefix })
            {
                Assert.Equal(prefix, format.HotkeyPrefix);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(HotkeyPrefix.None - 1)]
        [InlineData(HotkeyPrefix.Hide + 1)]
        public void HotKeyPrefix_SetInvalid_ThrowsInvalidEnumArgumentException(HotkeyPrefix prefix)
        {
            using (var format = new StringFormat())
            {
                Assert.ThrowsAny<ArgumentException>(() => format.HotkeyPrefix = prefix);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void HotkeyPrefix_GetSetWhenDisposed_ThrowsArgumentException()
        {
            var format = new StringFormat();
            format.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => format.HotkeyPrefix);
            AssertExtensions.Throws<ArgumentException>(null, () => format.HotkeyPrefix = HotkeyPrefix.Hide);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(StringTrimming.Word)]
        public void Trimming_SetValid_GetReturnsExpected(StringTrimming trimming)
        {
            using (var format = new StringFormat { Trimming = trimming })
            {
                Assert.Equal(trimming, format.Trimming);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(StringTrimming.None - 1)]
        [InlineData(StringTrimming.EllipsisPath + 1)]
        public void Trimming_SetInvalid_ThrowsInvalidEnumArgumentException(StringTrimming trimming)
        {
            using (var format = new StringFormat())
            {
                Assert.ThrowsAny<ArgumentException>(() => format.Trimming = trimming);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Trimming_GetSetWhenDisposed_ThrowsArgumentException()
        {
            var format = new StringFormat();
            format.Dispose();


            AssertExtensions.Throws<ArgumentException>(null, () => format.Trimming);
            AssertExtensions.Throws<ArgumentException>(null, () => format.Trimming = StringTrimming.Word);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GenericDefault_Get_ReturnsExpected()
        {
            StringFormat format = StringFormat.GenericDefault;
            Assert.NotSame(format, StringFormat.GenericDefault);

            Assert.Equal(StringAlignment.Near, format.Alignment);
            Assert.Equal(0, format.DigitSubstitutionLanguage);
            Assert.Equal(StringDigitSubstitute.User, format.DigitSubstitutionMethod);
            Assert.Equal((StringFormatFlags)0, format.FormatFlags);
            Assert.Equal(HotkeyPrefix.None, format.HotkeyPrefix);
            Assert.Equal(StringAlignment.Near, format.LineAlignment);
            Assert.Equal(StringTrimming.Character, format.Trimming);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GenericTypographic_Get_ReturnsExpected()
        {
            StringFormat format = StringFormat.GenericTypographic;
            Assert.NotSame(format, StringFormat.GenericTypographic);

            Assert.Equal(StringAlignment.Near, format.Alignment);
            Assert.Equal(0, format.DigitSubstitutionLanguage);
            Assert.Equal(StringDigitSubstitute.User, format.DigitSubstitutionMethod);
            Assert.Equal(StringFormatFlags.FitBlackBox | StringFormatFlags.LineLimit | StringFormatFlags.NoClip, format.FormatFlags);
            Assert.Equal(HotkeyPrefix.None, format.HotkeyPrefix);
            Assert.Equal(StringAlignment.Near, format.LineAlignment);
            Assert.Equal(StringTrimming.None, format.Trimming);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ToString_Flags_ReturnsExpected()
        {
            using (var format = new StringFormat(StringFormatFlags.DirectionVertical))
            {
                Assert.Equal("[StringFormat, FormatFlags=DirectionVertical]", format.ToString());
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ToString_Disposed_ThrowsArgumentException()
        {
            var format = new StringFormat(StringFormatFlags.DirectionVertical);
            format.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => format.ToString());
        }
    }
}
