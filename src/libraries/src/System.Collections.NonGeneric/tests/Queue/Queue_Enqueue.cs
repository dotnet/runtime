// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

public class Queue_Enqueue
{

    public virtual bool runTest()
    {
        // Local variables.
        bool bPass = true;
        int iCountErrors = 0;
        int iCountTestcases = 0;

        //--------------------------------------------------------------------------
        // Variable definitions.
        //--------------------------------------------------------------------------
        Queue myQueue = null;

        //
        // []	Enqueue:	Enqueue objects onto the queue.
        //
        myQueue = new Queue();
        iCountTestcases++;
        if (myQueue == null || myQueue.Count != 0)
        {
            iCountErrors++;
            return false;
        }

        // Enqueue 1000 integers onto the queue.
        for (int ii = 1; ii <= 1000; ++ii)
        {
            iCountTestcases++;
            // Enqueue object on stack.
            myQueue.Enqueue(ii);

            // Verify that item got enqueued.
            if (myQueue.Count != ii)
            {
                bPass = false;
                iCountErrors++;
                break;
            }
        }

        // [] Verify that 1000 object are on the queue
        iCountTestcases++;
        if (myQueue.Count != 1000)
        {
            iCountErrors++;
            bPass = false;
        }

        //
        // []	Enqueue:	Enqueue null object into the stack.
        //
        iCountTestcases++;
        try
        {
            // Attempt to enqueue null object. should work!
            myQueue.Enqueue(null);
        }
        catch (Exception)
        {
            bPass = false;
            iCountErrors++;
        }

        return bPass;
    }


    [Fact]
    public static void ExecuteQueue_Enqueue()
    {
        bool bResult = false;
        var test = new Queue_Enqueue();

        try
        {
            bResult = test.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("Fail! Error Err_main! Uncaught Exception in main(), exc_main==" + exc_main);
        }

        Assert.True(bResult);
    }

}
