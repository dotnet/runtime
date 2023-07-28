// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    internal sealed partial class CultureData
    {
        private bool InitCultureDataCore() =>
#if TARGET_OSX || TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
        GlobalizationMode.Hybrid ? InitAppleCultureDataCore() : InitIcuCultureDataCore();
#else
        InitIcuCultureDataCore();
#endif

        // Unix doesn't support user overrides
        partial void InitUserOverride(bool useUserOverride);

        private static string? LCIDToLocaleName(int culture)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            return IcuLocaleData.LCIDToLocaleName(culture);
        }

        private string[]? GetTimeFormatsCore(bool shortFormat)
        {
#if TARGET_OSX || TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
            string format = GlobalizationMode.Hybrid ? GetTimeFormatStringNative(shortFormat) : IcuGetTimeFormatString(shortFormat);
#else
            string format = IcuGetTimeFormatString(shortFormat);
#endif
            return new string[] { format };
        }

        private static int GetAnsiCodePage(string cultureName)
        {
            int ansiCodePage = IcuLocaleData.GetLocaleDataNumericPart(cultureName, IcuLocaleDataParts.AnsiCodePage);
            return ansiCodePage == -1 ? CultureData.Invariant.ANSICodePage : ansiCodePage;
        }

        private static int GetOemCodePage(string cultureName)
        {
            int oemCodePage = IcuLocaleData.GetLocaleDataNumericPart(cultureName, IcuLocaleDataParts.OemCodePage);
            return oemCodePage == -1 ? CultureData.Invariant.OEMCodePage : oemCodePage;
        }

        private static int GetMacCodePage(string cultureName)
        {
            int macCodePage = IcuLocaleData.GetLocaleDataNumericPart(cultureName, IcuLocaleDataParts.MacCodePage);
            return macCodePage == -1 ? CultureData.Invariant.MacCodePage : macCodePage;
        }

        private static int GetEbcdicCodePage(string cultureName)
        {
            int ebcdicCodePage = IcuLocaleData.GetLocaleDataNumericPart(cultureName, IcuLocaleDataParts.EbcdicCodePage);
            return ebcdicCodePage == -1 ? CultureData.Invariant.EBCDICCodePage : ebcdicCodePage;
        }

        internal static bool IsWin32Installed => false;

        internal static unsafe CultureData GetCurrentRegionData() => CultureInfo.CurrentCulture._cultureData;

        private static bool ShouldUseUserOverrideNlsData => false;
    }
}
