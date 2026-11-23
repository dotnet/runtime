// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.DataContracts;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.Serialization
{
    internal static class FastInvokerBuilder
    {
        public delegate object Setter(object obj, object? value);
        public delegate object? Getter(object obj);

        private delegate void StructSetDelegate<T, TArg>(ref T obj, TArg value);
        private delegate TResult StructGetDelegate<T, out TResult>(ref T obj);

        private static readonly MethodInfo s_createGetterInternal = typeof(FastInvokerBuilder).GetMethod(nameof(CreateGetterInternal), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!;
        private static readonly MethodInfo s_createSetterInternal = typeof(FastInvokerBuilder).GetMethod(nameof(CreateSetterInternal), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!;
        private static readonly MethodInfo s_make = typeof(FastInvokerBuilder).GetMethod(nameof(Make), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)!;

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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
#pragma warning disable IL3050 // AOT compiling should recognize that this call is gated by RuntimeFeature.IsDynamicCodeSupported.
                    var createGetterGeneric = s_createGetterInternal.MakeGenericMethod(declaringType, propertyType).CreateDelegate<Func<PropertyInfo, Getter>>();
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                    return createGetterGeneric(propInfo);
                }
                else
                {
                    return propInfo.GetValue;
                }
            }
            else if (memberInfo is FieldInfo fieldInfo)
            {
                if (CanCompileFieldAccess(fieldInfo))
                {
                    ParameterExpression obj = Expression.Parameter(typeof(object), nameof(obj));
                    return Expression.Lambda<Getter>(
                        Expression.Convert(CreateFieldExpression(fieldInfo, obj), typeof(object)), obj).Compile();
                }

                return fieldInfo.GetValue;
            }
            else
            {
                throw new InvalidOperationException(SR.Format(SR.InvalidMember, DataContract.GetClrTypeFullName(memberInfo.DeclaringType!), memberInfo.Name));
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "The call to MakeGenericMethod is safe due to the fact that FastInvokerBuilder.CreateSetterInternal<T, T1> is not annotated.")]
        public static Setter CreateSetter(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo propInfo)
            {
                if (propInfo.CanWrite)
                {
                    Type declaringType = propInfo.DeclaringType!;
                    Type propertyType = propInfo.PropertyType!;

                    if (declaringType.IsGenericType && declaringType.GetGenericTypeDefinition() == typeof(KeyValue<,>))
                    {
                        if (propInfo.Name == "Key")
                        {
                            return (obj, val) =>
                            {
                                ((IKeyValue)obj).Key = val;
                                return obj;
                            };
                        }
                        else
                        {
                            return (obj, val) =>
                            {
                                ((IKeyValue)obj).Value = val;
                                return obj;
                            };
                        }
                    }

                    if (RuntimeFeature.IsDynamicCodeCompiled && declaringType.IsValueType)
                    {
                        ParameterExpression obj = Expression.Parameter(typeof(object), nameof(obj));
                        ParameterExpression value = Expression.Parameter(typeof(object), nameof(value));
                        return Expression.Lambda<Setter>(
                            Expression.Block(
                                Expression.Call(Expression.Unbox(obj, declaringType), propInfo.SetMethod!, Expression.Convert(value, propertyType)),
                                obj),
                            obj, value).Compile();
                    }
                    // If either of the arguments to MakeGenericMethod is a valuetype, this is going to cause JITting.
                    // Only JIT if dynamic code is supported.
                    if (RuntimeFeature.IsDynamicCodeSupported || (!declaringType.IsValueType && !propertyType.IsValueType))
                    {
#pragma warning disable IL3050 // AOT compiling should recognize that this call is gated by RuntimeFeature.IsDynamicCodeSupported.
                        var createSetterGeneric = s_createSetterInternal.MakeGenericMethod(propInfo.DeclaringType!, propInfo.PropertyType).CreateDelegate<Func<PropertyInfo, Setter>>();
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
                        return createSetterGeneric(propInfo);
                    }
                    else
                    {
                        return (obj, val) =>
                        {
                            propInfo.SetValue(obj, val);
                            return obj;
                        };
                    }
                }
                else
                {
                    throw new InvalidOperationException(SR.Format(SR.NoSetMethodForProperty, propInfo.DeclaringType, propInfo.Name));
                }
            }
            else if (memberInfo is FieldInfo fieldInfo)
            {
                if (!fieldInfo.IsInitOnly && !fieldInfo.IsLiteral && CanCompileFieldAccess(fieldInfo))
                {
                    ParameterExpression obj = Expression.Parameter(typeof(object), nameof(obj));
                    ParameterExpression value = Expression.Parameter(typeof(object), nameof(value));
                    return Expression.Lambda<Setter>(
                        Expression.Block(
                            Expression.Assign(CreateFieldExpression(fieldInfo, obj), Expression.Convert(value, fieldInfo.FieldType)),
                            obj),
                        obj, value).Compile();
                }

                return (obj, val) =>
                {
                    fieldInfo.SetValue(obj, val);
                    return obj;
                };
            }
            else
            {
                throw new InvalidOperationException(SR.Format(SR.InvalidMember, DataContract.GetClrTypeFullName(memberInfo.DeclaringType!), memberInfo.Name));
            }
        }

        private static bool CanCompileFieldAccess(FieldInfo fieldInfo) =>
            RuntimeFeature.IsDynamicCodeCompiled &&
            // Expression compilation does not emit volatile field access prefixes.
            !fieldInfo.GetRequiredCustomModifiers().Contains(typeof(IsVolatile));

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        internal static Func<object, T, object> CreateTypedSetter<T>(DataMember member)
        {
            if (RuntimeFeature.IsDynamicCodeCompiled)
            {
                ParameterExpression obj = Expression.Parameter(typeof(object), nameof(obj));
                ParameterExpression value = Expression.Parameter(typeof(T), nameof(value));
                Expression? assignment = CreateSetterExpression(member.MemberInfo, obj, value);
                if (assignment is not null)
                    return Expression.Lambda<Func<object, T, object>>(Expression.Block(assignment, obj), obj, value).Compile();
            }

            Setter fallback = member.Setter;
            return (obj, value) => fallback(obj, value);
        }

        internal static Expression? CreateSetterExpression(MemberInfo member, Expression obj, Expression value) =>
            member switch
            {
                FieldInfo field when !field.IsInitOnly && !field.IsLiteral && CanCompileFieldAccess(field) =>
                    Expression.Assign(CreateFieldExpression(field, obj), value),
                PropertyInfo property when property.SetMethod is MethodInfo setter && property.GetIndexParameters().Length == 0 =>
                    Expression.Call(setter.IsStatic ? null :
                        property.DeclaringType!.IsValueType ? Expression.Unbox(obj, property.DeclaringType) :
                        Expression.Convert(obj, property.DeclaringType), setter, value),
                _ => null
            };

        internal static Expression? CreateGetterExpression(MemberInfo member, Expression obj)
        {
            if (member is FieldInfo field && CanCompileFieldAccess(field))
                return CreateFieldExpression(field, obj);
            if (member is PropertyInfo property && property.GetMethod is MethodInfo getter &&
                property.GetIndexParameters().Length == 0 && !property.PropertyType.IsByRef)
                return Expression.Call(Expression.Convert(obj, property.DeclaringType!), getter);

            return null;
        }

        private static MemberExpression CreateFieldExpression(FieldInfo fieldInfo, Expression obj)
        {
            Type declaringType = fieldInfo.DeclaringType!;
            // Unbox addresses the existing boxed struct instead of mutating a copy.
            Expression? instance = fieldInfo.IsStatic ? null :
                declaringType.IsValueType ? Expression.Unbox(obj, declaringType) : Expression.Convert(obj, declaringType);
            return Expression.Field(instance, fieldInfo);
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

                return (obj, val) =>
                {
                    var unboxed = (DeclaringType)obj;
                    setMethod(ref unboxed, (PropertyType)val!);
                    return unboxed!;
                };
            }
            else
            {
                var setMethod = propInfo.SetMethod!.CreateDelegate<Action<DeclaringType, PropertyType>>();

                return (obj, val) =>
                {
                    setMethod((DeclaringType)obj, (PropertyType)val!);
                    return obj;
                };
            }
        }
    }
}
