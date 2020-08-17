// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Diagnostics;

namespace System.Xml
{
    internal class BinHexDecoder : IncrementalReadDecoder
    {
        //
        // Fields
        //
        private byte[]? _buffer;
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
                fixed (byte* pBytes = &_buffer![_curIndex])
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
                fixed (byte* pBytes = &_buffer![_curIndex])
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
                throw new XmlException(SR.Xml_InvalidBinHexValueOddCount, new string(chars));
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

                int val = HexConverter.FromChar(ch);
                if (val != 0xFF)
                {
                    halfByte = (byte)val;
                }
                else if (xmlCharType.IsWhiteSpace(ch))
                {
                    continue;
                }
                else
                {
                    throw new XmlException(SR.Xml_InvalidBinHexValue, new string(pChars, 0, (int)(pCharsEndPos - pChars)));
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
