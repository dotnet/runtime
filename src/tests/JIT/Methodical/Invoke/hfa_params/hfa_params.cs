// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_hfa_params
{
public struct doublesStruct
{
    public double f1;
    public double f2;
    public double f3;
    public double f4;
}

public class A 
{
    static bool foo(doublesStruct d1, doublesStruct d2, doublesStruct d3) 
    {
        bool success = (d1.f1 == 1   &&
                        d1.f2 == 2   &&
                        d1.f3 == 3   &&
                        d1.f4 == 4   &&

                        d2.f1 == 11  &&
                        d2.f2 == 22  &&
                        d2.f3 == 33  &&
                        d2.f4 == 44  &&

                        d3.f1 == 111 &&
                        d3.f2 == 222 &&
                        d3.f3 == 333 &&
                        d3.f4 == 444);

        if (!success)
        {
            Console.WriteLine(string.Format("{0}, {1}, {2}, {3}", d1.f1, d1.f2, d1.f3, d1.f4));
            Console.WriteLine(string.Format("{0}, {1}, {2}, {3}", d2.f1, d2.f2, d2.f3, d2.f4));
            Console.WriteLine(string.Format("{0}, {1}, {2}, {3}", d3.f1, d3.f2, d3.f3, d3.f4));
        }

        return success;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // Test that a function with HFA args gets the expected contents of the structs.

        var ds = new doublesStruct();
        ds.f1 = 1;
        ds.f2 = 2;
        ds.f3 = 3;
        ds.f4 = 4;

        var ds2 = new doublesStruct();
        ds2.f1 = 11;
        ds2.f2 = 22;
        ds2.f3 = 33;
        ds2.f4 = 44;

        var ds3 = new doublesStruct();
        ds3.f1 = 111;
        ds3.f2 = 222;
        ds3.f3 = 333;
        ds3.f4 = 444;

        return (foo(ds, ds2, ds3) ? 100 : -1);
    }
}
}
