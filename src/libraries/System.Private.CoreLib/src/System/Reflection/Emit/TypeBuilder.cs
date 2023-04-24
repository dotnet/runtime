// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public abstract partial class TypeBuilder : TypeInfo
    {
        protected TypeBuilder()
        {
        }

        public const int UnspecifiedTypeSize = 0;

        public PackingSize PackingSize
            => PackingSizeCore;

        protected abstract PackingSize PackingSizeCore { get; }

        public int Size
            => SizeCore;

        protected abstract int SizeCore { get; }

        public void AddInterfaceImplementation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
        {
            ArgumentNullException.ThrowIfNull(interfaceType);

            AddInterfaceImplementationCore(interfaceType);
        }

        protected abstract void AddInterfaceImplementationCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type CreateType()
            => CreateTypeInfo();

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public TypeInfo CreateTypeInfo()
            => CreateTypeInfoCore();

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        protected abstract TypeInfo CreateTypeInfoCore();

        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes)
            => DefineConstructor(attributes, callingConvention, parameterTypes, null, null);

        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention,
            Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
                => DefineConstructorCore(attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);

        protected abstract ConstructorBuilder DefineConstructorCore(MethodAttributes attributes, CallingConventions callingConvention,
            Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers);

        public ConstructorBuilder DefineDefaultConstructor(MethodAttributes attributes)
            => DefineDefaultConstructorCore(attributes);

        protected abstract ConstructorBuilder DefineDefaultConstructorCore(MethodAttributes attributes);

        public EventBuilder DefineEvent(string name, EventAttributes attributes, Type eventtype)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return DefineEventCore(name, attributes, eventtype);
        }

        protected abstract EventBuilder DefineEventCore(string name, EventAttributes attributes, Type eventtype);

        public FieldBuilder DefineField(string fieldName, Type type, FieldAttributes attributes)
            => DefineField(fieldName, type, null, null, attributes);

        public FieldBuilder DefineField(string fieldName, Type type, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers,
            FieldAttributes attributes)
        {
            ArgumentException.ThrowIfNullOrEmpty(fieldName);
            ArgumentNullException.ThrowIfNull(type);

            return DefineFieldCore(fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);
        }

        protected abstract FieldBuilder DefineFieldCore(string fieldName, Type type, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers,
            FieldAttributes attributes);

        public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
        {
            ArgumentNullException.ThrowIfNull(names);
            if (names.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyArray, nameof(names));

            return DefineGenericParametersCore(names);
        }

        protected abstract GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names);

        public FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
        {
            ArgumentNullException.ThrowIfNull(data);

            return DefineInitializedDataCore(name, data, attributes);
        }

        protected abstract FieldBuilder DefineInitializedDataCore(string name, byte[] data, FieldAttributes attributes);

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes)
            => DefineMethod(name, attributes, CallingConventions.Standard, null, null);

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention)
            => DefineMethod(name, attributes, callingConvention, null, null);

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes)
                => DefineMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, Type? returnType, Type[]? parameterTypes)
            => DefineMethod(name, attributes, CallingConventions.Standard, returnType, parameterTypes);

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (parameterTypes != null)
            {
                if (parameterTypeOptionalCustomModifiers != null && parameterTypeOptionalCustomModifiers.Length != parameterTypes.Length)
                    throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, nameof(parameterTypeOptionalCustomModifiers), nameof(parameterTypes)));

                if (parameterTypeRequiredCustomModifiers != null && parameterTypeRequiredCustomModifiers.Length != parameterTypes.Length)
                    throw new ArgumentException(SR.Format(SR.Argument_MismatchedArrays, nameof(parameterTypeRequiredCustomModifiers), nameof(parameterTypes)));
            }

            return DefineMethodCore(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                    parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
        }

        protected abstract MethodBuilder DefineMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers);

        public void DefineMethodOverride(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        {
            ArgumentNullException.ThrowIfNull(methodInfoBody);
            ArgumentNullException.ThrowIfNull(methodInfoDeclaration);

            DefineMethodOverrideCore(methodInfoBody, methodInfoDeclaration);
        }

        protected abstract void DefineMethodOverrideCore(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration);

        public TypeBuilder DefineNestedType(string name)
            => DefineNestedType(name, TypeAttributes.NestedPrivate, null, null);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr)
            => DefineNestedType(name, attr, null, null);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
                => DefineNestedType(name, attr, parent, null);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return DefineNestedTypeCore(name, attr, parent, interfaces, PackingSize.Unspecified, UnspecifiedTypeSize);
        }

        protected abstract TypeBuilder DefineNestedTypeCore(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces, PackingSize packSize, int typeSize);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, int typeSize)
                => DefineNestedType(name, attr, parent, PackingSize.Unspecified, typeSize);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packSize)
                => DefineNestedType(name, attr, parent, packSize, UnspecifiedTypeSize);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packSize, int typeSize)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return DefineNestedTypeCore(name, attr, parent, null, packSize, typeSize);
        }

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public MethodBuilder DefinePInvokeMethod(string name, string dllName, MethodAttributes attributes,
            CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes,
            CallingConvention nativeCallConv, CharSet nativeCharSet)
                => DefinePInvokeMethod(name, dllName, name, attributes, callingConvention, returnType, null, null,
                    parameterTypes, null, null, nativeCallConv, nativeCharSet);

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes,
            CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes,
            CallingConvention nativeCallConv, CharSet nativeCharSet)
                => DefinePInvokeMethod(
                    name, dllName, entryName, attributes, callingConvention, returnType, null, null,
                    parameterTypes, null, null, nativeCallConv, nativeCharSet);

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes,
            CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers,
            CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentException.ThrowIfNullOrEmpty(dllName);
            ArgumentException.ThrowIfNullOrEmpty(entryName);

            return DefinePInvokeMethodCore(name, dllName, entryName, attributes, callingConvention,
                    returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                    parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers,
                    nativeCallConv, nativeCharSet);
        }

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        protected abstract MethodBuilder DefinePInvokeMethodCore(string name, string dllName, string entryName, MethodAttributes attributes,
            CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers,
            CallingConvention nativeCallConv, CharSet nativeCharSet);

        public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, Type returnType, Type[]? parameterTypes)
            => DefineProperty(name, attributes, returnType, null, null, parameterTypes, null, null);

        public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes,
            CallingConventions callingConvention, Type returnType, Type[]? parameterTypes)
             => DefineProperty(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);

        public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes,
            Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
                => DefineProperty(name, attributes, default,
                    returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                    parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);

        public PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, CallingConventions callingConvention,
            Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return DefinePropertyCore(name, attributes, callingConvention,
                    returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                    parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);
        }

        protected abstract PropertyBuilder DefinePropertyCore(string name, PropertyAttributes attributes, CallingConventions callingConvention,
            Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers);

        public ConstructorBuilder DefineTypeInitializer()
            => DefineTypeInitializerCore();

        protected abstract ConstructorBuilder DefineTypeInitializerCore();

        public FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
            => DefineUninitializedDataCore(name, size, attributes);

        protected abstract FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes);

        public bool IsCreated()
            => IsCreatedCore();

        protected abstract bool IsCreatedCore();

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);

            SetCustomAttributeCore(con, binaryAttribute);
        }

        protected abstract void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute);

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            SetCustomAttributeCore(customBuilder);
        }

        protected abstract void SetCustomAttributeCore(CustomAttributeBuilder customBuilder);

        public void SetParent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
            => SetParentCore(parent);

        protected abstract void SetParentCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent);
    }
}
