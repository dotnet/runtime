// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Poco(string value)
{
    public string Value { get; } = value;
}

public class Runtime_122879
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void StackTrasher()
    {
        Guid heavy = Guid.NewGuid();
        heavy.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    static Poco[] Creator() => CreateArray(new Poco("Passed"), 2);

    static T[] CreateArray<T>(T value, int count)
    {
        var output = new T[count];
        output.AsSpan().Fill(value);
        return output;
    }

    [Fact]
    public static void Test()
    {
        var result = Creator();
        StackTrasher(); // Removing this line makes it work as expected
        Console.WriteLine(result[0].Value);
    }
}
