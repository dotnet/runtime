// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class GitHubIssue12479
{
    public static int callee(int one,
                             int two,
                             int three,
                             int four,
                             int five,
                             int six,
                             int seven,
                             int eight)
    {
        int count = 0;

        // Make sure this is not inlined.
        for (int i = 0; i < one; ++i)
        {
            if (i % 4 == 0) ++count;
        }

        return count;
    }

    // Linux (x64): Eight incoming arguments, all passed in registers
    //
    // nCallerArgs: 8, stackSize: 0 bytes
    public static int caller(float one, 
                             float two, 
                             float three, 
                             float four, 
                             float five,
                             float six,
                             float seven,
                             float eight)
    {
        if (one % 2 == 0)
        {
            // Eight outgoing arguments, six passed in registers, two on the stack.
            //
            // nCalleeArgs: 8, stackSize: 8 bytes
            //
            // This is a fast tail call candidate that should not be fast tail called
            // because the callee's stack size will be larger than the caller's
            return callee((int) two,
                          (int) one,
                          (int) eight,
                          (int) five,
                          (int) four,
                          (int) seven,
                          (int) six,
                          (int) three);
        }
        else
        {
            // Eight outgoing arguments, six passed in registers, two on the stack.
            //
            // nCalleeArgs: 8, stackSize: 8 bytes
            //
            // This is a fast tail call candidate that should not be fast tail called
            // because the callee's stack size will be larger than the caller's
            return callee((int) one,
                          (int) two,
                          (int) three,
                          (int) four,
                          (int) five,
                          (int) six,
                          (int) seven,
                          (int) eight);
        }

        
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // We have 8 floating args on unix.
        int a = caller(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f);

        if (a == 1)
        {
            return 100;
        }

        else
        {
            return 101;
        }
    }
}
