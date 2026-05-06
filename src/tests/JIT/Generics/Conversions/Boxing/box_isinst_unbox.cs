// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public static class Tests
{
    private static int returnCode = 100;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstUnbox1<T>(T t) => t is int n ? n : -1;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstUnbox2<T>(T t) => t is string n ? n.Length : -1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstUnbox3<T>(T t) => t is Struct1<int> n ? n.a : -1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstUnbox4<T>(T t) => t is Struct1<IDisposable> n ? n.GetHashCode() : -1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstUnbox5<T>(T t) => t is Class1<int> n ? n.a : -1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstUnbox6<T>(T t) => t is RefBase n ? n.a : -1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int BoxIsInstUnbox7<T>(T t) => t is object[] n ? n.Length : -1;

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
        BoxIsInstUnbox1<int>(1).Expect(1);
        BoxIsInstUnbox1<uint>(1).Expect(-1);
        BoxIsInstUnbox1<byte>(1).Expect(-1);
        BoxIsInstUnbox1<long>(1).Expect(-1);
        BoxIsInstUnbox1<decimal>(1).Expect(-1);
        BoxIsInstUnbox1<int?>(1).Expect(1);
        BoxIsInstUnbox1<int?>(null).Expect(-1);
        BoxIsInstUnbox1<uint?>(1).Expect(-1);
        BoxIsInstUnbox1<string>("1").Expect(-1);
        BoxIsInstUnbox1<string>(1.ToString()).Expect(-1);
        BoxIsInstUnbox1<string>(null).Expect(-1);
        BoxIsInstUnbox1<Struct1<int>>(new Struct1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox1<Struct1<int>?>(new Struct1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox1<Struct1<uint>>(new Struct1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox1<Struct2<IDisposable>>(new Struct2<IDisposable>()).Expect(-1);
        BoxIsInstUnbox1<Class1<int>>(new Class1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox1<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox1<Class1<string>>(new Class1<string> { a = 1 }).Expect(-1);
        BoxIsInstUnbox1<Class1<int>>(new Class1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox1<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox1<Class1<string>>(new Class1<string> { a = 1 }).Expect(-1);
        BoxIsInstUnbox1<Class1<IDisposable>>(new Class1<IDisposable> { a = 1 }).Expect(-1);
        BoxIsInstUnbox1<string[]>(new string[1]).Expect(-1);
        BoxIsInstUnbox1<object[]>(new string[1]).Expect(-1);
        BoxIsInstUnbox1<IEnumerable<object>>(new string[1]).Expect(-1);

        BoxIsInstUnbox2<int>(1).Expect(-1);
        BoxIsInstUnbox2<uint>(1).Expect(-1);
        BoxIsInstUnbox2<byte>(1).Expect(-1);
        BoxIsInstUnbox2<long>(1).Expect(-1);
        BoxIsInstUnbox2<decimal>(1).Expect(-1);
        BoxIsInstUnbox2<int?>(1).Expect(-1);
        BoxIsInstUnbox2<int?>(null).Expect(-1);
        BoxIsInstUnbox2<uint?>(1).Expect(-1);
        BoxIsInstUnbox2<string>("1").Expect(1);
        BoxIsInstUnbox2<string>(1.ToString()).Expect(1);
        BoxIsInstUnbox2<string>(null).Expect(-1);
        BoxIsInstUnbox2<Struct1<int>>(new Struct1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox2<Struct1<int>?>(new Struct1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox2<Struct1<uint>>(new Struct1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox2<Struct2<IDisposable>>(new Struct2<IDisposable>()).Expect(-1);
        BoxIsInstUnbox2<Class1<int>>(new Class1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox2<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox2<Class1<string>>(new Class1<string> { a = 1 }).Expect(-1);
        BoxIsInstUnbox2<Class1<int>>(new Class1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox2<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox2<Class1<string>>(new Class1<string> { a = 1 }).Expect(-1);
        BoxIsInstUnbox2<Class1<IDisposable>>(new Class1<IDisposable> { a = 1 }).Expect(-1);
        BoxIsInstUnbox2<string[]>(new string[1]).Expect(-1);
        BoxIsInstUnbox2<object[]>(new string[1]).Expect(-1);
        BoxIsInstUnbox2<IEnumerable<object>>(new string[1]).Expect(-1);

        BoxIsInstUnbox3<int>(1).Expect(-1);
        BoxIsInstUnbox3<uint>(1).Expect(-1);
        BoxIsInstUnbox3<byte>(1).Expect(-1);
        BoxIsInstUnbox3<long>(1).Expect(-1);
        BoxIsInstUnbox3<decimal>(1).Expect(-1);
        BoxIsInstUnbox3<int?>(1).Expect(-1);
        BoxIsInstUnbox3<int?>(null).Expect(-1);
        BoxIsInstUnbox3<uint?>(1).Expect(-1);
        BoxIsInstUnbox3<string>("1").Expect(-1);
        BoxIsInstUnbox3<string>(1.ToString()).Expect(-1);
        BoxIsInstUnbox3<string>(null).Expect(-1);
        BoxIsInstUnbox3<Struct1<int>>(new Struct1<int> { a = 1 }).Expect(1);
        BoxIsInstUnbox3<Struct1<int>?>(new Struct1<int> { a = 1 }).Expect(1);
        BoxIsInstUnbox3<Struct1<uint>>(new Struct1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox3<Struct2<IDisposable>>(new Struct2<IDisposable>()).Expect(-1);
        BoxIsInstUnbox3<Class1<int>>(new Class1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox3<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox3<Class1<string>>(new Class1<string> { a = 1 }).Expect(-1);
        BoxIsInstUnbox3<Class1<int>>(new Class1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox3<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox3<Class1<string>>(new Class1<string> { a = 1 }).Expect(-1);
        BoxIsInstUnbox3<Class1<IDisposable>>(new Class1<IDisposable> { a = 1 }).Expect(-1);
        BoxIsInstUnbox3<string[]>(new string[1]).Expect(-1);
        BoxIsInstUnbox3<object[]>(new string[1]).Expect(-1);
        BoxIsInstUnbox3<IEnumerable<object>>(new string[1]).Expect(-1);

        BoxIsInstUnbox4<int>(1).Expect(-1);
        BoxIsInstUnbox4<uint>(1).Expect(-1);
        BoxIsInstUnbox4<byte>(1).Expect(-1);
        BoxIsInstUnbox4<long>(1).Expect(-1);
        BoxIsInstUnbox4<decimal>(1).Expect(-1);
        BoxIsInstUnbox4<int?>(1).Expect(-1);
        BoxIsInstUnbox4<int?>(null).Expect(-1);
        BoxIsInstUnbox4<uint?>(1).Expect(-1);
        BoxIsInstUnbox4<string>("1").Expect(-1);
        BoxIsInstUnbox4<string>(1.ToString()).Expect(-1);
        BoxIsInstUnbox4<string>(null).Expect(-1);
        BoxIsInstUnbox4<Struct1<int>>(new Struct1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox4<Struct1<int>?>(new Struct1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox4<Struct1<uint>>(new Struct1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox4<Struct2<IDisposable>>(new Struct2<IDisposable>()).Expect(-1);
        BoxIsInstUnbox4<Class1<int>>(new Class1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox4<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox4<Class1<string>>(new Class1<string> { a = 1 }).Expect(-1);
        BoxIsInstUnbox4<Class1<int>>(new Class1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox4<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox4<Class1<string>>(new Class1<string> { a = 1 }).Expect(-1);
        BoxIsInstUnbox4<Class1<IDisposable>>(new Class1<IDisposable> { a = 1 }).Expect(-1);
        BoxIsInstUnbox4<string[]>(new string[1]).Expect(-1);
        BoxIsInstUnbox4<object[]>(new string[1]).Expect(-1);
        BoxIsInstUnbox4<IEnumerable<object>>(new string[1]).Expect(-1);

        BoxIsInstUnbox5<int>(1).Expect(-1);
        BoxIsInstUnbox5<uint>(1).Expect(-1);
        BoxIsInstUnbox5<byte>(1).Expect(-1);
        BoxIsInstUnbox5<long>(1).Expect(-1);
        BoxIsInstUnbox5<decimal>(1).Expect(-1);
        BoxIsInstUnbox5<int?>(1).Expect(-1);
        BoxIsInstUnbox5<int?>(null).Expect(-1);
        BoxIsInstUnbox5<uint?>(1).Expect(-1);
        BoxIsInstUnbox5<string>("1").Expect(-1);
        BoxIsInstUnbox5<string>(1.ToString()).Expect(-1);
        BoxIsInstUnbox5<string>(null).Expect(-1);
        BoxIsInstUnbox5<Struct1<int>>(new Struct1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox5<Struct1<int>?>(new Struct1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox5<Struct1<uint>>(new Struct1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox5<Struct2<IDisposable>>(new Struct2<IDisposable>()).Expect(-1);
        BoxIsInstUnbox5<Class1<int>>(new Class1<int> { a = 1 }).Expect(1);
        BoxIsInstUnbox5<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox5<Class1<string>>(new Class1<string> { a = 1 }).Expect(-1);
        BoxIsInstUnbox5<Class1<int>>(new Class1<int> { a = 1 }).Expect(1);
        BoxIsInstUnbox5<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox5<Class1<string>>(new Class1<string> { a = 1 }).Expect(-1);
        BoxIsInstUnbox5<Class1<IDisposable>>(new Class1<IDisposable> { a = 1 }).Expect(-1);
        BoxIsInstUnbox5<string[]>(new string[1]).Expect(-1);
        BoxIsInstUnbox5<object[]>(new string[1]).Expect(-1);
        BoxIsInstUnbox5<IEnumerable<object>>(new string[1]).Expect(-1);

        BoxIsInstUnbox6<int>(1).Expect(-1);
        BoxIsInstUnbox6<uint>(1).Expect(-1);
        BoxIsInstUnbox6<byte>(1).Expect(-1);
        BoxIsInstUnbox6<long>(1).Expect(-1);
        BoxIsInstUnbox6<decimal>(1).Expect(-1);
        BoxIsInstUnbox6<int?>(1).Expect(-1);
        BoxIsInstUnbox6<int?>(null).Expect(-1);
        BoxIsInstUnbox6<uint?>(1).Expect(-1);
        BoxIsInstUnbox6<string>("1").Expect(-1);
        BoxIsInstUnbox6<string>(1.ToString()).Expect(-1);
        BoxIsInstUnbox6<string>(null).Expect(-1);
        BoxIsInstUnbox6<Struct1<int>>(new Struct1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox6<Struct1<int>?>(new Struct1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox6<Struct1<uint>>(new Struct1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox6<Struct2<IDisposable>>(new Struct2<IDisposable>()).Expect(-1);
        BoxIsInstUnbox6<Class1<int>>(new Class1<int> { a = 1 }).Expect(1);
        BoxIsInstUnbox6<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(1);
        BoxIsInstUnbox6<Class1<string>>(new Class1<string> { a = 1 }).Expect(1);
        BoxIsInstUnbox6<Class1<int>>(new Class1<int> { a = 1 }).Expect(1);
        BoxIsInstUnbox6<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(1);
        BoxIsInstUnbox6<Class1<string>>(new Class1<string> { a = 1 }).Expect(1);
        BoxIsInstUnbox6<Class1<IDisposable>>(new Class1<IDisposable> { a = 1 }).Expect(1);
        BoxIsInstUnbox6<string[]>(new string[1]).Expect(-1);
        BoxIsInstUnbox6<object[]>(new string[1]).Expect(-1);
        BoxIsInstUnbox6<IEnumerable<object>>(new string[1]).Expect(-1);

        BoxIsInstUnbox7<int>(1).Expect(-1);
        BoxIsInstUnbox7<uint>(1).Expect(-1);
        BoxIsInstUnbox7<byte>(1).Expect(-1);
        BoxIsInstUnbox7<long>(1).Expect(-1);
        BoxIsInstUnbox7<decimal>(1).Expect(-1);
        BoxIsInstUnbox7<int?>(1).Expect(-1);
        BoxIsInstUnbox7<int?>(null).Expect(-1);
        BoxIsInstUnbox7<uint?>(1).Expect(-1);
        BoxIsInstUnbox7<string>("1").Expect(-1);
        BoxIsInstUnbox7<string>(1.ToString()).Expect(-1);
        BoxIsInstUnbox7<string>(null).Expect(-1);
        BoxIsInstUnbox7<Struct1<int>>(new Struct1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox7<Struct1<int>?>(new Struct1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox7<Struct1<uint>>(new Struct1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox7<Struct2<IDisposable>>(new Struct2<IDisposable>()).Expect(-1);
        BoxIsInstUnbox7<Class1<int>>(new Class1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox7<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox7<Class1<string>>(new Class1<string> { a = 1 }).Expect(-1);
        BoxIsInstUnbox7<Class1<int>>(new Class1<int> { a = 1 }).Expect(-1);
        BoxIsInstUnbox7<Class1<uint>>(new Class1<uint> { a = 1 }).Expect(-1);
        BoxIsInstUnbox7<Class1<string>>(new Class1<string> { a = 1 }).Expect(-1);
        BoxIsInstUnbox7<Class1<IDisposable>>(new Class1<IDisposable> { a = 1 }).Expect(-1);
        BoxIsInstUnbox7<string[]>(new string[1]).Expect(1);
        BoxIsInstUnbox7<object[]>(new string[1]).Expect(1);
        BoxIsInstUnbox7<IEnumerable<object>>(new string[1]).Expect(1);

        return returnCode;
    }
}

public struct Struct1<T>
{
    public T a;
}

public struct Struct2<T>
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
