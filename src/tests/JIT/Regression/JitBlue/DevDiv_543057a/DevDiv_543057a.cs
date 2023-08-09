// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// The bug captured by this test was a case where:
// - We have DOTNET_JitStressRegs=3, so we're limiting the available registers.
// - We have a DIV with two double operands that are casts from int lclVars, and it is passed to a call.
// - We have 4 float lclVars in registers:
//   - One is active in a caller-save register (that will be x in our case)
//   - One is active in a callee-save register (y)
//   - One is inactive in a caller-save register (z)
//   - One is inactive in a callee-save register (w)
// - When we allocate the def (target) register for the second cast, we spill the first one.
// - When we try to reload it, we were incorrectly returning false from 'isRegInUse()'
//   for the inactive interval in the second half.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class DevDiv_543057

{
    public const int Pass = 100;
    public const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float GetFloat(int i)
    {
        return (float)i;
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int test(int i, int j)
    {
        // x and y will be preferenced to callee-save
        //   they need to have enough references to qualify for callee-save
        // z and w will not
        // x and z will be inactive at the call to Math.Ceiling
        // y and w will be active
        float x = GetFloat(1);
        float y = GetFloat(2);
        float z = GetFloat(3);
        // We want w in f1. At this point z is likely to be in f0.
        // So define 'result' first.
        float result = x - y;
        float w = x + y + z;
        if (i != j)
        {
            // Here all our floats are in registers. z is going to be redefined, so it is inactive,
            // and w is not used except in the else clause so it is also inactive.
            z = (float) Math.Ceiling(((double)i) / ((double)j));
            // Now we use x and y so that they are live across the call to Math.Ceiling.
            result = z + y + w;
        }
        else
        {
            // Here we need to use all of our float arguments so that they are all live.
            y = x * y * z * w;
            x = y * 2;
            // Now x and y are going to be live across this call, to encourage them to get a callee-save reg.
            Console.WriteLine("FAIL");
            // And use x a couple more times.
            x *= y;
            result += x + y;
        }
        Console.WriteLine("Result: " + result);
        return Pass;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        return test(5, 6);
    }
}

