// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    public partial class CultureInfo : IFormatProvider
    {
        internal static CultureInfo GetUserDefaultCulture()
        {
            if (GlobalizationMode.Invariant)
                return CultureInfo.InvariantCulture;

            string? strDefault = UserDefaultLocaleName;

            return strDefault != null ?
                GetCultureByName(strDefault) :
                CultureInfo.InvariantCulture;
        }

        private static unsafe CultureInfo GetUserDefaultUICulture()
        {
            if (GlobalizationMode.Invariant)
                return CultureInfo.InvariantCulture;

            const uint MUI_LANGUAGE_NAME = 0x8;    // Use ISO language (culture) name convention
            uint langCount = 0;
            uint bufLen = 0;

            if (Interop.Kernel32.GetUserPreferredUILanguages(MUI_LANGUAGE_NAME, &langCount, null, &bufLen) != Interop.BOOL.FALSE)
            {
                Span<char> languages = bufLen <= 256 ? stackalloc char[(int)bufLen] : new char[bufLen];
                fixed (char* pLanguages = languages)
                {
                    if (Interop.Kernel32.GetUserPreferredUILanguages(MUI_LANGUAGE_NAME, &langCount, pLanguages, &bufLen) != Interop.BOOL.FALSE)
                    {
                        return GetCultureByName(languages.ToString());
                    }
                }
            }

            return InitializeUserDefaultCulture();
        }

        internal static string? UserDefaultLocaleName { get; set; } = GetUserDefaultLocaleName();

        private static string? GetUserDefaultLocaleName() =>
            GlobalizationMode.Invariant ?
                CultureInfo.InvariantCulture.Name :
                CultureData.GetLocaleInfoEx(Interop.Kernel32.LOCALE_NAME_USER_DEFAULT, Interop.Kernel32.LOCALE_SNAME) ??
                CultureData.GetLocaleInfoEx(Interop.Kernel32.LOCALE_NAME_SYSTEM_DEFAULT, Interop.Kernel32.LOCALE_SNAME);
    }
}
