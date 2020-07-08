// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

class InterlockedRead
{
    ManualResetEvent mre;
    long lCurr;
    static int success;
    const int expected = 5;

    public InterlockedRead()
    {
        mre = new ManualResetEvent(false);
    }

    public static int Main()
    {
        InterlockedRead ir = new InterlockedRead();
        ir.TestOne(Int64.MaxValue);
        ir.TestOne(Int64.MinValue);
        ir.TestOne(0);
        ir.TestOne(Int32.MaxValue);
        ir.TestOne(Int32.MinValue);
        Console.WriteLine(success == expected ? "Test Passed" : "Test Failed");
        return (success == expected) ? 100 : -1;
    }

    public void TestOne(long iValue)
    {
        bool bRet = true;
        lCurr = iValue;
        if(iValue != Interlocked.Read(ref lCurr))
            bRet = false;
        if(bRet)
            success++;
    }
}
