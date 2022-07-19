// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public partial class ModuleBuilder : Module
    {
        public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, Type? returnType, Type[]? parameterTypes)
            => DefineGlobalMethod(name, attributes, CallingConventions.Standard, returnType, null, null, parameterTypes, null, null);

        public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes)
                => DefineGlobalMethod(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null);

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public MethodBuilder DefinePInvokeMethod(string name, string dllName,
            MethodAttributes attributes, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
                => DefinePInvokeMethod(name, dllName, name, attributes, callingConvention,
                    returnType, parameterTypes, nativeCallConv, nativeCharSet);

        public TypeBuilder DefineType(string name)
            => DefineType(name, TypeAttributes.NotPublic, null, null);

        public TypeBuilder DefineType(string name, TypeAttributes attr)
            => DefineType(name, attr, null, null);

        public TypeBuilder DefineType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent)
                => DefineType(name, attr, parent, null);

        public TypeBuilder DefineType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, int typesize)
                => DefineType(name, attr, parent, PackingSize.Unspecified, typesize);

        public TypeBuilder DefineType(string name, TypeAttributes attr,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? parent, PackingSize packsize)
                => DefineType(name, attr, parent, packsize, TypeBuilder.UnspecifiedTypeSize);
    }
}
