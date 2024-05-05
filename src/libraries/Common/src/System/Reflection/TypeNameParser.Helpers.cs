// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Text;

#nullable enable

#if SYSTEM_PRIVATE_CORELIB
namespace System.Reflection.Metadata
{
    internal struct TypeNameParseOptions
    {
        public TypeNameParseOptions() { }
#pragma warning disable CA1822 // Mark members as static
        // CoreLib does not enforce any limits
        public bool IsMaxDepthExceeded(int _) => false;
        public int MaxNodes
        {
            get
            {
                 Debug.Fail("Expected to be unreachable");
                 return 0;
            }
        }
#pragma warning restore CA1822
    }
}
#endif

namespace System.Reflection
{
    internal partial struct TypeNameParser
    {
#if !MONO // Mono never needs unescaped names
        private const char EscapeCharacter = '\\';

        /// <summary>
        /// Removes escape characters from the string (if there were any found).
        /// </summary>
        private static string UnescapeTypeName(string name)
        {
            int indexOfEscapeCharacter = name.IndexOf(EscapeCharacter);
            if (indexOfEscapeCharacter < 0)
            {
                return name;
            }

            return Unescape(name, indexOfEscapeCharacter);

            static string Unescape(string name, int indexOfEscapeCharacter)
            {
                // this code path is executed very rarely (IL Emit or pure IL with chars not allowed in C# or F#)
                var sb = new ValueStringBuilder(stackalloc char[64]);
                sb.EnsureCapacity(name.Length);
                sb.Append(name.AsSpan(0, indexOfEscapeCharacter));

                for (int i = indexOfEscapeCharacter; i < name.Length;)
                {
                    char c = name[i++];

                    if (c != EscapeCharacter)
                    {
                        sb.Append(c);
                    }
                    else if (i < name.Length && name[i] == EscapeCharacter) // escaped escape character ;)
                    {
                        sb.Append(c);
                        // Consume the escaped escape character, it's important for edge cases
                        // like escaped escape character followed by another escaped char (example: "\\\\\\+")
                        i++;
                    }
                }

                return sb.ToString();
            }
        }
#endif

        private static (string typeNamespace, string name) SplitFullTypeName(string typeName)
        {
            string typeNamespace, name;

            // Matches algorithm from ns::FindSep in src\coreclr\utilcode\namespaceutil.cpp
            // This could result in the type name beginning with a '.' character.
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Used to implement resolving types from strings.")]
        private Type? Resolve(TypeName typeName)
        {
            if (typeName.IsNested)
            {
                TypeName? current = typeName;
                int nestingDepth = 0;
                while (current is not null && current.IsNested)
                {
                    nestingDepth++;
                    current = current.DeclaringType;
                }

                // We're performing real type resolution, it is assumed that the caller has already validated the correctness
                // of this TypeName object against their own policies, so there is no need for this method to perform any further checks.
                string[] nestedTypeNames = new string[nestingDepth];
                current = typeName;
                while (current is not null && current.IsNested)
                {
#if MONO
                    nestedTypeNames[--nestingDepth] = current.Name;
#else // CLR, NativeAOT and tools require unescaped nested type names
                    nestedTypeNames[--nestingDepth] = UnescapeTypeName(current.Name);
#endif
                    current = current.DeclaringType;
                }
#if SYSTEM_PRIVATE_CORELIB
                string nonNestedParentName = current!.FullName;
#else // the tools require unescaped names
                string nonNestedParentName = UnescapeTypeName(current!.FullName);
#endif
                Type? type = GetType(nonNestedParentName, nestedTypeNames, typeName);
                return type is null || !typeName.IsConstructedGenericType ? type : MakeGenericType(type, typeName);
            }
            else if (typeName.IsConstructedGenericType)
            {
                Type? type = Resolve(typeName.GetGenericTypeDefinition());
                return type is null ? null : MakeGenericType(type, typeName);
            }
            else if (typeName.IsArray || typeName.IsPointer || typeName.IsByRef)
            {
                Type? type = Resolve(typeName.GetElementType());
                if (type is null)
                {
                    return null;
                }

                if (typeName.IsByRef)
                {
                    return type.MakeByRefType();
                }
                else if (typeName.IsPointer)
                {
                    return type.MakePointerType();
                }
                else if (typeName.IsSZArray)
                {
                    return type.MakeArrayType();
                }
                else
                {
                    Debug.Assert(typeName.IsVariableBoundArrayType);

                    return type.MakeArrayType(rank: typeName.GetArrayRank());
                }
            }
            else
            {
                Debug.Assert(typeName.IsSimple);

                Type? type = GetType(
#if SYSTEM_PRIVATE_CORELIB
                    typeName.FullName,
#else // the tools require unescaped names
                    UnescapeTypeName(typeName.FullName),
#endif
                    nestedTypeNames: ReadOnlySpan<string>.Empty, typeName);

                return type;
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "Used to implement resolving types from strings.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Used to implement resolving types from strings.")]
        private Type? MakeGenericType(Type type, TypeName typeName)
        {
            var genericArgs = typeName.GetGenericArguments();
#if SYSTEM_PRIVATE_CORELIB
            int size = genericArgs.Count;
#else
            int size = genericArgs.Length;
#endif
            Type[] genericTypes = new Type[size];
            for (int i = 0; i < size; i++)
            {
                Type? genericArg = Resolve(genericArgs[i]);
                if (genericArg is null)
                {
                    return null;
                }
                genericTypes[i] = genericArg;
            }

            return type.MakeGenericType(genericTypes);
        }
    }
}
