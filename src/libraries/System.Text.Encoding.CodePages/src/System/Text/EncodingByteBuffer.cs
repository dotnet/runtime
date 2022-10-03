// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text
{
    internal sealed class EncodingByteBuffer
    {
        private unsafe byte* _bytes;
        private readonly unsafe byte* _byteStart;
        private readonly unsafe byte* _byteEnd;
        private unsafe char* _chars;
        private readonly unsafe char* _charStart;
        private readonly unsafe char* _charEnd;
        private int _byteCountResult;
        private readonly EncodingNLS _enc;
        private readonly EncoderNLS? _encoder;
        internal EncoderFallbackBuffer fallbackBuffer;
        internal EncoderFallbackBufferHelper fallbackBufferHelper;

        internal unsafe EncodingByteBuffer(EncodingNLS inEncoding, EncoderNLS? inEncoder, byte* inByteStart, int inByteCount, char* inCharStart, int inCharCount)
        {
            _enc = inEncoding;
            _encoder = inEncoder;

            _charStart = inCharStart;
            _chars = inCharStart;
            _charEnd = inCharStart + inCharCount;

            _bytes = inByteStart;
            _byteStart = inByteStart;
            _byteEnd = inByteStart + inByteCount;

            if (_encoder == null)
                fallbackBuffer = _enc.EncoderFallback.CreateFallbackBuffer();
            else
            {
                fallbackBuffer = _encoder.FallbackBuffer;
                // If we're not converting we must not have data in our fallback buffer
                if (_encoder.m_throwOnOverflow && _encoder.InternalHasFallbackBuffer && fallbackBuffer.Remaining > 0)
                    throw new ArgumentException(SR.Format(SR.Argument_EncoderFallbackNotEmpty, _encoder.Encoding.EncodingName, _encoder.Fallback.GetType()));
            }
            fallbackBufferHelper = new EncoderFallbackBufferHelper(fallbackBuffer);
            fallbackBufferHelper.InternalInitialize(_chars, _charEnd, _encoder, _bytes != null);
        }

        internal unsafe bool AddByte(byte b, int moreBytesExpected)
        {
            Debug.Assert(moreBytesExpected >= 0, "[EncodingByteBuffer.AddByte]expected non-negative moreBytesExpected");
            if (_bytes != null)
            {
                if (_bytes >= _byteEnd - moreBytesExpected)
                {
                    // Throw maybe.  Check which buffer to back up (only matters if Converting)
                    MovePrevious(true);            // Throw if necessary
                    return false;                       // No throw, but no store either
                }

                *(_bytes++) = b;
            }
            _byteCountResult++;
            return true;
        }

        internal unsafe bool AddByte(byte b1)
        {
            return (AddByte(b1, 0));
        }

        internal unsafe bool AddByte(byte b1, byte b2)
        {
            return (AddByte(b1, b2, 0));
        }

        internal unsafe bool AddByte(byte b1, byte b2, int moreBytesExpected)
        {
            return (AddByte(b1, 1 + moreBytesExpected) && AddByte(b2, moreBytesExpected));
        }

        internal unsafe bool AddByte(byte b1, byte b2, byte b3)
        {
            return AddByte(b1, b2, b3, (int)0);
        }

        internal unsafe bool AddByte(byte b1, byte b2, byte b3, int moreBytesExpected)
        {
            return (AddByte(b1, 2 + moreBytesExpected) &&
                    AddByte(b2, 1 + moreBytesExpected) &&
                    AddByte(b3, moreBytesExpected));
        }

        internal unsafe bool AddByte(byte b1, byte b2, byte b3, byte b4)
        {
            return (AddByte(b1, 3) &&
                    AddByte(b2, 2) &&
                    AddByte(b3, 1) &&
                    AddByte(b4, 0));
        }

        internal unsafe void MovePrevious(bool bThrow)
        {
            if (fallbackBufferHelper.bFallingBack)
                fallbackBuffer.MovePrevious();                      // don't use last fallback
            else
            {
                Debug.Assert(_chars > _charStart || (bThrow && (_bytes == _byteStart)),
                    "[EncodingByteBuffer.MovePrevious]expected previous data or throw");
                if (_chars > _charStart)
                    _chars--;                                        // don't use last char
            }

            if (bThrow)
                _enc.ThrowBytesOverflow(_encoder, _bytes == _byteStart);    // Throw? (and reset fallback if not converting)
        }

        internal unsafe bool Fallback(char charFallback)
        {
            // Do the fallback
            return fallbackBufferHelper.InternalFallback(charFallback, ref _chars);
        }

        internal unsafe bool MoreData
        {
            get
            {
                // See if fallbackBuffer is not empty or if there's data left in chars buffer.
                return ((fallbackBuffer.Remaining > 0) || (_chars < _charEnd));
            }
        }

        internal unsafe char GetNextChar()
        {
            // See if there's something in our fallback buffer
            char cReturn = fallbackBufferHelper.InternalGetNextChar();

            // Nothing in the fallback buffer, return our normal data.
            if (cReturn == 0)
            {
                if (_chars < _charEnd)
                    cReturn = *(_chars++);
            }

            return cReturn;
        }

        internal unsafe int CharsUsed
        {
            get
            {
                return (int)(_chars - _charStart);
            }
        }

        internal unsafe int Count
        {
            get
            {
                return _byteCountResult;
            }
        }
    }
}
