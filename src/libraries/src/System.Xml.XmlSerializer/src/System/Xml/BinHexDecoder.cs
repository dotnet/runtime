// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Xml.Serialization;

namespace System.Xml
{
    internal abstract class IncrementalReadDecoder
    {
        internal abstract int DecodedCount { get; }
        internal abstract bool IsFull { get; }
        internal abstract void SetNextOutputBuffer(Array array, int offset, int len);
        internal abstract int Decode(char[] chars, int startPos, int len);
        internal abstract int Decode(string str, int startPos, int len);
        internal abstract void Reset();
    }

    internal class BinHexDecoder : IncrementalReadDecoder
    {
        //
        // Fields
        //
        private byte[] _buffer;
        private int _startIndex;
        private int _curIndex;
        private int _endIndex;
        private bool _hasHalfByteCached;
        private byte _cachedHalfByte;

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
            Debug.Assert(chars != null);
            Debug.Assert(len >= 0);
            Debug.Assert(startPos >= 0);
            Debug.Assert(chars.Length - startPos >= len);

            if (len == 0)
            {
                return 0;
            }
            int bytesDecoded, charsDecoded;
            fixed (char* pChars = &chars[startPos])
            {
                fixed (byte* pBytes = &_buffer[_curIndex])
                {
                    Decode(pChars, pChars + len, pBytes, pBytes + (_endIndex - _curIndex),
                            ref _hasHalfByteCached, ref _cachedHalfByte, out charsDecoded, out bytesDecoded);
                }
            }
            _curIndex += bytesDecoded;
            return charsDecoded;
        }

        internal override unsafe int Decode(string str, int startPos, int len)
        {
            Debug.Assert(str != null);
            Debug.Assert(len >= 0);
            Debug.Assert(startPos >= 0);
            Debug.Assert(str.Length - startPos >= len);

            if (len == 0)
            {
                return 0;
            }
            int bytesDecoded, charsDecoded;
            fixed (char* pChars = str)
            {
                fixed (byte* pBytes = &_buffer[_curIndex])
                {
                    Decode(pChars + startPos, pChars + startPos + len, pBytes, pBytes + (_endIndex - _curIndex),
                            ref _hasHalfByteCached, ref _cachedHalfByte, out charsDecoded, out bytesDecoded);
                }
            }
            _curIndex += bytesDecoded;
            return charsDecoded;
        }

        internal override void Reset()
        {
            _hasHalfByteCached = false;
            _cachedHalfByte = 0;
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
        // Static methods
        //
        public static unsafe byte[] Decode(char[] chars, bool allowOddChars)
        {
            if (chars == null)
            {
                throw new ArgumentNullException(nameof(chars));
            }

            int len = chars.Length;
            if (len == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] bytes = new byte[(len + 1) / 2];
            int bytesDecoded, charsDecoded;
            bool hasHalfByteCached = false;
            byte cachedHalfByte = 0;

            fixed (char* pChars = &chars[0])
            {
                fixed (byte* pBytes = &bytes[0])
                {
                    Decode(pChars, pChars + len, pBytes, pBytes + bytes.Length, ref hasHalfByteCached, ref cachedHalfByte, out charsDecoded, out bytesDecoded);
                }
            }

            if (hasHalfByteCached && !allowOddChars)
            {
                throw new XmlException(SR.Format(SR.Xml_InvalidBinHexValueOddCount, new string(chars)));
            }

            if (bytesDecoded < bytes.Length)
            {
                byte[] tmp = new byte[bytesDecoded];
                Buffer.BlockCopy(bytes, 0, tmp, 0, bytesDecoded);
                bytes = tmp;
            }

            return bytes;
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
                { // else if ( xmlCharType.IsWhiteSpace( ch ) ) {
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
