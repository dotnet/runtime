// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this test is regression test for VSW 448208
// A program AVed if there was a readonly static in a generic type


using System;
using Xunit;

public class GenType1<T>
{
    static readonly int s_i = 0;

    public static bool foo()
    {
        return s_i == 0;
    }
    
}

public class Test_b448208
{
    [Fact]
    public static void TestEntryPoint()
    {
        GenType1<int>.foo();
    }
}
