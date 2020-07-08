// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * TEST:        Casting
 * DESCRIPTION: Tests casting to and from IntPtrs.
 *              See also ToFromIntPtr.cs test.
 */

using System;
using System.Runtime.InteropServices;

public class Dummy
{
    public Dummy(int i)
    {
        this.i = i;
    }
    public int i;
}


public class CastingTest
{
    private int _numTests = 0;

    private bool CastTest()
    {
        _numTests++;

        int dummyValue = 101;

        GCHandle gch = GCHandle.Alloc(new Dummy(dummyValue));
        GCHandle gch2 = (GCHandle)((IntPtr)gch);

        bool success = (gch.Target == gch2.Target);

        gch.Free();

        if (success)
        {
            Console.WriteLine("CastTest Passed");
            return true;
        }

        Console.WriteLine("CastTest Failed");
        return false;
    }


    private bool FromZeroTest()
    {
        _numTests++;
        try
        {
            GCHandle gch3 = (GCHandle)IntPtr.Zero;
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("FromZeroTest Passed");
            return true;
        }
        catch (Exception)
        {
            Console.WriteLine("Unexpected Exception:");
        }

        Console.WriteLine("FromZeroTest Failed");
        return false;
    }


    private bool ToZeroTest()
    {
        _numTests++;

        GCHandle gch = GCHandle.Alloc(new Dummy(99));
        gch.Free();
        IntPtr intPtr = (IntPtr)gch;

        if (intPtr == IntPtr.Zero)
        {
            Console.WriteLine("ToZeroTest Passed");
            return true;
        }

        Console.WriteLine("ToZeroTest Failed");
        return false;
    }


    public bool RunTests()
    {
        int numPassed = 0;

        if (CastTest())
        {
            numPassed++;
        }

        if (ToZeroTest())
        {
            numPassed++;
        }

        if (FromZeroTest())
        {
            numPassed++;
        }

        Console.WriteLine();
        return (_numTests == numPassed);
    }


    public static int Main()
    {
        CastingTest t = new CastingTest();

        if (t.RunTests())
        {
            Console.WriteLine("CastingTest Passed!");
            return 100;
        }

        Console.WriteLine("CastingTest Failed!");
        return 1;
    }
}
