// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum TypeKind
    {
        TK_AggregateType,
        TK_VoidType,
        TK_NullType,
        TK_OpenTypePlaceholderType,
        TK_BoundLambdaType,
        TK_UnboundLambdaType,
        TK_MethodGroupType,
        TK_ErrorType,
        TK_NaturalIntegerType,
        TK_ArgumentListType,
        TK_ArrayType,
        TK_PointerType,
        TK_ParameterModifierType,
        TK_NullableType,
        TK_TypeParameterType
    }
}
