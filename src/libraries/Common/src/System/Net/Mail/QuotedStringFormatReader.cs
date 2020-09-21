// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Mime;

namespace System.Net.Mail
{
    // RFC 2822 Section 3.2.5 - Quoted strings
    // When a string of characters does not conform to an atom string (Section 3.2.4), it must be enclosed in double
    // quotes.  This allows for whitespace, quoted/escaped characters, etc.  ("Say hello. \"hello!\" ")
    //
    // For robustness, we allow the bounding double quotes to be omitted when we have another clear delineator such as
    // a comma: (sales@contoso.com, Contoso Pharmaceuticals info@contoso.com), where the display name 'Contoso Pharmaceuticals' should have been quoted.
    //
    // Quoted strings are allowed as MailAddress components local-part and display-name.
    // e.g. "display name" <"user name"@domain>
    internal static class QuotedStringFormatReader
    {
        //
        // This method reads a standard quoted string. Departing from the RFC, Unicode is permitted for display-name.
        //
        // Preconditions:
        //  - Index must be within the bounds of the data string.
        //  - The char at the given index is the initial quote. (data[index] == Quote)
        //
        // Return value: The next index past the terminating-quote (data[index + 1] == Quote).
        //   e.g. In (bob "user name"@domain), starting at index=14 (") returns index=3 (space).
        //
        // A FormatException will be thrown or false is returned if:
        // - A non-escaped character is encountered that is not valid in a quoted string.
        // - A Unicode character is encountered and Unicode has not been allowed.
        // - The final double quote is not found.
        //
        internal static bool TryReadReverseQuoted(string data, int index, bool permitUnicode, out int outIndex, bool throwExceptionIfFail)
        {
            Debug.Assert(0 <= index && index < data.Length, "Index out of range: " + index + ", " + data.Length);
            // Check for the first bounding quote
            Debug.Assert(data[index] == MailBnfHelper.Quote, "Initial char at index " + index + " was not a quote.");

            // Skip the bounding quote
            index--;

            do
            {
                // Check for valid whitespace
                if (!WhitespaceReader.TryReadFwsReverse(data, index, out index, throwExceptionIfFail))
                {
                    outIndex = default;
                    return false;
                }

                if (index < 0)
                {
                    break;
                }

                // Check for escaped characters
                if (!QuotedPairReader.TryCountQuotedChars(data, index, permitUnicode, out int quotedCharCount, throwExceptionIfFail))
                {
                    outIndex = default;
                    return false;
                }

                if (quotedCharCount > 0)
                {
                    // Skip quoted pairs
                    index -= quotedCharCount;
                }
                // Check for the terminating quote
                else if (data[index] == MailBnfHelper.Quote)
                {
                    // Skip the final bounding quote
                    outIndex = index - 1;
                    return true;
                }
                // Check invalid characters
                else if (!IsValidQtext(permitUnicode, data[index]))
                {
                    if (throwExceptionIfFail)
                    {
                        throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[index]));
                    }
                    else
                    {
                        outIndex = default;
                        return false;
                    }
                }
                // Valid char
                else
                {
                    index--;
                }
            }
            while (index >= 0);

            if (throwExceptionIfFail)
            {
                // We started with a quote, but did not end with one
                throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, MailBnfHelper.Quote));
            }
            else
            {
                outIndex = default;
                return false;
            }
        }

        //
        // This method attempts reading quoted-string formatted data when the bounding quotes were omitted.
        // This is common for e-mail display-names.
        //
        // Precondition: The index must be within the bounds of the data string.
        //
        // Return value:
        // - The index of the special delimiter provided.
        //   e.g. In (abc@x.com, billy box bob@bob.com), starting at index=19 (x) returns index=9 (,).
        // - -1 if the terminating character was not found.
        //   e.g. In (my name username@domain), starting at index=5 (e) returns index=-1.
        //
        // A FormatException will be thrown or false is returned if:
        // - A non-escaped character is encountered that is not valid in a quoted string.  This includes double quotes.
        // - A Unicode character is encountered and Unicode has not been allowed.
        //
        internal static bool TryReadReverseUnQuoted(string data, int index, bool permitUnicode, bool expectCommaDelimiter, out int outIndex, bool throwExceptionIfFail)
        {
            Debug.Assert(0 <= index && index < data.Length, "Index out of range: " + index + ", " + data.Length);

            do
            {
                // Check for valid whitespace
                if (!WhitespaceReader.TryReadFwsReverse(data, index, out index, throwExceptionIfFail))
                {
                    outIndex = default;
                    return false;
                }

                if (index < 0)
                {
                    break;
                }
                // Check for escaped characters
                if (!QuotedPairReader.TryCountQuotedChars(data, index, permitUnicode, out int quotedCharCount, throwExceptionIfFail))
                {
                    outIndex = default;
                    return false;
                }

                if (quotedCharCount > 0)
                {
                    index -= quotedCharCount;
                }
                // Check for the terminating char
                else if (expectCommaDelimiter && data[index] == MailBnfHelper.Comma)
                {
                    break;
                }
                // Check invalid characters
                else if (!IsValidQtext(permitUnicode, data[index]))
                {
                    if (throwExceptionIfFail)
                    {
                        throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[index]));
                    }
                    else
                    {
                        outIndex = default;
                        return false;
                    }
                }
                // Valid char
                else
                {
                    index--;
                }
            }
            while (index >= 0);

            outIndex = index;
            return true;
        }

        // Checks for Unicode characters and characters not allowed in quoted-strings. A quoted string may contain
        // non-whitespace control characters as well as all remaining ASCII chars except backslash and double quote.
        private static bool IsValidQtext(bool allowUnicode, char ch)
        {
            if (ch > MailBnfHelper.Ascii7bitMaxValue)
            {
                return allowUnicode;
            }
            else
            {
                return MailBnfHelper.Qtext[ch];
            }
        }
    }
}
