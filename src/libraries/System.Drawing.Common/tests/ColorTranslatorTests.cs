// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Tests;
using System.Threading;
using Xunit;

namespace System.Drawing.Tests
{
    public class ColorTranslatorTests
    {
        public static IEnumerable<(int, Color)> SystemColors_TestData()
        {
            yield return (unchecked((int)0x8000000A), SystemColors.ActiveBorder);
            yield return (unchecked((int)0x80000002), SystemColors.ActiveCaption);
            yield return (unchecked((int)0x80000009), SystemColors.ActiveCaptionText);
            yield return (unchecked((int)0x8000000C), SystemColors.AppWorkspace);
            yield return (unchecked((int)0x8000000F), SystemColors.Control);
            yield return (unchecked((int)0x80000010), SystemColors.ControlDark);
            yield return (unchecked((int)0x80000015), SystemColors.ControlDarkDark);
            yield return (unchecked((int)0x80000016), SystemColors.ControlLight);
            yield return (unchecked((int)0x80000014), SystemColors.ControlLightLight);
            yield return (unchecked((int)0x80000012), SystemColors.ControlText);
            yield return (unchecked((int)0x80000001), SystemColors.Desktop);
            yield return (unchecked((int)0x8000001B), SystemColors.GradientActiveCaption);
            yield return (unchecked((int)0x8000001C), SystemColors.GradientInactiveCaption);
            yield return (unchecked((int)0x80000011), SystemColors.GrayText);
            yield return (unchecked((int)0x8000000D), SystemColors.Highlight);
            yield return (unchecked((int)0x8000000E), SystemColors.HighlightText);
            yield return (unchecked((int)0x8000001A), SystemColors.HotTrack);
            yield return (unchecked((int)0x8000000B), SystemColors.InactiveBorder);
            yield return (unchecked((int)0x80000003), SystemColors.InactiveCaption);
            yield return (unchecked((int)0x80000013), SystemColors.InactiveCaptionText);
            yield return (unchecked((int)0x80000018), SystemColors.Info);
            yield return (unchecked((int)0x80000017), SystemColors.InfoText);
            yield return (unchecked((int)0x80000004), SystemColors.Menu);
            yield return (unchecked((int)0x8000001E), SystemColors.MenuBar);
            yield return (unchecked((int)0x8000001D), SystemColors.MenuHighlight);
            yield return (unchecked((int)0x80000007), SystemColors.MenuText);
            yield return (unchecked((int)0x80000000), SystemColors.ScrollBar);
            yield return (unchecked((int)0x80000005), SystemColors.Window);
            yield return (unchecked((int)0x80000006), SystemColors.WindowFrame);
            yield return (unchecked((int)0x80000008), SystemColors.WindowText);
        }

        public static IEnumerable<object[]> ToWin32Color_TestData()
        {
            yield return new object[] { new Color(), 0 };
            yield return new object[] { Color.Red, 255 };
            yield return new object[] { Color.White, 16777215 };
            yield return new object[] { Color.FromArgb(1, 2, 3), 197121 };
        }

        [Theory]
        [MemberData(nameof(ToWin32Color_TestData))]
        public void ToWin32Color_Color_ReturnsExpected(Color color, int expected)
        {
            Assert.Equal(expected, ColorTranslator.ToWin32(color));
        }

        public static IEnumerable<object[]> FromOle_TestData()
        {
            yield return new object[] { int.MinValue, SystemColors.ScrollBar };
            yield return new object[] { -1, Color.White };
            yield return new object[] { 0, Color.Black };
            yield return new object[] { 197121, Color.FromArgb(1, 2, 3) };
            yield return new object[] { 16777215, Color.White };
            yield return new object[] { int.MaxValue, Color.White };
            yield return new object[] { unchecked((int)0x8000001F), Color.FromArgb(255, 31, 0, 0) };
            yield return new object[] { unchecked((int)0x80000019), Color.FromArgb(255, 25, 0, 0) };

            foreach ((int oleColor, Color color) in SystemColors_TestData())
            {
                yield return new object[] { oleColor, color };
            }
        }

