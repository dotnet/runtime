// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public abstract class TypeBuilder : TypeInfo
    {
        protected TypeBuilder()
        {
        }

        public const int UnspecifiedTypeSize = 0;

        public virtual PackingSize PackingSize
            => PackingSize;

        public virtual int Size
            => Size;

        public virtual void AddInterfaceImplementation([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type interfaceType)
            => AddInterfaceImplementation(interfaceType);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public Type? CreateType()
            => CreateTypeInfo();

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public virtual TypeInfo? CreateTypeInfo()
            => CreateTypeInfo();

        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes)
            => DefineConstructor(attributes, callingConvention, parameterTypes, null, null);

        public virtual ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention,
            Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers)
                => DefineConstructor(attributes, callingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);

        public virtual ConstructorBuilder DefineDefaultConstructor(MethodAttributes attributes)
            => DefineDefaultConstructor(attributes);

        public virtual EventBuilder DefineEvent(string name, EventAttributes attributes, Type eventtype)
            => DefineEvent(name, attributes, eventtype);

        public FieldBuilder DefineField(string fieldName, Type type, FieldAttributes attributes)
            => DefineField(fieldName, type, null, null, attributes);

        public virtual FieldBuilder DefineField(string fieldName, Type type, Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers,
            FieldAttributes attributes)
                => DefineField(fieldName, type, requiredCustomModifiers, optionalCustomModifiers, attributes);

        public virtual GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
            => DefineGenericParameters(names);

        public virtual FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
            => DefineInitializedData(name, data, attributes);

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes)
            => DefineMethod(name, attributes, CallingConventions.Standard, null, null);

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention)
            => DefineMethod(name, attributes, callingConvention, null, null);

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes)
                => DefineMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, Type returnType, Type[] parameterTypes)
            => DefineMethod(name, attributes, CallingConventions.Standard, returnType, parameterTypes);

        public virtual MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
                => DefineMethod(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                    parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);

        public virtual void DefineMethodOverride(MethodInfo methodInfoBody, MethodInfo methodInfoDeclaration)
            => DefineMethodOverride(methodInfoBody, methodInfoDeclaration);

        public TypeBuilder DefineNestedType(string name)
            => DefineNestedType(name, TypeAttributes.NestedPrivate, null, null);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr)
            => DefineNestedType(name, attr, null, null);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
                => DefineNestedType(name, attr, parent, null);

        public virtual TypeBuilder DefineNestedType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces)
                => DefineNestedType(name, attr, parent, interfaces);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, int typeSize)
                => DefineNestedType(name, attr, parent, PackingSize.Unspecified, typeSize);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packSize)
                => DefineNestedType(name, attr, parent, packSize, TypeBuilder.UnspecifiedTypeSize);

        public virtual TypeBuilder DefineNestedType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packSize, int typeSize)
                => DefineNestedType(name, attr, parent, packSize, typeSize);

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
        public virtual MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes,
            CallingConventions callingConvention,
            Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers,
            CallingConvention nativeCallConv, CharSet nativeCharSet)
                => DefinePInvokeMethod(name, dllName, entryName, attributes, callingConvention,
                    returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                    parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers,
                    nativeCallConv, nativeCharSet);

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

        public virtual PropertyBuilder DefineProperty(string name, PropertyAttributes attributes, CallingConventions callingConvention,
            Type returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
                => DefineProperty(name, attributes, callingConvention,
                    returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                    parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);

        public virtual ConstructorBuilder DefineTypeInitializer()
            => DefineTypeInitializer();

        public virtual FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
            => DefineUninitializedData(name, size, attributes);

        public static ConstructorInfo GetConstructor(Type type, ConstructorInfo constructor)
        {
            Module? module = type?.Module;
            if (module is not ModuleBuilder moduleBuilder)
                throw new ArgumentException(SR.Argument_MustBeTypeBuilder, nameof(type));

            return moduleBuilder.GetConstructor(type!, constructor);
        }

        public static FieldInfo GetField(Type type, FieldInfo field)
        {
            Module? module = type?.Module;
            if (module is not ModuleBuilder moduleBuilder)
                throw new ArgumentException(SR.Argument_MustBeTypeBuilder, nameof(type));

            return moduleBuilder.GetField(type!, field);
        }

        public static MethodInfo GetMethod(Type type, MethodInfo method)
        {
            Module? module = type?.Module;
            if (module is not ModuleBuilder moduleBuilder)
                throw new ArgumentException(SR.Argument_MustBeTypeBuilder, nameof(type));

            return moduleBuilder.GetMethod(type!, method);
        }

        public virtual bool IsCreated()
            => IsCreated();

        public virtual void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
            => SetCustomAttribute(con, binaryAttribute);

        public virtual void SetCustomAttribute(CustomAttributeBuilder customBuilder)
            => SetCustomAttribute(customBuilder);

        public virtual void SetParent([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent)
            => SetParent(parent);
    }
}
