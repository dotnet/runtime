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

        // The following virtual methods are abstract in reference assembly. We keep them as virtual to maintain backward compatibility.

        public virtual void CreateGlobalFunctions()
            => CreateGlobalFunctions();

        public virtual EnumBuilder DefineEnum(string name, TypeAttributes visibility, Type underlyingType)
            => DefineEnum(name, visibility, underlyingType);

        public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, Type? returnType, Type[]? parameterTypes)
            => DefineGlobalMethod(name, attributes, CallingConventions.Standard, returnType, null, null, parameterTypes, null, null);

        public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes)
                => DefineGlobalMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);

        public virtual MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? requiredReturnTypeCustomModifiers, Type[]? optionalReturnTypeCustomModifiers,
            Type[]? parameterTypes, Type[][]? requiredParameterTypeCustomModifiers, Type[][]? optionalParameterTypeCustomModifiers)
                => DefineGlobalMethod(name, attributes, callingConvention,
                    returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers,
                    parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);

        public virtual FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
            => DefineInitializedData(name, data, attributes);

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public MethodBuilder DefinePInvokeMethod(string name, string dllName,
            MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
                => DefinePInvokeMethod(name, dllName, name, attributes, callingConvention,
                    returnType, parameterTypes, nativeCallConv, nativeCharSet);

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public virtual MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName,
            MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
                => DefinePInvokeMethod(name, dllName, entryName, attributes, callingConvention,
                    returnType, parameterTypes, nativeCallConv, nativeCharSet);

        public TypeBuilder DefineType(string name)
            => DefineType(name, TypeAttributes.NotPublic, null, null);

        public TypeBuilder DefineType(string name, TypeAttributes attr)
            => DefineType(name, attr, null, null);

        public TypeBuilder DefineType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
                => DefineType(name, attr, parent, null);

        public virtual TypeBuilder DefineType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, Type[]? interfaces)
                => DefineType(name, attr, parent, interfaces);

        public TypeBuilder DefineType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, int typesize)
                => DefineType(name, attr, parent, PackingSize.Unspecified, typesize);

        public TypeBuilder DefineType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packsize)
                => DefineType(name, attr, parent, packsize, TypeBuilder.UnspecifiedTypeSize);

        public virtual TypeBuilder DefineType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packingSize, int typesize)
                => DefineType(name, attr, parent, packingSize, typesize);

        public virtual FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
            => DefineUninitializedData(name, size, attributes);

        public virtual MethodInfo GetArrayMethod(Type arrayClass, string methodName, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes)
                => GetArrayMethod(arrayClass, methodName, callingConvention, returnType, parameterTypes);

        public virtual void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
            => SetCustomAttribute(con, binaryAttribute);

        public virtual void SetCustomAttribute(CustomAttributeBuilder customBuilder)
            => SetCustomAttribute(customBuilder);

        public abstract int GetTypeToken(Type type);
        public abstract int GetFieldToken(FieldInfo field);
        public abstract int GetMethodToken(MethodInfo method);
        public abstract int GetConstructorToken(ConstructorInfo contsuctor);
        public abstract int GetSignatureToken(SignatureHelper sigHelper);
        public abstract int GetStringConstant(string str);
    }
}
