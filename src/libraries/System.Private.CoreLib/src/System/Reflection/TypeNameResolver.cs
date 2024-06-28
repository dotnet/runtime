// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Text;

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

namespace System.Reflection
{
    internal partial struct TypeNameResolver
    {
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Used to implement resolving types from strings.")]
        private Type? Resolve(TypeName typeName)
        {
            if (typeName.IsSimple)
            {
                return GetSimpleType(typeName);
            }
            else if (typeName.IsConstructedGenericType)
            {
                return GetGenericType(typeName);
            }
            else if (typeName.IsArray || typeName.IsPointer || typeName.IsByRef)
            {
                Type? type = Resolve(typeName.GetElementType());
                if (type is null)
                    return null;

                if (typeName.IsArray)
                {
                    return typeName.IsSZArray ? type.MakeArrayType() : type.MakeArrayType(rank: typeName.GetArrayRank());
                }
                if (typeName.IsByRef)
                {
                    return type.MakeByRefType();
                }
                else if (typeName.IsPointer)
                {
                    return type.MakePointerType();
                }
            }

            Debug.Fail("Expected to be unreachable");
            return null;
        }

        private Type? GetSimpleType(TypeName typeName)
        {
            if (typeName.IsNested)
            {
                TypeName current = typeName;
                int nestingDepth = 0;
                do
                {
                    nestingDepth++;
                    current = current.DeclaringType!;
                }
                while (current.IsNested);

                string[] nestedTypeNames = new string[nestingDepth];
                current = typeName;
                while (current.IsNested)
                {
                    nestedTypeNames[--nestingDepth] = TypeNameHelpers.Unescape(current.Name);
                    current = current.DeclaringType!;
                }

                return GetType(current.FullName, nestedTypeNames, typeName);
            }
            else
            {
                return GetType(typeName.FullName, default, typeName);
            }

        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:UnrecognizedReflectionPattern",
            Justification = "Used to implement resolving types from strings.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Used to implement resolving types from strings.")]
        private Type? GetGenericType(TypeName typeName)
        {
            Type? type = Resolve(typeName.GetGenericTypeDefinition());
            if (type is null)
                return null;

            ReadOnlySpan<TypeName> genericArgs = typeName.GetGenericArguments();
            Type[] genericTypes = new Type[genericArgs.Length];
            for (int i = 0; i < genericArgs.Length; i++)
            {
                Type? genericArg = Resolve(genericArgs[i]);
                if (genericArg is null)
                    return null;
                genericTypes[i] = genericArg;
            }

            return type.MakeGenericType(genericTypes);
        }
    }
}
