// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
    internal sealed class ReflectionMemberAccessor : MemberAccessor
    {
        public ReflectionMemberAccessor()
        {
        }

        public override Func<object>? CreateParameterlessConstructor(Type type, ConstructorInfo? ctorInfo)
        {
            Debug.Assert(type != null);
            Debug.Assert(ctorInfo is null || ctorInfo.GetParameters().Length == 0);

            if (type.IsAbstract)
            {
                return null;
            }

            if (ctorInfo is null)
            {
                return type.IsValueType
                    ? () => Activator.CreateInstance(type, nonPublic: false)!
                    : null;
            }

            return () => ctorInfo.InvokeNoWrapExceptions(null);
        }

        public override Func<object[], T> CreateParameterizedConstructor<T>(ConstructorInfo constructor)
        {
            Type type = typeof(T);

            Debug.Assert(!type.IsAbstract);
            Debug.Assert(constructor.DeclaringType == type && !constructor.IsStatic);

            int parameterCount = constructor.GetParameters().Length;

            return (arguments) =>
            {
                // The input array was rented from the shared ArrayPool, so its size is likely to be larger than the param count.
                // The emit equivalent of this method does not (need to) allocate here + transfer the objects.
                object[] argsToPass = new object[parameterCount];

                for (int i = 0; i < parameterCount; i++)
                {
                    argsToPass[i] = arguments[i];
                }

                // Not wrapping in TargetInvocationException also plumbs ArgumentException through for
                // tuples with more than 7 generic parameters, e.g.
                // System.ArgumentException : The last element of an eight element tuple must be a Tuple.
                return (T)constructor.InvokeNoWrapExceptions(argsToPass);
            };
        }

        public override JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>?
            CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo constructor)
        {
            Type type = typeof(T);

            Debug.Assert(!type.IsAbstract);
            Debug.Assert(constructor.DeclaringType == type && !constructor.IsStatic);

            int parameterCount = constructor.GetParameters().Length;

            Debug.Assert(parameterCount <= JsonConstants.UnboxedParameterCountThreshold);

            return (arg0, arg1, arg2, arg3) =>
            {
                object[] arguments = new object[parameterCount];

                for (int i = 0; i < parameterCount; i++)
                {
                    switch (i)
                    {
                        case 0:
                            arguments[0] = arg0!;
                            break;
                        case 1:
                            arguments[1] = arg1!;
                            break;
                        case 2:
                            arguments[2] = arg2!;
                            break;
                        case 3:
                            arguments[3] = arg3!;
                            break;
                        default:
                            Debug.Fail("We shouldn't be here if there are more than 4 parameters.");
                            throw new InvalidOperationException();
                    }
                }

                return (T)constructor.InvokeNoWrapExceptions(arguments);
            };
        }

        public override Func<object?, T> CreateSingleParameterConstructor<T>(ConstructorInfo constructor)
        {
            Type type = typeof(T);

            Debug.Assert(!type.IsAbstract);
            Debug.Assert(constructor.DeclaringType == type && !constructor.IsStatic);
            Debug.Assert(constructor.GetParameters().Length == 1);

            return value =>
            {
                return (T)constructor.InvokeNoWrapExceptions(new object?[] { value });
            };
        }

        public override Action<TCollection, object?> CreateAddMethodDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TCollection>()
        {
            Type collectionType = typeof(TCollection);
            Type elementType = JsonTypeInfo.ObjectType;

            // We verified this won't be null when we created the converter for the collection type.
            MethodInfo addMethod = (collectionType.GetMethod("Push") ?? collectionType.GetMethod("Enqueue"))!;

            return delegate (TCollection collection, object? element)
            {
                addMethod.InvokeNoWrapExceptions(collection, new object[] { element! });
            };
        }

        public override Func<IEnumerable<TElement>, TCollection> CreateImmutableEnumerableCreateRangeDelegate<TCollection, TElement>()
        {
            MethodInfo createRange = typeof(TCollection).GetImmutableEnumerableCreateRangeMethod(typeof(TElement));
            return (Func<IEnumerable<TElement>, TCollection>)createRange.CreateDelegate(
                typeof(Func<IEnumerable<TElement>, TCollection>));
        }

        public override Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection> CreateImmutableDictionaryCreateRangeDelegate<TCollection, TKey, TValue>()
        {
            MethodInfo createRange = typeof(TCollection).GetImmutableDictionaryCreateRangeMethod(typeof(TKey), typeof(TValue));
            return (Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection>)createRange.CreateDelegate(
                typeof(Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection>));
        }

        public override Func<object, TProperty> CreatePropertyGetter<TProperty>(PropertyInfo propertyInfo)
        {
            MethodInfo getMethodInfo = propertyInfo.GetMethod!;

            return delegate (object obj)
            {
                return (TProperty)getMethodInfo.InvokeNoWrapExceptions(obj, null)!;
            };
        }

        public override Func<TDeclaringType, TProperty> CreatePropertyGetter<TDeclaringType, TProperty>(PropertyInfo propertyInfo)
        {
            MethodInfo getMethodInfo = propertyInfo.GetMethod!;

            return delegate (TDeclaringType obj)
            {
                return (TProperty)getMethodInfo.InvokeNoWrapExceptions(obj, null)!;
            };
        }

        public override Action<object, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo)
        {
            MethodInfo setMethodInfo = propertyInfo.SetMethod!;

            return delegate (object obj, TProperty value)
            {
                setMethodInfo.InvokeNoWrapExceptions(obj, new object[] { value! });
            };
        }

        public override Func<object, TProperty> CreateFieldGetter<TProperty>(FieldInfo fieldInfo) =>
            delegate (object obj)
            {
                return (TProperty)fieldInfo.GetValue(obj)!;
            };

        public override Action<object, TProperty> CreateFieldSetter<TProperty>(FieldInfo fieldInfo) =>
            delegate (object obj, TProperty value)
            {
                fieldInfo.SetValue(obj, value);
            };

        public override UnionTryGetValueAccessor<TUnion> CreateUnionTryGetValueAccessor<TUnion>(IReadOnlyList<KeyValuePair<Type, MethodInfo>> entries)
        {
            // Build per-entry typed delegates via Delegate.CreateDelegate so each TryGetValue
            // call is a direct invocation rather than MethodInfo.Invoke (which would box
            // value-type unions and allocate per call). Then return a closure that walks the
            // chain in caller-supplied order; first match wins.
            int count = entries.Count;
            Type[] caseTypes = new Type[count];
            UnionTryGetValueAccessor<TUnion>[] chain = new UnionTryGetValueAccessor<TUnion>[count];
            for (int i = 0; i < count; i++)
            {
                KeyValuePair<Type, MethodInfo> entry = entries[i];
                caseTypes[i] = entry.Key;
                chain[i] = (UnionTryGetValueAccessor<TUnion>)typeof(ReflectionMemberAccessor)
                    .GetMethod(nameof(CreateUnionTryGetValueAccessorCore), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(typeof(TUnion), entry.Key)
                    .Invoke(null, new object[] { entry.Value })!;
            }

            return (TUnion union, out Type? caseType, out object? value) =>
            {
                for (int i = 0; i < chain.Length; i++)
                {
                    if (chain[i](union, out _, out value))
                    {
                        caseType = caseTypes[i];
                        return true;
                    }
                }

                caseType = null;
                value = null;
                return false;
            };
        }

        private delegate bool TypedTryGetValueDelegate<TUnion, TCase>(TUnion union, out TCase? value);

        private delegate bool TypedStructTryGetValueDelegate<TUnion, TCase>(ref TUnion union, out TCase? value);

        private static UnionTryGetValueAccessor<TUnion> CreateUnionTryGetValueAccessorCore<TUnion, TCase>(MethodInfo method)
        {
            // Per-entry adapter: binds the user-declared method to a typed delegate and
            // returns a delegate matching UnionTryGetValueAccessor<TUnion>. The outer chained
            // delegate fills in caseType on success; this inner adapter only reports value.
            if (typeof(TUnion).IsValueType)
            {
                TypedStructTryGetValueDelegate<TUnion, TCase> typed =
                    (TypedStructTryGetValueDelegate<TUnion, TCase>)Delegate.CreateDelegate(
                        typeof(TypedStructTryGetValueDelegate<TUnion, TCase>), method, throwOnBindFailure: true)!;

                return (TUnion union, out Type? caseType, out object? value) =>
                {
                    bool result = typed(ref union, out TCase? extracted);
                    caseType = null;
                    value = result ? extracted : null;
                    return result;
                };
            }
            else
            {
                TypedTryGetValueDelegate<TUnion, TCase> typed =
                    (TypedTryGetValueDelegate<TUnion, TCase>)Delegate.CreateDelegate(
                        typeof(TypedTryGetValueDelegate<TUnion, TCase>), method, throwOnBindFailure: true)!;

                return (TUnion union, out Type? caseType, out object? value) =>
                {
                    bool result = typed(union, out TCase? extracted);
                    caseType = null;
                    value = result ? extracted : null;
                    return result;
                };
            }
        }
    }
}
