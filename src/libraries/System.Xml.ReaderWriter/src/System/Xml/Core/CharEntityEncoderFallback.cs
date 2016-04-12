// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace System.Xml
{
    //
    // CharEntityEncoderFallback
    //
    internal class CharEntityEncoderFallback : EncoderFallback
    {
        private CharEntityEncoderFallbackBuffer _fallbackBuffer;

        private int[] _textContentMarks;
        private int _endMarkPos;
        private int _curMarkPos;
        private int _startOffset;

        internal CharEntityEncoderFallback()
        {
        }

        public override EncoderFallbackBuffer CreateFallbackBuffer()
        {
            if (_fallbackBuffer == null)
            {
                _fallbackBuffer = new CharEntityEncoderFallbackBuffer(this);
            }
            return _fallbackBuffer;
        }

        public override int MaxCharCount
        {
            get
            {
                return 12;
            }
        }

        internal int StartOffset
        {
            get
            {
                return _startOffset;
            }
            set
            {
                _startOffset = value;
            }
        }

        internal void Reset(int[] textContentMarks, int endMarkPos)
        {
            _textContentMarks = textContentMarks;
            _endMarkPos = endMarkPos;
            _curMarkPos = 0;
        }

        internal bool CanReplaceAt(int index)
        {
            int mPos = _curMarkPos;
            int charPos = _startOffset + index;
            while (mPos < _endMarkPos && charPos >= _textContentMarks[mPos + 1])
            {
                mPos++;
            }
            _curMarkPos = mPos;

            return (mPos & 1) != 0;
        }
    }

    //
    // CharEntityFallbackBuffer
    //
    internal class CharEntityEncoderFallbackBuffer : EncoderFallbackBuffer
    {
        private CharEntityEncoderFallback _parent;

        private string _charEntity = string.Empty;
        private int _charEntityIndex = -1;

        internal CharEntityEncoderFallbackBuffer(CharEntityEncoderFallback parent)
        {
            _parent = parent;
        }

        public override bool Fallback(char charUnknown, int index)
        {
            // If we are already in fallback, throw, it's probably at the suspect character in charEntity
            if (_charEntityIndex >= 0)
            {
                (new EncoderExceptionFallback()).CreateFallbackBuffer().Fallback(charUnknown, index);
            }

            // find out if we can replace the character with entity
            if (_parent.CanReplaceAt(index))
            {
                // Create the replacement character entity
                _charEntity = string.Format(CultureInfo.InvariantCulture, "&#x{0:X};", new object[] { (int)charUnknown });
                _charEntityIndex = 0;
                return true;
            }
            else
            {
                EncoderFallbackBuffer errorFallbackBuffer = (new EncoderExceptionFallback()).CreateFallbackBuffer();
                errorFallbackBuffer.Fallback(charUnknown, index);
                return false;
            }
        }

        public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
        {
            // check input surrogate pair
            if (!char.IsSurrogatePair(charUnknownHigh, charUnknownLow))
            {
                throw XmlConvert.CreateInvalidSurrogatePairException(charUnknownHigh, charUnknownLow);
            }

            // If we are already in fallback, throw, it's probably at the suspect character in charEntity
            if (_charEntityIndex >= 0)
            {
                (new EncoderExceptionFallback()).CreateFallbackBuffer().Fallback(charUnknownHigh, charUnknownLow, index);
            }

            if (_parent.CanReplaceAt(index))
            {
                // Create the replacement character entity
                _charEntity = string.Format(CultureInfo.InvariantCulture, "&#x{0:X};", new object[] { SurrogateCharToUtf32(charUnknownHigh, charUnknownLow) });
                _charEntityIndex = 0;
                return true;
            }
            else
            {
                EncoderFallbackBuffer errorFallbackBuffer = (new EncoderExceptionFallback()).CreateFallbackBuffer();
                errorFallbackBuffer.Fallback(charUnknownHigh, charUnknownLow, index);
                return false;
            }
        }

        public override char GetNextChar()
        {
            // The protocol using GetNextChar() and MovePrevious() called by Encoder is not well documented.
            // Here we have to signal to Encoder that the previous read was last character. Only AFTER we can 
            // mark ourself as done (-1). Otherwise MovePrevious() can still be called, but -1 is already incorrectly set
            // and return false from MovePrevious(). Then Encoder swallowing the rest of the bytes.
            if (_charEntityIndex == _charEntity.Length)
            {
                _charEntityIndex = -1;
            }
            if (_charEntityIndex == -1)
            {
                return (char)0;
            }
            else
            {
                Debug.Assert(_charEntityIndex < _charEntity.Length);
                char ch = _charEntity[_charEntityIndex++];
                return ch;
            }
        }

        public override bool MovePrevious()
        {
            if (_charEntityIndex == -1)
            {
                return false;
            }
            else
            {
                // Could be == length if just read the last character
                Debug.Assert(_charEntityIndex <= _charEntity.Length);
                if (_charEntityIndex > 0)
                {
                    _charEntityIndex--;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        public override int Remaining
        {
            get
            {
                if (_charEntityIndex == -1)
                {
                    return 0;
                }
                else
                {
                    return _charEntity.Length - _charEntityIndex;
                }
            }
        }

        public override void Reset()
        {
            _charEntityIndex = -1;
        }

        private int SurrogateCharToUtf32(char highSurrogate, char lowSurrogate)
        {
            return XmlCharType.CombineSurrogateChar(lowSurrogate, highSurrogate);
        }
    }
}
