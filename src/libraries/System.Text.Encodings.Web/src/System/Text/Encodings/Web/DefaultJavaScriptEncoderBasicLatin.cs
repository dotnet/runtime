// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Internal;
using System.Text.Unicode;

#if NETCOREAPP
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
#endif

namespace System.Text.Encodings.Web
{
    internal sealed class DefaultJavaScriptEncoderBasicLatin : JavaScriptEncoder
    {
        internal static readonly DefaultJavaScriptEncoderBasicLatin s_singleton = new DefaultJavaScriptEncoderBasicLatin();

        private DefaultJavaScriptEncoderBasicLatin()
        {
            var filter = new TextEncoderSettings(UnicodeRanges.BasicLatin);

            AllowedCharactersBitmap allowedCharacters = filter.GetAllowedCharacters();

            // Forbid codepoints which aren't mapped to characters or which are otherwise always disallowed
            // (includes categories Cc, Cs, Co, Cn, Zs [except U+0020 SPACE], Zl, Zp)
            allowedCharacters.ForbidUndefinedCharacters();

            // Forbid characters that are special in HTML.
            // Even though this is a not HTML encoder,
            // it's unfortunately common for developers to
            // forget to HTML-encode a string once it has been JS-encoded,
            // so this offers extra protection.
            DefaultHtmlEncoder.ForbidHtmlCharacters(allowedCharacters);

            // '\' (U+005C REVERSE SOLIDUS) must always be escaped in Javascript / ECMAScript / JSON.
            // '/' (U+002F SOLIDUS) is not Javascript / ECMAScript / JSON-sensitive so doesn't need to be escaped.
            allowedCharacters.ForbidCharacter('\\');

            // '`' (U+0060 GRAVE ACCENT) is ECMAScript-sensitive (see ECMA-262).
            allowedCharacters.ForbidCharacter('`');

#if DEBUG
            // Verify and ensure that the AllowList bit map matches the set of allowed characters using AllowedCharactersBitmap
            for (int i = 0; i < AllowList.Length; i++)
            {
                char ch = (char)i;
                Debug.Assert((allowedCharacters.IsCharacterAllowed(ch) ? 1 : 0) == AllowList[ch]);
                Debug.Assert(allowedCharacters.IsCharacterAllowed(ch) == !NeedsEscaping(ch));
            }
            for (int i = AllowList.Length; i <= char.MaxValue; i++)
            {
                char ch = (char)i;
                Debug.Assert(!allowedCharacters.IsCharacterAllowed(ch));
                Debug.Assert(NeedsEscaping(ch));
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool WillEncode(int unicodeScalar)
        {
            if (UnicodeHelpers.IsSupplementaryCodePoint(unicodeScalar))
            {
                return true;
            }

            Debug.Assert(unicodeScalar >= char.MinValue && unicodeScalar <= char.MaxValue);

            return NeedsEscaping((char)unicodeScalar);
        }

        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            Debug.Assert(textLength >= 0);

            if (textLength == 0)
            {
                goto AllAllowed;
            }

            int idx = 0;
            short* ptr = (short*)text;
            short* end = ptr + (uint)textLength;

#if NETCOREAPP
            if (Sse2.IsSupported || (AdvSimd.Arm64.IsSupported && BitConverter.IsLittleEndian))
            {
                if (textLength >= Vector128<short>.Count)
                {
                    goto VectorizedStart;
                }
            }

        Sequential:
#endif
            Debug.Assert(textLength > 0 && ptr < end);

            do
            {
                Debug.Assert(text <= ptr && ptr < (text + textLength));

                if (NeedsEscaping(*(char*)ptr))
                {
                    goto Return;
                }

                ptr++;
                idx++;
            }
            while (ptr < end);

        AllAllowed:
            idx = -1;

        Return:
            return idx;

#if NETCOREAPP
        VectorizedStart:
            int index;
            short* vectorizedEnd;

            if (textLength >= 2 * Vector128<short>.Count)
            {
                vectorizedEnd = end - 2 * Vector128<short>.Count;

                do
                {
                    Debug.Assert(text <= ptr && ptr <= (text + textLength - 2 * Vector128<short>.Count));

                    // Load the next 16 characters, combine them to one byte vector.
                    // Chars that don't cleanly convert to ASCII bytes will get converted (saturated) to
                    // somewhere in the range [0x7F, 0xFF], which the NeedsEscaping method will detect.
                    Vector128<sbyte> sourceValue;

                    if (Sse2.IsSupported)
                    {
                        sourceValue = Sse2.PackSignedSaturate(
                            Sse2.LoadVector128(ptr),
                            Sse2.LoadVector128(ptr + Vector128<short>.Count));
                    }
                    else if (AdvSimd.Arm64.IsSupported)
                    {
                        Vector64<sbyte> lower = AdvSimd.ExtractNarrowingSaturateLower(AdvSimd.LoadVector128(ptr));
                        sourceValue = AdvSimd.ExtractNarrowingSaturateUpper(lower, AdvSimd.LoadVector128(ptr + Vector128<short>.Count));
                    }
                    else
                    {
                        throw new PlatformNotSupportedException();
                    }

                    // Check if any of the 16 characters need to be escaped.
                    index = NeedsEscaping(sourceValue);

                    // If index >= 16, that means none of the 16 characters needed to be escaped.
                    if (index < 16)
                    {
                        goto VectorizedFound;
                    }

                    ptr += 2 * Vector128<short>.Count;
                }
                while (ptr <= vectorizedEnd);
            }

            vectorizedEnd = end - Vector128<short>.Count;

        Vectorized:
            // PERF: JIT produces better code for do-while as for a while-loop (no spills)
            if (ptr <= vectorizedEnd)
            {
                do
                {
                    Debug.Assert(text <= ptr && ptr <= (text + textLength - Vector128<short>.Count));

                    // Load the next 8 characters + a dummy known that it must not be escaped.
                    // Put the dummy second, so it's easier for GetIndexOfFirstNeedToEscape.
                    Vector128<sbyte> sourceValue;

                    if (Sse2.IsSupported)
                    {
                        sourceValue = Sse2.PackSignedSaturate(
                            Sse2.LoadVector128(ptr),
                            Vector128.Create((short)'A'));  // max. one "iteration", so no need to cache this vector
                    }
                    else if (AdvSimd.Arm64.IsSupported)
                    {
                        Vector64<sbyte> saturated = AdvSimd.ExtractNarrowingSaturateLower(AdvSimd.LoadVector128(ptr));
                        sourceValue = Vector128.Create(saturated, Vector64.Create((sbyte)'A'));
                    }
                    else
                    {
                        throw new PlatformNotSupportedException();
                    }

                    index = NeedsEscaping(sourceValue);

                    // If index >= 16, that means none of the 16 bytes needed to be escaped.
                    if (index < 16)
                    {
                        goto VectorizedFound;
                    }

                    ptr += Vector128<short>.Count;
                }
                while (ptr <= vectorizedEnd);
            }

            // Process the remaining characters.
            Debug.Assert(end - ptr < Vector128<short>.Count);

            // Process the remaining elements vectorized, only if the remaining count
            // is above thresholdForRemainingVectorized, otherwise process them sequential.
            // Threshold found by testing.
            const int thresholdForRemainingVectorized = 5;
            if (ptr < end - thresholdForRemainingVectorized)
            {
                ptr = vectorizedEnd;
                goto Vectorized;
            }

            idx = CalculateIndex(ptr, text);

            if (idx < textLength)
            {
                goto Sequential;
            }

            goto AllAllowed;

        VectorizedFound:
            index += CalculateIndex(ptr, text);
            return index;

            static int CalculateIndex(short* ptr, char* text)
            {
                // Subtraction with short* results in a idiv, so use byte* and shift
                return (int)(((byte*)ptr - (byte*)text) >> 1);
            }
#endif
        }

        public override unsafe int FindFirstCharacterToEncodeUtf8(ReadOnlySpan<byte> utf8Text)
        {
            fixed (byte* pValue = utf8Text)
            {
                uint textLength = (uint)utf8Text.Length;

                if (textLength == 0)
                {
                    goto AllAllowed;
                }

                int idx = 0;
                byte* ptr = pValue;
                byte* end = ptr + textLength;

#if NETCOREAPP
                if (Sse2.IsSupported || (AdvSimd.Arm64.IsSupported && BitConverter.IsLittleEndian))
                {
                    if (textLength >= Vector128<sbyte>.Count)
                    {
                        goto Vectorized;
                    }
                }

            Sequential:
#endif
                Debug.Assert(textLength > 0 && ptr < end);

                do
                {
                    Debug.Assert(pValue <= ptr && ptr < (pValue + utf8Text.Length));

                    if (NeedsEscaping(*ptr))
                    {
                        goto Return;
                    }

                    ptr++;
                    idx++;
                }
                while (ptr < end);

            AllAllowed:
                idx = -1;

            Return:
                return idx;

#if NETCOREAPP
            Vectorized:
                byte* vectorizedEnd = end - Vector128<byte>.Count;
                int index;

                do
                {
                    Debug.Assert(pValue <= ptr && ptr <= (pValue + utf8Text.Length - Vector128<byte>.Count));

                    // Load the next 16 bytes
                    Vector128<sbyte> sourceValue = Sse2.IsSupported ?
                        Sse2.LoadVector128((sbyte*)ptr) :
                        AdvSimd.LoadVector128((sbyte*)ptr);

                    index = NeedsEscaping(sourceValue);

                    // If index >= 16, that means none of the 16 bytes needed to be escaped.
                    if (index < 16)
                    {
                        goto VectorizedFound;
                    }

                    ptr += Vector128<sbyte>.Count;
                }
                while (ptr <= vectorizedEnd);

                // Process the remaining elements.
                Debug.Assert(end - ptr < Vector128<byte>.Count);

                // Process the remaining elements vectorized, only if the remaining count
                // is above thresholdForRemainingVectorized, otherwise process them sequential.
                const int thresholdForRemainingVectorized = 4;
                if (ptr < end - thresholdForRemainingVectorized)
                {
                    // PERF: duplicate instead of jumping at the beginning of the previous loop
                    // otherwise all the static data (vectors) will be re-assigned to registers,
                    // so they are re-used.

                    Debug.Assert(pValue <= vectorizedEnd && vectorizedEnd <= (pValue + utf8Text.Length - Vector128<byte>.Count));

                    // Load the last 16 bytes
                    Vector128<sbyte> sourceValue = Sse2.IsSupported ?
                        Sse2.LoadVector128((sbyte*)vectorizedEnd) :
                        AdvSimd.LoadVector128((sbyte*)vectorizedEnd);

                    // If index >= 16, that means none of the 16 bytes needed to be escaped.
                    index = NeedsEscaping(sourceValue);
                    if (index < 16)
                    {
                        ptr = vectorizedEnd;
                        goto VectorizedFound;
                    }

                    goto AllAllowed;
                }

                idx = CalculateIndex(ptr, pValue);

                if (idx < textLength)
                {
                    goto Sequential;
                }

                goto AllAllowed;

            VectorizedFound:
                index += CalculateIndex(ptr, pValue);
                return index;

                static int CalculateIndex(byte* ptr, byte* pValue) => (int)(ptr - pValue);
#endif
            }
        }

        // The worst case encoding is 6 output chars per input char: [input] U+FFFF -> [output] "\uFFFF"
        // We don't need to worry about astral code points since they're represented as encoded
        // surrogate pairs in the output.
        public override int MaxOutputCharactersPerInputCharacter => 12; // "\uFFFF\uFFFF" is the longest encoded form

        private static readonly char[] s_b = new char[] { '\\', 'b' };
        private static readonly char[] s_t = new char[] { '\\', 't' };
        private static readonly char[] s_n = new char[] { '\\', 'n' };
        private static readonly char[] s_f = new char[] { '\\', 'f' };
        private static readonly char[] s_r = new char[] { '\\', 'r' };
        private static readonly char[] s_back = new char[] { '\\', '\\' };

        // Writes a scalar value as a JavaScript-escaped character (or sequence of characters).
        // See ECMA-262, Sec. 7.8.4, and ECMA-404, Sec. 9
        // https://www.ecma-international.org/ecma-262/5.1/#sec-7.8.4
        // https://www.ecma-international.org/publications/files/ECMA-ST/ECMA-404.pdf
        public override unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            // ECMA-262 allows encoding U+000B as "\v", but ECMA-404 does not.
            // Both ECMA-262 and ECMA-404 allow encoding U+002F SOLIDUS as "\/"
            // (in ECMA-262 this character is a NonEscape character); however, we
            // don't encode SOLIDUS by default unless the caller has provided an
            // explicit bitmap which does not contain it. In this case we'll assume
            // that the caller didn't want a SOLIDUS written to the output at all,
            // so it should be written using "\u002F" encoding.
            // HTML-specific characters (including apostrophe and quotes) will
            // be written out as numeric entities for defense-in-depth.
            // See UnicodeEncoderBase ctor comments for more info.

            if (!WillEncode(unicodeScalar))
            {
                return TryWriteScalarAsChar(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten);
            }

            char[] toCopy;
            switch (unicodeScalar)
            {
                case '\b':
                    toCopy = s_b;
                    break;
                case '\t':
                    toCopy = s_t;
                    break;
                case '\n':
                    toCopy = s_n;
                    break;
                case '\f':
                    toCopy = s_f;
                    break;
                case '\r':
                    toCopy = s_r;
                    break;
                case '\\':
                    toCopy = s_back;
                    break;
                default:
                    return JavaScriptEncoderHelper.TryWriteEncodedScalarAsNumericEntity(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten);
            }
            return TryCopyCharacters(toCopy, buffer, bufferLength, out numberOfCharactersWritten);
        }

