// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.Serialization
{
    internal static class FastInvokerBuilder
    {
        public delegate void Setter(ref object obj, object? value);
        public delegate object? Getter(object obj);

        private delegate void StructSetDelegate<T, TArg>(ref T obj, TArg value);
        private delegate TResult StructGetDelegate<T, out TResult>(ref T obj);

        private static readonly MethodInfo s_createGetterInternal = typeof(FastInvokerBuilder).GetMethod(nameof(CreateGetterInternal), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!;
        private static readonly MethodInfo s_createSetterInternal = typeof(FastInvokerBuilder).GetMethod(nameof(CreateSetterInternal), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!;
        private static readonly MethodInfo s_make = typeof(FastInvokerBuilder).GetMethod(nameof(Make), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "The call to MakeGenericMethod is safe due to the fact that we are preserving the constructors of type which is what Make() is doing.")]
        public static Func<object> GetMakeNewInstanceFunc(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            Type type)
        {
            Func<object> make = s_make.MakeGenericMethod(type).CreateDelegate<Func<object>>();
            return make;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "The call to MakeGenericMethod is safe due to the fact that FastInvokerBuilder.CreateGetterInternal<T, T1> is not annotated.")]
        public static Getter CreateGetter(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo propInfo)
            {
                Type declaringType = propInfo.DeclaringType!;
                Type propertyType = propInfo.PropertyType!;

                if (declaringType.IsGenericType && declaringType.GetGenericTypeDefinition() == typeof(KeyValue<,>))
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

                // If either of the arguments to MakeGenericMethod is a valuetype, this is going to cause JITting.
                // Only JIT if dynamic code is supported.
                if (RuntimeFeature.IsDynamicCodeSupported || (!declaringType.IsValueType && !propertyType.IsValueType))
                {
                    var createGetterGeneric = s_createGetterInternal.MakeGenericMethod(declaringType, propertyType).CreateDelegate<Func<PropertyInfo, Getter>>();
                    return createGetterGeneric(propInfo);
                }
                else
                {
                    return (obj) =>
                    {
                        return propInfo.GetValue(obj);
                    };
                }
            }
            else if (memberInfo is FieldInfo fieldInfo)
            {
                return (obj) =>
                {
                    var value = fieldInfo.GetValue(obj);
                    return value;
                };
            }
            else
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.InvalidMember, DataContract.GetClrTypeFullName(memberInfo.DeclaringType!), memberInfo.Name)));
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "The call to MakeGenericMethod is safe due to the fact that FastInvokerBuilder.CreateSetterInternal<T, T1> is not annotated.")]
        public static Setter CreateSetter(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
            {
                PropertyInfo propInfo = (PropertyInfo)memberInfo;
                if (propInfo.CanWrite)
                {
                    Type declaringType = propInfo.DeclaringType!;
                    Type propertyType = propInfo.PropertyType!;

                    if (declaringType.IsGenericType && declaringType.GetGenericTypeDefinition() == typeof(KeyValue<,>))
                    {
                        if (propInfo.Name == "Key")
                        {
                            return (ref object obj, object? val) =>
                            {
                                ((IKeyValue)obj).Key = val;
                            };
                        }
                        else
                        {
                            return (ref object obj, object? val) =>
                            {
                                ((IKeyValue)obj).Value = val;
                            };
                        }
                    }

                    // If either of the arguments to MakeGenericMethod is a valuetype, this is going to cause JITting.
                    // Only JIT if dynamic code is supported.
                    if (RuntimeFeature.IsDynamicCodeSupported || (!declaringType.IsValueType && !propertyType.IsValueType))
                    {
                        var createSetterGeneric = s_createSetterInternal.MakeGenericMethod(propInfo.DeclaringType!, propInfo.PropertyType).CreateDelegate<Func<PropertyInfo, Setter>>();
                        return createSetterGeneric(propInfo);
                    }
                    else
                    {
                        return (ref object obj, object? val) =>
                        {
                            propInfo.SetValue(obj, val);
                        };
                    }
                }
                else
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.NoSetMethodForProperty, propInfo.DeclaringType, propInfo.Name)));
                }
            }
            else if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                return (ref object obj, object? val) =>
                {
                    fieldInfo.SetValue(obj, val);
                };
            }
            else
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.Format(SR.InvalidMember, DataContract.GetClrTypeFullName(memberInfo.DeclaringType!), memberInfo.Name)));
            }
        }

        private static object Make<T>() where T : new()
        {
            var t = new T();
            return t;
        }

        private static Getter CreateGetterInternal<DeclaringType, PropertyType>(PropertyInfo propInfo)
        {
            if (typeof(DeclaringType).IsValueType)
            {
                var getMethod = propInfo.GetMethod!.CreateDelegate<StructGetDelegate<DeclaringType, PropertyType>>();

                return (obj) =>
                {
                    var unboxed = (DeclaringType)obj;
                    return getMethod(ref unboxed);
                };
            }
            else
            {
                var getMethod = propInfo.GetMethod!.CreateDelegate<Func<DeclaringType, PropertyType>>();

                return (obj) =>
                {
                    return getMethod((DeclaringType)obj);
                };
            }
        }

        private static Setter CreateSetterInternal<DeclaringType, PropertyType>(PropertyInfo propInfo)
        {
            if (typeof(DeclaringType).IsValueType)
            {
                var setMethod = propInfo.SetMethod!.CreateDelegate<StructSetDelegate<DeclaringType, PropertyType>>();

                return (ref object obj, object? val) =>
                {
                    var unboxed = (DeclaringType)obj;
                    setMethod(ref unboxed, (PropertyType)val!);
                    obj = unboxed!;
                };
            }
            else
            {
                var setMethod = propInfo.SetMethod!.CreateDelegate<Action<DeclaringType, PropertyType>>();

                return (ref object obj, object? val) =>
                {
                    setMethod((DeclaringType)obj, (PropertyType)val!);
                };
            }
        }
    }
}
