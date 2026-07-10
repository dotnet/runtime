// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Tests;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Globalization.Tests
{
    public class CurrentCultureTests
    {
        [Fact]
        public void CurrentCulture()
        {
            var newCulture = new CultureInfo(CultureInfo.CurrentCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
            using (new ThreadCultureChange(newCulture))
            {
                Assert.Equal(CultureInfo.CurrentCulture, newCulture);
            }

            if (PlatformDetection.IsNotBrowser)
            {
                newCulture = new CultureInfo("de-DE_phoneb");
                using (new ThreadCultureChange(newCulture))
                {
                    Assert.Equal(CultureInfo.CurrentCulture, newCulture);
                    Assert.Equal("de-DE_phoneb", newCulture.CompareInfo.Name);
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS)]
        public void CurrentCulture_Default_Not_Invariant()
        {
            // On OSX-like platforms, it should default to what the default system culture is 
            // set to.  Since we shouldn't assume en-US, we just test if it's not the invariant
            // culture.
            Assert.NotEqual(CultureInfo.CurrentCulture, CultureInfo.InvariantCulture);
            Assert.NotEqual(CultureInfo.CurrentUICulture, CultureInfo.InvariantCulture);
        }

        private static bool CurrentLocaleHasNumberFormatOverride =>
            PlatformDetection.IsAppleMobile &&
            (GetCurrentLocaleValue("decimalSeparator") != GetNamedLocaleValue(CultureInfo.CurrentCulture.Name, "decimalSeparator") ||
             GetCurrentLocaleValue("groupingSeparator") != GetNamedLocaleValue(CultureInfo.CurrentCulture.Name, "groupingSeparator"));

        [ConditionalFact(nameof(CurrentLocaleHasNumberFormatOverride))]
        [PlatformSpecific(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS)]
        public void CurrentCulture_NumberFormat_UsesCurrentLocale()
        {
            Assert.Equal(GetCurrentLocaleValue("decimalSeparator"), CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            Assert.Equal(GetCurrentLocaleValue("groupingSeparator"), CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS)]
        public void CultureWithoutUserOverrides_NumberFormat_UsesNamedLocale()
        {
            CultureInfo culture = new CultureInfo(CultureInfo.CurrentCulture.Name, useUserOverride: false);

            Assert.Equal(GetNamedLocaleValue(culture.Name, "decimalSeparator"), culture.NumberFormat.NumberDecimalSeparator);
            Assert.Equal(GetNamedLocaleValue(culture.Name, "groupingSeparator"), culture.NumberFormat.NumberGroupSeparator);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS)]
        public void CurrentNamedCulture_NumberFormat_UsesNamedLocale()
        {
            CultureInfo culture = new CultureInfo(CultureInfo.CurrentCulture.Name);

            Assert.Equal(GetNamedLocaleValue(culture.Name, "decimalSeparator"), culture.NumberFormat.NumberDecimalSeparator);
            Assert.Equal(GetNamedLocaleValue(culture.Name, "groupingSeparator"), culture.NumberFormat.NumberGroupSeparator);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS)]
        public void NonCurrentCulture_NumberFormat_UsesNamedLocale()
        {
            CultureInfo culture = new CultureInfo(CultureInfo.CurrentCulture.Name == "fr-FR" ? "en-US" : "fr-FR");

            Assert.Equal(GetNamedLocaleValue(culture.Name, "decimalSeparator"), culture.NumberFormat.NumberDecimalSeparator);
            Assert.Equal(GetNamedLocaleValue(culture.Name, "groupingSeparator"), culture.NumberFormat.NumberGroupSeparator);
        }

        private static string GetCurrentLocaleValue(string selectorName)
        {
            IntPtr localeClass = objc_getClass("NSLocale");
            IntPtr currentLocale = objc_msgSend(localeClass, sel_registerName("currentLocale"));

            return GetLocaleValue(currentLocale, selectorName);
        }

        private static string GetNamedLocaleValue(string localeName, string selectorName)
        {
            IntPtr localeNameUtf8 = Marshal.StringToCoTaskMemUTF8(localeName);
            try
            {
                IntPtr localeClass = objc_getClass("NSLocale");
                IntPtr stringClass = objc_getClass("NSString");
                IntPtr nativeLocaleName = objc_msgSend(stringClass, sel_registerName("stringWithUTF8String:"), localeNameUtf8);
                IntPtr locale = objc_msgSend(objc_msgSend(localeClass, sel_registerName("alloc")), sel_registerName("initWithLocaleIdentifier:"), nativeLocaleName);
                try
                {
                    return GetLocaleValue(locale, selectorName);
                }
                finally
                {
                    objc_msgSend(locale, sel_registerName("release"));
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(localeNameUtf8);
            }
        }

        private static string GetLocaleValue(IntPtr locale, string selectorName)
        {
            IntPtr value = objc_msgSend(locale, sel_registerName(selectorName));
            IntPtr utf8Value = objc_msgSend(value, sel_registerName("UTF8String"));

            return Marshal.PtrToStringUTF8(utf8Value)!;
        }

        [DllImport("/usr/lib/libobjc.A.dylib")]
        private static extern IntPtr objc_getClass(string name);

        [DllImport("/usr/lib/libobjc.A.dylib")]
        private static extern IntPtr sel_registerName(string selectorName);

        [DllImport("/usr/lib/libobjc.A.dylib")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr argument);

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX | TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS)]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.MacCatalyst | TestPlatforms.tvOS, "https://github.com/dotnet/runtime/issues/111501")]
        public void CurrentCulture_Default_Is_Specific()
        {
            // On OSX-like platforms, the current culture taken from default system culture should be specific.
            Assert.False(CultureInfo.CurrentCulture.IsNeutralCulture);
        }

        [Fact]
        public void CurrentCulture_Set_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => CultureInfo.CurrentCulture = null);
        }

        [Fact]
        public void CurrentUICulture()
        {
            var newUICulture = new CultureInfo(CultureInfo.CurrentUICulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
            using (new ThreadCultureChange(null, newUICulture))
            {
                Assert.Equal(CultureInfo.CurrentUICulture, newUICulture);
            }

            if (PlatformDetection.IsNotBrowser)
            {
                newUICulture = new CultureInfo("de-DE_phoneb");
                using (new ThreadCultureChange(null, newUICulture))
                {
                    Assert.Equal(CultureInfo.CurrentUICulture, newUICulture);
                    Assert.Equal("de-DE_phoneb", newUICulture.CompareInfo.Name);
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DefaultThreadCurrentCulture()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo newCulture = new CultureInfo(CultureInfo.DefaultThreadCurrentCulture == null || CultureInfo.DefaultThreadCurrentCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
                CultureInfo.DefaultThreadCurrentCulture = newCulture;

                Task task = Task.Run(() =>
                {
                    Assert.Equal(CultureInfo.CurrentCulture, newCulture);
                });
                ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
                task.Wait();
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DefaultThreadCurrentUICulture()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo newUICulture = new CultureInfo(CultureInfo.DefaultThreadCurrentUICulture == null || CultureInfo.DefaultThreadCurrentUICulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
                CultureInfo.DefaultThreadCurrentUICulture = newUICulture;

                Task task = Task.Run(() =>
                {
                    Assert.Equal(CultureInfo.CurrentUICulture, newUICulture);
                });
                ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
                task.Wait();
            }).Dispose();
        }

        [Fact]
        public void CurrentUICulture_Set_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => CultureInfo.CurrentUICulture = null);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Windows locale support doesn't rely on LANG variable
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "Bionic is not normal Linux, has no normal locales")]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("en-US.UTF-8", "en-US")]
        [InlineData("en-US", "en-US")]
        [InlineData("en_GB", "en-GB")]
        [InlineData("fr-FR", "fr-FR")]
        [InlineData("ru", "ru")]
        public void CurrentCulture_BasedOnLangEnvVar(string langEnvVar, string expectedCultureName)
        {
            var psi = new ProcessStartInfo();
            TestEnvironment.ClearGlobalizationEnvironmentVars(psi.Environment);

            psi.Environment["LANG"] = langEnvVar;

            RemoteExecutor.Invoke(expected =>
            {
                Assert.NotNull(CultureInfo.CurrentCulture);
                Assert.NotNull(CultureInfo.CurrentUICulture);

                Assert.Equal(expected, CultureInfo.CurrentCulture.Name);
                Assert.Equal(expected, CultureInfo.CurrentUICulture.Name);
            }, expectedCultureName, new RemoteInvokeOptions { StartInfo = psi }).Dispose();
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "Remote executor has problems with exit codes")]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("")]
        [InlineData(null)]
        public void CurrentCulture_DefaultWithNoLang(string? langEnvVar)
        {
            var psi = new ProcessStartInfo();
            TestEnvironment.ClearGlobalizationEnvironmentVars(psi.Environment);

            if (langEnvVar != null)
            {
               psi.Environment["LANG"] = langEnvVar;
            }

            // When LANG is empty or unset, on Unix it should default to the invariant culture.
            // On OSX-like platforms, it should default to what the default system culture is 
            // set to.  Since we shouldn't assume en-US, we just test if it's not the invariant
            // culture.
            RemoteExecutor.Invoke(() =>
            {
                Assert.NotNull(CultureInfo.CurrentCulture);
                Assert.NotNull(CultureInfo.CurrentUICulture);

                if (PlatformDetection.IsApplePlatform)
                {
                    Assert.NotEqual("", CultureInfo.CurrentCulture.Name);
                    Assert.NotEqual("", CultureInfo.CurrentUICulture.Name);

                    Assert.NotEqual(CultureInfo.CurrentCulture, CultureInfo.InvariantCulture);
                    Assert.NotEqual(CultureInfo.CurrentUICulture, CultureInfo.InvariantCulture);
                }
                else
                {
                    Assert.Equal("", CultureInfo.CurrentCulture.Name);
                    Assert.Equal("", CultureInfo.CurrentUICulture.Name);
                }
            }, new RemoteInvokeOptions { StartInfo = psi }).Dispose();
        }
    }
}
