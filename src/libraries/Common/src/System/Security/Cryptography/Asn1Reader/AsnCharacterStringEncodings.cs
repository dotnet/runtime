// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System.Security.Cryptography.Asn1
{
    internal static class AsnCharacterStringEncodings
    {
        private static readonly Text.Encoding s_utf8Encoding = new UTF8Encoding(false, throwOnInvalidBytes: true);
        private static readonly Text.Encoding s_bmpEncoding = new BMPEncoding();
        private static readonly Text.Encoding s_ia5Encoding = new IA5Encoding();
        private static readonly Text.Encoding s_visibleStringEncoding = new VisibleStringEncoding();
        private static readonly Text.Encoding s_printableStringEncoding = new PrintableStringEncoding();
        private static readonly Text.Encoding s_t61Encoding = new T61Encoding();

        internal static Text.Encoding GetEncoding(UniversalTagNumber encodingType)
        {
            switch (encodingType)
            {
                case UniversalTagNumber.UTF8String:
                    return s_utf8Encoding;
                case UniversalTagNumber.PrintableString:
                    return s_printableStringEncoding;
                case UniversalTagNumber.IA5String:
                    return s_ia5Encoding;
                case UniversalTagNumber.VisibleString:
                    return s_visibleStringEncoding;
                case UniversalTagNumber.BMPString:
                    return s_bmpEncoding;
                case UniversalTagNumber.T61String:
                    return s_t61Encoding;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encodingType), encodingType, null);
            }
        }
    }

    internal abstract class SpanBasedEncoding : Text.Encoding
    {
        protected SpanBasedEncoding()
            : base(0, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)
        {
        }

        protected abstract int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool write);
        protected abstract int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool write);

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return GetByteCount(new ReadOnlySpan<char>(chars, index, count));
        }

        public override unsafe int GetByteCount(char* chars, int count)
        {
            return GetByteCount(new ReadOnlySpan<char>(chars, count));
        }

        public override int GetByteCount(string s)
        {
            return GetByteCount(s.AsSpan());
        }

        public
#if netcoreapp || uap || NETCOREAPP
            override
#endif
        int GetByteCount(ReadOnlySpan<char> chars)
        {
            return GetBytes(chars, Span<byte>.Empty, write: false);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return GetBytes(
                new ReadOnlySpan<char>(chars, charIndex, charCount),
                new Span<byte>(bytes, byteIndex, bytes.Length - byteIndex),
                write: true);
        }

        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            return GetBytes(
                new ReadOnlySpan<char>(chars, charCount),
                new Span<byte>(bytes, byteCount),
                write: true);
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return GetCharCount(new ReadOnlySpan<byte>(bytes, index, count));
        }

        public override unsafe int GetCharCount(byte* bytes, int count)
        {
            return GetCharCount(new ReadOnlySpan<byte>(bytes, count));
        }

        public
#if netcoreapp || uap || NETCOREAPP
            override
