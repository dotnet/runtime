// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class Tests
{
    private static int returnCode = 100;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstBr1<T>(T t) => t is int ? 1 : 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstBr2<T>(T t) => t is string ? 1 : 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstBr3<T>(T t) => t is Struct1<int> ? 1 : 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstBr4<T>(T t) => t is Struct1<IDisposable> ? 1 : 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstBr5<T>(T t) => t is Class1<int> ? 1 : 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstBr6<T>(T t) => t is RefBase ? 1 : 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstBr7<T>(T t) => t is object[] ? 1 : 0;

    public static void Expect(this int actual, int expected, [CallerLineNumber] int line = 0)
    {
        if (expected != actual)
        {
            Console.WriteLine($"{actual} != {expected}, line {line}.");
            returnCode++;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        BoxIsInstBr1<int?>(1).Expect(1);
        BoxIsInstBr1<int?>(null).Expect(0);
        BoxIsInstBr1<uint?>(1).Expect(0);
        BoxIsInstBr1<string>(null).Expect(0);
        BoxIsInstBr1<Struct1<int>?>(new Struct1<int> { a = 1 }).Expect(0);
        BoxIsInstBr1<Struct1<DateTime>?>(new Struct1<DateTime> { a = DateTime.Now }).Expect(0);

        BoxIsInstBr2<int?>(1).Expect(0);
        BoxIsInstBr2<int?>(null).Expect(0);
        BoxIsInstBr2<uint?>(1).Expect(0);
        BoxIsInstBr2<Struct1<int>?>(new Struct1<int> { a = 1 }).Expect(0);
        BoxIsInstBr1<Struct1<DateTime>?>(new Struct1<DateTime> { a = DateTime.Now }).Expect(0);

        BoxIsInstBr3<int?>(1).Expect(0);
        BoxIsInstBr3<int?>(null).Expect(0);
        BoxIsInstBr3<uint?>(1).Expect(0);
        BoxIsInstBr3<Struct1<int>?>(new Struct1<int> { a = 1 }).Expect(1);
        BoxIsInstBr1<Struct1<DateTime>?>(new Struct1<DateTime> { a = DateTime.Now }).Expect(0);

        BoxIsInstBr4<int?>(1).Expect(0);
        BoxIsInstBr4<int?>(null).Expect(0);
        BoxIsInstBr4<uint?>(1).Expect(0);

        BoxIsInstBr5<int?>(1).Expect(0);
        BoxIsInstBr5<int?>(null).Expect(0);
        BoxIsInstBr5<uint?>(1).Expect(0);
        BoxIsInstBr5<Struct1<int>?>(new Struct1<int> { a = 1 }).Expect(0);
        BoxIsInstBr1<Struct1<DateTime>?>(new Struct1<DateTime> { a = DateTime.Now }).Expect(0);

        BoxIsInstBr6<int?>(1).Expect(0);
        BoxIsInstBr6<int?>(null).Expect(0);
        BoxIsInstBr6<uint?>(1).Expect(0);
        BoxIsInstBr6<Struct1<int>?>(new Struct1<int> { a = 1 }).Expect(0);
        BoxIsInstBr1<Struct1<DateTime>?>(new Struct1<DateTime> { a = DateTime.Now }).Expect(0);

        BoxIsInstBr7<int?>(1).Expect(0);
        BoxIsInstBr7<int?>(null).Expect(0);
        BoxIsInstBr7<uint?>(1).Expect(0);
        BoxIsInstBr7<Struct1<int>?>(new Struct1<int> { a = 1 }).Expect(0);
        BoxIsInstBr1<Struct1<DateTime>?>(new Struct1<DateTime> { a = DateTime.Now }).Expect(0);

        return returnCode;
    }
}

public struct Struct1<T>
{
    public T a;
}

public class RefBase : IDisposable
{
    public int a;
    public void Dispose() { }
}

public class Class1<T> : RefBase
{
    public T b;
}
