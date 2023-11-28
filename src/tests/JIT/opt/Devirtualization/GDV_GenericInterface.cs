// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Program
{
    private static int _retCode = 100;
    private static IFoo<Program> _foo;
    private static IFooCov<object> _fooCov;

    [Fact]
    public static int TestEntryPoint()
    {
        _foo = new Foo1<Program>();
        _fooCov = new Foo1Cov<Program>();

        for (int i = 0; i < 100; i++)
        {
            AssertEquals(Test(), 42);
            AssertEquals(TestCov(), 42);
            AssertEquals(TestShared<Program>(), 42);
            Thread.Sleep(16);
        }

        _foo = new Foo2<Program>();
        AssertEquals(Test(), 43);

        _fooCov = new Foo1Cov<object>();
        AssertEquals(TestCov(), 43);
        _fooCov = new Foo2Cov<Program>();
        AssertEquals(TestCov(), 142);

        AssertEquals(TestShared<object>(), 43);

        return _retCode;
    }

    private static void AssertEquals<T>(T t1, T t2)
    {
        if (!t1.Equals(t2))
        {
            Console.WriteLine($"{t1} != {t2}");
            _retCode++;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test() => GetIFoo().GetValue();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IFoo<Program> GetIFoo() => _foo;


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int TestCov() => GetIFooCov().GetValue();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IFooCov<object> GetIFooCov() => _fooCov;


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int TestShared<T>() => GetIFooM<T>().GetValue();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IFooCov<T> GetIFooM<T>() => new Foo1Cov<T>();
}

public interface IFoo<T>
{
    int GetValue();
}

public class Foo1<T> : IFoo<T>
{
    public int GetValue() => 42;
}

public class Foo2<T> : IFoo<T>
{
    public int GetValue() => 43;
}


public interface IFooCov<out T>
{
    int GetValue();
}

public class Foo1Cov<T> : IFooCov<T>
{
    public int GetValue()
    {
        if (typeof(T) == typeof(Program))
            return 42;
        return 43;
    }
}

public class Foo2Cov<T> : IFooCov<T>
{
    public int GetValue()
    {
        if (typeof(T) == typeof(Program))
            return 142;
        return 143;
    }
}
