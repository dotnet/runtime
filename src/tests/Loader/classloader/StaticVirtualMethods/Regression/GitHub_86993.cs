// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using Xunit;

// This regression test tracks the issue where explicit static interface
// method implementation compiles but program crashes with "Fatal error"
// (tested with .NET 7 and .NET 8 Preview 4). The test couldn't compile
// prior to 7.0.5 due to incomplete support for default interface implementations
// of static virtual methods.

public sealed class Program
{
    public interface IValue<TSelf, T> : IModulusOperators<TSelf, TSelf, int>
        where T : struct
        where TSelf : IValue<TSelf, T>
    {
        static int IModulusOperators<TSelf, TSelf, int>.operator %(TSelf left, TSelf right) => 0;
    }
    
    public readonly record struct Int32Value(int Value) : IValue<Int32Value, int>;

    [Fact]
    public static int TestEntryPoint()
    {
        const string ExpectedValue = "Int32Value { Value = 1 }";
        string actualValue = new Int32Value(1).ToString();
        Console.WriteLine("Actual value = '{0}', expected value = '{1}'", actualValue, ExpectedValue);
        return actualValue == ExpectedValue ? 100 : 101;
    }
}
