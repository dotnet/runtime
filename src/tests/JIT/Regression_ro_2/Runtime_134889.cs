// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134889
{
    public sealed class G<T> { }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<ArrayTypeMismatchException>(() => Store<string, object>(new G<string>[1], new G<object>()));
        Assert.Throws<ArrayTypeMismatchException>(() => StoreNested<string, object>(new G<string>[1][], new G<object>[1]));
        Assert.Throws<ArrayTypeMismatchException>(() => StoreNewArr<string, object>(new G<object>()));

        G<string>[] arr = new G<string>[1];
        G<string> val = new G<string>();
        Store<string, string>(arr, val);
        Assert.Same(val, arr[0]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Store<T, U>(G<T>[] arr, G<U> val)
    {
        object[] o = arr;
        o[0] = val;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreNested<T, U>(G<T>[][] arr, G<U>[] val)
    {
        object[] o = arr;
        o[0] = val;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object[] StoreNewArr<T, U>(G<U> val)
    {
        object[] o = new G<T>[1];
        o[0] = val;
        return o;
    }
}
