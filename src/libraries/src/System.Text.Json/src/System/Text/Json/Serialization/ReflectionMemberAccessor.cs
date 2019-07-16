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
        private delegate TProperty GetProperty<TClass, TProperty>(TClass obj);
        private delegate TProperty GetPropertyByRef<TClass, TProperty>(ref TClass obj);

        private delegate void SetProperty<TClass, TProperty>(TClass obj, TProperty value);
        private delegate void SetPropertyByRef<TClass, TProperty>(ref TClass obj, TProperty value);

        private delegate Func<object, TProperty> GetPropertyByRefFactory<TClass, TProperty>(GetPropertyByRef<TClass, TProperty> set);
        private delegate Action<object, TProperty> SetPropertyByRefFactory<TClass, TProperty>(SetPropertyByRef<TClass, TProperty> set);

        private static readonly MethodInfo s_createStructPropertyGetterMethod = new GetPropertyByRefFactory<int, int>(CreateStructPropertyGetter)
            .Method.GetGenericMethodDefinition();

        private static readonly MethodInfo s_createStructPropertySetterMethod = new SetPropertyByRefFactory<int, int>(CreateStructPropertySetter)
            .Method.GetGenericMethodDefinition();
        public override JsonClassInfo.ConstructorDelegate CreateConstructor(Type type)
        {
            Debug.Assert(type != null);
            ConstructorInfo realMethod = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, binder: null, Type.EmptyTypes, modifiers: null);

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

        public override ImmutableCollectionCreator ImmutableCollectionCreateRange(Type constructingType, Type collectionType, Type elementType)
        {
            MethodInfo createRange = ImmutableCollectionCreateRangeMethod(constructingType, elementType);

            if (createRange == null)
            {
                return null;
            }

            Type creatorType = typeof(ImmutableEnumerableCreator<,>).MakeGenericType(elementType, collectionType);
            ConstructorInfo constructor = creatorType.GetConstructor(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance, binder: null,
                Type.EmptyTypes,
                modifiers: null);

            ImmutableCollectionCreator creator = (ImmutableCollectionCreator)constructor.Invoke(new object[] { });
            creator.RegisterCreatorDelegateFromMethod(createRange);
            return creator;
        }


        public override ImmutableCollectionCreator ImmutableDictionaryCreateRange(Type constructingType, Type collectionType, Type elementType)
        {
            MethodInfo createRange = ImmutableDictionaryCreateRangeMethod(constructingType, elementType);

            if (createRange == null)
            {
                return null;
            }

            Type creatorType = typeof(ImmutableDictionaryCreator<,>).MakeGenericType(elementType, collectionType);
            ConstructorInfo constructor = creatorType.GetConstructor(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance, binder: null,
                Type.EmptyTypes,
                modifiers: null);

            ImmutableCollectionCreator creator = (ImmutableCollectionCreator)constructor.Invoke(new object[] { });
            creator.RegisterCreatorDelegateFromMethod(createRange);
            return creator;
        }

        public override Func<object, TProperty> CreatePropertyGetter<TClass, TProperty>(PropertyInfo propertyInfo)
        {
            MethodInfo getMethodInfo = propertyInfo.GetGetMethod();

            if (typeof(TClass).IsValueType)
            {
                var factory = CreateDelegate<GetPropertyByRefFactory<TClass, TProperty>>(s_createStructPropertyGetterMethod.MakeGenericMethod(typeof(TClass), typeof(TProperty)));
                var propertyGetter = CreateDelegate<GetPropertyByRef<TClass, TProperty>>(getMethodInfo);

                return factory(propertyGetter);
            }
            else
            {
                var propertyGetter = CreateDelegate<GetProperty<TClass, TProperty>>(getMethodInfo);
                return delegate (object obj)
                {
                    return propertyGetter((TClass)obj);
                };
            }
        }

        public override Action<object, TProperty> CreatePropertySetter<TClass, TProperty>(PropertyInfo propertyInfo)
        {
            MethodInfo setMethodInfo = propertyInfo.GetSetMethod();

            if (typeof(TClass).IsValueType)
            {
                var factory = CreateDelegate<SetPropertyByRefFactory<TClass, TProperty>>(s_createStructPropertySetterMethod.MakeGenericMethod(typeof(TClass), typeof(TProperty)));
                var propertySetter = CreateDelegate<SetPropertyByRef<TClass, TProperty>>(setMethodInfo);

                return factory(propertySetter);
            }
            else
            {
                var propertySetter = CreateDelegate<SetProperty<TClass, TProperty>>(setMethodInfo);
                return delegate (object obj, TProperty value)
                {
                    propertySetter((TClass)obj, value);
                };
            }
        }

        private static TDelegate CreateDelegate<TDelegate>(MethodInfo methodInfo)
            where TDelegate : Delegate
        {
            return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), methodInfo);
        }

        private static Func<object, TProperty> CreateStructPropertyGetter<TClass, TProperty>(GetPropertyByRef<TClass, TProperty> get)
            where TClass : struct
        {
            return delegate (object obj)
            {
                return get(ref Unsafe.Unbox<TClass>(obj));
            };
        }

        private static Action<object, TProperty> CreateStructPropertySetter<TClass, TProperty>(SetPropertyByRef<TClass, TProperty> set)
            where TClass : struct
        {
            return delegate (object obj, TProperty value)
            {
                set(ref Unsafe.Unbox<TClass>(obj), value);
            };
        }
    }
}
