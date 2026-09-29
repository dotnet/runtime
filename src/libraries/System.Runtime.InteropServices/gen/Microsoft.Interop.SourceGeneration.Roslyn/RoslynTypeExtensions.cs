// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using RoslynRefKind = Microsoft.CodeAnalysis.RefKind;
using RoslynScopedKind = Microsoft.CodeAnalysis.ScopedKind;
using RoslynSpecialType = Microsoft.CodeAnalysis.SpecialType;

namespace Microsoft.Interop;

/// <summary>Converts compiler type classifications into marshalling model classifications.</summary>
public static class RoslynTypeExtensions
{
    /// <summary>Maps the special types needed by the marshalling model.</summary>
    /// <param name="type">The compiler type classification.</param>
    /// <returns>The corresponding classification, or <see cref="SpecialType.None"/> for a general type.</returns>
    public static SpecialType ToInteropSpecialType(this RoslynSpecialType type) => type switch
    {
        RoslynSpecialType.System_Enum => SpecialType.System_Enum,
        RoslynSpecialType.System_Void => SpecialType.System_Void,
        RoslynSpecialType.System_Boolean => SpecialType.System_Boolean,
        RoslynSpecialType.System_Char => SpecialType.System_Char,
        RoslynSpecialType.System_SByte => SpecialType.System_SByte,
        RoslynSpecialType.System_Byte => SpecialType.System_Byte,
        RoslynSpecialType.System_Int16 => SpecialType.System_Int16,
        RoslynSpecialType.System_UInt16 => SpecialType.System_UInt16,
        RoslynSpecialType.System_Int32 => SpecialType.System_Int32,
        RoslynSpecialType.System_UInt32 => SpecialType.System_UInt32,
        RoslynSpecialType.System_Int64 => SpecialType.System_Int64,
        RoslynSpecialType.System_UInt64 => SpecialType.System_UInt64,
        RoslynSpecialType.System_Single => SpecialType.System_Single,
        RoslynSpecialType.System_Double => SpecialType.System_Double,
        RoslynSpecialType.System_String => SpecialType.System_String,
        RoslynSpecialType.System_IntPtr => SpecialType.System_IntPtr,
        RoslynSpecialType.System_UIntPtr => SpecialType.System_UIntPtr,
        _ => SpecialType.None
    };

    /// <summary>Converts a compiler reference kind into a signature reference kind.</summary>
    /// <param name="kind">The compiler reference kind.</param>
    /// <returns>The corresponding signature reference kind.</returns>
    public static RefKind ToInteropRefKind(this RoslynRefKind kind) => kind switch
    {
        RoslynRefKind.None => RefKind.None,
        RoslynRefKind.Ref => RefKind.Ref,
        RoslynRefKind.Out => RefKind.Out,
        RoslynRefKind.In => RefKind.In,
        RoslynRefKind.RefReadOnlyParameter => RefKind.RefReadOnlyParameter,
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
