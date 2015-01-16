// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using System.Threading.Tasks;
using Xunit;

public class Queue_get_SyncRoot
{
    private Queue _arrDaughter;
    private Queue _arrGrandDaughter;

    private const Int32 iNumberOfElements = 100;

    public bool runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;

        Queue arrSon;
        Queue arrMother;

        Task[] workers;
        Action ts1;
        Action ts2;
        Int32 iNumberOfWorkers = 1000;

        try
        {
            do
            {
                /////////////////////////  START TESTS ////////////////////////////
                ///////////////////////////////////////////////////////////////////

                arrMother = new Queue();
                for (int i = 0; i < iNumberOfElements; i++)
                {
                    arrMother.Enqueue(i);
                }

                if (arrMother.SyncRoot.GetType() != typeof(Object))
                {
                    iCountErrors++;
                }

                arrSon = Queue.Synchronized(arrMother);
                _arrGrandDaughter = Queue.Synchronized(arrSon);
                _arrDaughter = Queue.Synchronized(arrMother);

                iCountTestcases++;
                if (arrSon.SyncRoot != arrMother.SyncRoot)
                {
                    iCountErrors++;
                }

                iCountTestcases++;
                if (arrSon.SyncRoot != arrMother.SyncRoot)
                {
                    iCountErrors++;
                }

                iCountTestcases++;
                if (_arrGrandDaughter.SyncRoot != arrMother.SyncRoot)
                {
                    iCountErrors++;
                }

                iCountTestcases++;
                if (_arrDaughter.SyncRoot != arrMother.SyncRoot)
                {
                    iCountErrors++;
                }

                iCountTestcases++;
                if (arrSon.SyncRoot != arrMother.SyncRoot)
                {
                    iCountErrors++;
                }

                //we are going to rumble with the Queues with 2 threads

                workers = new Task[iNumberOfWorkers];
                ts1 = new Action(SortElements);
                ts2 = new Action(ReverseElements);
                for (int iThreads = 0; iThreads < iNumberOfWorkers; iThreads += 2)
                {
                    workers[iThreads] = Task.Factory.StartNew(ts1, TaskCreationOptions.LongRunning);
                    workers[iThreads + 1] = Task.Factory.StartNew(ts2, TaskCreationOptions.LongRunning);
                }

                Task.WaitAll(workers);
            } while (false);
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine(" : Error Err_8888yyy! exc_general==\n" + exc_general.ToString());
        }

        return iCountErrors == 0;
    }


    void SortElements()
    {
        _arrGrandDaughter.Clear();
        for (int i = 0; i < iNumberOfElements; i++)
        {
            _arrGrandDaughter.Enqueue(i);
        }
    }

    void ReverseElements()
    {
        _arrDaughter.Clear();
        for (int i = 0; i < iNumberOfElements; i++)
        {
            _arrDaughter.Enqueue(i);
        }
    }

    [Fact]
    public static void ExecuteQueue_get_SyncRoot()
    {
        bool bResult = false;
        var test = new Queue_get_SyncRoot();

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