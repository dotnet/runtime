// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    internal static class PrivateReflectionHelpers
    {
        public static FieldInfo GetFieldAssert(this Type type, string name)
        {
            FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            Debug.Assert(field != null, $"Expected field '{name}' on type '{type.Name}'.");
            return field;
        }

        public static MethodInfo GetMethodAssert(this Type type, string name)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            Debug.Assert(method != null, $"Expected method '{name}' on type '{type.Name}'.");
            return method;
        }

        public static PropertyInfo GetPropertyAssert(this Type type, string name)
        {
            PropertyInfo property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            Debug.Assert(property != null, $"Expected property '{name}' on type '{type.Name}'.");
            return property;
        }
    }
}
