// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if BUILDING_INBOX_LIBRARY
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Text.Json
{
    internal sealed class ReflectionEmitMemberAccessor : MemberAccessor
    {
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

            var dynamicMethod = new DynamicMethod(
                ConstructorInfo.ConstructorName,
                typeof(object),
                Type.EmptyTypes,
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();

            if (realMethod == null)
            {
                LocalBuilder local = generator.DeclareLocal(type);

                generator.Emit(OpCodes.Ldloca_S, local);
                generator.Emit(OpCodes.Initobj, type);
                generator.Emit(OpCodes.Ldloc, local);
                generator.Emit(OpCodes.Box, type);
            }
            else
            {
                generator.Emit(OpCodes.Newobj, realMethod);
            }

            generator.Emit(OpCodes.Ret);

            return (JsonClassInfo.ConstructorDelegate)dynamicMethod.CreateDelegate(typeof(JsonClassInfo.ConstructorDelegate));
        }


        public override ImmutableCollectionCreator ImmutableCollectionCreateRange(Type constructingType, Type collectionType, Type elementType)
        {
            MethodInfo createRange = ImmutableCollectionCreateRangeMethod(constructingType, elementType);

            if (createRange == null)
            {
                return null;
            }

            Type creatorType = typeof(ImmutableEnumerableCreator<,>).MakeGenericType(elementType, collectionType);

            ConstructorInfo realMethod = creatorType.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                Type.EmptyTypes,
                modifiers: null);

            Debug.Assert(realMethod != null);

            var dynamicMethod = new DynamicMethod(
                ConstructorInfo.ConstructorName,
                typeof(object),
                Type.EmptyTypes,
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();
            generator.Emit(OpCodes.Newobj, realMethod);
            generator.Emit(OpCodes.Ret);

            JsonClassInfo.ConstructorDelegate constructor = (JsonClassInfo.ConstructorDelegate)dynamicMethod.CreateDelegate(
                typeof(JsonClassInfo.ConstructorDelegate));

            ImmutableCollectionCreator creator = (ImmutableCollectionCreator)constructor();

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

            ConstructorInfo realMethod = creatorType.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                Type.EmptyTypes,
                modifiers: null);

            Debug.Assert(realMethod != null);

            var dynamicMethod = new DynamicMethod(
                ConstructorInfo.ConstructorName,
                typeof(object),
                Type.EmptyTypes,
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();
            generator.Emit(OpCodes.Newobj, realMethod);
            generator.Emit(OpCodes.Ret);

            JsonClassInfo.ConstructorDelegate constructor = (JsonClassInfo.ConstructorDelegate)dynamicMethod.CreateDelegate(
                typeof(JsonClassInfo.ConstructorDelegate));

            ImmutableCollectionCreator creator = (ImmutableCollectionCreator)constructor();

            creator.RegisterCreatorDelegateFromMethod(createRange);
            return creator;
        }

        public override Func<object, TProperty> CreatePropertyGetter<TClass, TProperty>(PropertyInfo propertyInfo) =>
            (Func<object, TProperty>)CreatePropertyGetter(propertyInfo, typeof(TClass));

        private static Delegate CreatePropertyGetter(PropertyInfo propertyInfo, Type classType)
        {
            MethodInfo realMethod = propertyInfo.GetGetMethod();
            Type objectType = typeof(object);

            var dynamicMethod = new DynamicMethod(
                realMethod.Name,
                propertyInfo.PropertyType,
                new[] { objectType },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);

            if (classType.IsValueType)
            {
                generator.Emit(OpCodes.Unbox, classType);
                generator.Emit(OpCodes.Call, realMethod);
            }
            else
            {
                generator.Emit(OpCodes.Castclass, classType);
                generator.Emit(OpCodes.Callvirt, realMethod);
            }

            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(objectType, propertyInfo.PropertyType));
        }

        public override Action<object, TProperty> CreatePropertySetter<TClass, TProperty>(PropertyInfo propertyInfo) =>
            (Action<object, TProperty>)CreatePropertySetter(propertyInfo, typeof(TClass));

        private static Delegate CreatePropertySetter(PropertyInfo propertyInfo, Type classType)
        {
            MethodInfo realMethod = propertyInfo.GetSetMethod();
            Type objectType = typeof(object);

            var dynamicMethod = new DynamicMethod(
                realMethod.Name,
                typeof(void),
                new[] { objectType, propertyInfo.PropertyType },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);

            if (classType.IsValueType)
            {
                generator.Emit(OpCodes.Unbox, classType);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Call, realMethod);
            }
            else
            {
                generator.Emit(OpCodes.Castclass, classType);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Callvirt, realMethod);
            };

            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(objectType, propertyInfo.PropertyType));
        }
    }
}
#endif
