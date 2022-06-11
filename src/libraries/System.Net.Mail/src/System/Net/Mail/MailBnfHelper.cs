// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

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

        // characters allowed in header names
        internal static readonly bool[] Ftext = CreateCharactersAllowedInHeaderNames();

        // characters allowed in tokens
        internal static readonly bool[] Ttext = CreateCharactersAllowedInTokens();

        // characters allowed inside of comments
        internal static readonly bool[] Ctext = CreateCharactersAllowedInComments();

        internal const int Ascii7bitMaxValue = 127;
        internal const char Quote = '\"';
        internal const char Space = ' ';
        internal const char Tab = '\t';
        internal const char CR = '\r';
        internal const char LF = '\n';
        internal const char StartComment = '(';
        internal const char EndComment = ')';
        internal const char Backslash = '\\';
        internal const char At = '@';
        internal const char EndAngleBracket = '>';
        internal const char StartAngleBracket = '<';
        internal const char StartSquareBracket = '[';
        internal const char EndSquareBracket = ']';
        internal const char Comma = ',';
        internal const char Dot = '.';

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

        private static bool[] CreateCharactersAllowedInHeaderNames()
        {
            // ftext = %d33-57 / %d59-126
            var ftext = new bool[128];
            for (int i = 33; i <= 57; i++) { ftext[i] = true; }
            for (int i = 59; i <= 126; i++) { ftext[i] = true; }
            return ftext;
        }

        private static bool[] CreateCharactersAllowedInTokens()
        {
            // ttext = %d33-126 except '()<>@,;:\"/[]?='
            var ttext = new bool[128];
            for (int i = 33; i <= 126; i++) { ttext[i] = true; }
            ttext['('] = false;
            ttext[')'] = false;
            ttext['<'] = false;
            ttext['>'] = false;
            ttext['@'] = false;
            ttext[','] = false;
            ttext[';'] = false;
            ttext[':'] = false;
            ttext['\\'] = false;
            ttext['"'] = false;
            ttext['/'] = false;
            ttext['['] = false;
            ttext[']'] = false;
            ttext['?'] = false;
            ttext['='] = false;
            return ttext;
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

        internal static bool SkipCFWS(string data, ref int offset)
        {
            int comments = 0;
            for (; offset < data.Length; offset++)
            {
                if (data[offset] > 127)
                    throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[offset]));
                else if (data[offset] == '\\' && comments > 0)
                    offset += 2;
                else if (data[offset] == '(')
                    comments++;
                else if (data[offset] == ')')
                    comments--;
                else if (data[offset] != ' ' && data[offset] != '\t' && comments == 0)
                    return true;

                if (comments < 0)
                {
                    throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[offset]));
                }
            }

            //returns false if end of string
            return false;
        }

        internal static void ValidateHeaderName(string data)
        {
            int offset = 0;
            for (; offset < data.Length; offset++)
            {
                if (data[offset] > Ftext.Length || !Ftext[data[offset]])
                    throw new FormatException(SR.InvalidHeaderName);
            }
            if (offset == 0)
                throw new FormatException(SR.InvalidHeaderName);
        }

        internal static string? ReadQuotedString(string data, ref int offset, StringBuilder? builder)
        {
            return ReadQuotedString(data, ref offset, builder, false, false);
        }

        internal static string? ReadQuotedString(string data, ref int offset, StringBuilder? builder, bool doesntRequireQuotes, bool permitUnicodeInDisplayName)
        {
            // assume first char is the opening quote
            if (!doesntRequireQuotes)
            {
                ++offset;
            }
            int start = offset;
            StringBuilder localBuilder = builder ?? new StringBuilder();
            for (; offset < data.Length; offset++)
            {
                if (data[offset] == '\\')
                {
                    localBuilder.Append(data, start, offset - start);
                    start = ++offset;
                }
                else if (data[offset] == '"')
                {
                    localBuilder.Append(data, start, offset - start);
                    offset++;
                    return (builder != null ? null : localBuilder.ToString());
                }
                else if (data[offset] == '=' &&
                    data.Length > offset + 3 &&
                    data[offset + 1] == '\r' &&
                    data[offset + 2] == '\n' &&
                    (data[offset + 3] == ' ' || data[offset + 3] == '\t'))
                {
                    //it's a soft crlf so it's ok
                    offset += 3;
                }
                else if (permitUnicodeInDisplayName)
                {
                    //if data contains Unicode and Unicode is permitted, then
                    //it is valid in a quoted string in a header.
                    if (data[offset] <= Ascii7bitMaxValue && !Qtext[data[offset]])
                        throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[offset]));
                }
                //not permitting Unicode, in which case Unicode is a formatting error
                else if (data[offset] > Ascii7bitMaxValue || !Qtext[data[offset]])
                {
                    throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[offset]));
                }
            }
            if (doesntRequireQuotes)
            {
                localBuilder.Append(data, start, offset - start);
                return (builder != null ? null : localBuilder.ToString());
            }
            throw new FormatException(SR.MailHeaderFieldMalformedHeader);
        }

        internal static string? ReadParameterAttribute(string data, ref int offset, StringBuilder? builder)
        {
            if (!SkipCFWS(data, ref offset))
                return null; //

            return ReadToken(data, ref offset, null);
        }

        internal static string ReadToken(string data, ref int offset, StringBuilder? builder)
        {
            int start = offset;
            for (; offset < data.Length; offset++)
            {
                if (data[offset] > Ascii7bitMaxValue)
                {
                    throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[offset]));
                }
                else if (!Ttext[data[offset]])
                {
                    break;
                }
            }

            if (start == offset && offset < data.Length)
            {
                throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[offset]));
            }

            return data.Substring(start, offset - start);
        }

        private static readonly string?[] s_months = new string?[] { null, "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        internal static string? GetDateTimeString(DateTime value, StringBuilder? builder)
        {
            StringBuilder localBuilder = builder ?? new StringBuilder();
            localBuilder.Append(value.Day);
            localBuilder.Append(' ');
            localBuilder.Append(s_months[value.Month]);
            localBuilder.Append(' ');
            localBuilder.Append(value.Year);
            localBuilder.Append(' ');
            if (value.Hour <= 9)
            {
                localBuilder.Append('0');
            }
            localBuilder.Append(value.Hour);
            localBuilder.Append(':');
            if (value.Minute <= 9)
            {
                localBuilder.Append('0');
            }
            localBuilder.Append(value.Minute);
            localBuilder.Append(':');
            if (value.Second <= 9)
            {
                localBuilder.Append('0');
            }
            localBuilder.Append(value.Second);

            string offset = TimeZoneInfo.Local.GetUtcOffset(value).ToString();
            if (offset[0] != '-')
            {
                localBuilder.Append(" +");
            }
            else
            {
                localBuilder.Append(' ');
            }

            string[] offsetFields = offset.Split(':');
            localBuilder.Append(offsetFields[0]);
            localBuilder.Append(offsetFields[1]);
            return (builder != null ? null : localBuilder.ToString());
        }

        internal static void GetTokenOrQuotedString(string data, StringBuilder builder, bool allowUnicode)
        {
            int offset = 0, start = 0;
            for (; offset < data.Length; offset++)
            {
                if (CheckForUnicode(data[offset], allowUnicode))
                {
                    continue;
                }

                if (!Ttext[data[offset]] || data[offset] == ' ')
                {
                    builder.Append('"');
                    for (; offset < data.Length; offset++)
                    {
                        if (CheckForUnicode(data[offset], allowUnicode))
                        {
                            continue;
                        }
                        else if (IsFWSAt(data, offset)) // Allow FWS == "\r\n "
                        {
                            // No-op, skip these three chars
                            offset += 2;
                        }
                        else if (!Qtext[data[offset]])
                        {
                            builder.Append(data, start, offset - start);
                            builder.Append('\\');
                            start = offset;
                        }
                    }
                    builder.Append(data, start, offset - start);
                    builder.Append('"');
                    return;
                }
            }

            //always a quoted string if it was empty.
            if (data.Length == 0)
            {
                builder.Append("\"\"");
            }
            // Token, no quotes needed
            builder.Append(data);
        }

        private static bool CheckForUnicode(char ch, bool allowUnicode)
        {
            if (ch < Ascii7bitMaxValue)
            {
                return false;
            }

            if (!allowUnicode)
            {
                throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, ch));
            }
            return true;
        }

        internal static bool IsAllowedWhiteSpace(char c)
        {
            // all allowed whitespace characters
            return c == Tab || c == Space || c == CR || c == LF;
        }

        internal static bool HasCROrLF(string data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == '\r' || data[i] == '\n')
                {
                    return true;
                }
            }
            return false;
        }

        // Is there a FWS ("\r\n " or "\r\n\t") starting at the given index?
        internal static bool IsFWSAt(string data, int index)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(index < data.Length);

            return (data[index] == MailBnfHelper.CR
                    && index + 2 < data.Length
                    && data[index + 1] == MailBnfHelper.LF
                    && (data[index + 2] == MailBnfHelper.Space
                        || data[index + 2] == MailBnfHelper.Tab));
        }
    }
}
