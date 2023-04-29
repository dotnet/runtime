// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public struct TestStruct
{
    public int f1;
    public int f2;
}
public class DelegateStruct
{
    private delegate TestStruct DelSt(TestStruct st, int x);

    private TestStruct DelMethod_Inline(TestStruct st, int x)
    {
        st.f1 = x;
        st.f2 = x * 2;
        return st;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int iret = 100;
        DelegateStruct ds = new DelegateStruct();
        DelSt d = new DelSt(ds.DelMethod_Inline);
        TestStruct ts;
        ts.f1 = 1;
        ts.f2 = 1;
        TestStruct tr = d(ts, 10);
        Console.WriteLine("tr f1={0} f2={1}", tr.f1, tr.f2);
        if (tr.f1 != 10 || tr.f2 != 20)
        {
            Console.WriteLine("FAIL");
            iret = 1;
        }
        else
        {
            Console.WriteLine("PASS");
        }
        return iret;
    }
}
