// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

namespace System.Reflection
{
    public static class RuntimeReflectionExtensions
    {
        private const BindingFlags everything = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static void CheckAndThrow(Type t)
        {
            if (t == null) throw new ArgumentNullException("type");
            if (!(t is RuntimeType)) throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));
        }

        private static void CheckAndThrow(MethodInfo m)
        {
            if (m == null) throw new ArgumentNullException("method");
            if (!(m is RuntimeMethodInfo)) throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"));
        }

        public static IEnumerable<PropertyInfo> GetRuntimeProperties(this Type type)
        {
            CheckAndThrow(type);
            return type.GetProperties(everything);
        }
        public static IEnumerable<EventInfo> GetRuntimeEvents(this Type type)
        {
            CheckAndThrow(type);
            return type.GetEvents(everything);
        }

        public static IEnumerable<MethodInfo> GetRuntimeMethods(this Type type)
        {
            CheckAndThrow(type);
            return type.GetMethods(everything);
        }

        public static IEnumerable<FieldInfo> GetRuntimeFields(this Type type)
        {
            CheckAndThrow(type);
            return type.GetFields(everything);
        }

        public static PropertyInfo GetRuntimeProperty(this Type type, string name)
        {
             CheckAndThrow(type);
             return type.GetProperty(name);
        }
        public static EventInfo GetRuntimeEvent(this Type type, string name)
        {
            CheckAndThrow(type);
            return type.GetEvent(name);
        }
        public static MethodInfo GetRuntimeMethod(this Type type, string name, Type[] parameters)
        {
            CheckAndThrow(type);
            return type.GetMethod(name, parameters);
        }
        public static FieldInfo GetRuntimeField(this Type type, string name)
        {
            CheckAndThrow(type);
            return type.GetField(name);
        }
        public static MethodInfo GetRuntimeBaseDefinition(this MethodInfo method){
            CheckAndThrow(method);
            return method.GetBaseDefinition();
        }

        public static InterfaceMapping GetRuntimeInterfaceMap(this TypeInfo typeInfo, Type interfaceType)
        {
            if (typeInfo == null) throw new ArgumentNullException("typeInfo");
            if (!(typeInfo is RuntimeType)) throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"));

            return typeInfo.GetInterfaceMap(interfaceType);
        }

        public static MethodInfo GetMethodInfo(this Delegate del)
        {
            if (del == null) throw new ArgumentNullException("del");

            return del.Method;
        }
    }
}
