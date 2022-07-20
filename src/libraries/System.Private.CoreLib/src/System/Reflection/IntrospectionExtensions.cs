// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    public static class IntrospectionExtensions
    {
        public static TypeInfo GetTypeInfo(this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (type is IReflectableType reflectableType)
                return reflectableType.GetTypeInfo();

            return new TypeDelegator(type);
        }
    }
}
