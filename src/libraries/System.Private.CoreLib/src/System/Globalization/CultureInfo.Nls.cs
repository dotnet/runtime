// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    public partial class CultureInfo : IFormatProvider
    {
        private static CultureInfo NlsGetPredefinedCultureInfo(string name)
        {
            Debug.Assert(GlobalizationMode.UseNls);

            CultureData.NlsIsEnsurePredefinedLocaleName(name);

            return GetCultureInfo(name);
        }
    }
}
