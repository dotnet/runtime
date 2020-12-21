// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;

// This testcase reproduces a bug where the tree re-sequencing was not correct for 
// fgMorphModToSubMulDiv(), resulting in an assert in LSRA.

static class Test
{
    static byte GetVal()
    {
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int DoMod(SByte arg)
    {
        byte val = GetVal();
        return arg % val;
    }

    static int Main()
    {
        int returnVal = -1;
        try
        {
            DoMod(4);
            Console.WriteLine("FAILED: No exception thrown");
            returnVal = -1;
        }
        catch (System.DivideByZeroException)
        {
            Console.WriteLine("PASS");
            returnVal = 100;
        }
        return returnVal;
    }
}
