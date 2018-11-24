// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

#if ENABLE_WINRT
using Internal.Runtime.Augments;
#endif

using System.Threading;
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

        /// <summary>
        /// Gets the default user culture from WinRT, if available.
        /// </summary>
        /// <remarks>
        /// This method may return null, if there is no default user culture or if WinRT isn't available.
        /// </remarks>
        private static CultureInfo GetUserDefaultCultureCacheOverride()
        {
#if ENABLE_WINRT
            WinRTInteropCallbacks callbacks = WinRTInterop.UnsafeCallbacks;
            if (callbacks != null && callbacks.IsAppxModel())
            {
                return (CultureInfo)callbacks.GetUserDefaultCulture();
            }
#endif

            return null;
        }

        internal static CultureInfo GetUserDefaultCulture()
        {
            if (GlobalizationMode.Invariant)
                return CultureInfo.InvariantCulture;
            
            const uint LOCALE_SNAME = 0x0000005c;
            const string LOCALE_NAME_USER_DEFAULT = null;
            const string LOCALE_NAME_SYSTEM_DEFAULT = "!x-sys-default-locale";

            string strDefault = CultureData.GetLocaleInfoEx(LOCALE_NAME_USER_DEFAULT, LOCALE_SNAME);
            if (strDefault == null)
            {
                strDefault = CultureData.GetLocaleInfoEx(LOCALE_NAME_SYSTEM_DEFAULT, LOCALE_SNAME);

                if (strDefault == null)
                {
                    // If system default doesn't work, use invariant
                    return CultureInfo.InvariantCulture;
                }
            }

            CultureInfo temp = GetCultureByName(strDefault, true);

            temp._isReadOnly = true;

            return temp;
        }

        private static CultureInfo GetUserDefaultUICulture()
        {
            if (GlobalizationMode.Invariant)
                return CultureInfo.InvariantCulture;

            const uint MUI_LANGUAGE_NAME = 0x8;    // Use ISO language (culture) name convention
            uint langCount = 0;
            uint bufLen = 0;

            if (Interop.Kernel32.GetUserPreferredUILanguages(MUI_LANGUAGE_NAME, out langCount, null, ref bufLen))
            {
                char [] languages = new char[bufLen];
                if (Interop.Kernel32.GetUserPreferredUILanguages(MUI_LANGUAGE_NAME, out langCount, languages, ref bufLen))
                {
                    int index = 0;
                    while (languages[index] != (char) 0 && index<languages.Length)
                    {
                        index++;
                    }

                    CultureInfo temp = GetCultureByName(new string(languages, 0, index), true);
                    temp._isReadOnly = true;
                    return temp;
                }
            }

            return GetUserDefaultCulture();
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  CurrentCulture
        //
        //  This instance provides methods based on the current user settings.
        //  These settings are volatile and may change over the lifetime of the
        //  thread.
        //
        ////////////////////////////////////////////////////////////////////////

        //
        // We use the following order to return CurrentCulture and CurrentUICulture
        //      o   Use WinRT to return the current user profile language
        //      o   use current thread culture if the user already set one using CurrentCulture/CurrentUICulture
        //      o   use thread culture if the user already set one using DefaultThreadCurrentCulture
        //          or DefaultThreadCurrentUICulture
        //      o   Use NLS default user culture
        //      o   Use NLS default system culture
        //      o   Use Invariant culture
        //
        public static CultureInfo CurrentCulture
        {
            get
            {
#if FEATURE_APPX
                if (ApplicationModel.IsUap)
                {
                    CultureInfo culture = GetCultureInfoForUserPreferredLanguageInAppX();
                    if (culture != null)
                        return culture;
                }
#endif
                CultureInfo ci = GetUserDefaultCultureCacheOverride();
                if (ci != null)
                {
                    return ci;
                }

                if (Thread.m_CurrentCulture != null)
                {
                    return Thread.m_CurrentCulture;
                }

                ci = s_DefaultThreadCurrentCulture;
                if (ci != null)
                {
                    return ci;
                }

                return s_userDefaultCulture ?? InitializeUserDefaultCulture();
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

#if FEATURE_APPX
                if (ApplicationModel.IsUap)
                {
                    if (SetCultureInfoForUserPreferredLanguageInAppX(value))
                    {
                        // successfully set the culture, otherwise fallback to legacy path
                        return; 
                    }
                }
#endif
                if (s_asyncLocalCurrentCulture == null)
                {
                    Interlocked.CompareExchange(ref s_asyncLocalCurrentCulture, new AsyncLocal<CultureInfo>(AsyncLocalSetCurrentCulture), null);
                }
                s_asyncLocalCurrentCulture.Value = value;
            }
        }

        public static CultureInfo CurrentUICulture
        {
            get
            {
#if FEATURE_APPX
                if (ApplicationModel.IsUap)
                {
                    CultureInfo culture = GetCultureInfoForUserPreferredLanguageInAppX();
                    if (culture != null)
                        return culture;
                }
#endif
                return GetCurrentUICultureNoAppX();
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                CultureInfo.VerifyCultureName(value, true);
#if FEATURE_APPX
                if (ApplicationModel.IsUap)
                {
                    if (SetCultureInfoForUserPreferredLanguageInAppX(value))
                    {
                        // successfully set the culture, otherwise fallback to legacy path
                        return; 
                    }
                }
#endif
                if (s_asyncLocalCurrentUICulture == null)
                {
                    Interlocked.CompareExchange(ref s_asyncLocalCurrentUICulture, new AsyncLocal<CultureInfo>(AsyncLocalSetCurrentUICulture), null);
                }

                // this one will set s_currentThreadUICulture too
                s_asyncLocalCurrentUICulture.Value = value;               
            }
        }

#if FEATURE_APPX
        internal static CultureInfo GetCultureInfoForUserPreferredLanguageInAppX()
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

            CultureInfo toReturn = null;

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
