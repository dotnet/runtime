// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal enum GcTypeKind
{
    None,
    Ref,
    Interior,
    Other,
}

internal static class GcTypeKindClassifier
{
    public static GcTypeKind GetGcKind(CorElementType etype) => etype switch
    {
        CorElementType.Class or CorElementType.Object or CorElementType.String
            or CorElementType.Array or CorElementType.SzArray
            or CorElementType.Var or CorElementType.MVar
            or CorElementType.GenericInst => GcTypeKind.Ref,
        CorElementType.Byref => GcTypeKind.Interior,
        CorElementType.ValueType or CorElementType.TypedByRef => GcTypeKind.Other,
        _ => GcTypeKind.None,
    };
}
