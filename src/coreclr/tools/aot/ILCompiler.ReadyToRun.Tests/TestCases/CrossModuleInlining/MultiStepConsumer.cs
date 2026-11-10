// Test: Non-composite consumer of assemblies that were also compiled as composite.
// Step 1 compiles MultiStepLibA + MultiStepLibB as composite.
// Step 2 compiles this assembly non-composite with --ref to LibA and --opt-cross-module.
using System;
using System.Runtime.CompilerServices;

public static class MultiStepConsumer
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int GetValueFromLibA()
    {
        return MultiStepLibA.GetValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string GetLabelFromLibA()
    {
        return MultiStepLibA.GetLabel();
    }
}
