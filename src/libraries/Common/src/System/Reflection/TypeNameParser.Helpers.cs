// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;

#nullable enable

namespace System.Reflection
{
    internal partial struct TypeNameParser
    {
        private const char EscapeCharacter = '\\';

        private static string UnescapeTypeName(string name)
        {
            int indexOfEscapeCharacter = name.IndexOf(EscapeCharacter);
            if (indexOfEscapeCharacter < 0)
            {
                return name;
            }

            // this code path is executed very rarely (IL Emit or pure IL with chars not allowed in C# or F#)
            var sb = new ValueStringBuilder(stackalloc char[64]);
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

        /// <summary>
        /// Returns non-null array when some unescaping of nested names was required.
        /// </summary>
        private static string[]? UnescapeTypeNames(ReadOnlySpan<string> names)
        {
            if (names.IsEmpty) // nothing to check
            {
                return null;
            }

            int i = 0;
            for (; i < names.Length; i++)
            {
#if NETCOREAPP
                if (names[i].Contains(EscapeCharacter))
#else
                if (names[i].IndexOf(EscapeCharacter) >= 0)
#endif
                {
                    break;
                }
            }

            if (i == names.Length) // nothing to escape
            {
                return null;
            }

            string[] unescapedNames = new string[names.Length];
            for (int j = 0; j < i; j++)
            {
                unescapedNames[j] = names[j]; // copy what not needed escaping
            }
            for (; i < names.Length; i++)
            {
                unescapedNames[i] = UnescapeTypeName(names[i]); // escape the rest
            }
            return unescapedNames;
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

        private Type? Resolve(Metadata.TypeName typeName)
        {
            if (typeName.IsNestedType)
            {
                Metadata.TypeName? current = typeName;
                int nestingDepth = 0;
                while (current is not null && current.IsNestedType)
                {
                    nestingDepth++;
                    current = current.ContainingType;
                }

                string[] nestedTypeNames = new string[nestingDepth];
                current = typeName;
                while (current is not null && current.IsNestedType)
                {
                    nestedTypeNames[--nestingDepth] = current.Name;
                    current = current.ContainingType;
                }
                string nonNestedParentName = current!.FullName;

                Type? type = GetType(nonNestedParentName, nestedTypeNames, typeName.GetAssemblyName(), typeName.FullName);
                return Make(type, typeName);
            }
            else if (typeName.UnderlyingType is null)
            {
                Type? type = GetType(typeName.FullName, nestedTypeNames: ReadOnlySpan<string>.Empty, typeName.GetAssemblyName(), typeName.FullName);

                return Make(type, typeName);
            }

            return Make(Resolve(typeName.UnderlyingType), typeName);
        }

#if !NETSTANDARD2_0 // needed for ILVerification project
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "Used to implement resolving types from strings.")]
#endif
        private Type? Make(Type? type, Metadata.TypeName typeName)
        {
            if (type is null || typeName.IsElementalType)
            {
                return type;
            }
            else if (typeName.IsConstructedGenericType)
            {
                ReadOnlySpan<Metadata.TypeName> genericArgs = typeName.GetGenericArguments();
                Type[] genericTypes = new Type[genericArgs.Length];
                for (int i = 0; i < genericArgs.Length; i++)
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
            else if (typeName.IsManagedPointerType)
            {
                return type.MakeByRefType();
            }
            else if (typeName.IsUnmanagedPointerType)
            {
                return type.MakePointerType();
            }
            else if (typeName.IsSzArrayType)
            {
                return type.MakeArrayType();
            }
            else
            {
                return type.MakeArrayType(rank: typeName.GetArrayRank());
            }
        }
    }
}
