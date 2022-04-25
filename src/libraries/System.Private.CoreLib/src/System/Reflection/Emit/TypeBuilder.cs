// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public partial class TypeBuilder : TypeInfo
    {
        public const int UnspecifiedTypeSize = 0;

        public ConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes)
            => DefineConstructor(attributes, callingConvention, parameterTypes, null, null);

        public FieldBuilder DefineField(string fieldName, Type type, FieldAttributes attributes)
            => DefineField(fieldName, type, null, null, attributes);

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes)
            => DefineMethod(name, attributes, CallingConventions.Standard, null, null);

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention)
            => DefineMethod(name, attributes, callingConvention, null, null);

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes)
                => DefineMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);

        public MethodBuilder DefineMethod(string name, MethodAttributes attributes, Type returnType, Type[] parameterTypes)
            => DefineMethod(name, attributes, CallingConventions.Standard, returnType, parameterTypes);

        public TypeBuilder DefineNestedType(string name)
            => DefineNestedType(name, TypeAttributes.NestedPrivate, null, null);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr)
            => DefineNestedType(name, attr, null, null);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
                => DefineNestedType(name, attr, parent, null);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, int typeSize)
                => DefineNestedType(name, attr, parent, PackingSize.Unspecified, typeSize);

        public TypeBuilder DefineNestedType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packSize)
                => DefineNestedType(name, attr, parent, packSize, TypeBuilder.UnspecifiedTypeSize);

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
    }
}
