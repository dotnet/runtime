// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Globalization
{
    public partial class CultureInfo : IFormatProvider
    {
        internal static CultureInfo IcuGetUserDefaultCulture()
        {
            Debug.Assert(!GlobalizationMode.UseNls);

            if (GlobalizationMode.Invariant)
                return CultureInfo.InvariantCulture;

            CultureInfo cultureInfo;
            string? localeName;
            if (CultureData.GetDefaultLocaleName(out localeName))
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

        private static CultureInfo IcuGetPredefinedCultureInfo(string name)
        {
            Debug.Assert(!GlobalizationMode.UseNls);

            if (!Interop.Globalization.IsPredefinedLocale(name))
            {
                throw new CultureNotFoundException(nameof(name), SR.Format(SR.Argument_InvalidPredefinedCultureName, name));
            }

            return GetCultureInfo(name);
        }

        private static CultureInfo IcuGetUserDefaultUICulture()
        {
            Debug.Assert(!GlobalizationMode.UseNls);

            return InitializeUserDefaultCulture();
        }
    }
}
