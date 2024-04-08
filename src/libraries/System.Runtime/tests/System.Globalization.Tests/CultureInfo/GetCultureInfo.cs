// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.RemoteExecutor;
using System.Diagnostics;
using System.Collections.Concurrent;
using Xunit;

namespace System.Globalization.Tests
{
    public class GetCultureInfoTests
    {
        public static bool PlatformSupportsFakeCulture => (!PlatformDetection.IsWindows || PlatformDetection.WindowsVersion >= 10) && PlatformDetection.IsNotBrowser;
        public static bool PlatformSupportsFakeCultureAndRemoteExecutor => PlatformSupportsFakeCulture && RemoteExecutor.IsSupported;

        public static IEnumerable<object[]> GetCultureInfoTestData()
        {
            yield return new object[] { "en" };
            yield return new object[] { "en-US" };
            yield return new object[] { "ja-JP" };
            yield return new object[] { "ar-SA" };
            yield return new object[] { "xx-XX" };
            if (PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
            {
                yield return new object[] { "de-AT-1901" };
            }
            yield return new object[] { "zh-Hans" };
            yield return new object[] { "zh-Hans-HK" };
            yield return new object[] { "zh-Hans-MO" };
            yield return new object[] { "zh-Hans-TW" };
            yield return new object[] { "zh-Hant" };
            yield return new object[] { "zh-Hant-CN" };
            yield return new object[] { "zh-Hant-SG" };

            if (PlatformDetection.IsIcuGlobalization || PlatformDetection.IsHybridGlobalizationOnApplePlatform)
            {
                if (PlatformDetection.IsNotWindows)
                {
                    yield return new object[] { "x\u0000X-Yy", "x" }; // Null byte
                    if (PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
                    {
                        yield return new object[] { "zh-cmn", "zh-CMN" };
                        yield return new object[] { "zh-CMN-HANS" };
                        yield return new object[] { "zh-cmn-Hant", "zh-CMN-HANT" };
                    }
                }
                if (PlatformDetection.IsNotHybridGlobalizationOnApplePlatform)
                {
                    yield return new object[] { "sgn-BE-FR" };
                }
                yield return new object[] { "zh-min-nan", "nan" };
                yield return new object[] { "zh-gan", "gan" };
                yield return new object[] { "zh-Hans-CN" };
                yield return new object[] { "zh-Hans-SG" };
                yield return new object[] { "zh-Hant-HK" };
                yield return new object[] { "zh-Hant-MO" };
                yield return new object[] { "zh-Hant-TW" };
                yield return new object[] { "zh-yue", "yue" };
                yield return new object[] { "zh-wuu", "wuu" };
            }
            else
            {
                yield return new object[] { "sgn-BE-FR", "sgn-BE-fr" };
                yield return new object[] { "zh-Hans-CN", "zh-CN" };
                yield return new object[] { "zh-Hans-SG", "zh-SG" };
                yield return new object[] { "zh-Hant-HK", "zh-HK" };
                yield return new object[] { "zh-Hant-MO", "zh-MO" };
                yield return new object[] { "zh-Hant-TW", "zh-TW" };
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)] // Windows specific behavior
        [InlineData("x\u0000X-Yy")]
        [InlineData("zh-cmn")]
        [InlineData("zh-CMN-HANS")]
        [InlineData("zh-cmn-Hant")]
        public void TestIvalidCultureNames(string cultureName)
        {
            Assert.Throws<CultureNotFoundException>(() => new CultureInfo(cultureName));
        }

        [ConditionalTheory(nameof(PlatformSupportsFakeCulture))]
        [MemberData(nameof(GetCultureInfoTestData))]
        public void GetCultureInfo(string name, string expected = null)
        {
            if (expected == null) expected = name;
            Assert.Equal(expected, CultureInfo.GetCultureInfo(name).Name);
            Assert.Equal(expected, CultureInfo.GetCultureInfo(name, predefinedOnly: false).Name);
        }

        [ConditionalTheory(nameof(PlatformSupportsFakeCulture))]
        [InlineData("z")]
        [InlineData("en@US")]
        [InlineData("\uFFFF")]
        [InlineData("\u0080")]
        [InlineData("-foo")]
        [InlineData("foo-")]
        [InlineData("/foo")]
        [InlineData("_bar")]
        [InlineData("bar_")]
        [InlineData("bar/")]
        [InlineData("foo__bar")]
        [InlineData("foo--bar")]
        [InlineData("foo-_bar")]
        [InlineData("foo_-bar")]
        [InlineData("foo/bar")]
        [InlineData("/")]
        [InlineData("01234567890123456789012345678901234567890123456789012345678901234567890123456789012345")] // > 85 characters
        public void TestInvalidCultureNames(string name)
        {
            Assert.Throws<CultureNotFoundException>(() => CultureInfo.GetCultureInfo(name));
            Assert.Throws<CultureNotFoundException>(() => CultureInfo.GetCultureInfo(name, predefinedOnly: false));
            Assert.Throws<CultureNotFoundException>(() => CultureInfo.GetCultureInfo(name, predefinedOnly: true));
        }

        [ConditionalTheory(nameof(PlatformSupportsFakeCulture))]
        [InlineData("en")]
        [InlineData("en-US")]
        [InlineData("ja-JP")]
        [InlineData("ar-SA")]
        public void TestGetCultureInfoWithNoneConstructedCultures(string name)
        {
            Assert.Equal(name, CultureInfo.GetCultureInfo(name).Name);
            Assert.Equal(name, CultureInfo.GetCultureInfo(name, predefinedOnly: false).Name);
            Assert.Equal(name, CultureInfo.GetCultureInfo(name, predefinedOnly: true).Name);
        }

        [ConditionalTheory(nameof(PlatformSupportsFakeCulture))]
        [InlineData("xx")]
        [InlineData("xx-XX")]
        [InlineData("xx-YY")]
        public void TestFakeCultureNames(string name)
        {
            Assert.Equal(name, CultureInfo.GetCultureInfo(name).Name);
            Assert.Equal(name, CultureInfo.GetCultureInfo(name, predefinedOnly: false).Name);
            Assert.Throws<CultureNotFoundException>(() => CultureInfo.GetCultureInfo(name, predefinedOnly: true));
        }

        [ConditionalTheory(nameof(PlatformSupportsFakeCultureAndRemoteExecutor))]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "Remote executor has problems with exit codes")]
        [InlineData("1", "xx-XY")]
        [InlineData("1", "zx-ZY")]
        [InlineData("0", "xx-XY")]
        [InlineData("0", "zx-ZY")]
        public void PredefinedCulturesOnlyEnvVarTest(string predefinedCulturesOnlyEnvVar, string cultureName)
        {
            var psi = new ProcessStartInfo();
            psi.Environment.Clear();

            psi.Environment.Add("DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY", predefinedCulturesOnlyEnvVar);

            RemoteExecutor.Invoke((culture, predefined) =>
            {
                if (predefined == "1")
                {
                    AssertExtensions.Throws<CultureNotFoundException>(() => new CultureInfo(culture));
                }
                else
                {
                    CultureInfo ci = new CultureInfo(culture);
                    Assert.Equal(culture, ci.Name);
                }
            }, cultureName, predefinedCulturesOnlyEnvVar, new RemoteInvokeOptions { StartInfo = psi }).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnPlatform(TestPlatforms.LinuxBionic, "Remote executor has problems with exit codes")]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void TestAllowInvariantCultureOnly(bool enableInvariant, bool predefinedCulturesOnly, bool declarePredefinedCulturesOnly)
        {
            var psi = new ProcessStartInfo();
            psi.Environment.Clear();

            if (enableInvariant)
            {
                psi.Environment.Add("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "true");
            }

            if (declarePredefinedCulturesOnly)
            {
                psi.Environment.Add("DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY", predefinedCulturesOnly ? "true" : "false");
            }

            bool restricted = enableInvariant && (declarePredefinedCulturesOnly ? predefinedCulturesOnly : true);

            RemoteExecutor.Invoke((invariantEnabled, isRestricted) =>
            {
                bool restrictedMode = bool.Parse(isRestricted);

                // First ensure we can create the current cultures regardless of the mode we are in
                Assert.NotNull(CultureInfo.CurrentCulture);
                Assert.NotNull(CultureInfo.CurrentUICulture);

                // Invariant culture should be valid all the time
                Assert.Equal("", new CultureInfo("").Name);
                Assert.Equal("", CultureInfo.InvariantCulture.Name);

                if (restrictedMode)
                {
                    Assert.Equal("", CultureInfo.CurrentCulture.Name);
                    Assert.Equal("", CultureInfo.CurrentUICulture.Name);

                    // Throwing exception is testing accessing the resources in this restricted mode.
                    // We should retrieve the resources from the neutral resources in the main assemblies.
                    AssertExtensions.Throws<CultureNotFoundException>(() => new CultureInfo("en-US"));
                    AssertExtensions.Throws<CultureNotFoundException>(() => new CultureInfo("en"));

                    AssertExtensions.Throws<CultureNotFoundException>(() => new CultureInfo("ja-JP"));
                    AssertExtensions.Throws<CultureNotFoundException>(() => new CultureInfo("es"));

                    // Test throwing exceptions from non-core assemblies.
                    Exception exception = Record.Exception(() => new ConcurrentBag<string>(null));
                    Assert.NotNull(exception);
                    Assert.IsType<ArgumentNullException>(exception);
                    Assert.Equal("collection", (exception as ArgumentNullException).ParamName);
                    Assert.Equal("Value cannot be null. (Parameter 'collection')", exception.Message);
                }
                else
                {
                    Assert.Equal("en-US", new CultureInfo("en-US").Name);
                    Assert.Equal("ja-JP", new CultureInfo("ja-JP").Name);
                    Assert.Equal("en", new CultureInfo("en").Name);
                    Assert.Equal("es", new CultureInfo("es").Name);
                }

                // Ensure the Invariant Mode functionality still work
                if (bool.Parse(invariantEnabled))
                {
                    Assert.True(CultureInfo.CurrentCulture.Calendar is GregorianCalendar);
                    Assert.True("abcd".Equals("ABCD", StringComparison.CurrentCultureIgnoreCase));
                    Assert.Equal("Invariant Language (Invariant Country)", CultureInfo.CurrentCulture.NativeName);
                }

            }, enableInvariant.ToString(), restricted.ToString(), new RemoteInvokeOptions { StartInfo = psi }).Dispose();
        }
    }
}
