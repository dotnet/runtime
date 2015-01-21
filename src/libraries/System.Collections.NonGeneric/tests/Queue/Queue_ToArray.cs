// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System;
using System.Collections;
using Xunit;

public class Queue_ToArray
{
    public virtual bool runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;

        //////// Testing Queue \ Remove (Object obj)
        Queue myQueue = null;
        String str1 = null;
        String str2 = null;
        Boolean mrBool = false;
        byte mrByte = Byte.MaxValue;
        Int16 mrInt2 = -2;
        Int32 mrInt4 = -2;
        Int64 mrInt8 = -2;
        Single mrSingle = -2;
        Double mrDouble = -2;
        Char mrChar = ' ';
        Object[] @object = null;

        // [] Queue is empty
        myQueue = new Queue();
        str1 = "test1";
        iCountTestcases++;
        if (myQueue.ToArray().Length != 0)
        {
            iCountErrors++;
        }

        // [] Insert several object and put them into an array without removing them from the Queue
        str1 = "test1";
        str2 = "test2";
        mrBool = true;
        mrByte = Byte.MaxValue;
        mrInt2 = Int16.MaxValue;
        mrInt4 = Int32.MaxValue;
        mrInt8 = Int64.MinValue;
        mrSingle = Single.MaxValue;
        mrDouble = Double.MinValue;
        mrChar = '\0';
        @object = new Object[10];

        myQueue.Enqueue(str1);
        myQueue.Enqueue(str2);
        myQueue.Enqueue(mrBool);
        myQueue.Enqueue(mrByte);
        myQueue.Enqueue(mrInt2);
        myQueue.Enqueue(mrInt4);
        myQueue.Enqueue(mrInt8);
        myQueue.Enqueue(mrSingle);
        myQueue.Enqueue(mrDouble);
        myQueue.Enqueue(mrChar);

        @object = myQueue.ToArray();
        iCountTestcases++;
        if (myQueue.Count != 10)
        {
            iCountErrors++;
        }

        iCountTestcases++;
        if (@object.Length != 10)
        {
            iCountErrors++;
        }
        iCountTestcases++;
        if (!Object.ReferenceEquals(@object[0], str1))
        {
            iCountErrors++;
        }
        iCountTestcases++;
        if (!Object.ReferenceEquals(@object[1], str2))
        {
            iCountErrors++;
        }
        iCountTestcases++;
        if ((Boolean)@object[2] != mrBool)
        {
            iCountErrors++;
        }
        iCountTestcases++;
        if ((byte)@object[3] != mrByte)
        {
            iCountErrors++;
        }
        iCountTestcases++;
        if ((Int16)@object[4] != mrInt2)
        {
            iCountErrors++;
        }
        iCountTestcases++;
        if ((Int32)@object[5] != mrInt4)
        {
            iCountErrors++;
        }
        iCountTestcases++;
        if ((Int64)@object[6] != mrInt8)
        {
            iCountErrors++;
        }
        iCountTestcases++;
        if ((Single)@object[7] != mrSingle)
        {
            iCountErrors++;
        }
        iCountTestcases++;
        if ((Double)@object[8] != mrDouble)
        {
            iCountErrors++;
        }
        iCountTestcases++;
        if ((Char)@object[9] != mrChar)
        {
            iCountErrors++;
        }

        // [] Insert several object, put them into an array and remove them from the Queue

        myQueue = new Queue();
        str1 = "test1";
        str2 = "test2";
        mrBool = true;
        mrByte = Byte.MaxValue;
        mrInt2 = Int16.MaxValue;
        mrInt4 = Int32.MaxValue;
        mrInt8 = Int64.MinValue;
        mrSingle = Single.MaxValue;
        mrDouble = Double.MinValue;
        mrChar = '\0';
        @object = new Object[10];

        myQueue.Enqueue(str1);
        myQueue.Enqueue(str2);
        myQueue.Enqueue(mrBool);
        myQueue.Enqueue(mrByte);
        myQueue.Enqueue(mrInt2);
        myQueue.Enqueue(mrInt4);
        myQueue.Enqueue(mrInt8);
        myQueue.Enqueue(mrSingle);
        myQueue.Enqueue(mrDouble);
        myQueue.Enqueue(mrChar);

        @object = myQueue.ToArray();

        iCountTestcases++;
        if (@object.Length != 10)
        {
            iCountErrors++;
        }
        if (myQueue.Count != 10)
        {
            iCountErrors++;
        }

        return iCountErrors == 0;
    }

    [Fact]
    public static void ExecuteQueue_ToArray()
    {
        bool bResult = false;
        var test = new Queue_ToArray();

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
