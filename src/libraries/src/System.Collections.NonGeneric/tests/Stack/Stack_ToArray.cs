// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using Xunit;

public class Stack_ToArray
{
    public bool runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;

        Stack stk1;
        Object[] oArr;
        Int32 iNumberOfElements;
        String strValue;

        try
        {
            do
            {
                /////////////////////////  START TESTS ////////////////////////////
                ///////////////////////////////////////////////////////////////////

                //[] Vanila test case - this gives an object array of the values in the stack
                stk1 = new Stack();
                oArr = stk1.ToArray();

                iCountTestcases++;
                if (oArr.Length != 0)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_752dsg! Expected value not returned, " + oArr.Length);
                }

                iNumberOfElements = 10;
                for (int i = 0; i < iNumberOfElements; i++)
                    stk1.Push(i);

                oArr = stk1.ToArray();
                Array.Sort(oArr);

                iCountTestcases++;
                for (int i = 0; i < oArr.Length; i++)
                {
                    strValue = "Value_" + i;
                    if ((Int32)oArr[i] != i)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_7423fg! Expected value not returned, " + (Int32)oArr[i]);
                    }
                }
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

    [Fact]
    public static void ExecuteStack_ToArray()
    {
        bool bResult = false;
        var test = new Stack_ToArray();

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

