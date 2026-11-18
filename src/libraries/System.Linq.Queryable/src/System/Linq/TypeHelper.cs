// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Linq
{
    internal static class TypeHelper
    {
        internal static Type? FindGenericType(Type definition, Type type)
        {
            bool? definitionIsInterface = null;
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == definition)
                    return type;
                if (!definitionIsInterface.HasValue)
                    definitionIsInterface = definition.IsInterface;
                if (definitionIsInterface.GetValueOrDefault())
                {
                    Type[] interfaces = type.GetInterfacesWithSameMetadataDefinitionAs(definition);
                    return interfaces.Length > 0 ? interfaces[0] : null;
                }
                type = type.BaseType!;
            }
            return null;
        }
    }
}
