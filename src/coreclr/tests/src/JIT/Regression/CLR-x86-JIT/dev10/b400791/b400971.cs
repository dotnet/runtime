// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

/*
   AV in mscorwks!WKS::gc_heap::mark_object_simple1
*/

using System;

class ByRef_GCHole
{
    static volatile int returnCode = 0;
    ~ByRef_GCHole()
    {
        if (returnCode == 0)
        {
            Console.WriteLine("FAILED: Collected the wrong object!");
            returnCode = 99;
        }
    }

    static void DoSomething(ref ByRef_GCHole p)
    {
        try
        {
            if (returnCode == 0)
            {
                Console.WriteLine(p.ToString() + "passed");
                returnCode = 100;
            }
        }
        catch
        {
            Console.WriteLine("FAILED: Object is invalid!");
            returnCode = 98;
        }
    }

    static int Main()
    {
        ByRef_GCHole h;

        // NOTE: After talking to Grant, the if else below is necessary, because a if/else is 
        // required for the problem to occur and the jit should not know which branch 
        // is going to be executed. That's where the volatile static int comes into play.
        if (returnCode == 0)
        {
            h = new ByRef_GCHole();
        }
        else
        {
            h = null;
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();

        DoSomething(ref h);

        return returnCode;
    }
}
