// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;

// Tests that side effects induced by the HWI nodes are correctly accounted for.

unsafe class HwiSideEffects
{
    public static int Main()
    {
        if (ProblemWithInterferenceChecks(2) != 2)
        {
            return 101;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint ProblemWithInterferenceChecks(uint a)
    {
        uint x;
        if (Bmi2.IsSupported)
        {
            // Make sure we don't try to contain "a" under the "add" here.
            x = a + Bmi2.MultiplyNoFlags(a, a, &a);
        }
        else
        {
            x = a;
        }

        return x;
    }
}
