// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public abstract class ModuleBuilder : Module
    {
        protected ModuleBuilder()
        {
        }

        public void CreateGlobalFunctions()
            => CreateGlobalFunctionsCore();

        protected abstract void CreateGlobalFunctionsCore();

        public EnumBuilder DefineEnum(string name, TypeAttributes visibility, Type underlyingType)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return DefineEnumCore(name, visibility, underlyingType);
        }

        protected abstract EnumBuilder DefineEnumCore(string name, TypeAttributes visibility, Type underlyingType);

        public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, Type? returnType, Type[]? parameterTypes)
            => DefineGlobalMethod(name, attributes, CallingConventions.Standard, returnType, null, null, parameterTypes, null, null);

        public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes)
                => DefineGlobalMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);

        public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers,
            Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return DefineGlobalMethodCore(name, attributes, callingConvention,
                   returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers,
                   parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
        }

        protected abstract MethodBuilder DefineGlobalMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers,
            Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers);

        public FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
            => DefineInitializedDataCore(name, data, attributes);

        protected abstract FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes);

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public MethodBuilder DefinePInvokeMethod(string name, string dllName,
            MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
                => DefinePInvokeMethod(name, dllName, name, attributes, callingConvention,
                    returnType, parameterTypes, nativeCallConv, nativeCharSet);

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName,
            MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
                => DefinePInvokeMethodCore(name, dllName, entryName, attributes, callingConvention,
                    returnType, parameterTypes, nativeCallConv, nativeCharSet);

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        protected abstract MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName,
            MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet);

        public TypeBuilder DefineType(string name)
            => DefineType(name, TypeAttributes.NotPublic, null, null);

        public TypeBuilder DefineType(string name, TypeAttributes attr)
            => DefineType(name, attr, null, null);

        public TypeBuilder DefineType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
                => DefineType(name, attr, parent, null);

        public TypeBuilder DefineType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return DefineTypeCore(name, attr, parent, interfaces, PackingSize.Unspecified, TypeBuilder.UnspecifiedTypeSize);
        }

        public TypeBuilder DefineType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, int typesize)
                => DefineType(name, attr, parent, PackingSize.Unspecified, typesize);

        public TypeBuilder DefineType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packsize)
                => DefineType(name, attr, parent, packsize, TypeBuilder.UnspecifiedTypeSize);

        public TypeBuilder DefineType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packingSize, int typesize)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return DefineTypeCore(name, attr, parent, null, packingSize, typesize);
        }

        protected abstract TypeBuilder DefineTypeCore(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces, PackingSize packingSize, int typesize);

        public FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
            => DefineUninitializedDataCore(name, size, attributes);

        protected abstract FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes);

        public MethodInfo GetArrayMethod(Type arrayClass, string methodName, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes)
        {
            ArgumentNullException.ThrowIfNull(arrayClass);
            ArgumentException.ThrowIfNullOrEmpty(methodName);

            return GetArrayMethodCore(arrayClass, methodName, callingConvention, returnType, parameterTypes);
        }

        protected abstract MethodInfo GetArrayMethodCore(Type arrayClass, string methodName,
            CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes);

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);

            SetCustomAttributeCore(con, binaryAttribute);
        }

        protected abstract void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute);

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            SetCustomAttributeCore(customBuilder.Ctor, customBuilder.Data);
        }

        public abstract int GetTypeMetadataToken(Type type);
        public abstract int GetFieldMetadataToken(FieldInfo field);
        public abstract int GetMethodMetadataToken(MethodInfo method);
        public abstract int GetMethodMetadataToken(ConstructorInfo constructor);
        public abstract int GetSignatureMetadataToken(SignatureHelper signature);
        public abstract int GetStringMetadataToken(string stringConstant);
    }
}
