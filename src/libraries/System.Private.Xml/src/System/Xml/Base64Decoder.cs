// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Xml
{
    internal class Base64Decoder : IncrementalReadDecoder
    {
        //
        // Fields
        //
        private byte[] _buffer;
        private int _startIndex;
        private int _curIndex;
        private int _endIndex;

        private int _bits;
        private int _bitsFilled;

        private static readonly String s_charsBase64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        private static readonly byte[] s_mapBase64 = ConstructMapBase64();
        private const int MaxValidChar = (int)'z';
        private const byte Invalid = unchecked((byte)-1);

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

        internal override unsafe int Decode(char[] chars, int startPos, int len)
        {
            if (chars == null)
            {
                throw new ArgumentNullException(nameof(chars));
            }
            if (len < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(len));
            }
            if (startPos < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startPos));
            }
            if (chars.Length - startPos < len)
            {
                throw new ArgumentOutOfRangeException(nameof(len));
            }

            if (len == 0)
            {
                return 0;
            }
            int bytesDecoded, charsDecoded;
            fixed (char* pChars = &chars[startPos])
            {
                fixed (byte* pBytes = &_buffer[_curIndex])
                {
                    Decode(pChars, pChars + len, pBytes, pBytes + (_endIndex - _curIndex), out charsDecoded, out bytesDecoded);
                }
            }
            _curIndex += bytesDecoded;
            return charsDecoded;
        }

        internal override unsafe int Decode(string str, int startPos, int len)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }
            if (len < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(len));
            }
            if (startPos < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startPos));
            }
            if (str.Length - startPos < len)
            {
                throw new ArgumentOutOfRangeException(nameof(len));
            }

            if (len == 0)
            {
                return 0;
            }
            int bytesDecoded, charsDecoded;
            fixed (char* pChars = str)
            {
                fixed (byte* pBytes = &_buffer[_curIndex])
                {
                    Decode(pChars + startPos, pChars + startPos + len, pBytes, pBytes + (_endIndex - _curIndex), out charsDecoded, out bytesDecoded);
                }
            }
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
        private static byte[] ConstructMapBase64()
        {
            byte[] mapBase64 = new byte[MaxValidChar + 1];
            for (int i = 0; i < mapBase64.Length; i++)
            {
                mapBase64[i] = Invalid;
            }
            for (int i = 0; i < s_charsBase64.Length; i++)
            {
                mapBase64[(int)s_charsBase64[i]] = (byte)i;
            }
            return mapBase64;
        }

        private unsafe void Decode(char* pChars, char* pCharsEndPos,
                             byte* pBytes, byte* pBytesEndPos,
                             out int charsDecoded, out int bytesDecoded)
        {
#if DEBUG
            Debug.Assert(pCharsEndPos - pChars >= 0);
            Debug.Assert(pBytesEndPos - pBytes >= 0);
#endif

            // walk hex digits pairing them up and shoving the value of each pair into a byte
            byte* pByte = pBytes;
            char* pChar = pChars;
            int b = _bits;
            int bFilled = _bitsFilled;
            XmlCharType xmlCharType = XmlCharType.Instance;
            while (pChar < pCharsEndPos && pByte < pBytesEndPos)
            {
                char ch = *pChar;
                // end?
                if (ch == '=')
                {
                    break;
                }
                pChar++;

                // ignore whitespace
                if (xmlCharType.IsWhiteSpace(ch))
                {
                    continue;
                }

                int digit;
                if (ch > 122 || (digit = s_mapBase64[ch]) == Invalid)
                {
                    throw new XmlException(SR.Xml_InvalidBase64Value, new string(pChars, 0, (int)(pCharsEndPos - pChars)));
                }

                b = (b << 6) | digit;
                bFilled += 6;

                if (bFilled >= 8)
                {
                    // get top eight valid bits
                    *pByte++ = (byte)((b >> (bFilled - 8)) & 0xFF);
                    bFilled -= 8;

                    if (pByte == pBytesEndPos)
                    {
                        goto Return;
                    }
                }
            }

            if (pChar < pCharsEndPos && *pChar == '=')
            {
                bFilled = 0;
                // ignore padding chars
                do
                {
                    pChar++;
                } while (pChar < pCharsEndPos && *pChar == '=');

                // ignore whitespace after the padding chars
                if (pChar < pCharsEndPos)
                {
                    do
                    {
                        if (!(xmlCharType.IsWhiteSpace(*pChar++)))
                        {
                            throw new XmlException(SR.Xml_InvalidBase64Value, new string(pChars, 0, (int)(pCharsEndPos - pChars)));
                        }
                    } while (pChar < pCharsEndPos);
                }
            }

        Return:
            _bits = b;
            _bitsFilled = bFilled;

            bytesDecoded = (int)(pByte - pBytes);
            charsDecoded = (int)(pChar - pChars);
        }
    }
}
