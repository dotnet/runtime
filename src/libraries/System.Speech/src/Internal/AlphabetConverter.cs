// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Text;

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages.

namespace System.Speech.Internal
{
    internal enum AlphabetType
    {
        Sapi, Ipa, Ups
    }

    /// <summary>
    /// This class allows conversion between SAPI and IPA phonemes.
    /// Objects of this class are not thread safe for modifying state.
    /// </summary>
    internal class AlphabetConverter
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        internal AlphabetConverter (int langId)
        {
            _currentLangId = -1;
            SetLanguageId (langId);
        }

        #endregion

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region internal Methods

        /// <summary>
        /// Convert from SAPI phonemes to IPA phonemes.
        /// </summary>
        /// <param name="phonemes"></param>
        /// <returns>
        /// Return an array of unicode characters each of which represents an IPA phoneme if the SAPI phonemes are valid.
        /// Otherwise, return null.
        /// </returns>
        internal char [] SapiToIpa (char [] phonemes)
        {
            return Convert (phonemes, true);
        }

        /// <summary>
        /// Convert from IPA phonemes to SAPI phonemes.
        /// </summary>
        /// <param name="phonemes"></param>
        /// Return an array of unicode characters each of which represents a SAPI phoneme if the IPA phonemes are valid.
        /// Otherwise, return null.
        /// <returns></returns>
        internal char [] IpaToSapi (char [] phonemes)
        {
            return Convert (phonemes, false);
        }

        /// <summary>
        /// Determines whether a given string of SAPI ids can be potentially converted using a single
        /// conversion unit, that is, a prefix of some convertible string.
        /// </summary>
        /// <param name="phonemes">The string of SAPI or UPS phoneme ids</param>
        /// <param name="isSapi">To indicate whether parameter phonemes is in SAPI or UPS phonemes</param>
        /// <returns></returns>
        internal bool IsPrefix(string phonemes, bool isSapi)
        {
            return _phoneMap.IsPrefix(phonemes, isSapi);
        }

        internal bool IsConvertibleUnit(string phonemes, bool isSapi)
        {
            return _phoneMap.ConvertPhoneme(phonemes, isSapi) != null;
        }

        internal int SetLanguageId (int langId)
        {
            if (langId < 0)
            {
                throw new ArgumentException (SR.Get (SRID.MustBeGreaterThanZero), "langId");
            }
            if (langId == _currentLangId)
            {
                return _currentLangId;
            }

            int i;
            int oldLangId = _currentLangId;
            for (i = 0; i < _langIds.Length; i++)
            {
                if (_langIds [i] == langId)
                {
                    break;
                }
            }
            if (i == _langIds.Length)
            {
                _currentLangId = langId;
                _phoneMap = null;
            }
            else
            {
                lock (_staticLock)
                {
                    if (_phoneMaps [i] == null)
                    {
                        _phoneMaps [i] = CreateMap (_resourceNames [i]);
                    }
                    _phoneMap = _phoneMaps [i];
                    _currentLangId = langId;
                }

            }
            return oldLangId;
        }
        #endregion

        //*******************************************************************
        //
        // Private Methods
        //
        //*******************************************************************

        #region Private Methods

        private char [] Convert (char [] phonemes, bool isSapi)
        {
            // If the phoneset of the selected language is UPS anyway, that is phone mapping is unnecessary,
            // we return the same phoneme string. But we still need to make a copy.
            if (_phoneMap == null || phonemes.Length == 0)
            {
                return (char []) phonemes.Clone ();
            }

            //
            // We break the phoneme string into substrings of phonemes, each of which is directly convertible from
            // the mapping table. If there is ambiguity, we always choose the largest substring as we go from left
            // to right. 
            //
            // In order to do this, we check whether a given substring is a potential prefix of a convertible substring.
            //

            StringBuilder result = new StringBuilder ();
            int startIndex; // Starting index of a substring being considered
            int endIndex;   // The ending index of the last convertible substring
            String token;           // Holds a substring of phonemes that are directly convertible from the mapping table.
            String lastConvert;     // Holds last convertible substring, starting from startIndex.

            String tempConvert;
            String source = new String (phonemes);
            int i;

            lastConvert = null;
            startIndex = i = 0;
            endIndex = -1;

#pragma warning disable 56507
        
            while (i < source.Length)
            {
                token = source.Substring (startIndex, i - startIndex + 1);
                if (_phoneMap.IsPrefix (token, isSapi))
                {
                    tempConvert = _phoneMap.ConvertPhoneme (token, isSapi);
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
                        result.Append (lastConvert);
                        i = endIndex;
                        startIndex = endIndex + 1;
                        lastConvert = null;
                    }
                }
                i++;
            }

