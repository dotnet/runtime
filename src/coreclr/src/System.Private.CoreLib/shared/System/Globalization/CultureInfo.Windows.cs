// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if FEATURE_APPX
using System.Resources;
using Internal.Resources;
#endif

namespace System.Globalization
{
    public partial class CultureInfo : IFormatProvider
    {
#if FEATURE_APPX
        // When running under AppX, we use this to get some information about the language list
        private static volatile WindowsRuntimeResourceManagerBase s_WindowsRuntimeResourceManager;

        [ThreadStatic]
        private static bool ts_IsDoingAppXCultureInfoLookup;
#endif

        internal static CultureInfo GetUserDefaultCulture()
        {
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

        private unsafe static CultureInfo GetUserDefaultUICulture()
        {
#if !ENABLE_WINRT
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
#endif

            return InitializeUserDefaultCulture();
        }

#if FEATURE_APPX
        internal static CultureInfo? GetCultureInfoForUserPreferredLanguageInAppX()
        {
            // If a call to GetCultureInfoForUserPreferredLanguageInAppX() generated a recursive
            // call to itself, return null, since we don't want to stack overflow.  For example,
            // this can happen if some code in this method ends up calling CultureInfo.CurrentCulture.
            // In this case, returning null will mean CultureInfo.CurrentCulture gets the default Win32
            // value, which should be fine.
            if (ts_IsDoingAppXCultureInfoLookup)
            {
                return null;
            }

            CultureInfo? toReturn;

            try
            {
                ts_IsDoingAppXCultureInfoLookup = true;

                if (s_WindowsRuntimeResourceManager == null)
                {
                    s_WindowsRuntimeResourceManager = ResourceManager.GetWinRTResourceManager();
                }

                toReturn = s_WindowsRuntimeResourceManager.GlobalResourceContextBestFitCultureInfo;
            }
            finally
            {
               ts_IsDoingAppXCultureInfoLookup = false;
            }

            return toReturn;
        }

        internal static bool SetCultureInfoForUserPreferredLanguageInAppX(CultureInfo ci)
        {
            if (s_WindowsRuntimeResourceManager == null)
            {
                s_WindowsRuntimeResourceManager = ResourceManager.GetWinRTResourceManager();
            }

            return s_WindowsRuntimeResourceManager.SetGlobalResourceContextDefaultCulture(ci);
        }
#endif
    }
}
