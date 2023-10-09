// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK || NETCOREAPP
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
    internal sealed partial class ReflectionEmitCachingMemberAccessor : MemberAccessor
    {
        private static readonly ReflectionEmitMemberAccessor s_sourceAccessor = new();
        private static readonly Cache<(string id, Type declaringType, MemberInfo? member)> s_cache =
            new(slidingExpiration: TimeSpan.FromMilliseconds(1000), evictionInterval: TimeSpan.FromMilliseconds(200));

        public static void Clear() => s_cache.Clear();

        public override Action<TCollection, object?> CreateAddMethodDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TCollection>()
            => s_cache.GetOrAdd((nameof(CreateAddMethodDelegate), typeof(TCollection), null),
                static (_) => s_sourceAccessor.CreateAddMethodDelegate<TCollection>());

        public override Func<object>? CreateParameterlessConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ConstructorInfo? ctorInfo)
            => s_cache.GetOrAdd((nameof(CreateParameterlessConstructor), type, ctorInfo),
                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077:UnrecognizedReflectionPattern",
                    Justification = "Cannot apply DynamicallyAccessedMembersAttribute to tuple properties.")]
#pragma warning disable IL2077 // The suppression doesn't work for the trim analyzer: https://github.com/dotnet/roslyn/issues/59746
                static (key) => s_sourceAccessor.CreateParameterlessConstructor(key.declaringType, (ConstructorInfo?)key.member));
#pragma warning restore IL2077

        public override Func<object, TProperty> CreateFieldGetter<TProperty>(FieldInfo fieldInfo)
            => s_cache.GetOrAdd((nameof(CreateFieldGetter), typeof(TProperty), fieldInfo), static key => s_sourceAccessor.CreateFieldGetter<TProperty>((FieldInfo)key.member!));

        public override Action<object, TProperty> CreateFieldSetter<TProperty>(FieldInfo fieldInfo)
            => s_cache.GetOrAdd((nameof(CreateFieldSetter), typeof(TProperty), fieldInfo), static key => s_sourceAccessor.CreateFieldSetter<TProperty>((FieldInfo)key.member!));

        [RequiresUnreferencedCode(IEnumerableConverterFactoryHelpers.ImmutableConvertersUnreferencedCodeMessage)]
        public override Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection> CreateImmutableDictionaryCreateRangeDelegate<TCollection, TKey, TValue>()
            => s_cache.GetOrAdd((nameof(CreateImmutableDictionaryCreateRangeDelegate), typeof((TCollection, TKey, TValue)), null),
                static (_) => s_sourceAccessor.CreateImmutableDictionaryCreateRangeDelegate<TCollection, TKey, TValue>());

        [RequiresUnreferencedCode(IEnumerableConverterFactoryHelpers.ImmutableConvertersUnreferencedCodeMessage)]
        public override Func<IEnumerable<TElement>, TCollection> CreateImmutableEnumerableCreateRangeDelegate<TCollection, TElement>()
            => s_cache.GetOrAdd((nameof(CreateImmutableEnumerableCreateRangeDelegate), typeof((TCollection, TElement)), null),
                static (_) => s_sourceAccessor.CreateImmutableEnumerableCreateRangeDelegate<TCollection, TElement>());

        public override Func<object[], T> CreateParameterizedConstructor<T>(ConstructorInfo constructor)
            => s_cache.GetOrAdd((nameof(CreateParameterizedConstructor), typeof(T), constructor), static key => s_sourceAccessor.CreateParameterizedConstructor<T>((ConstructorInfo)key.member!));

        public override JsonTypeInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>? CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo constructor)
            => s_cache.GetOrAdd((nameof(CreateParameterizedConstructor), typeof(T), constructor), static key => s_sourceAccessor.CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>((ConstructorInfo)key.member!));

        public override Func<object, TProperty> CreatePropertyGetter<TProperty>(PropertyInfo propertyInfo)
            => s_cache.GetOrAdd((nameof(CreatePropertyGetter), typeof(TProperty), propertyInfo), static key => s_sourceAccessor.CreatePropertyGetter<TProperty>((PropertyInfo)key.member!));

        public override Action<object, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo)
            => s_cache.GetOrAdd((nameof(CreatePropertySetter), typeof(TProperty), propertyInfo), static key => s_sourceAccessor.CreatePropertySetter<TProperty>((PropertyInfo)key.member!));
    }
}
#endif
