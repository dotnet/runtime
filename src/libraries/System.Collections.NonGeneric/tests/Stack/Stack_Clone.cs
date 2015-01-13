// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using Xunit;

public class Stack_Clone
{
    public bool runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;

        Stack stk;
        Stack stkClone;

        A a1;
        A a2;

        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            //[]vanila 
            iCountTestcases++;

            stk = new Stack();

            for (int i = 0; i < 100; i++)
                stk.Push(i);

            stkClone = (Stack)stk.Clone();
            if (stkClone.Count != 100)
            {
                iCountErrors++;
                Console.WriteLine("Err_93745sdg! wrong value returned");
            }

            for (int i = 0; i < 100; i++)
            {
                if (!stkClone.Contains(i))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_93475sdg! wrong value returned");
                }
            }

            //[]making sure that this is shallow
            iCountTestcases++;

            stk = new Stack();

            stk.Push(new A(10));

            stkClone = (Stack)stk.Clone();
            if (stkClone.Count != 1)
            {
                iCountErrors++;
                Console.WriteLine("Err_93745sdg! wrong value returned");
            }

            a1 = (A)stk.Pop();
            a1.I = 50;

            if (stkClone.Count != 1)
            {
                iCountErrors++;
                Console.WriteLine("Err_93745sdg! wrong value returned");
            }

            a2 = (A)stkClone.Pop();

            if (a2.I != 50)
            {
                iCountErrors++;
                Console.WriteLine("Err_93745sdg! wrong value returned, " + a2.I);
            }

            //[]vanila with synchronized stack
            iCountTestcases++;

            stk = new Stack();

            for (int i = 0; i < 100; i++)
                stk.Push(i);

            stkClone = (Stack)(Stack.Synchronized(stk)).Clone();
            if (stkClone.Count != 100)
            {
                iCountErrors++;
                Console.WriteLine("Err_2072asfd! Expected Count=100 actual={0}", stkClone.Count);
            }

            if (!stkClone.IsSynchronized)
            {
                iCountErrors++;
                Console.WriteLine("Err_1723 Expected Synchronized Stack");
            }

            for (int i = 0; i < 100; i++)
            {
                if (!stkClone.Contains(i))
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
    public static void ExecuteStack_Clone()
    {
        bool bResult = false;
        var test = new Stack_Clone();

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
