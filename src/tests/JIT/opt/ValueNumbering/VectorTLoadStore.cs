// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using Xunit;

struct TestData 
{
    public int a;
    public Vector<int> b;
    public int c;

    public TestData()
    {
        a = 1234;
        b = Vector<int>.AllBitsSet;
        c = 5678;
    }
}

public class VectorTLoadStore
{
    static unsafe bool TestPointerStoreLocal()
    {
        Vector<int> data = new Vector<int>();
        *(int*)&data = 1234;
        *(int*)&data = 5678;
        *(int*)&data = 1234;
        *(int*)&data = 5678;
        return data[0] == 5678;

    }

    static unsafe bool TestInexactPointerStoreLocal()
    {
        Vector<int> data = new Vector<int>();
        *(Vector<int>*)&data = Vector<int>.One;
        *(Vector<int>*)&data = Vector<int>.Zero;
        *(Vector<int>*)&data = Vector<int>.One;
        *(Vector<int>*)&data = Vector<int>.Zero;
        return (data == Vector<int>.Zero);
    }

    static unsafe bool TestPointerStoreField()
    {
        TestData data = new TestData();
        *(int*)&(data.b) = 1234;
        *(int*)&(data.b) = 5678;
        *(int*)&(data.b) = 1234;
        *(int*)&(data.b) = 5678;
        return data.b[0] == 5678;
    }

    delegate bool TestFunction();

    [Fact]
    public unsafe static int TestEntryPoint()
    {
        int successCount = 0;
        int failCount = 0;


        TestFunction[] funcs = {
            TestPointerStoreLocal, TestInexactPointerStoreLocal,
            TestPointerStoreField,
        };
        
        foreach (var func in funcs)
        {
            if (func())
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        return (failCount == 0) ? 100 : 101;
    }
}