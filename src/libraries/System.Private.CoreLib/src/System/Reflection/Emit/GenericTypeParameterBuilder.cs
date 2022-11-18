// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Reflection.Emit
{
    public abstract partial class GenericTypeParameterBuilder : TypeInfo
    {
        protected GenericTypeParameterBuilder()
        {
        }

        public virtual void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute) => SetCustomAttribute(con, binaryAttribute);

        public virtual void SetCustomAttribute(CustomAttributeBuilder customBuilder) => SetCustomAttribute(customBuilder);

        public virtual void SetBaseTypeConstraint([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? baseTypeConstraint) => SetBaseTypeConstraint(baseTypeConstraint);

        public virtual void SetInterfaceConstraints(params Type[]? interfaceConstraints) => SetInterfaceConstraints(interfaceConstraints);

        public virtual void SetGenericParameterAttributes(GenericParameterAttributes genericParameterAttributes) => SetGenericParameterAttributes(genericParameterAttributes);
    }
}
