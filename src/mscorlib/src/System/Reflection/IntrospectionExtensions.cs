// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection
{
    public static class IntrospectionExtensions
    {
        public static TypeInfo GetTypeInfo(this Type type)
        {
            IReflectableType reflectableType = type as IReflectableType;
            if (reflectableType != null)
                return reflectableType.GetTypeInfo();

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            // This is bizarre but compatible with the desktop which casts "type" to IReflectableType without checking and
            // thus, throws an InvalidCastException.
            object ignore = (IReflectableType)type;
            Debug.Fail("Did not expect to get here.");
            throw new InvalidOperationException();
        }
    }
}

