// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public struct Data
{
    public int _a;
    public bool _b;
    public long _c;
    
    public override bool Equals(object otherAsObj)
    {
        Data other = (Data)otherAsObj;
        return other._a == _a && other._b == _b && other._c == _c;
    }
}

// Test focuses on arm instantiating stub behavior

public struct StructAPITest<T>
{
    public string _id;
    
    // r0 = thisptr. r1 = instParam
    public int ReturnsIntNoParams()
    {
        Program.s_InstStr = typeof(StructAPITest<T>).ToString() + " - " + _id;
        return 123;
    }
    
    // r0 = thisptr. r1 = retBuffer. r2 = instParam
    public Data ReturnNeedBufferNoParams()
    {
        Program.s_InstStr = typeof(StructAPITest<T>).ToString() + " - " + _id;
        Data d; d._a = 123; d._b = true; d._c = 456;
        return d;
    }

    // r0 = thisptr. r1 = instParam. r2 = 'val'
    public int ReturnsIntSmallParam(int val)
    {
        Program.s_InstStr = typeof(StructAPITest<T>).ToString() + " - " + _id;
        return val;
    }

    // r0 = thisptr. r1 = instParam. r2+r3 = 'val'
    public int ReturnsIntDoubleRegisterParam(long val)
    {
        Program.s_InstStr = typeof(StructAPITest<T>).ToString() + " - " + _id;
        return (int)val;
    }

    // r0 = thisptr. r1 = retBuffer. r2 = instParam. r3 = 'val'
    public Data ReturnNeedBufferSmallParam(int val)
    {
        Program.s_InstStr = typeof(StructAPITest<T>).ToString() + " - " + _id;
        Data d; d._a = val; d._b = true; d._c = 999;
        return d;
    }

    // r0 = thisptr. r1 = retBuffer. r2 = instParam. r3 = unused. Stack0+Stack1+Stack2 = 'val'
    public Data ReturnNeedBufferLargeParam(Data val)
    {
        Program.s_InstStr = typeof(StructAPITest<T>).ToString() + " - " + _id;
        return val;
    }
}

public class Program
{
    public static string s_InstStr;
    
    static int AssertEqual<T>(T actual, T expected)
    {
        if (!actual.Equals(expected))
        {
            Console.WriteLine("Failed Scenario. Actual = {0}. Expected = {1}", actual, expected);
            return 1;
        }
        return 0;
    }
        
    [Fact]
    public static int TestEntryPoint()
    {
        int numFailures = 0;
        var foo = new StructAPITest<string>(); foo._id = "ABC";
        Data d; d._a = 123; d._b = true; d._c = 456;
        
        var ReturnsIntNoParams = typeof(StructAPITest<string>).GetMethod("ReturnsIntNoParams");
        var ReturnNeedBufferNoParams = typeof(StructAPITest<string>).GetMethod("ReturnNeedBufferNoParams");
        var ReturnsIntSmallParam = typeof(StructAPITest<string>).GetMethod("ReturnsIntSmallParam");
        var ReturnsIntDoubleRegisterParam = typeof(StructAPITest<string>).GetMethod("ReturnsIntDoubleRegisterParam");
        var ReturnNeedBufferSmallParam = typeof(StructAPITest<string>).GetMethod("ReturnNeedBufferSmallParam");
        var ReturnNeedBufferLargeParam = typeof(StructAPITest<string>).GetMethod("ReturnNeedBufferLargeParam");
        
        s_InstStr = "";
        numFailures += AssertEqual(ReturnsIntNoParams.Invoke(foo, new object[] { }), 123);
        numFailures += AssertEqual(s_InstStr, "StructAPITest`1[System.String] - ABC");
        
        s_InstStr = "";
        numFailures += AssertEqual(ReturnNeedBufferNoParams.Invoke(foo, new object[] { }), d);
        numFailures += AssertEqual(s_InstStr, "StructAPITest`1[System.String] - ABC");
        
        s_InstStr = "";
        numFailures += AssertEqual(ReturnsIntSmallParam.Invoke(foo, new object[] { (int)3434 }), 3434);
        numFailures += AssertEqual(s_InstStr, "StructAPITest`1[System.String] - ABC");

        s_InstStr = "";
        numFailures += AssertEqual(ReturnsIntDoubleRegisterParam.Invoke(foo, new object[] { (long)5656 }), 5656);
        numFailures += AssertEqual(s_InstStr, "StructAPITest`1[System.String] - ABC");

        s_InstStr = "";
        d._a = 789;
        d._c = 999;
        numFailures += AssertEqual(ReturnNeedBufferSmallParam.Invoke(foo, new object[] { (int)789 }), d);
        numFailures += AssertEqual(s_InstStr, "StructAPITest`1[System.String] - ABC");

        s_InstStr = "";
        numFailures += AssertEqual(ReturnNeedBufferLargeParam.Invoke(foo, new object[] { d }), d);
        numFailures += AssertEqual(s_InstStr, "StructAPITest`1[System.String] - ABC");
        
        return numFailures == 0 ? 100 : -1;
    }
}
