// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace System.Speech.Internal
{
    internal enum AlphabetType
    {
        Sapi,
        Ipa,
        Ups
    }

    /// <summary>
    /// This class allows conversion between SAPI and IPA phonemes.
    /// Objects of this class are not thread safe for modifying state.
    /// </summary>
    internal class AlphabetConverter
    {
        #region Constructors

        internal AlphabetConverter(int langId)
        {
            _currentLangId = -1;
            SetLanguageId(langId);
        }

        #endregion

        #region internal Methods

        /// <summary>
        /// Convert from SAPI phonemes to IPA phonemes.
        /// </summary>
        /// <returns>
        /// Return an array of unicode characters each of which represents an IPA phoneme if the SAPI phonemes are valid.
        /// Otherwise, return null.
        /// </returns>
        internal char[] SapiToIpa(char[] phonemes)
        {
            return Convert(phonemes, true);
        }

        /// <summary>
        /// Convert from IPA phonemes to SAPI phonemes.
        /// </summary>
        /// Return an array of unicode characters each of which represents a SAPI phoneme if the IPA phonemes are valid.
        /// Otherwise, return null.
        internal char[] IpaToSapi(char[] phonemes)
        {
            return Convert(phonemes, false);
        }

        /// <summary>
        /// Determines whether a given string of SAPI ids can be potentially converted using a single
        /// conversion unit, that is, a prefix of some convertible string.
        /// </summary>
        /// <param name="phonemes">The string of SAPI or UPS phoneme ids</param>
        /// <param name="isSapi">To indicate whether parameter phonemes is in SAPI or UPS phonemes</param>
        internal bool IsPrefix(string phonemes, bool isSapi)
        {
            if (_phoneMap == null)
                return false;

            return _phoneMap.IsPrefix(phonemes, isSapi);
        }

        internal bool IsConvertibleUnit(string phonemes, bool isSapi)
        {
            if (_phoneMap == null)
                return false;

            return _phoneMap.ConvertPhoneme(phonemes, isSapi) != null;
        }

        internal int SetLanguageId(int langId)
        {
            if (langId < 0)
            {
                throw new ArgumentException(SR.Get(SRID.MustBeGreaterThanZero), nameof(langId));
            }
            if (langId == _currentLangId)
            {
                return _currentLangId;
            }

            int i;
            int oldLangId = _currentLangId;
            for (i = 0; i < s_langIds.Length; i++)
            {
                if (s_langIds[i] == langId)
                {
                    break;
                }
            }
            if (i == s_langIds.Length)
            {
                //Debug.Fail($"No phoneme map for LCID {langId}, maps exist for {string.Join(',', s_langIds)}\n");
                _currentLangId = langId;
                _phoneMap = null;
            }
            else
            {
                lock (s_staticLock)
                {
                    if (s_phoneMaps[i] == null)
                    {
                        s_phoneMaps[i] = CreateMap(s_resourceNames[i]);
                    }
                    _phoneMap = s_phoneMaps[i];
                    _currentLangId = langId;
                }
            }
            return oldLangId;
        }
        #endregion

        #region Private Methods

        private char[] Convert(char[] phonemes, bool isSapi)
        {
            // If the phoneset of the selected language is UPS anyway, that is phone mapping is unnecessary,
            // we return the same phoneme string. But we still need to make a copy.
            if (_phoneMap == null || phonemes.Length == 0)
            {
                return (char[])phonemes.Clone();
            }

            //
            // We break the phoneme string into substrings of phonemes, each of which is directly convertible from
            // the mapping table. If there is ambiguity, we always choose the largest substring as we go from left
            // to right.
            //
            // In order to do this, we check whether a given substring is a potential prefix of a convertible substring.
            //

            StringBuilder result = new();
            int startIndex; // Starting index of a substring being considered
            int endIndex;   // The ending index of the last convertible substring
            string token;           // Holds a substring of phonemes that are directly convertible from the mapping table.
            string lastConvert;     // Holds last convertible substring, starting from startIndex.

            string tempConvert;
            string source = new(phonemes);
            int i;

            lastConvert = null;
            startIndex = i = 0;
            endIndex = -1;

            while (i < source.Length)
            {
                token = source.Substring(startIndex, i - startIndex + 1);
                if (_phoneMap.IsPrefix(token, isSapi))
                {
                    tempConvert = _phoneMap.ConvertPhoneme(token, isSapi);
                    // Note we may have an empty string for conversion result here
                    if (tempConvert != null)
                    {
                        lastConvert = tempConvert;
                        endIndex = i;
                    }
                }
                else
                {
                    // If we have not had a convertible substring, the input is not convertible.
                    if (lastConvert == null)
                    {
                        break;
                    }
                    else
                    {
                        // Use the converted substring, and start over from the last convertible position.
                        result.Append(lastConvert);
                        i = endIndex;
                        startIndex = endIndex + 1;
                        lastConvert = null;
                    }
                }
                i++;
            }

            if (lastConvert != null && endIndex == phonemes.Length - 1)
            {
                result.Append(lastConvert);
            }
            else
            {
                return null;
            }

            return result.ToString().ToCharArray();
        }

        private PhoneMapData CreateMap(string resourceName)
        {
            Assembly assembly = Assembly.GetAssembly(GetType());
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new FileLoadException(SR.Get(SRID.CannotLoadResourceFromManifest, resourceName, assembly.FullName));
            }
            return new PhoneMapData(new BufferedStream(stream));
        }

        #endregion

        #region Private Fields

        private int _currentLangId;
        private PhoneMapData _phoneMap;

        private static int[] s_langIds = new int[] { 0x804, 0x404, 0x407, 0x409, 0x40A, 0x40C, 0x411 };
        private static string[] s_resourceNames =
                    new string[] { "upstable_chs.upsmap", "upstable_cht.upsmap", "upstable_deu.upsmap", "upstable_enu.upsmap",
                                   "upstable_esp.upsmap", "upstable_fra.upsmap", "upstable_jpn.upsmap",
};
        private static PhoneMapData[] s_phoneMaps = new PhoneMapData[7];
        private static object s_staticLock = new();

        #endregion

        #region Private Type

        internal class PhoneMapData
        {
            private sealed class ConversionUnit
            {
                public string sapi;
                public string ups;
                public bool isDefault;
            }

            internal PhoneMapData(Stream input)
            {
                using (BinaryReader reader = new(input, System.Text.Encoding.Unicode))
                {
                    int size = reader.ReadInt32();
                    _convertTable = new ConversionUnit[size];
                    int i;
                    for (i = 0; i < size; i++)
                    {
                        _convertTable[i] = new ConversionUnit
                        {
                            sapi = ReadPhoneString(reader),
                            ups = ReadPhoneString(reader),
                            isDefault = reader.ReadInt32() != 0 ? true : false
                        };
                    }

                    _prefixSapiTable = InitializePrefix(true);
                    _prefixUpsTable = InitializePrefix(false);
                }
            }

            internal bool IsPrefix(string prefix, bool isSapi)
            {
                if (isSapi)
                {
                    return _prefixSapiTable.ContainsKey(prefix);
                }
                else
                {
                    return _prefixUpsTable.ContainsKey(prefix);
                }
            }

            internal string ConvertPhoneme(string phoneme, bool isSapi)
            {
                ConversionUnit unit;
                if (isSapi)
                {
                    unit = (ConversionUnit)_prefixSapiTable[phoneme];
                }
                else
                {
                    unit = (ConversionUnit)_prefixUpsTable[phoneme];
                }
                if (unit == null)
                {
                    return null;
                }
                return isSapi ? unit.ups : unit.sapi;
            }

            /// <summary>
            /// Create a hash table of all possible prefix substrings for each ConversionUnit
            /// </summary>
            /// <param name="isSapi">Creating a SAPI or UPS prefix table</param>
            private Hashtable InitializePrefix(bool isSapi)
            {
                int i, j;
                Hashtable prefixTable = Hashtable.Synchronized(new Hashtable());
                string from, key;
                for (i = 0; i < _convertTable.Length; i++)
                {
                    if (isSapi)
                    {
                        from = _convertTable[i].sapi;
                    }
                    else
                    {
                        from = _convertTable[i].ups;
                    }

                    for (j = 0; j + 1 < from.Length; j++)
                    {
                        key = from.Substring(0, j + 1);
                        if (!prefixTable.ContainsKey(key))
                        {
                            prefixTable[key] = null;
                        }
                    }

                    if (_convertTable[i].isDefault || prefixTable[from] == null)
                    {
                        prefixTable[from] = _convertTable[i];
                    }
                }
                return prefixTable;
            }

            private static string ReadPhoneString(BinaryReader reader)
            {
                int phoneLength;
                char[] phoneString;
                phoneLength = reader.ReadInt16() / 2;
                phoneString = reader.ReadChars(phoneLength);
                return new string(phoneString, 0, phoneLength - 1);
            }

            private Hashtable _prefixSapiTable, _prefixUpsTable;
            private ConversionUnit[] _convertTable;
        }

        #endregion
    }
}
