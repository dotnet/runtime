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

#if NET
            ConstructorInvoker invoker = ConstructorInvoker.Create(ctorInfo);
            return invoker.Invoke;
#else
            return () => ctorInfo.InvokeNoWrapExceptions(null);
#endif
        }

        public override Func<object?[], T> CreateParameterizedConstructor<T>(ConstructorInfo constructor)
        {
            Type type = typeof(T);

            Debug.Assert(!type.IsAbstract);
            Debug.Assert(constructor.DeclaringType == type && !constructor.IsStatic);

            int parameterCount = constructor.GetParameters().Length;

#if NET
            ConstructorInvoker invoker = ConstructorInvoker.Create(constructor);
            return arguments => (T)invoker.Invoke(arguments.AsSpan(0, parameterCount));
#else
            return (arguments) =>
            {
                // The input array was rented from the shared ArrayPool, so its size is likely to be larger than the param count.
                // The emit equivalent of this method does not (need to) allocate here + transfer the objects.
                object[] argsToPass = new object[parameterCount];

                Array.Copy(arguments, 0, argsToPass, 0, parameterCount);

                // Not wrapping in TargetInvocationException also plumbs ArgumentException through for
                // tuples with more than 7 generic parameters, e.g.
                // System.ArgumentException : The last element of an eight element tuple must be a Tuple.
                return (T)constructor.InvokeNoWrapExceptions(argsToPass);
            };
#endif
        }

        public override JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>?
            CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo constructor)
        {
            Type type = typeof(T);

            Debug.Assert(!type.IsAbstract);
            Debug.Assert(constructor.DeclaringType == type && !constructor.IsStatic);

            int parameterCount = constructor.GetParameters().Length;
#if NET
            ConstructorInvoker invoker = ConstructorInvoker.Create(constructor);
#endif

            Debug.Assert(parameterCount <= JsonConstants.UnboxedParameterCountThreshold);

            return (arg0, arg1, arg2, arg3) =>
            {
#if NET
                switch (parameterCount)
                {
                    case 0:
                        return (T)invoker.Invoke();
                    case 1:
                        return (T)invoker.Invoke(arg0);
                    case 2:
                        return (T)invoker.Invoke(arg0, arg1);
                    case 3:
                        return (T)invoker.Invoke(arg0, arg1, arg2);
                    case 4:
                        return (T)invoker.Invoke(arg0, arg1, arg2, arg3);
                    default:
                        Debug.Fail("We shouldn't be here if there are more than 4 parameters.");
                        throw new InvalidOperationException();
                }
#else
                object?[] arguments = new object?[parameterCount];

                switch (parameterCount)
                {
                    case > 4:
                        Debug.Fail("We shouldn't be here if there are more than 4 parameters.");
                        throw new InvalidOperationException();
                    case 4:
                        arguments[3] = arg3;
                        goto case 3;
                    case 3:
                        arguments[2] = arg2;
                        goto case 2;
                    case 2:
                        arguments[1] = arg1;
                        goto case 1;
                    case 1:
                        arguments[0] = arg0;
                        break;
                }

                return (T)constructor.InvokeNoWrapExceptions(arguments);
#endif
            };
        }

        public override Func<object?, T> CreateSingleParameterConstructor<T>(ConstructorInfo constructor)
        {
            Type type = typeof(T);

            Debug.Assert(!type.IsAbstract);
            Debug.Assert(constructor.DeclaringType == type && !constructor.IsStatic);
            Debug.Assert(constructor.GetParameters().Length == 1);

#if NET
            ConstructorInvoker invoker = ConstructorInvoker.Create(constructor);
            return value => (T)invoker.Invoke(value);
#else
            return value => (T)constructor.InvokeNoWrapExceptions(new object?[] { value });
#endif
        }

        public override Action<TCollection, object?> CreateAddMethodDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TCollection>()
        {
            Type collectionType = typeof(TCollection);
            Type elementType = JsonTypeInfo.ObjectType;

            // We verified this won't be null when we created the converter for the collection type.
            MethodInfo addMethod = (collectionType.GetMethod("Push") ?? collectionType.GetMethod("Enqueue"))!;
#if NET
            MethodInvoker invoker = MethodInvoker.Create(addMethod);
            return (collection, element) => invoker.Invoke(collection, element);
#else
            return (collection, element) => addMethod.InvokeNoWrapExceptions(collection, new object[] { element });
#endif
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
#if NET
            MethodInvoker invoker = MethodInvoker.Create(getMethodInfo);
            return obj => (TProperty)invoker.Invoke(obj)!;
#else
            return obj => (TProperty)getMethodInfo.InvokeNoWrapExceptions(obj, null)!;
#endif
        }

        public override Func<TDeclaringType, TProperty> CreatePropertyGetter<TDeclaringType, TProperty>(PropertyInfo propertyInfo)
        {
            MethodInfo getMethodInfo = propertyInfo.GetMethod!;

            // We can directly wrap a delegate over the getter only if the declaring type is a reference type.
            if (!typeof(TDeclaringType).IsValueType)
            {
                return getMethodInfo.CreateDelegate<Func<TDeclaringType, TProperty>>();
            }

#if NET
            MethodInvoker invoker = MethodInvoker.Create(getMethodInfo);
            return obj => (TProperty)invoker.Invoke(obj)!;
#else
            return obj => (TProperty)getMethodInfo.InvokeNoWrapExceptions(obj, null)!;
#endif
        }

        public override Action<object, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo)
        {
            MethodInfo setMethodInfo = propertyInfo.SetMethod!;

#if NET
            MethodInvoker invoker = MethodInvoker.Create(setMethodInfo);
            return (obj, value) => invoker.Invoke(obj, value);
#else
            return (obj, value) => setMethodInfo.InvokeNoWrapExceptions(obj, new object[] { value });
#endif
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
