// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoNames
    {
        private static bool IsIcuAndRemoteExecutionSupported => RemoteExecutor.IsSupported && PlatformDetection.IsIcuGlobalization;
        [ConditionalTheory(nameof(IsIcuAndRemoteExecutionSupported))]
        [InlineData("en", "en", "English", "English")]
        [InlineData("en", "fr", "English", "anglais")]
        [InlineData("aa", "aa", "Afar", "Afar")]
        [InlineData("en-US", "en-US", "English (United States)", "English (United States)")]
        [InlineData("en-US", "fr-FR", "English (United States)", "anglais (États-Unis)")]
        [InlineData("en-US", "de-DE", "English (United States)", "Englisch (Vereinigte Staaten)")]
        [InlineData("aa-ER", "aa-ER", "Afar (Eritrea)", "Afar (Eritrea)")]
        [InlineData("", "en-US", "Invariant Language (Invariant Country)", "Invariant Language (Invariant Country)")]
        [InlineData("", "fr-FR", "Invariant Language (Invariant Country)", "Invariant Language (Invariant Country)")]
        [InlineData("", "", "Invariant Language (Invariant Country)", "Invariant Language (Invariant Country)")]
        public void TestDisplayName(string cultureName, string uiCultureName, string nativeName, string displayName)
        {
            RemoteExecutor.Invoke((locale, uiLocale, native, display) =>
            {
                CultureInfo ci = CultureInfo.GetCultureInfo(locale);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(uiLocale);

                Assert.Equal(native, ci.NativeName);
                Assert.Equal(display, ci.DisplayName);
            }, cultureName, uiCultureName, nativeName, displayName).Dispose();
        }

        [ConditionalFact(nameof(IsIcuAndRemoteExecutionSupported))]
        public void TestDisplayNameWithSettingUICultureMultipleTime()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo ci = CultureInfo.GetCultureInfo("en-US");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                Assert.Equal("English (United States)", ci.DisplayName);

                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
                Assert.Equal("anglais (États-Unis)", ci.DisplayName);

                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");
                Assert.Equal("Englisch (Vereinigte Staaten)", ci.DisplayName);
            }).Dispose();
        }
    }
}
