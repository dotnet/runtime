// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    public partial class CultureInfo : IFormatProvider
    {
        private static CultureInfo GetUserDefaultCultureCacheOverride()
        {
            return null; // ICU doesn't provide a user override
        }

        internal static CultureInfo GetUserDefaultCulture()
        {
            CultureInfo cultureInfo = null;
            string localeName;
            if (CultureData.GetDefaultLocaleName(out localeName))
            {
                cultureInfo = GetCultureByName(localeName, true);
                cultureInfo.m_isReadOnly = true;
            }
            else
            {
                cultureInfo = CultureInfo.InvariantCulture;
            }

            return cultureInfo;
        }
    }
}
