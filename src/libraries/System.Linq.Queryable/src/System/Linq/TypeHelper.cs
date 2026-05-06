// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    internal static class TypeHelper
    {
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:RequiresUnreferencedCode",
            Justification = "GetInterfaces is only called if 'definition' is interface type. " +
                "In that case though the interface must be present (otherwise the Type of it could not exist) " +
                "which also means that the trimmer kept the interface and thus kept it on all types " +
                "which implement it. It doesn't matter if the GetInterfaces call below returns fewer types" +
                "as long as it returns the 'definition' as well.")]
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
                    foreach (Type itype in type.GetInterfaces())
                    {
                        Type? found = FindGenericType(definition, itype);
                        if (found != null)
                            return found;
                    }
                }
                type = type.BaseType!;
            }
            return null;
        }
    }
}
