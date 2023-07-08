// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test_GitHub_22556
{
    int f;

    Test_GitHub_22556(int f)
    {
        this.f = f;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Add(null, 0);
        }
        catch (Exception e)
        {
            if (e is NullReferenceException)
            {
                Console.WriteLine("PASS");
                return 100;
            }
        }
        Console.WriteLine("FAIL");
        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Add(Test_GitHub_22556 t, int i)
    {
        // When t is null and i is 0, this should throw
        // NullReferenceException since the operands of
        // addition have to be evaluated left to right.
        int x = t.f + 1 / i;
        return x;
    }
}

