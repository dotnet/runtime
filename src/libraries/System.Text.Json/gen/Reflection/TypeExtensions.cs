// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Text.Json.Reflection
{
    internal static class TypeExtensions
    {
        public static string GetCompilableName(this Type type)
        {
            if (type.IsArray)
            {
                int rank = type.GetArrayRank();
                return GetCompilableName(type.GetElementType()!) + TypeWrapper.FormatArrayTypeNameSuffix(rank);
            }

            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            StringBuilder sb = new();

            sb.Append("global::");

            string? @namespace = type.Namespace;
            if (!string.IsNullOrEmpty(@namespace) && @namespace != JsonConstants.GlobalNamespaceValue)
            {
                sb.Append(@namespace);
                sb.Append('.');
            }

            int argumentIndex = 0;
            AppendTypeChain(sb, type, type.GetGenericArguments(), ref argumentIndex);

            return sb.ToString();

            static void AppendTypeChain(StringBuilder sb, Type type, Type[] genericArguments, ref int argumentIndex)
            {
                Type? declaringType = type.DeclaringType;
                if (declaringType != null)
                {
                    AppendTypeChain(sb, declaringType, genericArguments, ref argumentIndex);
                    sb.Append('.');
                }
                int backTickIndex = type.Name.IndexOf('`');
                if (backTickIndex == -1)
                {
                    sb.Append(type.Name);
                }
                else
                {
                    sb.Append(type.Name, 0, backTickIndex);

                    sb.Append('<');

                    int startIndex = argumentIndex;
                    argumentIndex = type.GetGenericArguments().Length;
                    for (int i = startIndex; i < argumentIndex; i++)
                    {
                        if (i != startIndex)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(GetCompilableName(genericArguments[i]));
                    }

                    sb.Append('>');
                }
            }
        }

        public static string GetTypeInfoPropertyName(this Type type)
        {
            if (type.IsArray)
            {
                int rank = type.GetArrayRank();
                string suffix = rank == 1 ? "Array" : $"Array{rank}D"; // Array, Array2D, Array3D, ...
                return GetTypeInfoPropertyName(type.GetElementType()!) + suffix;
            }
            else if (!type.IsGenericType)
            {
                return type.Name;
            }

            StringBuilder sb = new();

            string name = ((TypeWrapper)type).SimpleName;

            sb.Append(name);

            foreach (Type genericArg in type.GetGenericArguments())
            {
                sb.Append(GetTypeInfoPropertyName(genericArg));
            }

            return sb.ToString();
        }

        public static bool IsNullableValueType(this Type type, Type nullableOfTType, out Type? underlyingType)
        {
            Debug.Assert(nullableOfTType != null);

            // TODO: log bug because Nullable.GetUnderlyingType doesn't work due to
            // https://github.com/dotnet/runtimelab/blob/7472c863db6ec5ddab7f411ddb134a6e9f3c105f/src/libraries/System.Private.CoreLib/src/System/Nullable.cs#L124
            // i.e. type.GetGenericTypeDefinition() will never equal typeof(Nullable<>), as expected in that code segment.
            if (type.IsGenericType && type.GetGenericTypeDefinition() == nullableOfTType)
            {
                underlyingType = type.GetGenericArguments()[0];
                return true;
            }

            underlyingType = null;
            return false;
        }

        public static bool IsNullableValueType(this Type type, [NotNullWhen(true)] out Type? underlyingType)
        {
            if (type.IsGenericType && type.Name.StartsWith("Nullable`1"))
            {
                underlyingType = type.GetGenericArguments()[0];
                return true;
            }

            underlyingType = null;
            return false;
        }

        public static bool CanContainNullableReferenceTypeAnnotations(this Type type)
        {
            // Returns true iff Type instance has potential for receiving nullable reference type annotations,
            // i.e. the type is a reference type or contains generic parameters that are reference types.

            if (!type.IsValueType)
            {
                return true;
            }

            if (type.IsGenericType)
            {
                foreach (Type genericParam in type.GetGenericArguments())
                {
                    if (CanContainNullableReferenceTypeAnnotations(genericParam))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool CanUseDefaultConstructorForDeserialization(this Type type, out ConstructorInfo? constructorInfo)
        {
            constructorInfo = type.GetConstructor(Type.EmptyTypes);

            if ((constructorInfo != null || type.IsValueType) && !type.IsAbstract && !type.IsInterface)
            {
                return true;
            }

            constructorInfo = null;
            return false;
        }

        public static Type? GetCompatibleBaseClass(this Type type, string baseTypeFullName)
        {
            Type? baseTypeToCheck = type;

            while (baseTypeToCheck != null && baseTypeToCheck != typeof(object))
            {
                if (baseTypeToCheck.FullName == baseTypeFullName)
                {
                    return baseTypeToCheck;
                }

                baseTypeToCheck = baseTypeToCheck.BaseType;
            }

            return null;
        }
    }
}
