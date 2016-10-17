// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Mime;
using System.Collections.Generic;

namespace System.Net.Mail
{
    //
    // This class is responsible for parsing E-mail addresses as described in RFC 2822.
    //
    // Ideally, addresses are formatted as ("Display name" <username@domain>), but we still try to read several
    // other formats, including common invalid formats like (Display name username@domain).
    // 
    // To make the detection of invalid address formats simpler, all address parsing is done in reverse order, 
    // including lists.  This way we know that the domain must be first, then the local-part, and then whatever 
    // remains must be the display-name.
    //
    internal static class MailAddressParser
    {
        // Parse a single MailAddress from the given string.
        // 
        // Throws a FormatException if any part of the MailAddress is invalid.
        internal static MailAddress ParseAddress(string data)
        {
            int index = data.Length - 1;
            MailAddress parsedAddress = MailAddressParser.ParseAddress(data, false, ref index);
            Debug.Assert(index == -1, "The index indicates that part of the address was not parsed: " + index);
            return parsedAddress;
        }

        // Parse a comma separated list of MailAddress's
        //
        // Throws a FormatException if any MailAddress is invalid.
        internal static List<MailAddress> ParseMultipleAddresses(string data)
        {
            List<MailAddress> results = new List<MailAddress>();
            int index = data.Length - 1;
            while (index >= 0)
            {
                // Because we're parsing in reverse, we must make an effort to preserve the order of the addresses.
                results.Insert(0, MailAddressParser.ParseAddress(data, true, ref index));
                Debug.Assert(index == -1 || data[index] == MailBnfHelper.Comma,
                    "separator not found while parsing multiple addresses");
                index--;
            }
            return results;
        }

        //
        // Parse a single MailAddress, potentially from a list.
        //
        // Preconditions: 
        //  - Index must be within the bounds of the data string.
        //  - The data string must not be null or empty
        //
        // Postconditions:
        // - Returns a valid MailAddress object parsed from the string
        // - For a single MailAddress index is set to -1
        // - For a list data[index] is the comma separator or -1 if the end of the data string was reached.
        //
        // Throws a FormatException if any part of the MailAddress is invalid.
        private static MailAddress ParseAddress(string data, bool expectMultipleAddresses, ref int index)
        {
            Debug.Assert(!string.IsNullOrEmpty(data));
            Debug.Assert(index >= 0 && index < data.Length, "Index out of range: " + index + ", " + data.Length);

            // Parsed components to be assembled as a MailAddress later
            string domain = null;
            string localPart = null;
            string displayName = null;

            // Skip comments and whitespace
            index = ReadCfwsAndThrowIfIncomplete(data, index);

            // Do we expect angle brackets around the address?
            // e.g. ("display name" <user@domain>)
            bool expectAngleBracket = false;
            if (data[index] == MailBnfHelper.EndAngleBracket)
            {
                expectAngleBracket = true;
                index--;
            }

            domain = ParseDomain(data, ref index);

            // The next character after the domain must be the '@' symbol
            if (data[index] != MailBnfHelper.At)
            {
                throw new FormatException(SR.MailAddressInvalidFormat);
            }

            // Skip the '@' symbol
            index--;

            localPart = ParseLocalPart(data, ref index, expectAngleBracket, expectMultipleAddresses);

            // Check for a matching angle bracket around the address
            if (expectAngleBracket)
            {
                if (index >= 0 && data[index] == MailBnfHelper.StartAngleBracket)
                {
                    index--; // Skip the angle bracket
                    // Skip white spaces, but leave comments, as they may be part of the display name.
                    index = WhitespaceReader.ReadFwsReverse(data, index);
                }
                else
                {
                    // Mismatched angle brackets, throw
                    throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter,
                        (index >= 0 ? data[index] : MailBnfHelper.EndAngleBracket)));
                }
            }

            // Is there anything left to parse?
            // There could still be a display name or another address
            if (index >= 0 && !(expectMultipleAddresses && data[index] == MailBnfHelper.Comma))
            {
                displayName = ParseDisplayName(data, ref index, expectMultipleAddresses);
            }
            else
            {
                displayName = string.Empty;
            }

