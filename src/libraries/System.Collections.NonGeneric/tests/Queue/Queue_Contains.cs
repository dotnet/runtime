// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using Xunit;

public class Queue_Contains
{
    public bool runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;

        Queue que;

        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            //[]vanilla - Contains is a straight forward API
            iCountTestcases++;

            que = new Queue();

            for (int i = 0; i < 100; i++)
                que.Enqueue(i);

            if (que.Count != 100)
            {
                iCountErrors++;
                Console.WriteLine("Err_93745sdg! wrong value returned");
            }

            for (int i = 0; i < 100; i++)
            {
                if (!que.Contains(i))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_93475sdg! wrong value returned");
                }
            }

            //[]bogus value
            iCountTestcases++;
            if (que.Contains(150))
            {
                iCountErrors++;
                Console.WriteLine("Err_93475sdg! wrong value returned");
            }

            if (que.Contains("Hello World"))
            {
                iCountErrors++;
                Console.WriteLine("Err_93475sdg! wrong value returned");
            }

            //[]null
            iCountTestcases++;
            if (que.Contains(null))
            {
                iCountErrors++;
                Console.WriteLine("Err_3276sfg! wrong value returned");
            }

            que.Enqueue(null);
            if (!que.Contains(null))
            {
                iCountErrors++;
                Console.WriteLine("Err_9734sg! wrong value returned");
            }


            //[]vanilla - Contains is a straight forward API SYNCHRONIZED
            iCountTestcases++;

            que = new Queue();
            que = Queue.Synchronized(que);

            for (int i = 0; i < 100; i++)
                que.Enqueue(i);

            if (que.Count != 100)
            {
                iCountErrors++;
                Console.WriteLine("Err_1168sada! wrong value returned");
            }

            for (int i = 0; i < 100; i++)
            {
                if (!que.Contains(i))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_1659asd! wrong value returned");
                }
            }

            //[]bogus value SYNCHRONIZED
            iCountTestcases++;
            if (que.Contains(150))
            {
                iCountErrors++;
                Console.WriteLine("Err_9798asdf! wrong value returned");
            }

            if (que.Contains("Hello World"))
            {
                iCountErrors++;
                Console.WriteLine("Err_5648jhjj! wrong value returned");
            }

            //[]null SYNCHRONIZED
            iCountTestcases++;
            if (que.Contains(null))
            {
                iCountErrors++;
                Console.WriteLine("Err_64989hjkl! wrong value returned");
            }

            que.Enqueue(null);
            if (!que.Contains(null))
            {
                iCountErrors++;
                Console.WriteLine("Err_9879jhjh! wrong value returned");
            }
            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine(" : Error Err_8888yyy! exc_general==" + exc_general.ToString());
        }
        ////  Finish Diagnostics

        if (iCountErrors == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    [Fact]
    public static void ExecuteQueue_Contains()
    {
        bool bResult = false;
        var test = new Queue_Contains();

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


