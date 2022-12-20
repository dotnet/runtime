// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public partial class ModuleBuilder : Module
    {
        internal ModuleBuilder()
        {
            // Prevent generating a default constructor
        }

        public override Assembly Assembly
        {
            get
            {
                return default;
            }
        }

        [RequiresAssemblyFiles(UnknownStringMessageInRAF)]
        public override string FullyQualifiedName
        {
            get
            {
                return default;
            }
        }

        [RequiresAssemblyFiles(UnknownStringMessageInRAF)]
        public override string Name
        {
            get
            {
                return default;
            }
        }

        public void CreateGlobalFunctions()
        {
        }

        public EnumBuilder DefineEnum(string name, TypeAttributes visibility, Type underlyingType)
        {
            return default;
        }

        public MethodBuilder DefineGlobalMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
        {
            return default;
        }

        public FieldBuilder DefineInitializedData(string name, byte[] data, FieldAttributes attributes)
        {
            return default;
        }

        [RequiresUnreferencedCode("P/Invoke marshalling may dynamically access members that could be trimmed.")]
        public MethodBuilder DefinePInvokeMethod(string name, string dllName, string entryName, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, CallingConvention nativeCallConv, CharSet nativeCharSet)
        {
            return default;
        }

        public TypeBuilder DefineType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, PackingSize packingSize, int typesize)
        {
            return default;
        }

        public TypeBuilder DefineType(string name, TypeAttributes attr, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent, Type[] interfaces)
        {
            return default;
        }

        public FieldBuilder DefineUninitializedData(string name, int size, FieldAttributes attributes)
        {
            return default;
        }

        public override bool Equals(object? obj)
        {
            return default;
        }

        public MethodInfo GetArrayMethod(Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            return default;
        }

        public override int GetHashCode()
        {
            return default;
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
        }
    }
}
