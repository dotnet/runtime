// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Reflection
{
    public static partial class RuntimeReflectionExtensions
    {
        private const BindingFlags Everything = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

        public static IEnumerable<FieldInfo> GetRuntimeFields(this Type type)
        {
            if (type == null) 
            {
                throw new ArgumentNullException(nameof(type));
            }
            return type.GetFields(Everything);
        }

        public static IEnumerable<MethodInfo> GetRuntimeMethods(this Type type)
        {
            if (type == null) 
            {
                throw new ArgumentNullException(nameof(type));
            }
            return type.GetMethods(Everything);
        }

        public static IEnumerable<PropertyInfo> GetRuntimeProperties(this Type type)
        {
            if (type == null) 
            {
                throw new ArgumentNullException(nameof(type));
            }
            return type.GetProperties(Everything);
        }

        public static IEnumerable<EventInfo> GetRuntimeEvents(this Type type)
        {
            if (type == null) 
            {
                throw new ArgumentNullException(nameof(type));
            }
            return type.GetEvents(Everything);
        }

        public static FieldInfo GetRuntimeField(this Type type, string name)
        {
            if (type == null) 
            {
                throw new ArgumentNullException(nameof(type));
            }
            return type.GetField(name);
        }

        public static MethodInfo GetRuntimeMethod(this Type type, string name, Type[] parameters)
        {
            if (type == null) 
            {
                throw new ArgumentNullException(nameof(type));
            }
            return type.GetMethod(name, parameters);
        }

        public static PropertyInfo GetRuntimeProperty(this Type type, string name)
        {
            if (type == null) 
            {
                throw new ArgumentNullException(nameof(type));
            }
            return type.GetProperty(name);
        }

        public static EventInfo GetRuntimeEvent(this Type type, string name)
        {
            if (type == null) 
            {
                throw new ArgumentNullException(nameof(type));
            }
            return type.GetEvent(name);
        }

        public static MethodInfo GetRuntimeBaseDefinition(this MethodInfo method)
        {
            if (method == null) 
            {
                throw new ArgumentNullException(nameof(method));
            }
            return method.GetBaseDefinition();
        }

        public static InterfaceMapping GetRuntimeInterfaceMap(this TypeInfo typeInfo, Type interfaceType)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }
            return typeInfo.GetInterfaceMap(interfaceType);
        }

        public static MethodInfo GetMethodInfo(this Delegate del)
        {
            if (del == null) 
            {
                throw new ArgumentNullException(nameof(del));
            }
            return del.Method;
        }
    }
}

