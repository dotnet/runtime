// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Reflection
{
    //
    // A simple lexer for assembly display names.
    //
    internal ref struct AssemblyNameLexer
    {
        private string _input;
        private int _index;

        internal AssemblyNameLexer(string s)
        {
            _input = s;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private char GetNextChar()
        {
            char ch;
            if (_index < _input.Length)
            {
                ch = _input[_index++];
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

        //
        // Return the next token in assembly name. If the result is DisplayNameToken.String,
        // sets "tokenString" to the tokenized string.
        //
        internal Token GetNext(out string tokenString)
        {
            tokenString = string.Empty;
            char c;

            while (true)
            {
                c = GetNextChar();
                switch (c)
                {
                    case ',':
                        return Token.Comma;
                    case '=':
                        return Token.Equals;
                    case '\0':
                        return Token.End;
                }

                if (!IsWhiteSpace(c))
                {
                    break;
                }
            }

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
