// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Xml
{
    internal sealed class BinHexDecoder : IncrementalReadDecoder
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

        internal override int Decode(char[] chars, int startPos, int len)
        {
            ArgumentNullException.ThrowIfNull(chars);

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

            Decode(chars.AsSpan(startPos, len), _buffer.AsSpan(_curIndex, _endIndex - _curIndex),
                ref _hasHalfByteCached, ref _cachedHalfByte,
                out int charsDecoded, out int bytesDecoded);

            _curIndex += bytesDecoded;
            return charsDecoded;
        }

        internal override int Decode(string str, int startPos, int len)
        {
            ArgumentNullException.ThrowIfNull(str);

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

            Decode(str.AsSpan(startPos, len), _buffer.AsSpan(_curIndex, _endIndex - _curIndex),
                ref _hasHalfByteCached, ref _cachedHalfByte,
                out int charsDecoded, out int bytesDecoded);

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
        public static byte[] Decode(ReadOnlySpan<char> chars, bool allowOddChars)
        {
            int len = chars.Length;
            if (len == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] bytes = new byte[(len + 1) / 2];
            bool hasHalfByteCached = false;
            byte cachedHalfByte = 0;

            Decode(chars, bytes, ref hasHalfByteCached, ref cachedHalfByte, out _, out int bytesDecoded);

            if (hasHalfByteCached && !allowOddChars)
            {
                throw new XmlException(SR.Xml_InvalidBinHexValueOddCount, new string(chars));
            }

            if (bytesDecoded < bytes.Length)
            {
                Array.Resize(ref bytes, bytesDecoded);
            }

            return bytes;
        }

        //
        // Private methods
        //

        private static void Decode(ReadOnlySpan<char> chars,
                                   Span<byte> bytes,
                                   ref bool hasHalfByteCached, ref byte cachedHalfByte,
                                   out int charsDecoded, out int bytesDecoded)
        {
            int iByte = 0;
            int iChar = 0;

            for (; iChar < chars.Length; iChar++)
            {
                if ((uint)iByte >= (uint)bytes.Length)
                {
                    break; // ran out of space in the destination buffer
                }

                byte halfByte;
                char ch = chars[iChar];

                int val = HexConverter.FromChar(ch);
                if (val != 0xFF)
                {
                    halfByte = (byte)val;
                }
                else if (XmlCharType.IsWhiteSpace(ch))
                {
                    continue;
                }
                else
                {
                    throw new XmlException(SR.Xml_InvalidBinHexValue, chars.ToString());
                }

                if (hasHalfByteCached)
                {
                    bytes[iByte++] = (byte)((cachedHalfByte << 4) + halfByte);
                    hasHalfByteCached = false;
                }
                else
                {
                    cachedHalfByte = halfByte;
                    hasHalfByteCached = true;
                }
            }

            bytesDecoded = iByte;
            charsDecoded = iChar;
        }
    }
}
