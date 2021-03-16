// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.DotNet.RemoteExecutor;
using System.Diagnostics;
using Xunit;

namespace System.Globalization.Tests
{
    public class GetCultureInfoTests
    {
        public static bool PlatformSupportsFakeCulture => (!PlatformDetection.IsWindows || (PlatformDetection.WindowsVersion >= 10 && !PlatformDetection.IsNetFramework)) && PlatformDetection.IsNotBrowser;
        public static bool PlatformSupportsFakeCultureAndRemoteExecutor => PlatformSupportsFakeCulture && RemoteExecutor.IsSupported;

        public static IEnumerable<object[]> GetCultureInfoTestData()
        {
            yield return new object[] { "en" };
            yield return new object[] { "en-US" };
            yield return new object[] { "ja-JP" };
            yield return new object[] { "ar-SA" };
            yield return new object[] { "xx-XX" };
            yield return new object[] { "de-AT-1901" };
            yield return new object[] { "zh-Hans" };
            yield return new object[] { "zh-Hans-HK" };
            yield return new object[] { "zh-Hans-MO" };
            yield return new object[] { "zh-Hans-TW" };
            yield return new object[] { "zh-Hant" };
            yield return new object[] { "zh-Hant-CN" };
            yield return new object[] { "zh-Hant-SG" };

            if (PlatformDetection.IsIcuGlobalization)
            {
                yield return new object[] { "x\u0000X-Yy", "x" }; // Null byte
                yield return new object[] { "sgn-BE-FR" };
                yield return new object[] { "zh-min-nan", "nan" };
                yield return new object[] { "zh-cmn", "zh-CMN" };
                yield return new object[] { "zh-CMN-HANS" };
                yield return new object[] { "zh-cmn-Hant", "zh-CMN-HANT" };
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
        [InlineData("0123456789012345678901234567890123456789012345678901234567890123456789012345678901234")] // > 85 characters
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
    }
}
