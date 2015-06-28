// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
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
        public static HtmlEncoder Create(TextEncoderSettings settings)
        {
            return new DefaultHtmlEncoder(settings);
        }
        public static HtmlEncoder Create(params UnicodeRange[] allowedRanges)
        {
            return new DefaultHtmlEncoder(allowedRanges);
        }
    }

    internal sealed class DefaultHtmlEncoder : HtmlEncoder
    {
        private AllowedCharactersBitmap _allowedCharacters;
        internal readonly static DefaultHtmlEncoder Singleton = new DefaultHtmlEncoder(new TextEncoderSettings(UnicodeRanges.BasicLatin));

        public DefaultHtmlEncoder(TextEncoderSettings filter)
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

        public DefaultHtmlEncoder(params UnicodeRange[] allowedRanges) : this(new TextEncoderSettings(allowedRanges))
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
            return _allowedCharacters.FindFirstCharacterToEncode(text, textLength);
        }

        public override int MaxOutputCharactersPerInputCharacter
        {
            get { return 8; } // "&#xFFFF;" is the longest encoded form 
        }

        static readonly char[] s_quote = "&quot;".ToCharArray();
        static readonly char[] s_ampersand = "&amp;".ToCharArray();
        static readonly char[] s_lessthan = "&lt;".ToCharArray();
        static readonly char[] s_greaterthan = "&gt;".ToCharArray();

        public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (!WillEncode(unicodeScalar)) { return TryWriteScalarAsChar(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '\"') { return TryCopyCharacters(s_quote, buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '&') { return TryCopyCharacters(s_ampersand, buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '<') { return TryCopyCharacters(s_lessthan, buffer, bufferLength, out numberOfCharactersWritten); }
            else if (unicodeScalar == '>') { return TryCopyCharacters(s_greaterthan, buffer, bufferLength, out numberOfCharactersWritten); }
            else { return TryWriteEncodedScalarAsNumericEntity(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten); }
        }

        private unsafe static bool TryWriteEncodedScalarAsNumericEntity(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            Debug.Assert(buffer != null && bufferLength >= 0);

            // We're writing the characters in reverse, first determine
            // how many there are
            const int nibbleSize = 4;
            int numberOfHexCharacters = 0;
            int compareUnicodeScalar = unicodeScalar;

            do
            {
                Debug.Assert(numberOfHexCharacters < 8, "Couldn't have written 8 characters out by this point.");
                numberOfHexCharacters++;
                compareUnicodeScalar >>= nibbleSize;
            } while (compareUnicodeScalar != 0);

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

            // Jump to the end of the hex position and write backwards
            buffer += numberOfHexCharacters;
            do
            {
                *buffer = HexUtil.Int32LsbToHexDigit(unicodeScalar & 0xF);
                unicodeScalar >>= nibbleSize;
                buffer--;
            }
            while (unicodeScalar != 0);

            buffer += numberOfHexCharacters + 1;
            *buffer = ';';
            return true;
        }
    }
}
