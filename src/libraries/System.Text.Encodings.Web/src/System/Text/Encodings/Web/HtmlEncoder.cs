// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Internal;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    public abstract class HtmlEncoder : TextEncoder
    {
        public static HtmlEncoder Default
        {
            get { return DefaultHtmlEncoder.Singleton; }
        }
        public static HtmlEncoder Create(CodePointFilter filter)
        {
            return new DefaultHtmlEncoder(filter);
        }
        public static HtmlEncoder Create(params UnicodeRange[] allowedRanges)
        {
            return new DefaultHtmlEncoder(allowedRanges);
        }
    }

    public sealed class DefaultHtmlEncoder : HtmlEncoder
    {
        private AllowedCharsBitmap _allowedCharsBitmap;
        internal readonly static DefaultHtmlEncoder Singleton = new DefaultHtmlEncoder(new CodePointFilter(UnicodeRanges.BasicLatin));

        public DefaultHtmlEncoder(CodePointFilter filter)
        {
            _allowedCharsBitmap = filter.GetAllowedCharsBitmap();

            // Forbid codepoints which aren't mapped to characters or which are otherwise always disallowed
            // (includes categories Cc, Cs, Co, Cn, Zs [except U+0020 SPACE], Zl, Zp)
            _allowedCharsBitmap.ForbidUndefinedCharacters();

            ForbidHtmlCharacters(_allowedCharsBitmap);
        }

        internal static void ForbidHtmlCharacters(AllowedCharsBitmap allowedCharsBitmap)
        {
            allowedCharsBitmap.ForbidCharacter('<');
            allowedCharsBitmap.ForbidCharacter('>');
            allowedCharsBitmap.ForbidCharacter('&');
            allowedCharsBitmap.ForbidCharacter('\''); // can be used to escape attributes
            allowedCharsBitmap.ForbidCharacter('\"'); // can be used to escape attributes
            allowedCharsBitmap.ForbidCharacter('+'); // technically not HTML-specific, but can be used to perform UTF7-based attacks
        }

        public DefaultHtmlEncoder(params UnicodeRange[] allowedRanges) : this(new CodePointFilter(allowedRanges))
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

        public override int MaxOutputCharsPerInputChar
        {
            get { return 8; } // "&#xFFFF;" is the longest encoded form 
        }

        static readonly char[] quote = "&quot;".ToCharArray();
        static readonly char[] ampersand = "&amp;".ToCharArray();
        static readonly char[] lessthan = "&lt;".ToCharArray();
        static readonly char[] greaterthan = "&gt;".ToCharArray();

        public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int length, out int writtenChars)
        {
            if (!Encodes(unicodeScalar)) { return unicodeScalar.TryWriteScalarAsChar(buffer, length, out writtenChars); }
            else if (unicodeScalar == '\"') { return quote.TryCopyCharacters(buffer, length, out writtenChars); }
            else if (unicodeScalar == '&') { return ampersand.TryCopyCharacters(buffer, length, out writtenChars); }
            else if (unicodeScalar == '<') { return lessthan.TryCopyCharacters(buffer, length, out writtenChars); }
            else if (unicodeScalar == '>') { return greaterthan.TryCopyCharacters(buffer, length, out writtenChars); }
            else { return TryWriteEncodedScalarAsNumericEntity(unicodeScalar, buffer, length, out writtenChars); }
        }

        private unsafe static bool TryWriteEncodedScalarAsNumericEntity(int unicodeScalar, char* buffer, int length, out int writtenChars)
        {
            // We're building the characters up in reverse
            char* chars = stackalloc char[8 /* "FFFFFFFF" */];
            int numCharsWritten = 0;
            do
            {
                Debug.Assert(numCharsWritten < 8, "Couldn't have written 8 characters out by this point.");
                // Pop off the last nibble
                chars[numCharsWritten++] = HexUtil.ToHexDigit(unicodeScalar & 0xF);
                unicodeScalar >>= 4;
            } while (unicodeScalar != 0);

            writtenChars = numCharsWritten + 4; // four chars are &, #, x, and ;
            Debug.Assert(numCharsWritten > 0, "At least one character should've been written.");

            if (numCharsWritten + 4 > length)
            {
                writtenChars = 0;
                return false;
            }
            // Finally, write out the HTML-encoded scalar value.
            *buffer = '&';
            buffer++;
            *buffer = '#';
            buffer++;
            *buffer = 'x';
            buffer++;
            do
            {
                *buffer = chars[--numCharsWritten];
                buffer++;
            }
            while (numCharsWritten != 0);

            *buffer = ';';
            return true;
        }
    }
}
