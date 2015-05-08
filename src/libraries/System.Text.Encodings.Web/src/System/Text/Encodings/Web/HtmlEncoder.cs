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
        private AllowedCharactersBitmap _allowedCharacters;
        internal readonly static DefaultHtmlEncoder Singleton = new DefaultHtmlEncoder(new CodePointFilter(UnicodeRanges.BasicLatin));

        public DefaultHtmlEncoder(CodePointFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException("filter");
            }

            _allowedCharacters = filter.GetAllowedCharacters();

            // Forbid codepoints which aren't mapped to characters or which are otherwise always disallowed
            // (includes categories Cc, Cs, Co, Cn, Zs [except U+0020 SPACE], Zl, Zp)
            _allowedCharacters.ForbidUndefinedCharacters();

            ForbidHtmlCharacters(_allowedCharacters);
        }

        internal static void ForbidHtmlCharacters(AllowedCharactersBitmap allowedCharacters)
        {
            allowedCharacters.ForbidCharacter('<');
            allowedCharacters.ForbidCharacter('>');
            allowedCharacters.ForbidCharacter('&');
            allowedCharacters.ForbidCharacter('\''); // can be used to escape attributes
            allowedCharacters.ForbidCharacter('\"'); // can be used to escape attributes
            allowedCharacters.ForbidCharacter('+'); // technically not HTML-specific, but can be used to perform UTF7-based attacks
        }

        public DefaultHtmlEncoder(params UnicodeRange[] allowedRanges) : this(new CodePointFilter(allowedRanges))
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
            return _allowedCharacters.FindFirstCharacterToEncode(text, textLength);
        }

        public override int MaxOutputCharactersPerInputCharacter
        {
            get { return 8; } // "&#xFFFF;" is the longest encoded form 
        }

        static readonly char[] quote = "&quot;".ToCharArray();
        static readonly char[] ampersand = "&amp;".ToCharArray();
        static readonly char[] lessthan = "&lt;".ToCharArray();
        static readonly char[] greaterthan = "&gt;".ToCharArray();

        [CLSCompliant(false)]
        public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (!Encodes(unicodeScalar)) { return unicodeScalar.TryWriteScalarAsChar(buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '\"') { return quote.TryCopyCharacters(buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '&') { return ampersand.TryCopyCharacters(buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '<') { return lessthan.TryCopyCharacters(buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '>') { return greaterthan.TryCopyCharacters(buffer, bufferLength, out numberOfCharactersWritten); }
            else { return TryWriteEncodedScalarAsNumericEntity(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten); }
        }

        private unsafe static bool TryWriteEncodedScalarAsNumericEntity(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            Debug.Assert(buffer != null && bufferLength >= 0);

            // We're building the characters up in reverse
            char* chars = stackalloc char[8 /* "FFFFFFFF" */];
            int numberOfHexCharacters = 0;
            do
            {
                Debug.Assert(numberOfHexCharacters < 8, "Couldn't have written 8 characters out by this point.");
                // Pop off the last nibble
                chars[numberOfHexCharacters++] = HexUtil.Int32LsbToHexDigit(unicodeScalar & 0xF);
                unicodeScalar >>= 4;
            } while (unicodeScalar != 0);

            numberOfCharactersWritten = numberOfHexCharacters + 4; // four chars are &, #, x, and ;
            Debug.Assert(numberOfHexCharacters > 0, "At least one character should've been written.");

            if (numberOfHexCharacters + 4 > bufferLength)
            {
                numberOfCharactersWritten = 0;
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
                *buffer = chars[--numberOfHexCharacters];
                buffer++;
            }
            while (numberOfHexCharacters != 0);

            *buffer = ';';
            return true;
        }
    }
}
