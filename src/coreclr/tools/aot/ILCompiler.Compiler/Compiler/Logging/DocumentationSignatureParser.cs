// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Internal.TypeSystem;

namespace ILCompiler.Logging
{
    /// <summary>
    ///  Parses a signature for a member, in the format used for C# Documentation Comments:
    ///  https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format
    ///  Adapted from Roslyn's DocumentationCommentId:
    ///  https://github.com/dotnet/roslyn/blob/master/src/Compilers/Core/Portable/DocumentationCommentId.cs
    /// </summary>
    ///
    /// Roslyn's API works with ISymbol, which represents a symbol exposed by the compiler.
    /// a Symbol has information about the source language, name, metadata name,
    /// containing scopes, visibility/accessibility, attributes, etc.
    /// This API instead works with the Cecil OM. It can be used to refer to IL definitions
    /// where the signature of a member can contain references to instantiated generics.
    ///
    public static class DocumentationSignatureParser
    {
        [Flags]
        public enum MemberType
        {
            Method = 0x0001,
            Field = 0x0002,
            Type = 0x0004,
            Property = 0x0008,
            Event = 0x0010,
            All = Method | Field | Type | Property | Event
        }

        public static IEnumerable<TypeSystemEntity> GetMembersForDocumentationSignature(string id, ModuleDesc module)
        {
            var results = new List<TypeSystemEntity>();
            if (id == null || module == null)
                return results;

            ParseDocumentationSignature(id, module, results);
            return results;
        }

        // Takes a documentation signature (not including the documentation member type prefix) and resolves it to a type
        // in the assembly.
        public static TypeDesc GetTypeByDocumentationSignature(IAssemblyDesc assembly, string signature)
        {
            int index = 0;
            var results = new List<TypeSystemEntity>();
            ParseSignaturePart(signature, ref index, (ModuleDesc)assembly, MemberType.Type, results);
            Debug.Assert(results.Count <= 1);
            return results.Count == 0 ? null : (TypeDesc)results[0];
        }

        // Takes a member signature (not including the declaring type) and returns the matching members on the type.
        public static IEnumerable<TypeSystemEntity> GetMembersByDocumentationSignature(MetadataType type, string signature, bool acceptName = false)
        {
            int index = 0;
            var results = new List<TypeSystemEntity>();
            var nameBuilder = new StringBuilder();
            var (name, arity) = ParseTypeOrNamespaceName(signature, ref index, nameBuilder);
            GetMatchingMembers(signature, ref index, type.Module, type, name, arity, MemberType.All, results, acceptName);
            return results;
        }

        private static string GetSignaturePart(TypeDesc type)
        {
            var builder = new StringBuilder();
            DocumentationSignatureGenerator.PartVisitor.Instance.AppendName(builder, type);
            return builder.ToString();
        }

        private static bool ParseDocumentationSignature(string id, ModuleDesc module, List<TypeSystemEntity> results)
        {
            if (id == null)
                return false;

            if (id.Length < 2)
                return false;

            int index = 0;
            results.Clear();
            ParseSignature(id, ref index, module, results);
            return results.Count > 0;
        }

        private static void ParseSignature(string id, ref int index, ModuleDesc module, List<TypeSystemEntity> results)
        {
            Debug.Assert(results.Count == 0);
            var memberTypeChar = PeekNextChar(id, index);
            MemberType memberType;

            switch (memberTypeChar)
            {
                case 'E':
                    memberType = MemberType.Event;
                    break;
                case 'F':
                    memberType = MemberType.Field;
                    break;
                case 'M':
                    memberType = MemberType.Method;
                    break;
                case 'N':
                    // We do not support namespaces, which do not exist in IL.
                    return;
                case 'P':
                    memberType = MemberType.Property;
                    break;
                case 'T':
                    memberType = MemberType.Type;
                    break;
                default:
                    // Documentation comment id must start with E, F, M, P, or T
                    return;
            }

            index++;
            // Note: this allows leaving out the ':'.
            if (PeekNextChar(id, index) == ':')
                index++;

            ParseSignaturePart(id, ref index, module, memberType, results);
        }

