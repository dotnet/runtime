// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file makes NetStandard Reflection's "subclassing" surface area look as much like NetCore as possible so the rest of the code can be written without #if's.

namespace System.Reflection.TypeLoading
{
    internal static class NetCoreApiEmulators
    {
        // On NetCore, call the real thing.

        public static bool IsSignatureType(this Type type) => type.IsSignatureType;
        public static bool IsSZArray(this Type type) => type.IsSZArray;
        public static bool IsVariableBoundArray(this Type type) => type.IsVariableBoundArray;
        public static bool IsGenericMethodParameter(this Type type) => type.IsGenericMethodParameter;
        public static Type MakeSignatureGenericType(this Type genericTypeDefinition, Type[] typeArguments) => Type.MakeGenericSignatureType(genericTypeDefinition, typeArguments);
    }

    /// <summary>
    /// Another layer of base types.
    /// For pre-8.0 NetCore, these base types add newer NetCore apis.
    /// </summary>
    internal abstract class LeveledTypeInfo : TypeInfo
    {
        protected LeveledTypeInfo() : base() { }

        public abstract bool IsFunctionPointer { get; }
        public abstract bool IsUnmanagedFunctionPointer { get; }
        public abstract Type[] GetFunctionPointerCallingConventions();
        public abstract Type[] GetFunctionPointerParameterTypes();
        public abstract Type GetFunctionPointerReturnType();
        public abstract Type[] GetOptionalCustomModifiers();
        public abstract Type[] GetRequiredCustomModifiers();
    }

    internal abstract class LeveledAssembly : Assembly
    {
    }

    internal abstract class LeveledConstructorInfo : ConstructorInfo
    {
    }

    internal abstract class LeveledMethodInfo : MethodInfo
    {
    }

    internal abstract class LeveledEventInfo : EventInfo
    {
    }

    internal abstract class LeveledFieldInfo : FieldInfo
    {
        public abstract Type GetModifiedFieldType();
    }

    internal abstract class LeveledParameterInfo : ParameterInfo
    {
        public abstract Type GetModifiedParameterType();
    }

    internal abstract class LeveledPropertyInfo : PropertyInfo
    {
        public abstract Type GetModifiedPropertyType();
    }

    internal abstract class LeveledCustomAttributeData : CustomAttributeData
    {
    }
}
