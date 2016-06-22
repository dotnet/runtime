// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.Serialization
{
    internal static class FastInvokerBuilder
    {
        public delegate void Setter(ref object obj, object value);
        public delegate object Getter(object obj);

        private delegate void StructSetDelegate<T, T1>(ref T obj, T1 value);

        private static MethodInfo s_buildGetAccessorInternal = typeof(FastInvokerBuilder).GetMethod(nameof(BuildGetAccessorInternal), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        private static MethodInfo s_buildSetAccessorInternal = typeof(FastInvokerBuilder).GetMethod(nameof(BuildSetAccessorInternal), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        private static MethodInfo s_make = typeof(FastInvokerBuilder).GetMethod(nameof(Make), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        public static Func<object> GetMakeNewInstanceFunc(Type type)
        {
            Func<object> make = (Func<object>)s_make.MakeGenericMethod(type).CreateDelegate(typeof(Func<object>));
            return make;
        }

        public static Getter BuildGetAccessor(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
            {
                var propInfo = (PropertyInfo)memberInfo;
                Func<PropertyInfo, Getter> buildGetAccessorGeneric = (Func<PropertyInfo, Getter>)s_buildGetAccessorInternal.MakeGenericMethod(propInfo.DeclaringType, propInfo.PropertyType).CreateDelegate(typeof(Func<PropertyInfo, Getter>));
                Getter accessor = buildGetAccessorGeneric(propInfo);
                return accessor;
            }
            else if (memberInfo is FieldInfo)
            {
                return (obj) =>
                {
                    FieldInfo fieldInfo = (FieldInfo)memberInfo;
                    var value = fieldInfo.GetValue(obj);
                    return value;
                };
            }
            else
            {
                throw new InvalidOperationException($"The type, {memberInfo.GetType()}, of memberInfo is not supported.");
            }        
        }

        public static Setter BuildSetAccessor(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
            {
                PropertyInfo propInfo = (PropertyInfo)memberInfo;
                if (propInfo.CanWrite)
                {
                    Func<PropertyInfo, Setter> buildSetAccessorGeneric = (Func<PropertyInfo, Setter>)s_buildSetAccessorInternal.MakeGenericMethod(propInfo.DeclaringType, propInfo.PropertyType).CreateDelegate(typeof(Func<PropertyInfo, Setter>));
                    Setter accessor = buildSetAccessorGeneric(propInfo);
                    return accessor;
                }
                else
                {
                    throw new InvalidOperationException($"Property {propInfo.Name} of type {DataContract.GetClrTypeFullName(propInfo.DeclaringType)} cannot be set.");
                }
            }
            else if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                return (ref object obj, object val) =>
                {
                    fieldInfo.SetValue(obj, val);
                };
            }
            else
            {
                throw new NotImplementedException("Unknown member type");
            }
        }

        private static object Make<T>() where T : new()
        {
            var t = new T();
            return t;
        }

        private static Getter BuildGetAccessorInternal<DeclaringType, PropertyType>(PropertyInfo propInfo)
        {
            Func<DeclaringType, PropertyType> getMethod = propInfo.GetMethod.CreateDelegate<Func<DeclaringType, PropertyType>>();

            return (obj) =>
            {
                return getMethod((DeclaringType)obj);
            };
        }

        private static Setter BuildSetAccessorInternal<DeclaringType, PropertyType>(PropertyInfo propInfo)
        {
            if(typeof(DeclaringType).GetTypeInfo().IsGenericType && typeof(DeclaringType).GetGenericTypeDefinition() == typeof(KeyValue<,>))
            {
                if(propInfo.Name == "Key")
                {
                    return (ref object obj, object val) =>
                    {
                        ((IKeyValue)obj).Key = val;
                    };
                }
                else
                {
                    return (ref object obj, object val) =>
                    {
                        ((IKeyValue)obj).Value = val;
                    };
                }
            }

            if (typeof(DeclaringType).GetTypeInfo().IsValueType)
            {
                var setMethod = propInfo.SetMethod.CreateDelegate<StructSetDelegate<DeclaringType, PropertyType>>();

                return (ref object obj, object val) =>
                {
                    var unboxed = (DeclaringType)obj;
                    setMethod(ref unboxed, (PropertyType)val);
                    obj = unboxed;
                };
            }
            else
            {
                Action<DeclaringType, PropertyType> setMethod = propInfo.SetMethod.CreateDelegate<Action<DeclaringType, PropertyType>>();

                return (ref object obj, object val) =>
                {
                    setMethod((DeclaringType)obj, (PropertyType)val);
                };
            }
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
