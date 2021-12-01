// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    internal sealed class DefaultJavaScriptEncoder : JavaScriptEncoder
    {
        internal static readonly DefaultJavaScriptEncoder BasicLatinSingleton = new DefaultJavaScriptEncoder(new TextEncoderSettings(UnicodeRanges.BasicLatin));
        internal static readonly DefaultJavaScriptEncoder UnsafeRelaxedEscapingSingleton = new DefaultJavaScriptEncoder(new TextEncoderSettings(UnicodeRanges.All), allowMinimalJsonEscaping: true);

        private readonly OptimizedInboxTextEncoder _innerEncoder;

        internal DefaultJavaScriptEncoder(TextEncoderSettings settings)
            : this(settings, allowMinimalJsonEscaping: false)
        {
        }

        private DefaultJavaScriptEncoder(TextEncoderSettings settings, bool allowMinimalJsonEscaping)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            // '\' (U+005C REVERSE SOLIDUS) must always be escaped in Javascript / ECMAScript / JSON.
            // '/' (U+002F SOLIDUS) is not Javascript / ECMAScript / JSON-sensitive so doesn't need to be escaped.
            // '`' (U+0060 GRAVE ACCENT) is ECMAScript-sensitive (see ECMA-262).

            _innerEncoder = allowMinimalJsonEscaping
                ? new OptimizedInboxTextEncoder(EscaperImplementation.SingletonMinimallyEscaped, settings.GetAllowedCodePointsBitmap(), forbidHtmlSensitiveCharacters: false,
                    extraCharactersToEscape: stackalloc char[] { '\"', '\\' })
                : new OptimizedInboxTextEncoder(EscaperImplementation.Singleton, settings.GetAllowedCodePointsBitmap(), forbidHtmlSensitiveCharacters: true,
                    extraCharactersToEscape: stackalloc char[] { '\\', '`' });
        }

        public override int MaxOutputCharactersPerInputCharacter => 6; // "\uXXXX" for a single char ("\uXXXX\uYYYY" [12 chars] for supplementary scalar value)

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
            internal static readonly EscaperImplementation Singleton = new EscaperImplementation(allowMinimalEscaping: false);
            internal static readonly EscaperImplementation SingletonMinimallyEscaped = new EscaperImplementation(allowMinimalEscaping: true);

            // Map stores the second byte for any ASCII input that can be escaped as the two-element sequence
            // REVERSE SOLIDUS followed by a single character. For example, <LF> maps to the two chars "\n".
            // The map does not contain an entry for chars which cannot be escaped in this manner.
            private readonly AsciiByteMap _preescapedMap;

            private EscaperImplementation(bool allowMinimalEscaping)
            {
                _preescapedMap.InsertAsciiChar('\b', (byte)'b');
                _preescapedMap.InsertAsciiChar('\t', (byte)'t');
                _preescapedMap.InsertAsciiChar('\n', (byte)'n');
                _preescapedMap.InsertAsciiChar('\f', (byte)'f');
                _preescapedMap.InsertAsciiChar('\r', (byte)'r');
                _preescapedMap.InsertAsciiChar('\\', (byte)'\\');

                if (allowMinimalEscaping)
                {
                    _preescapedMap.InsertAsciiChar('\"', (byte)'\"');
                }
            }

            // Writes a scalar value as a JavaScript-escaped character (or sequence of characters).
            // See ECMA-262, Sec. 7.8.4, and ECMA-404, Sec. 9
            // https://www.ecma-international.org/ecma-262/5.1/#sec-7.8.4
            // https://www.ecma-international.org/publications/files/ECMA-ST/ECMA-404.pdf
            //
            // ECMA-262 allows encoding U+000B as "\v", but ECMA-404 does not.
            // Both ECMA-262 and ECMA-404 allow encoding U+002F SOLIDUS as "\/"
            // (in ECMA-262 this character is a NonEscape character); however, we
            // don't encode SOLIDUS by default unless the caller has provided an
            // explicit bitmap which does not contain it. In this case we'll assume
            // that the caller didn't want a SOLIDUS written to the output at all,
            // so it should be written using "\u002F" encoding.
            // HTML-specific characters (including apostrophe and quotes) will
            // be written out as numeric entities for defense-in-depth.

            internal override int EncodeUtf8(Rune value, Span<byte> destination)
            {
                if (_preescapedMap.TryLookup(value, out byte preescapedForm))
                {
                    if (!SpanUtility.IsValidIndex(destination, 1)) { goto OutOfSpace; }
                    destination[0] = (byte)'\\';
                    destination[1] = preescapedForm;
                    return 2;

                OutOfSpace:
                    return -1;
                }

                return TryEncodeScalarAsHex(this, value, destination);

#pragma warning disable IDE0060 // 'this' taken explicitly to avoid argument shuffling by caller
                static int TryEncodeScalarAsHex(object @this, Rune value, Span<byte> destination)
#pragma warning restore IDE0060
                {
                    if (value.IsBmp)
                    {
                        // Write 6 bytes: "\uXXXX"
                        if (!SpanUtility.IsValidIndex(destination, 5)) { goto OutOfSpaceInner; }
                        destination[0] = (byte)'\\';
                        destination[1] = (byte)'u';
                        HexConverter.ToBytesBuffer((byte)value.Value, destination, 4);
                        HexConverter.ToBytesBuffer((byte)((uint)value.Value >> 8), destination, 2);
                        return 6;
                    }
                    else
                    {
                        // Write 12 bytes: "\uXXXX\uYYYY"
                        UnicodeHelpers.GetUtf16SurrogatePairFromAstralScalarValue((uint)value.Value, out char highSurrogate, out char lowSurrogate);
                        if (!SpanUtility.IsValidIndex(destination, 11)) { goto OutOfSpaceInner; }
                        destination[0] = (byte)'\\';
                        destination[1] = (byte)'u';
                        HexConverter.ToBytesBuffer((byte)highSurrogate, destination, 4);
                        HexConverter.ToBytesBuffer((byte)((uint)highSurrogate >> 8), destination, 2);
                        destination[6] = (byte)'\\';
                        destination[7] = (byte)'u';
                        HexConverter.ToBytesBuffer((byte)lowSurrogate, destination, 10);
                        HexConverter.ToBytesBuffer((byte)((uint)lowSurrogate >> 8), destination, 8);
                        return 12;
                    }

                OutOfSpaceInner:

                    return -1;
                }
            }

            internal override int EncodeUtf16(Rune value, Span<char> destination)
            {
                if (_preescapedMap.TryLookup(value, out byte preescapedForm))
                {
                    if (!SpanUtility.IsValidIndex(destination, 1)) { goto OutOfSpace; }
                    destination[0] = '\\';
                    destination[1] = (char)preescapedForm;
                    return 2;

                OutOfSpace:
                    return -1;
                }

                return TryEncodeScalarAsHex(this, value, destination);

#pragma warning disable IDE0060 // 'this' taken explicitly to avoid argument shuffling by caller
                static int TryEncodeScalarAsHex(object @this, Rune value, Span<char> destination)
#pragma warning restore IDE0060
                {
                    if (value.IsBmp)
                    {
                        // Write 6 chars: "\uXXXX"
                        if (!SpanUtility.IsValidIndex(destination, 5)) { goto OutOfSpaceInner; }
                        destination[0] = '\\';
                        destination[1] = 'u';
                        HexConverter.ToCharsBuffer((byte)value.Value, destination, 4);
                        HexConverter.ToCharsBuffer((byte)((uint)value.Value >> 8), destination, 2);
                        return 6;
                    }
                    else
                    {
                        // Write 12 chars: "\uXXXX\uYYYY"
                        UnicodeHelpers.GetUtf16SurrogatePairFromAstralScalarValue((uint)value.Value, out char highSurrogate, out char lowSurrogate);
                        if (!SpanUtility.IsValidIndex(destination, 11)) { goto OutOfSpaceInner; }
                        destination[0] = '\\';
                        destination[1] = 'u';
                        HexConverter.ToCharsBuffer((byte)highSurrogate, destination, 4);
                        HexConverter.ToCharsBuffer((byte)((uint)highSurrogate >> 8), destination, 2);
                        destination[6] = '\\';
                        destination[7] = 'u';
                        HexConverter.ToCharsBuffer((byte)lowSurrogate, destination, 10);
                        HexConverter.ToCharsBuffer((byte)((uint)lowSurrogate >> 8), destination, 8);
                        return 12;
                    }

                OutOfSpaceInner:

                    return -1;
                }
            }
        }
    }
}
