// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this test verifies that there is no deadlock when we do pumping inside a class constructor 
// from an STA thread.


using System;
using Xunit;

public class MyWaitForPendingFinalizersClass
{
    public MyWaitForPendingFinalizersClass()
    {
        Console.WriteLine("Inside MyWaitForPendingFinalizersClass cctor");

        // Wait for all finalizers to complete before continuing.
        // This is essentially a way to pump in CLR since we are suspending the
        // current thread until the thread processing the finalization queue has 
        // emptied that queue.
        // For more info on this see cbrumme's blogg posting on Pumping in the CLR.
        GC.WaitForPendingFinalizers();

        Console.WriteLine("End of MyWaitForPendingFinalizersClass cctor");
    }
}

class MyFinalizeObject
{
    ~MyFinalizeObject()
    {
        Console.WriteLine("Finalizing a MyFinalizeObject");
    }
}

public class Test_pumpFromCctor
{
    // We can increase this number to fill up more memory.
    const int numMfos = 10;
    // We can increase this number to cause more
    // post-finalization work to be done.
    const int maxIterations = 10;

    [Fact]
    public static void TestEntryPoint()
    {
        MyFinalizeObject mfo;

        // Create objects that require finalization.
        for (int j = 0; j < numMfos; j++)
        {
            mfo = new MyFinalizeObject();
        }

        //Force garbage collection.
        // all finalizable objects will be placed in Finalization queue.
        GC.Collect();

        MyWaitForPendingFinalizersClass cl = new MyWaitForPendingFinalizersClass();

        // Worker loop to perform post-finalization code.
        for (int i = 0; i < maxIterations; i++)
        {
            Console.WriteLine("Doing some post-finalize work");
        }

        // if we got to this point, the test passed since no deadlock happened 
        // inside MyWaitForPendingFinalizersClass class constructor.
    }
}
