// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

public class Queue_ctor_int_float
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
        float nDefaultGrowFactor = (float)2.0;

        //
        // []	Construct queue using the specified defaults.
        //
        iCountTestcases++;
        myQueue = new Queue(nDefaultCapacity, nDefaultGrowFactor);
        if (myQueue == null)
        {
            iCountErrors++;
            return false;
        }

        //
        // []	Construct queue using invalid parameters.
        //
        iCountTestcases++;
        try
        {
            myQueue = new Queue(0, (float)0.0);
            iCountErrors++;
        }
        catch (System.ArgumentException)
        {
        }

        iCountTestcases++;
        try
        {
            myQueue = new Queue(-1, nDefaultGrowFactor);
            iCountErrors++;
        }
        catch (System.ArgumentException)
        {
        }
        iCountTestcases++;
        try
        {
            myQueue = new Queue(nDefaultCapacity, (float)30.0);
            iCountErrors++;
        }
        catch (System.ArgumentException)
        {
        }
        iCountTestcases++;
        try
        {
            myQueue = new Queue(nDefaultCapacity, (float)-30.0);
            iCountErrors++;
        }
        catch (System.ArgumentException)
        {
        }

        return iCountErrors == 0;
    }


    [Fact]
    public static void ExecuteQueue_ctor_int_float()
    {
        bool bResult = false;
        var test = new Queue_ctor_int_float();

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
