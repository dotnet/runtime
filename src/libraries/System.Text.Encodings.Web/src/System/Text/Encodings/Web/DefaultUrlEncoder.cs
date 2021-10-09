// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    internal sealed class DefaultUrlEncoder : UrlEncoder
    {
        internal static readonly DefaultUrlEncoder BasicLatinSingleton = new DefaultUrlEncoder(new TextEncoderSettings(UnicodeRanges.BasicLatin));

        private readonly OptimizedInboxTextEncoder _innerEncoder;

        internal DefaultUrlEncoder(TextEncoderSettings settings)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            // Per RFC 3987, Sec. 2.2, we want encodings that are safe for
            // four particular components: 'isegment', 'ipath-noscheme',
            // 'iquery', and 'ifragment'. The relevant definitions are below.
            //
            //    ipath-noscheme = isegment-nz-nc *( "/" isegment )
            //
            //    isegment       = *ipchar
            //
            //    isegment-nz-nc = 1*( iunreserved / pct-encoded / sub-delims
            //                         / "@" )
            //                   ; non-zero-length segment without any colon ":"
            //
            //    ipchar         = iunreserved / pct-encoded / sub-delims / ":"
            //                   / "@"
            //
            //    iquery         = *( ipchar / iprivate / "/" / "?" )
            //
            //    ifragment      = *( ipchar / "/" / "?" )
            //
            //    iunreserved    = ALPHA / DIGIT / "-" / "." / "_" / "~" / ucschar
            //
            //    ucschar        = %xA0-D7FF / %xF900-FDCF / %xFDF0-FFEF
            //                   / %x10000-1FFFD / %x20000-2FFFD / %x30000-3FFFD
            //                   / %x40000-4FFFD / %x50000-5FFFD / %x60000-6FFFD
            //                   / %x70000-7FFFD / %x80000-8FFFD / %x90000-9FFFD
            //                   / %xA0000-AFFFD / %xB0000-BFFFD / %xC0000-CFFFD
            //                   / %xD0000-DFFFD / %xE1000-EFFFD
            //
            //    pct-encoded    = "%" HEXDIG HEXDIG
            //
            //    sub-delims     = "!" / "$" / "&" / "'" / "(" / ")"
            //                   / "*" / "+" / "," / ";" / "="
            //
            // The only common characters between these four components are the
            // intersection of 'isegment-nz-nc' and 'ipchar', which is really
            // just 'isegment-nz-nc' (colons forbidden).
            //
            // From this list, the base encoder already forbids "&", "'", "+",
            // and we'll additionally forbid "=" since it has special meaning
            // in x-www-form-urlencoded representations.
            //
            // This means that the full list of allowed characters from the
            // Basic Latin set is:
            // ALPHA / DIGIT / "-" / "." / "_" / "~" / "!" / "$" / "(" / ")" / "*" / "," / ";" / "@"

            _innerEncoder = new OptimizedInboxTextEncoder(EscaperImplementation.Singleton, settings.GetAllowedCodePointsBitmap(), extraCharactersToEscape: stackalloc char[] {
                ' ', // chars from Basic Latin which aren't already disallowed by the base encoder
                '#',
                '%',
                '/',
                ':',
                '=',
                '?',
                '[',
                '\\',
                ']',
                '^',
                '`',
                '{',
                '|',
                '}',
                '\uFFF0', // specials (U+FFF0 .. U+FFFF) are forbidden by the definition of 'ucschar' above
                '\uFFF1',
                '\uFFF2',
                '\uFFF3',
                '\uFFF4',
                '\uFFF5',
                '\uFFF6',
                '\uFFF7',
                '\uFFF8',
                '\uFFF9',
                '\uFFFA',
                '\uFFFB',
                '\uFFFC',
                '\uFFFD',
                '\uFFFE',
                '\uFFFF',
            });
        }

        public override int MaxOutputCharactersPerInputCharacter => 9; // "%XX%YY%ZZ" for a single char ("%XX%YY%ZZ%WW" [12 chars] for supplementary scalar value)

        /*
         * These overrides should be copied to all other subclasses that are backed
         * by the fast inbox escaping mechanism.
         */

