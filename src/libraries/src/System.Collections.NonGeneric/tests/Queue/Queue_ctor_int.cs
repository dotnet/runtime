// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System;
using System.Collections;
using Xunit;

public class Queue_ctor_int
{
    public virtual bool runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;

        //////// Testing Queue \ Queue(int capacity)
        Queue myQueue = null;
        String str1 = null;
        int defaultCap = 32;

        // [] Constructor Queue using specified defaults
        myQueue = new Queue(defaultCap);
        iCountTestcases++;
        if (myQueue == null)
        {
            iCountErrors++;
            print("E_839k");
        }

        defaultCap = 0;
        iCountTestcases++;
        try
        {
            myQueue = new Queue(defaultCap);
        }
        catch (Exception exc)
        {
            iCountErrors++;
            print("E_2jdd");
            Console.Error.WriteLine(exc);
        }


        // [] Check capacity increase
        defaultCap = 32;
        myQueue = new Queue(defaultCap);
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

        // [] Constructing Queue using invalid parameters
        defaultCap = -1;
        iCountTestcases++;
        try
        {
            myQueue = new Queue(defaultCap);
            iCountErrors++;
            print("E_3jsd");
        }
        catch (ArgumentException) { }
        catch (Exception exc2)
        {
            iCountErrors++;
            print("E_24ai");
            Console.Error.WriteLine(exc2);
        }

        return iCountErrors == 0;
    }


    ////// Print helper method
    private void print(String error)
    {
        StringBuilder output = new StringBuilder("POINTTOBREAK: find ");
        output.Append(error);
        output.Append(" (Queue_ctor_int.cs)");
        Console.Out.WriteLine(output.ToString());
    }


    [Fact]
    public static void ExecuteQueue_ctor_int()
    {
        bool bResult = false;
        var test = new Queue_ctor_int();

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
