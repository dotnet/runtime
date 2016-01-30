// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Text;


namespace System.Runtime.Serialization.Json
{
    internal class JavaScriptString
    {
        private string _s;
        private int _index;

        internal JavaScriptString(string s)
        {
            _s = s;
        }

        internal Nullable<char> GetNextNonEmptyChar()
        {
            while (_s.Length > _index)
            {
                char c = _s[_index++];
                if (!Char.IsWhiteSpace(c))
                {
                    return c;
                }
            }

            return null;
        }

        internal Nullable<char> MoveNext()
        {
            if (_s.Length > _index)
            {
                return _s[_index++];
            }

            return null;
        }

        internal string MoveNext(int count)
        {
            if (_s.Length >= _index + count)
            {
                string result = _s.Substring(_index, count);
                _index += count;

                return result;
            }

            return null;
        }

        internal void MovePrev()
        {
            if (_index > 0)
            {
                _index--;
            }
        }

        internal void MovePrev(int count)
        {
            while (_index > 0 && count > 0)
            {
                _index--;
                count--;
            }
        }

        private static void AppendCharAsUnicode(StringBuilder builder, char c)
        {
            builder.Append("\\u");
            builder.AppendFormat(CultureInfo.InvariantCulture, "{0:x4}", (int)c);
        }

        private static bool ShouldAppendAsUnicode(char c)
        {
            // Note on newline characters: Newline characters in JSON strings need to be encoded on the way out
            // See Unicode 6.2, Table 5-1 (http://www.unicode.org/versions/Unicode6.2.0/ch05.pdf]) for the full list.

            // We only care about NEL, LS, and PS, since the other newline characters are all
            // control characters so are already encoded.

            return c < ' ' ||
                   c >= (char)0xfffe || // max char
                   (c >= (char)0xd800 && c <= (char)0xdfff) || // between high and low surrogate
                   (c == '\u0085' || c == '\u2028' || c == '\u2029'); // Unicode new line characters
        }

        internal static string QuoteString(string value)
        {
            StringBuilder b = null;

            if (String.IsNullOrEmpty(value))
            {
                return String.Empty;
            }

            int startIndex = 0;
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                if (c == '\"' || c == '\'' || c == '/' || c == '\\' || ShouldAppendAsUnicode(c))
                {
                    if (b == null)
                    {
                        b = new StringBuilder(value.Length + 5);
                    }

                    if (count > 0)
                    {
                        b.Append(value, startIndex, count);
                    }

                    startIndex = i + 1;
                    count = 0;
                }

                switch (c)
                {
                    case '\"':
                        b.Append("\\\"");
                        break;
                    case '\\':
                        b.Append("\\\\");
                        break;
                    case '/':
                        b.Append("\\/");
                        break;
                    case '\'':
                        b.Append("\'");
                        break;
                    default:
                        if (ShouldAppendAsUnicode(c))
                        {
                            AppendCharAsUnicode(b, c);
                        }
                        else
                        {
                            count++;
                        }
                        break;
                }
            }

            if (b == null)
            {
                return value;
            }

            if (count > 0)
            {
                b.Append(value, startIndex, count);
            }

            return b.ToString();
        }

        public override string ToString()
        {
            if (_s.Length > _index)
            {
                return _s.Substring(_index);
            }

            return String.Empty;
        }

        internal string GetDebugString(string message)
        {
            return message + " (" + _index + "): " + _s;
        }
    }
}