            return new MailAddress(displayName, localPart, domain);
        }

        // Read through a section of CFWS.  If we reach the end of the data string then throw because not enough of the
        // MailAddress components were found.
        private static int ReadCfwsAndThrowIfIncomplete(string data, int index)
        {
            index = WhitespaceReader.ReadCfwsReverse(data, index);
            if (index < 0)
            {
                // More components were expected.  Incomplete address, invalid
                throw new FormatException(SR.MailAddressInvalidFormat);
            }
            return index;
        }

        // Parses the domain section of an address.  The domain may be in dot-atom format or surrounded by square 
        // brackets in domain-literal format.
        // e.g. <user@domain.com> or <user@[whatever I want]>
        //
        // Preconditions:
        // - data[index] is just inside of the angle brackets (if any).
        //
        // Postconditions:
        // - data[index] should refer to the '@' symbol
        // - returns the parsed domain, including any square brackets for domain-literals
        //
        // Throws a FormatException:
        // - For invalid un-escaped chars, including Unicode
        // - If the start of the data string is reached
        private static string ParseDomain(string data, ref int index)
        {
            // Skip comments and whitespace
            index = ReadCfwsAndThrowIfIncomplete(data, index);

            // Mark one end of the domain component
            int startingIndex = index;

            // Is the domain component in domain-literal format or dot-atom format?
            if (data[index] == MailBnfHelper.EndSquareBracket)
            {
                index = DomainLiteralReader.ReadReverse(data, index);
            }
            else
            {
                index = DotAtomReader.ReadReverse(data, index);
            }

            string domain = data.Substring(index + 1, startingIndex - index);

            // Skip comments and whitespace
            index = ReadCfwsAndThrowIfIncomplete(data, index);

            return NormalizeOrThrow(domain);
        }

        // Parses the local-part section of an address.  The local-part may be in dot-atom format or 
        // quoted-string format. e.g. <user.name@domain> or <"user name"@domain>
        // We do not support the obsolete formats of user."name"@domain, "user".name@domain, or "user"."name"@domain.
        //
        // Preconditions:
        // - data[index + 1] is the '@' symbol
        //
        // Postconditions:
        // - data[index] should refer to the '<', if any, otherwise the next non-CFWS char.
        // - index == -1 if the beginning of the data string has been reached.
        // - returns the parsed local-part, including any bounding quotes around quoted-strings
        //
        // Throws a FormatException:
        // - For invalid un-escaped chars, including Unicode
        // - If the final value of data[index] is not a valid character to precede the local-part 
        private static string ParseLocalPart(string data, ref int index, bool expectAngleBracket,
            bool expectMultipleAddresses)
        {
            // Skip comments and whitespace
            index = ReadCfwsAndThrowIfIncomplete(data, index);

            // Mark the start of the local-part
            int startingIndex = index;

            // Is the local-part component in quoted-string format or dot-atom format?
            if (data[index] == MailBnfHelper.Quote)
            {
                index = QuotedStringFormatReader.ReadReverseQuoted(data, index, true);
            }
            else
            {
                index = DotAtomReader.ReadReverse(data, index);

                // Check that the local-part is properly separated from the next component. It may be separated by a 
                // comment, white space, an expected angle bracket, a quote for the display-name, or an expected comma 
                // before the next address.
                if (index >= 0 &&
                        !(
                            MailBnfHelper.IsAllowedWhiteSpace(data[index]) // < local@domain >
                            || data[index] == MailBnfHelper.EndComment // <(comment)local@domain>
                            || (expectAngleBracket && data[index] == MailBnfHelper.StartAngleBracket) // <local@domain>
                            || (expectMultipleAddresses && data[index] == MailBnfHelper.Comma) // local@dom,local@dom
                                                                                               // Note: The following condition is more lax than the RFC.  This is done so we could support 
                                                                                               // a common invalid formats as shown below.
                            || data[index] == MailBnfHelper.Quote // "display"local@domain
                        )
                    )
                {
                    throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[index]));
                }
            }

            string localPart = data.Substring(index + 1, startingIndex - index);

            index = WhitespaceReader.ReadCfwsReverse(data, index);

            return NormalizeOrThrow(localPart);
        }

        // Parses the display-name section of an address.  In departure from the RFC, we attempt to read data in the 
        // quoted-string format even if the bounding quotes are omitted.  We also permit Unicode, which the RFC does
        // not allow for.
        // e.g. ("display name" <user@domain>) or (display name <user@domain>)
        //
        // Preconditions:
        //
        // Postconditions:
        // - data[index] should refer to the comma ',' separator, if any
        // - index == -1 if the beginning of the data string has been reached.
        // - returns the parsed display-name, excluding any bounding quotes around quoted-strings
        //
        // Throws a FormatException:
        // - For invalid un-escaped chars, except Unicode
        // - If the postconditions cannot be met.
        private static string ParseDisplayName(string data, ref int index, bool expectMultipleAddresses)
        {
            string displayName;

            // Whatever is left over must be the display name. The display name should be a single word/atom or a 
            // quoted string, but for robustness we allow the quotes to be omitted, so long as we can find the comma 
            // separator before the next address.

            // Read the comment (if any).  If the display name is contained in quotes, the surrounding comments are 
            // omitted. Otherwise, mark this end of the comment so we can include it as part of the display name.
            int firstNonCommentIndex = WhitespaceReader.ReadCfwsReverse(data, index);

            // Check to see if there's a quoted-string display name
            if (firstNonCommentIndex >= 0 && data[firstNonCommentIndex] == MailBnfHelper.Quote)
            {
                // The preceding comment was not part of the display name.  Read just the quoted string.
                index = QuotedStringFormatReader.ReadReverseQuoted(data, firstNonCommentIndex, true);

                Debug.Assert(data[index + 1] == MailBnfHelper.Quote, "Mis-aligned index: " + index);

                // Do not include the bounding quotes on the display name
                int leftIndex = index + 2;
                displayName = data.Substring(leftIndex, firstNonCommentIndex - leftIndex);

                // Skip any CFWS after the display name
                index = WhitespaceReader.ReadCfwsReverse(data, index);

                // Check for completion. We are valid if we hit the end of the data string or if the rest of the data 
                // belongs to another address.
                if (index >= 0 && !(expectMultipleAddresses && data[index] == MailBnfHelper.Comma))
                {
                    // If there was still data, only a comma could have been the next valid character
                    throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[index]));
                }
            }
            else
            {
                // The comment (if any) should be part of the display name.
                int startingIndex = index;

                // Read until the dividing comma or the end of the line.
                index = QuotedStringFormatReader.ReadReverseUnQuoted(data, index, true, expectMultipleAddresses);

                Debug.Assert(index < 0 || data[index] == MailBnfHelper.Comma, "Mis-aligned index: " + index);

                // Do not include the Comma (if any), and because there were no bounding quotes, 
                // trim extra whitespace.
                displayName = data.SubstringTrim(index + 1, startingIndex - index);
            }
            return NormalizeOrThrow(displayName);
        }

        internal static string NormalizeOrThrow(string input)
        {
            try
            {
                return input.Normalize(Text.NormalizationForm.FormC);
            }
            catch (ArgumentException e)
            {
                throw new FormatException(SR.MailAddressInvalidFormat, e);
            }
        }
    }
}
