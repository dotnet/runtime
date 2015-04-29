// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Net.Mail;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Mime
{
    internal static class MailBnfHelper
    {
        // characters allowed in atoms
        internal static bool[] Atext = new bool[128];

        // characters allowed in quoted strings (not including unicode)
        internal static bool[] Qtext = new bool[128];

        // characters allowed in domain literals
        internal static bool[] Dtext = new bool[128];

        // characters allowed in header names
        internal static bool[] Ftext = new bool[128];

        // characters allowed in tokens
        internal static bool[] Ttext = new bool[128];

        // characters allowed inside of comments
        internal static bool[] Ctext = new bool[128];

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
        internal static readonly IList<char> Whitespace;

        static MailBnfHelper()
        {
            // NOTE: See RFC 2822 for more detail.  By default, every value in the array is false and only
            // those values which are allowed in that particular set are then set to true.  The numbers
            // annotating each definition below are the range of ASCII values which are allowed in that definition.

            // all allowed whitespace characters
            Whitespace = new List<char>();
            Whitespace.Add(Tab);
            Whitespace.Add(Space);
            Whitespace.Add(CR);
            Whitespace.Add(LF);

            // atext = ALPHA / DIGIT / "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "/" / "=" / "?" / "^" / "_" / "`" / "{" / "|" / "}" / "~"
            for (int i = '0'; i <= '9'; i++) { Atext[i] = true; }
            for (int i = 'A'; i <= 'Z'; i++) { Atext[i] = true; }
            for (int i = 'a'; i <= 'z'; i++) { Atext[i] = true; }
            Atext['!'] = true;
            Atext['#'] = true;
            Atext['$'] = true;
            Atext['%'] = true;
            Atext['&'] = true;
            Atext['\''] = true;
            Atext['*'] = true;
            Atext['+'] = true;
            Atext['-'] = true;
            Atext['/'] = true;
            Atext['='] = true;
            Atext['?'] = true;
            Atext['^'] = true;
            Atext['_'] = true;
            Atext['`'] = true;
            Atext['{'] = true;
            Atext['|'] = true;
            Atext['}'] = true;
            Atext['~'] = true;

            // fqtext = %d1-9 / %d11 / %d12 / %d14-33 / %d35-91 / %d93-127
            for (int i = 1; i <= 9; i++) { Qtext[i] = true; }
            Qtext[11] = true;
            Qtext[12] = true;
            for (int i = 14; i <= 33; i++) { Qtext[i] = true; }
            for (int i = 35; i <= 91; i++) { Qtext[i] = true; }
            for (int i = 93; i <= 127; i++) { Qtext[i] = true; }

            // fdtext = %d1-8 / %d11 / %d12 / %d14-31 / %d33-90 / %d94-127
            for (int i = 1; i <= 8; i++) { Dtext[i] = true; }
            Dtext[11] = true;
            Dtext[12] = true;
            for (int i = 14; i <= 31; i++) { Dtext[i] = true; }
            for (int i = 33; i <= 90; i++) { Dtext[i] = true; }
            for (int i = 94; i <= 127; i++) { Dtext[i] = true; }

            // ftext = %d33-57 / %d59-126
            for (int i = 33; i <= 57; i++) { Ftext[i] = true; }
            for (int i = 59; i <= 126; i++) { Ftext[i] = true; }

            // ttext = %d33-126 except '()<>@,;:\"/[]?='
            for (int i = 33; i <= 126; i++) { Ttext[i] = true; }
            Ttext['('] = false;
            Ttext[')'] = false;
            Ttext['<'] = false;
            Ttext['>'] = false;
            Ttext['@'] = false;
            Ttext[','] = false;
            Ttext[';'] = false;
            Ttext[':'] = false;
            Ttext['\\'] = false;
            Ttext['"'] = false;
            Ttext['/'] = false;
            Ttext['['] = false;
            Ttext[']'] = false;
            Ttext['?'] = false;
            Ttext['='] = false;

            // ctext- %d1-8 / %d11 / %d12 / %d14-31 / %33-39 / %42-91 / %93-127
            for (int i = 1; i <= 8; i++) { Ctext[i] = true; }
            Ctext[11] = true;
            Ctext[12] = true;
            for (int i = 14; i <= 31; i++) { Ctext[i] = true; }
            for (int i = 33; i <= 39; i++) { Ctext[i] = true; }
            for (int i = 42; i <= 91; i++) { Ctext[i] = true; }
            for (int i = 93; i <= 127; i++) { Ctext[i] = true; }
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

        internal static string ReadQuotedString(string data, ref int offset, StringBuilder builder)
        {
            return ReadQuotedString(data, ref offset, builder, false, false);
        }

        internal static string ReadQuotedString(string data, ref int offset, StringBuilder builder, bool doesntRequireQuotes, bool permitUnicodeInDisplayName)
        {
            // assume first char is the opening quote
            if (!doesntRequireQuotes)
            {
                ++offset;
            }
            int start = offset;
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder());
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
                    //if data contains unicode and unicode is permitted, then 
                    //it is valid in a quoted string in a header.
                    if (data[offset] <= Ascii7bitMaxValue && !Qtext[data[offset]])
                        throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[offset]));
                }
                //not permitting unicode, in which case unicode is a formatting error
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

        internal static string ReadParameterAttribute(string data, ref int offset, StringBuilder builder)
        {
            if (!SkipCFWS(data, ref offset))
                return null; // 

            return ReadToken(data, ref offset, null);
        }

        internal static string ReadToken(string data, ref int offset, StringBuilder builder)
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

            if (start == offset)
            {
                throw new FormatException(SR.Format(SR.MailHeaderFieldInvalidCharacter, data[offset]));
            }

            return data.Substring(start, offset - start);
        }

        private static string[] s_months = new string[] { null, "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        internal static string GetDateTimeString(DateTime value, StringBuilder builder)
        {
            StringBuilder localBuilder = (builder != null ? builder : new StringBuilder());
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
                            offset++;
                            offset++;
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
