// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public abstract class ConstructorBuilder : ConstructorInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ConstructorBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is invoked by derived classes.
        /// </remarks>
        protected ConstructorBuilder()
        {
        }

        public bool InitLocals
        {
            get => InitLocalsCore;
            set { InitLocalsCore = value; }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets whether the local variables in this constructor should be zero-initialized.
        /// </summary>
        /// <value>Read/write. Gets or sets whether the local variables in this constructor should be zero-initialized.</value>
        protected abstract bool InitLocalsCore { get; set; }

        public ParameterBuilder DefineParameter(int iSequence, ParameterAttributes attributes, string? strParamName)
            => DefineParameterCore(iSequence, attributes, strParamName);

        /// <summary>
        /// When overridden in a derived class, defines a parameter of this constructor.
        /// </summary>
        /// <param name="iSequence">The position of the parameter in the parameter list. Parameters are indexed beginning with the number 1 for the first parameter.</param>
        /// <param name="attributes">The attributes of the parameter.</param>
        /// <param name="strParamName">The name of the parameter. The name can be the null string.</param>
        /// <returns>A <see cref="ParameterBuilder"/> that represents the new parameter of this constructor.</returns>
        protected abstract ParameterBuilder DefineParameterCore(int iSequence, ParameterAttributes attributes, string? strParamName);

        public ILGenerator GetILGenerator()
            => GetILGeneratorCore(64);

        public ILGenerator GetILGenerator(int streamSize)
            => GetILGeneratorCore(streamSize);

        /// <summary>
        /// When overridden in a derived class, gets an <see cref="ILGenerator"/> that can be used to emit a method body for this constructor.
        /// </summary>
        /// <param name="streamSize">The size of the IL stream, in bytes.</param>
        /// <returns>An <see cref="ILGenerator"/> for this constructor.</returns>
        protected abstract ILGenerator GetILGeneratorCore(int streamSize);

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(binaryAttribute);

            SetCustomAttributeCore(con, binaryAttribute);
        }

        /// <summary>
        /// When overridden in a derived class, sets a custom attribute on this constructor.
        /// </summary>
        /// <param name="con">The constructor for the custom attribute.</param>
        /// <param name="binaryAttribute">A <see cref="ReadOnlySpan{T}"/> of bytes representing the attribute.</param>
        protected abstract void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute);

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            SetCustomAttributeCore(customBuilder.Ctor, customBuilder.Data);
        }

        public void SetImplementationFlags(MethodImplAttributes attributes)
            => SetImplementationFlagsCore(attributes);

        /// <summary>
        /// When overridden in a derived class, sets the method implementation flags for this constructor.
        /// </summary>
        /// <param name="attributes">The method implementation flags.</param>
        protected abstract void SetImplementationFlagsCore(MethodImplAttributes attributes);
    }
}
