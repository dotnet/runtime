// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Globalization
{
    internal partial class CultureData
    {
        /// <summary>
        /// Check with the OS to see if this is a valid culture.
        /// If so we populate a limited number of fields.  If its not valid we return false.
        ///
        /// The fields we populate:
        ///
        /// sWindowsName -- The name that windows thinks this culture is, ie:
        ///                            en-US if you pass in en-US
        ///                            de-DE_phoneb if you pass in de-DE_phoneb
        ///                            fj-FJ if you pass in fj (neutral, on a pre-Windows 7 machine)
        ///                            fj if you pass in fj (neutral, post-Windows 7 machine)
        ///
        /// sRealName -- The name you used to construct the culture, in pretty form
        ///                       en-US if you pass in EN-us
        ///                       en if you pass in en
        ///                       de-DE_phoneb if you pass in de-DE_phoneb
        ///
        /// sSpecificCulture -- The specific culture for this culture
        ///                             en-US for en-US
        ///                             en-US for en
        ///                             de-DE_phoneb for alt sort
        ///                             fj-FJ for fj (neutral)
        ///
        /// sName -- The IETF name of this culture (ie: no sort info, could be neutral)
        ///                en-US if you pass in en-US
        ///                en if you pass in en
        ///                de-DE if you pass in de-DE_phoneb
        ///
        /// bNeutral -- TRUE if it is a neutral locale
        ///
        /// For a neutral we just populate the neutral name, but we leave the windows name pointing to the
        /// windows locale that's going to provide data for us.
        /// </summary>
        private unsafe bool InitCultureDataCore()
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            int result;
            string realNameBuffer = _sRealName;
            char* pBuffer = stackalloc char[Interop.Kernel32.LOCALE_NAME_MAX_LENGTH];

            result = GetLocaleInfoEx(realNameBuffer, Interop.Kernel32.LOCALE_SNAME, pBuffer, Interop.Kernel32.LOCALE_NAME_MAX_LENGTH);

            // Did it fail?
            if (result == 0)
            {
                return false;
            }

            // It worked, note that the name is the locale name, so use that (even for neutrals)
            // We need to clean up our "real" name, which should look like the windows name right now
            // so overwrite the input with the cleaned up name
            _sRealName = new string(pBuffer, 0, result - 1);
            realNameBuffer = _sRealName;

            // Check for neutrality, don't expect to fail
            // (buffer has our name in it, so we don't have to do the gc. stuff)

            result = GetLocaleInfoEx(realNameBuffer, Interop.Kernel32.LOCALE_INEUTRAL | Interop.Kernel32.LOCALE_RETURN_NUMBER, pBuffer, sizeof(int) / sizeof(char));
            if (result == 0)
            {
                return false;
            }

            // Remember our neutrality
            _bNeutral = *((uint*)pBuffer) != 0;

            // Note: Parents will be set dynamically

            // Start by assuming the windows name will be the same as the specific name since windows knows
            // about specifics on all versions. Only for downlevel Neutral locales does this have to change.
            _sWindowsName = realNameBuffer;

            // Neutrals and non-neutrals are slightly different
            if (_bNeutral)
            {
                // Neutral Locale

                // IETF name looks like neutral name
                _sName = realNameBuffer;

                // Specific locale name is whatever ResolveLocaleName (win7+) returns.
                // (Buffer has our name in it, and we can recycle that because windows resolves it before writing to the buffer)
                result = Interop.Kernel32.ResolveLocaleName(realNameBuffer, pBuffer, Interop.Kernel32.LOCALE_NAME_MAX_LENGTH);

                // 0 is failure, 1 is invariant (""), which we expect
                if (result < 1)
                {
                    return false;
                }

                // We found a locale name, so use it.
                // In vista this should look like a sort name (de-DE_phoneb) or a specific culture (en-US) and be in the "pretty" form
                _sSpecificCulture = new string(pBuffer, 0, result - 1);
            }
            else
            {
                // Specific Locale

                // Specific culture's the same as the locale name since we know its not neutral
                // On mac we'll use this as well, even for neutrals. There's no obvious specific
                // culture to use and this isn't exposed, but behaviorally this is correct on mac.
                // Note that specifics include the sort name (de-DE_phoneb)
                _sSpecificCulture = realNameBuffer;

                _sName = realNameBuffer;

                // We need the IETF name (sname)
                // If we aren't an alt sort locale then this is the same as the windows name.
                // If we are an alt sort locale then this is the same as the part before the _ in the windows name
                // This is for like de-DE_phoneb and es-ES_tradnl that hsouldn't have the _ part

                result = GetLocaleInfoEx(realNameBuffer, Interop.Kernel32.LOCALE_ILANGUAGE | Interop.Kernel32.LOCALE_RETURN_NUMBER, pBuffer, sizeof(int) / sizeof(char));
                if (result == 0)
                {
                    return false;
                }

                _iLanguage = *((int*)pBuffer);

                if (!IsCustomCultureId(_iLanguage))
                {
                    // not custom locale
                    int index = realNameBuffer.IndexOf('_');
                    if (index > 0)
                    {
                        _sName = realNameBuffer.Substring(0, index);
                    }
                }
            }

            // It succeeded.
            return true;
        }

        internal bool IsWin32Installed => true;

        internal static unsafe CultureData GetCurrentRegionData()
        {
            Span<char> geoIso2Letters = stackalloc char[10];

            int geoId = Interop.Kernel32.GetUserGeoID(Interop.Kernel32.GEOCLASS_NATION);
            if (geoId != Interop.Kernel32.GEOID_NOT_AVAILABLE)
            {
                int geoIsoIdLength;
                fixed (char* pGeoIsoId = geoIso2Letters)
                {
                    geoIsoIdLength = Interop.Kernel32.GetGeoInfo(geoId, Interop.Kernel32.GEO_ISO2, pGeoIsoId, geoIso2Letters.Length, 0);
                }

                if (geoIsoIdLength != 0)
                {
                    geoIsoIdLength -= geoIso2Letters[geoIsoIdLength - 1] == 0 ? 1 : 0; // handle null termination and exclude it.
                    CultureData? cd = GetCultureDataForRegion(geoIso2Letters.Slice(0, geoIsoIdLength).ToString(), true);
                    if (cd != null)
                    {
                        return cd;
                    }
                }
            }

            // Fallback to current locale data.
            return CultureInfo.CurrentCulture._cultureData;
        }
    }
}
