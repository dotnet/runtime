// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Internal;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// Represents a type used to do HTML encoding.
    /// </summary>
    public abstract class HtmlEncoder : TextEncoder
    {
        /// <summary>
        /// Returns a default built-in instance of <see cref="HtmlEncoder"/>.
        /// </summary>
        public static HtmlEncoder Default
        {
            get { return DefaultHtmlEncoder.Singleton; }
        }

        /// <summary>
        /// Creates a new instance of HtmlEncoder with provided settings.
        /// </summary>
        /// <param name="settings">Settings used to control how the created <see cref="HtmlEncoder"/> encodes, primarily which characters to encode.</param>
        /// <returns>A new instance of the <see cref="HtmlEncoder"/>.</returns>
        public static HtmlEncoder Create(TextEncoderSettings settings)
        {
            return new DefaultHtmlEncoder(settings);
        }

        /// <summary>
        /// Creates a new instance of HtmlEncoder specifying character to be encoded.
        /// </summary>
        /// <param name="allowedRanges">Set of characters that the encoder is allowed to not encode.</param>
        /// <returns>A new instance of the <see cref="HtmlEncoder"/></returns>
        /// <remarks>Some characters in <paramref name="allowedRanges"/> might still get encoded, i.e. this parameter is just telling the encoder what ranges it is allowed to not encode, not what characters it must not encode.</remarks>
        public static HtmlEncoder Create(params UnicodeRange[] allowedRanges)
        {
            return new DefaultHtmlEncoder(allowedRanges);
        }
    }

    internal sealed class DefaultHtmlEncoder : HtmlEncoder
    {
        private readonly AllowedCharactersBitmap _allowedCharacters;
        internal static readonly DefaultHtmlEncoder Singleton = new DefaultHtmlEncoder(new TextEncoderSettings(UnicodeRanges.BasicLatin));

        public DefaultHtmlEncoder(TextEncoderSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _allowedCharacters = settings.GetAllowedCharacters();

            // Forbid codepoints which aren't mapped to characters or which are otherwise always disallowed
            // (includes categories Cc, Cs, Co, Cn, Zs [except U+0020 SPACE], Zl, Zp)
            _allowedCharacters.ForbidUndefinedCharacters();

            HtmlEncoderHelper.ForbidHtmlCharacters(_allowedCharacters);
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
            get { return 10; } // "&#x10FFFF;" is the longest encoded form
        }

        private const string s_quote = "&quot;";
        private const string s_ampersand = "&amp;";
        private const string s_lessthan = "&lt;";
        private const string s_greaterthan = "&gt;";

        public unsafe override bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            Span<char> destination = new Span<char>(buffer, bufferLength);
            if (!WillEncode(unicodeScalar)) { return TryWriteScalarAsChar(unicodeScalar, destination, out numberOfCharactersWritten); }
            else if (unicodeScalar == '\"') { return TryCopyCharacters(s_quote, destination, out numberOfCharactersWritten); }
            else if (unicodeScalar == '&') { return TryCopyCharacters(s_ampersand, destination, out numberOfCharactersWritten); }
            else if (unicodeScalar == '<') { return TryCopyCharacters(s_lessthan, destination, out numberOfCharactersWritten); }
            else if (unicodeScalar == '>') { return TryCopyCharacters(s_greaterthan, destination, out numberOfCharactersWritten); }
            else { return TryWriteEncodedScalarAsNumericEntity(unicodeScalar, buffer, bufferLength, out numberOfCharactersWritten); }
        }

        private static unsafe bool TryWriteEncodedScalarAsNumericEntity(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
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
                *buffer = HexConverter.ToCharUpper(unicodeScalar);
                unicodeScalar >>= nibbleSize;
                buffer--;
            }
            while (unicodeScalar != 0);

            buffer += numberOfHexCharacters + 1;
            *buffer = ';';
            return true;
        }
    }

    /// <summary>
    /// Separates static methods from HtmlEncoder and DefaultHtmlEncoder so those classes can be trimmed
    /// when only these static methods are needed.
    /// </summary>
    internal static class HtmlEncoderHelper
    {
        internal static void ForbidHtmlCharacters(AllowedCharactersBitmap allowedCharacters)
        {
            allowedCharacters.ForbidCharacter('<');
            allowedCharacters.ForbidCharacter('>');
            allowedCharacters.ForbidCharacter('&');
            allowedCharacters.ForbidCharacter('\''); // can be used to escape attributes
            allowedCharacters.ForbidCharacter('\"'); // can be used to escape attributes
            allowedCharacters.ForbidCharacter('+'); // technically not HTML-specific, but can be used to perform UTF7-based attacks
        }
    }
}