            if (lastConvert != null && endIndex == phonemes.Length - 1)
            {
                result.Append (lastConvert);
            }
            else
            {
                return null;
            }
#pragma warning restore 56507

            return result.ToString ().ToCharArray ();
        }

        private PhoneMapData CreateMap (string resourceName)
        {
            Assembly assembly = Assembly.GetAssembly (GetType ());
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new FileLoadException(SR.Get(SRID.CannotLoadResourceFromManifest, resourceName, assembly.FullName));
            }
            return new PhoneMapData (new BufferedStream (stream));
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private int _currentLangId;
        private PhoneMapData _phoneMap;

        private static int[] _langIds = new int[] { 0x804, 0x404, 0x407, 0x409, 0x40A, 0x40C, 0x411};
        private static String[] _resourceNames =
                    new String[] { "upstable_chs.upsmap", "upstable_cht.upsmap", "upstable_deu.upsmap", "upstable_enu.upsmap", 
                                   "upstable_esp.upsmap", "upstable_fra.upsmap", "upstable_jpn.upsmap", 

                                        };
        private static PhoneMapData [] _phoneMaps = new PhoneMapData [7];
        private static object _staticLock = new object ();

        #endregion

        //*******************************************************************
        //
        // Private Type
        //
        //*******************************************************************

        #region Private Type

        internal class PhoneMapData
        {
            private class ConversionUnit
            {
                public String sapi;
                public String ups;
                public bool isDefault;
            }

            internal PhoneMapData (Stream input)
            {
                using (BinaryReader reader = new BinaryReader (input, System.Text.Encoding.Unicode))
                {
                    int size = reader.ReadInt32 ();
                    convertTable = new ConversionUnit [size];
                    int i;
                    for (i = 0; i < size; i++)
                    {
                        convertTable [i] = new ConversionUnit ();
                        convertTable [i].sapi = ReadPhoneString (reader);
                        convertTable [i].ups = ReadPhoneString (reader);
                        convertTable [i].isDefault = reader.ReadInt32 () != 0 ? true : false;
                    }

                    prefixSapiTable = InitializePrefix (true);
                    prefixUpsTable = InitializePrefix (false);
                }
            }

            internal bool IsPrefix (string prefix, bool isSapi)
            {
                if (isSapi)
                {
                    return prefixSapiTable.ContainsKey (prefix);
                }
                else
                {
                    return prefixUpsTable.ContainsKey (prefix);
                }
            }

            internal string ConvertPhoneme (string phoneme, bool isSapi)
            {
                ConversionUnit unit;
                if (isSapi)
                {
                    unit = (ConversionUnit) prefixSapiTable [phoneme];
                }
                else
                {
                    unit = (ConversionUnit) prefixUpsTable [phoneme];
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
            /// <returns></returns>
            private Hashtable InitializePrefix (bool isSapi)
            {
                int i, j;
                Hashtable prefixTable = Hashtable.Synchronized (new Hashtable ());
                string from, key;
                for (i = 0; i < convertTable.Length; i++)
                {
                    if (isSapi)
                    {
                        from = convertTable [i].sapi;
                    }
                    else
                    {
                        from = convertTable [i].ups;
                    }

                    for (j = 0; j + 1 < from.Length; j++)
                    {
                        key = from.Substring (0, j + 1);
                        if (!prefixTable.ContainsKey (key))
                        {
                            prefixTable [key] = null;
                        }
                    }

                    if (convertTable [i].isDefault || prefixTable [from] == null)
                    {
                        prefixTable [from] = convertTable [i];
                    }
                }
                return prefixTable;
            }

            static private string ReadPhoneString (BinaryReader reader)
            {
                int phoneLength;
                char [] phoneString;
                phoneLength = reader.ReadInt16 () / 2;
                phoneString = reader.ReadChars (phoneLength);
                return new String (phoneString, 0, phoneLength - 1);
            }

            private Hashtable prefixSapiTable, prefixUpsTable;
            private ConversionUnit [] convertTable;
        }

        #endregion
    }
}
