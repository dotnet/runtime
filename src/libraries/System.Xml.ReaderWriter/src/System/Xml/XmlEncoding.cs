// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Diagnostics;

namespace System.Xml
{
    internal class UTF16Decoder : System.Text.Decoder
    {
        private bool _bigEndian;
        private int _lastByte;
        private const int CharSize = 2;

        public UTF16Decoder(bool bigEndian)
        {
            _lastByte = -1;
            _bigEndian = bigEndian;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return GetCharCount(bytes, index, count, false);
        }

        public override int GetCharCount(byte[] bytes, int index, int count, bool flush)
        {
            int byteCount = count + ((_lastByte >= 0) ? 1 : 0);
            if (flush && (byteCount % CharSize != 0))
            {
                throw new ArgumentException(SR.Format(SR.Enc_InvalidByteInEncoding, -1), (string)null);
            }
            return byteCount / CharSize;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            int charCount = GetCharCount(bytes, byteIndex, byteCount);

            if (_lastByte >= 0)
            {
                if (byteCount == 0)
                {
                    return charCount;
                }
                int nextByte = bytes[byteIndex++];
                byteCount--;

                chars[charIndex++] = _bigEndian
                    ? (char)(_lastByte << 8 | nextByte)
                    : (char)(nextByte << 8 | _lastByte);
                _lastByte = -1;
            }

            if ((byteCount & 1) != 0)
            {
                _lastByte = bytes[byteIndex + --byteCount];
            }

            // use the fast BlockCopy if possible
            if (_bigEndian == BitConverter.IsLittleEndian)
            {
                int byteEnd = byteIndex + byteCount;
                if (_bigEndian)
                {
                    while (byteIndex < byteEnd)
                    {
                        int hi = bytes[byteIndex++];
                        int lo = bytes[byteIndex++];
                        chars[charIndex++] = (char)(hi << 8 | lo);
                    }
                }
                else
                {
                    while (byteIndex < byteEnd)
                    {
                        int lo = bytes[byteIndex++];
                        int hi = bytes[byteIndex++];
                        chars[charIndex++] = (char)(hi << 8 | lo);
                    }
                }
            }
            else
            {
                Buffer.BlockCopy(bytes, byteIndex, chars, charIndex * CharSize, byteCount);
            }
            return charCount;
        }

        public override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
        {
            charsUsed = 0;
            bytesUsed = 0;

            if (_lastByte >= 0)
            {
                if (byteCount == 0)
                {
                    completed = true;
                    return;
                }
                int nextByte = bytes[byteIndex++];
                byteCount--;
                bytesUsed++;

                chars[charIndex++] = _bigEndian
                    ? (char)(_lastByte << 8 | nextByte)
                    : (char)(nextByte << 8 | _lastByte);
                charCount--;
                charsUsed++;
                _lastByte = -1;
            }

            if (charCount * CharSize < byteCount)
            {
                byteCount = charCount * CharSize;
                completed = false;
            }
            else
            {
                completed = true;
            }

            if (_bigEndian == BitConverter.IsLittleEndian)
            {
                int i = byteIndex;
                int byteEnd = i + (byteCount & ~0x1);
                if (_bigEndian)
                {
                    while (i < byteEnd)
                    {
                        int hi = bytes[i++];
                        int lo = bytes[i++];
                        chars[charIndex++] = (char)(hi << 8 | lo);
                    }
                }
                else
                {
                    while (i < byteEnd)
                    {
                        int lo = bytes[i++];
                        int hi = bytes[i++];
                        chars[charIndex++] = (char)(hi << 8 | lo);
                    }
                }
            }
            else
            {
                Buffer.BlockCopy(bytes, byteIndex, chars, charIndex * CharSize, (int)(byteCount & ~0x1));
            }
            charsUsed += byteCount / CharSize;
            bytesUsed += byteCount;

            if ((byteCount & 1) != 0)
            {
                _lastByte = bytes[byteIndex + byteCount - 1];
            }
        }
    }

    internal class SafeAsciiDecoder : Decoder
    {
        public SafeAsciiDecoder()
        {
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return count;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            int i = byteIndex;
            int j = charIndex;
            while (i < byteIndex + byteCount)
            {
                chars[j++] = (char)bytes[i++];
            }
            return byteCount;
        }

        public override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
        {
            if (charCount < byteCount)
            {
                byteCount = charCount;
                completed = false;
            }
            else
            {
                completed = true;
            }

            int i = byteIndex;
            int j = charIndex;
            int byteEndIndex = byteIndex + byteCount;

            while (i < byteEndIndex)
            {
                chars[j++] = (char)bytes[i++];
            }

            charsUsed = byteCount;
            bytesUsed = byteCount;
        }
    }
}
