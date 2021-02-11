// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    public partial class CultureInfo : IFormatProvider
    {
        private static CultureInfo IcuGetPredefinedCultureInfo(string name)
        {
            Debug.Assert(!GlobalizationMode.UseNls);

            if (!Interop.Globalization.IsPredefinedLocale(name))
            {
                throw new CultureNotFoundException(nameof(name), SR.Format(SR.Argument_InvalidPredefinedCultureName, name));
            }

            return GetCultureInfo(name);
        }
    }
}
