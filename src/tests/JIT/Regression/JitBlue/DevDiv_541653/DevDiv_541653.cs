// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// The bug captured by this test was a case where:
// - We have a double register pair that was previous occupied by a double lclVar.
// - That lclVar becomes dead, but has subsequent references, so it remains as the
//   previousInterval on the RegRecord.
// - The first float half is then assigned to another lclVar. It is live across a
//   loop backedge, so it is live at the end of the loop, but is then released before
//   the next block is allocated.
// - At this time, the double lclVar is restored to that RegRecord (as inactive), but
//   the loop over the lclVars sees only the second half, and asserts because it doesn't
//   expect to ever encounter an interval in the second half (it should have been skipped).

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class DevDiv_541643
{
    public const int Pass = 100;
    public const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static float GetFloat(int i)
    {
        return (float)i;
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static double GetDouble(int i)
    {
        return (double)i;
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int GetInt(float f)
    {
        return (int)f;
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int GetInt(double d)
    {
        return (int)d;
    }
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int test(int count)
    {
        double d = GetDouble(0);
        // Use d; it will be dead until we redefine it below.
        int result = (int)d;

        // Now define our float lclVar and use it in a loop.
        float f = GetFloat(1);
        for (int i = 0; i < count; i++)
        {
            result += GetInt(f);
        }

        // Finally, redefine d and use it.
        d = GetDouble(3);
        for (int i = 0; i < count; i++)
        {
            result += GetInt(d);
        }

        Console.WriteLine("Result: " + result);
        return result;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        int result = test(10);
        return Pass;
    }
}
