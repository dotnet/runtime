// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Threading.Tasks;
using Xunit;

public class Stack_get_SyncRoot
{
    private Stack _arrDaughter;
    private Stack _arrGrandDaughter;

    private const Int32 iNumberOfElements = 100;

    public bool runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;

        Stack arrSon;
        Stack arrMother;

        Int32 iNumberOfWorkers = 1000;

        try
        {
            do
            {
                /////////////////////////  START TESTS ////////////////////////////
                ///////////////////////////////////////////////////////////////////

                //[] Vanila test case - testing SyncRoot is not as simple as its implementation looks like. This is the working
                //scenrio we have in mind.
                //1) Create your Down to earth mother Stack
                //2) Get a Fixed wrapper from it
                //3) Get a Synchronized wrapper from 2)
                //4) Get a synchronized wrapper of the mother from 1)
                //5) all of these should SyncRoot to the mother earth

                arrMother = new Stack();
                for (int i = 0; i < iNumberOfElements; i++)
                {
                    arrMother.Push(i);
                }

                if (arrMother.SyncRoot.GetType() != typeof(Object))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_47235fsd! Expected SyncRoot to be an object actual={0}", arrMother.SyncRoot.GetType());
                }

                arrSon = Stack.Synchronized(arrMother);
                _arrGrandDaughter = Stack.Synchronized(arrSon);
                _arrDaughter = Stack.Synchronized(arrMother);

                iCountTestcases++;
                if (arrSon.SyncRoot != arrMother.SyncRoot)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_234dnvf! Expected value not returned, " + (arrSon.SyncRoot == arrMother.SyncRoot));
                }

                iCountTestcases++;
                if (arrSon.SyncRoot != arrMother.SyncRoot)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_8753bdd! Expected value not returned, " + (arrSon.SyncRoot == arrMother));
                }

                iCountTestcases++;
                if (_arrGrandDaughter.SyncRoot != arrMother.SyncRoot)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_4927fd0fd! Expected value not returned, " + (_arrGrandDaughter.SyncRoot == arrMother));
                }

                iCountTestcases++;
                if (_arrDaughter.SyncRoot != arrMother.SyncRoot)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_85390gfg! Expected value not returned, " + (_arrDaughter.SyncRoot == arrMother));
                }

                iCountTestcases++;
                if (arrSon.SyncRoot != arrMother.SyncRoot)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_234dnvf! Expected value not returned, " + (arrSon.SyncRoot == arrMother.SyncRoot));
                }

                var ts1 = new Action(SortElements);
                var ts2 = new Action(ReverseElements);
                Task[] tasks = new Task[iNumberOfWorkers];
                for (int iThreads = 0; iThreads < iNumberOfWorkers; iThreads += 2)
                {
                    tasks[iThreads] = Task.Run(ts1);
                    tasks[iThreads + 1] = Task.Run(ts2);
                }
                Task.WaitAll(tasks);

                ///////////////////////////////////////////////////////////////////
                /////////////////////////// END TESTS /////////////////////////////
            } while (false);
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine(" : Error Err_8888yyy! exc_general==\n" + exc_general.ToString());
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

    void SortElements()
    {
        _arrGrandDaughter.Clear();
        for (int i = 0; i < iNumberOfElements; i++)
        {
            _arrGrandDaughter.Push(i);
        }
    }

    void ReverseElements()
    {
        _arrDaughter.Clear();
        for (int i = 0; i < iNumberOfElements; i++)
        {
            _arrDaughter.Push(i);
        }
    }

    [Fact]
    public static void ExecuteStack_get_SyncRoot()
    {
        bool bResult = false;
        var test = new Stack_get_SyncRoot();

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