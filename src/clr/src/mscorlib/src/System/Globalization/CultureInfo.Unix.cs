// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

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
            if (GlobalizationMode.Invariant)
                return CultureInfo.InvariantCulture;

            CultureInfo cultureInfo = null;
            string localeName;
            if (CultureData.GetDefaultLocaleName(out localeName))
            {
                cultureInfo = GetCultureByName(localeName, true);
                cultureInfo._isReadOnly = true;
            }
            else
            {
                cultureInfo = CultureInfo.InvariantCulture;
            }

            return cultureInfo;
        }

        private static CultureInfo GetUserDefaultUICulture()
        {
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
                if (Thread.m_CurrentCulture != null)
                {
                    return Thread.m_CurrentCulture;
                }

                CultureInfo ci = s_DefaultThreadCurrentCulture;
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
                return GetCurrentUICultureNoAppX();
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                CultureInfo.VerifyCultureName(value, true);
                if (s_asyncLocalCurrentUICulture == null)
                {
                    Interlocked.CompareExchange(ref s_asyncLocalCurrentUICulture, new AsyncLocal<CultureInfo>(AsyncLocalSetCurrentUICulture), null);
                }

                // this one will set s_currentThreadUICulture too
                s_asyncLocalCurrentUICulture.Value = value;               
            }
        }
        
    }
}
