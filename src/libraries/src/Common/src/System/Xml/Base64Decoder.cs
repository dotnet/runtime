// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Xml
{
    internal class Base64Decoder : IncrementalReadDecoder
    {
        //
        // Fields
        //
        byte[] buffer;
        int startIndex;
        int curIndex;
        int endIndex;

        int bits;
        int bitsFilled;

        private const string CharsBase64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        private static readonly byte[] MapBase64 = ConstructMapBase64();
        private const int MaxValidChar = (int)'z';
        private const byte Invalid = unchecked((byte)-1);

        //
        // IncrementalReadDecoder interface
        //
        internal override int DecodedCount
        {
            get
            {
                return curIndex - startIndex;
            }
        }

        internal override bool IsFull
        {
            get
            {
                return curIndex == endIndex;
            }
        }

        internal override unsafe int Decode(char[] chars, int startPos, int len)
        {
            if (chars == null)
            {
                throw new ArgumentNullException("chars");
            }
            if (len < 0)
            {
                throw new ArgumentOutOfRangeException("len");
            }
            if (startPos < 0)
            {
                throw new ArgumentOutOfRangeException("startPos");
            }
            if (chars.Length - startPos < len)
            {
                throw new ArgumentOutOfRangeException("len");
            }

            if (len == 0)
            {
                return 0;
            }
            int bytesDecoded, charsDecoded;
            fixed (char* pChars = &chars[startPos])
            {
                fixed (byte* pBytes = &buffer[curIndex])
                {
                    Decode(pChars, pChars + len, pBytes, pBytes + (endIndex - curIndex), out charsDecoded, out bytesDecoded);
                }
            }
            curIndex += bytesDecoded;
            return charsDecoded;
        }

        internal override unsafe int Decode(string str, int startPos, int len)
        {
            if (str == null)
            {
                throw new ArgumentNullException("str");
            }
            if (len < 0)
            {
                throw new ArgumentOutOfRangeException("len");
            }
            if (startPos < 0)
            {
                throw new ArgumentOutOfRangeException("startPos");
            }
            if (str.Length - startPos < len)
            {
                throw new ArgumentOutOfRangeException("len");
            }

            if (len == 0)
            {
                return 0;
            }
            int bytesDecoded, charsDecoded;
            fixed (char* pChars = str)
            {
                fixed (byte* pBytes = &buffer[curIndex])
                {
                    Decode(pChars + startPos, pChars + startPos + len, pBytes, pBytes + (endIndex - curIndex), out charsDecoded, out bytesDecoded);
                }
            }
            curIndex += bytesDecoded;
            return charsDecoded;
        }

        internal override void Reset()
        {
            bitsFilled = 0;
            bits = 0;
        }

        internal override void SetNextOutputBuffer(byte[] buffer, int index, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(count >= 0);
            Debug.Assert(index >= 0);
            Debug.Assert(buffer.Length - index >= count);
            Debug.Assert((buffer as byte[]) != null);

            this.buffer = (byte[])buffer;
            this.startIndex = index;
            this.curIndex = index;
            this.endIndex = index + count;
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
            for (int i = 0; i < CharsBase64.Length; i++)
            {
                mapBase64[(int)CharsBase64[i]] = (byte)i;
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
            int b = bits;
            int bFilled = bitsFilled;
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

                // ignore white space
                if ((xmlCharType.charProperties[ch] & XmlCharType.fWhitespace) != 0)
                {
                    continue;
                }

                int digit;
                if (ch > 122 || (digit = MapBase64[ch]) == Invalid)
                {
                    throw new XmlException(SR.Format(SR.Xml_InvalidBase64Value, new string(pChars, 0, (int)(pCharsEndPos - pChars))));
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
                        if (!((xmlCharType.charProperties[*pChar++] & XmlCharType.fWhitespace) != 0))
                        {
                            throw new XmlException(SR.Format(SR.Xml_InvalidBase64Value, new string(pChars, 0, (int)(pCharsEndPos - pChars))));
                        }
                    } while (pChar < pCharsEndPos);
                }
            }

        Return:
            bits = b;
            bitsFilled = bFilled;

            bytesDecoded = (int)(pByte - pBytes);
            charsDecoded = (int)(pChar - pChars);
        }
    }
}
