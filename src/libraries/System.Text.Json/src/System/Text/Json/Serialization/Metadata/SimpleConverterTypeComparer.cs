// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Metadata
{
    internal sealed class SimpleConverterTypeComparer : IEqualityComparer<Type>
    {
        public bool Equals(Type? x, Type? y)
        {
            Debug.Assert(x != null && y != null);

            if (x == y)
            {
                return true;
            }

            return false;
        }

        public int GetHashCode(Type obj)
        {
            Type? baseType;

            baseType = obj.BaseType;

            if (baseType?.IsGenericType == true)
            {
                Type[] genericArgs = baseType.GetGenericArguments();
                Type genericArgument = genericArgs[0];

                return genericArgument.GetHashCode();
            }

            return obj.GetHashCode();
        }
    }
}