#pragma warning disable CS0618 // some of the adapters are intentionally marked [Obsolete]
        private protected override OperationStatus EncodeCore(ReadOnlySpan<char> source, Span<char> destination, out int charsConsumed, out int charsWritten, bool isFinalBlock)
            => _innerEncoder.Encode(source, destination, out charsConsumed, out charsWritten, isFinalBlock);

        private protected override OperationStatus EncodeUtf8Core(ReadOnlySpan<byte> utf8Source, Span<byte> utf8Destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
            => _innerEncoder.EncodeUtf8(utf8Source, utf8Destination, out bytesConsumed, out bytesWritten, isFinalBlock);

        private protected override int FindFirstCharacterToEncode(ReadOnlySpan<char> text)
            => _innerEncoder.GetIndexOfFirstCharToEncode(text);

        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
            => _innerEncoder.FindFirstCharacterToEncode(text, textLength);

        public override int FindFirstCharacterToEncodeUtf8(ReadOnlySpan<byte> utf8Text)
            => _innerEncoder.GetIndexOfFirstByteToEncode(utf8Text);

        public override unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
            => _innerEncoder.TryEncodeUnicodeScalar(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten);

        public override bool WillEncode(int unicodeScalar)
            => !_innerEncoder.IsScalarValueAllowed(new Rune(unicodeScalar));
#pragma warning restore CS0618

        /*
         * End overrides section.
         */

        private sealed class EscaperImplementation : ScalarEscaperBase
        {
            internal static readonly EscaperImplementation Singleton = new EscaperImplementation();

            private EscaperImplementation() { }

            internal override int EncodeUtf8(Rune value, Span<byte> destination)
            {
                uint utf8lsb = (uint)UnicodeHelpers.GetUtf8RepresentationForScalarValue((uint)value.Value);

                if (!SpanUtility.IsValidIndex(destination, 2)) { goto OutOfSpace; }
                destination[0] = (byte)'%';
                HexConverter.ToBytesBuffer((byte)utf8lsb, destination, startingIndex: 1);
                if ((utf8lsb >>= 8) == 0) { return 3; } // "%XX"

                if (!SpanUtility.IsValidIndex(destination, 5)) { goto OutOfSpace; }
                destination[3] = (byte)'%';
                HexConverter.ToBytesBuffer((byte)utf8lsb, destination, startingIndex: 4);
                if ((utf8lsb >>= 8) == 0) { return 6; } // "%XX%YY"

                if (!SpanUtility.IsValidIndex(destination, 8)) { goto OutOfSpace; }
                destination[6] = (byte)'%';
                HexConverter.ToBytesBuffer((byte)utf8lsb, destination, startingIndex: 7);
                if ((utf8lsb >>= 8) == 0) { return 9; } // "%XX%YY%ZZ"

                if (!SpanUtility.IsValidIndex(destination, 11)) { goto OutOfSpace; }
                destination[9] = (byte)'%';
                HexConverter.ToBytesBuffer((byte)utf8lsb, destination, startingIndex: 10);
                return 12;  // "%XX%YY%ZZ%WW"

            OutOfSpace:

                return -1;
            }

            internal override int EncodeUtf16(Rune value, Span<char> destination)
            {
                uint utf8lsb = (uint)UnicodeHelpers.GetUtf8RepresentationForScalarValue((uint)value.Value);

                if (!SpanUtility.IsValidIndex(destination, 2)) { goto OutOfSpace; }
                destination[0] = '%';
                HexConverter.ToCharsBuffer((byte)utf8lsb, destination, startingIndex: 1);
                if ((utf8lsb >>= 8) == 0) { return 3; } // "%XX"

                if (!SpanUtility.IsValidIndex(destination, 5)) { goto OutOfSpace; }
                destination[3] = '%';
                HexConverter.ToCharsBuffer((byte)utf8lsb, destination, startingIndex: 4);
                if ((utf8lsb >>= 8) == 0) { return 6; } // "%XX%YY"

                if (!SpanUtility.IsValidIndex(destination, 8)) { goto OutOfSpace; }
                destination[6] = '%';
                HexConverter.ToCharsBuffer((byte)utf8lsb, destination, startingIndex: 7);
                if ((utf8lsb >>= 8) == 0) { return 9; } // "%XX%YY%ZZ"

                if (!SpanUtility.IsValidIndex(destination, 11)) { goto OutOfSpace; }
                destination[9] = '%';
                HexConverter.ToCharsBuffer((byte)utf8lsb, destination, startingIndex: 10);
                return 12;  // "%XX%YY%ZZ%WW"

            OutOfSpace:

                return -1;
            }
        }
    }
}
