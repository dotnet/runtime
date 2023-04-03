// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class Test_DevDiv_653853
{
    [Fact]
    public static int TestEntryPoint()
    {        
        if (RunTest(0) == 5)
        {
            Console.WriteLine("SUCCESS");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILURE");
            return -1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RunTest(int arg)
    {
        float f = 0.0F;
        // The bug was that after removing the cast and
        // replacing the right-hand side of the comparison
        // with 0, its value number was changed to the value number
        // of the initial right-hand side tree. That broke the
        // assumption that constant nodes have known constant value numbers.
        if (arg != (sbyte)f)
        {
            return 2*arg;
        }
        else
        {
            return arg + 5;
        }
    }
}