#endif
        int GetCharCount(ReadOnlySpan<byte> bytes)
        {
            return GetChars(bytes, Span<char>.Empty, write: false);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            return GetChars(
                new ReadOnlySpan<byte>(bytes, byteIndex, byteCount),
                new Span<char>(chars, charIndex, chars.Length - charIndex),
                write: true);
        }

        public override unsafe int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            return GetChars(
                new ReadOnlySpan<byte>(bytes, byteCount),
                new Span<char>(chars, charCount),
                write: true);
        }
    }

    internal class IA5Encoding : RestrictedAsciiStringEncoding
    {
        // T-REC-X.680-201508 sec 41, Table 8.
        // ISO International Register of Coded Character Sets to be used with Escape Sequences 001
        //   is ASCII 0x00 - 0x1F
        // ISO International Register of Coded Character Sets to be used with Escape Sequences 006
        //   is ASCII 0x21 - 0x7E
        // Space is ASCII 0x20, delete is ASCII 0x7F.
        //
        // The net result is all of 7-bit ASCII
        internal IA5Encoding()
            : base(0x00, 0x7F)
        {
        }
    }

    internal class VisibleStringEncoding : RestrictedAsciiStringEncoding
    {
        // T-REC-X.680-201508 sec 41, Table 8.
        // ISO International Register of Coded Character Sets to be used with Escape Sequences 006
        //   is ASCII 0x21 - 0x7E
        // Space is ASCII 0x20.
        internal VisibleStringEncoding()
            : base(0x20, 0x7E)
        {
        }
    }

    internal class PrintableStringEncoding : RestrictedAsciiStringEncoding
    {
        // T-REC-X.680-201508 sec 41.4
        internal PrintableStringEncoding()
            : base("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 '()+,-./:=?")
        {
        }
    }

    internal abstract class RestrictedAsciiStringEncoding : SpanBasedEncoding
    {
        private readonly bool[] _isAllowed;

        protected RestrictedAsciiStringEncoding(byte minCharAllowed, byte maxCharAllowed)
        {
            Debug.Assert(minCharAllowed <= maxCharAllowed);
            Debug.Assert(maxCharAllowed <= 0x7F);

            bool[] isAllowed = new bool[0x80];

            for (byte charCode = minCharAllowed; charCode <= maxCharAllowed; charCode++)
            {
                isAllowed[charCode] = true;
            }

            _isAllowed = isAllowed;
        }

        protected RestrictedAsciiStringEncoding(IEnumerable<char> allowedChars)
        {
            bool[] isAllowed = new bool[0x7F];

            foreach (char c in allowedChars)
            {
                if (c >= isAllowed.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(allowedChars));
                }

                Debug.Assert(isAllowed[c] == false);
                isAllowed[c] = true;
            }

            _isAllowed = isAllowed;
        }

        public override int GetMaxByteCount(int charCount)
        {
            return charCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }

        protected override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool write)
        {
            if (chars.IsEmpty)
            {
                return 0;
            }

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];

                if ((uint)c >= (uint)_isAllowed.Length || !_isAllowed[c])
                {
                    EncoderFallback.CreateFallbackBuffer().Fallback(c, i);

                    Debug.Fail("Fallback should have thrown");
                    throw new CryptographicException();
                }

                if (write)
                {
                    bytes[i] = (byte)c;
                }
            }

            return chars.Length;
        }

        protected override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool write)
        {
            if (bytes.IsEmpty)
            {
                return 0;
            }

            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];

                if ((uint)b >= (uint)_isAllowed.Length || !_isAllowed[b])
                {
                    DecoderFallback.CreateFallbackBuffer().Fallback(
                        new[] { b },
                        i);

                    Debug.Fail("Fallback should have thrown");
                    throw new CryptographicException();
                }

                if (write)
                {
                    chars[i] = (char)b;
                }
            }

            return bytes.Length;
        }
    }

    /// <summary>
    ///   Big-Endian UCS-2 encoding (the same as UTF-16BE, but disallowing surrogate pairs to leave plane 0)
    /// </summary>
    // T-REC-X.690-201508 sec 8.23.8 says to see ISO/IEC 10646:2003 section 13.1.
    // ISO/IEC 10646:2003 sec 13.1 says each character is represented by "two octets".
    // ISO/IEC 10646:2003 sec 6.3 says that when serialized as octets to use big endian.
    internal class BMPEncoding : SpanBasedEncoding
    {
        protected override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool write)
        {
            if (chars.IsEmpty)
            {
                return 0;
            }

            int writeIdx = 0;

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];

                if (char.IsSurrogate(c))
                {
                    EncoderFallback.CreateFallbackBuffer().Fallback(c, i);

                    Debug.Fail("Fallback should have thrown");
                    throw new CryptographicException();
                }

                ushort val16 = c;

                if (write)
                {
                    bytes[writeIdx + 1] = (byte)val16;
                    bytes[writeIdx] = (byte)(val16 >> 8);
                }

                writeIdx += 2;
            }

            return writeIdx;
        }

        protected override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool write)
        {
            if (bytes.IsEmpty)
            {
                return 0;
            }

            if (bytes.Length % 2 != 0)
            {
                DecoderFallback.CreateFallbackBuffer().Fallback(
                    bytes.Slice(bytes.Length - 1).ToArray(),
                    bytes.Length - 1);

                Debug.Fail("Fallback should have thrown");
                throw new CryptographicException();
            }

            int writeIdx = 0;

            for (int i = 0; i < bytes.Length; i += 2)
            {
                int val = bytes[i] << 8 | bytes[i + 1];
                char c = (char)val;

                if (char.IsSurrogate(c))
                {
                    DecoderFallback.CreateFallbackBuffer().Fallback(
                        bytes.Slice(i, 2).ToArray(),
                        i);

                    Debug.Fail("Fallback should have thrown");
                    throw new CryptographicException();
                }

                if (write)
                {
                    chars[writeIdx] = c;
                }

                writeIdx++;
            }

            return writeIdx;
        }

        public override int GetMaxByteCount(int charCount)
        {
            checked
            {
                return charCount * 2;
            }
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount / 2;
        }
    }

    /// <summary>
    /// Compatibility encoding for T61Strings. Interprets the characters as UTF-8 or
    /// ISO-8859-1 as a fallback.
    /// </summary>
    internal class T61Encoding : Text.Encoding
    {
        private static readonly Text.Encoding s_utf8Encoding = new UTF8Encoding(false, throwOnInvalidBytes: true);
        private static readonly Text.Encoding s_latin1Encoding = System.Text.Encoding.GetEncoding("iso-8859-1");

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return s_utf8Encoding.GetByteCount(chars, index, count);
        }

        public override unsafe int GetByteCount(char* chars, int count)
        {
            return s_utf8Encoding.GetByteCount(chars, count);
        }

        public override int GetByteCount(string s)
        {
            return s_utf8Encoding.GetByteCount(s);
        }

