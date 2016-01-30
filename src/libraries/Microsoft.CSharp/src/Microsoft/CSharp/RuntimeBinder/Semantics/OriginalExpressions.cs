// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
