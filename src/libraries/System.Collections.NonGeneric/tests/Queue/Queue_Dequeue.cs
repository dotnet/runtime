// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

public class Queue_Dequeue
{
    public virtual bool runTest()
    {
        // Local variables.
        bool bPass = true;
        int iCountTestcases = 0;
        int iCountErrors = 0;

        //--------------------------------------------------------------------------
        // Variable definitions.
        //--------------------------------------------------------------------------

        Queue myQueue = null;
        myQueue = new Queue();
        iCountTestcases++;
        if (myQueue == null || myQueue.Count != 0)
        {
            iCountErrors++;
            return false;
        }

        // []	Enqueue and Dequeue and object.
        for (int ii = 1; ii <= 1000; ++ii)
        {
            iCountTestcases++;
            // Dequeue object onto queue.
            myQueue.Enqueue((Int32)(ii));

            // Dequeue object from stack.
            Int32 myInt = (Int32)myQueue.Dequeue();
            if (myQueue.Count != 0 || (int)myInt != ii)
            {
                bPass = false;
                iCountErrors++;
                break;
            }
        }

        // Dequeue:	Dequeue 1000 object / Dequeue and verify objects.
        for (int ii = 1; ii <= 1000; ++ii)
            myQueue.Enqueue((Int32)(ii));

        iCountTestcases++;
        // Verify that 1000 objects has been Dequeued.
        if (myQueue.Count != 1000)
        {
            iCountErrors++;
            bPass = false;
        }

        // Dequeue and verify objects.
        for (int ii = 1; ii <= 1000; ++ii)
        {
            iCountTestcases++;
            Int32 myInt = (Int32)myQueue.Dequeue();
            if ((int)(myInt) != ii)
            {
                iCountErrors++;
                bPass = false;
                break;
            }
        }

        // []	Verify dequeue on an empty collection.
        iCountTestcases++;
        try
        {
            while (myQueue.Count != 0)
                myQueue.Dequeue();

            // Dequeue empty collection. should thrown InvalidOperationException
            myQueue.Dequeue();
            iCountErrors++;
            bPass = false;
        }
        catch (InvalidOperationException)
        { }
        catch (Exception ex)
        {
            Console.Error.Write(ex.ToString());
            Console.Error.WriteLine(" unexpected exception");
            iCountErrors++;
            bPass = false;
        }

        //
        // Dequeue:	Verify empty collection exception.
        //
        try
        {

            while (myQueue.Count != 0)
                myQueue.Dequeue();

            iCountTestcases++;
            myQueue.Dequeue();
            iCountErrors++;
            bPass = false;
        }
        catch (InvalidOperationException) { }
        catch (Exception ex)
        {
            bPass = false;
            iCountErrors++;
            Console.Error.Write(ex.ToString());
            Console.Error.WriteLine(" caught");
        }

        return bPass;
    }



    [Fact]
    public static void ExecuteQueue_Dequeue()
    {
        bool bResult = false;
        var test = new Queue_Dequeue();

        try
        {
            bResult = test.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("Fail! Error Err_main! Uncaught Exception in main(), exc_main==" + exc_main);
        }

        Assert.Equal(true, bResult);
    }
}
