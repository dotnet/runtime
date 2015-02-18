// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using Xunit;

public class Queue_TrimToSize
{
    public bool runTest()
    {
        //////////// Global Variables used for all tests
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;

        Queue que;
        Object dequeuedValue;

        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            iCountTestcases++;
            que = new Queue();

            try
            {
                que.TrimToSize();
            }
            catch (Exception)
            {
                iCountErrors++;
            }

            que = new Queue();
            for (int i = 0; i < 1000; i++)
            {
                que.Enqueue(i);
            }

            try
            {
                que.TrimToSize();
            }
            catch (Exception)
            {
                iCountErrors++;
            }

            que = new Queue(100000);

            try
            {
                que.TrimToSize();
            }
            catch (Exception)
            {
                iCountErrors++;
            }

            //[]Empty Queue
            iCountTestcases++;

            que = new Queue();
            que.TrimToSize();

            if (que.Count != 0)
            {
                iCountErrors++;
            }

            que.Enqueue(100);

            if ((int)(dequeuedValue = que.Dequeue()) != 100)
            {
                iCountErrors++;
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
            que.TrimToSize();

            if (que.Count != 0)
            {
                iCountErrors++;
            }

            que.Enqueue(100);
            if ((int)(dequeuedValue = que.Dequeue()) != 100)
            {
                iCountErrors++;
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

            que.TrimToSize();

            if (que.Count != 0)
            {
                iCountErrors++;
            }

            que.Enqueue(100);

            if ((int)(dequeuedValue = que.Dequeue()) != 100)
            {
                iCountErrors++;
            }

            //[]TrimToSize then Dequeue then EnQueue
            iCountTestcases++;

            que = new Queue();

            //    Insert 50 items in the Queue
            for (int i = 0; i < 100; i++)
            {
                que.Enqueue(i);
            }

            que.TrimToSize();

            if (que.Count != 100)
            {
                iCountErrors++;
            }

            if ((int)(dequeuedValue = que.Dequeue()) != 0)
            {
                iCountErrors++;
            }

            que.Enqueue(100);
            if ((int)(dequeuedValue = que.Dequeue()) != 1)
            {
                iCountErrors++;
                Console.WriteLine("Err_51084aheid! wrong value returned Expected={0} Actual={1}", 1, dequeuedValue);
            }

            //[] Wrap the queue then dequeue and enqueue
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

            que.TrimToSize();

            if (50 != que.Count)
            {
                iCountErrors++;
                Console.WriteLine("Err_154488ahjeid! Count wrong value returned Expected={0} Actual={1}", 50, que.Count);
            }

            if ((int)(dequeuedValue = que.Dequeue()) != 75)
            {
                iCountErrors++;
                Console.WriteLine("Err_410848ajeid! wrong value returned Expected={0} Actual={1}", 50, dequeuedValue);
            }

            //    Add an item to the Queue
            que.Enqueue(100);

            if (50 != que.Count)
            {
                iCountErrors++;
                Console.WriteLine("Err_152180ajekd! Count wrong value returned Expected={0} Actual={1}", 50, que.Count);
            }

            if ((int)(dequeuedValue = que.Dequeue()) != 76)
            {
                iCountErrors++;
                Console.WriteLine("Err_5154ejhae! wrong value returned Expected={0} Actual={1}", 51, dequeuedValue);
            }

            //[] Wrap the queue then enqueue and dequeue
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

            que.TrimToSize();

            if (50 != que.Count)
            {
                iCountErrors++;
                Console.WriteLine("Err_15418ajioed! Count wrong value returned Expected={0} Actual={1}", 50, que.Count);
            }

            //    Add an item to the Queue
            que.Enqueue(100);

            if ((int)(dequeuedValue = que.Dequeue()) != 75)
            {
                iCountErrors++;
                Console.WriteLine("Err_211508ajoied! wrong value returned Expected={0} Actual={1}", 50, dequeuedValue);
            }

            //    Add an item to the Queue
            que.Enqueue(101);

            if (51 != que.Count)
            {
                iCountErrors++;
                Console.WriteLine("Err_4055ajeid! Count wrong value returned Expected={0} Actual={1}", 51, que.Count);
            }

            if ((int)(dequeuedValue = que.Dequeue()) != 76)
            {
                iCountErrors++;
                Console.WriteLine("Err_440815ajkejid! wrong value returned Expected={0} Actual={1}", 51, dequeuedValue);
            }

            //[]vanilla SYNCHRONIZED

            iCountTestcases++;

            que = new Queue();
            que = Queue.Synchronized(que);

            try
            {
                que.TrimToSize();
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_1689asdfa! unexpected exception thrown," + ex.GetType().Name);
            }

            que = new Queue();
            que = Queue.Synchronized(que);
            for (int i = 0; i < 1000; i++)
            {
                que.Enqueue(i);
            }

            try
            {
                que.TrimToSize();
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_79446jhjk! unexpected exception thrown," + ex.GetType().Name);
            }

            que = new Queue(100000);
            que = Queue.Synchronized(que);

            try
            {
                que.TrimToSize();
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_4156hjkj! unexpected exception thrown," + ex.GetType().Name);
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

        return iCountErrors == 0;
    }


    [Fact]
    public static void ExecuteQueue_TrimToSize()
    {
        bool bResult = false;
        var test = new Queue_TrimToSize();

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

