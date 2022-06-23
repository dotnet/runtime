// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if smolloy_codedom_full_internalish
namespace System.Runtime.Serialization.CodeDom
#else
namespace System.CodeDom
#endif
{
#if smolloy_codedom_full_internalish
    internal enum CodeBinaryOperatorType
#else
    public enum CodeBinaryOperatorType
#endif
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulus,
        Assign,
        IdentityInequality,
        IdentityEquality,
        ValueEquality,
        BitwiseOr,
        BitwiseAnd,
        BooleanOr,
        BooleanAnd,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
    }
}
