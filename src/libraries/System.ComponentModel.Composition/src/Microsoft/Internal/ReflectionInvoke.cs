// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Microsoft.Internal
{
    internal static class ReflectionInvoke
    {
        public static object SafeCreateInstance(this Type type, params object[] arguments)
        {
            return Activator.CreateInstance(type, arguments);
        }

        public static object SafeInvoke(this ConstructorInfo constructor, params object[] arguments)
        {
            return constructor.Invoke(arguments);
        }

        public static object SafeInvoke(this MethodInfo method, object instance, params object[] arguments)
        {
            return method.Invoke(instance, arguments);
        }

        public static object SafeGetValue(this FieldInfo field, object instance)
        {
            return field.GetValue(instance);
        }

        public static void SafeSetValue(this FieldInfo field, object instance, object value)
        {
            field.SetValue(instance, value);
        }
    }
}
