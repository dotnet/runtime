// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Speech.Internal
{
    internal class StringBlob
    {
        #region Constructors

        internal StringBlob()
        {
        }

        internal StringBlob(char[] pszStringArray)
        {
            int cch = pszStringArray.Length;
            if (cch > 0)
            {
                // First string is always empty.
                if (pszStringArray[0] != 0)
                {
                    throw new FormatException(SR.Get(SRID.RecognizerInvalidBinaryGrammar));
                }

                // First pass to copy data and count strings.
                for (int iPos = 1, iEnd = cch, iStart = 1; iPos < iEnd; iPos++)
                {
                    if (pszStringArray[iPos] == '\0')
                    {
                        string sWord = new(pszStringArray, iStart, iPos - iStart);
                        _refStrings.Add(sWord);
                        _offsetStrings.Add(_totalStringSizes);
                        _strings.Add(sWord, ++_cWords);
                        _totalStringSizes += sWord.Length + 1;
                        iStart = iPos + 1;
                    }
                }
            }
        }

        #endregion

        #region internal Methods

        //
        //  The ID for a null string is always 0, the ID for subsequent strings is the
        //  index of the string + 1;
        //
        internal int Add(string psz, out int idWord)
        {
            int offset = 0;
            idWord = 0;
            if (!string.IsNullOrEmpty(psz))
            {
                // Check if the string is already in the table
                if (!_strings.TryGetValue(psz, out idWord))
                {
                    System.Diagnostics.Debug.Assert(_strings.Count == _refStrings.Count);

                    // No add it to the string table
                    idWord = ++_cWords;
                    offset = _totalStringSizes;
                    _refStrings.Add(psz);
                    _offsetStrings.Add(offset);
                    _strings.Add(psz, _cWords);
                    _totalStringSizes += psz.Length + 1;
                }
                else
                {
                    offset = OffsetFromId(idWord);
                }
            }

            return offset;
        }

        // Returns idWord; use IndexFromId to recover string offset
        internal int Find(string psz)
        {
            // Compatibility the SAPI version
            if (string.IsNullOrEmpty(psz) || _cWords == 0)
            {
                return 0;
            }

            // Use the dictionary to find the value
            int iWord;
            return _strings.TryGetValue(psz, out iWord) ? iWord : -1;
        }

        internal string this[int index]
        {
            get
            {
                if ((index < 1) || index > _cWords)
                {
                    throw new InvalidOperationException();
                }

                return _refStrings[index - 1];
            }
        }

        /// <summary>
        /// Only DEBUG code should use this
        /// </summary>
        internal string FromOffset(int offset)
        {
            int iPos = 1;
            int iWord = 1;

            System.Diagnostics.Debug.Assert(offset > 0);

            if (offset == 1 && _cWords >= 1)
            {
                return this[iWord];
            }

            foreach (string s in _refStrings)
            {
                iWord++;
                iPos += s.Length + 1;
                if (offset == iPos)
                {
                    return this[iWord];
                }
            }
            return null;
        }

        internal int StringSize()
        {
            return _cWords > 0 ? _totalStringSizes : 0;
        }

        internal int SerializeSize()
        {
            return ((StringSize() * _sizeOfChar + 3) & ~3) / 2;
        }

        internal char[] SerializeData()
        {
            // force a 0xcccc at the end of the buffer if the length is odd
            int iEnd = SerializeSize();

            char[] aData = new char[iEnd];

            // aData [0] is set by the framework to zero
            int iPos = 1;

            foreach (string s in _refStrings)
            {
                for (int i = 0; i < s.Length; i++)
                {
                    aData[iPos++] = s[i];
                }
                aData[iPos++] = '\0';
            }

            if (StringSize() % 2 == 1)
            {
                aData[iPos++] = (char)0xCCCC;
            }

            System.Diagnostics.Debug.Assert(iEnd == 0 || iPos == SerializeSize());

            return aData;
        }

        internal int OffsetFromId(int index)
        {
            System.Diagnostics.Debug.Assert(index <= _cWords);
            if (index > 0)
            {
                return _offsetStrings[index - 1];
            }

            return 0;
        }

        #endregion

        #region internal Properties

        internal int Count
        {
            get
            {
                return _cWords;
            }
        }

        #endregion

        #region Private Fields

        // List of words, end-to-end
        private Dictionary<string, int> _strings = new();

        // List of indices in the dictionary of words
        private List<string> _refStrings = new();

        // List of indices in the dictionary of words
        private List<int> _offsetStrings = new();

        // Number of words
        private int _cWords;

        // Cached value for the total string sizes - The first digit is always zero.
        private int _totalStringSizes = 1;

        // .NET is Unicode so 2 bytes per characters
        private const int _sizeOfChar = 2;

        #endregion
    }
}
