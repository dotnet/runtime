// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETFRAMEWORK || NETCOREAPP
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Text.Json.Serialization
{
    internal sealed class ReflectionEmitMemberAccessor : MemberAccessor
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

        public override JsonClassInfo.ParameterizedConstructorDelegate<T>? CreateParameterizedConstructor<T>(ConstructorInfo constructor) =>
            CreateDelegate<JsonClassInfo.ParameterizedConstructorDelegate<T>>(CreateParameterizedConstructor(constructor));

        private static DynamicMethod? CreateParameterizedConstructor(ConstructorInfo constructor)
        {
            Type? type = constructor.DeclaringType;

            Debug.Assert(type != null);
            Debug.Assert(!type.IsAbstract);
            Debug.Assert(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Contains(constructor));

            ParameterInfo[] parameters = constructor.GetParameters();
            int parameterCount = parameters.Length;

            if (parameterCount > JsonConstants.MaxParameterCount)
            {
                return null;
            }

            var dynamicMethod = new DynamicMethod(
                ConstructorInfo.ConstructorName,
                type,
                new[] { typeof(object[]) },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();

            for (int i = 0; i < parameterCount; i++)
            {
                Type paramType = parameters[i].ParameterType;

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldc_I4_S, i);
                generator.Emit(OpCodes.Ldelem_Ref);
                generator.Emit(OpCodes.Unbox_Any, paramType);
            }

            generator.Emit(OpCodes.Newobj, constructor);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        public override JsonClassInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>?
            CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo constructor) =>
            CreateDelegate<JsonClassInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>>(
                CreateParameterizedConstructor(constructor, typeof(TArg0), typeof(TArg1), typeof(TArg2), typeof(TArg3)));

        private static DynamicMethod? CreateParameterizedConstructor(ConstructorInfo constructor, Type parameterType1, Type parameterType2, Type parameterType3, Type parameterType4)
        {
            Type? type = constructor.DeclaringType;

            Debug.Assert(type != null);
            Debug.Assert(!type.IsAbstract);
            Debug.Assert(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Contains(constructor));

            ParameterInfo[] parameters = constructor.GetParameters();
            int parameterCount = parameters.Length;

            var dynamicMethod = new DynamicMethod(
                ConstructorInfo.ConstructorName,
                type,
                new[] { parameterType1, parameterType2, parameterType3, parameterType4 },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();

            for (int index = 0; index < parameterCount; index++)
            {
                Debug.Assert(index <= JsonConstants.UnboxedParameterCountThreshold);

                generator.Emit(
                    index switch
                    {
                        0 => OpCodes.Ldarg_0,
                        1 => OpCodes.Ldarg_1,
                        2 => OpCodes.Ldarg_2,
                        3 => OpCodes.Ldarg_3,
                        _ => throw new InvalidOperationException()
                    });
            }

            generator.Emit(OpCodes.Newobj, constructor);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        public override Action<TCollection, object?> CreateAddMethodDelegate<TCollection>() =>
            CreateDelegate<Action<TCollection, object?>>(CreateAddMethodDelegate(typeof(TCollection)));

        private static DynamicMethod CreateAddMethodDelegate(Type collectionType)
        {
            // We verified this won't be null when we created the converter that calls this method.
            MethodInfo realMethod = (collectionType.GetMethod("Push") ?? collectionType.GetMethod("Enqueue"))!;

            var dynamicMethod = new DynamicMethod(
                realMethod.Name,
                typeof(void),
                new[] { collectionType, typeof(object) },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, realMethod);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        public override Func<IEnumerable<TElement>, TCollection> CreateImmutableEnumerableCreateRangeDelegate<TElement, TCollection>() =>
            CreateDelegate<Func<IEnumerable<TElement>, TCollection>>(
                CreateImmutableEnumerableCreateRangeDelegate(typeof(TCollection), typeof(TElement), typeof(IEnumerable<TElement>)));

        private static DynamicMethod CreateImmutableEnumerableCreateRangeDelegate(Type collectionType, Type elementType, Type enumerableType)
        {
            MethodInfo realMethod = collectionType.GetImmutableEnumerableCreateRangeMethod(elementType);

            var dynamicMethod = new DynamicMethod(
                realMethod.Name,
                collectionType,
                new[] { enumerableType },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, realMethod);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        public override Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection> CreateImmutableDictionaryCreateRangeDelegate<TElement, TCollection>() =>
            CreateDelegate<Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection>>(
                CreateImmutableDictionaryCreateRangeDelegate(typeof(TCollection), typeof(TElement), typeof(IEnumerable<KeyValuePair<string, TElement>>)));

        private static DynamicMethod CreateImmutableDictionaryCreateRangeDelegate(Type collectionType, Type elementType, Type enumerableType)
        {
            MethodInfo realMethod = collectionType.GetImmutableDictionaryCreateRangeMethod(elementType);

            var dynamicMethod = new DynamicMethod(
                realMethod.Name,
                collectionType,
                new[] { enumerableType },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, realMethod);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        public override Func<object?, TProperty> CreatePropertyGetter<TProperty>(PropertyInfo propertyInfo) =>
            CreateDelegate<Func<object?, TProperty>>(CreatePropertyGetter(propertyInfo, propertyInfo.DeclaringType!, typeof(TProperty)));

        private static DynamicMethod CreatePropertyGetter(PropertyInfo propertyInfo, Type classType, Type propertyType)
        {
            MethodInfo? realMethod = propertyInfo.GetMethod;
            Type objectType = typeof(object);

            Debug.Assert(realMethod != null);
            var dynamicMethod = new DynamicMethod(
                realMethod.Name,
                propertyType,
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

            return dynamicMethod;
        }

        public override Action<object?, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo) =>
            CreateDelegate<Action<object?, TProperty>>(CreatePropertySetter(propertyInfo, propertyInfo.DeclaringType!, typeof(TProperty)));

        private static DynamicMethod CreatePropertySetter(PropertyInfo propertyInfo, Type classType, Type propertyType)
        {
            MethodInfo? realMethod = propertyInfo.SetMethod;
            Type objectType = typeof(object);

            Debug.Assert(realMethod != null);
            var dynamicMethod = new DynamicMethod(
                realMethod.Name,
                typeof(void),
                new[] { objectType, propertyType },
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

            return dynamicMethod;
        }

        [return: NotNullIfNotNull("method")]
        private static T? CreateDelegate<T>(DynamicMethod? method) where T : Delegate =>
            (T?)method?.CreateDelegate(typeof(T));
    }
}
#endif
