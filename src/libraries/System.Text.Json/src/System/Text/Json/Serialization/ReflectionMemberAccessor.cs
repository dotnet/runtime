// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization
{
    internal sealed class ReflectionMemberAccessor : MemberAccessor
    {
        public override JsonClassInfo.ConstructorDelegate? CreateConstructor(Type type)
        {
            Debug.Assert(type != null);
            ConstructorInfo? realMethod = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, binder: null, Type.EmptyTypes, modifiers: null);

            if (type.IsAbstract)
            {
                return null;
            }

            if (realMethod == null && !type.IsValueType)
            {
                return null;
            }

            return () => Activator.CreateInstance(type);
        }

        public override Action<TCollection, object?> CreateAddMethodDelegate<TCollection>()
        {
            Type collectionType = typeof(TCollection);
            Type elementType = typeof(object);

            // We verified this won't be null when we created the converter for the collection type.
            MethodInfo addMethod = (collectionType.GetMethod("Push") ?? collectionType.GetMethod("Enqueue"))!;

            return delegate (TCollection collection, object? element)
            {
                addMethod.Invoke(collection, new object[] { element! });
            };
        }

        public override Func<IEnumerable<TElement>, TCollection> CreateImmutableEnumerableCreateRangeDelegate<TElement, TCollection>()
        {
            MethodInfo createRange = typeof(TCollection).GetImmutableEnumerableCreateRangeMethod(typeof(TElement));
            return (Func<IEnumerable<TElement>, TCollection>)createRange.CreateDelegate(
                typeof(Func<IEnumerable<TElement>, TCollection>));
        }

        public override Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection> CreateImmutableDictionaryCreateRangeDelegate<TElement, TCollection>()
        {
            MethodInfo createRange = typeof(TCollection).GetImmutableDictionaryCreateRangeMethod(typeof(TElement));
            return (Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection>)createRange.CreateDelegate(
                typeof(Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection>));
        }

        public override Func<object, TProperty> CreatePropertyGetter<TProperty>(PropertyInfo propertyInfo)
        {
            MethodInfo getMethodInfo = propertyInfo.GetGetMethod()!;

            return delegate (object obj)
            {
                return (TProperty)getMethodInfo.Invoke(obj, null)!;
            };
        }

        public override Action<object, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo)
        {
            MethodInfo setMethodInfo = propertyInfo.GetSetMethod()!;

            return delegate (object obj, TProperty value)
            {
                setMethodInfo.Invoke(obj, new object[] { value! });
            };
        }
    }
}
