// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using Xunit;

public class Queue_Clone
{
    public bool runTest()
    {
        //////////// Global Variables used for all tests

        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;

        Queue que;
        Queue queClone;

        Object dequeuedValue;

        A a1;
        A a2;

        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            //[]vanilla 
            iCountTestcases++;

            que = new Queue();

            for (int i = 0; i < 100; i++)
                que.Enqueue(i);

            queClone = (Queue)que.Clone();
            if (queClone.Count != 100)
            {
                iCountErrors++;
                Console.WriteLine("Err_93745sdg! wrong value returned");
            }

            for (int i = 0; i < 100; i++)
            {
                if (!queClone.Contains(i))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_93475sdg! wrong value returned");
                }
            }

            //[]making sure that this is shallow
            iCountTestcases++;

            que = new Queue();

            que.Enqueue(new A(10));

            queClone = (Queue)que.Clone();
            if (queClone.Count != 1)
            {
                iCountErrors++;
                Console.WriteLine("Err_93745sdg! wrong value returned");
            }

            a1 = (A)que.Dequeue();
            a1.I = 50;

            if (queClone.Count != 1)
            {
                iCountErrors++;
                Console.WriteLine("Err_93745sdg! wrong value returned");
            }

            a2 = (A)queClone.Dequeue();

            if (a2.I != 50)
            {
                iCountErrors++;
                Console.WriteLine("Err_93745sdg! wrong value returned, " + a2.I);
            }

            //[]Empty Que
            iCountTestcases++;

            que = new Queue();
            queClone = (Queue)que.Clone();

            if (queClone.Count != 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_4488ajied! Count wrong value returned Expected={0} Actual={1}", 0, queClone.Count);
            }

            queClone.Enqueue(100);

            if ((int)(dequeuedValue = queClone.Dequeue()) != 100)
            {
                iCountErrors++;
                Console.WriteLine("Err_45884ajeiod! wrong value returned Expected={0} Actual={1}", 100, dequeuedValue);
            }

            //[]After Clear
            iCountTestcases++;

            que = new Queue();

            //    Insert 50 items in the Queue
            for (int i = 0; i < 100; i++)
            {
                que.Enqueue(i);
            }

            que.Clear();
            queClone = (Queue)que.Clone();

            if (queClone.Count != 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_4488ajied! Count wrong value returned Expected={0} Actual={1}", 0, queClone.Count);
            }

            queClone.Enqueue(100);

            if ((int)(dequeuedValue = queClone.Dequeue()) != 100)
            {
                iCountErrors++;
                Console.WriteLine("Err_51188ajeid! wrong value returned Expected={0} Actual={1}", 100, dequeuedValue);
            }

            //[]After Dequeue all items
            iCountTestcases++;

            que = new Queue();

            //    Insert 50 items in the Queue
            for (int i = 0; i < 100; i++)
            {
                que.Enqueue(i);
            }

            for (int i = 0; i < 100; i++)
            {
                que.Dequeue();
            }

            queClone = (Queue)que.Clone();

            if (queClone.Count != 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_418aheid! Count wrong value returned Expected={0} Actual={1}", 0, queClone.Count);
            }

            queClone.Enqueue(100);

            if ((int)(dequeuedValue = queClone.Dequeue()) != 100)
            {
                iCountErrors++;
                Console.WriteLine("Err_48088ahide! wrong value returned Expected={0} Actual={1}", 100, dequeuedValue);
            }

            //[]Clone then Dequeue then EnQueue
            iCountTestcases++;

            que = new Queue();

            //    Insert 50 items in the Queue
            for (int i = 0; i < 100; i++)
            {
                que.Enqueue(i);
            }

            queClone = (Queue)que.Clone();

            if (queClone.Count != 100)
            {
                iCountErrors++;
                Console.WriteLine("Err_418aheid! Count wrong value returned Expected={0} Actual={1}", 100, queClone.Count);
            }

            if ((int)(dequeuedValue = queClone.Dequeue()) != 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_48088ahide! wrong value returned Expected={0} Actual={1}", 0, dequeuedValue);
            }

            queClone.Enqueue(100);

            if ((int)(dequeuedValue = queClone.Dequeue()) != 1)
            {
                iCountErrors++;
                Console.WriteLine("Err_51084aheid! wrong value returned Expected={0} Actual={1}", 1, dequeuedValue);
            }

            //[] Our Clone implementation is not working because we are not copying the pointers - see bug 7447
            iCountTestcases++;

            que = new Queue();

