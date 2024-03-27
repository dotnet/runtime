// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Xml
{
    internal sealed class CharEntityEncoderFallback : EncoderFallback
    {
        private CharEntityEncoderFallbackBuffer? _fallbackBuffer;
        private int[]? _textContentMarks;
        private int _endMarkPos;
        private int _curMarkPos;

        public override int MaxCharCount => 12;
        internal int StartOffset { get; set; }

        public override EncoderFallbackBuffer CreateFallbackBuffer()
        {
            return _fallbackBuffer ??= new CharEntityEncoderFallbackBuffer(this);
        }

        [MemberNotNull(nameof(_textContentMarks))]
        internal void Reset(int[] textContentMarks, int endMarkPos)
        {
            _textContentMarks = textContentMarks;
            _endMarkPos = endMarkPos;
            _curMarkPos = 0;
        }

        private bool CanReplaceAt(int index)
        {
            Debug.Assert(_textContentMarks != null);

            int mPos = _curMarkPos;
            int charPos = StartOffset + index;
            while (mPos < _endMarkPos && charPos >= _textContentMarks[mPos + 1])
            {
                mPos++;
            }
            _curMarkPos = mPos;

            return (mPos & 1) != 0;
        }


        private sealed class CharEntityEncoderFallbackBuffer : EncoderFallbackBuffer
        {
            private readonly CharEntityEncoderFallback _parent;

            private string _charEntity = string.Empty;
            private int _charEntityIndex = -1;

            internal CharEntityEncoderFallbackBuffer(CharEntityEncoderFallback parent)
            {
                _parent = parent;
            }

            public override int Remaining => _charEntityIndex == -1 ? 0 : _charEntity.Length - _charEntityIndex;

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
                    _charEntity = string.Create(null, stackalloc char[64], $"&#x{(int)charUnknown:X};");
                    _charEntityIndex = 0;
                    return true;
                }

                EncoderFallbackBuffer errorFallbackBuffer = (new EncoderExceptionFallback()).CreateFallbackBuffer();
                errorFallbackBuffer.Fallback(charUnknown, index);
                return false;
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
                    _charEntity = string.Create(null, stackalloc char[64], $"&#x{SurrogateCharToUtf32(charUnknownHigh, charUnknownLow):X};");
                    _charEntityIndex = 0;
                    return true;
                }

                EncoderFallbackBuffer errorFallbackBuffer = (new EncoderExceptionFallback()).CreateFallbackBuffer();
                errorFallbackBuffer.Fallback(charUnknownHigh, charUnknownLow, index);
                return false;
            }

            public override char GetNextChar()
            {
                // The protocol using GetNextChar() and MovePrevious() called by Encoder is not well documented.
                // Here we have to signal to Encoder that the previous read was last character. Only AFTER we can
                // mark our self as done (-1). Otherwise MovePrevious() can still be called, but -1 is already incorrectly set
                // and return false from MovePrevious(). Then Encoder swallowing the rest of the bytes.
                if (_charEntityIndex == _charEntity.Length)
                {
                    _charEntityIndex = -1;
                }

                if (_charEntityIndex == -1)
                {
                    return (char)0;
                }

                Debug.Assert(_charEntityIndex < _charEntity.Length);

                return _charEntity[_charEntityIndex++];
            }

            public override bool MovePrevious()
            {
                if (_charEntityIndex == -1)
                {
                    return false;
                }

                // Could be == length if just read the last character
                Debug.Assert(_charEntityIndex <= _charEntity.Length);

                if (_charEntityIndex > 0)
                {
                    _charEntityIndex--;

                    return true;
                }

                return false;
            }

            public override void Reset()
            {
                _charEntityIndex = -1;
            }

            private static int SurrogateCharToUtf32(char highSurrogate, char lowSurrogate)
            {
                return XmlCharType.CombineSurrogateChar(lowSurrogate, highSurrogate);
            }
        }
    }
}
