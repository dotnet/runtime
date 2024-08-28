// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

using Xunit;

[ConditionalClass(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMonoInterpreter))]
public class Arrays
{
    private class TestClass { }

    [Fact]
    public static void TypeMismatch_ArrayElement()
    {
        Console.WriteLine($"Running {nameof(TypeMismatch_ArrayElement)}...");
        TestClass[][] arr = new TestClass[1][];
        MethodInfo arraySet = typeof(object[][]).GetMethod("Set");
        Exception e = Assert.Throws<TargetInvocationException>(() => arraySet.Invoke(arr, [0, new object[] { new object() }]));
        Assert.IsType<ArrayTypeMismatchException>(e.InnerException);
    }

    [Fact]
    public static void TypeMismatch_MultidimensionalArrayElement()
    {
        Console.WriteLine($"Running {nameof(TypeMismatch_MultidimensionalArrayElement)}...");
        TestClass[][,] arr = new TestClass[1][,];
        MethodInfo arraySet = typeof(object[][,]).GetMethod("Set");
        Exception e = Assert.Throws<TargetInvocationException>(() => arraySet.Invoke(arr, [0, new object[1,1] { {new object()} }]));
        Assert.IsType<ArrayTypeMismatchException>(e.InnerException);
    }

    [Fact]
    public static void TypeMismatch_ClassElement()
    {
        Console.WriteLine($"Running {nameof(TypeMismatch_ClassElement)}...");
        {
            TestClass[] arr = new TestClass[1];
            MethodInfo arraySet = typeof(object[]).GetMethod("Set");
            Exception e = Assert.Throws<TargetInvocationException>(() => arraySet.Invoke(arr, [0, new object()]));
            Assert.IsType<ArrayTypeMismatchException>(e.InnerException);
        }
        {
            TestClass[,] arr = new TestClass[1,1];
            MethodInfo arraySet = typeof(object[,]).GetMethod("Set");
            Exception e = Assert.Throws<TargetInvocationException>(() => arraySet.Invoke(arr, [0, 0, new object()]));
            Assert.IsType<ArrayTypeMismatchException>(e.InnerException);
        }
    }
}