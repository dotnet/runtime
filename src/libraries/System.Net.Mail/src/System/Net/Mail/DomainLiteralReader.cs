// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Mime;
using System.Text;

namespace System.Net.Mail
{
    //
    // RFC 2822 Section 3.4.1 - Addr-Spec, Domain-Literals
    // A domain literal is a domain identifier that does not conform to the dot-atom format (Section 3.2.4) and must be
    // enclosed in brackets '[' ']'.  Domain literals may contain quoted-pairs.
    //
    internal static class DomainLiteralReader
    {
        //
        // Reads a domain literal in reverse
        //
        // Preconditions:
        //  - Index must be within the bounds of the data string.
        //  - The char at the given index is the initial bracket. (data[index] == EndSquareBracket)
        //
        // Return value:
        // - The next index past the terminating bracket (data[index + 1] == StartSquareBracket).
        //   e.g. In (user@[domain]), starting at index=12 (]) returns index=4 (@).
        //
        // A FormatException will be thrown or false is returned if:
        // - A non-escaped character is encountered that is not valid in a domain literal, including Unicode.
        // - The final bracket is not found.
        //
        internal static bool TryReadReverse(string data, int index, out int outIndex, bool throwExceptionIfFail)
        {
            Debug.Assert(0 <= index && index < data.Length, $"index was outside the bounds of the string: {index}");
            Debug.Assert(data[index] == MailBnfHelper.EndSquareBracket, "data did not end with a square bracket");

            // Skip the end bracket
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
                if (!QuotedPairReader.TryCountQuotedChars(data, index, false, out int quotedCharCount, throwExceptionIfFail))
                {
                    outIndex = default;
                    return false;
                }

                if (quotedCharCount > 0)
                {
                    // Skip quoted pairs
                    index -= quotedCharCount;
                }
                // Check for the terminating bracket
                else if (data[index] == MailBnfHelper.StartSquareBracket)
                {
                    // We're done parsing
                    outIndex = index - 1;
                    return true;
                }
                // Check for invalid characters
                else if (!Ascii.IsValid(data[index]) || !MailBnfHelper.Dtext[data[index]])
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
                // We didn't find a matching '[', throw.
                throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter,
                    MailBnfHelper.EndSquareBracket));
            }
            else
            {
                outIndex = default;
                return false;
            }
        }
    }
}
