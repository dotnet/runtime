// Second library for multi-step composite compilation.
// Compiled together with MultiStepLeaf as a composite in step 1.
using System;
using System.Runtime.CompilerServices;

public static class MultiStepMid
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetCompositeValue() => MultiStepLeaf.GetValue() + 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetCompositeLabel() => MultiStepLeaf.GetLabel() + "_B";
}
