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
    internal ref struct AssemblyNameLexer
    {
        private ReadOnlySpan<char> _chars;
        private int _index;

        internal AssemblyNameLexer(string s)
        {
            // Get a ReadOnlySpan<char> for the string including NUL terminator.(An actual NUL terminator in the input string will be treated
            // as an actual end of string: this is compatible with desktop behavior.)
            _chars = new ReadOnlySpan<char>(ref s.GetRawStringData(), s.Length);
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

        private static bool IsWhiteSpace(char ch)
        {
            switch (ch)
            {
                case '\n':
                case '\r':
                case ' ':
                case '\t':
                    return true;
                default:
                    return false;
            }
        }

        private char GetNextChar()
        {
            char ch;
            if (_index < _chars.Length)
            {
                ch = _chars[_index++];
                if (ch == '\0')
                {
                    throw new FileLoadException();
                }
            }
            else
            {
                ch = '\0';
            }

            return ch;
        }

        private char PeekNextChar()
        {
            return _index < _chars.Length ?
                _chars[_index] :
                '\0';
        }

        //
        // Return the next token in assembly name. If the result is DisplayNameToken.String,
        // sets "tokenString" to the tokenized string.
        //
        internal Token GetNext(out string tokenString)
        {
            tokenString = string.Empty;
            while (IsWhiteSpace(PeekNextChar()))
                _index++;

            char c = GetNextChar();
            if (c == 0)                 // TODO: VS Should add helper that checks for the string end, if not throw on 0
                return Token.End;
            if (c == ',')
                return Token.Comma;
            if (c == '=')
                return Token.Equals;

            ValueStringBuilder sb = new ValueStringBuilder(stackalloc char[64]);

            char quoteChar = '\0';
            if (c == '\'' || c == '\"')
            {
                quoteChar = c;
                c = GetNextChar();
            }

            for (; ; )
            {
                if (c == 0)
                {
                    if (quoteChar != 0)
                    {
                        // EOS and unclosed quotes is an error
                        throw new FileLoadException();
                    }
                    else
                    {
                        // Reached end of input and therefore of string
                        break;
                    }
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
                    c = GetNextChar();

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

                c = GetNextChar();
            }


            if (quoteChar == 0)
            {
                while (sb.Length > 0 && IsWhiteSpace(sb[sb.Length - 1]))
                    sb.Length--;
            }

            tokenString = sb.ToString();
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
    }
}
