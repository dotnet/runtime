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
            string? localeName;
            bool result = false;
#if TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
            if (GlobalizationMode.Hybrid)
            {
               localeName = Interop.Globalization.GetDefaultLocaleNameNative();
               result = localeName != null;
            }
            else
            {
                result = CultureData.GetDefaultLocaleName(out localeName);
            }
#else
            result = CultureData.GetDefaultLocaleName(out localeName);
#endif
            if (result)
            {
                Debug.Assert(localeName != null);
                cultureInfo = GetCultureByName(localeName);
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
