// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Threading;

public class Test
{
    private static int CallbackCounter = 0;

    private static AutoResetEvent anotherCallbackEvent = new AutoResetEvent(false);
    private static AutoResetEvent callbackEvent = new AutoResetEvent(false);

    public static void AnotherCallback()
    {
        Interlocked.Increment(ref CallbackCounter);
        GC.RegisterNoGCRegionCallback(19 * 1024 * 1024,  new Action(Callback));
        bool thrown = false;
        try
        {
            GC.RegisterNoGCRegionCallback(19 * 1024 * 1024,  new Action(Callback));
        }
        catch (InvalidOperationException ioex)
        {
            thrown = true;
        }
        if (!thrown)
        {
            throw new Exception("Expected exception not thrown");
        }
        anotherCallbackEvent.Set();
    }

    public static void Callback()
    {
        Interlocked.Increment(ref CallbackCounter);
        callbackEvent.Set();
    }

    public static void ShouldNotCall()
    {
        Interlocked.Increment(ref CallbackCounter);
        Console.WriteLine("We should not see this");
    }

    public static void TestZero()
    {
        bool thrown = false;
        try
        {
            GC.RegisterNoGCRegionCallback(0,  new Action(Callback));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            thrown = true;
        }
        if (!thrown)
        {
            throw new Exception("Expected exception not thrown");
        }
        if (CallbackCounter != 0)
        {
            throw new Exception("Callback should not be called");
        }
    }

    public static void TestNegative()
    {
        bool thrown = false;
        try
        {
            GC.RegisterNoGCRegionCallback(-10,  new Action(Callback));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            thrown = true;
        }
        if (!thrown)
        {
            throw new Exception("Expected exception not thrown");
        }
        if (CallbackCounter != 0)
        {
            throw new Exception("Callback should not be called");
        }
    }

    public static void TestNull()
    {
        bool thrown = false;
        try
        {
            GC.RegisterNoGCRegionCallback(10,  null);
        }
        catch (ArgumentNullException ex)
        {
            thrown = true;
        }
        if (!thrown)
        {
            throw new Exception("Expected exception not thrown");
        }
    }

    public static void TestNotStarted()
    {
        bool thrown = false;
        try
        {
            GC.RegisterNoGCRegionCallback(100 * 1024 * 1024,  new Action(Callback));
        }
        catch (InvalidOperationException ex)
        {
            thrown = true;
        }
        if (!thrown)
        {
            throw new Exception("Expected exception not thrown");
        }
        if (CallbackCounter != 0)
        {
            throw new Exception("Callback should not be called");
        }
    }

    public static void TestExceed()
    {
        bool thrown = false;
        
        try
        {
            GC.TryStartNoGCRegion(10 * 1024 * 1024);
            GC.RegisterNoGCRegionCallback(100 * 1024 * 1024,  new Action(Callback));
        }
        catch (InvalidOperationException ex)
        {
            thrown = true;
        }
        finally
        {
            GC.EndNoGCRegion();
        }
        if (!thrown)
        {
            throw new Exception("Expected exception not thrown");
        }
        if (CallbackCounter != 0)
        {
            throw new Exception("Callback should not be called");
        }
    }

    public static void TestSimple(int oh)
    {
        CallbackCounter = 0;
        GC.TryStartNoGCRegion(10 * 1024 * 1024);
        GC.RegisterNoGCRegionCallback(5 * 1024 * 1024,  new Action(Callback));
        Allocate(oh, 30);
        callbackEvent.WaitOne();
        if (CallbackCounter != 1)
        {
            throw new Exception("Callback should be called");
        }
    }

    public static void TestCascade()
    {
        CallbackCounter = 0;
        GC.TryStartNoGCRegion(10 * 1024 * 1024);
        GC.RegisterNoGCRegionCallback(2 * 1024 * 1024,  new Action(AnotherCallback));
        Allocate(SOH, 3);
        anotherCallbackEvent.WaitOne();
        Allocate(SOH, 27);
        callbackEvent.WaitOne();
        if (CallbackCounter != 2)
        {
            throw new Exception("Callback should be called");
        }
    }

    public static void TestLoop()
    {
        CallbackCounter = 0;
        for (int i = 0; i < 100; i++)
        {
            GC.TryStartNoGCRegion(10 * 1024 * 1024);
            GC.RegisterNoGCRegionCallback(2 * 1024 * 1024, new Action(ShouldNotCall));
            GC.EndNoGCRegion();
        }
        if (CallbackCounter != 0)
        {
            throw new Exception("Callback invoked");
        }
    }

    private static int SOH = 0;
    private static int LOH = 0;

    public unsafe static void Allocate(int oh, int mb)
    {
        int overhead = 3 * sizeof(IntPtr);
        for (int i = 0; i < mb; i++)
        {
            if (oh == SOH)
            {
                // This loop allocates 1M
                for (int j = 0; j < 1024 * 1024 / 32; j++)
                {
                    byte[] x = new byte[32 - overhead]; 
                    x[0] = 86;
                }
            }
            else
            {
                byte[] x = new byte[1024 * 1024 - overhead];
                x[0] = 86;
            }
        }
    }

    public static int Main()
    {
        int test = 0;
        try
        {
            test++; TestZero();
            test++; TestNegative();
            test++; TestNull();
            test++; TestNotStarted();
            test++; TestExceed();
            test++; TestSimple(SOH);
            test++; TestSimple(LOH);
            test++; TestSimple(SOH); // Intentional repeat to make sure it works.
            test++; TestSimple(LOH);
            test++; TestCascade();
            test++; TestLoop();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test #{test} failed");
            Console.WriteLine(ex);
            return test;
        }
        Console.WriteLine($"All {test} tests passed");
        return 100;
    }
}

