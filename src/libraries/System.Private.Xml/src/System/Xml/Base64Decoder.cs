// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Xml
{
    internal sealed class Base64Decoder : IncrementalReadDecoder
    {
        //
        // Fields
        //
        private byte[]? _buffer;
        private int _startIndex;
        private int _curIndex;
        private int _endIndex;

        private int _bits;
        private int _bitsFilled;

        //
        // IncrementalReadDecoder interface
        //
        internal override int DecodedCount
        {
            get
            {
                return _curIndex - _startIndex;
            }
        }

        internal override bool IsFull
        {
            get
            {
                return _curIndex == _endIndex;
            }
        }

        internal override int Decode(char[] chars, int startPos, int len)
        {
            ArgumentNullException.ThrowIfNull(chars);

            ArgumentOutOfRangeException.ThrowIfNegative(len);
            ArgumentOutOfRangeException.ThrowIfNegative(startPos);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(len, chars.Length - startPos);

            if (len == 0)
            {
                return 0;
            }

            Decode(chars.AsSpan(startPos, len), _buffer.AsSpan(_curIndex, _endIndex - _curIndex), out int charsDecoded, out int bytesDecoded);

            _curIndex += bytesDecoded;
            return charsDecoded;
        }

        internal override int Decode(string str, int startPos, int len)
        {
            ArgumentNullException.ThrowIfNull(str);

            ArgumentOutOfRangeException.ThrowIfNegative(len);
            ArgumentOutOfRangeException.ThrowIfNegative(startPos);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(len, str.Length - startPos);

            if (len == 0)
            {
                return 0;
            }

            Decode(str.AsSpan(startPos, len), _buffer.AsSpan(_curIndex, _endIndex - _curIndex), out int charsDecoded, out int bytesDecoded);

            _curIndex += bytesDecoded;
            return charsDecoded;
        }

        internal override void Reset()
        {
            _bitsFilled = 0;
            _bits = 0;
        }

        internal override void SetNextOutputBuffer(Array buffer, int index, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(count >= 0);
            Debug.Assert(index >= 0);
            Debug.Assert(buffer.Length - index >= count);
            Debug.Assert((buffer as byte[]) != null);

            _buffer = (byte[])buffer;
            _startIndex = index;
            _curIndex = index;
            _endIndex = index + count;
        }

        //
        // Private methods
        //

        private void Decode(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsDecoded, out int bytesDecoded)
        {
            // walk hex digits pairing them up and shoving the value of each pair into a byte
            int iByte = 0;
            int iChar = 0;
            int b = _bits;
            int bFilled = _bitsFilled;

            const byte Invalid = 255;
            ReadOnlySpan<byte> mapBase64 = // 123
            [
                255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 62,  255, 255, 255, 63,
                52,  53,  54,  55,  56,  57,  58,  59,  60,  61,  255, 255, 255, 255, 255, 255,
                255, 0,   1,   2,   3,   4,   5,   6,   7,   8,   9,   10,  11,  12,  13,  14,
                15,  16,  17,  18,  19,  20,  21,  22,  23,  24,  25,  255, 255, 255, 255, 255,
                255, 26,  27,  28,  29,  30,  31,  32,  33,  34,  35,  36,  37,  38,  39,  40,
                41,  42,  43,  44,  45,  46,  47,  48,  49,  50,  51,
            ];

            while ((uint)iChar < (uint)chars.Length)
            {
                if ((uint)iByte >= (uint)bytes.Length)
                {
                    break; // ran out of space in the destination buffer
                }

                char ch = chars[iChar];
                // end?
                if (ch == '=')
                {
                    break;
                }
                iChar++;

                // ignore whitespace
                if (XmlCharType.IsWhiteSpace(ch))
                {
                    continue;
                }

                int digit;
                if (ch >= mapBase64.Length || (digit = mapBase64[ch]) == Invalid)
                {
                    throw new XmlException(SR.Xml_InvalidBase64Value, chars.ToString());
                }

                b = (b << 6) | digit;
                bFilled += 6;

                if (bFilled >= 8)
                {
                    // get top eight valid bits
                    bytes[iByte++] = (byte)((b >> (bFilled - 8)) & 0xFF);
                    bFilled -= 8;

                    if (iByte == bytes.Length)
                    {
                        goto Return;
                    }
                }
            }

            if ((uint)iChar < (uint)chars.Length && chars[iChar] == '=')
            {
                bFilled = 0;
                // ignore padding chars
                do
                {
                    iChar++;
                } while ((uint)iChar < (uint)chars.Length && chars[iChar] == '=');

                // ignore whitespace after the padding chars
                while ((uint)iChar < (uint)chars.Length)
                {
                    if (!XmlCharType.IsWhiteSpace(chars[iChar++]))
                    {
                        throw new XmlException(SR.Xml_InvalidBase64Value, chars.ToString());
                    }
                }
            }

        Return:
            _bits = b;
            _bitsFilled = bFilled;

            bytesDecoded = iByte;
            charsDecoded = iChar;
        }
    }
}
