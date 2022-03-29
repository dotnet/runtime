// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Reflection
{
    //
    // A simple lexer for assembly display names.
    //
    internal struct AssemblyNameLexer
    {
        internal AssemblyNameLexer(string s)
        {
            // Convert string to char[] with NUL terminator. (An actual NUL terminator in the input string will be treated
            // as an actual end of string: this is compatible with desktop behavior.)
            char[] chars = new char[s.Length + 1];
            s.CopyTo(0, chars, 0, s.Length);
            _chars = chars;
            _index = 0;
        }

        //
        // Return the next token in assembly name. If you expect the result to be DisplayNameToken.String,
        // use GetNext(out String) instead.
        //
        internal Token GetNext()
        {
            return GetNext(out _);
        }

        //
        // Return the next token in assembly name. If the result is DisplayNameToken.String,
        // sets "tokenString" to the tokenized string.
        //
        internal Token GetNext(out string tokenString)
        {
            tokenString = null;
            while (char.IsWhiteSpace(_chars[_index]))
                _index++;

            char c = _chars[_index++];
            if (c == 0)
                return Token.End;
            if (c == ',')
                return Token.Comma;
            if (c == '=')
                return Token.Equals;

            StringBuilder sb = new StringBuilder();

            char quoteChar = (char)0;
            if (c == '\'' || c == '\"')
            {
                quoteChar = c;
                c = _chars[_index++];
            }

            for (;;)
            {
                if (c == 0)
                {
                    _index--;
                    break;  // Terminate: End of string (desktop compat: if string was quoted, permitted to terminate without end-quote.)
                }

                if (quoteChar != 0 && c == quoteChar)
                    break;  // Terminate: Found closing quote of quoted string.

                if (quoteChar == 0 && (c == ',' || c == '='))
                {
                    _index--;
                    break;  // Terminate: Found start of a new ',' or '=' token.
                }

                if (quoteChar == 0 && (c == '\'' || c == '\"'))
                    throw new FileLoadException();  // Desktop compat: Unescaped quote illegal unless entire string is quoted.

                if (c == '\\')
                {
                    c = _chars[_index++];

                    switch (c)
                    {
                        case '\\':
                        case ',':
                        case '=':
                        case '\'':
                        case '"':
                            sb.Append(c);
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        default:
                            throw new FileLoadException();  // Unrecognized escape
                    }
                }
                else
                {
                    sb.Append(c);
                }

                c = _chars[_index++];
            }

            tokenString = sb.ToString();
            if (quoteChar == 0)
                tokenString = tokenString.Trim(); // Unless quoted, whitespace at beginning or end doesn't count.
            return Token.String;
        }

        // Token categories for display name lexer.
        internal enum Token
        {
            Equals = 1,
            Comma = 2,
            String = 3,
            End = 4,
        }

        private readonly char[] _chars;
        private int _index;
    }
}
