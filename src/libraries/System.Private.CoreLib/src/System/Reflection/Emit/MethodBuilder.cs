// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    public abstract class MethodBuilder : MethodInfo
    {
        protected MethodBuilder()
        {
        }

        public bool InitLocals
        {
            get => InitLocalsCore;
            set { InitLocalsCore = value; }
        }

        protected abstract bool InitLocalsCore { get; set; }

        public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
        {
            ArgumentNullException.ThrowIfNull(names);

            if (names.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyArray, nameof(names));

            return DefineGenericParametersCore(names);
        }

        protected abstract GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names);

        public ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string? strParamName)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(position);

            return DefineParameterCore(position, attributes, strParamName);
        }

        protected abstract ParameterBuilder DefineParameterCore(int position, ParameterAttributes attributes, string? strParamName);

        public ILGenerator GetILGenerator()
            => GetILGenerator(64);

        public ILGenerator GetILGenerator(int size)
            => GetILGeneratorCore(size);

        protected abstract ILGenerator GetILGeneratorCore(int size);

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);

            SetCustomAttributeCore(con, binaryAttribute);
        }

        internal void SetCustomAttribute(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            SetCustomAttributeCore(con, binaryAttribute);
        }

        protected abstract void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute);

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            SetCustomAttributeCore(customBuilder.Ctor, customBuilder.Data);
        }

        public void SetImplementationFlags(MethodImplAttributes attributes) => SetImplementationFlagsCore(attributes);

        protected abstract void SetImplementationFlagsCore(MethodImplAttributes attributes);

        public void SetParameters(params Type[] parameterTypes)
            => SetSignature(null, null, null, parameterTypes, null, null);

        public void SetReturnType(Type? returnType)
            => SetSignature(returnType, null, null, null, null, null);

        public void SetSignature(Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
            => SetSignatureCore(returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);

        protected abstract void SetSignatureCore(Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers);
    }
}
