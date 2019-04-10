// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections;
using System.Diagnostics;

namespace System.Globalization
{
    public class TextElementEnumerator : IEnumerator
    {
        private readonly string _str;
        private int _index;
        private readonly int _startIndex;

                // This is the length of the total string, counting from the beginning of string.
        private readonly int _strLen;

        // The current text element lenght after MoveNext() is called.
        private int _currTextElementLen;

        private UnicodeCategory _uc;

        // The next abstract char to look at after MoveNext() is called.
        // It could be 1 or 2, depending on if it is a surrogate or not.
        private int _charLen;

        internal TextElementEnumerator(string str, int startIndex, int strLen)
        {
            Debug.Assert(str != null, "TextElementEnumerator(): str != null");
            Debug.Assert(startIndex >= 0 && strLen >= 0, "TextElementEnumerator(): startIndex >= 0 && strLen >= 0");
            Debug.Assert(strLen >= startIndex, "TextElementEnumerator(): strLen >= startIndex");
            _str = str;
            _startIndex = startIndex;
            _strLen = strLen;
            Reset();
        }

        public bool MoveNext()
        {
            if (_index >= _strLen)
            {
                // Make the _index to be greater than _strLen so that we can throw exception if GetTextElement() is called.
                _index = _strLen + 1;
                return false;
            }

            _currTextElementLen = StringInfo.GetCurrentTextElementLen(_str, _index, _strLen, ref _uc, ref _charLen);
            _index += _currTextElementLen;
            return true;
        }

        public object? Current => GetTextElement(); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/23268

        public string GetTextElement()
        {
            if (_index == _startIndex)
            {
                throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
            }
            if (_index > _strLen)
            {
                throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
            }

            return _str.Substring(_index - _currTextElementLen, _currTextElementLen);
        }

        public int ElementIndex
        {
            get
            {
                if (_index == _startIndex)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                }

                return _index - _currTextElementLen;
            }
        }

        public void Reset()
        {
            _index = _startIndex;
            if (_index < _strLen)
            {
                // If we have more than 1 character, get the category of the current char.
                _uc = CharUnicodeInfo.InternalGetUnicodeCategory(_str, _index, out _charLen);
            }
        }
    }
}
