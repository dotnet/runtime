// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Mime;

namespace System.Net.Mail
{
    // FWS, CFWS, and Comments are defined in RFC 2822 section 3.2.3.
    //
    // FWS is a Folding White Space, or a series of spaces and tabs that may also contain a CRLF.
    //
    // CFWS is a FWS that also allows for nested comments enclosed in parenthesis.  (comment (nested))
    //
    // Valid values for these are declared in MailBnfHelper.cs and are explained in MailBNF.cs.
    //
    internal static class WhitespaceReader
    {
        //
        // This skips all folding and whitespace characters
        //
        // Preconditions: 
        // - The data string must not be null or empty. 
        // - The index must be within the upper bounds of the data string.
        //
        // Return value: 
        // - The index of the next character that is NOT a whitespace character.
        // - -1 if the beginning of the data string is reached.
        //
        // A FormatException will be thrown if a CR or LF is found NOT in the sequence CRLF.
        internal static int ReadFwsReverse(string data, int index)
        {
            Debug.Assert(!string.IsNullOrEmpty(data), "data was null or empty");
            Debug.Assert(index < data.Length, "index was outside the bounds of the string");
            bool expectCR = false;

            for (; index >= 0; index--)
            {
                // Check for a valid CRLF pair
                if (data[index] == MailBnfHelper.CR && expectCR)
                {
                    expectCR = false; // valid pair
                }
                // LF without CR, or CR without LF, invalid
                else if (data[index] == MailBnfHelper.CR || expectCR)
                {
                    throw new FormatException(SR.MailAddressInvalidFormat);
                }
                // LF is only valid if preceded by a CR.
                // Skip both if they're found together.
                else if (data[index] == MailBnfHelper.LF)
                {
                    expectCR = true;
                }
                // Skip whitespace
                else if (data[index] == MailBnfHelper.Space || data[index] == MailBnfHelper.Tab)
                {
                    // No-op
                }
                else
                {
                    // Neither check hit so we must be on something that is not whitespace
                    break;
                }
            }

            if (expectCR)
            {
                // We found a LF without an immediately preceding CR, invalid.
                throw new FormatException(SR.MailAddressInvalidFormat);
            }
            return index;
        }

        // This method functions similarly to ReadFwsReverse but will also skip any comments.  
        //
        // Comments are text within '(' and ')' and may be nested. There may also be consecutive comments.  Unicode is 
        // allowed, as the comments are not transmitted.
        // 
        // This method was explicitly written in a non-recursive fashion to avoid malicious or accidental 
        // stack-overflows from user input.
        // 
        // Preconditions: 
        // - The data string must not be null or empty
        // - The index must be within the upper bounds of the data string.
        // 
        // Return value: 
        // - The given index if it data[index] was not a ')' or whitespace
        // - The index of the next non comment or whitespace character
        //   e.g. " d ( ( c o mment) )" returns index 1
        // - -1 if skipping the comments and/or whitespace moves you to the beginning of the data string.
        //   e.g. " (comment) " returns -1
        //
        // Throws a FormatException for mismatched '(' and ')', or for unescaped characters not allowed in comments.
        internal static int ReadCfwsReverse(string data, int index)
        {
            Debug.Assert(!string.IsNullOrEmpty(data), "data was null or empty");
            Debug.Assert(index < data.Length, "index was outside the bounds of the string");

            int commentDepth = 0;

            // Check for valid whitespace
            index = ReadFwsReverse(data, index);

            while (index >= 0)
            {
                // Check for escaped characters.  They must be within comments.
                int quotedCharCount = QuotedPairReader.CountQuotedChars(data, index, true);
                if (commentDepth > 0 && quotedCharCount > 0)
                {
                    index = index - quotedCharCount;
                }
                // Start a new comment
                else if (data[index] == MailBnfHelper.EndComment)
                {
                    commentDepth++;
                    index--;
                }
                // Finish a comment
                else if (data[index] == MailBnfHelper.StartComment)
                {
                    commentDepth--;
                    if (commentDepth < 0)
                    {
                        // Mismatched '('
                        throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter,
                            MailBnfHelper.StartComment));
                    }
                    index--;
                }
                // Check for valid characters within comments.  Allow Unicode, as we won't transmit any comments.
                else if (commentDepth > 0
                    && (data[index] > MailBnfHelper.Ascii7bitMaxValue || MailBnfHelper.Ctext[data[index]]))
                {
                    index--;
                }
                // If we're still in a comment, this must be an invalid char
                else if (commentDepth > 0)
                {
                    throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[index]));
                }
                // We must no longer be in a comment, and this is not a whitespace char, return
                else
                {
                    break;
                }

                // Check for valid whitespace
                index = ReadFwsReverse(data, index);
            }

            if (commentDepth > 0)
            {
                // Mismatched ')', throw
                throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, MailBnfHelper.EndComment));
            }

            return index;
        }
    }
}
