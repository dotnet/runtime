// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private static readonly Type s_nullReferenceException = typeof(NullReferenceException);
        private static readonly Type s_invalidCastExceptionType = typeof(InvalidCastException);

        private static readonly Type s_stringType = typeof(string);
        private static readonly Type s_typeType = typeof(Type);

        private static readonly MethodInfo s_object_GetType = typeof(object).GetMethod(nameof(object.GetType))!;
        private static readonly MethodInfo s_throwHelper_ThrowJsonException_DeserializeUnableToAssignValue
            = typeof(ThrowHelper).GetMethod(nameof(ThrowHelper.ThrowJsonException_DeserializeUnableToAssignValue))!;
        private static readonly MethodInfo s_throwHelper_ThrowJsonException_DeserializeUnableToAssignNull
            = typeof(ThrowHelper).GetMethod(nameof(ThrowHelper.ThrowJsonException_DeserializeUnableToAssignNull))!;

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
                JsonClassInfo.ObjectType,
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
                new[] { collectionType, JsonClassInfo.ObjectType },
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
            CreateDelegate<Func<object?, TProperty>>(CreatePropertyGetter(propertyInfo, typeof(TProperty)));

        private static DynamicMethod CreatePropertyGetter(PropertyInfo propertyInfo, Type runtimePropertyType)
        {
            MethodInfo? realMethod = propertyInfo.GetMethod;
            Debug.Assert(realMethod != null);

            Type? declaringType = propertyInfo.DeclaringType;
            Debug.Assert(declaringType != null);

            Type declaredPropertyType = propertyInfo.PropertyType;

            DynamicMethod dynamicMethod = CreateGetterMethod(propertyInfo.Name, runtimePropertyType);
            ILGenerator generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);

            if (declaringType.IsValueType)
            {
                generator.Emit(OpCodes.Unbox, declaringType);
                generator.Emit(OpCodes.Call, realMethod);
            }
            else
            {
                generator.Emit(OpCodes.Castclass, declaringType);
                generator.Emit(OpCodes.Callvirt, realMethod);
            }

            // declaredPropertyType: Type of the property
            // runtimePropertyType:  <T> of JsonConverter / JsonPropertyInfo

            if (declaredPropertyType.IsValueType && declaredPropertyType != runtimePropertyType)
            {
                generator.Emit(OpCodes.Box, declaredPropertyType);
            }

            generator.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        public override Action<object?, TProperty> CreatePropertySetter<TProperty>(PropertyInfo propertyInfo) =>
            CreatePropertySetterDelegate<TProperty>(propertyInfo);

        private static Action<object?, TProperty> CreatePropertySetterDelegate<TProperty>(PropertyInfo propertyInfo)
        {
            string memberName = propertyInfo.Name;
            Type declaredPropertyType = propertyInfo.PropertyType;
            Type runtimePropertyType = typeof(TProperty);

            DynamicMethod propertySetter = CreatePropertySetter(propertyInfo, memberName, declaredPropertyType, runtimePropertyType);

            if (declaredPropertyType == runtimePropertyType)
            {
                // If declared and runtime type are identical, the assignment cannot fail, thus
                // there is no need for exception handling or closures.

                return CreateDelegate<Action<object?, TProperty>>(propertySetter);
            }
            else
            {
                // Return a delegate with some closures needed for creating the potential exception.

                Action<object?, TProperty, string, Type> propertySetterDelegate =
                    CreateDelegate<Action<object?, TProperty, string, Type>>(propertySetter);

                return (object? obj, TProperty value) => propertySetterDelegate(obj, value, memberName, declaredPropertyType);
            }
        }

        private static DynamicMethod CreatePropertySetter(PropertyInfo propertyInfo, string memberName, Type declaredPropertyType, Type runtimePropertyType)
        {
            MethodInfo? realMethod = propertyInfo.SetMethod;
            Debug.Assert(realMethod != null);

            Type? declaringType = propertyInfo.DeclaringType;
            Debug.Assert(declaringType != null);

            // declaredPropertyType: Type of the property
            // runtimePropertyType:  <T> of JsonConverter / JsonPropertyInfo

            bool valueNeedsCastOrUnbox = declaredPropertyType != runtimePropertyType;

            DynamicMethod dynamicMethod = CreateSetterMethod(memberName, runtimePropertyType, valueNeedsCastOrUnbox);
            ILGenerator generator = dynamicMethod.GetILGenerator();

            if (!valueNeedsCastOrUnbox)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(declaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);
                generator.Emit(OpCodes.Ldarg_1);
            }
            else
            {
                LocalBuilder value = generator.DeclareLocal(declaredPropertyType);

                // try
                generator.BeginExceptionBlock();

                // When applied to a reference type, the unbox.any instruction has the same effect as castclass.
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Unbox_Any, declaredPropertyType);
                generator.Emit(OpCodes.Stloc_0, value);

                EmitSetterCatchBlocks(generator);

                generator.EndExceptionBlock();

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(declaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);
                generator.Emit(OpCodes.Ldloc_0);
            }

            generator.Emit(declaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, realMethod);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        public override Func<object, TProperty> CreateFieldGetter<TProperty>(FieldInfo fieldInfo) =>
            CreateDelegate<Func<object, TProperty>>(CreateFieldGetter(fieldInfo, typeof(TProperty)));

        private static DynamicMethod CreateFieldGetter(FieldInfo fieldInfo, Type runtimeFieldType)
        {
            Type? declaringType = fieldInfo.DeclaringType;
            Debug.Assert(declaringType != null);

            Type declaredFieldType = fieldInfo.FieldType;

            DynamicMethod dynamicMethod = CreateGetterMethod(fieldInfo.Name, runtimeFieldType);
            ILGenerator generator = dynamicMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(
                declaringType.IsValueType
                    ? OpCodes.Unbox
                    : OpCodes.Castclass,
                declaringType);
            generator.Emit(OpCodes.Ldfld, fieldInfo);

            // declaredFieldType: Type of the field
            // runtimeFieldType:  <T> of JsonConverter / JsonPropertyInfo

            if (declaredFieldType.IsValueType && declaredFieldType != runtimeFieldType)
            {
                generator.Emit(OpCodes.Box, declaredFieldType);
            }

            generator.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        public override Action<object, TProperty> CreateFieldSetter<TProperty>(FieldInfo fieldInfo) =>
            CreateFieldSetterDelegate<TProperty>(fieldInfo);

        private static Action<object?, TProperty> CreateFieldSetterDelegate<TProperty>(FieldInfo fieldInfo)
        {
            string memberName = fieldInfo.Name;
            Type declaredFieldType = fieldInfo.FieldType;
            Type runtimeFieldType = typeof(TProperty);

            DynamicMethod fieldSetter = CreateFieldSetter(fieldInfo, memberName, declaredFieldType, runtimeFieldType);

            if (declaredFieldType == runtimeFieldType)
            {
                // If declared and runtime type are identical, the assignment cannot fail, thus
                // there is no need for exception handling or closures.

                return CreateDelegate<Action<object?, TProperty>>(fieldSetter);
            }
            else
            {
                // Return a delegate with some closures needed for creating the potential exception.

                Action<object?, TProperty, string, Type> fieldSetterDelegate =
                    CreateDelegate<Action<object?, TProperty, string, Type>>(fieldSetter);

                return (object? obj, TProperty value) => fieldSetterDelegate(obj, value, memberName, declaredFieldType);
            }
        }

        private static DynamicMethod CreateFieldSetter(FieldInfo fieldInfo, string memberName, Type declaredFieldType, Type runtimeFieldType)
        {
            Type? declaringType = fieldInfo.DeclaringType;
            Debug.Assert(declaringType != null);

            // declaredFieldType: Type of the field
            // runtimeFieldType:  <T> of JsonConverter / JsonPropertyInfo

            bool valueNeedsCastOrUnbox = declaredFieldType != runtimeFieldType;

            DynamicMethod dynamicMethod = CreateSetterMethod(memberName, runtimeFieldType, valueNeedsCastOrUnbox);
            ILGenerator generator = dynamicMethod.GetILGenerator();

            if (!valueNeedsCastOrUnbox)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(declaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);
                generator.Emit(OpCodes.Ldarg_1);
            }
            else
            {
                LocalBuilder value = generator.DeclareLocal(declaredFieldType);

                // try
                generator.BeginExceptionBlock();

                // When applied to a reference type, the unbox.any instruction has the same effect as castclass.
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Unbox_Any, declaredFieldType);
                generator.Emit(OpCodes.Stloc_0, value);

                EmitSetterCatchBlocks(generator);

                generator.EndExceptionBlock();

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(declaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);
                generator.Emit(OpCodes.Ldloc_0);
            }

            generator.Emit(OpCodes.Stfld, fieldInfo);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        private static void EmitSetterCatchBlocks(ILGenerator generator)
        {
            // catch (NullReferenceException)
            generator.BeginCatchBlock(s_nullReferenceException);
            generator.Emit(OpCodes.Pop);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Ldarg_3);
            generator.Emit(OpCodes.Call, s_throwHelper_ThrowJsonException_DeserializeUnableToAssignNull);

            // catch (InvalidCastException)
            generator.BeginCatchBlock(s_invalidCastExceptionType);
            generator.Emit(OpCodes.Pop);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, s_object_GetType);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Ldarg_3);
            generator.Emit(OpCodes.Call, s_throwHelper_ThrowJsonException_DeserializeUnableToAssignValue);
        }

        private static DynamicMethod CreateGetterMethod(string memberName, Type memberType) =>
            new DynamicMethod(
                memberName + "Getter",
                memberType,
                new[] { JsonClassInfo.ObjectType },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

        private static DynamicMethod CreateSetterMethod(string memberName, Type memberType, bool valueNeedsCastOrUnbox) =>
            new DynamicMethod(
                memberName + "Setter",
                typeof(void),
                valueNeedsCastOrUnbox ? new[] { JsonClassInfo.ObjectType, memberType, s_stringType, s_typeType } : new[] { JsonClassInfo.ObjectType, memberType },
                typeof(ReflectionEmitMemberAccessor).Module,
                skipVisibility: true);

        [return: NotNullIfNotNull("method")]
        private static T? CreateDelegate<T>(DynamicMethod? method) where T : Delegate =>
            (T?)method?.CreateDelegate(typeof(T));
    }
}
#endif
