// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal sealed class ReflectionMemberAccessor : MemberAccessor
    {
        public override JsonClassInfo.ConstructorDelegate? CreateConstructor(Type type)
        {
            Debug.Assert(type != null);
            ConstructorInfo? realMethod = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, binder: null, Type.EmptyTypes, modifiers: null);

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

        public override Action<TProperty> CreateAddDelegate<TProperty>(MethodInfo addMethod, object target)
        {
            Debug.Assert(addMethod != null && target != null);
            return (Action<TProperty>)addMethod.CreateDelegate(typeof(Action<TProperty>), target);
        }

        [PreserveDependency(".ctor()", "System.Text.Json.ImmutableEnumerableCreator`2")]
        public override ImmutableCollectionCreator ImmutableCollectionCreateRange(Type constructingType, Type collectionType, Type elementType)
        {
            MethodInfo createRange = ImmutableCollectionCreateRangeMethod(constructingType, elementType);

            Type creatorType = typeof(ImmutableEnumerableCreator<,>).MakeGenericType(elementType, collectionType);
            ConstructorInfo constructor = creatorType.GetConstructor(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance, binder: null,
                Type.EmptyTypes,
                modifiers: null)!;

            ImmutableCollectionCreator creator = (ImmutableCollectionCreator)constructor.Invoke(Array.Empty<object>());
            creator.RegisterCreatorDelegateFromMethod(createRange);
            return creator;
        }

        [PreserveDependency(".ctor()", "System.Text.Json.ImmutableDictionaryCreator`2")]
        public override ImmutableCollectionCreator ImmutableDictionaryCreateRange(Type constructingType, Type collectionType, Type elementType)
        {
            Debug.Assert(collectionType.IsGenericType);

            // Only string keys are allowed.
            if (collectionType.GetGenericArguments()[0] != typeof(string))
            {
                throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(collectionType, parentType: null, memberInfo: null);
            }

            MethodInfo createRange = ImmutableDictionaryCreateRangeMethod(constructingType, elementType);

            Type creatorType = typeof(ImmutableDictionaryCreator<,>).MakeGenericType(elementType, collectionType);
            ConstructorInfo constructor = creatorType.GetConstructor(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance, binder: null,
                Type.EmptyTypes,
                modifiers: null)!;

            ImmutableCollectionCreator creator = (ImmutableCollectionCreator)constructor.Invoke(Array.Empty<object>());
            creator.RegisterCreatorDelegateFromMethod(createRange);
            return creator;
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
