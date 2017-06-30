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

        private delegate void StructSetDelegate<T, TArg>(ref T obj, TArg value);
        private delegate TResult StructGetDelegate<T, out TResult>(ref T obj);

        private static MethodInfo s_createGetterInternal = typeof(FastInvokerBuilder).GetMethod(nameof(CreateGetterInternal), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        private static MethodInfo s_createSetterInternal = typeof(FastInvokerBuilder).GetMethod(nameof(CreateSetterInternal), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        private static MethodInfo s_make = typeof(FastInvokerBuilder).GetMethod(nameof(Make), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        public static Func<object> GetMakeNewInstanceFunc(Type type)
        {
            Func<object> make = s_make.MakeGenericMethod(type).CreateDelegate<Func<object>>();
            return make;
        }

        public static Getter CreateGetter(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
            {
                var propInfo = (PropertyInfo)memberInfo;
                var createGetterGeneric = s_createGetterInternal.MakeGenericMethod(propInfo.DeclaringType, propInfo.PropertyType).CreateDelegate<Func<PropertyInfo, Getter>>();
                Getter accessor = createGetterGeneric(propInfo);
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
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.InvalidMember, DataContract.GetClrTypeFullName(memberInfo.DeclaringType), memberInfo.Name)));
            }        
        }

        public static Setter CreateSetter(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
            {
                PropertyInfo propInfo = (PropertyInfo)memberInfo;
                if (propInfo.CanWrite)
                {
                    var buildSetAccessorGeneric = s_createSetterInternal.MakeGenericMethod(propInfo.DeclaringType, propInfo.PropertyType).CreateDelegate<Func<PropertyInfo, Setter>>();
                    Setter accessor = buildSetAccessorGeneric(propInfo);
                    return accessor;
                }
                else
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.NoSetMethodForProperty, propInfo.DeclaringType, propInfo.Name)));
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
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.InvalidMember, DataContract.GetClrTypeFullName(memberInfo.DeclaringType), memberInfo.Name)));
            }
        }

        private static object Make<T>() where T : new()
        {
            var t = new T();
            return t;
        }

        private static Getter CreateGetterInternal<DeclaringType, PropertyType>(PropertyInfo propInfo)
        {
            if (typeof(DeclaringType).IsGenericType && typeof(DeclaringType).GetGenericTypeDefinition() == typeof(KeyValue<,>))
            {
                if (propInfo.Name == "Key")
                {
                    return (obj) =>
                    {
                        return ((IKeyValue)obj).Key;
                    };
                }
                else
                {
                    return (obj) =>
                    {
                        return ((IKeyValue)obj).Value;
                    };
                }
            }

            if (typeof(DeclaringType).IsValueType)
            {
                var getMethod = propInfo.GetMethod.CreateDelegate<StructGetDelegate<DeclaringType, PropertyType>>();

                return (obj) =>
                {
                    var unboxed = (DeclaringType)obj;
                    return getMethod(ref unboxed);
                };
            }
            else
            {
                var getMethod = propInfo.GetMethod.CreateDelegate<Func<DeclaringType, PropertyType>>();

                return (obj) =>
                {
                    return getMethod((DeclaringType)obj);
                };
            }
        }

        private static Setter CreateSetterInternal<DeclaringType, PropertyType>(PropertyInfo propInfo)
        {
            if(typeof(DeclaringType).IsGenericType && typeof(DeclaringType).GetGenericTypeDefinition() == typeof(KeyValue<,>))
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

            if (typeof(DeclaringType).IsValueType)
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
                var setMethod = propInfo.SetMethod.CreateDelegate<Action<DeclaringType, PropertyType>>();

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
            try
            {
                return method.CreateDelegate(typeof(T)) as T;
            }
            catch(Exception e)
            {
                throw new InvalidOperationException(SR.Format(SR.FailedToCreateMethodDelegate, method.Name, method.DeclaringType.FullName), e);
            }
        }
    }
}
