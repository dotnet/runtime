// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class C
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int[] M()
    {
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Test(int i, int j, bool execute)
    {
        if (execute)
        {
            return M()[checked(i + j)] == 0;
        }

        return true;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // The original repro of the bug associated with this test involved an assert after re-morphing a tree modified
        // by CSE: the original tree contained both a CSE def and a CSE use, and re-morphing eliminated the use, causing
        // CSE to assert when attempting to replace the use with a reference to the CSE lclVar. This call to `Test` is
        // intended to trigger that assert.
        bool test1 = Test(0, 0, false);

        // The associated code in morph involves folding `(x + null)` to `x`. During the investigation of the original
        // issue, it was found that the folding code also failed to check for side effects in `x` resulting in SBCG if
        // side effects were in fact present in `x`. This call to `Test` is intended to ensure that the fold is not
        // performed in the face of a tree that contains side-effects: in particular, the overflowing add in the
        // called method should occur before any other exception.
        bool test2 = false;
        try
        {
            Test(int.MaxValue, int.MaxValue, true);
        }
        catch (System.OverflowException)
        {
            test2 = true;
        }
        catch (System.Exception e)
        {
            System.Console.WriteLine(e);
        }

        return test1 && test2 ? 100 : 101;
    }
}
