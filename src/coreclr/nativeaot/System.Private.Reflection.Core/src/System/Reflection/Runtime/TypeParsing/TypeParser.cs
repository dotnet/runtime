// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Runtime.Assemblies;

namespace System.Reflection.Runtime.TypeParsing
{
    //
    // Parser for type names passed to GetType() apis.
    //
    internal sealed class TypeParser
    {
        //
        // Parses a typename. The typename may be optionally postpended with a "," followed by a legal assembly name.
        //
        public static TypeName ParseAssemblyQualifiedTypeName(string s, bool throwOnError)
        {
            if (throwOnError)
                return ParseAssemblyQualifiedTypeName(s);

            try
            {
                return ParseAssemblyQualifiedTypeName(s);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        //
        // Parses a typename. The typename may be optionally postpended with a "," followed by a legal assembly name.
        //
        private static TypeName ParseAssemblyQualifiedTypeName(string s)
        {
            // Desktop compat: a whitespace-only "typename" qualified by an assembly name throws an ArgumentException rather than
            // a TypeLoadException.
            int idx = 0;
            while (idx < s.Length && char.IsWhiteSpace(s[idx]))
            {
                idx++;
            }
            if (idx < s.Length && s[idx] == ',')
                throw new ArgumentException(SR.Arg_TypeLoadNullStr, $"typeName@{idx}");

            try
            {
                TypeParser parser = new TypeParser(s);
                NonQualifiedTypeName typeName = parser.ParseNonQualifiedTypeName();
                TokenType token = parser._lexer.GetNextToken();
                if (token == TokenType.End)
                    return typeName;
                if (token == TokenType.Comma)
                {
                    RuntimeAssemblyName assemblyName = parser._lexer.GetNextAssemblyName();
                    token = parser._lexer.Peek;
                    if (token != TokenType.End)
                        throw new ArgumentException();
                    return new AssemblyQualifiedTypeName(typeName, assemblyName);
                }
                throw new ArgumentException();
            }
            catch (TypeLexer.IllegalEscapeSequenceException)
            {
                // Emulates a CLR4.5 bug that causes any string that contains an illegal escape sequence to be parsed as the empty string.
                return ParseAssemblyQualifiedTypeName(string.Empty);
            }
        }

        private TypeParser(string s)
        {
            _lexer = new TypeLexer(s);
        }


        //
        // Parses a type name without any assembly name qualification.
        //
        private NonQualifiedTypeName ParseNonQualifiedTypeName()
        {
            // Parse the named type or constructed generic type part first.
            NonQualifiedTypeName typeName = ParseNamedOrConstructedGenericTypeName();

            // Iterate through any "has-element" qualifiers ([], &, *).
            for (;;)
            {
                TokenType token = _lexer.Peek;
                if (token == TokenType.End)
                    break;
                if (token == TokenType.Asterisk)
                {
                    _lexer.Skip();
                    typeName = new PointerTypeName(typeName);
                }
                else if (token == TokenType.Ampersand)
                {
                    _lexer.Skip();
                    typeName = new ByRefTypeName(typeName);
                }
                else if (token == TokenType.OpenSqBracket)
                {
                    _lexer.Skip();
                    token = _lexer.GetNextToken();
                    if (token == TokenType.Asterisk)
                    {
                        typeName = new MultiDimArrayTypeName(typeName, 1);
                        token = _lexer.GetNextToken();
                    }
                    else
                    {
                        int rank = 1;
                        while (token == TokenType.Comma)
                        {
                            token = _lexer.GetNextToken();
                            rank++;
                        }
                        if (rank == 1)
                            typeName = new ArrayTypeName(typeName);
                        else
                            typeName = new MultiDimArrayTypeName(typeName, rank);
                    }
                    if (token != TokenType.CloseSqBracket)
                        throw new ArgumentException();
                }
                else
                {
                    break;
                }
            }
            return typeName;
        }

        //
        // Foo or Foo+Inner or Foo[String] or Foo+Inner[String]
        //
        private NonQualifiedTypeName ParseNamedOrConstructedGenericTypeName()
        {
            NamedTypeName namedType = ParseNamedTypeName();
            // Because "[" is used both for generic arguments and array indexes, we must peek two characters deep.
            if (!(_lexer.Peek == TokenType.OpenSqBracket && (_lexer.PeekSecond == TokenType.Other || _lexer.PeekSecond == TokenType.OpenSqBracket)))
                return namedType;
            else
            {
                _lexer.Skip();
                LowLevelListWithIList<TypeName> genericTypeArguments = new LowLevelListWithIList<TypeName>();
                for (;;)
                {
                    TypeName genericTypeArgument = ParseGenericTypeArgument();
                    genericTypeArguments.Add(genericTypeArgument);
                    TokenType token = _lexer.GetNextToken();
                    if (token == TokenType.CloseSqBracket)
                        break;
                    if (token != TokenType.Comma)
                        throw new ArgumentException();
                }

                return new ConstructedGenericTypeName(namedType, genericTypeArguments);
            }
        }

        //
        // Foo or Foo+Inner
        //
        private NamedTypeName ParseNamedTypeName()
        {
            NamedTypeName namedType = ParseNamespaceTypeName();
            while (_lexer.Peek == TokenType.Plus)
            {
                _lexer.Skip();
                string nestedTypeName = _lexer.GetNextIdentifier();
                namedType = new NestedTypeName(nestedTypeName, namedType);
            }
            return namedType;
        }

        //
        // Non-nested named type.
        //
        private NamespaceTypeName ParseNamespaceTypeName()
        {
            string fullName = _lexer.GetNextIdentifier();
            return new NamespaceTypeName(fullName);
        }

        //
        // Parse a generic argument. In particular, generic arguments can take the special form [<typename>,<assemblyname>].
        //
        private TypeName ParseGenericTypeArgument()
        {
            TokenType token = _lexer.GetNextToken();
            if (token == TokenType.Other)
            {
                NonQualifiedTypeName nonQualifiedTypeName = ParseNonQualifiedTypeName();
                return nonQualifiedTypeName;
            }
            else if (token == TokenType.OpenSqBracket)
            {
                RuntimeAssemblyName assemblyName = null;
                NonQualifiedTypeName typeName = ParseNonQualifiedTypeName();
                token = _lexer.GetNextToken();
                if (token == TokenType.Comma)
                {
                    assemblyName = _lexer.GetNextEmbeddedAssemblyName();
                    token = _lexer.GetNextToken();
                }
                if (token != TokenType.CloseSqBracket)
                    throw new ArgumentException();
                if (assemblyName == null)
                    return typeName;
                else
                    return new AssemblyQualifiedTypeName(typeName, assemblyName);
            }
            else
                throw new ArgumentException();
        }

        private readonly TypeLexer _lexer;
    }
}
