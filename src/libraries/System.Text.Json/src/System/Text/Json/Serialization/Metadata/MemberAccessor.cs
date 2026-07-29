// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.Json.Serialization.Metadata
{
    // Adapts the user-declared 'bool TryGetValue(out TCase)' instance methods on a union type
    // to a uniform '(TUnion, out Type?, out object?) => bool' shape: a single chained delegate
    // tries each TryGetValue overload in caller-supplied order and returns the matching case
    // type alongside the boxed value on first hit. Implementations live alongside the other
    // MemberAccessor helpers.
    internal delegate bool UnionTryGetValueAccessor<TUnion>(TUnion union, out Type? caseType, out object? value);

    internal abstract class MemberAccessor
    {
        private static MemberAccessor? s_instance;

        internal static MemberAccessor Instance
        {
            [RequiresUnreferencedCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            get
            {
                return s_instance ?? Initialize();
                static MemberAccessor Initialize()
                {
                    MemberAccessor value =
#if NET
                        // if dynamic code isn't supported, fallback to reflection
                        RuntimeFeature.IsDynamicCodeSupported ?
                            new ReflectionEmitCachingMemberAccessor() :
                            new ReflectionMemberAccessor();
#elif NETFRAMEWORK
                            new ReflectionEmitCachingMemberAccessor();
#else
                            new ReflectionMemberAccessor();
#endif
                    return Interlocked.CompareExchange(ref s_instance, value, null) ?? value;
                }
            }
        }

        internal static void ClearCache() => s_instance?.Clear();

        public abstract Func<object>? CreateParameterlessConstructor(Type type, ConstructorInfo? constructorInfo);

        public abstract Func<object[], T> CreateParameterizedConstructor<T>(ConstructorInfo constructor);

        public abstract JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>? CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo constructor);

        public abstract Func<object?, T> CreateSingleParameterConstructor<T>(ConstructorInfo constructor);

        public abstract Action<TCollection, object?> CreateAddMethodDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TCollection>();

        public abstract Func<IEnumerable<TElement>, TCollection> CreateImmutableEnumerableCreateRangeDelegate<TCollection, TElement>();

        public abstract Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection> CreateImmutableDictionaryCreateRangeDelegate<TCollection, TKey, TValue>();

        public abstract Func<object, TProperty> CreatePropertyGetter<TProperty>(PropertyInfo propertyInfo);

        public abstract Func<TDeclaringType, TProperty> CreatePropertyGetter<TDeclaringType, TProperty>(PropertyInfo propertyInfo);

        public abstract Action<object, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo);

        public abstract Func<object, TProperty> CreateFieldGetter<TProperty>(FieldInfo fieldInfo);

        public abstract Action<object, TProperty> CreateFieldSetter<TProperty>(FieldInfo fieldInfo);

        public abstract UnionTryGetValueAccessor<TUnion> CreateUnionTryGetValueAccessor<TUnion>(IReadOnlyList<KeyValuePair<Type, MethodInfo>> entries);

        public virtual void Clear() { }
    }
}
