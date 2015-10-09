// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
