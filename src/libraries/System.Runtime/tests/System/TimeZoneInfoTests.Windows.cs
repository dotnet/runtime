// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Tests
{
    public static partial class TimeZoneInfoTests
    {
        private static readonly CultureInfo[] s_CulturesForWindowsNlsDisplayNamesTest = WindowsUILanguageHelper.GetInstalledWin32CulturesWithUniqueLanguages();
        private static bool CanTestWindowsNlsDisplayNames => RemoteExecutor.IsSupported && s_CulturesForWindowsNlsDisplayNamesTest.Length > 1;

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(nameof(CanTestWindowsNlsDisplayNames))]
        public static void TestWindowsNlsDisplayNames()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo[] cultures = s_CulturesForWindowsNlsDisplayNamesTest;

                CultureInfo.CurrentUICulture = cultures[0];
                TimeZoneInfo.ClearCachedData();
                TimeZoneInfo tz1 = TimeZoneInfo.FindSystemTimeZoneById(s_strPacific);

                CultureInfo.CurrentUICulture = cultures[1];
                TimeZoneInfo.ClearCachedData();
                TimeZoneInfo tz2 = TimeZoneInfo.FindSystemTimeZoneById(s_strPacific);

                Assert.True(tz1.DisplayName != tz2.DisplayName, $"The display name '{tz1.DisplayName}' should be different between {cultures[0].Name} and {cultures[1].Name}.");
                Assert.True(tz1.StandardName != tz2.StandardName, $"The standard name '{tz1.StandardName}' should be different between {cultures[0].Name} and {cultures[1].Name}.");
                Assert.True(tz1.DaylightName != tz2.DaylightName, $"The daylight name '{tz1.DaylightName}' should be different between {cultures[0].Name} and {cultures[1].Name}.");
            }).Dispose();
        }

        //  Function pointer types are not supported in PInvokes on browser.
        //
        //  This helper class is used to retrieve information about installed OS languages from Windows.
        //  Its methods returns empty when run on non-Windows platforms.
        private static class WindowsUILanguageHelper
        {
            public static CultureInfo[] GetInstalledWin32CulturesWithUniqueLanguages() =>
                GetInstalledWin32Cultures()
                    .GroupBy(c => c.TwoLetterISOLanguageName)
                    .Select(g => g.First())
                    .ToArray();

            public static unsafe CultureInfo[] GetInstalledWin32Cultures()
            {
                if (!OperatingSystem.IsWindows())
                {
                    return new CultureInfo[0];
                }

                var list = new List<CultureInfo>();
                GCHandle handle = GCHandle.Alloc(list);
                try
                {
                    EnumUILanguages(
                        &EnumUiLanguagesCallback,
                        MUI_ALL_INSTALLED_LANGUAGES | MUI_LANGUAGE_NAME,
                        GCHandle.ToIntPtr(handle));
                }
                finally
                {
                    handle.Free();
                }

                return list.ToArray();
            }

            [UnmanagedCallersOnly]
            private static unsafe int EnumUiLanguagesCallback(char* lpUiLanguageString, IntPtr lParam)
            {
                // native string is null terminated
                var cultureName = new string(lpUiLanguageString);

                string tzResourceFilePath = Path.Join(Environment.SystemDirectory, cultureName, "tzres.dll.mui");
                if (!File.Exists(tzResourceFilePath))
                {
                    // If Windows installed a UI language but did not include the time zone resources DLL for that language,
                    // then skip this language as .NET will not be able to get the localized resources for that language.
                    return 1;
                }

                try
                {
                    var handle = GCHandle.FromIntPtr(lParam);
                    var list = (List<CultureInfo>)handle.Target;
                    list!.Add(CultureInfo.GetCultureInfo(cultureName));
                    return 1;
                }
                catch
                {
                    return 0;
                }
            }

            private const uint MUI_LANGUAGE_NAME = 0x8;
            private const uint MUI_ALL_INSTALLED_LANGUAGES = 0x20;

            [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
            private static extern unsafe bool EnumUILanguages(delegate* unmanaged<char*, IntPtr, int> lpUILanguageEnumProc, uint dwFlags, IntPtr lParam);
        }
    }
}