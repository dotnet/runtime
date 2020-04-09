// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Globalization
{
    public partial class CultureInfo : IFormatProvider
    {
        internal static CultureInfo NlsGetUserDefaultCulture()
        {
            Debug.Assert(GlobalizationMode.UseNls);

            if (GlobalizationMode.Invariant)
                return CultureInfo.InvariantCulture;

            string? strDefault = CultureData.GetLocaleInfoEx(Interop.Kernel32.LOCALE_NAME_USER_DEFAULT, Interop.Kernel32.LOCALE_SNAME);
            if (strDefault == null)
            {
                strDefault = CultureData.GetLocaleInfoEx(Interop.Kernel32.LOCALE_NAME_SYSTEM_DEFAULT, Interop.Kernel32.LOCALE_SNAME);

                if (strDefault == null)
                {
                    // If system default doesn't work, use invariant
                    return CultureInfo.InvariantCulture;
                }
            }

            return GetCultureByName(strDefault);
        }

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

        private static unsafe CultureInfo NlsGetUserDefaultUICulture()
        {
            Debug.Assert(GlobalizationMode.UseNls);

            if (GlobalizationMode.Invariant)
                return CultureInfo.InvariantCulture;

            const uint MUI_LANGUAGE_NAME = 0x8;    // Use ISO language (culture) name convention
            uint langCount = 0;
            uint bufLen = 0;

            if (Interop.Kernel32.GetUserPreferredUILanguages(MUI_LANGUAGE_NAME, &langCount, null, &bufLen) != Interop.BOOL.FALSE)
            {
                char[] languages = new char[bufLen];
                fixed (char* pLanguages = languages)
                {
                    if (Interop.Kernel32.GetUserPreferredUILanguages(MUI_LANGUAGE_NAME, &langCount, pLanguages, &bufLen) != Interop.BOOL.FALSE)
                    {
                        int index = 0;
                        while (languages[index] != (char)0 && index < languages.Length)
                        {
                            index++;
                        }

                        return GetCultureByName(new string(languages, 0, index));
                    }
                }
            }

            return InitializeUserDefaultCulture();
        }
    }
}
