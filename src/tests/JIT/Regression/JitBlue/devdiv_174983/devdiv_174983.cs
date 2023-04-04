// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Test_devdiv_174983
{
    public int i;
    public int j;

    public static int l;

    [Fact]
    public static int TestEntryPoint()
    {
        Test_devdiv_174983 test = new Test_devdiv_174983();
        test.i = 3;
        test.j = 5;
        Test_devdiv_174983.l = 1;
        if (test.Foo() == 8)
        {
            Console.WriteLine("PASS!");
            return 100;
        }
        else
        {
            Console.WriteLine("FAIL!");
            return 101;
        }
    }

    // This tests a multi-entry loop where we have to evaluate recursive phi's
    // for heap state in value numbering.
    public int Foo()
    {        
        if (l != 17)
        {
            goto L2;
        }

        i = 0;

L1:
        if (l == 1)
        {
            // In the bug the compiler incorrectly concluded that i is always 0 here.
            return i + j;
        }       

L2:
        if (l == 12)
        {
            return i;
        }
        goto L1;
    }
}
