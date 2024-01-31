// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

#nullable enable

namespace System.Reflection
{
    //
    // Parser for type names passed to GetType() apis.
    //
    [StructLayout(LayoutKind.Auto)]
    internal ref partial struct TypeNameParser
    {
        private ReadOnlySpan<char> _input;
        private int _index;
        private int _errorIndex; // Position for error reporting

        private TypeNameParser(ReadOnlySpan<char> name)
        {
            _input = name;
            _errorIndex = _index = 0;
        }

        //
        // Parses a type name. The type name may be optionally postpended with a "," followed by a legal assembly name.
        //
        private Type? Parse()
        {
            TypeName? typeName = ParseNonQualifiedTypeName();
            if (typeName is null)
                return null;

            string? assemblyName = null;

            TokenType token = GetNextToken();
            if (token != TokenType.End)
            {
                if (token != TokenType.Comma)
                {
                    ParseError();
                    return null;
                }

                if (!CheckTopLevelAssemblyQualifiedName())
                    return null;

                assemblyName = GetNextAssemblyName();
                if (assemblyName is null)
                    return null;
                Debug.Assert(Peek == TokenType.End);
            }

            return typeName.ResolveType(ref this, assemblyName);
        }

        //
        // Parses a type name without any assembly name qualification.
        //
        private TypeName? ParseNonQualifiedTypeName()
        {
            // Parse the named type or constructed generic type part first.
            TypeName? typeName = ParseNamedOrConstructedGenericTypeName();
            if (typeName is null)
                return null;

            // Iterate through any "has-element" qualifiers ([], &, *).
            while (true)
            {
                TokenType token = Peek;
                if (token == TokenType.End)
                    break;
                if (token == TokenType.Asterisk)
                {
                    Skip();
                    typeName = new ModifierTypeName(typeName, ModifierTypeName.Pointer);
                }
                else if (token == TokenType.Ampersand)
                {
                    Skip();
                    typeName = new ModifierTypeName(typeName, ModifierTypeName.ByRef);
                }
                else if (token == TokenType.OpenSqBracket)
                {
                    Skip();
                    token = GetNextToken();
                    if (token == TokenType.Asterisk)
                    {
                        typeName = new ModifierTypeName(typeName, 1);
                        token = GetNextToken();
                    }
                    else
                    {
                        int rank = 1;
                        while (token == TokenType.Comma)
                        {
                            token = GetNextToken();
                            rank++;
                        }
                        if (rank == 1)
                            typeName = new ModifierTypeName(typeName, ModifierTypeName.Array);
                        else
                            typeName = new ModifierTypeName(typeName, rank);
                    }
                    if (token != TokenType.CloseSqBracket)
                    {
                        ParseError();
                        return null;
                    }
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
        private TypeName? ParseNamedOrConstructedGenericTypeName()
        {
            TypeName? namedType = ParseNamedTypeName();
            if (namedType is null)
                return null;

            // Because "[" is used both for generic arguments and array indexes, we must peek two characters deep.
            if (!(Peek is TokenType.OpenSqBracket && (PeekSecond is TokenType.Other or TokenType.OpenSqBracket)))
                return namedType;

            Skip();

            TypeName[] typeArguments = new TypeName[2];
            int typeArgumentsCount = 0;
            while (true)
            {
                TypeName? typeArgument = ParseGenericTypeArgument();
                if (typeArgument is null)
                    return null;
                if (typeArgumentsCount >= typeArguments.Length)
                    Array.Resize(ref typeArguments, 2 * typeArgumentsCount);
                typeArguments[typeArgumentsCount++] = typeArgument;
                TokenType token = GetNextToken();
                if (token == TokenType.CloseSqBracket)
                    break;
                if (token != TokenType.Comma)
                {
                    ParseError();
                    return null;
                }
            }

            return new GenericTypeName(namedType, typeArguments, typeArgumentsCount);
        }

        //
        // Foo or Foo+Inner
        //
        private TypeName? ParseNamedTypeName()
        {
            string? fullName = GetNextIdentifier();
            if (fullName is null)
                return null;

            fullName = ApplyLeadingDotCompatQuirk(fullName);

            if (Peek == TokenType.Plus)
            {
                string[] nestedNames = new string[1];
                int nestedNamesCount = 0;

                do
                {
                    Skip();

                    string? nestedName = GetNextIdentifier();
                    if (nestedName is null)
                        return null;

                    nestedName = ApplyLeadingDotCompatQuirk(nestedName);

                    if (nestedNamesCount >= nestedNames.Length)
                        Array.Resize(ref nestedNames, 2 * nestedNamesCount);
                    nestedNames[nestedNamesCount++] = nestedName;
                }
                while (Peek == TokenType.Plus);

                return new NestedNamespaceTypeName(fullName, nestedNames, nestedNamesCount);
            }
            else
            {
                return new NamespaceTypeName(fullName);
            }

            // Compat: Ignore leading '.' for type names without namespace. .NET Framework historically ignored leading '.' here. It is likely
            // that code out there depends on this behavior. For example, type names formed by concatenating namespace and name, without checking for
            // empty namespace (bug), are going to have superfluous leading '.'.
            // This behavior means that types that start with '.' are not round-trippable via type name.
            static string ApplyLeadingDotCompatQuirk(string typeName)
            {
#if NETCOREAPP
                return (typeName.StartsWith('.') && !typeName.AsSpan(1).Contains('.')) ? typeName.Substring(1) : typeName;
#else
                return ((typeName.Length > 0) && (typeName[0] == '.') && typeName.LastIndexOf('.') == 0) ? typeName.Substring(1) : typeName;
#endif
            }
        }

        //
        // Parse a generic argument. In particular, generic arguments can take the special form [<typename>,<assemblyname>].
        //
        private TypeName? ParseGenericTypeArgument()
        {
            TokenType token = GetNextToken();
            if (token == TokenType.Other)
            {
                return ParseNonQualifiedTypeName();
            }
            if (token != TokenType.OpenSqBracket)
            {
                ParseError();
                return null;
            }
            string? assemblyName = null;
            TypeName? typeName = ParseNonQualifiedTypeName();
            if (typeName is null)
                return null;

            token = GetNextToken();
            if (token == TokenType.Comma)
            {
                assemblyName = GetNextEmbeddedAssemblyName();
                token = GetNextToken();
            }
            if (token != TokenType.CloseSqBracket)
            {
                ParseError();
                return null;
            }

            return (assemblyName != null) ? new AssemblyQualifiedTypeName(typeName, assemblyName) : typeName;
        }

        //
        // String tokenizer for type names passed to the GetType() APIs.
        //

        private TokenType Peek
        {
            get
            {
                SkipWhiteSpace();
                char c = (_index < _input.Length) ? _input[_index] : '\0';
                return CharToToken(c);
            }
        }

        private TokenType PeekSecond
        {
            get
            {
                SkipWhiteSpace();
                int index = _index + 1;
                while (index < _input.Length && char.IsWhiteSpace(_input[index]))
                    index++;
                char c = (index < _input.Length) ? _input[index] : '\0';
                return CharToToken(c);
            }
        }

        private void Skip()
        {
            SkipWhiteSpace();
            if (_index < _input.Length)
                _index++;
        }

        // Return the next token and skip index past it unless already at end of string
        // or the token is not a reserved token.
        private TokenType GetNextToken()
        {
            _errorIndex = _index;

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
        private string? GetNextIdentifier()
        {
            SkipWhiteSpace();

            ValueStringBuilder sb = new ValueStringBuilder(stackalloc char[64]);

            int src = _index;
            while (true)
            {
                if (src >= _input.Length)
                    break;
                char c = _input[src];
                TokenType token = CharToToken(c);
                if (token != TokenType.Other)
                    break;
                src++;
                if (c == '\\') // Check for escaped character
                {
                    // Update error location
                    _errorIndex = src - 1;

                    c = (src < _input.Length) ? _input[src++] : '\0';

                    if (!NeedsEscapingInTypeName(c))
                    {
                        // If we got here, a backslash was used to escape a character that is not legal to escape inside a type name.
                        ParseError();
                        return null;
                    }
                }
                sb.Append(c);
            }
            _index = src;

            if (sb.Length == 0)
            {
                // The identifier has to be non-empty
                _errorIndex = src;
                ParseError();
                return null;
            }

            return sb.ToString();
        }

        //
        // Lex the next segment as the assembly name at the end of an assembly-qualified type name. (Do not use for
        // assembly names embedded inside generic type arguments.)
        //
        private string? GetNextAssemblyName()
        {
            if (!StartAssemblyName())
                return null;

            string assemblyName = _input.Slice(_index).ToString();
            _index = _input.Length;
            return assemblyName;
        }

        //
        // Lex the next segment as an assembly name embedded inside a generic argument type.
        //
        // Terminated by an unescaped ']'.
        //
        private string? GetNextEmbeddedAssemblyName()
        {
            if (!StartAssemblyName())
                return null;

            ValueStringBuilder sb = new ValueStringBuilder(stackalloc char[64]);

            int src = _index;
            while (true)
            {
                if (src >= _input.Length)
                {
                    ParseError();
                    return null;
                }
                char c = _input[src];
                if (c == ']')
                    break;
                src++;

                // Backslash can be used to escape a ']' - any other backslash character is left alone (along with the backslash)
                // for the AssemblyName parser to handle.
                if (c == '\\' && (src < _input.Length) && _input[src] == ']')
                {
                    c = _input[src++];
                }
                sb.Append(c);
            }
            _index = src;

            if (sb.Length == 0)
            {
                // The assembly name has to be non-empty
                _errorIndex = src;
                ParseError();
                return null;
            }

            return sb.ToString();
        }

        private bool StartAssemblyName()
        {
            // Compat: Treat invalid starting token of assembly name as type name parsing error instead of assembly name parsing error. This only affects
            // exception returned by the parser.
            if (Peek is TokenType.End or TokenType.Comma)
            {
                ParseError();
                return false;
            }
            return true;
        }

        //
        // Classify a character as a TokenType. (Fortunately, all tokens in type name strings other than identifiers are single-character tokens.)
        //
        private static TokenType CharToToken(char c)
        {
            return c switch
            {
                '\0' => TokenType.End,
                '[' => TokenType.OpenSqBracket,
                ']' => TokenType.CloseSqBracket,
                ',' => TokenType.Comma,
                '+' => TokenType.Plus,
                '*' => TokenType.Asterisk,
                '&' => TokenType.Ampersand,
                _ => TokenType.Other,
            };
        }

        //
        // The type name parser has a strange attitude towards whitespace. It throws away whitespace between punctuation tokens and whitespace
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
            while (_index < _input.Length && char.IsWhiteSpace(_input[_index]))
                _index++;
        }

        private enum TokenType
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

        //
        // The TypeName class is the base class for a family of types that represent the nodes in a parse tree for
        // assembly-qualified type names.
        //
        private abstract class TypeName
        {
            /// <summary>
            /// Helper for the Type.GetType() family of APIs. "containingAssemblyIsAny" is the assembly to search for (as determined
            /// by a qualifying assembly string in the original type string passed to Type.GetType(). If null, it means the type stream
            /// didn't specify an assembly name. How to respond to that is up to the type resolver delegate in getTypeOptions - this class
            /// is just a middleman.
            /// </summary>
            public abstract Type? ResolveType(ref TypeNameParser parser, string? containingAssemblyIfAny);
        }

        //
        // Represents a parse of a type name qualified by an assembly name.
        //
        private sealed class AssemblyQualifiedTypeName : TypeName
        {
            private readonly string _assemblyName;
            private readonly TypeName _nonQualifiedTypeName;

            public AssemblyQualifiedTypeName(TypeName nonQualifiedTypeName, string assemblyName)
            {
                _nonQualifiedTypeName = nonQualifiedTypeName;
                _assemblyName = assemblyName;
            }

            public override Type? ResolveType(ref TypeNameParser parser, string? containingAssemblyIfAny)
            {
                return _nonQualifiedTypeName.ResolveType(ref parser, _assemblyName);
            }
        }

        //
        // Non-nested named type. The full name is the namespace-qualified name. For example, the FullName for
        // System.Collections.Generic.IList<> is "System.Collections.Generic.IList`1".
        //
        private sealed partial class NamespaceTypeName : TypeName
        {
            private readonly string _fullName;
            public NamespaceTypeName(string fullName)
            {
                _fullName = fullName;
            }

            public override Type? ResolveType(ref TypeNameParser parser, string? containingAssemblyIfAny)
            {
                return parser.GetType(_fullName, default, containingAssemblyIfAny);
            }
        }

        //
        // Nested type name.
        //
        private sealed partial class NestedNamespaceTypeName : TypeName
        {
            private readonly string _fullName;
            private readonly string[] _nestedNames;
            private readonly int _nestedNamesCount;

            public NestedNamespaceTypeName(string fullName, string[] nestedNames, int nestedNamesCount)
            {
                _fullName = fullName;
                _nestedNames = nestedNames;
                _nestedNamesCount = nestedNamesCount;
            }

            public override Type? ResolveType(ref TypeNameParser parser, string? containingAssemblyIfAny)
            {
                return parser.GetType(_fullName, _nestedNames.AsSpan(0, _nestedNamesCount), containingAssemblyIfAny);
            }
        }

        //
        // Array, byref or pointer type name.
        //
        private sealed class ModifierTypeName : TypeName
        {
            private readonly TypeName _elementTypeName;

            // Positive value is multi-dimensional array rank.
            // Negative value is modifier encoded using constants below.
            private readonly int _rankOrModifier;

            public const int Array = -1;
            public const int Pointer = -2;
            public const int ByRef = -3;

            public ModifierTypeName(TypeName elementTypeName, int rankOrModifier)
            {
                _elementTypeName = elementTypeName;
                _rankOrModifier = rankOrModifier;
            }

#if NETCOREAPP
            [UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi",
                Justification = "Used to implement resolving types from strings.")]
#endif
            public override Type? ResolveType(ref TypeNameParser parser, string? containingAssemblyIfAny)
            {
                Type? elementType = _elementTypeName.ResolveType(ref parser, containingAssemblyIfAny);
                if (elementType is null)
                    return null;

                return _rankOrModifier switch
                {
                    Array => elementType.MakeArrayType(),
                    Pointer => elementType.MakePointerType(),
                    ByRef => elementType.MakeByRefType(),
                    _ => elementType.MakeArrayType(_rankOrModifier)
                };
            }
        }

        //
        // Constructed generic type name.
        //
        private sealed class GenericTypeName : TypeName
        {
            private readonly TypeName _typeDefinition;
            private readonly TypeName[] _typeArguments;
            private readonly int _typeArgumentsCount;

            public GenericTypeName(TypeName genericTypeDefinition, TypeName[] typeArguments, int typeArgumentsCount)
            {
                _typeDefinition = genericTypeDefinition;
                _typeArguments = typeArguments;
                _typeArgumentsCount = typeArgumentsCount;
            }

#if NETCOREAPP
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
                Justification = "Used to implement resolving types from strings.")]
            [UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi",
                Justification = "Used to implement resolving types from strings.")]
#endif
            public override Type? ResolveType(ref TypeNameParser parser, string? containingAssemblyIfAny)
            {
                Type? typeDefinition = _typeDefinition.ResolveType(ref parser, containingAssemblyIfAny);
                if (typeDefinition is null)
                    return null;

                Type[] arguments = new Type[_typeArgumentsCount];
                for (int i = 0; i < arguments.Length; i++)
                {
                    Type? typeArgument = _typeArguments[i].ResolveType(ref parser, null);
                    if (typeArgument is null)
                        return null;
                    arguments[i] = typeArgument;
                }

                return typeDefinition.MakeGenericType(arguments);
            }
        }

