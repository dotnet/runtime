// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Globalization
{
    public partial class CultureInfo : IFormatProvider
    {
        private static CultureInfo NlsGetPredefinedCultureInfo(string name)
        {
            Debug.Assert(GlobalizationMode.UseNls);

            CultureInfo culture = GetCultureInfo(name);
            string englishName = culture.EnglishName;

            // Check if the English Name starts with "Unknown Locale" or "Unknown Language" terms.
            const int SecondTermIndex = 8;

            if (englishName.StartsWith("Unknown ", StringComparison.Ordinal) && englishName.Length > SecondTermIndex &&
                (englishName.IndexOf("Locale", SecondTermIndex, StringComparison.Ordinal) == SecondTermIndex ||
                 englishName.IndexOf("Language", SecondTermIndex, StringComparison.Ordinal) == SecondTermIndex))
            {
                throw new CultureNotFoundException(nameof(name), SR.Format(SR.Argument_InvalidPredefinedCultureName, name));
            }

            return culture;
        }
    }
}
