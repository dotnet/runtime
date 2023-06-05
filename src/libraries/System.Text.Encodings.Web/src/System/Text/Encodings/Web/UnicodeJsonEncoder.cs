// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Encodings.Web
{
    internal sealed class UnicodeJsonEncoder : JavaScriptEncoder
    {
        internal static readonly UnicodeJsonEncoder Singleton = new UnicodeJsonEncoder();

        private readonly bool _preferHexEscape;
        private readonly bool _preferUppercase;

        public UnicodeJsonEncoder()
            : this(preferHexEscape: false, preferUppercase: false)
        {
        }

        public UnicodeJsonEncoder(bool preferHexEscape, bool preferUppercase)
        {
            _preferHexEscape = preferHexEscape;
            _preferUppercase = preferUppercase;
        }

        public override int MaxOutputCharactersPerInputCharacter => 6; // "\uXXXX" for a single char ("\uXXXX\uYYYY" [12 chars] for supplementary scalar value)

        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
        {
            for (int index = 0; index < textLength; ++index)
            {
                char value = text[index];

                if (NeedsEncoding(value))
                {
                    return index;
                }
            }

            return -1;
        }

        public override unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            bool encode = WillEncode(unicodeScalar);

            if (!encode)
            {
                Span<char> span = new Span<char>(buffer, bufferLength);
                int spanWritten;
                bool succeeded = new Rune(unicodeScalar).TryEncodeToUtf16(span, out spanWritten);
                numberOfCharactersWritten = spanWritten;
                return succeeded;
            }

            if (!_preferHexEscape && unicodeScalar <= char.MaxValue && HasTwoCharacterEscape((char)unicodeScalar))
            {
                if (bufferLength < 2)
                {
                    numberOfCharactersWritten = 0;
                    return false;
                }

                buffer[0] = '\\';
                buffer[1] = GetTwoCharacterEscapeSuffix((char)unicodeScalar);
                numberOfCharactersWritten = 2;
                return true;
            }
            else
            {
                if (bufferLength < 6)
                {
                    numberOfCharactersWritten = 0;
                    return false;
                }

                buffer[0] = '\\';
                buffer[1] = 'u';
                buffer[2] = '0';
                buffer[3] = '0';
                buffer[4] = ToHexDigit((unicodeScalar & 0xf0) >> 4, _preferUppercase);
                buffer[5] = ToHexDigit(unicodeScalar & 0xf, _preferUppercase);
                numberOfCharactersWritten = 6;
                return true;
            }
        }

        public override bool WillEncode(int unicodeScalar)
        {
            if (unicodeScalar > char.MaxValue)
            {
                return false;
            }

            return NeedsEncoding((char)unicodeScalar);
        }

        // https://datatracker.ietf.org/doc/html/rfc8259#section-7
        private static bool NeedsEncoding(char value)
        {
            if (value == '"' || value == '\\')
            {
                return true;
            }

            return value <= '\u001f';
        }

        private static bool HasTwoCharacterEscape(char value)
        {
            // RFC 8259, Section 7, "char = " BNF
            switch (value)
            {
                case '"':
                case '\\':
                case '/':
                case '\b':
                case '\f':
                case '\n':
                case '\r':
                case '\t':
                    return true;
                default:
                    return false;
            }
        }

        private static char GetTwoCharacterEscapeSuffix(char value)
        {
            // RFC 8259, Section 7, "char = " BNF
            switch (value)
            {
                case '"':
                    return '"';
                case '\\':
                    return '\\';
                case '/':
                    return '/';
                case '\b':
                    return 'b';
                case '\f':
                    return 'f';
                case '\n':
                    return 'n';
                case '\r':
                    return 'r';
                case '\t':
                    return 't';
                default:
                    throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static char ToHexDigit(int value, bool uppercase)
        {
            if (value > 0xf)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value < 10)
            {
                return (char)(value + '0');
            }
            else
            {
                return (char)(value - 0xa + (uppercase ? 'A' : 'a'));
            }
        }
    }
}
