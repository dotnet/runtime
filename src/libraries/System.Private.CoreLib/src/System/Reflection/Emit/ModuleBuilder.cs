// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public abstract class ModuleBuilder : Module
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ModuleBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is invoked by derived classes.
        /// </remarks>
        protected ModuleBuilder()
        {
        }

        public void CreateGlobalFunctions()
            => CreateGlobalFunctionsCore();

        /// <summary>
        /// When overridden in a derived class, completes the global function definitions and global data definitions for this dynamic module.
        /// </summary>
        protected abstract void CreateGlobalFunctionsCore();

        public EnumBuilder DefineEnum(string name, TypeAttributes visibility, Type underlyingType)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return DefineEnumCore(name, visibility, underlyingType);
        }

        /// <summary>
        /// When overridden in a derived class, defines an enumeration type that is a value type with a single non-static field called value__ of the specified type.
        /// </summary>
        /// <param name="name">The full path of the enumeration type. <paramref name="name" /> cannot contain embedded nulls.</param>
        /// <param name="visibility">The type attributes for the enumeration visibility. The attributes are any bits defined by <see cref="TypeAttributes.VisibilityMask" />.</param>
        /// <param name="underlyingType">The underlying type for the enumeration. This must be a built-in integer type.</param>
        /// <returns>The defined enumeration.</returns>
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

        /// <summary>
        /// When overridden in a derived class, defines a global method with the specified name, attributes, calling convention,
        /// return type, custom modifiers for the return type, parameter types, and custom modifiers for the parameter types.
        /// </summary>
        /// <param name="name">The name of the method. <paramref name="name" /> cannot contain embedded null characters.</param>
        /// <param name="attributes">The attributes of the method. attributes must include <see cref="MethodAttributes.Static" />.</param>
        /// <param name="callingConvention">The calling convention for the method.</param>
        /// <param name="returnType">The return type of the method.</param>
        /// <param name="requiredReturnTypeCustomModifiers">An array of types representing the required custom modifiers for the return type.</param>
        /// <param name="optionalReturnTypeCustomModifiers">An array of types representing the optional custom modifiers for the return type.</param>
        /// <param name="parameterTypes">The types of the method's parameters.</param>
        /// <param name="requiredParameterTypeCustomModifiers">An array of arrays of types. Each array of types represents the required custom modifiers for the corresponding parameter of the global method.</param>
        /// <param name="optionalParameterTypeCustomModifiers">An array of arrays of types. Each array of types represents the optional custom modifiers for the corresponding parameter of the global method.</param>
        /// <returns>The defined global method.</returns>
        protected abstract MethodBuilder DefineGlobalMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers,
            Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers);

        public FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
            => DefineInitializedDataCore(name, data, attributes);

        /// <summary>
        /// When overridden in a derived class, defines an initialized data field in the .sdata section of the portable executable (PE) file.
        /// </summary>
        /// <param name="name">The name used to refer to the data. <paramref name="name" /> cannot contain embedded nulls.</param>
        /// <param name="data">The binary large object (BLOB) of data.</param>
        /// <param name="attributes">The attributes for the field. The default is <see langword="Static" />.</param>
        /// <returns>A field to reference the data.</returns>
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

        /// <summary>
        /// When overridden in a derived class, defines a <see langword="PInvoke" /> method.
        /// </summary>
        /// <param name="name">The name of the <see langword="PInvoke" /> method. <paramref name="name" /> cannot contain embedded nulls.</param>
        /// <param name="dllName">The name of the DLL in which the <see langword="PInvoke" /> method is defined.</param>
        /// <param name="entryName">The name of the entry point in the DLL.</param>
        /// <param name="attributes">The attributes of the method.</param>
        /// <param name="callingConvention">The method's calling convention.</param>
        /// <param name="returnType">The method's return type.</param>
        /// <param name="parameterTypes">The types of the method's parameters.</param>
        /// <param name="nativeCallConv">The native calling convention.</param>
        /// <param name="nativeCharSet">The method's native character set.</param>
        /// <returns>The defined <see langword="PInvoke" /> method.</returns>
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

        /// <summary>
        /// When overridden in a derived class, constructs a <see cref="TypeBuilder"/>.
        /// </summary>
        /// <param name="name">The full path of the type. <paramref name="name" /> cannot contain embedded nulls.</param>
        /// <param name="attr">The attributes of the defined type.</param>
        /// <param name="parent">The type that the defined type extends.</param>
        /// <param name="interfaces">The list of interfaces that the type implements.</param>
        /// <param name="packingSize">The packing size of the type.</param>
        /// <param name="typesize">The total size of the type.</param>
        /// <returns>A <see cref="TypeBuilder"/> created with all of the requested attributes.</returns>
        protected abstract TypeBuilder DefineTypeCore(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces, PackingSize packingSize, int typesize);

        public FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
            => DefineUninitializedDataCore(name, size, attributes);

        /// <summary>
        /// When overridden in a derived class, defines an uninitialized data field in the .sdata section of the portable executable (PE) file.
        /// </summary>
        /// <param name="name">The name used to refer to the data. <paramref name="name" /> cannot contain embedded nulls.</param>
        /// <param name="size">The size of the data field.</param>
        /// <param name="attributes">The attributes for the field.</param>
        /// <returns>A field to reference the data.</returns>
        protected abstract FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes);

        public MethodInfo GetArrayMethod(Type arrayClass, string methodName, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes)
        {
            ArgumentNullException.ThrowIfNull(arrayClass);
            ArgumentException.ThrowIfNullOrEmpty(methodName);

            return GetArrayMethodCore(arrayClass, methodName, callingConvention, returnType, parameterTypes);
        }

        /// <summary>
        /// When overridden in a derived class, returns the named method on an array class.
        /// </summary>
        /// <param name="arrayClass">An array class.</param>
        /// <param name="methodName">The name of a method on the array class.</param>
        /// <param name="callingConvention">The method's calling convention.</param>
        /// <param name="returnType">The return type of the method.</param>
        /// <param name="parameterTypes">The types of the method's parameters.</param>
        /// <returns>The named method on an array class.</returns>
        protected abstract MethodInfo GetArrayMethodCore(Type arrayClass, string methodName,
            CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes);

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);

            SetCustomAttributeCore(con, binaryAttribute);
        }

        /// <summary>
        /// When overridden in a derived class, sets a custom attribute on this assembly.
        /// </summary>
        /// <param name="con">The constructor for the custom attribute.</param>
        /// <param name="binaryAttribute">A <see cref="ReadOnlySpan{T}"/> of bytes representing the attribute.</param>
        protected abstract void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute);

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            SetCustomAttributeCore(customBuilder.Ctor, customBuilder.Data);
        }

        /// <summary>
        /// When overridden in a derived class, returns the metadata token for the given <see cref="Type"/> relative to the Module.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> for which to retrieve the token.</param>
        /// <returns>An integer representing the metadata token.</returns>
        /// <remarks>
        /// Tokens are used to identify objects when the objects are used in IL instructions. Tokens are always relative to the Module.
        /// </remarks>
        public abstract int GetTypeMetadataToken(Type type);

        /// <summary>
        /// When overridden in a derived class, returns the metadata token for the given <see cref="FieldInfo"/> relative to the Module.
        /// </summary>
        /// <param name="field">The <see cref="FieldInfo"/> for which to retrieve the token.</param>
        /// <returns>An integer representing the metadata token.</returns>
        /// <remarks>
        /// Tokens are used to identify objects when the objects are used in IL instructions. Tokens are always relative to the Module.
        /// </remarks>
        public abstract int GetFieldMetadataToken(FieldInfo field);

        /// <summary>
        /// When overridden in a derived class, returns the metadata token for the given <see cref="MethodInfo"/> relative to the Module.
        /// </summary>
        /// <param name="method">The <see cref="MethodInfo"/> for which to retrieve the token.</param>
        /// <returns>An integer representing the metadata token.</returns>
        /// <remarks>
        /// Tokens are used to identify objects when the objects are used in IL instructions. Tokens are always relative to the Module.
        /// </remarks>
        public abstract int GetMethodMetadataToken(MethodInfo method);

        /// <summary>
        /// When overridden in a derived class, returns the metadata token for the given <see cref="ConstructorInfo"/> relative to the Module.
        /// </summary>
        /// <param name="constructor">The <see cref="ConstructorInfo"/> for which to retrieve the token.</param>
        /// <returns>An integer representing the metadata token.</returns>
        /// <remarks>
        /// Tokens are used to identify objects when the objects are used in IL instructions. Tokens are always relative to the Module.
        /// </remarks>
        public abstract int GetMethodMetadataToken(ConstructorInfo constructor);

        /// <summary>
        /// When overridden in a derived class, returns the metadata token for the given <see cref="SignatureHelper"/> relative to the Module.
        /// </summary>
        /// <param name="signature">The <see cref="SignatureHelper"/> for which to retrieve the token.</param>
        /// <returns>An integer representing the metadata token.</returns>
        /// <remarks>
        /// Tokens are used to identify objects when the objects are used in IL instructions. Tokens are always relative to the Module.
        /// </remarks>
        public abstract int GetSignatureMetadataToken(SignatureHelper signature);

        /// <summary>
        /// When overridden in a derived class, returns the metadata token for the given <see cref="string"/> constant relative to the Module.
        /// </summary>
        /// <param name="stringConstant">The <see cref="string"/> constant for which to retrieve the token.</param>
        /// <returns>An integer representing the metadata token.</returns>
        /// <remarks>
        /// Tokens are used to identify objects when the objects are used in IL instructions. Tokens are always relative to the Module.
        /// </remarks>
        public abstract int GetStringMetadataToken(string stringConstant);
    }
}