        // Parses and resolves a fully-qualified (namespace and nested types but no assembly) member signature,
        // without the member type prefix. The results include all members matching the specified member types.
        public static void ParseSignaturePart(string id, ref int index, ModuleDesc module, MemberType memberTypes, List<TypeSystemEntity> results)
        {
            // Roslyn resolves types by searching namespaces top-down.
            // We don't have namespace info. Instead try treating each part of a
            // dotted name as a type first, then as a namespace if it fails
            // to resolve to a type.
            TypeDesc containingType = null;
            var nameBuilder = new StringBuilder();

            string name;
            int arity;

            // process dotted names
            while (true)
            {
                (name, arity) = ParseTypeOrNamespaceName(id, ref index, nameBuilder);
                // if we are at the end of the dotted name and still haven't resolved it to
                // a type, there are no results.
                if (string.IsNullOrEmpty(name))
                    return;

                // no more dots, so don't loop any more
                if (PeekNextChar(id, index) != '.')
                    break;

                // must be a namespace or type since name continues after dot
                index++;

                // try to resolve it as a type
                var typeOrNamespaceName = nameBuilder.ToString();
                GetMatchingTypes(module, declaringType: containingType, name: typeOrNamespaceName, arity: arity, results: results);
                Debug.Assert(results.Count <= 1);
                if (results.Any())
                {
                    // the name resolved to a type
                    var result = results.Single();
                    Debug.Assert(result is TypeDesc);
                    // result becomes the new container
                    containingType = result as TypeDesc;
                    nameBuilder.Clear();
                    results.Clear();
                    continue;
                }

                // it didn't resolve as a type.

                // only types have arity.
                if (arity > 0)
                    return;

                // treat it as a namespace and continue building up the type name
                nameBuilder.Append('.');
            }

            var memberName = nameBuilder.ToString();
            GetMatchingMembers(id, ref index, module, containingType, memberName, arity, memberTypes, results);
        }

        // Gets all members of the specified member kinds of the containing type, with
        // mathing name, arity, and signature at the current index (for methods and properties).
        // This will also resolve types from the given module if no containing type is given.
        public static void GetMatchingMembers(string id, ref int index, ModuleDesc module, TypeDesc containingType, string memberName, int arity, MemberType memberTypes, List<TypeSystemEntity> results, bool acceptName = false)
        {
            if (memberTypes.HasFlag(MemberType.Type))
                GetMatchingTypes(module, containingType, memberName, arity, results);

            if (containingType == null)
                return;

            int startIndex = index;
            int endIndex = index;

            if (memberTypes.HasFlag(MemberType.Method))
            {
                GetMatchingMethods(id, ref index, containingType, memberName, arity, results, acceptName);
                endIndex = index;
                index = startIndex;
            }

#if false
            if (memberTypes.HasFlag(MemberType.Property))
            {
                GetMatchingProperties(id, ref index, containingType, memberName, results, acceptName);
                endIndex = index;
                index = startIndex;
            }
#endif

            index = endIndex;

#if false
            if (memberTypes.HasFlag(MemberType.Event))
                GetMatchingEvents(containingType, memberName, results);
#endif

            if (memberTypes.HasFlag(MemberType.Field))
                GetMatchingFields(containingType, memberName, results);
        }

        // Parses a part of a dotted declaration name, including generic definitions.
        // Returns the name (either a namespace or the unmangled name of a C# type) and an arity
        // which may be non-zero for generic types.
        public static (string name, int arity) ParseTypeOrNamespaceName(string id, ref int index, StringBuilder nameBuilder)
        {
            var name = ParseName(id, ref index);
            // don't parse ` after an empty name
            if (string.IsNullOrEmpty(name))
                return (name, 0);

            nameBuilder.Append(name);
            var arity = 0;

            // has type parameters?
            if (PeekNextChar(id, index) == '`')
            {
                index++;

                bool genericType = true;

                // method type parameters?
                // note: this allows `` for type parameters
                if (PeekNextChar(id, index) == '`')
                {
                    index++;
                    genericType = false;
                }

                arity = ReadNextInteger(id, ref index);

                if (genericType)
                {
                    // We need to mangle generic type names but not generic method names.
                    nameBuilder.Append('`');
                    nameBuilder.Append(arity);
                }
            }

            return (name, arity);
        }

        // Roslyn resolves types in a signature to their declaration by searching through namespaces.
        // To avoid looking for types by name in all referenced assemblies, we just represent types
        // that are part of a signature by their doc comment strings, and we check for matching
        // strings when looking for matching member signatures.
        private static string ParseTypeSymbol(string id, ref int index, TypeSystemEntity typeParameterContext)
        {
            var results = new List<string>();
            ParseTypeSymbol(id, ref index, typeParameterContext, results);
            if (results.Count == 1)
                return results[0];

            Debug.Assert(results.Count == 0);
            return null;
        }

