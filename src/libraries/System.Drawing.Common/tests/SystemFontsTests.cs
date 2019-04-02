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

        [ConditionalTheory(Helpers.IsDrawingSupported)]
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
            int userLangId = GetUserDefaultLCID();
            SystemFontList fonts;

            switch (userLangId & 0x3ff)
            {
                case 0x11: // ja-JP (Japanese)
                    fonts = new SystemFontList("Yu Gothic UI");
                    break;
                case 0x5C: // chr-Cher-US (Cherokee)
                    fonts = new SystemFontList("Gadugi");
                    break;
                case 0x12: // ko-KR (Korean)
                    fonts = new SystemFontList("\ub9d1\uc740\x20\uace0\ub515");
                    break;
                case 0x4: // zh-TW (Traditional Chinese, Taiwan) or zh-CN (Simplified Chinese, PRC)
                    // Although the primary language ID is the same, the fonts are different
                    // So we have to determine by the full language ID
                    // https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/70feba9f-294e-491e-b6eb-56532684c37f
                    // Assuming this doc is correct AND the font only differs by whether it's traditional or not it should work
                    switch (userLangId & 0xFFFF)
                    {
                        case 0x0004: // zh-Hans
                        case 0x7804: // zh
                        case 0x0804: // zh-CN
                        case 0x1004: // zh-SG
                            fonts = new SystemFontList("Microsoft JhengHei UI");
                            break;
                        case 0x7C04: // zh-Hant
                        case 0x0C04: // zh-HK
                        case 0x1404: // zh-MO
                        case 0x0404: // zh-TW
                            fonts = new SystemFontList("Microsoft YaHei UI");
                            break;
                        default:
                            throw new InvalidOperationException("The primary language ID is Chinese, however it was not able to" +
                                                                $" determine the user locale from the LCID with value: {userLangId & 0xFFFF:X4}.");
                    }
                    break;
                case 0x1E: // th-TH
                case 0x54: // lo-LA
                case 0x53: // km-KH 
                    fonts = new SystemFontList("Leelawadee UI");
                    break;
                case 0x4A: // te-IN
                case 0x49: // ta-IN
                case 0x5B: // si-LK
                case 0x48: // or-IN
                case 0x4E: // mr-IN
                case 0x4C: // ml-IN
                case 0x57: // kok-IN
                case 0x45: // bn-BD
                case 0x4D: // as-IN
                    fonts = new SystemFontList("Nirmala UI");
                    break;
                case 0x5E: // am-ET
                    fonts = new SystemFontList("Ebrima");
                    break;
                default: // For now we assume everything else uses Segoe UI
                    // If there's other failure reported we can add it
                    fonts = new SystemFontList("Segoe UI");
                    break;
            }

            return fonts.ToTestData();
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
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

        [DllImport("kernel32.dll", SetLastError = false, CharSet = CharSet.Auto)]
        public static extern int GetUserDefaultLCID();

        // Do not test DefaultFont and DialogFont, as we can't reliably determine from LCID
        // https://github.com/dotnet/corefx/issues/35664#issuecomment-473556522
        class SystemFontList
        {
            public SystemFontList(string c_it_m_mb_scFonts)
            {
                CaptionFont = c_it_m_mb_scFonts;
                IconTitleFont = c_it_m_mb_scFonts;
                MenuFont = c_it_m_mb_scFonts;
                MessageBoxFont = c_it_m_mb_scFonts;
                SmallCaptionFont = c_it_m_mb_scFonts;
                StatusFont = c_it_m_mb_scFonts;
            }

            public string CaptionFont { get; set; }
            public string IconTitleFont { get; set; }
            public string MenuFont { get; set; }
            public string MessageBoxFont { get; set; }
            public string SmallCaptionFont { get; set; }
            public string StatusFont { get; set; }

            public IEnumerable<object[]> ToTestData()
            {
                return new []
                {
                new object[] {(Func<Font>)(() => SystemFonts.CaptionFont), nameof(CaptionFont), CaptionFont},
                new object[] {(Func<Font>)(() => SystemFonts.IconTitleFont), nameof(IconTitleFont), IconTitleFont},
                new object[] {(Func<Font>)(() => SystemFonts.MenuFont), nameof(MenuFont), MenuFont},
                new object[] {(Func<Font>)(() => SystemFonts.MessageBoxFont), nameof(MessageBoxFont), MessageBoxFont},
                new object[] {(Func<Font>)(() => SystemFonts.SmallCaptionFont), nameof(SmallCaptionFont), SmallCaptionFont},
                new object[] {(Func<Font>)(() => SystemFonts.StatusFont), nameof(StatusFont), StatusFont}
                };
            }
        }
    }
}
