// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using Internal.Runtime.Augments;

namespace System.Globalization
{
    public partial class CultureInfo : IFormatProvider
    {
        /// <summary>
        /// Gets the default user culture from WinRT, if available.
        /// </summary>
        /// <remarks>
        /// This method may return null, if there is no default user culture or if WinRT isn't available.
        /// </remarks>
        private static CultureInfo GetUserDefaultCultureCacheOverride()
        {
            WinRTInteropCallbacks callbacks = WinRTInterop.UnsafeCallbacks;
            if (callbacks != null && callbacks.IsAppxModel())
            {
                return (CultureInfo)callbacks.GetUserDefaultCulture();
            }

            return null;
        }

        private static CultureInfo GetUserDefaultCulture()
        {
            const uint LOCALE_SNAME = 0x0000005c;
            const string LOCALE_NAME_USER_DEFAULT = null;
            const string LOCALE_NAME_SYSTEM_DEFAULT = "!x-sys-default-locale";

            string strDefault = Interop.mincore.GetLocaleInfoEx(LOCALE_NAME_USER_DEFAULT, LOCALE_SNAME);
            if (strDefault == null)
            {
                strDefault = Interop.mincore.GetLocaleInfoEx(LOCALE_NAME_SYSTEM_DEFAULT, LOCALE_SNAME);

                if (strDefault == null)
                {
                    // If system default doesn't work, use invariant
                    return CultureInfo.InvariantCulture;
                }
            }

            CultureInfo temp = GetCultureByName(strDefault, true);

            temp.m_isReadOnly = true;

            return temp;
        }
    }
}
