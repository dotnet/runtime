// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
    internal sealed class DefaultJavaScriptEncoder : JavaScriptEncoder
    {
        private readonly AllowedCharactersBitmap _allowedCharacters;

        private readonly int[] _asciiNeedsEscaping = new int[0x80];

        public DefaultJavaScriptEncoder(TextEncoderSettings filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            _allowedCharacters = filter.GetAllowedCharacters();

            // Forbid codepoints which aren't mapped to characters or which are otherwise always disallowed
            // (includes categories Cc, Cs, Co, Cn, Zs [except U+0020 SPACE], Zl, Zp)
            _allowedCharacters.ForbidUndefinedCharacters();

            // Forbid characters that are special in HTML.
            // Even though this is a not HTML encoder,
            // it's unfortunately common for developers to
            // forget to HTML-encode a string once it has been JS-encoded,
            // so this offers extra protection.
            DefaultHtmlEncoder.ForbidHtmlCharacters(_allowedCharacters);

            // '\' (U+005C REVERSE SOLIDUS) must always be escaped in Javascript / ECMAScript / JSON.
            // '/' (U+002F SOLIDUS) is not Javascript / ECMAScript / JSON-sensitive so doesn't need to be escaped.
            _allowedCharacters.ForbidCharacter('\\');

            // '`' (U+0060 GRAVE ACCENT) is ECMAScript-sensitive (see ECMA-262).
            _allowedCharacters.ForbidCharacter('`');

            for (int i = 0; i < _asciiNeedsEscaping.Length; i++)
            {
                _asciiNeedsEscaping[i] = WillEncode(i) ? 1 : -1;
            }
        }

        public DefaultJavaScriptEncoder(params UnicodeRange[] allowedRanges) : this(new TextEncoderSettings(allowedRanges))
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool WillEncode(int unicodeScalar)
        {
            if (UnicodeHelpers.IsSupplementaryCodePoint(unicodeScalar))
            {
                return true;
            }

            Debug.Assert(unicodeScalar >= char.MinValue && unicodeScalar <= char.MaxValue);

            return !_allowedCharacters.IsUnicodeScalarAllowed(unicodeScalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return _allowedCharacters.FindFirstCharacterToEncode(text, textLength);
        }

        public override unsafe int FindFirstCharacterToEncodeUtf8(ReadOnlySpan<byte> utf8Text)
        {
            fixed (byte* ptr = utf8Text)
            {
                int idx = 0;

#if NETCOREAPP
                if (Sse2.IsSupported || AdvSimd.Arm64.IsSupported)
                {
                    sbyte* startingAddress = (sbyte*)ptr;
                    while (utf8Text.Length - 16 >= idx)
                    {
                        Debug.Assert(startingAddress >= ptr && startingAddress <= (ptr + utf8Text.Length - 16));

                        bool containsNonAsciiBytes;

                        // Load the next 16 bytes, and check for ASCII text.
                        // Any byte that's not in the ASCII range will already be negative when casted to signed byte.
                        if (Sse2.IsSupported)
                        {
                            Vector128<sbyte> sourceValue = Sse2.LoadVector128(startingAddress);
                            containsNonAsciiBytes = Sse2Helper.ContainsNonAsciiByte(sourceValue);
                        }
                        else if (AdvSimd.Arm64.IsSupported)
                        {
                            Vector128<sbyte> sourceValue = AdvSimd.LoadVector128(startingAddress);
                            containsNonAsciiBytes = AdvSimdHelper.ContainsNonAsciiByte(sourceValue);
                        }
                        else
                        {
                            throw new PlatformNotSupportedException();
                        }

                        if (containsNonAsciiBytes)
                        {
                            // At least one of the following 16 bytes is non-ASCII.

                            int processNextSixteen = idx + 16;
                            Debug.Assert(processNextSixteen <= utf8Text.Length);

                            while (idx < processNextSixteen)
                            {
                                Debug.Assert((ptr + idx) <= (ptr + utf8Text.Length));

                                if (UnicodeUtility.IsAsciiCodePoint(ptr[idx]))
                                {
                                    if (DoesAsciiNeedEncoding(ptr[idx]) == 1)
                                    {
                                        goto Return;
                                    }
                                    idx++;
                                }
                                else
                                {
                                    OperationStatus opStatus = UnicodeHelpers.DecodeScalarValueFromUtf8(utf8Text.Slice(idx), out uint nextScalarValue, out int utf8BytesConsumedForScalar);

                                    Debug.Assert(nextScalarValue <= int.MaxValue);
                                    if (opStatus != OperationStatus.Done || WillEncode((int)nextScalarValue))
                                    {
                                        goto Return;
                                    }

                                    Debug.Assert(opStatus == OperationStatus.Done);
                                    idx += utf8BytesConsumedForScalar;
                                }
                            }
                        }
                        else
                        {
                            if (DoesAsciiNeedEncoding(ptr[idx]) == 1

                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1
                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1
                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1
                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1

                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1
                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1
                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1
                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1

                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1
                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1
                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1
                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1

                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1
                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1
                                || DoesAsciiNeedEncoding(ptr[++idx]) == 1)
                            {
                                goto Return;
                            }
                            idx++;
                        }
                        startingAddress = (sbyte*)ptr + idx;
                    }

                    // Process the remaining bytes.
                    Debug.Assert(utf8Text.Length - idx < 16);
                }
#endif

                while (idx < utf8Text.Length)
                {
                    Debug.Assert((ptr + idx) <= (ptr + utf8Text.Length));

                    if (UnicodeUtility.IsAsciiCodePoint(ptr[idx]))
                    {
                        if (DoesAsciiNeedEncoding(ptr[idx]) == 1)
                        {
                            goto Return;
                        }
                        idx++;
                    }
                    else
                    {
                        OperationStatus opStatus = UnicodeHelpers.DecodeScalarValueFromUtf8(utf8Text.Slice(idx), out uint nextScalarValue, out int utf8BytesConsumedForScalar);

                        Debug.Assert(nextScalarValue <= int.MaxValue);
                        if (opStatus != OperationStatus.Done || WillEncode((int)nextScalarValue))
                        {
                            goto Return;
                        }

                        Debug.Assert(opStatus == OperationStatus.Done);
                        idx += utf8BytesConsumedForScalar;
                    }
                }
                Debug.Assert(idx == utf8Text.Length);

                idx = -1; // All bytes are allowed.

            Return:
                return idx;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int DoesAsciiNeedEncoding(byte value)
        {
            Debug.Assert(value <= 0x7F);

            int needsEscaping = _asciiNeedsEscaping[value];

            Debug.Assert(needsEscaping == 1 || needsEscaping == -1);

            return needsEscaping;
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
                case '\b': toCopy = s_b; break;
                case '\t': toCopy = s_t; break;
                case '\n': toCopy = s_n; break;
                case '\f': toCopy = s_f; break;
                case '\r': toCopy = s_r; break;
                case '\\': toCopy = s_back; break;
                default: return JavaScriptEncoderHelper.TryWriteEncodedScalarAsNumericEntity(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten);
            }
            return TryCopyCharacters(toCopy, buffer, bufferLength, out numberOfCharactersWritten);
        }
    }
}
