// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum TypeKind
    {
        TK_AggregateType,
        TK_VoidType,
        TK_NullType,
        TK_MethodGroupType,
        TK_ArgumentListType,
        TK_ArrayType,
        TK_PointerType,
        TK_ParameterModifierType,
        TK_NullableType,
        TK_TypeParameterType
    }
}
