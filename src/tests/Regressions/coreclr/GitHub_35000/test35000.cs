// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Reflection;
using Xunit;

public class Test35000
{
    public class TestData0
    {
        public virtual object MyMethod(int a, int b, int c, int d, int e, int f, int g, int h) { return null; }
    }

    public class TestData1 : TestData0
    {
        public override object MyMethod(int a, int b, int c, int d, int e, int f, int g, int h) { return null; }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var method = typeof(TestData0).GetMethod(nameof(TestData0.MyMethod));
        var func = (Func<TestData0, int, int, int, int, int, int, int, int, object>)Delegate.CreateDelegate(typeof(Func<TestData0, int, int, int, int, int, int, int, int, object>), null, method);

        TestData0 data = new TestData0();
        TestData0 data1 = new TestData1();

        int nullRefCount = 0;

        const int LoopCount = 10;

        for (int j = 0; j < LoopCount; j++)
        {
            for (int i = 0; i < 50; i++)
            {
                func(data, 1, 2, 3, 4, 5, 6, 7, 8);
                func(data1, 1, 2, 3, 4, 5, 6, 7, 8);
            }

            try
            {
                func(null, 1, 2, 3, 4, 5, 6, 7, 8);
            }
            catch (NullReferenceException e)
            {
                nullRefCount++;
                Console.WriteLine(e);
            }
        }

        return (nullRefCount == LoopCount) ? 100 : 101;
    }
}
