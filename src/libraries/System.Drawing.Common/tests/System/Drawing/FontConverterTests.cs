// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using Xunit;
using static System.Drawing.FontConverter;

namespace System.ComponentModel.TypeConverterTests
{
    public class FontNameConverterTest
    {
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TestConvertFrom()
        {
            FontConverter.FontNameConverter converter = new FontConverter.FontNameConverter();
            // returns "Times" under Linux and "Times New Roman" under Windows
            if (PlatformDetection.IsWindows)
            {
                Assert.Equal("Times New Roman", converter.ConvertFrom("Times") as string);
            }
            else
            {
                Assert.Equal("Times", converter.ConvertFrom("Times") as string);
            }
            Assert.True(converter.GetStandardValuesSupported(), "standard values supported");
            Assert.False(converter.GetStandardValuesExclusive(), "standard values exclusive");
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ExTestConvertFrom_ThrowsNotSupportedException()
        {
            FontConverter.FontNameConverter converter = new FontConverter.FontNameConverter();
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(null));
            Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(1));
        }
    }

    public class FontConverterTest
    {
        public static char s_Separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator[0];

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(TestConvertFormData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full Framework doesn't support inputs without units")]
        public void TestConvertFrom(string input, string expectedName, float expectedSize, GraphicsUnit expectedUnits, FontStyle expectedFontStyle)
        {
            FontConverter converter = new FontConverter();
            Font font = (Font)converter.ConvertFrom(input);

            // Unix fonts
            Assert.Equal(expectedName, font.Name);
            Assert.Equal(expectedSize, font.Size);
            Assert.Equal(expectedUnits, font.Unit);
            Assert.Equal(expectedFontStyle, font.Style);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ArgumentExceptionFontConverterData))]
        public void InvalidInputThrowsArgumentException(string input, string paramName, string netfxParamName)
        {
            FontConverter converter = new FontConverter();
            AssertExtensions.Throws<ArgumentException>(paramName, netfxParamName, () => converter.ConvertFrom(input));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(InvalidEnumArgumentExceptionFontConverterData))]
        public void InvalidInputThrowsInvalidEnumArgumentException(string input, string paramName)
        {
            FontConverter converter = new FontConverter();
            Assert.Throws<InvalidEnumArgumentException>(paramName, () => converter.ConvertFrom(input));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void EmptyStringInput()
        {
            FontConverter converter = new FontConverter();
            Font font = (Font)converter.ConvertFrom(string.Empty);
            Assert.Null(font);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDrawingSupported), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        public void GetFontPropsSorted()
        {
            // The order provided since .NET Framework
            string[] expectedPropNames = new[]
            {
                nameof(Font.Name),
                nameof(Font.Size),
                nameof(Font.Unit),
                nameof(Font.Bold),
                nameof(Font.GdiCharSet),
                nameof(Font.GdiVerticalFont),
                nameof(Font.Italic),
                nameof(Font.Strikeout),
                nameof(Font.Underline),
            };

            FontConverter converter = new FontConverter();
            Font font = new($"Courier New", 8.25f, FontStyle.Regular, GraphicsUnit.Point);

            PropertyDescriptorCollection props = converter.GetProperties(font);
            string[] propNames = new string[props.Count];

            int index = 0;
            foreach (PropertyDescriptor prop in props)
            {
                propNames[index++] = prop.DisplayName;
            }

            Assert.True(propNames.SequenceEqual(expectedPropNames));
        }

        public static TheoryData<string, string, float, GraphicsUnit, FontStyle> TestConvertFormData()
        {
            var data =
                new TheoryData<string, string, float, GraphicsUnit, FontStyle>()
                {
                    { $"Courier New", "Courier New", 8.25f, GraphicsUnit.Point, FontStyle.Regular },
                    { $"Courier New{s_Separator} 11", "Courier New", 11f, GraphicsUnit.Point, FontStyle.Regular },
                    { $"Arial{s_Separator} 11px", "Arial", 11f, GraphicsUnit.Pixel, FontStyle.Regular },
                    { $"Courier New{s_Separator} 11 px", "Courier New", 11f, GraphicsUnit.Pixel, FontStyle.Regular },
                    { $"Courier New{s_Separator} 11 px{s_Separator} style=Regular", "Courier New", 11f, GraphicsUnit.Pixel, FontStyle.Regular },
                    { $"Courier New{s_Separator} style=Bold", "Courier New", 8.25f, GraphicsUnit.Point, FontStyle.Bold },
                    { $"Courier New{s_Separator} 11 px{s_Separator} style=Bold{s_Separator} Italic", "Courier New", 11f, GraphicsUnit.Pixel, FontStyle.Bold | FontStyle.Italic },
                    { $"Courier New{s_Separator} 11 px{s_Separator} style=Regular, Italic", "Courier New", 11f, GraphicsUnit.Pixel, FontStyle.Regular | FontStyle.Italic },
                    { $"Courier New{s_Separator} 11 px{s_Separator} style=Bold{s_Separator} Italic{s_Separator} Strikeout", "Courier New", 11f, GraphicsUnit.Pixel, FontStyle.Bold | FontStyle.Italic | FontStyle.Strikeout },
                    { $"Arial{s_Separator} 11 px{s_Separator} style=Bold, Italic, Strikeout", "Arial", 11f, GraphicsUnit.Pixel, FontStyle.Bold | FontStyle.Italic | FontStyle.Strikeout },
                    { $"11px", "Microsoft Sans Serif", 8.25f, GraphicsUnit.Point, FontStyle.Regular },
                    { $"Style=Bold", "Microsoft Sans Serif", 8.25f, GraphicsUnit.Point, FontStyle.Regular },
                    { $"arIAL{s_Separator} 10{s_Separator} style=bold", "Arial", 10f, GraphicsUnit.Point, FontStyle.Bold },
                    { $"Arial{s_Separator} 10{s_Separator}", "Arial", 10f, GraphicsUnit.Point, FontStyle.Regular },
                    { $"Arial{s_Separator}", "Arial", 8.25f, GraphicsUnit.Point, FontStyle.Regular },
                    { $"Arial{s_Separator} 10{s_Separator} style=12", "Arial", 10f, GraphicsUnit.Point, FontStyle.Underline | FontStyle.Strikeout },
                    { $"Courier New{s_Separator} Style=Bold", "Courier New", 8.25f, GraphicsUnit.Point, FontStyle.Bold }, // FullFramework style keyword is case sensitive.
                    { $"11px{s_Separator} Style=Bold", "Microsoft Sans Serif", 8.25f, GraphicsUnit.Point, FontStyle.Bold}
                };

            // FullFramework disregards all arguments if the font name is an empty string.
            // Empty string is not an installed font on Windows 7, windows 8 and some versions of windows 10.
            if (EmptyFontPresent)
            {
                data.Add($"{s_Separator} 10{s_Separator} style=bold", "", 10f, GraphicsUnit.Point, FontStyle.Bold);
            }
            else
            {
                data.Add($"{s_Separator} 10{s_Separator} style=bold", "Microsoft Sans Serif", 10f, GraphicsUnit.Point, FontStyle.Bold);
            }

            return data;
        }

        private static bool EmptyFontPresent
        {
            get
            {
                using (var installedFonts = new InstalledFontCollection())
                {
                    return installedFonts.Families.Select(t => t.Name).Contains(string.Empty);
                }
            }
        }

        public static TheoryData<string, string, string> ArgumentExceptionFontConverterData() => new TheoryData<string, string, string>()
        {
            { $"Courier New{s_Separator} 11 px{s_Separator} type=Bold{s_Separator} Italic", "units", null },
            { $"Courier New{s_Separator} {s_Separator} Style=Bold", "value", null },
            { $"Courier New{s_Separator} 11{s_Separator} Style=", "value", null },
            { $"Courier New{s_Separator} 11{s_Separator} Style=RandomEnum", null, null },
            { $"Arial{s_Separator} 10{s_Separator} style=bold{s_Separator}", "value", null },
            { $"Arial{s_Separator} 10{s_Separator} style=null", null, null },
            { $"Arial{s_Separator} 10{s_Separator} style=abc#", null, null },
            { $"Arial{s_Separator} 10{s_Separator} style=##", null, null },
            { $"Arial{s_Separator} 10display{s_Separator} style=bold", null, null },
            { $"Arial{s_Separator} 10style{s_Separator} style=bold", "units", null },
        };

        public static TheoryData<string, string> InvalidEnumArgumentExceptionFontConverterData() => new TheoryData<string, string>()
        {
            { $"Arial{s_Separator} 10{s_Separator} style=56", "style" },
            { $"Arial{s_Separator} 10{s_Separator} style=-1", "style" },
        };
    }

    public class FontUnitConverterTest
    {
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetStandardValuesTest()
        {
            FontUnitConverter converter = new FontUnitConverter();
            var values = converter.GetStandardValues();
            Assert.Equal(6, values.Count); // The six supported values of Graphics unit: World, Pixel, Point, Inch, Document, Millimeter.

            foreach (var item in values)
            {
                Assert.NotEqual(GraphicsUnit.Display, (GraphicsUnit)item);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData("Display", GraphicsUnit.Display)]
        [InlineData("Document", GraphicsUnit.Document)]
        [InlineData("Inch", GraphicsUnit.Inch)]
        [InlineData("Millimeter", GraphicsUnit.Millimeter)]
        [InlineData("Pixel", GraphicsUnit.Pixel)]
        [InlineData("Point", GraphicsUnit.Point)]
        [InlineData("World", GraphicsUnit.World)]
        public void CanConvertFrom(string input, GraphicsUnit expected)
        {
            FontUnitConverter converter = new FontUnitConverter();
            GraphicsUnit value = (GraphicsUnit)converter.ConvertFrom(input);
            Assert.Equal(expected, value);
        }
    }
}
