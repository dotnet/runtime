// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * TEST NAME: NullHandle
 * DESCRIPTION: operates on Weakhandles whose m_handle is null
 */

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public class WR : WeakReference
{
    public WR(Object o) : base(o, false) { }

    ~WR()
    {
        Console.WriteLine("Resurrected!");
        Test.w = this;
    }
}

public class Test
{
    public static WR w;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static WR ReturnWR() { return new WR(new Object()); }

    public static int Main()
    {
        int numTests = 0;
        int numPassed = 0;
        WR wr = ReturnWR();
        wr = null;

        // this will resurrect wr
        GC.Collect();
        GC.WaitForPendingFinalizers();

        try
        {
            numTests++;
            Console.WriteLine("Get Target Test");
            Console.WriteLine(Test.w.Target);
            Console.WriteLine("Passed");
            numPassed++;
        }

        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        try
        {
            numTests++;
            Console.WriteLine("IsAlive Test");
            bool b = Test.w.IsAlive;
            Console.WriteLine(b);

            if (!b)
            {
                Console.WriteLine("Passed");
                numPassed++;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        try
        {
            numTests++;
            Console.WriteLine("Set Target Test");
            Test.w.Target = new Object();
        }
        catch (InvalidOperationException)
        {
            numPassed++;
            Console.WriteLine("Passed");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        if (numTests == numPassed)
        {
            return 100;
        }

        return 1;
    }
}
