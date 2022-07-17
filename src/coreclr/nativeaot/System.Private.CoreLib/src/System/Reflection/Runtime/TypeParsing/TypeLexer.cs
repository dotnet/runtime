// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.Assemblies;

namespace System.Reflection.Runtime.TypeParsing
{
    //
    // String tokenizer for typenames passed to the GetType() api's.
    //
    internal sealed class TypeLexer
    {
        public TypeLexer(string s)
        {
            // Turn the string into a char array with a NUL terminator.
            char[] chars = new char[s.Length + 1];
            s.CopyTo(0, chars, 0, s.Length);
            _chars = chars;
            _index = 0;
        }

        public TokenType Peek
        {
            get
            {
                SkipWhiteSpace();
                char c = _chars[_index];
                return CharToToken(c);
            }
        }

        public TokenType PeekSecond
        {
            get
            {
                SkipWhiteSpace();
                int index = _index + 1;
                while (char.IsWhiteSpace(_chars[index]))
                    index++;
                char c = _chars[index];
                return CharToToken(c);
            }
        }


        public void Skip()
        {
            Debug.Assert(_index != _chars.Length);
            SkipWhiteSpace();
            _index++;
        }

        // Return the next token and skip index past it unless already at end of string
        // or the token is not a reserved token.
        public TokenType GetNextToken()
        {
            TokenType tokenType = Peek;
            if (tokenType == TokenType.End || tokenType == TokenType.Other)
                return tokenType;
            Skip();
            return tokenType;
        }

        //
        // Lex the next segment as part of a type name. (Do not use for assembly names.)
        //
        // Note that unescaped "."'s do NOT terminate the identifier, but unescaped "+"'s do.
        //
        // Terminated by the first non-escaped reserved character ('[', ']', '+', '&', '*' or ',')
        //
        public string GetNextIdentifier()
        {
            SkipWhiteSpace();

            int src = _index;
            char[] buffer = new char[_chars.Length];
            int dst = 0;
            for (;;)
            {
                char c = _chars[src];
                TokenType token = CharToToken(c);
                if (token != TokenType.Other)
                    break;
                src++;
                if (c == '\\')
                {
                    c = _chars[src];
                    if (c != NUL)
                        src++;
                    if (!c.NeedsEscapingInTypeName())
                    {
                        // If we got here, a backslash was used to escape a character that is not legal to escape inside a type name.
                        //
                        // Common sense would dictate throwing an ArgumentException but that's not what the desktop CLR does.
                        // The desktop CLR treats this case by returning FALSE from TypeName::TypeNameParser::GetIdentifier().
                        // Unfortunately, no one checks this return result. Instead, the CLR keeps parsing (unfortunately, the lexer
                        // was left in some strange state by the previous failure but typically, this goes unnoticed) and eventually, tries to resolve
                        // a Type whose name is the empty string. When it can't resolve that type, the CLR throws a TypeLoadException()
                        // complaining about be unable to find a type with the empty name.
                        //
                        // To emulate this accidental behavior, we'll throw a special exception that's caught by the TypeParser.
                        //
                        throw new IllegalEscapeSequenceException();
                    }
                }
                buffer[dst++] = c;
            }

            _index = src;
            return new string(buffer, 0, dst);
        }

        //
        // Lex the next segment as the assembly name at the end of an assembly-qualified type name. (Do not use for
        // assembly names embedded inside generic type arguments.)
        //
        // Terminated by NUL. There are no escape characters defined by the typename lexer (however, AssemblyName
        // does have its own escape rules.)
        //
        public RuntimeAssemblyName GetNextAssemblyName()
        {
            SkipWhiteSpace();

            int src = _index;
            char[] buffer = new char[_chars.Length];
            int dst = 0;
            for (;;)
            {
                char c = _chars[src];
                if (c == NUL)
                    break;
                src++;
                buffer[dst++] = c;
            }
            _index = src;
            string fullName = new string(buffer, 0, dst);
            return RuntimeAssemblyName.Parse(fullName);
        }

        //
        // Lex the next segment as an assembly name embedded inside a generic argument type.
        //
        // Terminated by an unescaped ']'.
        //
        public RuntimeAssemblyName GetNextEmbeddedAssemblyName()
        {
            SkipWhiteSpace();

            int src = _index;
            char[] buffer = new char[_chars.Length];
            int dst = 0;
            for (;;)
            {
                char c = _chars[src];
                if (c == NUL)
                    throw new ArgumentException();
                if (c == ']')
                    break;
                src++;

                // Backslash can be used to escape a ']' - any other backslash character is left alone (along with the backslash)
                // for the AssemblyName parser to handle.
                if (c == '\\' && _chars[src] == ']')
                {
                    c = _chars[src++];
                }
                buffer[dst++] = c;
            }
            _index = src;
            string fullName = new string(buffer, 0, dst);
            return RuntimeAssemblyName.Parse(fullName);
        }

        //
        // Classify a character as a TokenType. (Fortunately, all tokens in typename strings other than identifiers are single-character tokens.)
        //
        private static TokenType CharToToken(char c)
        {
            switch (c)
            {
                case NUL:
                    return TokenType.End;
                case '[':
                    return TokenType.OpenSqBracket;
                case ']':
                    return TokenType.CloseSqBracket;
                case ',':
                    return TokenType.Comma;
                case '+':
                    return TokenType.Plus;
                case '*':
                    return TokenType.Asterisk;
                case '&':
                    return TokenType.Ampersand;
                default:
                    return TokenType.Other;
            }
        }

        //
        // The desktop typename parser has a strange attitude towards whitespace. It throws away whitespace between punctuation tokens and whitespace
        // preceding identifiers or assembly names (and this cannot be escaped away). But whitespace between the end of an identifier
        // and the punctuation that ends it is *not* ignored.
        //
        // In other words, GetType("   Foo") searches for "Foo" but GetType("Foo   ") searches for "Foo   ".
        //
        // Whitespace between the end of an assembly name and the punction mark that ends it is also not ignored by this parser,
        // but this is irrelevant since the assembly name is then turned over to AssemblyName for parsing, which *does* ignore trailing whitespace.
        //
        private void SkipWhiteSpace()
        {
            while (char.IsWhiteSpace(_chars[_index]))
                _index++;
        }


        private int _index;
        private readonly char[] _chars;
        private const char NUL = (char)0;


        public sealed class IllegalEscapeSequenceException : Exception
        {
        }
    }

    internal enum TokenType
    {
        End = 0,              //At end of string
        OpenSqBracket = 1,    //'['
        CloseSqBracket = 2,   //']'
        Comma = 3,            //','
        Plus = 4,             //'+'
        Asterisk = 5,         //'*'
        Ampersand = 6,        //'&'
        Other = 7,            //Type identifier, AssemblyName or embedded AssemblyName.
    }
}
