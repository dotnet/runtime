// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test determines if a range check can pass through an operation
// Suppose we have:
//     x = rangecheck(i, bound) where i is the iterator variable
//     y = x * 4
//       = y
//     i = i + 1
// We want to strength-reduce this as
//     y' = rangecheck(y, bound*4)
//        = y'
//     y  = y + 4
// This is only legal under certain conditions
//     - multiply by a positive constant
//     - add/subtract when the initial IV is known to be >= 0
//     - add a value smaller than pointer size (to avoid a value off the end of an array) <-- This last check was missing and hence the bug!!!!!
// 
// Problem: The JIT seems to be rewriting the IV and the loop test in terms of the struct member, which is not safe because the member is more than pointer size past the end of the array. 
// 
// Fix: Stop OSR from rewriting IV's when the ADD is greater than pointer size. Need to further beat down OSR to have it stop rewriting IVs when they would be offset more than pointer-sized past the end of an array or object. 
// 
// PLEASE NOTE: You have to set complus_GCSTRESS=4 to see the AV.

using System;
using Xunit;

namespace Test_osrAddovershot_cs
{
internal struct MyStruct
{
    public int a, b, c, d, e, f, g, h;
}

public static class Repro
{
    private static int SumMSH(MyStruct[] ms)
    {
        int sum = 0;
        for (int i = 0; i < ms.Length; i++)
        {
            sum += ms[i].h; //Gives an AV
                            //sum += ms[i].b; //will not give an AV since offset is less than 8 bytes. 
        }
        return sum;
    }

    private static MyStruct[] InitMS(int length)
    {
        MyStruct[] ms = new MyStruct[length];
        for (int i = 0, j = 0; i < ms.Length; i++)
        {
            ms[i].a = j++;
            ms[i].b = j++;
            ms[i].c = j++;
            ms[i].d = j++;
            ms[i].e = j++;
            ms[i].f = j++;
            ms[i].g = j++;
            ms[i].h = j++;
        }
        return ms;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        MyStruct[] ms = InitMS(5); //InitMS(args.Length > 0 ? int.Parse(args[0]) : 5);
                                   //Do not expect to take in any arguments here for simplicity sake.
                                   //This does not impact functionality of the repro.
        if (SumMSH(ms) == 115) return 100; else return 101;
    }
}
}
