// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System
{
    // Provides methods for making shallow/deep clones of objects.

    public static class ObjectCloner
    {
        // This should only be used for reference types.
        // Simply doing T copied = value will give you the same effect with a value type.
        public static T MemberwiseClone<T>(T obj) where T : class
        {
            Debug.Assert(obj != null);

            // Invoke MemberwiseClone() via reflection to create a shallow copy of the object
            MethodInfo memberwiseClone = typeof(object)
                .GetTypeInfo()
                .GetDeclaredMethod("MemberwiseClone");
            object cloned = memberwiseClone.Invoke(obj, parameters: Array.Empty<object>());

            Debug.Assert(cloned != null && !object.ReferenceEquals(obj, cloned));
            Debug.Assert(obj.GetType() == cloned.GetType());
            return (T)cloned;
        }
    }
}
