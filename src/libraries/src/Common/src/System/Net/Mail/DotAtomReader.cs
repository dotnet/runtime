// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Mime;

namespace System.Net.Mail
{
    //
    // RFC 2822 Section 3.2.4 - Atom, Dot-Atom
    //
    // A Dot-Atom is a string of ASCII characters separated by dots.  Dots would normally not be allowed at the start 
    // or end, but we do allow dots at the end for compatibility with other mail clients.  We also allow 
    // multiple consecutive dots, which would normally be invalid.
    //
    internal static class DotAtomReader
    {
        // Reads a Dot Atom in reverse.
        //
        // Preconditions:  
        //  - Index must be within the bounds of the data string.
        //
        // Return value: 
        // - The first index of a character not valid in a dot-atom.  It is then up to the caller to 
        //   determine if the next character is a valid delimiter.
        //   e.g. "user.name@domain.com", starting at index 19 (m) returns 9 (@).
        //   e.g. "user.name@dom in.com", starting at index 19 (m) returns 13 (space).
        // - -1 if the dot-atom section terminated at the start of the data string.
        //   e.g. "user.name@domain.com", starting at index 8 (e) returns -1.
        //
        // A FormatException will be thrown if:
        // - The initial character at data[index] is invalid in a dot-atom.
        //   e.g. "a@b.com", starting at index 1 (@).
        // - The leading character is a dot.
        //   e.g. "a@.b.com", starting at index 7 (m), throws because the leading char (index 2) is a dot.
        //
        internal static int ReadReverse(string data, int index)
        {
            Debug.Assert(0 <= index && index < data.Length, "index was outside the bounds of the string: " + index);

            int startIndex = index;
            // Scan for the first invalid chars (including whitespace)
            for (; 0 <= index; index--)
            {
                if (data[index] <= MailBnfHelper.Ascii7bitMaxValue // Any Unicode allowed
                 && (data[index] != MailBnfHelper.Dot && !MailBnfHelper.Atext[data[index]])) // Invalid char
                {
                    break;
                }
            }

            // Check for empty/invalid dot-atom
            if (startIndex == index)
            {
                throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[index]));
            }
            // Check for leading dot
            else if (data[index + 1] == MailBnfHelper.Dot)
            {
                throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, MailBnfHelper.Dot));
            }
            return index;
        }
    }
}
