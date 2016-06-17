// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.Serialization
{
    internal static class FastInvokerBuilder
    {
        private static MethodInfo s_buildGetAccessorInternal = typeof(FastInvokerBuilder).GetMethod("BuildGetAccessorInternal", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        private static MethodInfo s_buildSetAccessorInternal = typeof(FastInvokerBuilder).GetMethod("BuildSetAccessorInternal", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        private static MethodInfo s_make = typeof(FastInvokerBuilder).GetMethod("Make", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        public static Func<object> GetMakeNewInstanceFunc(Type type)
        {
            Func<object> make = (Func<object>)s_make.MakeGenericMethod(type).CreateDelegate(typeof(Func<object>));
            return make;
        }

        public static Func<object, object> BuildGetAccessor(PropertyInfo propInfo)
        {
            Func<PropertyInfo, Func<object, object>> buildGetAccessorGeneric = (Func<PropertyInfo, Func<object, object>>)s_buildGetAccessorInternal.MakeGenericMethod(propInfo.DeclaringType, propInfo.PropertyType).CreateDelegate(typeof(Func<PropertyInfo, Func<object, object>>));
            Func<object, object> accessor = buildGetAccessorGeneric(propInfo);
            return accessor;
        }

        public static Action<object, object> BuildSetAccessor(PropertyInfo propInfo)
        {
            Func<PropertyInfo, Action<object, object>> buildSetAccessorGeneric = (Func<PropertyInfo, Action<object, object>>)s_buildSetAccessorInternal.MakeGenericMethod(propInfo.DeclaringType, propInfo.PropertyType).CreateDelegate(typeof(Func<PropertyInfo, Action<object, object>>));
            Action<object, object> accessor = buildSetAccessorGeneric(propInfo);
            return accessor;
        }

        private static object Make<T>() where T : new()
        {
            var t = new T();
            return t;
        }

        private static Func<object, object> BuildGetAccessorInternal<DeclaringType, PropertyType>(PropertyInfo propInfo)
        {
            Func<DeclaringType, PropertyType> getMethod = propInfo.GetMethod.CreateDelegate<Func<DeclaringType, PropertyType>>();

            return (s) =>
            {
                return getMethod((DeclaringType)s);
            };
        }

        private static Action<object, object> BuildSetAccessorInternal<DeclaringType, PropertyType>(PropertyInfo propInfo)
        {
            Action<DeclaringType, PropertyType> setMethod = propInfo.SetMethod.CreateDelegate<Action<DeclaringType, PropertyType>>();

            return (s, t) =>
            {
                setMethod((DeclaringType)s, (PropertyType)t);
            };
        }
    }

    internal static class CreateDelegateExtension
    {
        // a generic extension for CreateDelegate
        public static T CreateDelegate<T>(this MethodInfo method) where T : class
        {
            return method.CreateDelegate(typeof(T)) as T;
        }
    }
}
