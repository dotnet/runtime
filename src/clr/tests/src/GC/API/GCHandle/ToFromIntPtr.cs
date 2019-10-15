// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * TEST:        ToFromIntPtrTest
 * DESCRIPTION: Added ToIntPtr and FromIntPtr methods to adhere to FXCop rule "OperatorOverloadsHaveNamedAlternativeMethods".
 *              See also Casting.cs test.
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


public class ToFromIntPtrTest
{
    private int _numTests = 0;

    private bool ToFromTest()
    {
        _numTests++;

        int dummyValue = 101;

        GCHandle gch = GCHandle.Alloc(new Dummy(dummyValue));
        GCHandle gch2 = GCHandle.FromIntPtr(GCHandle.ToIntPtr(gch));

        bool success = (gch.Target == gch2.Target);

        gch.Free();

        if (success)
        {
            Console.WriteLine("ToFromTest Passed");
            return true;
        }

        Console.WriteLine("ToFromTest Failed");
        return false;
    }


    private bool FromZeroTest()
    {
        _numTests++;
        try
        {
            GCHandle gch3 = GCHandle.FromIntPtr(IntPtr.Zero);
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("FromZeroTest Passed");
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected Exception:");
            //Console.WriteLine(e);
        }

        Console.WriteLine("FromZeroTest Failed");
        return false;
    }


    private bool ToZeroTest()
    {
        _numTests++;

        GCHandle gch = GCHandle.Alloc(new Dummy(99));
        gch.Free();
        IntPtr intPtr = GCHandle.ToIntPtr(gch);

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

        if (ToFromTest())
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
        ToFromIntPtrTest t = new ToFromIntPtrTest();

        if (t.RunTests())
        {
            Console.WriteLine("ToFromIntPtrTest Passed!");
            return 100;
        }

        Console.WriteLine("ToFromIntPtrTest Failed!");
        return 1;
    }
}