        [Theory]
        [MemberData(nameof(FromOle_TestData))]
        public void FromOle_Color_ReturnsExpected(int oleColor, Color color)
        {
            Assert.Equal(color, ColorTranslator.FromOle(oleColor));
            Assert.Equal(color, ColorTranslator.FromWin32(oleColor));
        }

        public static IEnumerable<object[]> ToOle_TestData()
        {
            yield return new object[] { new Color(), 0 };
            yield return new object[] { Color.Red, 255 };
            yield return new object[] { Color.White, 16777215 };
            yield return new object[] { Color.FromArgb(1, 2, 3), 197121 };

            foreach ((int oleColor, Color color) in SystemColors_TestData())
            {
                yield return new object[] { color, oleColor };
            }


            // These system colors are equivalent to Control, ControlLight and ControlDark.
            yield return new object[] { SystemColors.ButtonFace, unchecked((int)0x8000000F) };
            yield return new object[] { SystemColors.ButtonHighlight, unchecked((int)0x80000014) };
            yield return new object[] { SystemColors.ButtonShadow, unchecked((int)0x80000010) };
        }

        [Theory]
        [MemberData(nameof(ToOle_TestData))]
        public void ToOle_Color_ReturnsExpected(Color color, int oleColor)
        {
            Assert.Equal(oleColor, ColorTranslator.ToOle(color));
        }

        public static IEnumerable<(string, Color)> HtmlColors_TestData()
        {
            yield return ("activeborder", SystemColors.ActiveBorder);
            yield return ("activecaption", SystemColors.ActiveCaption);
            yield return ("appworkspace", SystemColors.AppWorkspace);
            yield return ("background", SystemColors.Desktop);
            yield return ("buttonface", SystemColors.Control);
            yield return ("buttonhighlight", SystemColors.ControlLightLight);
            yield return ("buttonshadow", SystemColors.ControlDark);
            yield return ("buttontext", SystemColors.ControlText);
            yield return ("captiontext", SystemColors.ActiveCaptionText);
            yield return ("graytext", SystemColors.GrayText);
            yield return ("highlight", SystemColors.Highlight);
            yield return ("highlighttext", SystemColors.HighlightText);
            yield return ("inactiveborder", SystemColors.InactiveBorder);
            yield return ("inactivecaption", SystemColors.InactiveCaption);
            yield return ("inactivecaptiontext", SystemColors.InactiveCaptionText);
            yield return ("infobackground", SystemColors.Info);
            yield return ("infotext", SystemColors.InfoText);
            yield return ("menu", SystemColors.Menu);
            yield return ("menutext", SystemColors.MenuText);
            yield return ("scrollbar", SystemColors.ScrollBar);
            yield return ("window", SystemColors.Window);
            yield return ("windowframe", SystemColors.WindowFrame);
            yield return ("windowtext", SystemColors.WindowText);
            yield return ("threeddarkshadow", SystemColors.ControlDarkDark);

            yield return ("LightGrey", Color.LightGray);
            yield return ("Blue", Color.Blue);
            yield return ("#1F2E3D", Color.FromArgb(31, 46, 61));
        }

        public static IEnumerable<object[]> FromHtml_TestData()
        {
            yield return new object[] { null, Color.Empty };
            yield return new object[] { "", Color.Empty };
            yield return new object[] { "  ", Color.Empty };
            yield return new object[] { "''", Color.FromName("") };
            yield return new object[] { "\"\"", Color.FromName("") };

            yield return new object[] { "#1B3", Color.FromArgb(17, 187, 51) };
            yield return new object[] { "  #1F2E3D  ", Color.FromArgb(31, 46, 61) };

            yield return new object[] { "ActiveBorder", SystemColors.ActiveBorder };
            yield return new object[] { "ACTIVEBORDER", SystemColors.ActiveBorder };
            yield return new object[] { "  Blue  ", Color.Blue };
            yield return new object[] { "'Blue'", Color.Blue };
            yield return new object[] { "\"Blue\"", Color.Blue };
            yield return new object[] { "'None'", Color.FromName("None") };
            yield return new object[] { "\"None\"", Color.FromName("None") };
            yield return new object[] { "255,0,0", Color.Red };

            // Color(argb)
            yield return new object[] { 498, Color.FromArgb(0, 0, 1, 242) };
            yield return new object[] { "&h1F2", Color.FromArgb(0, 0, 1, 242) };
            yield return new object[] { "&h1F2", Color.FromArgb(0, 0, 1, 242) };

            // Color(red, green, blue)
            yield return new object[] { "1, 0x2, &h3", Color.FromArgb(1, 2, 3) };

            // Color(alpha, red, green, blue)
            yield return new object[] { "1, 2, 0x3, &h4", Color.FromArgb(1, 2, 3, 4) };

            foreach ((string htmlColor, Color color) in HtmlColors_TestData())
            {
                yield return new object[] { htmlColor, color };
            }

            // Some of the SystemColors.Control colors don't roundtrip.
            yield return new object[] { "threedface", SystemColors.Control };
            yield return new object[] { "threedhighlight", SystemColors.ControlLight };
            yield return new object[] { "threedlightshadow", SystemColors.ControlLightLight };
        }

