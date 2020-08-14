// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    internal static class JavaScriptEncoderHelper
    {
        public static unsafe bool TryWriteEncodedScalarAsNumericEntity(int unicodeScalar, char* buffer, int length, out int numberOfCharactersWritten)
        {
            Debug.Assert(buffer != null && length >= 0);

            if (UnicodeHelpers.IsSupplementaryCodePoint(unicodeScalar))
            {
                // Convert this back to UTF-16 and write out both characters.
                UnicodeHelpers.GetUtf16SurrogatePairFromAstralScalarValue(unicodeScalar, out char leadingSurrogate, out char trailingSurrogate);
                if (TryWriteEncodedSingleCharacter(leadingSurrogate, buffer, length, out int leadingSurrogateCharactersWritten) &&
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
        private static unsafe bool TryWriteEncodedSingleCharacter(int unicodeScalar, char* buffer, int length, out int numberOfCharactersWritten)
        {
            Debug.Assert(buffer != null && length >= 0);
            Debug.Assert(!UnicodeHelpers.IsSupplementaryCodePoint(unicodeScalar), "The incoming value should've been in the BMP.");

            if (length < 6)
            {
                numberOfCharactersWritten = 0;
                return false;
            }

            // Encode this as 6 chars "\uFFFF".
            *buffer = '\\';
            buffer++;
            *buffer = 'u';
            buffer++;
            *buffer = HexConverter.ToCharUpper(unicodeScalar >> 12);
            buffer++;
            *buffer = HexConverter.ToCharUpper(unicodeScalar >> 8);
            buffer++;
            *buffer = HexConverter.ToCharUpper(unicodeScalar >> 4);
            buffer++;
            *buffer = HexConverter.ToCharUpper(unicodeScalar);

            numberOfCharactersWritten = 6;
            return true;
        }
    }
}