        private static void ParseTypeSymbol(string id, ref int index, TypeSystemEntity typeParameterContext, List<string> results)
        {
            // Note: Roslyn has a special case that deviates from the language spec, which
            // allows context expressions embedded in a type reference => <context-definition>:<type-parameter>
            // We do not support this special format.

            Debug.Assert(results.Count == 0);

            if (PeekNextChar(id, index) == '`')
                ParseTypeParameterSymbol(id, ref index, typeParameterContext, results);
            else
                ParseNamedTypeSymbol(id, ref index, typeParameterContext, results);

            // apply any array or pointer constructions to results
            var startIndex = index;
            var endIndex = index;

            for (int i = 0; i < results.Count; i++)
            {
                index = startIndex;
                var typeReference = results[i];

                while (true)
                {
                    if (PeekNextChar(id, index) == '[')
                    {
                        var boundsStartIndex = index;
                        var bounds = ParseArrayBounds(id, ref index);
                        var boundsEndIndex = index;
                        Debug.Assert(bounds > 0);
                        // Instead of constructing a representation of the array bounds, we
                        // use the original input to represent the bounds, and later match it
                        // against the generated strings for types in signatures.
                        // This ensures that we will only resolve members with supported array bounds.
                        typeReference += id.Substring(boundsStartIndex, boundsEndIndex - boundsStartIndex);
                        continue;
                    }

                    if (PeekNextChar(id, index) == '*')
                    {
                        index++;
                        typeReference += '*';
                        continue;
                    }

                    break;
                }

                if (PeekNextChar(id, index) == '@')
                {
                    index++;
                    typeReference += '@';
                }

                results[i] = typeReference;
                endIndex = index;
            }

            index = endIndex;
        }

        private static void ParseTypeParameterSymbol(string id, ref int index, TypeSystemEntity typeParameterContext, List<string> results)
        {
            // skip the first `
            Debug.Assert(PeekNextChar(id, index) == '`');
            index++;

            Debug.Assert(
                typeParameterContext == null ||
                typeParameterContext is MethodDesc ||
                typeParameterContext is TypeDesc
            );

            if (PeekNextChar(id, index) == '`')
            {
                // `` means this is a method type parameter
                index++;
                var methodTypeParameterIndex = ReadNextInteger(id, ref index);

                if (typeParameterContext is MethodDesc methodContext)
                {
                    var count = methodContext.Instantiation.Length;
                    if (count > 0 && methodTypeParameterIndex < count)
                    {
                        results.Add("``" + methodTypeParameterIndex);
                    }
                }
            }
            else
            {
                // regular type parameter
                var typeParameterIndex = ReadNextInteger(id, ref index);

                var typeContext = typeParameterContext is MethodDesc methodContext
                    ? methodContext.OwningType
                    : typeParameterContext as TypeDesc;

                if (typeParameterIndex >= 0 ||
                    typeParameterIndex < typeContext?.Instantiation.Length)
                {
                    // No need to look at declaring types like Roslyn, because type parameters are redeclared.
                    results.Add("`" + typeParameterIndex);
                }
            }
        }

        private static void ParseNamedTypeSymbol(string id, ref int index, TypeSystemEntity typeParameterContext, List<string> results)
        {
            Debug.Assert(results.Count == 0);
            var nameBuilder = new StringBuilder();
            // loop for dotted names
            while (true)
            {
                var name = ParseName(id, ref index);
                if (string.IsNullOrEmpty(name))
                    return;

                nameBuilder.Append(name);

                List<string> typeArguments = null;
                int arity = 0;

                // type arguments
                if (PeekNextChar(id, index) == '{')
                {
                    typeArguments = new List<string>();
                    if (!ParseTypeArguments(id, ref index, typeParameterContext, typeArguments))
                    {
                        continue;
                    }

                    arity = typeArguments.Count;
                }

                if (arity != 0)
                {
                    Debug.Assert(typeArguments != null && typeArguments.Count != 0);
                    nameBuilder.Append('{');
                    bool needsComma = false;
                    foreach (var typeArg in typeArguments)
                    {
                        if (needsComma)
                        {
                            nameBuilder.Append(',');
                        }
                        nameBuilder.Append(typeArg);
                        needsComma = true;
                    }
                    nameBuilder.Append('}');
                }

                if (PeekNextChar(id, index) != '.')
                    break;

                index++;
                nameBuilder.Append('.');
            }

            results.Add(nameBuilder.ToString());
        }

