// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests GC.GetGeneration

using System;

public class GetGenerationTest
{
    private static int s_numTests = 0;

    private bool objectTest()
    {
        s_numTests++;
        Object obj = new Object();
        int g1 = GC.GetGeneration(obj);

        GC.Collect();

        int g2 = GC.GetGeneration(obj);

        if ((g1 == g2) && (g1 == GC.MaxGeneration))
        {
            Console.WriteLine("GCStress is on");
            Console.WriteLine("ObjectTest Passed!");
            return true;
        }

        if (g1 < g2)
        {
            Console.WriteLine("ObjectTest Passed!");
            return true;
        }

        Console.WriteLine("{0} {1}", g1, g2);
        Console.WriteLine("ObjectTest Failed!");
        return false;
    }


    private bool arrayTest()
    {
        s_numTests++;
        int[] arr = new int[25];
        int g1 = GC.GetGeneration(arr);

        GC.Collect();

        int g2 = GC.GetGeneration(arr);

        if ((g1 == g2) && (g1 == GC.MaxGeneration))
        {
            Console.WriteLine("GCStress is on");
            Console.WriteLine("ObjectTest Passed!");
            return true;
        }

        if (g1 < g2)
        {
            Console.WriteLine("arrayTest Passed!");
            return true;
        }

        Console.WriteLine("{0} {1}", g1, g2);
        Console.WriteLine("arrayTest Failed!");
        return false;
    }


    private bool failTest()
    {
        s_numTests++;

        Object obj = new Object();
        obj = null;

        try
        {
            GC.GetGeneration(obj);
        }
        catch (ArgumentNullException)
        {
            Console.WriteLine("failTest Passed!");
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected exception thrown:");
            Console.WriteLine(e);
        }

        Console.WriteLine("failTest Failed!");
        return false;
    }


    public bool RunTests()
    {
        int numPassed = 0;

        if (objectTest())
            numPassed++;

        if (arrayTest())
            numPassed++;

        if (failTest())
            numPassed++;


        Console.WriteLine();
        if (s_numTests == numPassed)
            return true;

        return false;
    }



    public static int Main()
    {
        GetGenerationTest t = new GetGenerationTest();

        if (t.RunTests())
        {
            Console.WriteLine("Test for GetGeneration() passed!");
            return 100;
        }


        Console.WriteLine("Test for GetGeneration() FAILED!");
        return 1;
    }
}