#if netcoreapp || uap || NETCOREAPP
        public override int GetByteCount(ReadOnlySpan<char> chars)
        {
            return s_utf8Encoding.GetByteCount(chars);
        }
#endif

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return s_utf8Encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
        }

        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount)
        {
            return s_utf8Encoding.GetBytes(chars, charCount, bytes, byteCount);
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            try
            {
                return s_utf8Encoding.GetCharCount(bytes, index, count);
            }
            catch (DecoderFallbackException)
            {
                return s_latin1Encoding.GetCharCount(bytes, index, count);
            }
        }

        public override unsafe int GetCharCount(byte* bytes, int count)
        {
            try
            {
                return s_utf8Encoding.GetCharCount(bytes, count);
            }
            catch (DecoderFallbackException)
            {
                return s_latin1Encoding.GetCharCount(bytes, count);
            }
        }

#if netcoreapp || uap || NETCOREAPP
        public override int GetCharCount(ReadOnlySpan<byte> bytes)
        {
            try
            {
                return s_utf8Encoding.GetCharCount(bytes);
            }
            catch (DecoderFallbackException)
            {
                return s_latin1Encoding.GetCharCount(bytes);
            }
        }
#endif

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            try
            {
                return s_utf8Encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            }
            catch (DecoderFallbackException)
            {
                return s_latin1Encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            }
        }

        public override unsafe int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            try
            {
                return s_utf8Encoding.GetChars(bytes, byteCount, chars, charCount);
            }
            catch (DecoderFallbackException)
            {
                return s_latin1Encoding.GetChars(bytes, byteCount, chars, charCount);
            }
        }

        public override int GetMaxByteCount(int charCount)
        {
            return s_utf8Encoding.GetMaxByteCount(charCount);
        }

        public override int GetMaxCharCount(int byteCount)
        {
            // Latin-1 is single byte encoding, so byteCount == charCount
            // UTF-8 is multi-byte encoding, so byteCount >= charCount
            // We want to return the maximum of those two, which happens to be byteCount.
            return byteCount;
        }
    }
}
