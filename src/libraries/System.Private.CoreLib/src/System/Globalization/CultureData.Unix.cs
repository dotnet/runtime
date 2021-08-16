// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    internal sealed partial class CultureData
    {
        private bool InitCultureDataCore() => InitIcuCultureDataCore();

        private void InitUserOverride(bool useUserOverride)
        {
            // Unix doesn't support user overrides
            _bUseOverrides = false;
        }

        private static string? LCIDToLocaleName(int culture)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            return IcuLocaleData.LCIDToLocaleName(culture);
        }

        private string[]? GetTimeFormatsCore(bool shortFormat)
        {
            string format = IcuGetTimeFormatString(shortFormat);
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
