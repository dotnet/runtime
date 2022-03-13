// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* 
 * Tests GC.KeepAlive(obj), where obj is the Object reference whose
 * finalizer you don't want called until after the call to KeepAlive.
 *
 * Changes:
 *   -Added Dummy2 object whose finalizer should get called for comparison
 *
 * Notes:
 *   - passes with complus_jitminops set*
 *   - passes with complus_gcstress = 0,1,2,3,4
 *   - passes in debug mode
 */

using System;
using System.Runtime.CompilerServices;

public class Test_KeepAlive
{
    public static bool visited1 = false;
    public static bool visited2 = false;


    public class Dummy
    {
        ~Dummy()
        {
            // this finalizer should not get called until after
            // the call to GC.KeepAlive(obj)
            Console.WriteLine("In Finalize() of Dummy");
            visited1 = true;
        }
    }


    public class Dummy2
    {
        ~Dummy2()
        {
            // this finalizer should get called after
            // the call to GC.WaitForPendingFinalizers()
            Console.WriteLine("In Finalize() of Dummy2");
            visited2 = true;
        }
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void RunTest2()
    {
        Dummy2 obj2 = new Dummy2();
        obj2 = null;
    }


    public static bool RunTest()
    {
        bool success = false;

        Dummy obj = new Dummy();

        RunTest2();

        // *uncomment the for loop to make test fail with complus_jitminops set
        // by design as per briansul

        //for (int i=0; i<5; i++) {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        //}

        success = (visited1 == false) && (visited2 == true);

        GC.KeepAlive(obj);  // will keep obj alive until this point

        return success;
    }

    public static int Main()
    {
        bool success = RunTest();

        if (success)
        {
            Console.WriteLine("Test for KeepAlive() passed!");
            return 100;
        }
        else
        {
            Console.WriteLine("Test for KeepAlive() failed!");
            return 1;
        }
    }
}
