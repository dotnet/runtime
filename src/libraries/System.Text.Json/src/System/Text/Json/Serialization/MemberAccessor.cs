// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;

namespace System.Text.Json
{
    internal abstract class MemberAccessor
    {
        public abstract JsonClassInfo.ConstructorDelegate? CreateConstructor(Type classType);

        public abstract JsonClassInfo.ParameterizedConstructorDelegate<T>? CreateParameterizedConstructor<T>(ConstructorInfo constructor);

        public abstract JsonClassInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>?
            CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo constructor);

        public abstract Action<TCollection, object?> CreateAddMethodDelegate<TCollection>();

        public abstract Func<IEnumerable<TElement>, TCollection> CreateImmutableEnumerableCreateRangeDelegate<TElement, TCollection>();

        public abstract Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection> CreateImmutableDictionaryCreateRangeDelegate<TElement, TCollection>();

        public abstract Func<object, TProperty> CreatePropertyGetter<TProperty>(PropertyInfo propertyInfo);

        public abstract Action<object, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo);

        public abstract Func<object, TProperty> CreateFieldGetter<TProperty>(FieldInfo fieldInfo);

        public abstract Action<object, TProperty> CreateFieldSetter<TProperty>(FieldInfo fieldInfo);
    }
}
