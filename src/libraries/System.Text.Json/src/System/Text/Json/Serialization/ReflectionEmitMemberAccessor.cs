// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETFRAMEWORK || NETCOREAPP
using System.Collections.Generic;
using System.Diagnostics;
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

        public override JsonClassInfo.ParameterizedConstructorDelegate<T>? CreateParameterizedConstructor<T>(ConstructorInfo constructor)
        {
            Type type = typeof(T);

            Debug.Assert(!type.IsAbstract);
            // If ctor is non-public, we've verified upstream that it has the [JsonConstructorAttribute].
            Debug.Assert(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Contains(constructor));

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

                if (paramType.IsValueType)
                {
                    generator.Emit(OpCodes.Unbox_Any, paramType);
                }
                else
                {
                    generator.Emit(OpCodes.Castclass, paramType);
                };
            }

            generator.Emit(OpCodes.Newobj, constructor);
            generator.Emit(OpCodes.Ret);

            return (JsonClassInfo.ParameterizedConstructorDelegate<T>)dynamicMethod.CreateDelegate(typeof(JsonClassInfo.ParameterizedConstructorDelegate<T>));
        }

        public override JsonClassInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>?
            CreateParameterizedConstructor<T, TArg0, TArg1, TArg2, TArg3>(ConstructorInfo constructor)
        {
            Type type = typeof(T);

            Debug.Assert(!type.IsAbstract);
            // If ctor is non-public, we've verified upstream that it has the [JsonConstructorAttribute].
            Debug.Assert(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Contains(constructor));

            ParameterInfo[] parameters = constructor.GetParameters();
            int parameterCount = parameters.Length;

            Debug.Assert(parameterCount <= JsonConstants.UnboxedParameterCountThreshold);

            var dynamicMethod = new DynamicMethod(
                ConstructorInfo.ConstructorName,
                type,
                new[] { typeof(TArg0), typeof(TArg1), typeof(TArg2), typeof(TArg3) },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();

            for (int index = 0; index < parameterCount; index++)
            {
                switch (index)
                {
                    case 0:
                        generator.Emit(OpCodes.Ldarg_0);
                        break;
                    case 1:
                        generator.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        generator.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        generator.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        Debug.Fail("We shouldn't be here if there are more than 4 parameters.");
                        throw new InvalidOperationException();
                }
            }

            generator.Emit(OpCodes.Newobj, constructor);
            generator.Emit(OpCodes.Ret);

            return (JsonClassInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>)
                dynamicMethod.CreateDelegate(
                    typeof(JsonClassInfo.ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>));
        }

        public override Action<TCollection, object?> CreateAddMethodDelegate<TCollection>()
        {
            Type collectionType = typeof(TCollection);
            Type elementType = typeof(object);

            // We verified this won't be null when we created the converter that calls this method.
            MethodInfo realMethod = (collectionType.GetMethod("Push") ?? collectionType.GetMethod("Enqueue"))!;

            var dynamicMethod = new DynamicMethod(
                realMethod.Name,
                typeof(void),
                new[] { collectionType, elementType },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, realMethod);
            generator.Emit(OpCodes.Ret);

            return (Action<TCollection, object?>)dynamicMethod.CreateDelegate(typeof(Action<TCollection, object?>));
        }

        public override Func<IEnumerable<TElement>, TCollection> CreateImmutableEnumerableCreateRangeDelegate<TElement, TCollection>()
        {
            Type collectionType = typeof(TCollection);
            MethodInfo realMethod = collectionType.GetImmutableEnumerableCreateRangeMethod(typeof(TElement));

            var dynamicMethod = new DynamicMethod(
                realMethod.Name,
                collectionType,
                new[] { typeof(IEnumerable<TElement>) },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, realMethod);
            generator.Emit(OpCodes.Ret);

            return (Func<IEnumerable<TElement>, TCollection>)dynamicMethod.CreateDelegate(typeof(Func<IEnumerable<TElement>, TCollection>));
        }

        public override Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection> CreateImmutableDictionaryCreateRangeDelegate<TElement, TCollection>()
        {
            Type collectionType = typeof(TCollection);
            MethodInfo realMethod = collectionType.GetImmutableDictionaryCreateRangeMethod(typeof(TElement));

            var dynamicMethod = new DynamicMethod(
                realMethod.Name,
                collectionType,
                new[] { typeof(IEnumerable<KeyValuePair<string, TElement>>) },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

            ILGenerator generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, realMethod);
            generator.Emit(OpCodes.Ret);

            return (Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection>)dynamicMethod.CreateDelegate(typeof(Func<IEnumerable<KeyValuePair<string, TElement>>, TCollection>));
        }

        public override Func<object?, TProperty> CreatePropertyGetter<TProperty>(PropertyInfo propertyInfo) =>
            (Func<object?, TProperty>)CreatePropertyGetter(propertyInfo, propertyInfo.DeclaringType!, typeof(TProperty));

        private static Delegate CreatePropertyGetter(PropertyInfo propertyInfo, Type classType, Type propertyType)
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

            return dynamicMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(objectType, propertyType));
        }

        public override Action<object?, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo) =>
            (Action<object?, TProperty>)CreatePropertySetter(propertyInfo, propertyInfo.DeclaringType!, typeof(TProperty));

        private static Delegate CreatePropertySetter(PropertyInfo propertyInfo, Type classType, Type propertyType)
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

            return dynamicMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(objectType, propertyType));
        }
    }
}
#endif
