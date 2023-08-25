// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public abstract partial class TypeBuilder : TypeInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TypeBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is invoked by derived classes.
        /// </remarks>
        protected TypeBuilder()
        {
        }

        public const int UnspecifiedTypeSize = 0;

        public PackingSize PackingSize
            => PackingSizeCore;

        /// <summary>
        /// When overridden in a derived class, retrieves the packing size of this type.
        /// </summary>
        /// <value>Read-only. Retrieves the packing size of this type.</value>
        protected abstract PackingSize PackingSizeCore { get; }

        public int Size
            => SizeCore;

        /// <summary>
        /// When overridden in a derived class, retrieves the total size of a type.
        /// </summary>
        /// <value>Read-only. Retrieves this type's total size.</value>
        protected abstract int SizeCore { get; }

        public void AddInterfaceImplementation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
        {
            ArgumentNullException.ThrowIfNull(interfaceType);

            AddInterfaceImplementationCore(interfaceType);
        }

        /// <summary>
        /// When overridden in a derived class, adds an interface that this type implements.
        /// </summary>
        /// <param name="interfaceType">The interface that this type implements.</param>
        protected abstract void AddInterfaceImplementationCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type CreateType()
            => CreateTypeInfo();

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public TypeInfo CreateTypeInfo()
            => CreateTypeInfoCore();

        /// <summary>
        /// When overridden in a derived class, gets a <see cref="TypeInfo"/> object that represents this type.
        /// </summary>
        /// <returns>A <see cref="TypeInfo"/> object that represents this type.</returns>
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        protected abstract TypeInfo CreateTypeInfoCore();

        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes)
            => DefineConstructor(attributes, callingConvention, parameterTypes, null, null);

        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention,
            Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
                => DefineConstructorCore(attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);

        /// <summary>
        /// When overridden in a derived class, adds a new constructor to the type, with the given attributes, signature, and custom modifiers.
        /// </summary>
        /// <param name="attributes">The attributes of the constructor.</param>
        /// <param name="callingConvention">The calling convention of the constructor.</param>
        /// <param name="parameterTypes">The parameter types of the constructor.</param>
        /// <param name="requiredCustomModifiers">An array of arrays of types. Each array of types represents the required custom modifiers for the corresponding parameter.</param>
        /// <param name="optionalCustomModifiers">An array of arrays of types. Each array of types represents the optional custom modifiers for the corresponding parameter.</param>
        /// <returns>The defined constructor.</returns>
        protected abstract ConstructorBuilder DefineConstructorCore(MethodAttributes attributes, CallingConventions callingConvention,
            Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers);

        public ConstructorBuilder DefineDefaultConstructor(MethodAttributes attributes)
            => DefineDefaultConstructorCore(attributes);

        /// <summary>
        /// When overridden in a derived class, defines the parameterless constructor. The constructor defined here will simply call the parameterless constructor of the parent.
        /// </summary>
        /// <param name="attributes">A <see cref="MethodAttributes"/> object representing the attributes to be applied to the constructor.</param>
        /// <returns>Returns the constructor.</returns>
        protected abstract ConstructorBuilder DefineDefaultConstructorCore(MethodAttributes attributes);

        public EventBuilder DefineEvent(string name, EventAttributes attributes, Type eventtype)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return DefineEventCore(name, attributes, eventtype);
        }

        /// <summary>
        /// When overridden in a derived class, adds a new event to the type, with the given name, attributes and event type.
        /// </summary>
        /// <param name="name">The name of the event. <paramref name="name" /> cannot contain embedded nulls.</param>
        /// <param name="attributes">The attributes of the event.</param>
        /// <param name="eventtype">The type of the event.</param>
        /// <returns>The defined event.</returns>
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

        /// <summary>
        /// When overridden in a derived class, adds a new field to the type, with the given name, attributes, field type, and custom modifiers.
        /// </summary>
        /// <param name="fieldName">The name of the field. <paramref name="fieldName"/> cannot contain embedded nulls.</param>
        /// <param name="type">The type of the field.</param>
        /// <param name="requiredCustomModifiers">An array of types representing the required custom modifiers for the field.</param>
        /// <param name="optionalCustomModifiers">An array of types representing the optional custom modifiers for the field.</param>
        /// <param name="attributes">The attributes of the field.</param>
        /// <returns>The defined field.</returns>
        protected abstract FieldBuilder DefineFieldCore(string fieldName, Type type, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers,
            FieldAttributes attributes);

        public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
        {
            ArgumentNullException.ThrowIfNull(names);
            if (names.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyArray, nameof(names));

            return DefineGenericParametersCore(names);
        }

        /// <summary>
        /// When overridden in a derived class, defines the generic type parameters for the current type, specifying their number and their names.
        /// </summary>
        /// <param name="names">An array of names for the generic type parameters.</param>
        /// <returns>An array of <see cref="GenericTypeParameterBuilder"/> objects that can be used to define the constraints of the generic type parameters for the current type.</returns>
        protected abstract GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names);

        public FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
        {
            ArgumentNullException.ThrowIfNull(data);

            return DefineInitializedDataCore(name, data, attributes);
        }

        /// <summary>
        /// When overridden in a derived class, defines initialized data field in the .sdata section of the portable executable (PE) file.
        /// </summary>
        /// <param name="name">The name used to refer to the data. <paramref name="name" /> cannot contain embedded nulls</param>
        /// <param name="data">The blob of data.</param>
        /// <param name="attributes">The attributes for the field.</param>
        /// <returns>A field to reference the data.</returns>
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

        /// <summary>
        /// When overridden in a derived class, adds a new method to the type, with the specified name, method attributes, calling convention, method signature, and custom modifiers.
        /// </summary>
        /// <param name="name">The name of the method. <paramref name="name" /> cannot contain embedded nulls.</param>
        /// <param name="attributes">The attributes of the method.</param>
        /// <param name="callingConvention">The calling convention of the method.</param>
        /// <param name="returnType">The return type of the method.</param>
        /// <param name="returnTypeRequiredCustomModifiers">An array of types representing the required custom modifiers.</param>
        /// <param name="returnTypeOptionalCustomModifiers">An array of types representing the optional custom modifiers.</param>
        /// <param name="parameterTypes">The types of the parameters of the method.</param>
        /// <param name="parameterTypeRequiredCustomModifiers">An array of arrays of types. Each array of types represents the required custom modifiers for the corresponding parameter.</param>
        /// <param name="parameterTypeOptionalCustomModifiers">An array of arrays of types. Each array of types represents the optional custom modifiers for the corresponding parameter.</param>
        /// <returns>A <see cref="MethodBuilder"/> object representing the newly added method.</returns>
        protected abstract MethodBuilder DefineMethodCore(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers);

        public void DefineMethodOverride(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
        {
            ArgumentNullException.ThrowIfNull(methodInfoBody);
            ArgumentNullException.ThrowIfNull(methodInfoDeclaration);

            DefineMethodOverrideCore(methodInfoBody, methodInfoDeclaration);
        }

        /// <summary>
        /// When overridden in a derived class, specifies a given method body that implements a given method declaration, potentially with a different name.
        /// </summary>
        /// <param name="methodInfoBody">The method body to be used. This should be a <see cref="MethodBuilder"/> object.</param>
        /// <param name="methodInfoDeclaration">The method whose declaration is to be used.</param>
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

        /// <summary>
        /// When overridden in a derived class, defines a nested type, given its name, attributes, size, and the type that it extends.
        /// </summary>
        /// <param name="name">The short name of the type. <paramref name="name" /> cannot contain embedded null values.</param>
        /// <param name="attr">The attributes of the type.</param>
        /// <param name="parent">The type that the nested type extends.</param>
        /// <param name="interfaces">The interfaces that the nested type implements.</param>
        /// <param name="packSize">The packing size of the type.</param>
        /// <param name="typeSize">The total size of the type.</param>
        /// <returns>The defined nested type.</returns>
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

        /// <summary>
        /// When overridden in a derived class, defines a PInvoke method with the provided name, DLL name, entry point name, attributes,
        /// calling convention, return type, types of the parameters, PInvoke flags, and custom modifiers for the parameters and return type.
        /// </summary>
        /// <param name="name">The name of the <see langword="PInvoke" /> method. <paramref name="name" /> cannot contain embedded nulls.</param>
        /// <param name="dllName">The name of the DLL in which the <see langword="PInvoke" /> method is defined.</param>
        /// <param name="entryName">The name of the entry point in the DLL.</param>
        /// <param name="attributes">The attributes of the method.</param>
        /// <param name="callingConvention">The method's calling convention.</param>
        /// <param name="returnType">The method's return type.</param>
        /// <param name="returnTypeRequiredCustomModifiers">An array of types representing the required custom modifiers</param>
        /// <param name="returnTypeOptionalCustomModifiers">An array of types representing the optional custom modifiers</param>
        /// <param name="parameterTypes">The types of the method's parameters.</param>
        /// <param name="parameterTypeRequiredCustomModifiers">An array of arrays of types. Each array of types represents the required custom modifiers for the corresponding parameter.</param>
        /// <param name="parameterTypeOptionalCustomModifiers">An array of arrays of types. Each array of types represents the optional custom modifiers for the corresponding parameter.</param>
        /// <param name="nativeCallConv">The native calling convention.</param>
        /// <param name="nativeCharSet">The method's native character set.</param>
        /// <returns>A <see cref="MethodBuilder"/> representing the defined <see langword="PInvoke" /> method.</returns>
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

        /// <summary>
        /// When overridden in a derived class, adds a new property to the type, with the given name, calling convention, property signature, and custom modifiers.
        /// </summary>
        /// <param name="name">The name of the property. <paramref name="name" /> cannot contain embedded nulls.</param>
        /// <param name="attributes">The attributes of the property.</param>
        /// <param name="callingConvention">The calling convention of the property accessors.</param>
        /// <param name="returnType">The return type of the property.</param>
        /// <param name="returnTypeRequiredCustomModifiers">An array of types representing the required custom modifiers</param>
        /// <param name="returnTypeOptionalCustomModifiers">An array of types representing the optional custom modifiers</param>
        /// <param name="parameterTypes">The types of the method's parameters.</param>
        /// <param name="parameterTypeRequiredCustomModifiers">An array of arrays of types. Each array of types represents the required custom modifiers for the corresponding parameter.</param>
        /// <param name="parameterTypeOptionalCustomModifiers">An array of arrays of types. Each array of types represents the optional custom modifiers for the corresponding parameter.</param>
        /// <returns>The defined property.</returns>
        protected abstract PropertyBuilder DefinePropertyCore(string name, PropertyAttributes attributes, CallingConventions callingConvention,
            Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers);

        public ConstructorBuilder DefineTypeInitializer()
            => DefineTypeInitializerCore();

        /// <summary>
        /// When overridden in a derived class, defines the initializer for this type.
        /// </summary>
        /// <returns>Returns a type initializer.</returns>
        protected abstract ConstructorBuilder DefineTypeInitializerCore();

        public FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
            => DefineUninitializedDataCore(name, size, attributes);

        /// <summary>
        /// When overridden in a derived class, defines an uninitialized data field in the <see langword=".sdata" /> section of the portable executable (PE) file.
        /// </summary>
        /// <param name="name">The name used to refer to the data. <paramref name="name" /> cannot contain embedded nulls.</param>
        /// <param name="size">The size of the data field.</param>
        /// <param name="attributes">The attributes for the field.</param>
        /// <returns>A field to reference the data.</returns>
        protected abstract FieldBuilder DefineUninitializedDataCore(string name, int size, FieldAttributes attributes);

        public bool IsCreated()
            => IsCreatedCore();

        /// <summary>
        /// When overridden in a derived class, returns a value that indicates whether the current dynamic type has been created.
        /// </summary>
        /// <returns><see langword="true"/> if the CreateType() method has been called; otherwise, <see langword="false"/>.</returns>
        protected abstract bool IsCreatedCore();

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

        public void SetParent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
            => SetParentCore(parent);

        /// <summary>
        /// When overridden in a derived class, sets the base type of the type currently under construction.
        /// </summary>
        /// <param name="parent">The new base type.</param>
        protected abstract void SetParentCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent);

        public override Type MakePointerType()
        {
            return SymbolType.FormCompoundType("*", this, 0)!;
        }

        public override Type MakeByRefType()
        {
            return SymbolType.FormCompoundType("&", this, 0)!;
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType()
        {
            return SymbolType.FormCompoundType("[]", this, 0)!;
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType(int rank)
        {
            string s = GetRankString(rank);
            return SymbolType.FormCompoundType(s, this, 0)!;
        }

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override Type MakeGenericType(params Type[] typeArguments)
        {
            return TypeBuilderInstantiation.MakeGenericType(this, typeArguments);
        }
    }
}
