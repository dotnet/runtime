// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Internal;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    public abstract class JavaScriptEncoder : TextEncoder
    {
        public static JavaScriptEncoder Default
        {
            get { return DefaultJavaScriptEncoder.Singleton; }
        }
        public static JavaScriptEncoder Create(CodePointFilter filter)
        {
            return new DefaultJavaScriptEncoder(filter);
        }
        public static JavaScriptEncoder Create(params UnicodeRange[] allowedRanges)
        {
            return new DefaultJavaScriptEncoder(allowedRanges);
        }
    }

    public sealed class DefaultJavaScriptEncoder : JavaScriptEncoder
    {
        private AllowedCharactersBitmap _allowedCharacters;

        internal readonly static DefaultJavaScriptEncoder Singleton = new DefaultJavaScriptEncoder(new CodePointFilter(UnicodeRanges.BasicLatin));

        public DefaultJavaScriptEncoder(CodePointFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException("filter");
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

            _allowedCharacters.ForbidCharacter('\\');
            _allowedCharacters.ForbidCharacter('/');
        }

        public DefaultJavaScriptEncoder(params UnicodeRange[] allowedRanges) : this(new CodePointFilter(allowedRanges))
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Encodes(int unicodeScalar)
        {
            if (UnicodeHelpers.IsSupplementaryCodePoint(unicodeScalar)) return true;
            return !_allowedCharacters.IsUnicodeScalarAllowed(unicodeScalar);
        }

        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe override int FindFirstCharacterToEncode(char* text, int textLength)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            return _allowedCharacters.FindFirstCharacterToEncode(text, textLength);
        }

        // The worst case encoding is 6 output chars per input char: [input] U+FFFF -> [output] "\uFFFF"
        // We don't need to worry about astral code points since they're represented as encoded
        // surrogate pairs in the output.
        public override int MaxOutputCharactersPerInputCharacter
        {
            get { return 6; } // "\uFFFF" is the longest encoded form 
        }

        static readonly char[] b = new char[] { '\\', 'b' };
        static readonly char[] t = new char[] { '\\', 't' };
        static readonly char[] n = new char[] { '\\', 'n' };
        static readonly char[] f = new char[] { '\\', 'f' };
        static readonly char[] r = new char[] { '\\', 'r' };
        static readonly char[] forward = new char[] { '\\', '/' };
        static readonly char[] back = new char[] { '\\', '\\' };

        // Writes a scalar value as a JavaScript-escaped character (or sequence of characters).
        // See ECMA-262, Sec. 7.8.4, and ECMA-404, Sec. 9
        // http://www.ecma-international.org/ecma-262/5.1/#sec-7.8.4
        // http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-404.pdf
        [CLSCompliant(false)]
        public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            // ECMA-262 allows encoding U+000B as "\v", but ECMA-404 does not.
            // Both ECMA-262 and ECMA-404 allow encoding U+002F SOLIDUS as "\/".
            // (In ECMA-262 this character is a NonEscape character.)
            // HTML-specific characters (including apostrophe and quotes) will
            // be written out as numeric entities for defense-in-depth.
            // See UnicodeEncoderBase ctor comments for more info.

            if (!Encodes(unicodeScalar)) { return unicodeScalar.TryWriteScalarAsChar(buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '\b') { return b.TryCopyCharacters(buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '\t') { return t.TryCopyCharacters(buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '\n') { return n.TryCopyCharacters(buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '\f') { return f.TryCopyCharacters(buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '\r') { return r.TryCopyCharacters(buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '/') { return forward.TryCopyCharacters(buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '\\') { return back.TryCopyCharacters(buffer, bufferLength, out numberOfCharactersWritten); }
            else { return TryWriteEncodedScalarAsNumericEntity(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten); }
        }

        private unsafe static bool TryWriteEncodedScalarAsNumericEntity(int unicodeScalar, char* buffer, int length, out int numberOfCharactersWritten)
        {
            Debug.Assert(buffer != null && length >= 0);

            if (UnicodeHelpers.IsSupplementaryCodePoint(unicodeScalar))
            {
                // Convert this back to UTF-16 and write out both characters.
                char leadingSurrogate, trailingSurrogate;
                UnicodeHelpers.GetUtf16SurrogatePairFromAstralScalarValue(unicodeScalar, out leadingSurrogate, out trailingSurrogate);
                int leadingSurrogateCharactersWritten;
                if (TryWriteEncodedSingleCharacter(leadingSurrogate, buffer, length, out leadingSurrogateCharactersWritten) &&
                    TryWriteEncodedSingleCharacter(trailingSurrogate, buffer + leadingSurrogateCharactersWritten, length - leadingSurrogateCharactersWritten, out numberOfCharactersWritten)
                )
                {
                    numberOfCharactersWritten += leadingSurrogateCharactersWritten;
                    return true;
                }
                else
                {
                    numberOfCharactersWritten = 0;
                    return false;
                }
            }
            else
            {
                // This is only a single character.
                return TryWriteEncodedSingleCharacter(unicodeScalar, buffer, length, out numberOfCharactersWritten);
            }
        }

        // Writes an encoded scalar value (in the BMP) as a JavaScript-escaped character.
        private unsafe static bool TryWriteEncodedSingleCharacter(int unicodeScalar, char* buffer, int length, out int numberOfCharactersWritten)
        {
            Debug.Assert(buffer != null && length >= 0);
            Debug.Assert(!UnicodeHelpers.IsSupplementaryCodePoint(unicodeScalar), "The incoming value should've been in the BMP.");

            if (length < 6)
            {
                numberOfCharactersWritten = 0;
                return false;
            }

            // Encode this as 6 chars "\uFFFF".
            *buffer = '\\'; buffer++;
            *buffer = 'u'; buffer++;
            *buffer = HexUtil.Int32LsbToHexDigit(unicodeScalar >> 12); buffer++;
            *buffer = HexUtil.Int32LsbToHexDigit((int)((unicodeScalar >> 8) & 0xFU)); buffer++;
            *buffer = HexUtil.Int32LsbToHexDigit((int)((unicodeScalar >> 4) & 0xFU)); buffer++;
            *buffer = HexUtil.Int32LsbToHexDigit((int)(unicodeScalar & 0xFU)); buffer++;

            numberOfCharactersWritten = 6;
            return true;
        }
    }
}
