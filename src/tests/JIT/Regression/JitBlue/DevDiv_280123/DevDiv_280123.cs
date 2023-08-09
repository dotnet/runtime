// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

// This test ensures that the value number store (and its users) behave properly in the event that VN data is requested
// for trees without value numbers. The original repro was a rather large method with a significant amount of dead code
// due to the pattern exhibited in C.N: an entry block that was not transformed from a conditional return to an
// unconditional return followed by dead code that must be kept due to the presence of EH. Value numbering does not
// assign value numbers to the dead code, but assertion prop still runs over the dead code and attempts to use VN info,
// which resulted in a number of asserts.

public static class C
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int N(ref int i)
    {
        bool b = true;
        if (b)
        {
            return 100;
        }

        try
        {
            b = i != 1;
        }
        finally
        {
            b = i != 0;
        }

        return b ? 0 : 1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int i = 0;
        return N(ref i);
    }
}
