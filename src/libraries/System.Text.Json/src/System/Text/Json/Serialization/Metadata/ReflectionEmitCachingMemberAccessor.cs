// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK || NETCOREAPP
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
    internal sealed partial class ReflectionEmitCachingMemberAccessor : MemberAccessor
    {
        private static readonly ReflectionEmitMemberAccessor s_sourceAccessor = new();
        private readonly Cache<(string id, Type declaringType, MemberInfo? member)> _cache =
            new(slidingExpiration: TimeSpan.FromMilliseconds(1000), evictionInterval: TimeSpan.FromMilliseconds(200));

        public override void Clear() => _cache.Clear();

        public override Action<TCollection, object?> CreateAddMethodDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TCollection>() =>
            _cache.GetOrAdd(
                key: (nameof(CreateAddMethodDelegate), typeof(TCollection), null),
                static _ => s_sourceAccessor.CreateAddMethodDelegate<TCollection>());

        public override Func<object>? CreateParameterlessConstructor(Type type, ConstructorInfo? ctorInfo) =>
            _cache.GetOrAdd(
                key: (nameof(CreateParameterlessConstructor), type, ctorInfo),
                static key => s_sourceAccessor.CreateParameterlessConstructor(key.declaringType, (ConstructorInfo?)key.member));

        public override Func<object, TProperty> CreateFieldGetter<TProperty>(FieldInfo fieldInfo) =>
            _cache.GetOrAdd(
                key: (nameof(CreateFieldGetter), typeof(TProperty), fieldInfo),
                static key => s_sourceAccessor.CreateFieldGetter<TProperty>((FieldInfo)key.member!));

        public override Action<object, TProperty> CreateFieldSetter<TProperty>(FieldInfo fieldInfo) =>
            _cache.GetOrAdd(
                key: (nameof(CreateFieldSetter), typeof(TProperty), fieldInfo),
                static key => s_sourceAccessor.CreateFieldSetter<TProperty>((FieldInfo)key.member!));

        public override Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection> CreateImmutableDictionaryCreateRangeDelegate<TCollection, TKey, TValue>() =>
            _cache.GetOrAdd(
                key: (nameof(CreateImmutableDictionaryCreateRangeDelegate), typeof((TCollection, TKey, TValue)), null),
                static _ => s_sourceAccessor.CreateImmutableDictionaryCreateRangeDelegate<TCollection, TKey, TValue>());

        public override Func<IEnumerable<TElement>, TCollection> CreateImmutableEnumerableCreateRangeDelegate<TCollection, TElement>() =>
            _cache.GetOrAdd(
                key: (nameof(CreateImmutableEnumerableCreateRangeDelegate), typeof((TCollection, TElement)), null),
                static _ => s_sourceAccessor.CreateImmutableEnumerableCreateRangeDelegate<TCollection, TElement>());

        public override Func<object[], T> CreateParameterizedConstructor<T>(ConstructorInfo constructor) =>
            _cache.GetOrAdd(
                key: (nameof(CreateParameterizedConstructor), typeof(T), constructor),
                static key => s_sourceAccessor.CreateParameterizedConstructor<T>((ConstructorInfo)key.member!));

        public override JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>? CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo constructor) =>
            _cache.GetOrAdd(
                key: (nameof(CreateParameterizedConstructor), typeof(T), constructor),
                static key => s_sourceAccessor.CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>((ConstructorInfo)key.member!));

        public override Func<object, TProperty> CreatePropertyGetter<TProperty>(PropertyInfo propertyInfo) =>
            _cache.GetOrAdd(
                key: (nameof(CreatePropertyGetter), typeof(TProperty), propertyInfo),
                static key => s_sourceAccessor.CreatePropertyGetter<TProperty>((PropertyInfo)key.member!));

        public override Action<object, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo) =>
            _cache.GetOrAdd(
                key: (nameof(CreatePropertySetter), typeof(TProperty), propertyInfo),
                static key => s_sourceAccessor.CreatePropertySetter<TProperty>((PropertyInfo)key.member!));
    }
}
#endif
