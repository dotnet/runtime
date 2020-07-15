// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Text.Unicode;

namespace System.Globalization
{
    public class TextElementEnumerator : IEnumerator
    {
        private readonly string _str;
        private readonly int _strStartIndex; // where in _str the enumeration should begin

        private int _currentTextElementOffset;
        private int _currentTextElementLength;
        private string? _currentTextElementSubstr;

        internal TextElementEnumerator(string str, int startIndex)
        {
            Debug.Assert(str != null, "TextElementEnumerator(): str != null");
            Debug.Assert(startIndex >= 0 && startIndex <= str.Length, "TextElementEnumerator(): startIndex >= 0 && startIndex <= str.Length");

            _str = str;
            _strStartIndex = startIndex;

            Reset();
        }

        public bool MoveNext()
        {
            _currentTextElementSubstr = null; // clear any cached substr

            int newOffset = _currentTextElementOffset + _currentTextElementLength;
            _currentTextElementOffset = newOffset; // advance
            _currentTextElementLength = 0; // prevent future calls to MoveNext() or get_Current from succeeding if we've hit end of data

            if (newOffset >= _str.Length)
            {
                return false; // reached the end of the data
            }

            _currentTextElementLength = TextSegmentationUtility.GetLengthOfFirstUtf16ExtendedGraphemeCluster(_str.AsSpan(newOffset));
            return true;
        }

        public object Current => GetTextElement();

        public string GetTextElement()
        {
            // Returned the cached substr if we've already generated it.
            // Otherwise perform the substr operation now.

            string? currentSubstr = _currentTextElementSubstr;
            if (currentSubstr is null)
            {
                if (_currentTextElementOffset >= _str.Length)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }

                currentSubstr = _str.Substring(_currentTextElementOffset, _currentTextElementLength);
                _currentTextElementSubstr = currentSubstr;
            }

            return currentSubstr;
        }

        public int ElementIndex
        {
            get
            {
                if (_currentTextElementOffset >= _str.Length)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }

                return _currentTextElementOffset - _strStartIndex;
            }
        }

        public void Reset()
        {
            // These first two fields are set to intentionally out-of-range values.
            // They'll be fixed up once the enumerator starts.

            _currentTextElementOffset = _str.Length;
            _currentTextElementLength = _strStartIndex - _str.Length;
            _currentTextElementSubstr = null;
        }
    }
}
