// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Internal;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// Represents a type used to do URL encoding.
    /// </summary>
    public abstract class UrlEncoder : TextEncoder
    {
        /// <summary>
        /// Returns a default built-in instance of <see cref="UrlEncoder"/>.
        /// </summary>
        public static UrlEncoder Default
        {
            get { return DefaultUrlEncoder.Singleton; }
        }

        /// <summary>
        /// Creates a new instance of UrlEncoder with provided settings.
        /// </summary>
        /// <param name="settings">Settings used to control how the created <see cref="UrlEncoder"/> encodes, primarily which characters to encode.</param>
        /// <returns>A new instance of the <see cref="UrlEncoder"/>.</returns>
        public static UrlEncoder Create(TextEncoderSettings settings)
        {
            return new DefaultUrlEncoder(settings);
        }

        /// <summary>
        /// Creates a new instance of UrlEncoder specifying character to be encoded.
        /// </summary>
        /// <param name="allowedRanges">Set of characters that the encoder is allowed to not encode.</param>
        /// <returns>A new instance of the <see cref="UrlEncoder"/>.</returns>
        /// <remarks>Some characters in <paramref name="allowedRanges"/> might still get encoded, i.e. this parameter is just telling the encoder what ranges it is allowed to not encode, not what characters it must not encode.</remarks> 
        public static UrlEncoder Create(params UnicodeRange[] allowedRanges)
        {
            return new DefaultUrlEncoder(allowedRanges);
        }
    }

    internal sealed class DefaultUrlEncoder : UrlEncoder
    {
        private AllowedCharactersBitmap _allowedCharacters;

        internal static readonly DefaultUrlEncoder Singleton = new DefaultUrlEncoder(new TextEncoderSettings(UnicodeRanges.BasicLatin));

        // We perform UTF8 conversion of input, which means that the worst case is
        // 12 output chars per input surrogate char: [input] U+FFFF U+FFFF -> [output] "%XX%YY%ZZ%WW".
        public override int MaxOutputCharactersPerInputCharacter
        {
            get { return 12; }
        }

        public DefaultUrlEncoder(TextEncoderSettings filter)
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
            // forget to HTML-encode a string once it has been URL-encoded,
            // so this offers extra protection.
            DefaultHtmlEncoder.ForbidHtmlCharacters(_allowedCharacters);

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

            const string forbiddenChars = @" #%/:=?[\]^`{|}"; // chars from Basic Latin which aren't already disallowed by the base encoder
            foreach (char character in forbiddenChars)
            {
                _allowedCharacters.ForbidCharacter(character);
            }

            // Specials (U+FFF0 .. U+FFFF) are forbidden by the definition of 'ucschar' above
            for (int i = 0; i < 16; i++)
            {
                _allowedCharacters.ForbidCharacter((char)(0xFFF0 | i));
            }
        }

        public DefaultUrlEncoder(params UnicodeRange[] allowedRanges) : this(new TextEncoderSettings(allowedRanges))
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool WillEncode(int unicodeScalar)
        {
            if (UnicodeHelpers.IsSupplementaryCodePoint(unicodeScalar)) return true;
            return !_allowedCharacters.IsUnicodeScalarAllowed(unicodeScalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe override int FindFirstCharacterToEncode(char* text, int textLength)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }
            return _allowedCharacters.FindFirstCharacterToEncode(text, textLength);
        }

        public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (!WillEncode(unicodeScalar)) { return TryWriteScalarAsChar(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten); }

            numberOfCharactersWritten = 0;
            uint asUtf8 = unchecked((uint)UnicodeHelpers.GetUtf8RepresentationForScalarValue((uint)unicodeScalar));
            do
            {
                char highNibble, lowNibble;
                HexUtil.ByteToHexDigits(unchecked((byte)asUtf8), out highNibble, out lowNibble);

                if (numberOfCharactersWritten + 3 > bufferLength)
                {
                    numberOfCharactersWritten = 0;
                    return false;
                }

                *buffer = '%'; buffer++;
                *buffer = highNibble; buffer++;
                *buffer = lowNibble; buffer++;

                numberOfCharactersWritten += 3;
            }
            while ((asUtf8 >>= 8) != 0);
            return true;
        }
    }
}
