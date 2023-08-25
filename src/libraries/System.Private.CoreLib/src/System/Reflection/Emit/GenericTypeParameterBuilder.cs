// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    public abstract partial class GenericTypeParameterBuilder : TypeInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="GenericTypeParameterBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is invoked by derived classes.
        /// </remarks>
        protected GenericTypeParameterBuilder()
        {
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute) => SetCustomAttributeCore(con, binaryAttribute);

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

        public void SetBaseTypeConstraint([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? baseTypeConstraint) => SetBaseTypeConstraintCore(baseTypeConstraint);

        /// <summary>
        /// When overridden in a derived class, sets the base type that a type must inherit in order to be substituted for the type parameter.
        /// </summary>
        /// <param name="baseTypeConstraint">The <see cref="Type"/> that must be inherited by any type that is to be substituted for the type parameter.</param>
        protected abstract void SetBaseTypeConstraintCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? baseTypeConstraint);

        public void SetInterfaceConstraints(params Type[]? interfaceConstraints) => SetInterfaceConstraintsCore(interfaceConstraints);

        /// <summary>
        /// When overridden in a derived class, sets the interfaces a type must implement in order to be substituted for the type parameter.
        /// </summary>
        /// <param name="interfaceConstraints">An array of <see cref="Type"/> objects that represent the interfaces a type must implement
        /// in order to be substituted for the type parameter.</param>
        protected abstract void SetInterfaceConstraintsCore(params Type[]? interfaceConstraints);

        public void SetGenericParameterAttributes(GenericParameterAttributes genericParameterAttributes) => SetGenericParameterAttributesCore(genericParameterAttributes);

        /// <summary>
        /// When overridden in a derived class, sets the variance characteristics and special constraints of the generic parameter,
        /// such as the parameterless constructor constraint.
        /// </summary>
        /// <param name="genericParameterAttributes">A bitwise combination of <see cref="GenericParameterAttributes"/> values that
        /// represent the variance characteristics and special constraints of the generic type parameter.</param>
        protected abstract void SetGenericParameterAttributesCore(GenericParameterAttributes genericParameterAttributes);
    }
}
