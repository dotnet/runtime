// Inlinable helpers that forward sync calls through to SyncLeafMethods.
// Used to exercise transitive cross-module inlining.
using System;
using System.Runtime.CompilerServices;

public static class InlinableLeafCallers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetExternalValue() => SyncLeafMethods.ExternalValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetNestedValue() => SyncLeafMethods.Outer.Inner.NestedValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SyncLeafMethods.ExternalType CreateExternal() =>
        new SyncLeafMethods.ExternalType { Value = 42 };
}
