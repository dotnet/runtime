// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// This regression test tracks the issue where implementation of a static virtual method
// on a derived type is not found when there is a re-abstraction of the same method
// higher in inheritance hierarchy.

public class Test1 : I2
{

    [Fact]
    public static int TestEntryPoint()
    {
        string result = Test<Test1>();
        const string expectedResult = "Test1.M1";
        Console.WriteLine("Expected {0}, found {1}: {2}", expectedResult, result, expectedResult == result ? "match" : "mismatch");
        return result == expectedResult ? 100 : 101;
    }

    static string Test<i1>() where i1 : I1
    {
        return i1.M1();
    }

    static string I1.M1()
    {
        return "Test1.M1";
    }
}

public interface I1
{
    static abstract string M1();
}

public interface I2 : I1
{
    static abstract string I1.M1();
}