        private static int ParseArrayBounds(string id, ref int index)
        {
            index++; // skip '['

            int bounds = 0;

            while (true)
            {
                // note: the actual bounds are ignored.
                // C# only supports arrays with lower bound zero.
                // size is not known.

                if (char.IsDigit(PeekNextChar(id, index)))
                    ReadNextInteger(id, ref index);

                if (PeekNextChar(id, index) == ':')
                {
                    index++;

                    // note: the spec says that omitting both the lower bounds and the size
                    // should omit the ':' as well, but this allows for it in the input.
                    if (char.IsDigit(PeekNextChar(id, index)))
                        ReadNextInteger(id, ref index);
                }

                bounds++;

                if (PeekNextChar(id, index) == ',')
                {
                    index++;
                    continue;
                }

                break;
            }

            // note: this allows leaving out the closing ']'
            if (PeekNextChar(id, index) == ']')
                index++;

            return bounds;
        }

        private static bool ParseTypeArguments(string id, ref int index, TypeSystemEntity typeParameterContext, List<string> typeArguments)
        {
            index++; // skip over {

            while (true)
            {
                var type = ParseTypeSymbol(id, ref index, typeParameterContext);

                if (type == null)
                {
                    // if a type argument cannot be identified, argument list is no good
                    return false;
                }

                // add first one
                typeArguments.Add(type);

                if (PeekNextChar(id, index) == ',')
                {
                    index++;
                    continue;
                }

                break;
            }

            // note: this doesn't require closing }
            if (PeekNextChar(id, index) == '}')
            {
                index++;
            }

            return true;
        }

        private static void GetMatchingTypes(ModuleDesc module, TypeDesc declaringType, string name, int arity, List<TypeSystemEntity> results)
        {
            Debug.Assert(module != null);

            if (declaringType == null)
            {
                int indexOfLastDot = name.LastIndexOf('.');
                string namespacepart;
                string namepart;
                if (indexOfLastDot > 0 && indexOfLastDot < name.Length - 1)
                {
                    namespacepart = name.Substring(indexOfLastDot - 1);
                    namepart = name.Substring(indexOfLastDot + 1);
                }
                else
                {
                    namespacepart = "";
                    namepart = name;
                }

                var type = module.GetType(namespacepart, namepart, throwIfNotFound: false);
                if (type != null)
                {
                    results.Add(type);
                }
                return;
            }

            if (declaringType is not MetadataType mdDeclaringType)
                return;

            foreach (var nestedType in mdDeclaringType.GetNestedTypes())
            {
                Debug.Assert(string.IsNullOrEmpty(nestedType.Namespace));
                if (nestedType.Name != name)
                    continue;

                // Compute arity counting only the newly-introduced generic parameters
                var declaringArity = declaringType.Instantiation.Length;
                int totalArity = nestedType.Instantiation.Length;
                var nestedTypeArity = totalArity - declaringArity;
                if (nestedTypeArity != arity)
                    continue;

                results.Add(nestedType);
                return;
            }
        }

