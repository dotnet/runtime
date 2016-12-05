// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;

namespace System.Linq
{
    internal static class TypeHelper
    {
        internal static Type FindGenericType(Type definition, Type type)
        {
            bool? definitionIsInterface = null;
            while (type != null && type != typeof(object))
            {
                TypeInfo typeInfo = type.GetTypeInfo();
                if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == definition)
                    return type;
                if (!definitionIsInterface.HasValue)
                    definitionIsInterface = definition.GetTypeInfo().IsInterface;
                if (definitionIsInterface.GetValueOrDefault())
                {
                    foreach (Type itype in typeInfo.ImplementedInterfaces)
                    {
                        Type found = FindGenericType(definition, itype);
                        if (found != null)
                            return found;
                    }
                }
                type = type.GetTypeInfo().BaseType;
            }
            return null;
        }

        internal static IEnumerable<MethodInfo> GetStaticMethods(this Type type)
        {
            return type.GetRuntimeMethods().Where(m => m.IsStatic);
        }
    }
}
