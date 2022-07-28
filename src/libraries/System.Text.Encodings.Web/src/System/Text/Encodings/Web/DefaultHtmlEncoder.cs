// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    internal sealed class DefaultHtmlEncoder : HtmlEncoder
    {
        internal static readonly DefaultHtmlEncoder BasicLatinSingleton = new DefaultHtmlEncoder(new TextEncoderSettings(UnicodeRanges.BasicLatin));

        private readonly OptimizedInboxTextEncoder _innerEncoder;

        internal DefaultHtmlEncoder(TextEncoderSettings settings)
        {
            if (settings is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.settings);
            }

            _innerEncoder = new OptimizedInboxTextEncoder(EscaperImplementation.Singleton, settings.GetAllowedCodePointsBitmap());
        }

        public override int MaxOutputCharactersPerInputCharacter => 8; // "&#xFFFF;" is worst case for single char ("&#x10FFFF;" [10 chars] worst case for arbitrary scalar value)

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
                if (value.Value == '<')
                {
                    if (!SpanUtility.TryWriteBytes(destination, (byte)'&', (byte)'l', (byte)'t', (byte)';')) { goto OutOfSpace; }
                    return 4;
                }
                else if (value.Value == '>')
                {
                    if (!SpanUtility.TryWriteBytes(destination, (byte)'&', (byte)'g', (byte)'t', (byte)';')) { goto OutOfSpace; }
                    return 4;
                }
                else if (value.Value == '&')
                {
                    if (!SpanUtility.TryWriteBytes(destination, (byte)'&', (byte)'a', (byte)'m', (byte)'p', (byte)';')) { goto OutOfSpace; }
                    return 5;
                }
                else if (value.Value == '\"')
                {
                    if (!SpanUtility.TryWriteBytes(destination, (byte)'&', (byte)'q', (byte)'u', (byte)'o', (byte)'t', (byte)';')) { goto OutOfSpace; }
                    return 6;
                }
                else
                {
                    return TryEncodeScalarAsHex(this, (uint)value.Value, destination);
                }

            OutOfSpace:

                return -1;

#pragma warning disable IDE0060 // 'this' taken explicitly to avoid argument shuffling by caller
                static int TryEncodeScalarAsHex(object @this, uint scalarValue, Span<byte> destination)
#pragma warning restore IDE0060
                {
                    UnicodeDebug.AssertIsValidScalar(scalarValue);

                    // See comments in the UTF-16 equivalent method later in this file.

                    int idxOfSemicolon = (int)((uint)BitOperations.Log2(scalarValue) / 4) + 4;
                    Debug.Assert(4 <= idxOfSemicolon && idxOfSemicolon <= 9, "Expected '&#x0;'..'&#x10FFFF;'.");

                    if (!SpanUtility.IsValidIndex(destination, idxOfSemicolon)) { goto OutOfSpaceInner; }
                    destination[idxOfSemicolon] = (byte)';';

                    if (!SpanUtility.TryWriteBytes(destination, (byte)'&', (byte)'#', (byte)'x', (byte)'0'))
                    {
                        Debug.Fail("We should've had enough room to write 4 bytes.");
                    }

                    destination = destination.Slice(3, idxOfSemicolon - 3);
                    for (int i = destination.Length - 1; SpanUtility.IsValidIndex(destination, i); i--)
                    {
                        char asUpperHex = HexConverter.ToCharUpper((int)scalarValue);
                        destination[i] = (byte)asUpperHex;
                        scalarValue >>= 4; // write a nibble - not a byte - at a time
                    }

                    return destination.Length + 4;

                OutOfSpaceInner:

                    return -1;
                }
            }

            internal override int EncodeUtf16(Rune value, Span<char> destination)
            {
                if (value.Value == '<')
                {
                    if (!SpanUtility.TryWriteChars(destination, '&', 'l', 't', ';')) { goto OutOfSpace; }
                    return 4;
                }
                else if (value.Value == '>')
                {
                    if (!SpanUtility.TryWriteChars(destination, '&', 'g', 't', ';')) { goto OutOfSpace; }
                    return 4;
                }
                else if (value.Value == '&')
                {
                    if (!SpanUtility.TryWriteChars(destination, '&', 'a', 'm', 'p', ';')) { goto OutOfSpace; }
                    return 5;
                }
                else if (value.Value == '\"')
                {
                    if (!SpanUtility.TryWriteChars(destination, '&', 'q', 'u', 'o', 't', ';')) { goto OutOfSpace; }
                    return 6;
                }
                else
                {
                    return TryEncodeScalarAsHex(this, (uint)value.Value, destination);
                }

            OutOfSpace:

                return -1;

#pragma warning disable IDE0060 // 'this' taken explicitly to avoid argument shuffling by caller
                static int TryEncodeScalarAsHex(object @this, uint scalarValue, Span<char> destination)
#pragma warning restore IDE0060
                {
                    UnicodeDebug.AssertIsValidScalar(scalarValue);

                    // For inputs 0x0000..0x10FFFF, log2 will return 0..20.
                    // (It counts the number of bits following the highest set bit.)
                    //
                    // We divide by 4 to get the number of nibbles (this rounds down),
                    // then +1 to account for rounding effects. This also accounts for
                    // that when log2 results in an exact multiple of 4, no rounding has
                    // taken place, but we need to include a char for the preceding '0x1'.
                    // Finally, we +4 to account for the "&#x" prefix and the ";" suffix,
                    // then -1 to get the index of the last legal location we want to write to.
                    // >> +1 +4 -1 = +4

                    int idxOfSemicolon = (int)((uint)BitOperations.Log2(scalarValue) / 4) + 4;
                    Debug.Assert(4 <= idxOfSemicolon && idxOfSemicolon <= 9, "Expected '&#x0;'..'&#x10FFFF;'.");

                    if (!SpanUtility.IsValidIndex(destination, idxOfSemicolon)) { goto OutOfSpaceInner; }
                    destination[idxOfSemicolon] = ';';

                    // It's more efficient to write 4 chars at a time instead of 1 char.
                    // The '0' at the end will be overwritten.
                    if (!SpanUtility.TryWriteChars(destination, '&', '#', 'x', '0'))
                    {
                        Debug.Fail("We should've had enough room to write 4 chars.");
                    }

                    destination = destination.Slice(3, idxOfSemicolon - 3);
                    for (int i = destination.Length - 1; SpanUtility.IsValidIndex(destination, i); i--)
                    {
                        char asUpperHex = HexConverter.ToCharUpper((int)scalarValue);
                        destination[i] = asUpperHex;
                        scalarValue >>= 4; // write a nibble - not a byte - at a time
                    }

                    return destination.Length + 4;

                OutOfSpaceInner:

                    return -1;
                }
            }
        }
    }
}
