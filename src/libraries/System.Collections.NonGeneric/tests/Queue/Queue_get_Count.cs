// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

public class Queue_get_Count
{
    public virtual bool runTest()
    {
        // Local variables.
        int iCountErrors = 0;
        int iCountTestcases = 0;

        //--------------------------------------------------------------------------
        // Variable definitions.
        //--------------------------------------------------------------------------
        Queue myQueue = null;
        int nDefaultCapacity = 32;

        //
        // []	GetCount:	Validate new Queue is empty.

        iCountTestcases++;
        myQueue = new Queue();
        if (myQueue == null)
        {
            iCountErrors++;
            return false;
        }

        if (myQueue.Count != 0 || myQueue.Count != 0)
        {
            iCountErrors++;
        }

        // []	GetCount:	GetCount objects onto Queue and get size.
        for (int ii = 0; ii < nDefaultCapacity; ++ii)
            myQueue.Enqueue(ii);

        iCountTestcases++;
        if (myQueue.Count == 0 || myQueue.Count != nDefaultCapacity)
        {
            iCountErrors++;
        }

        //
        // []	GetCount:	Dequeue objects from Queue and get size.
        //
        iCountTestcases++;
        for (int ii = 0; ii < nDefaultCapacity / 2; ++ii)
            myQueue.Dequeue();

        if (myQueue.Count == 0 || myQueue.Count != nDefaultCapacity / 2)
        {
            iCountErrors++;
        }

        return iCountErrors == 0;
    }

    [Fact]
    public static void ExecuteQueue_get_Count()
    {
        bool bResult = false;
        var test = new Queue_get_Count();

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
