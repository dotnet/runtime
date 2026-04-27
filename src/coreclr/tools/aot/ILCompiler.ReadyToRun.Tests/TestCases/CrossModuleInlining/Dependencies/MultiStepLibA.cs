// Dependency library for multi-step compilation tests.
// Contains sync inlineable methods used in both composite and non-composite steps.
using System;
using System.Runtime.CompilerServices;

public static class MultiStepLibA
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetValue() => 42;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetLabel() => "LibA";
}
