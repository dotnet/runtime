// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Xunit;

public class Program
{
    const int ARRAY_SIZE = 1024;

    [Fact]
    public static int TestEntryPoint()
    {
        // Run all tests 3x times to exercise both slow and fast paths work
        for (int i = 0; i < 3; i++)
            RunAllTests();

        Console.WriteLine(Assert.HasAssertFired ? "FAILED" : "PASSED");
        return Assert.HasAssertFired ? 1 : 100;
    }

    static void RunAllTests()
    {
        RunTest1();
        RunTest2();
        RunTest3();
        RunTest4();
        RunTest5();
        RunTest6();
        RunTest7();
        RunTest8();
    }

    static void RunTest1()
    {
        int [] arr = new int[ARRAY_SIZE];

        Assert.AreEqual(arr.GetType().ToString(), "System.Int32[]");
    }

    static void RunTest2()
    {
        object [] arr = new object[ARRAY_SIZE];

        Assert.AreEqual(arr.GetType().ToString(), "System.Object[]");
    }

    static void RunTest3()
    {
        int [] arr = new_array_generic<int>();

        Assert.AreEqual(arr.GetType().ToString(), "System.Int32[]");
    }

    static void RunTest4()
    {
        string [] arr = new_array_generic<string>();

        Assert.AreEqual(arr.GetType().ToString(), "System.String[]");
    }

    static void RunTest5()
    {
        object [] arr = new_array_generic<object>();

        Assert.AreEqual(arr.GetType().ToString(), "System.Object[]");
    }

    static void RunTest6()
    {
        GenericClass1<int> [] arr = new GenericClass1<int>[ARRAY_SIZE];

        Assert.AreEqual(arr.GetType().ToString(), "GenericClass1`1[System.Int32][]");
    }

    static void RunTest7()
    {
        GenericClass1<object> [] arr = new_array_generic<GenericClass1<object>>();

        Assert.AreEqual(arr.GetType().ToString(), "GenericClass1`1[System.Object][]");
    }

    static void RunTest8()
    {
        genericclass1_object_array_field = new_array_generic<GenericClass2<object>>();

        Assert.AreEqual(genericclass1_object_array_field.GetType().ToString(), "GenericClass2`1[System.Object][]");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static T[] new_array_generic<T>()
    {
        return new T[ARRAY_SIZE];
    }

    static volatile GenericClass1<object> [] genericclass1_object_array_field;
}

class GenericClass1<T>
{
}

class GenericClass2<T> : GenericClass1<T>
{
}

public static class Assert
{
    public static bool HasAssertFired;

    public static void AreEqual(Object actual, Object expected)
    {
        if (!(actual == null && expected == null) && !actual.Equals(expected))
        {
            Console.WriteLine("Not equal!");
            Console.WriteLine("actual   = " + actual.ToString());
            Console.WriteLine("expected = " + expected.ToString());
            HasAssertFired = true;
        }
    }
}
