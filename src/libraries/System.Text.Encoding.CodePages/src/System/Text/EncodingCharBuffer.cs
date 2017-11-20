// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text
{
    internal class EncodingCharBuffer
    {
        private unsafe char* _chars;
        private unsafe char* _charStart;
        private unsafe char* _charEnd;
        private int _charCountResult = 0;
        private EncodingNLS _enc;
        private DecoderNLS _decoder;
        private unsafe byte* _byteStart;
        private unsafe byte* _byteEnd;
        private unsafe byte* _bytes;
        private DecoderFallbackBuffer _fallbackBuffer;
        private DecoderFallbackBufferHelper _fallbackBufferHelper;

        internal unsafe EncodingCharBuffer(EncodingNLS enc, DecoderNLS decoder, char* charStart, int charCount, byte* byteStart, int byteCount)
        {
            _enc = enc;
            _decoder = decoder;

            _chars = charStart;
            _charStart = charStart;
            _charEnd = charStart + charCount;

            _byteStart = byteStart;
            _bytes = byteStart;
            _byteEnd = byteStart + byteCount;

            if (_decoder == null)
                _fallbackBuffer = enc.DecoderFallback.CreateFallbackBuffer();
            else
                _fallbackBuffer = _decoder.FallbackBuffer;

            // If we're getting chars or getting char count we don't expect to have
            // to remember fallbacks between calls (so it should be empty)
            Debug.Assert(_fallbackBuffer.Remaining == 0,
                "[Encoding.EncodingCharBuffer.EncodingCharBuffer]Expected empty fallback buffer for getchars/charcount");
            _fallbackBufferHelper = new DecoderFallbackBufferHelper(_fallbackBuffer);
            _fallbackBufferHelper.InternalInitialize(_bytes, _charEnd);
        }

        internal unsafe bool AddChar(char ch, int numBytes)
        {
            if (_chars != null)
            {
                if (_chars >= _charEnd)
                {
                    // Throw maybe
                    _bytes -= numBytes;                                        // Didn't encode these bytes
                    _enc.ThrowCharsOverflow(_decoder, _bytes <= _byteStart);    // Throw?
                    return false;                                           // No throw, but no store either
                }

                *(_chars++) = ch;
            }
            _charCountResult++;
            return true;
        }

        internal unsafe bool AddChar(char ch)
        {
            return AddChar(ch, 1);
        }


        internal unsafe bool AddChar(char ch1, char ch2, int numBytes)
        {
            // Need room for 2 chars
            if (_chars >= _charEnd - 1)
            {
                // Throw maybe
                _bytes -= numBytes;                                        // Didn't encode these bytes
                _enc.ThrowCharsOverflow(_decoder, _bytes <= _byteStart);    // Throw?
                return false;                                           // No throw, but no store either
            }
            return AddChar(ch1, numBytes) && AddChar(ch2, numBytes);
        }

        internal unsafe void AdjustBytes(int count)
        {
            _bytes = unchecked(_bytes + count);
        }

        internal unsafe bool MoreData
        {
            get
            {
                return _bytes < _byteEnd;
            }
        }

        // Do we have count more bytes?
        internal unsafe bool EvenMoreData(int count)
        {
            return (_bytes <= _byteEnd - count);
        }

        // GetNextByte shouldn't be called unless the caller's already checked more data or even more data,
        // but we'll double check just to make sure.
        internal unsafe byte GetNextByte()
        {
            Debug.Assert(_bytes < _byteEnd, "[EncodingCharBuffer.GetNextByte]Expected more date");
            if (_bytes >= _byteEnd)
                return 0;
            return *(_bytes++);
        }

        internal unsafe int BytesUsed
        {
            get
            {
                return unchecked((int)(_bytes - _byteStart));
            }
        }

        internal unsafe bool Fallback(byte fallbackByte)
        {
            // Build our buffer
            byte[] byteBuffer = new byte[] { fallbackByte };

            // Do the fallback and add the data.
            return Fallback(byteBuffer);
        }

        internal unsafe bool Fallback(byte byte1, byte byte2)
        {
            // Build our buffer
            byte[] byteBuffer = new byte[] { byte1, byte2 };

            // Do the fallback and add the data.
            return Fallback(byteBuffer);
        }

        internal unsafe bool Fallback(byte byte1, byte byte2, byte byte3, byte byte4)
        {
            // Build our buffer
            byte[] byteBuffer = new byte[] { byte1, byte2, byte3, byte4 };

            // Do the fallback and add the data.
            return Fallback(byteBuffer);
        }

        internal unsafe bool Fallback(byte[] byteBuffer)
        {
            // Do the fallback and add the data.
            if (_chars != null)
            {
                char* pTemp = _chars;
                if (_fallbackBufferHelper.InternalFallback(byteBuffer, _bytes, ref _chars) == false)
                {
                    // Throw maybe
                    _bytes -= byteBuffer.Length;                             // Didn't use how many ever bytes we're falling back
                    _fallbackBufferHelper.InternalReset();                         // We didn't use this fallback.
                    _enc.ThrowCharsOverflow(_decoder, _chars == _charStart);    // Throw?
                    return false;                                           // No throw, but no store either
                }
                _charCountResult += unchecked((int)(_chars - pTemp));
            }
            else
            {
                _charCountResult += _fallbackBufferHelper.InternalFallback(byteBuffer, _bytes);
            }

            return true;
        }

        internal unsafe int Count
        {
            get
            {
                return _charCountResult;
            }
        }
    }
}
