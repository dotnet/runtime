// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public struct TestStruct
{
    public int icount;
}

public class ReturnStruct
{
    public static TestStruct RecursiveMethod_Inline(TestStruct teststruct, int c)
    {
        if (0 != c)
        {
            --c;
            teststruct.icount = c;
            return RecursiveMethod_Inline(teststruct, c);
        }
        return teststruct;
    }
    public static int Main()
    {
        int iret = 100;
        TestStruct ts;

        ts.icount = 10;
        TestStruct newts = RecursiveMethod_Inline(ts, 10);

        if (newts.icount != 0)
        {
            Console.WriteLine("FAIL: wrong return values at count=10");
            iret = 10;
        }
        ts.icount = 20;
        newts = RecursiveMethod_Inline(ts, 20);

        if (newts.icount != 0)
        {
            Console.WriteLine("FAIL: wrong return values at count=20");
            iret = 20;
        }
        if (iret == 100)
            Console.WriteLine("values ok");
        return iret;
    }
}


