// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    internal class Ucs4Encoding : Encoding
    {
        internal Ucs4Decoder ucs4Decoder;

        public override string WebName
        {
            get
            {
                return this.EncodingName;
            }
        }

        public override Decoder GetDecoder()
        {
            return ucs4Decoder;
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return checked(count * 4);
        }

        public override int GetByteCount(char[] chars)
        {
            return chars.Length * 4;
        }

        public override byte[] GetBytes(string s)
        {
            return null; //ucs4Decoder.GetByteCount(chars, index, count);
        }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return 0;
        }
        public override int GetMaxByteCount(int charCount)
        {
            return 0;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return ucs4Decoder.GetCharCount(bytes, index, count);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            return ucs4Decoder.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return (byteCount + 3) / 4;
        }

        public override int CodePage
        {
            get
            {
                return 0;
            }
        }

        public override int GetCharCount(byte[] bytes)
        {
            return bytes.Length / 4;
        }

        public override Encoder GetEncoder()
        {
            return null;
        }

        internal static Encoding UCS4_Littleendian
        {
            get
            {
                return new Ucs4Encoding4321();
            }
        }

        internal static Encoding UCS4_Bigendian
        {
            get
            {
                return new Ucs4Encoding1234();
            }
        }

        internal static Encoding UCS4_2143
        {
            get
            {
                return new Ucs4Encoding2143();
            }
        }

        internal static Encoding UCS4_3412
        {
            get
            {
                return new Ucs4Encoding3412();
            }
        }
    }

    internal sealed class Ucs4Encoding1234 : Ucs4Encoding
    {
        private static readonly byte[] s_preamble = new byte[4] { 0x00, 0x00, 0xfe, 0xff };

        public Ucs4Encoding1234()
        {
            ucs4Decoder = new Ucs4Decoder1234();
        }

        public override string EncodingName
        {
            get
            {
                return "ucs-4 (Bigendian)";
            }
        }

        public override byte[] GetPreamble()
        {
            return new byte[4] { 0x00, 0x00, 0xfe, 0xff };
        }

        public override ReadOnlySpan<byte> Preamble => s_preamble;
    }

    internal sealed class Ucs4Encoding4321 : Ucs4Encoding
    {
        private static readonly byte[] s_preamble = new byte[4] { 0xff, 0xfe, 0x00, 0x00 };

        public Ucs4Encoding4321()
        {
            ucs4Decoder = new Ucs4Decoder4321();
        }

        public override string EncodingName
        {
            get
            {
                return "ucs-4";
            }
        }

        public override byte[] GetPreamble()
        {
            return new byte[4] { 0xff, 0xfe, 0x00, 0x00 };
        }

        public override ReadOnlySpan<byte> Preamble => s_preamble;
    }

    internal sealed class Ucs4Encoding2143 : Ucs4Encoding
    {
        private static readonly byte[] s_preamble = new byte[4] { 0x00, 0x00, 0xff, 0xfe };

        public Ucs4Encoding2143()
        {
            ucs4Decoder = new Ucs4Decoder2143();
        }

        public override string EncodingName
        {
            get
            {
                return "ucs-4 (order 2143)";
            }
        }
        public override byte[] GetPreamble()
        {
            return new byte[4] { 0x00, 0x00, 0xff, 0xfe };
        }

        public override ReadOnlySpan<byte> Preamble => s_preamble;
    }

    internal sealed class Ucs4Encoding3412 : Ucs4Encoding
    {
        private static readonly byte[] s_preamble = new byte[4] { 0xfe, 0xff, 0x00, 0x00 };

        public Ucs4Encoding3412()
        {
            ucs4Decoder = new Ucs4Decoder3412();
        }

        public override string EncodingName
        {
            get
            {
                return "ucs-4 (order 3412)";
            }
        }

        public override byte[] GetPreamble()
        {
            return new byte[4] { 0xfe, 0xff, 0x00, 0x00 };
        }

        public override ReadOnlySpan<byte> Preamble => s_preamble;
    }

    internal abstract class Ucs4Decoder : Decoder
    {
        internal byte[] lastBytes = new byte[4];
        internal int lastBytesCount = 0;

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return (count + lastBytesCount) / 4;
        }

        internal abstract int GetFullChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            // finish a character from the bytes that were cached last time
            int i = lastBytesCount;
            if (lastBytesCount > 0)
            {
                // copy remaining bytes into the cache
                for (; lastBytesCount < 4 && byteCount > 0; lastBytesCount++)
                {
                    lastBytes[lastBytesCount] = bytes[byteIndex];
                    byteIndex++;
                    byteCount--;
                }
                // still not enough bytes -> return
                if (lastBytesCount < 4)
                {
                    return 0;
                }
                // decode 1 character from the byte cache
                i = GetFullChars(lastBytes, 0, 4, chars, charIndex);
                Debug.Assert(i == 1);
                charIndex += i;
                lastBytesCount = 0;
            }
            else
            {
                i = 0;
            }

            // decode block of byte quadruplets
            i = GetFullChars(bytes, byteIndex, byteCount, chars, charIndex) + i;

            // cache remaining bytes that does not make up a character
            int bytesLeft = (byteCount & 0x3);
            if (bytesLeft >= 0)
            {
                for (int j = 0; j < bytesLeft; j++)
                {
                    lastBytes[j] = bytes[byteIndex + byteCount - bytesLeft + j];
                }
                lastBytesCount = bytesLeft;
            }
            return i;
        }

        public override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed)
        {
            bytesUsed = 0;
            charsUsed = 0;
            // finish a character from the bytes that were cached last time
            int i = 0;
            int lbc = lastBytesCount;
            if (lbc > 0)
            {
                // copy remaining bytes into the cache
                for (; lbc < 4 && byteCount > 0; lbc++)
                {
                    lastBytes[lbc] = bytes[byteIndex];
                    byteIndex++;
                    byteCount--;
                    bytesUsed++;
                }
                // still not enough bytes -> return
                if (lbc < 4)
                {
                    lastBytesCount = lbc;
                    completed = true;
                    return;
                }
                // decode 1 character from the byte cache
                i = GetFullChars(lastBytes, 0, 4, chars, charIndex);

                charIndex += i;
                charCount -= i;
                charsUsed = i;

                lastBytesCount = 0;
            }
            else
            {
                i = 0;
            }

            // modify the byte count for GetFullChars depending on how many characters were requested
            if (charCount * 4 < byteCount)
            {
                byteCount = charCount * 4;
                completed = false;
            }
            else
            {
                completed = true;
            }
            bytesUsed += byteCount;

            // decode block of byte quadruplets
            charsUsed = GetFullChars(bytes, byteIndex, byteCount, chars, charIndex) + i;

            // cache remaining bytes that does not make up a character
            int bytesLeft = (byteCount & 0x3);
            if (bytesLeft >= 0)
            {
                for (int j = 0; j < bytesLeft; j++)
                {
                    lastBytes[j] = bytes[byteIndex + byteCount - bytesLeft + j];
                }
                lastBytesCount = bytesLeft;
            }
        }

        internal void Ucs4ToUTF16(uint code, char[] chars, int charIndex)
        {
            chars[charIndex] = (char)(XmlCharType.SurHighStart + (char)((code >> 16) - 1) + (char)((code >> 10) & 0x3F));
            chars[charIndex + 1] = (char)(XmlCharType.SurLowStart + (char)(code & 0x3FF));
        }
    }

    internal class Ucs4Decoder4321 : Ucs4Decoder
    {
        internal override int GetFullChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            uint code;
            int i, j;

            byteCount += byteIndex;

            for (i = byteIndex, j = charIndex; i + 3 < byteCount;)
            {
                code = (uint)((bytes[i + 3] << 24) | (bytes[i + 2] << 16) | (bytes[i + 1] << 8) | bytes[i]);
                if (code > 0x10FFFF)
                {
                    throw new ArgumentException(SR.Format(SR.Enc_InvalidByteInEncoding, new object[1] { i }), (string)null);
                }
                else if (code > 0xFFFF)
                {
                    Ucs4ToUTF16(code, chars, j);
                    j++;
                }
                else
                {
                    if (XmlCharType.IsSurrogate((int)code))
                    {
                        throw new XmlException(SR.Xml_InvalidCharInThisEncoding, string.Empty);
                    }
                    else
                    {
                        chars[j] = (char)code;
                    }
                }
                j++;
                i += 4;
            }
            return j - charIndex;
        }
    };

    internal class Ucs4Decoder1234 : Ucs4Decoder
    {
        internal override int GetFullChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            uint code;
            int i, j;

            byteCount += byteIndex;

            for (i = byteIndex, j = charIndex; i + 3 < byteCount;)
            {
                code = (uint)((bytes[i] << 24) | (bytes[i + 1] << 16) | (bytes[i + 2] << 8) | bytes[i + 3]);
                if (code > 0x10FFFF)
                {
                    throw new ArgumentException(SR.Format(SR.Enc_InvalidByteInEncoding, new object[1] { i }), (string)null);
                }
                else if (code > 0xFFFF)
                {
                    Ucs4ToUTF16(code, chars, j);
                    j++;
                }
                else
                {
                    if (XmlCharType.IsSurrogate((int)code))
                    {
                        throw new XmlException(SR.Xml_InvalidCharInThisEncoding, string.Empty);
                    }
                    else
                    {
                        chars[j] = (char)code;
                    }
                }
                j++;
                i += 4;
            }
            return j - charIndex;
        }
    }


    internal class Ucs4Decoder2143 : Ucs4Decoder
    {
        internal override int GetFullChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            uint code;
            int i, j;

            byteCount += byteIndex;

            for (i = byteIndex, j = charIndex; i + 3 < byteCount;)
            {
                code = (uint)((bytes[i + 1] << 24) | (bytes[i] << 16) | (bytes[i + 3] << 8) | bytes[i + 2]);
                if (code > 0x10FFFF)
                {
                    throw new ArgumentException(SR.Format(SR.Enc_InvalidByteInEncoding, new object[1] { i }), (string)null);
                }
                else if (code > 0xFFFF)
                {
                    Ucs4ToUTF16(code, chars, j);
                    j++;
                }
                else
                {
                    if (XmlCharType.IsSurrogate((int)code))
                    {
                        throw new XmlException(SR.Xml_InvalidCharInThisEncoding, string.Empty);
                    }
                    else
                    {
                        chars[j] = (char)code;
                    }
                }
                j++;
                i += 4;
            }
            return j - charIndex;
        }
    }


    internal class Ucs4Decoder3412 : Ucs4Decoder
    {
        internal override int GetFullChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            uint code;
            int i, j;

            byteCount += byteIndex;

            for (i = byteIndex, j = charIndex; i + 3 < byteCount;)
            {
                code = (uint)((bytes[i + 2] << 24) | (bytes[i + 3] << 16) | (bytes[i] << 8) | bytes[i + 1]);
                if (code > 0x10FFFF)
                {
                    throw new ArgumentException(SR.Format(SR.Enc_InvalidByteInEncoding, new object[1] { i }), (string)null);
                }
                else if (code > 0xFFFF)
                {
                    Ucs4ToUTF16(code, chars, j);
                    j++;
                }
                else
                {
                    if (XmlCharType.IsSurrogate((int)code))
                    {
                        throw new XmlException(SR.Xml_InvalidCharInThisEncoding, string.Empty);
                    }
                    else
                    {
                        chars[j] = (char)code;
                    }
                }
                j++;
                i += 4;
            }
            return j - charIndex;
        }
    }
}
