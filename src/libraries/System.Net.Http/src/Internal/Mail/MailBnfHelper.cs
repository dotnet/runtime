// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Mime
{
    internal static class MailBnfHelper
    {
        // characters allowed in atoms
        internal static readonly bool[] Atext = CreateCharactersAllowedInAtoms();

        // characters allowed in quoted strings (not including Unicode)
        internal static readonly bool[] Qtext = CreateCharactersAllowedInQuotedStrings();

        // characters allowed in domain literals
        internal static readonly bool[] Dtext = CreateCharactersAllowedInDomainLiterals();

        // characters allowed inside of comments
        internal static readonly bool[] Ctext = CreateCharactersAllowedInComments();

        internal static readonly int Ascii7bitMaxValue = 127;
        internal static readonly char Quote = '\"';
        internal static readonly char Space = ' ';
        internal static readonly char Tab = '\t';
        internal static readonly char CR = '\r';
        internal static readonly char LF = '\n';
        internal static readonly char StartComment = '(';
        internal static readonly char EndComment = ')';
        internal static readonly char Backslash = '\\';
        internal static readonly char At = '@';
        internal static readonly char EndAngleBracket = '>';
        internal static readonly char StartAngleBracket = '<';
        internal static readonly char StartSquareBracket = '[';
        internal static readonly char EndSquareBracket = ']';
        internal static readonly char Comma = ',';
        internal static readonly char Dot = '.';

        // NOTE: See RFC 2822 for more detail.  By default, every value in the array is false and only
        // those values which are allowed in that particular set are then set to true.  The numbers
        // annotating each definition below are the range of ASCII values which are allowed in that definition.

        private static bool[] CreateCharactersAllowedInAtoms()
        {
            // atext = ALPHA / DIGIT / "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "/" / "=" / "?" / "^" / "_" / "`" / "{" / "|" / "}" / "~"
            var atext = new bool[128];
            for (int i = '0'; i <= '9'; i++) { atext[i] = true; }
            for (int i = 'A'; i <= 'Z'; i++) { atext[i] = true; }
            for (int i = 'a'; i <= 'z'; i++) { atext[i] = true; }
            atext['!'] = true;
            atext['#'] = true;
            atext['$'] = true;
            atext['%'] = true;
            atext['&'] = true;
            atext['\''] = true;
            atext['*'] = true;
            atext['+'] = true;
            atext['-'] = true;
            atext['/'] = true;
            atext['='] = true;
            atext['?'] = true;
            atext['^'] = true;
            atext['_'] = true;
            atext['`'] = true;
            atext['{'] = true;
            atext['|'] = true;
            atext['}'] = true;
            atext['~'] = true;
            return atext;
        }

        private static bool[] CreateCharactersAllowedInQuotedStrings()
        {
            // fqtext = %d1-9 / %d11 / %d12 / %d14-33 / %d35-91 / %d93-127
            var qtext = new bool[128];
            for (int i = 1; i <= 9; i++) { qtext[i] = true; }
            qtext[11] = true;
            qtext[12] = true;
            for (int i = 14; i <= 33; i++) { qtext[i] = true; }
            for (int i = 35; i <= 91; i++) { qtext[i] = true; }
            for (int i = 93; i <= 127; i++) { qtext[i] = true; }
            return qtext;
        }

        private static bool[] CreateCharactersAllowedInDomainLiterals()
        {
            // fdtext = %d1-8 / %d11 / %d12 / %d14-31 / %d33-90 / %d94-127
            var dtext = new bool[128];
            for (int i = 1; i <= 8; i++) { dtext[i] = true; }
            dtext[11] = true;
            dtext[12] = true;
            for (int i = 14; i <= 31; i++) { dtext[i] = true; }
            for (int i = 33; i <= 90; i++) { dtext[i] = true; }
            for (int i = 94; i <= 127; i++) { dtext[i] = true; }
            return dtext;
        }

        private static bool[] CreateCharactersAllowedInComments()
        {
            // ctext- %d1-8 / %d11 / %d12 / %d14-31 / %33-39 / %42-91 / %93-127
            var ctext = new bool[128];
            for (int i = 1; i <= 8; i++) { ctext[i] = true; }
            ctext[11] = true;
            ctext[12] = true;
            for (int i = 14; i <= 31; i++) { ctext[i] = true; }
            for (int i = 33; i <= 39; i++) { ctext[i] = true; }
            for (int i = 42; i <= 91; i++) { ctext[i] = true; }
            for (int i = 93; i <= 127; i++) { ctext[i] = true; }
            return ctext;
        }

        internal static bool IsAllowedWhiteSpace(char c)
        {
            // all allowed whitespace characters
            return c == Tab || c == Space || c == CR || c == LF;
        }
    }
}
