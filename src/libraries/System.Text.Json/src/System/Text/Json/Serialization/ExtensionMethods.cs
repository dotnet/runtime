// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json
{
    internal static class ExtensionMethods
    {
        internal static Type? GetCompatibleGenericBaseClass(this Type type, Type baseType)
        {
            Debug.Assert(baseType.IsGenericType);
            Debug.Assert(!baseType.IsInterface);

            baseType = baseType.GetGenericTypeDefinition();

            Type baseTypeToCheck = type;

            while (baseTypeToCheck != null && baseTypeToCheck != typeof(object))
            {
                if (baseTypeToCheck.IsGenericType)
                {
                    Type genericTypeToCheck = baseTypeToCheck.GetGenericTypeDefinition();
                    if (genericTypeToCheck == baseType)
                    {
                        return baseTypeToCheck;
                    }
                }

                baseTypeToCheck = baseTypeToCheck.BaseType!;
            }

            return null;
        }

        internal static Type? GetCompatibleGenericInterface(this Type type, Type interfaceType)
        {
            Debug.Assert(interfaceType.IsGenericType);
            Debug.Assert(interfaceType.IsInterface);

            interfaceType = interfaceType.GetGenericTypeDefinition();

            Type interfaceToCheck = type;

            if (interfaceToCheck.IsGenericType)
            {
                interfaceToCheck = interfaceToCheck.GetGenericTypeDefinition();
            }

            if (interfaceToCheck == interfaceType)
            {
                return type;
            }

            foreach (Type typeToCheck in type.GetInterfaces())
            {
                if (typeToCheck.IsGenericType)
                {
                    Type genericInterfaceToCheck = typeToCheck.GetGenericTypeDefinition();
                    if (genericInterfaceToCheck == interfaceType)
                    {
                        return typeToCheck;
                    }
                }
            }

            return null;
        }
    }
}
