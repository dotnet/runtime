// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using RoslynScopedKind = Microsoft.CodeAnalysis.ScopedKind;

namespace Microsoft.Interop;

/// <summary>Converts compiler type classifications into marshalling model classifications.</summary>
public static class RoslynTypeExtensions
{
    /// <summary>Maps the special types needed by the marshalling model.</summary>
    /// <param name="type">The compiler type classification.</param>
    /// <returns>The corresponding classification, or <see cref="WellKnownType.None"/> for a general type.</returns>
    public static WellKnownType ToWellKnownType(this SpecialType type) => type switch
    {
        SpecialType.System_Enum => WellKnownType.System_Enum,
        SpecialType.System_Void => WellKnownType.System_Void,
        SpecialType.System_Boolean => WellKnownType.System_Boolean,
        SpecialType.System_Char => WellKnownType.System_Char,
        SpecialType.System_SByte => WellKnownType.System_SByte,
        SpecialType.System_Byte => WellKnownType.System_Byte,
        SpecialType.System_Int16 => WellKnownType.System_Int16,
        SpecialType.System_UInt16 => WellKnownType.System_UInt16,
        SpecialType.System_Int32 => WellKnownType.System_Int32,
        SpecialType.System_UInt32 => WellKnownType.System_UInt32,
        SpecialType.System_Int64 => WellKnownType.System_Int64,
        SpecialType.System_UInt64 => WellKnownType.System_UInt64,
        SpecialType.System_Single => WellKnownType.System_Single,
        SpecialType.System_Double => WellKnownType.System_Double,
        SpecialType.System_String => WellKnownType.System_String,
        SpecialType.System_IntPtr => WellKnownType.System_IntPtr,
        SpecialType.System_UIntPtr => WellKnownType.System_UIntPtr,
        _ => WellKnownType.None
    };

    /// <summary>Converts a compiler reference kind into a signature reference kind.</summary>
    /// <param name="kind">The compiler reference kind.</param>
    /// <returns>The corresponding signature reference kind.</returns>
    public static ByRefMarshalKind ToByRefMarshalKind(this RefKind kind) => kind switch
    {
        RefKind.None => ByRefMarshalKind.None,
        RefKind.Ref => ByRefMarshalKind.Ref,
        RefKind.Out => ByRefMarshalKind.Out,
        RefKind.In => ByRefMarshalKind.In,
        RefKind.RefReadOnlyParameter => ByRefMarshalKind.RefReadOnlyParameter,
        _ => throw new UnreachableException($"Unexpected reference kind: {kind}.")
    };

    /// <summary>Converts a compiler escape scope into a signature escape scope.</summary>
    /// <param name="kind">The compiler escape scope.</param>
    /// <returns>The corresponding signature escape scope.</returns>
    public static ScopedKind ToInteropScopedKind(this RoslynScopedKind kind) => kind switch
    {
        RoslynScopedKind.None => ScopedKind.None,
        RoslynScopedKind.ScopedRef => ScopedKind.ScopedRef,
        RoslynScopedKind.ScopedValue => ScopedKind.ScopedValue,
        _ => throw new UnreachableException($"Unexpected scoped kind: {kind}.")
    };
}
