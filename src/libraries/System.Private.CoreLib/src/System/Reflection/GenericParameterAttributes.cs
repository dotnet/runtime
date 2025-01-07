// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    [Flags]
    public enum GenericParameterAttributes
    {
        None = 0x0000,
        VarianceMask = 0x0003,
        Covariant = 0x0001,
        Contravariant = 0x0002,
        SpecialConstraintMask = 0x001C,
        ReferenceTypeConstraint = 0x0004,
        NotNullableValueTypeConstraint = 0x0008,
        DefaultConstructorConstraint = 0x0010,
        AllowByRefLike = 0x0020,
    }
}
