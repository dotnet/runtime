// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    public abstract class MethodBuilder : MethodInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MethodBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is invoked by derived classes.
        /// </remarks>
        protected MethodBuilder()
        {
        }

        public bool InitLocals
        {
            get => InitLocalsCore;
            set { InitLocalsCore = value; }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets a Boolean value that specifies whether the local variables
        /// in this method are zero initialized. The default value of this property is true.
        /// </summary>
        /// <value><see langword="true" /> if the local variables in this method should be zero initialized; otherwise <see langword="false" />.</value>
        protected abstract bool InitLocalsCore { get; set; }

        public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
        {
            ArgumentNullException.ThrowIfNull(names);

            if (names.Length == 0)
                throw new ArgumentException(SR.Arg_EmptyArray, nameof(names));

            return DefineGenericParametersCore(names);
        }

        /// <summary>
        /// When overridden in a derived class, sets the number of generic type parameters for the current method, specifies their names,
        /// and returns an array of <see cref="GenericTypeParameterBuilder"/> objects that can be used to define their constraints.
        /// </summary>
        /// <param name="names">An array of strings that represent the names of the generic type parameters.</param>
        /// <returns>An array of <see cref="GenericTypeParameterBuilder"/> objects representing the type parameters of the generic method.</returns>
        protected abstract GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names);

        public ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string? strParamName)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(position);

            return DefineParameterCore(position, attributes, strParamName);
        }

        /// <summary>
        /// When overridden in a derived class, defines a parameter or return parameter for this method.
        /// </summary>
        /// <param name="position">The position of the parameter in the parameter list. Parameters are indexed beginning with the number 1 for the first parameter;
        /// the number 0 represents the return parameter of the method.</param>
        /// <param name="attributes">The <see cref="ParameterAttributes"/> of the parameter.</param>
        /// <param name="strParamName">The name of the parameter. The name can be the null string.</param>
        /// <returns>Returns a <see cref="ParameterBuilder"/> object that represents a parameter of this method or the return parameter of this method.</returns>
        /// <remarks>
        /// Returned <see cref="ParameterBuilder"/> can be used to apply custom attributes.
        /// </remarks>
        protected abstract ParameterBuilder DefineParameterCore(int position, ParameterAttributes attributes, string? strParamName);

        public ILGenerator GetILGenerator()
            => GetILGenerator(64);

        public ILGenerator GetILGenerator(int size)
            => GetILGeneratorCore(size);

        /// <summary>
        /// When overridden in a derived class, gets an <see cref="ILGenerator"/> that can be used to emit a method body for this method.
        /// </summary>
        /// <param name="size">The size of the IL stream, in bytes.</param>
        /// <returns>Returns an <see cref="ILGenerator"/> object for this method.</returns>
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

        public void SetImplementationFlags(MethodImplAttributes attributes) => SetImplementationFlagsCore(attributes);

        /// <summary>
        /// When overridden in a derived class, sets the implementation flags for this method.
        /// </summary>
        /// <param name="attributes">The implementation flags to set.</param>
        protected abstract void SetImplementationFlagsCore(MethodImplAttributes attributes);

        public void SetParameters(params Type[] parameterTypes)
            => SetSignature(null, null, null, parameterTypes, null, null);

        public void SetReturnType(Type? returnType)
            => SetSignature(returnType, null, null, null, null, null);

        public void SetSignature(Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers)
            => SetSignatureCore(returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers,
                parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers);

        /// <summary>
        /// When overridden in a derived class, sets the method signature, including the return type, the parameter types,
        /// and the required and optional custom modifiers of the return type and parameter types.
        /// </summary>
        /// <param name="returnType">The return type of the method.</param>
        /// <param name="returnTypeRequiredCustomModifiers">An array of types representing the required custom modifiers.</param>
        /// <param name="returnTypeOptionalCustomModifiers">An array of types representing the optional custom modifiers.</param>
        /// <param name="parameterTypes">The types of the parameters of the method.</param>
        /// <param name="parameterTypeRequiredCustomModifiers">An array of arrays of types. Each array of types represents the required custom modifiers for the corresponding parameter.</param>
        /// <param name="parameterTypeOptionalCustomModifiers">An array of arrays of types. Each array of types represents the optional custom modifiers for the corresponding parameter.</param>
        protected abstract void SetSignatureCore(Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers,
            Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers);
    }
}
