// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System;
using System.Collections;
using Xunit;

public class Queue_ctor
{
    public virtual bool runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;

        Queue myQueue = null;
        String str1 = null;

        // [] Constructor Queue using specified defaults
        myQueue = new Queue();
        iCountTestcases++;
        if (myQueue == null)
        {
            iCountErrors++;
            print("E_839k");
        }

        // [] Check capacity increase
        int defaultCap = 32;
        myQueue = new Queue();
        str1 = "test";
        for (int i = 0; i <= defaultCap; i++)
        {
            myQueue.Enqueue(str1);
        }
        iCountTestcases++;
        if (myQueue.Count != defaultCap + 1)
        {
            iCountErrors++;
            print("E_34aj");
        }

        ///// Finish diagnostics and reporting of results.
        return iCountErrors == 0;
    }

    ////// Print helper method
    private void print(String error)
    {
        StringBuilder output = new StringBuilder("POINTTOBREAK: find ");
        output.Append(error);
        output.Append(" (Queue_ctor.cs)");
        Console.Out.WriteLine(output.ToString());
    }


    [Fact]
    public static void ExecuteQueue_ctor()
    {
        bool bResult = false;
        var test = new Queue_ctor();

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