        [Theory]
        [MemberData(nameof(FromHtml_TestData))]
        public void FromHtml_String_ReturnsExpected(string htmlColor, Color expected)
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture, CultureInfo.InvariantCulture))
            {
                Assert.Equal(expected, ColorTranslator.FromHtml(htmlColor));
            }
        }

        [Theory]
        [InlineData("'")]
        [InlineData("'\"")]
        [InlineData("\"'")]
        [InlineData("#")]
        [InlineData("  #G12  ")]
        [InlineData("  #G12345  ")]
        [InlineData("#FFFFFFFFF")]
        [InlineData("0x")]
        [InlineData("0xFFFFFFFFF")]
        [InlineData("0xG12")]
        [InlineData("&h")]
        [InlineData("&hG12")]
        public void FromHtml_Invalid_Throws(string htmlColor)
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture, CultureInfo.InvariantCulture))
            {
                Exception exception = AssertExtensions.Throws<ArgumentException, Exception>(() => ColorTranslator.FromHtml(htmlColor));
                if (exception is ArgumentException argumentException)
                    Assert.Equal("htmlColor", argumentException.ParamName);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData("#G12", typeof(FormatException))]
        [InlineData("#G12345", typeof(FormatException))]
        [InlineData("1,2", typeof(ArgumentException))]
        [InlineData("1,2,3,4,5", typeof(ArgumentException))]
        [InlineData("-1,2,3", typeof(ArgumentException))]
        [InlineData("256,2,3", typeof(ArgumentException))]
        [InlineData("1,-1,3", typeof(ArgumentException))]
        [InlineData("1,256,3", typeof(ArgumentException))]
        [InlineData("1,2,-1", typeof(ArgumentException))]
        [InlineData("1,2,256", typeof(ArgumentException))]
        public void FromHtml_Invalid_Throw(string htmlColor, Type exception)
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture, CultureInfo.InvariantCulture))
            {
                Assert.Throws(exception, () => ColorTranslator.FromHtml(htmlColor));
            }
        }

        public static IEnumerable<object[]> ToHtml_TestData()
        {
            yield return new object[] { Color.Empty, "" };

            foreach ((string htmlColor, Color color) in HtmlColors_TestData())
            {
                yield return new object[] { color, htmlColor };
            }

            // SystemColors.ControlLight don't roundtrip.
            yield return new object[] { SystemColors.ControlLight, "buttonface" };
            yield return new object[] { SystemColors.GradientActiveCaption, "activecaption" };
            yield return new object[] { SystemColors.HotTrack, "highlight" };
            yield return new object[] { SystemColors.MenuHighlight, "highlighttext" };
            yield return new object[] { SystemColors.GradientInactiveCaption, "inactivecaption" };
            yield return new object[] { SystemColors.MenuBar, "menu" };
            yield return new object[] { SystemColors.ButtonShadow, "" };
        }

        [Theory]
        [MemberData(nameof(ToHtml_TestData))]
        public void ToHtml_Color_ReturnsExpected(Color color, string expected)
        {
            Assert.Equal(expected, ColorTranslator.ToHtml(color));
        }
    }
}
