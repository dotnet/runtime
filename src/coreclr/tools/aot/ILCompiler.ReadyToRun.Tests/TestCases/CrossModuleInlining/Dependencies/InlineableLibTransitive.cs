using System;
using System.Runtime.CompilerServices;

public static class InlineableLibTransitive
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetExternalValue() => ExternalLib.ExternalValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetNestedValue() => ExternalLib.Outer.Inner.NestedValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ExternalLib.ExternalType CreateExternal() => new ExternalLib.ExternalType { Value = 42 };
}
