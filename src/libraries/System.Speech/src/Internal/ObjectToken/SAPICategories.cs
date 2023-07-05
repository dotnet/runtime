// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Speech.Internal.Synthesis;
using Microsoft.Win32;

namespace System.Speech.Internal.ObjectTokens
{
    internal static class SAPICategories
    {
        #region internal Methods

        internal static ObjectToken DefaultToken(string category)
        {
            Helpers.ThrowIfEmptyOrNull(category, nameof(category));

            // Try first to get the preferred token for the current user
            // If failed try to get it for the local machine
            return
                DefaultToken(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Speech\" + category, _defaultTokenIdValueName) ??
                DefaultToken(SpeechRegistryKey + category, _defaultTokenIdValueName);
        }

        /// <summary>
        /// Retrieve the Multimedia device ID. If the entry 'DefaultTokenId' is defined in the registry
        /// under 'HKEY_CURRENT_USER\SOFTWARE\Microsoft\Speech\AudioOutput' then a multimedia device is looked
        /// for with this token. Otherwise, picks the default WAVE_MAPPER is returned.
        /// </summary>
        internal static int DefaultDeviceOut()
        {
            int device = -1;
            using (ObjectTokenCategory tokenCategory = ObjectTokenCategory.Create(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Speech\AudioOutput"))
            {
                if (tokenCategory != null)
                {
                    string deviceName;
                    if (tokenCategory.TryGetString(_defaultTokenIdValueName, out deviceName))
                    {
                        int pos = deviceName.IndexOf('\\');
                        if (pos > 0 && pos < deviceName.Length)
                        {
                            using (RegistryDataKey deviceKey = RegistryDataKey.Create(deviceName.Substring(pos + 1), Registry.LocalMachine))
                            {
                                if (deviceKey != null)
                                {
                                    device = AudioDeviceOut.GetDevicedId(deviceKey.Name);
                                }
                            }
                        }
                    }
                }
            }

            return device;
        }

        #endregion

        private const string SpeechRegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech\";

        internal const string CurrentUserVoices = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Speech\Voices";

        #region internal Fields

        internal const string Recognizers = SpeechRegistryKey + "Recognizers";
        internal const string Voices = SpeechRegistryKey + "Voices";

        internal const string AudioIn = SpeechRegistryKey + "AudioInput";

        #endregion

        #region Private Methods

        private static ObjectToken DefaultToken(string category, string defaultTokenIdValueName)
        {
            ObjectToken token = GetPreference(category, defaultTokenIdValueName);

            if (token != null)
            {
                // Now do special check to see if we have another token from the same vendor with a
                // more recent version - if so use that.

                // First lets change the category to LOCAL_MACHINE
                using (ObjectTokenCategory tokenCategory = ObjectTokenCategory.Create(category))
                {
                    if (tokenCategory != null)
                    {
                        if (token != null)
                        {
                            foreach (ObjectToken tokenSeed in (IEnumerable<ObjectToken>)tokenCategory)
                            {
                                token = GetHighestTokenVersion(token, tokenSeed, s_asVersionDefault);
                            }
                        }
                        else
                        {
                            // If there wasn't a default, just pick one with the proper culture
                            string[] sCultureId = new string[] { string.Format(CultureInfo.InvariantCulture, "{0:x}", CultureInfo.CurrentUICulture.LCID) };

                            foreach (ObjectToken tokenSeed in (IEnumerable<ObjectToken>)tokenCategory)
                            {
                                if (tokenSeed.MatchesAttributes(sCultureId))
                                {
                                    token = tokenSeed;
                                    break;
                                }
                            }

                            // Still nothing, picks the first one
                            if (token == null)
                            {
                                foreach (ObjectToken tokenSeed in (IEnumerable<ObjectToken>)tokenCategory)
                                {
                                    token = tokenSeed;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return token;
        }

        /// <summary>
        /// Try to get the preferred token for a category
        /// </summary>
        private static ObjectToken GetPreference(string category, string defaultLocation)
        {
            ObjectToken token = null;

            using (ObjectTokenCategory tokenCategory = ObjectTokenCategory.Create(category))
            {
                if (tokenCategory != null)
                {
                    string sToken;
                    if (tokenCategory.TryGetString(defaultLocation, out sToken))
                    {
                        token = tokenCategory.OpenToken(sToken);
                    }
                }
            }
            return token;
        }

        /// <summary>
        /// Takes two tokens and compares them using version info.
        /// Note only tokens that match on Vendor, ProductLine, Language get compared, the pfDidCompare flag indicates this
        /// </summary>
        private static int CompareTokenVersions(ObjectToken token1, ObjectToken token2, out bool pfDidCompare)
        {
            pfDidCompare = false;

            RegistryDataKey attributes1 = null;
            RegistryDataKey attributes2 = null;
            attributes1 = token1.Attributes;
            attributes2 = token2.Attributes;

            // get vendor, version, language, product line for token 1
            if (attributes1 != null)
            {
                string vendor1;
                string productLine1;
                string version1;
                string language1;
                attributes1.TryGetString("Vendor", out vendor1);
                attributes1.TryGetString("ProductLine", out productLine1);
                attributes1.TryGetString("Version", out version1);
                attributes1.TryGetString("Language", out language1);

                // get vendor, version, language, product line for token 2
                if (attributes2 != null)
                {
                    string vendor2;
                    string productLine2;
                    string version2;
                    string language2;
                    attributes2.TryGetString("Vendor", out vendor2);
                    attributes2.TryGetString("ProductLine", out productLine2);
                    attributes2.TryGetString("Version", out version2);
                    attributes2.TryGetString("Language", out language2);

                    if (((string.IsNullOrEmpty(vendor1) && string.IsNullOrEmpty(vendor2)) || (!string.IsNullOrEmpty(vendor1) && !string.IsNullOrEmpty(vendor2) && vendor1 == vendor2)) &&
                        ((string.IsNullOrEmpty(productLine1) && string.IsNullOrEmpty(productLine2)) || (!string.IsNullOrEmpty(productLine1) && !string.IsNullOrEmpty(productLine2) && productLine1 == productLine2)) &&
                        ((string.IsNullOrEmpty(language1) && string.IsNullOrEmpty(language2)) || (!string.IsNullOrEmpty(language1) && !string.IsNullOrEmpty(language2) && language1 == language2)))
                    {
                        pfDidCompare = true;
                        return CompareVersions(version1, version2);
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Takes two version number strings and compares them.
        /// If V1 or V2 invalid format then the valid string is returned as being greater.
        /// </summary>
        private static int CompareVersions(string sV1, string sV2)
        {
            ushort[] v1 = new ushort[4];
            ushort[] v2 = new ushort[4];

            bool fV1OK = ParseVersion(sV1, v1);
            bool fV2OK = ParseVersion(sV2, v2);

            if (!fV1OK && !fV2OK)
            {
                return 0;
            }
            else if (fV1OK && !fV2OK)
            {
                return 1;
            }
            else if (!fV1OK && fV2OK)
            {
                return -1;
            }
            else
            {
                for (int ul = 0; ul < 4; ul++)
                {
                    if (v1[ul] > v2[ul])
                    {
                        return 1;
                    }
                    else if (v1[ul] < v2[ul])
                    {
                        return -1;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Takes a version number string, checks it is valid, and fills the four
        /// values in the Version array. Valid version stings are "a[.b[.c[.d]]]",
        /// where a,b,c,d are +ve integers, 0 . 9999. If b,c,d are missing those
        /// version values are set as zero.
        /// </summary>
        private static bool ParseVersion(string s, ushort[] Version)
        {
            bool fIsValid = true;
            Version[0] = Version[1] = Version[2] = Version[3] = 0;

            if (string.IsNullOrEmpty(s))
            {
                fIsValid = false;
            }
            else
            {
                int iPosPrev = 0;
                for (int i = 0; i < 4 && iPosPrev < s.Length; i++)
                {
                    int iPosDot = s.IndexOf('.', iPosPrev);

                    // read +ve integer
                    string sInteger = s.Substring(iPosPrev, iPosDot);
                    ushort val;

                    if (!ushort.TryParse(sInteger, out val) || val > 9999)
                    {
                        fIsValid = false;
                        break;
                    }
                    Version[i] = val;

                    iPosPrev = iPosDot + 1;
                }

                if (fIsValid && iPosPrev != s.Length)
                {
                    fIsValid = false;
                }
            }
            return fIsValid;
        }

        private static ObjectToken GetHighestTokenVersion(ObjectToken token, ObjectToken tokenSeed, string[] criterias)
        {
            // if override and higher version - new preferred.
            bool fOverride = tokenSeed.MatchesAttributes(criterias);

            if (fOverride)
            {
                bool fDidCompare;
                int lRes = CompareTokenVersions(tokenSeed, token, out fDidCompare);

                if (fDidCompare && lRes > 0)
                {
                    token = tokenSeed;
                }
            }
            return token;
        }

        #endregion

        #region private Fields

        private const string _defaultTokenIdValueName = "DefaultTokenId";

        private static readonly string[] s_asVersionDefault = new string[] { "VersionDefault" };

        #endregion
    }
}
