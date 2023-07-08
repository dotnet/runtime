// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    public abstract partial class GenericTypeParameterBuilder : TypeInfo
    {
        protected GenericTypeParameterBuilder()
        {
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute) => SetCustomAttributeCore(con, binaryAttribute);

        protected abstract void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute);

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            ArgumentNullException.ThrowIfNull(customBuilder);

            SetCustomAttributeCore(customBuilder.Ctor, customBuilder.Data);
        }

        public void SetBaseTypeConstraint([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? baseTypeConstraint) => SetBaseTypeConstraintCore(baseTypeConstraint);

        protected abstract void SetBaseTypeConstraintCore([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? baseTypeConstraint);

        public void SetInterfaceConstraints(params Type[]? interfaceConstraints) => SetInterfaceConstraintsCore(interfaceConstraints);

        protected abstract void SetInterfaceConstraintsCore(params Type[]? interfaceConstraints);

        public void SetGenericParameterAttributes(GenericParameterAttributes genericParameterAttributes) => SetGenericParameterAttributesCore(genericParameterAttributes);

        protected abstract void SetGenericParameterAttributesCore(GenericParameterAttributes genericParameterAttributes);
    }
}
