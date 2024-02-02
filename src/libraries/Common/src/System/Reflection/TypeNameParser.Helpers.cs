// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;

#nullable enable

namespace System.Reflection
{
    internal partial struct TypeNameParser
    {
#if NETCOREAPP
        private static ReadOnlySpan<char> CharsToEscape => "\\[]+*&,";

        private static bool NeedsEscapingInTypeName(char c)
            => CharsToEscape.Contains(c);
#else
        private static char[] CharsToEscape { get; } = "\\[]+*&,".ToCharArray();

        private static bool NeedsEscapingInTypeName(char c)
            => Array.IndexOf(CharsToEscape, c) >= 0;
#endif

        private static string EscapeTypeName(string name)
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

        private static string EscapeTypeName(string typeName, ReadOnlySpan<string> nestedTypeNames)
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
                string nonNestedParentName = current!.Name;

                Type? type = GetType(nonNestedParentName, nestedTypeNames, typeName.AssemblyName);
                return Make(type, typeName);
            }
            else if (typeName.UnderlyingType is null)
            {
                Type? type = GetType(typeName.Name, nestedTypeNames: ReadOnlySpan<string>.Empty, typeName.AssemblyName);

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
                Metadata.TypeName[] genericArgs = typeName.GetGenericArguments();
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
