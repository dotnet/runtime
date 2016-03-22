// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Encapsulates CultureInfo fallback for resource 
** lookup
**
** 
===========================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
#if FEATURE_CORECLR
using System.Diagnostics.Contracts;
#endif
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Resources
{
    internal class ResourceFallbackManager : IEnumerable<CultureInfo>
    {
        private CultureInfo m_startingCulture;
        private CultureInfo m_neutralResourcesCulture;
        private bool m_useParents;

// Added but disabled from desktop in .NET 4.0, stayed disabled in .NET 4.5
#if FEATURE_CORECLR
        // This is a cache of the thread, process, user, and OS-preferred fallback cultures.
        // However, each thread may have a different value, and these may change during the
        // lifetime of the process.  So this cache must be verified each time we use it.
        // Hence, we'll keep an array of strings for culture names & check it each time,
        // but we'll really cache an array of CultureInfo's.  Using thread-local statics
        // as well to avoid differences across threads.
        [ThreadStatic]
        private static CultureInfo[] cachedOsFallbackArray;
#endif // FEATURE_CORECLR

        internal ResourceFallbackManager(CultureInfo startingCulture, CultureInfo neutralResourcesCulture, bool useParents)
        {
            if (startingCulture != null)
            {
                m_startingCulture = startingCulture;
            }
            else
            {
                m_startingCulture = CultureInfo.CurrentUICulture;
            }

            m_neutralResourcesCulture = neutralResourcesCulture;
            m_useParents = useParents;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // WARING: This function must be kept in sync with ResourceManager.GetFirstResourceSet()
        public IEnumerator<CultureInfo> GetEnumerator()
        {
            bool reachedNeutralResourcesCulture = false;

            // 1. starting culture chain, up to neutral
            CultureInfo currentCulture = m_startingCulture;
            do
            {
                if (m_neutralResourcesCulture != null && currentCulture.Name == m_neutralResourcesCulture.Name) 
                {
                    // Return the invariant culture all the time, even if the UltimateResourceFallbackLocation
                    // is a satellite assembly.  This is fixed up later in ManifestBasedResourceGroveler::UltimateFallbackFixup.
                    yield return CultureInfo.InvariantCulture;
                    reachedNeutralResourcesCulture = true;
                    break;
                }
                yield return currentCulture;
                currentCulture = currentCulture.Parent;
            } while (m_useParents && !currentCulture.HasInvariantCultureName);

            if (!m_useParents || m_startingCulture.HasInvariantCultureName)
            {
                yield break;
            }

// Added but disabled from desktop in .NET 4.0, stayed disabled in .NET 4.5
#if FEATURE_CORECLR
            // 2. user preferred cultures, omitting starting culture if tried already
            //    Compat note: For console apps, this API will return cultures like Arabic
            //    or Hebrew that are displayed right-to-left.  These don't work with today's 
            //    CMD.exe.  Since not all apps can short-circuit RTL languages to look at
            //    US English resources, we're exposing an appcompat flag for this, to make the
            //    osFallbackArray an empty array, mimicing our V2 behavior.  Apps should instead
            //    be using CultureInfo.GetConsoleFallbackUICulture, and then test whether that
            //    culture's code page can be displayed on the console, and if not, they should
            //    set their culture to their neutral resources language.
            //    Note: the app compat switch will omit the OS Preferred fallback culture.
            //    Compat note 2:  This feature breaks certain apps dependent on fallback to neutral
            //    resources.  See extensive note in GetResourceFallbackArray.  
            CultureInfo[] osFallbackArray = LoadPreferredCultures();
            if (osFallbackArray != null)
            {
                foreach (CultureInfo ci in osFallbackArray)
                {
                    // only have to check starting culture and immediate parent for now.
                    // in Dev10, revisit this policy.
                    if (m_startingCulture.Name != ci.Name && m_startingCulture.Parent.Name != ci.Name)
                    {
                        yield return ci;
                    }
                }
            }
#endif // FEATURE_CORECLR

            // 3. invariant
            //    Don't return invariant twice though.
            if (reachedNeutralResourcesCulture)
                yield break;

            yield return CultureInfo.InvariantCulture;
        }

// Added but disabled from desktop in .NET 4.0, stayed disabled in .NET 4.5
#if FEATURE_CORECLR
        private static CultureInfo[] LoadPreferredCultures()
        {
            // The list of preferred cultures includes thread, process, user, and OS
            // information and may theoretically change every time we call it.  
            // The caching does save us some allocations - this complexity saved about 
            // 7% of the wall clock time on a US English machine, and may save more on non-English
            // boxes (since the fallback list may be longer).
            String[] cultureNames = GetResourceFallbackArray();
            if (cultureNames == null)
                return null;

            bool useCachedNames = (cachedOsFallbackArray != null && cultureNames.Length == cachedOsFallbackArray.Length);
            if (useCachedNames)
            {
                for (int i = 0; i < cultureNames.Length; i++)
                {
                    if (!String.Equals(cultureNames[i], cachedOsFallbackArray[i].Name))
                    {
                        useCachedNames = false;
                        break;
                    }
                }
            }
            if (useCachedNames)
                return cachedOsFallbackArray;

            cachedOsFallbackArray = LoadCulturesFromNames(cultureNames);
            return cachedOsFallbackArray;
        }

        private static CultureInfo[] LoadCulturesFromNames(String[] cultureNames)
        {
            if (cultureNames == null)
                return null;

            CultureInfo[] cultures = new CultureInfo[cultureNames.Length];
            int culturesIndex = 0;
            for (int i = 0; i < cultureNames.Length; i++)
            {
                // get cached, read-only cultures to avoid excess allocations
                cultures[culturesIndex] = CultureInfo.GetCultureInfo(cultureNames[i]);
                // Note GetCultureInfo can return null for a culture name that we don't support on the current OS.
                // Don't leave a null in the middle of the array.
                if (!Object.ReferenceEquals(cultures[culturesIndex], null))
                    culturesIndex++;
            }

            // If we couldn't create a culture, return an array of the right length.
            if (culturesIndex != cultureNames.Length)
            {
                CultureInfo[] ret = new CultureInfo[culturesIndex];
                Array.Copy(cultures, ret, culturesIndex);
                cultures = ret;
            }

            return cultures;
        }


        // Note: May return null.
        [System.Security.SecuritySafeCritical] // auto-generated
        private static String[] GetResourceFallbackArray()
        {
            // AppCompat note:  We've added this feature for desktop V4 but we ripped it out
            // before shipping V4.  It shipped in SL 2 and SL 3.  We preserved this behavior in SL 4
            // for compat with previous Silverlight releases.  We considered re-introducing this in .NET 
            // 4.5 for Windows 8 but chose not to because the Windows 8 immersive resources model
            // has been redesigned from the ground up and we chose to support it (for portable libraries
            // only) instead of further enhancing support for the classic resources model.
            // ---------------------------------------------------------------------
            // 
            // We have an appcompat problem that prevents us from adopting the ideal MUI model for
            // culture fallback.  Up until .NET Framework v4, our fallback was this:
            // 
            // CurrentUICulture & parents   Neutral
            // 
            // We also had applications that took a dependency on falling back to neutral resources.
            // IE, say an app is developed by US English developers - they may include English resources
            // in the main assembly, not ship an "en" satellite assembly, and ship a French satellite.
            // They may also omit the NeutralResourcesLanguageAttribute.
            // 
            // Starting with Silverlight v2 and following advice from the MUI team, we wanted to call 
            // the OS's GetThreadPreferredUILanguages, inserting the results like this:
            //
            // CurrentUICulture & parents   user-preferred fallback   OS-preferred fallback  Neutral
            //
            // This does not fit well for two reasons:
            //   1) There is no concept of neutral resources in MUI
            //   2) The user-preferred culture fallbacks make no sense in servers & non-interactive apps
            // This leads to bad results on certain combinations of OS language installations, user
            // settings, and applications built in certain styles.  The OS-preferred fallback should
            // be last, and the user-preferred fallback just breaks certain apps no matter where you put it.
            // 
            // Necessary and sufficient conditions for an AppCompat bug (if we respected user & OS fallbacks):
            //   1) A French OS (ie, you walk into an Internet café in Paris)
            //   2) A .NET application whose neutral resources are authored in English.
            //   3) The application did not provide an English satellite assembly (a common pattern).
            //   4) The application is localized to French.
            //   5) The user wants to read English, expressed in either of two ways:
            //      a. Changing Windows’ Display Language in the Regional Options Control Panel
            //      b. The application explicitly ASKS THE USER what language to display.
            // 
            // Obviously the exact languages above can be interchanged a bit - I’m keeping this concrete.
            // Also the NeutralResourcesLanguageAttribute will allow this to work, but usually we set it
            // to en-US for our assemblies, meaning all other English cultures are broken.
            //
            // Workarounds:            
            //   *) Use the NeutralResourcesLanguageAttribute and tell us that your neutral resources 
            //      are in region-neutral English (en).
            //   *) Consider shipping a region-neutral English satellite assembly.

            // Future work:
            // 2) Consider a mechanism for individual assemblies to opt into wanting user-preferred fallback.
            //    They should ship their neutral resources in a satellite assembly, or use the 
            //    NeutralResourcesLanguageAttribute to say their neutral resources are in a REGION-NEUTRAL
            //    language.  An appdomain or process-wide flag may not be sufficient.
            // 3) Ask Windows to clarify the scenario for the OS preferred fallback list, to decide whether
            //    we should probe there before or after looking at the neutral resources.  If we move it 
            //    to after the neutral resources, ask Windows to return a user-preferred fallback list 
            //    without the OS preferred fallback included.  This is a feature request for 
            //    GetThreadPreferredUILanguages.  We can muddle through without it by removing the OS 
            //    preferred fallback cultures from end of the combined user + OS preferred fallback list, carefully.
            // 4) Do not look at user-preferred fallback if Environment.UserInteractive is false.  (IE, 
            //    the Windows user who launches ASP.NET shouldn't determine how a web page gets
            //    localized - the server itself must respect the remote client's requested languages.)
            // 6) Figure out what should happen in servers (ASP.NET, SQL, NT Services, etc).
            // 
            // Done:
            // 1) Got data from Windows on priority of supporting OS preferred fallback.  We need to do it.
            //    Helps with consistency w/ Windows, and may be necessary for a long tail of other languages
            //    (ie, Windows has various degrees of localization support for ~135 languages, and fallbacks
            //     to certain languages is important.)
            // 5) Revisited guidance for using the NeutralResourcesLanguageAttribute.  Our docs should now say
            //    always pick a region-neutral language (ie, "en").

// TODO (matell): I think we actually want to pull this into the PAL on CultureInfo?
#if FEATURE_COREFX_GLOBALIZATION
            return null;
#else
            return CultureInfo.nativeGetResourceFallbackArray();
#endif
        }

#endif // FEATURE_CORECLR
    }
}
