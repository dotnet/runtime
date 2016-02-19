// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Xml
{
    internal class BinHexDecoder : IncrementalReadDecoder
    {
        //
        // Fields
        //
        byte[] buffer;
        int startIndex;
        int curIndex;
        int endIndex;
        bool hasHalfByteCached;
        byte cachedHalfByte;

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
                fixed (byte* pBytes = &buffer[curIndex])
                {
                    Decode(pChars, pChars + len, pBytes, pBytes + (endIndex - curIndex),
                            ref this.hasHalfByteCached, ref this.cachedHalfByte, out charsDecoded, out bytesDecoded);
                }
            }
            curIndex += bytesDecoded;
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
                fixed (byte* pBytes = &buffer[curIndex])
                {
                    Decode(pChars + startPos, pChars + startPos + len, pBytes, pBytes + (endIndex - curIndex),
                            ref this.hasHalfByteCached, ref this.cachedHalfByte, out charsDecoded, out bytesDecoded);
                }
            }
            curIndex += bytesDecoded;
            return charsDecoded;
        }

        internal override void Reset()
        {
            this.hasHalfByteCached = false;
            this.cachedHalfByte = 0;
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

        private static unsafe void Decode(char* pChars, char* pCharsEndPos,
                                    byte* pBytes, byte* pBytesEndPos,
                                    ref bool hasHalfByteCached, ref byte cachedHalfByte,
                                    out int charsDecoded, out int bytesDecoded)
        {
#if DEBUG
            Debug.Assert(pCharsEndPos - pChars >= 0);
            Debug.Assert(pBytesEndPos - pBytes >= 0);
#endif

            char* pChar = pChars;
            byte* pByte = pBytes;
            XmlCharType xmlCharType = XmlCharType.Instance;
            while (pChar < pCharsEndPos && pByte < pBytesEndPos)
            {
                byte halfByte;
                char ch = *pChar++;

                if (ch >= 'a' && ch <= 'f')
                {
                    halfByte = (byte)(ch - 'a' + 10);
                }
                else if (ch >= 'A' && ch <= 'F')
                {
                    halfByte = (byte)(ch - 'A' + 10);
                }
                else if (ch >= '0' && ch <= '9')
                {
                    halfByte = (byte)(ch - '0');
                }
                else if ((xmlCharType.charProperties[ch] & XmlCharType.fWhitespace) != 0)
                {
                    continue;
                }
                else
                {
                    throw new XmlException(SR.Format(SR.Xml_InvalidBinHexValue, new string(pChars, 0, (int)(pCharsEndPos - pChars))));
                }

                if (hasHalfByteCached)
                {
                    *pByte++ = (byte)((cachedHalfByte << 4) + halfByte);
                    hasHalfByteCached = false;
                }
                else
                {
                    cachedHalfByte = halfByte;
                    hasHalfByteCached = true;
                }
            }

            bytesDecoded = (int)(pByte - pBytes);
            charsDecoded = (int)(pChar - pChars);
        }
    }
}