        private static ReadOnlySpan<byte> AllowList => new byte[byte.MaxValue + 1]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // U+0000..U+000F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // U+0010..U+001F
            1, 1, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 1, 1, 1, 1, // U+0020..U+002F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1, // U+0030..U+003F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // U+0040..U+004F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, // U+0050..U+005F
            0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // U+0060..U+006F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, // U+0070..U+007F

            // Also include the ranges from U+0080 to U+00FF for performance to avoid UTF8 code from checking boundary.
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // U+00F0..U+00FF
        };

        public const int LastAsciiCharacter = 0x7F;

        private static bool NeedsEscaping(byte value) => AllowList[value] == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NeedsEscaping(char value) => value > LastAsciiCharacter || AllowList[value] == 0;

#if NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int NeedsEscaping(Vector128<sbyte> sourceValue)
        {
            Debug.Assert(Sse2.IsSupported || AdvSimd.Arm64.IsSupported);

            if (Sse2.IsSupported)
            {
                // Check if any of the 16 bytes need to be escaped.
                Vector128<sbyte> mask = Ssse3.IsSupported
                    ? Ssse3Helper.CreateEscapingMask_DefaultJavaScriptEncoderBasicLatin(sourceValue)
                    : Sse2Helper.CreateEscapingMask_DefaultJavaScriptEncoderBasicLatin(sourceValue);

                int index = Sse2Helper.GetIndexOfFirstNonAsciiByte(mask.AsByte());
                return index;
            }
            else
            {
                Vector128<sbyte> mask = AdvSimdHelper.CreateEscapingMask_DefaultJavaScriptEncoderBasicLatin(sourceValue);
                int index = AdvSimdHelper.GetIndexOfFirstNonAsciiByte(mask.AsByte());
                return index;
            }
        }
#endif
    }
}
