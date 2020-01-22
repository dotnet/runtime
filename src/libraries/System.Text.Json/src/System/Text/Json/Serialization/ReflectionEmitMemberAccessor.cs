// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if BUILDING_INBOX_LIBRARY
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal sealed class ReflectionEmitMemberAccessor : MemberAccessor
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

            ConstructorInfo? realMethod = creatorType.GetConstructor(
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

            ImmutableCollectionCreator? creator = (ImmutableCollectionCreator?)constructor();

            Debug.Assert(creator != null);
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

            ConstructorInfo? realMethod = creatorType.GetConstructor(
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

            ImmutableCollectionCreator? creator = (ImmutableCollectionCreator?)constructor();

            Debug.Assert(creator != null);
            creator.RegisterCreatorDelegateFromMethod(createRange);
            return creator;
        }

        public override Func<object?, TProperty> CreateGetter<TClass, TProperty>(PropertyInfo propertyInfo) =>
            (Func<object?, TProperty>)CreateGetter(typeof(TClass), propertyInfo);

        private static Delegate CreateGetter(Type classType, PropertyInfo propertyInfo)
        {
            MethodInfo? realMethod = propertyInfo.GetGetMethod();
            Debug.Assert(realMethod != null);

            DynamicMethod dynamicMethod = CreateGetterMethod(propertyInfo.Name, propertyInfo.PropertyType);
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

            return dynamicMethod
                .CreateDelegate(typeof(Func<,>)
                .MakeGenericType(typeof(object), propertyInfo.PropertyType));
        }

        public override Func<object?, TProperty> CreateGetter<TClass, TProperty>(FieldInfo fieldInfo) =>
            (Func<object?, TProperty>)CreateGetter(typeof(TClass), fieldInfo);

        private static Delegate CreateGetter(Type classType, FieldInfo fieldInfo)
        {
            DynamicMethod dynamicMethod = CreateGetterMethod(fieldInfo.Name, fieldInfo.FieldType);
            ILGenerator generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(
                classType.IsValueType
                    ? OpCodes.Unbox
                    : OpCodes.Castclass,
                classType);
            generator.Emit(OpCodes.Ldfld, fieldInfo);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(object), fieldInfo.FieldType));
        }

        private static DynamicMethod CreateGetterMethod(string memberName, Type memberType) =>
            new DynamicMethod(
                memberName + "Getter",
                memberType,
                new[] { typeof(object) },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

        public override Action<object?, TProperty> CreateSetter<TClass, TProperty>(PropertyInfo propertyInfo) =>
            (Action<object?, TProperty>)CreateSetter(typeof(TClass), propertyInfo);

        private static Delegate CreateSetter(Type classType, PropertyInfo propertyInfo)
        {
            MethodInfo? realMethod = propertyInfo.GetSetMethod();
            Debug.Assert(realMethod != null);

            DynamicMethod dynamicMethod = CreateSetterMethod(propertyInfo.Name, propertyInfo.PropertyType);
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

            return dynamicMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(object), propertyInfo.PropertyType));
        }

        public override Action<object?, TProperty> CreateSetter<TClass, TProperty>(FieldInfo fieldInfo) =>
            (Action<object?, TProperty>)CreateSetter(typeof(TClass), fieldInfo);

        private static Delegate CreateSetter(Type classType, FieldInfo fieldInfo)
        {
            DynamicMethod dynamicMethod = CreateSetterMethod(fieldInfo.Name, fieldInfo.FieldType);
            ILGenerator generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(
                classType.IsValueType
                    ? OpCodes.Unbox
                    : OpCodes.Castclass,
                classType);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stfld, fieldInfo);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(object), fieldInfo.FieldType));
        }

        private static DynamicMethod CreateSetterMethod(string memberName, Type memberType) =>
            new DynamicMethod(
                memberName + "Setter",
                typeof(void),
                new[] { typeof(object), memberType },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);
    }
}
#endif
