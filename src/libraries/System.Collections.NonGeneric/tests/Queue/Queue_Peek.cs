// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System;
using System.Collections;
using Xunit;

public class Queue_Peek
{
    public virtual bool runTest()
    {
        int iCountErrors = 0;
        int iCountTestcases = 0;

        //////// Testing Queue \ Peek()

        Queue myQueue = null;
        String str1 = null;
        String str2 = null;
        Boolean mrBool;
        byte mrByte = (byte)0;
        Int16 mrInt2 = 0;
        Int32 mrInt4 = 0;
        Int64 mrInt8 = 0L;
        Single mrSingle = (float)0.0;
        Double mrDouble = (double)0.0;
        Char mrChar = (Char)('\0');

        // [] Should throw InvalidOperationException if Queue empty
        myQueue = new Queue();
        iCountTestcases++;
        try
        {
            myQueue.Peek();
            ++iCountErrors;
        }
        catch (InvalidOperationException)
        { }
        catch (Exception)
        {
            ++iCountErrors;
        }

        // [] Inserting several object and checking them with peek
        str1 = "test1";
        str2 = "test2";
        mrBool = (Boolean)(true);
        mrByte = (byte)(127);
        //    mrByte = (Byte)(Byte.MaxValue);
        mrInt2 = (Int16)(Int16.MaxValue);
        mrInt4 = (Int32)(Int32.MaxValue);
        mrInt8 = (Int64)(Int64.MinValue);
        mrSingle = (Single)(Single.MaxValue);
        mrDouble = (Double)(Double.MinValue);

        myQueue.Enqueue(str1);
        myQueue.Enqueue(str2);
        myQueue.Enqueue(mrBool);
        myQueue.Enqueue(mrByte);
        myQueue.Enqueue(mrInt2);
        myQueue.Enqueue(mrInt4);
        myQueue.Enqueue(mrInt8);
        myQueue.Enqueue(mrSingle);
        myQueue.Enqueue(mrDouble);

        iCountTestcases++;
        if (!Object.ReferenceEquals(myQueue.Peek(), str1))
        {
            iCountErrors++;
        }
        iCountTestcases++;
        myQueue.Dequeue();
        if (!Object.ReferenceEquals(myQueue.Peek(), str2))
        {
            iCountErrors++;
        }
        iCountTestcases++;
        myQueue.Dequeue();
        if (Convert.ToBoolean(myQueue.Peek()) != mrBool)
        {
            iCountErrors++;
        }
        iCountTestcases++;
        myQueue.Dequeue();
        if (Convert.ToByte(myQueue.Peek()) != mrByte)
        {
            iCountErrors++;
        }
        iCountTestcases++;
        myQueue.Dequeue();
        if (Convert.ToInt16(myQueue.Peek()) != mrInt2)
        {
            iCountErrors++;
        }
        iCountTestcases++;
        myQueue.Dequeue();
        if (Convert.ToInt32(myQueue.Peek()) != mrInt4)
        {
            iCountErrors++;
        }
        iCountTestcases++;
        myQueue.Dequeue();
        if (Convert.ToInt64(myQueue.Peek()) != mrInt8)
        {
            iCountErrors++;
        }
        myQueue.Dequeue();
        iCountTestcases++;
        if (Convert.ToSingle(myQueue.Peek()) != mrSingle)
        {
            iCountErrors++;
        }
        myQueue.Dequeue();
        iCountTestcases++;
        if (Convert.ToDouble(myQueue.Peek()) != mrDouble)
        {
            iCountErrors++;
        }

        myQueue.Enqueue(mrChar);
        iCountTestcases++;
        if (Convert.ToDouble(myQueue.Peek()) != mrDouble)
        {
            iCountErrors++;
        }
        myQueue.Dequeue();
        iCountTestcases++;
        if (Convert.ToChar(myQueue.Dequeue()) != mrChar)
        {
            iCountTestcases++;
        }

        return (iCountErrors == 0);
    }


    [Fact]
    public static void ExecuteQueue_Peek()
    {
        bool bResult = false;
        var test = new Queue_Peek();

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
