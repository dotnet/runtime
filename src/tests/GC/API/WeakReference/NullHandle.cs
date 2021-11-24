// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        Test_NullHandle.w = this;
    }
}

public class Test_NullHandle
{
    // This weak reference gets resurrected by WR's destructor.
    public static WR w;
    
    // This weak reference is destroyed to prompt WR's destructor to run.
    public static WR wr;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void CreateWR() { wr = new WR(new Object()); }
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void DestroyWR() { wr = null; }

    public static int Main()
    {
        int numTests = 0;
        int numPassed = 0;
        CreateWR();
        DestroyWR();

        // this will resurrect wr
        GC.Collect();
        GC.WaitForPendingFinalizers();

        try
        {
            numTests++;
            Console.WriteLine("Get Target Test");
            Console.WriteLine(Test_NullHandle.w.Target);
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
            bool b = Test_NullHandle.w.IsAlive;
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
            Test_NullHandle.w.Target = new Object();
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