            for (int i = 0; i < 100; i++)
                que.Enqueue(i);

            que.Dequeue();
            queClone = (Queue)que.Clone();
            if ((int)(dequeuedValue = queClone.Dequeue()) != 1)
            {
                iCountErrors++;
                Console.WriteLine("Err_9347sfg! wrong value returned Expected={0} Actual={1}", 1, dequeuedValue);
            }

            //[] Wrap the queue Dequeue then Enqueue
            iCountTestcases++;

            que = new Queue(100);

            //    Insert 50 items in the Queue
            for (int i = 0; i < 50; i++)
            {
                que.Enqueue(i);
            }

            //    Insert and Remove 75 items in the Queue this should wrap the queue 
            //    Where there is 25 at the end of the array and 25 at the beginning
            for (int i = 0; i < 75; i++)
            {
                que.Enqueue(i + 50);
                que.Dequeue();
            }

            queClone = (Queue)que.Clone();

            if (50 != queClone.Count)
            {
                iCountErrors++;
                Console.WriteLine("Err_154488ahjeid! Count wrong value returned Expected={0} Actual={1}", 50, queClone.Count);
            }

            if ((int)(dequeuedValue = queClone.Dequeue()) != 75)
            {
                iCountErrors++;
                Console.WriteLine("Err_410848ajeid! wrong value returned Expected={0} Actual={1}", 50, dequeuedValue);
            }

            //    Add an item to the Queue
            queClone.Enqueue(100);

            if (50 != queClone.Count)
            {
                iCountErrors++;
                Console.WriteLine("Err_152180ajekd! Count wrong value returned Expected={0} Actual={1}", 50, queClone.Count);
            }

            if ((int)(dequeuedValue = queClone.Dequeue()) != 76)
            {
                iCountErrors++;
                Console.WriteLine("Err_5154ejhae! wrong value returned Expected={0} Actual={1}", 51, dequeuedValue);
            }

            //[] Wrap the queue Enqueue then Dequeue
            iCountTestcases++;

            que = new Queue(100);

            //    Insert 50 items in the Queue
            for (int i = 0; i < 50; i++)
            {
                que.Enqueue(i);
            }

            //    Insert and Remove 75 items in the Queue this should wrap the queue 
            //    Where there is 25 at the end of the array and 25 at the beginning
            for (int i = 0; i < 75; i++)
            {
                que.Enqueue(i + 50);
                que.Dequeue();
            }

            queClone = (Queue)que.Clone();

            if (50 != queClone.Count)
            {
                iCountErrors++;
                Console.WriteLine("Err_20845ajed! Count wrong value returned Expected={0} Actual={1}", 50, queClone.Count);
            }

            //    Add an item to the Queue
            queClone.Enqueue(100);

            if ((int)(dequeuedValue = queClone.Dequeue()) != 75)
            {
                iCountErrors++;
                Console.WriteLine("Err_15684ajed! wrong value returned Expected={0} Actual={1}", 50, dequeuedValue);
            }

            //    Add an item to the Queue
            queClone.Enqueue(101);

            if (51 != queClone.Count)
            {
                iCountErrors++;
                Console.WriteLine("Err_51588jhed! Count wrong value returned Expected={0} Actual={1}", 51, queClone.Count);
            }

            if ((int)(dequeuedValue = queClone.Dequeue()) != 76)
            {
                iCountErrors++;
                Console.WriteLine("Err_8089aied! wrong value returned Expected={0} Actual={1}", 51, dequeuedValue);
            }

            //[]vanilla with synchronized queue
            iCountTestcases++;

            que = new Queue();

            for (int i = 0; i < 100; i++)
                que.Enqueue(i);

            queClone = (Queue)(Queue.Synchronized(que)).Clone();
            if (queClone.Count != 100)
            {
                iCountErrors++;
                Console.WriteLine("Err_2072asfd! Expected Count=100 actual={0}", queClone.Count);
            }

            if (!queClone.IsSynchronized)
            {
                iCountErrors++;
                Console.WriteLine("Err_1723 Expected Synchronized queue");
            }

            for (int i = 0; i < 100; i++)
            {
                if (!queClone.Contains(i))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2721saas! wrong value returned");
                }
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
    public static void ExecuteQueue_Clone()
    {
        bool bResult = false;
        var test = new Queue_Clone();

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


class A
{
    private int _i;
    public A(int i)
    {
        this._i = i;
    }
    internal Int32 I
    {
        set { _i = value; }
        get { return _i; }
    }
}
