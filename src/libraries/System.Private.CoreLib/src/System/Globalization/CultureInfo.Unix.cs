// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    public partial class CultureInfo : IFormatProvider
    {
        internal static CultureInfo GetUserDefaultCulture()
        {
            if (GlobalizationMode.Invariant)
                return CultureInfo.InvariantCulture;

            CultureInfo cultureInfo;
            if (CultureData.GetDefaultLocaleName(out string? localeName))
            {
                Debug.Assert(localeName != null);
#if TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
                CultureData? cultureData = CultureData.GetUserDefaultCultureData(localeName);
                cultureInfo = cultureData is null ? InvariantCulture : new CultureInfo(cultureData, isReadOnly: true);
#else
                cultureInfo = GetCultureByName(localeName);
#endif
            }
            else
            {
                cultureInfo = CultureInfo.InvariantCulture;
            }

            return cultureInfo;
        }

        private static CultureInfo GetUserDefaultUICulture()
        {
            return InitializeUserDefaultCulture();
        }
    }
}
