// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Text.Json.Reflection
{
    internal static class TypeExtensions
    {
        public static string GetCompilableName(this Type type)
        {
            if (type.IsArray)
            {
                return GetCompilableName(type.GetElementType()) + "[]";
            }

            string compilableName;

            if (!type.IsGenericType)
            {
                compilableName = type.FullName;
            }
            else
            {
                StringBuilder sb = new();

                string fullName = type.FullName;
                int backTickIndex = fullName.IndexOf('`');

                string baseName = fullName.Substring(0, backTickIndex);

                sb.Append(baseName);

                sb.Append('<');

                Type[] genericArgs = type.GetGenericArguments();
                int genericArgCount = genericArgs.Length;
                List<string> genericArgNames = new(genericArgCount);

                for (int i = 0; i < genericArgCount; i++)
                {
                    genericArgNames.Add(GetCompilableName(genericArgs[i]));
                }

                sb.Append(string.Join(", ", genericArgNames));

                sb.Append('>');

                compilableName = sb.ToString();
            }

            compilableName = compilableName.Replace("+", ".");
            return "global::" + compilableName;
        }

        public static string GetTypeInfoPropertyName(this Type type)
        {
            if (type.IsArray)
            {
                return GetTypeInfoPropertyName(type.GetElementType()) + "Array";
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

        public static bool IsNullableValueType(this Type type, out Type? underlyingType)
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

        public static bool CanUseDefaultConstructorForDeserialization(this Type type)
            => (type.GetConstructor(Type.EmptyTypes) != null || type.IsValueType) && !type.IsAbstract && !type.IsInterface;

        public static bool IsObjectType(this Type type) => type.FullName == "System.Object";

        public static bool IsStringType(this Type type) => type.FullName == "System.String";

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
