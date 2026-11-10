// Second library for multi-step composite compilation.
// Compiled together with MultiStepLibA as a composite in step 1.
using System;
using System.Runtime.CompilerServices;

public static class MultiStepLibB
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetCompositeValue() => MultiStepLibA.GetValue() + 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetCompositeLabel() => MultiStepLibA.GetLabel() + "_B";
}
