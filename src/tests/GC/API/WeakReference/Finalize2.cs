// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * TEST NAME: Finalize2
 * DESCRIPTION: operates on Weakhandles whose targets are being finalized
 */

using System;
using System.Runtime.CompilerServices;

public class GetTargetTest
{
    public WeakReference w;
    static public bool Passed = false;

    public GetTargetTest(bool trackResurrection)
    {
        w = new WeakReference(this, trackResurrection);
    }

    ~GetTargetTest()
    {
        Console.WriteLine("Running ~GetTargetTest");
        // target is being finalized.  Internal handle should be null
        try
        {
            Object o = w.Target;
            if (o == null)
            {
                Console.WriteLine("getTarget passed");
                Console.WriteLine();
                Passed = true;
                return;
            }
            GC.KeepAlive(o);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        Console.WriteLine("getTarget failed");
        Console.WriteLine();
    }
}

public class SetTargetTest
{
    public WeakReference w;
    static public bool Passed = false;

    public SetTargetTest(bool trackResurrection)
    {
        w = new WeakReference(this, trackResurrection);
    }

    ~SetTargetTest()
    {
        // target is being finalized.  Internal handle should be null
        Console.WriteLine("Running ~SetTargetTest");

        try
        {
            w.Target = new Object();
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("Caught expected InvalidOperationException");
            Console.WriteLine("setTarget passed");
            Console.WriteLine();
            Passed = true;
            return;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        Console.WriteLine("setTarget failed");
        Console.WriteLine();
    }
}

public class IsAliveTest
{
    public WeakReference w;
    static public bool Passed = false;

    public IsAliveTest(bool trackResurrection)
    {
        w = new WeakReference(this, trackResurrection);
    }

    ~IsAliveTest()
    {
        Console.WriteLine("Running ~IsAliveTest");
        // target is being finalized.  Internal handle should be null

        try
        {
            bool b = w.IsAlive;

            if (!b)
            {
                Console.WriteLine("IsAliveTest passed");
                Console.WriteLine();
                Passed = true;
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        Console.WriteLine("IsAlive failed");
        Console.WriteLine();
    }
}

public class NullHandle
{
    public static GetTargetTest getTargetTest;
    public static SetTargetTest setTargetTest;
    public static IsAliveTest isAliveTest;
    
    public bool RunTests(bool trackResurrection)
    {
        CreateGetTargetTest(trackResurrection);
        CreateSetTargetTest(trackResurrection);
        CreateIsAliveTest(trackResurrection);

        // make sure Finalizers are called
        DestroyGetTargetTest();
        DestroySetTargetTest();
        DestroyIsAliveTest();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Console.WriteLine();

        return ((GetTargetTest.Passed) && (SetTargetTest.Passed) && (IsAliveTest.Passed));
    }
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void CreateGetTargetTest(bool trackResurrection) 
    {
        getTargetTest = new GetTargetTest(trackResurrection);
    }
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void CreateSetTargetTest(bool trackResurrection) 
    {
        setTargetTest = new SetTargetTest(trackResurrection);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void CreateIsAliveTest(bool trackResurrection) 
    {
        isAliveTest = new IsAliveTest(trackResurrection);
    }
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void DestroyGetTargetTest()
    {
        getTargetTest = null;
    }
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void DestroySetTargetTest()
    {
        setTargetTest = null;
    }
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void DestroyIsAliveTest()
    {
        isAliveTest = null;
    }

    public static int Main()
    {
        NullHandle t = new NullHandle();
        bool longPassed = false;
        bool shortPassed = false;

        if (t.RunTests(false))
        {
            Console.WriteLine("Short WR Test Passed!");
            shortPassed = true;
        }
        else
        {
            Console.WriteLine("Short WR Test Failed!");
        }

        Console.WriteLine();
        Console.WriteLine();

        if (t.RunTests(true))
        {
            Console.WriteLine("Long WR Test Passed!");
            longPassed = true;
        }
        else
        {
            Console.WriteLine("Long WR Test Failed!");
        }

        Console.WriteLine();
        Console.WriteLine();

        if (longPassed && shortPassed)
        {
            Console.WriteLine("Test Passed!");
            return 100;
        }

        Console.WriteLine("Test Failed!");
        return 1;
    }
}
