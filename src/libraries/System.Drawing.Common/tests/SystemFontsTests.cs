// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Drawing.Tests
{
    public class SystemFontsTests
    {
        public static IEnumerable<object[]> SystemFonts_TestData()
        {
            yield return new object[] { (Func<Font>)(() => SystemFonts.CaptionFont) };
            yield return new object[] { (Func<Font>)(() => SystemFonts.IconTitleFont) };
            yield return new object[] { (Func<Font>)(() => SystemFonts.MenuFont) };
            yield return new object[] { (Func<Font>)(() => SystemFonts.MessageBoxFont) };
            yield return new object[] { (Func<Font>)(() => SystemFonts.SmallCaptionFont) };
            yield return new object[] { (Func<Font>)(() => SystemFonts.StatusFont) };
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(SystemFonts_TestData))]
        public void SystemFont_Get_ReturnsExpected(Func<Font> getFont)
        {
            using (Font font = getFont())
            using (Font otherFont = getFont())
            {
                Assert.NotNull(font);
                Assert.NotNull(otherFont);
                Assert.NotSame(font, otherFont);

                // Assert.Equal on a font will use the native handle to assert equality, which is not always guaranteed.
                Assert.Equal(font.Name, otherFont.Name);
            }
        }

        public static IEnumerable<object[]> SystemFonts_WindowsNames_TestData()
        {
            yield return Font(() => SystemFonts.CaptionFont, "CaptionFont", "Segoe UI");
            yield return Font(() => SystemFonts.IconTitleFont, "IconTitleFont", "Segoe UI");
            yield return Font(() => SystemFonts.MenuFont, "MenuFont", "Segoe UI");
            yield return Font(() => SystemFonts.MessageBoxFont, "MessageBoxFont", "Segoe UI");
            yield return Font(() => SystemFonts.SmallCaptionFont, "SmallCaptionFont", "Segoe UI");
            yield return Font(() => SystemFonts.StatusFont, "StatusFont", "Segoe UI");

            bool isArabic = (GetSystemDefaultLCID() & 0x3ff) == 0x0001;
            yield return Font(() => SystemFonts.DefaultFont, "DefaultFont", isArabic ? "Tahoma" : "Microsoft Sans Serif");

            bool isJapanese = (GetSystemDefaultLCID() & 0x3ff) == 0x0011;
            yield return Font(() => SystemFonts.DialogFont, "DialogFont", isJapanese ? "Microsoft Sans Serif" : "Tahoma");
        }

        public static object[] Font(Func<Font> getFont, string systemFontName, string windowsFontName) => new object[] { getFont, systemFontName, windowsFontName };

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(SystemFonts_WindowsNames_TestData))]
        public void SystemFont_Get_ReturnsExpected_WindowsNames(Func<Font> getFont, string systemFontName, string windowsFontName)
        {
            using (Font font = getFont())
            using (Font otherFont = getFont())
            using (Font fontFromName = SystemFonts.GetFontByName(systemFontName))
            {
                Assert.NotSame(font, otherFont);
                Assert.Equal(font, otherFont);
                Assert.Equal(font, fontFromName);

                Assert.Equal(systemFontName, font.SystemFontName);

                // Windows 8 updated some system fonts.
                if (!PlatformDetection.IsWindows7)
                {
                    Assert.Equal(windowsFontName, font.Name);
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("captionfont")]
        public void GetFontByName_NoSuchName_ReturnsNull(string systemFontName)
        {
            Assert.Null(SystemFonts.GetFontByName(systemFontName));
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetSystemDefaultLCID();
    }
}