        private static void GetMatchingMethods(string id, ref int index, TypeDesc type, string memberName, int arity, List<TypeSystemEntity> results, bool acceptName = false)
        {
            if (type == null)
                return;

            var parameters = new List<string>();
            var startIndex = index;
            var endIndex = index;

            foreach (var method in type.GetMethods())
            {
                index = startIndex;
                if (method.Name != memberName)
                    continue;

                var methodArity = method.Instantiation.Length;
                if (methodArity != arity)
                    continue;

                parameters.Clear();
                bool isNameOnly = true;
                if (PeekNextChar(id, index) == '(')
                {
                    isNameOnly = false;
                    // if the parameters cannot be identified (some error), then the symbol cannot match, try next method symbol
                    if (!ParseParameterList(id, ref index, method, parameters))
                        continue;
                }

                // note: this allows extra characters at the end

                if (PeekNextChar(id, index) == '~')
                {
                    isNameOnly = false;
                    index++;
                    string returnType = ParseTypeSymbol(id, ref index, method);
                    if (returnType == null)
                        continue;

                    // if return type is specified, then it must match
                    if (GetSignaturePart(method.Signature.ReturnType) != returnType)
                        continue;
                }

                if (!isNameOnly || !acceptName)
                {
                    // check parameters unless we are matching a name only
                    if (!AllParametersMatch(method.Signature, parameters))
                        continue;
                }

                results.Add(method);
                endIndex = index;
            }
            index = endIndex;
        }

#if false
        static void GetMatchingProperties(string id, ref int index, TypeDesc type, string memberName, List<TypeSystemEntity> results, bool acceptName = false)
        {
            if (type == null)
                return;

            int startIndex = index;
            int endIndex = index;

            List<string> parameters = null;
            // Unlike Roslyn, we don't need to decode property names because we are working
            // directly with IL.
            foreach (var property in type.Properties)
            {
                index = startIndex;
                if (property.Name != memberName)
                    continue;
                if (PeekNextChar(id, index) == '(')
                {
                    if (parameters == null)
                    {
                        parameters = new List<string>();
                    }
                    else
                    {
                        parameters.Clear();
                    }
                    if (!ParseParameterList(id, ref index, property.DeclaringType, parameters))
                        continue;
                    if (!AllParametersMatch(property.Parameters, parameters))
                        continue;
                }
                else
                {
                    if (!acceptName && property.Parameters.Count != 0)
                        continue;
                }
                results.Add(property);
                endIndex = index;
            }

            index = endIndex;
        }
#endif

        private static void GetMatchingFields(TypeDesc type, string memberName, List<TypeSystemEntity> results)
        {
            if (type == null)
                return;
            foreach (var field in type.GetFields())
            {
                if (field.Name != memberName)
                    continue;
                results.Add(field);
            }
        }

#if false
        static void GetMatchingEvents(TypeDesc type, string memberName, List<TypeSystemEntity> results)
        {
            if (type == null)
                return;
            foreach (var evt in type.Events)
            {
                if (evt.Name != memberName)
                    continue;
                results.Add(evt);
            }
        }
#endif

        private static bool AllParametersMatch(MethodSignature methodParameters, List<string> expectedParameters)
        {
            if (methodParameters.Length != expectedParameters.Count)
                return false;

            for (int i = 0; i < expectedParameters.Count; i++)
            {
                if (GetSignaturePart(methodParameters[i]) != expectedParameters[i])
                    return false;
            }

            return true;
        }

        private static bool ParseParameterList(string id, ref int index, TypeSystemEntity typeParameterContext, List<string> parameters)
        {
            Debug.Assert(typeParameterContext != null);

            index++; // skip over '('

            if (PeekNextChar(id, index) == ')')
            {
                // note: this will match parameterless methods, or methods with only varargs parameters
                index++;
                return true;
            }

            string parameter = ParseTypeSymbol(id, ref index, typeParameterContext);
            if (parameter == null)
                return false;

            parameters.Add(parameter);

            while (PeekNextChar(id, index) == ',')
            {
                index++;

                parameter = ParseTypeSymbol(id, ref index, typeParameterContext);
                if (parameter == null)
                    return false;

                parameters.Add(parameter);
            }

            // note: this doesn't require the trailing ')'
            if (PeekNextChar(id, index) == ')')
            {
                index++;
            }

            return true;
        }

        private static char PeekNextChar(string id, int index)
        {
            return index >= id.Length ? '\0' : id[index];
        }

        private static readonly char[] s_nameDelimiters = { ':', '.', '(', ')', '{', '}', '[', ']', ',', '\'', '@', '*', '`', '~' };

        private static string ParseName(string id, ref int index)
        {
            string name;

            int delimiterOffset = id.IndexOfAny(s_nameDelimiters, index);
            if (delimiterOffset >= 0)
            {
                name = id.Substring(index, delimiterOffset - index);
                index = delimiterOffset;
            }
            else
            {
                name = id.Substring(index);
                index = id.Length;
            }

            return DecodeName(name);
        }

        // undoes dot encodings within names...
        private static string DecodeName(string name)
        {
            return name.Replace('#', '.');
        }

        private static int ReadNextInteger(string id, ref int index)
        {
            int n = 0;

            // note: this can overflow
            while (index < id.Length && char.IsDigit(id[index]))
            {
                n = n * 10 + (id[index] - '0');
                index++;
            }

            return n;
        }
    }
}
