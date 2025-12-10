// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class CapacityTests
{
    [Fact]
    public static void TestLargeClassWithIntMethods()
    {
        // Scenario 1: allocate an instance of a class with 40000 methods that return int
        // The allocation should succeed
        var instance = new LargeClassWithIntMethods();
        Assert.NotNull(instance);
    }

    [Fact]
    public static void TestLargeClassWithTaskMethods_Success()
    {
        // Scenario 2: allocate an instance of a class with 32750 methods that return Task
        // The allocation should succeed
        var instance = new LargeClassWithTaskMethods_Success();
        Assert.NotNull(instance);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object AllocLargeClassWithTaskMethods_Exception()
    {
        var instance = new LargeClassWithTaskMethods_Exception();
        return instance;
    }

    [Fact]
    public static void TestLargeClassWithTaskMethods_Exception()
    {
        // Scenario 3: make a call to a method that allocates an instance of a class with 32763 methods that return Task
        // The call should throw an exception
        Assert.Throws<TypeLoadException>(() =>
        {
            AllocLargeClassWithTaskMethods_Exception();
        });
    }
}