        //
        // Type name escaping helpers
        //

#if NETCOREAPP
        private static ReadOnlySpan<char> CharsToEscape => "\\[]+*&,";

        private static bool NeedsEscapingInTypeName(char c)
            => CharsToEscape.Contains(c);
#else
        private static char[] CharsToEscape { get; } = "\\[]+*&,".ToCharArray();

        private static bool NeedsEscapingInTypeName(char c)
            => Array.IndexOf(CharsToEscape, c) >= 0;
#endif

        internal static string EscapeTypeName(string name)
        {
            if (name.AsSpan().IndexOfAny(CharsToEscape) < 0)
                return name;

            var sb = new ValueStringBuilder(stackalloc char[64]);
            foreach (char c in name)
            {
                if (NeedsEscapingInTypeName(c))
                    sb.Append('\\');
                sb.Append(c);
            }

            return sb.ToString();
        }

        internal static string EscapeTypeName(string typeName, ReadOnlySpan<string> nestedTypeNames)
        {
            string fullName = EscapeTypeName(typeName);
            if (nestedTypeNames.Length > 0)
            {
                var sb = new StringBuilder(fullName);
                for (int i = 0; i < nestedTypeNames.Length; i++)
                {
                    sb.Append('+');
                    sb.Append(EscapeTypeName(nestedTypeNames[i]));
                }
                fullName = sb.ToString();
            }
            return fullName;
        }

        private static (string typeNamespace, string name) SplitFullTypeName(string typeName)
        {
            string typeNamespace, name;

            // Matches algorithm from ns::FindSep in src\coreclr\utilcode\namespaceutil.cpp
            int separator = typeName.LastIndexOf('.');
            if (separator <= 0)
            {
                typeNamespace = "";
                name = typeName;
            }
            else
            {
                if (typeName[separator - 1] == '.')
                    separator--;
                typeNamespace = typeName.Substring(0, separator);
                name = typeName.Substring(separator + 1);
            }

            return (typeNamespace, name);
        }

#if SYSTEM_PRIVATE_CORELIB
        private void ParseError()
        {
            if (_throwOnError)
                throw new ArgumentException(SR.Arg_ArgumentException, $"typeName@{_errorIndex}");
        }
#endif
    }
}
