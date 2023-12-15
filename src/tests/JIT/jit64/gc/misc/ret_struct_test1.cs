// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

struct MyValueClass
{
    public string S1;
    public int i2;
    public string S3;
    public string S4;
};

public class T
{
    [Fact]
    public static int TestEntryPoint()
    {
        MyValueClass mvc = foo();

        Console.WriteLine(mvc.S1);
        Console.WriteLine(mvc.i2);
        Console.WriteLine(mvc.S3);
        Console.WriteLine(mvc.S4);
        if (mvc.S1 != "Hello") return -1;
        if (mvc.S3 != "this") return -1;
        if (mvc.S4 != "works!") return -1;
        if (mvc.i2 != 7) return -1;
        return 100;
    }

    private static MyValueClass foo()
    {
        MyValueClass mvcRetVal = new MyValueClass();

        mvcRetVal.S1 = "Hello";
        mvcRetVal.S3 = "this";
        mvcRetVal.S4 = "works!";
        mvcRetVal.i2 = 7;

        return mvcRetVal;
    }
}
