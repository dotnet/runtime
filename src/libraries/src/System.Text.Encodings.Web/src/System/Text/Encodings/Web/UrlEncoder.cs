// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Internal;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    public abstract class UrlEncoder : TextEncoder
    {
        public static UrlEncoder Default
        {
            get { return DefaultUrlEncoder.Singleton; }
        }
        public static UrlEncoder Create(CodePointFilter filter)
        {
            return new DefaultUrlEncoder(filter);
        }
        public static UrlEncoder Create(params UnicodeRange[] allowedRanges)
        {
            return new DefaultUrlEncoder(allowedRanges);
        }
    }

    public sealed class DefaultUrlEncoder : UrlEncoder
    {
        private AllowedCharsBitmap _allowedCharsBitmap;

        internal readonly static DefaultUrlEncoder Singleton = new DefaultUrlEncoder(new CodePointFilter(UnicodeRanges.BasicLatin));

        // We perform UTF8 conversion of input, which means that the worst case is
        // 9 output chars per input char: [input] U+FFFF -> [output] "%XX%YY%ZZ".
        // We don't need to worry about astral code points since they consume 2 input
        // chars to produce 12 output chars "%XX%YY%ZZ%WW", which is 6 output chars per input char.
        public override int MaxOutputCharsPerInputChar
        {
            get { return 9; }
        }

        public DefaultUrlEncoder(CodePointFilter filter)
        {
            _allowedCharsBitmap = filter.GetAllowedCharsBitmap();

            // Forbid codepoints which aren't mapped to characters or which are otherwise always disallowed
            // (includes categories Cc, Cs, Co, Cn, Zs [except U+0020 SPACE], Zl, Zp)
            _allowedCharsBitmap.ForbidUndefinedCharacters();

            // Forbid characters that are special in HTML.
            // Even though this is a not HTML encoder, 
            // it's unfortunately common for developers to
            // forget to HTML-encode a string once it has been URL-encoded,
            // so this offers extra protection.
            DefaultHtmlEncoder.ForbidHtmlCharacters(_allowedCharsBitmap);

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
            foreach (char c in forbiddenChars)
            {
                _allowedCharsBitmap.ForbidCharacter(c);
            }

            // Specials (U+FFF0 .. U+FFFF) are forbidden by the definition of 'ucschar' above
            for (int i = 0; i < 16; i++)
            {
                _allowedCharsBitmap.ForbidCharacter((char)(0xFFF0 | i));
            }

            // Supplementary characters are forbidden anyway by the base encoder //TODO: make sure it's true after the changes
        }

        public DefaultUrlEncoder(params UnicodeRange[] allowedRanges) : this(new CodePointFilter(allowedRanges))
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Encodes(int unicodeScalar)
        {
            if (UnicodeHelpers.IsSupplementaryCodePoint(unicodeScalar)) return true;
            return !_allowedCharsBitmap.IsUnicodeScalarAllowed(unicodeScalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe override int FindFirstCharacterToEncode(char* text, int charCount)
        {
            return AllowedCharsBitmap.FindFirstCharacterToEncode(_allowedCharsBitmap, text, charCount);
        }

        public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int length, out int writtenChars)
        {
            if (!Encodes(unicodeScalar)) { return unicodeScalar.TryWriteScalarAsChar(buffer, length, out writtenChars); }

            writtenChars = 0;
            uint asUtf8 = (uint)UnicodeHelpers.GetUtf8RepresentationForScalarValue((uint)unicodeScalar);
            do
            {
                char highNibble, lowNibble;
                HexUtil.WriteHexEncodedByte((byte)asUtf8, out highNibble, out lowNibble);
                if (length < 3)
                {
                    writtenChars = 0;
                    return false;
                }
                *buffer = '%'; buffer++;
                *buffer = highNibble; buffer++;
                *buffer = lowNibble; buffer++;

                writtenChars += 3;
            }
            while ((asUtf8 >>= 8) != 0);
            return true;
        }
    }
}
