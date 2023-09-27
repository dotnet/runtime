// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Reflection
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IntrospectionExtensions
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static TypeInfo GetTypeInfo(this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (type is IReflectableType reflectableType)
                return reflectableType.GetTypeInfo();

            return new TypeDelegator(type);
        }
    }
}
