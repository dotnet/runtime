// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class StaticInitOrder
{
    private static int s_ReturnCode = 100;

    public static int Main(string[] args)
    {
        AssertEquals(0, TestStatcCtor(0));
        AssertEquals(420, TestStatcCtor(10));

        CallCounter.Calls = 0;
        AssertEquals(0, TestBeforeFieldInit(0));
        AssertEquals(1420, TestBeforeFieldInit(10));

        CallCounter.Calls = 0;
        AssertEquals(0, TestStatcCtor_Generic_vt(0));
        AssertEquals(50, TestStatcCtor_Generic_ref(10));

        CallCounter.Calls = 0;
        AssertEquals(0, TestBeforeFieldInit_Generic_vt(0));
        AssertEquals(50, TestBeforeFieldInit_Generic_ref(10));

        return s_ReturnCode;
    }

    private static int TestStatcCtor(int iterations)
    {
        int value = 0;
        AssertEquals(0, CallCounter.Calls);
        for (int i = 0; i < iterations; i++)
        {
            if (i == 0)
            {
                AssertEquals(0, CallCounter.Calls);
            }
            value += StaticCctor.Value;
            AssertEquals(1, CallCounter.Calls);
        }
        AssertEquals(iterations == 0 ? 0 : 1, CallCounter.Calls);
        return value;
    }

    private static int TestStatcCtor_Generic_vt(int iterations)
    {
        int value = 0;
        AssertEquals(0, CallCounter.Calls);
        for (int i = 0; i < iterations; i++)
        {
            if (i == 0)
            {
                AssertEquals(0, CallCounter.Calls);
            }
            value += StaticCctorGeneric<int>.Value;
            AssertEquals(1, CallCounter.Calls);
        }
        AssertEquals(iterations == 0 ? 0 : 1, CallCounter.Calls);
        return value;
    }

    private static int TestStatcCtor_Generic_ref(int iterations)
    {
        int value = 0;
        AssertEquals(0, CallCounter.Calls);
        for (int i = 0; i < iterations; i++)
        {
            if (i == 0)
            {
                AssertEquals(0, CallCounter.Calls);
            }
            value += StaticCctorGeneric<string>.Value.Length;
            AssertEquals(1, CallCounter.Calls);
        }
        AssertEquals(iterations == 0 ? 0 : 1, CallCounter.Calls);
        return value;
    }

    private static int TestBeforeFieldInit(int iterations)
    {
        int value = 0;
        AssertEquals(0, CallCounter.Calls);
        for (int i = 0; i < iterations; i++)
        {
            if (i == 0)
            {
                AssertEquals(0, CallCounter.Calls);
            }
            value += FieldInit.Value;
            AssertEquals(1, CallCounter.Calls);
        }
        AssertEquals(iterations == 0 ? 0 : 1, CallCounter.Calls);
        return value;
    }

    private static int TestBeforeFieldInit_Generic_vt(int iterations)
    {
        int value = 0;
        AssertEquals(0, CallCounter.Calls);
        for (int i = 0; i < iterations; i++)
        {
            if (i == 0)
            {
                AssertEquals(0, CallCounter.Calls);
            }
            value += FieldInitGeneric<int>.Value;
            AssertEquals(1, CallCounter.Calls);
        }
        AssertEquals(iterations == 0 ? 0 : 1, CallCounter.Calls);
        return value;
    }

    private static int TestBeforeFieldInit_Generic_ref(int iterations)
    {
        int value = 0;
        AssertEquals(0, CallCounter.Calls);
        for (int i = 0; i < iterations; i++)
        {
            if (i == 0)
            {
                AssertEquals(0, CallCounter.Calls);
            }
            value += FieldInitGeneric<string>.Value.Length;
            AssertEquals(1, CallCounter.Calls);
        }
        AssertEquals(iterations == 0 ? 0 : 1, CallCounter.Calls);
        return value;
    }

    private static void AssertEquals(int expected, int actual, [CallerLineNumber] int line = 0)
    {
        if (expected != actual)
        {
            Console.WriteLine($"{expected} != {actual}, L{line}");
            s_ReturnCode++;
        }
    }
}

public static class CallCounter
{
    public static int Calls { get; set; }
}

public static class StaticCctor
{
    public static readonly int Value;

    static StaticCctor()
    {
        CallCounter.Calls++;
        Value = 42;
    }
}

public static class StaticCctorGeneric<T>
{
    public static readonly T Value;

    static StaticCctorGeneric()
    {
        CallCounter.Calls++;
        if (typeof(T) == typeof(string))
            Value = (T) (object)"hello";
        else if (typeof(T) == typeof(int))
            Value = (T)(object)242;
        else
            Value = default(T);
    }
}

public static class FieldInit
{
    public static int Value { get; } = Initialize();

    private static int Initialize()
    {
        CallCounter.Calls++;
        return 142;
    }
}

public static class FieldInitGeneric<T>
{
    public static T Value { get; } = Initialize();

    private static T Initialize()
    {
        CallCounter.Calls++;
        if (typeof(T) == typeof(string))
            return (T)(object)"hello";
        else if (typeof(T) == typeof(int))
            return (T)(object)242;
        else
            return default(T);
    }
}
