// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// GC classification of an argument for stack scanning. Mirrors the <c>m_gc</c> field of
/// native <c>gElementTypeInfo</c> in src/coreclr/vm/siginfo.cpp.
/// </summary>
internal enum GcTypeKind
{
    /// <summary>Not a GC reference (primitives, pointers).</summary>
    None,
    /// <summary>Object reference (class, string, array).</summary>
    Ref,
    /// <summary>Interior pointer (byref).</summary>
    Interior,
    /// <summary>Value type that may contain embedded GC references.</summary>
    Other,
}

/// <summary>
/// Maps a <see cref="CorElementType"/> to its GC classification. Pure function of the
/// element type; not calling-convention or ABI dependent.
/// </summary>
internal static class GcTypeKindClassifier
{
    /// <summary>
    /// Maps a (possibly normalized) <see cref="CorElementType"/> to its GC classification,
    /// matching the <c>m_gc</c> field of native <c>gElementTypeInfo</c>.
    /// </summary>
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
