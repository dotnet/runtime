// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum CONSTRESKIND
    {
        ConstTrue,
        ConstFalse,
        ConstNotConst,
    }
    internal enum LambdaParams
    {
        FromDelegate,
        FromLambda,
        Error
    }
    internal enum TypeOrSimpleNameResolution
    {
        Unknown,
        CType,
        SimpleName
    }
    internal enum InitializerKind
    {
        CollectionInitializer,
        ObjectInitializer
    }

    internal enum ConstantStringConcatenation
    {
        NotAString,
        NotYetCalculated,
        Calculated
    }

    internal enum ForeachKind
    {
        Array,
        String,
        Enumerator
    }
}
