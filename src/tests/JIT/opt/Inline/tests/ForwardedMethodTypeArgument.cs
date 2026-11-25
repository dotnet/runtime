// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ForwardedMethodTypeArgument
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Inner<X>() => typeof(X);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Outer<T, U>(bool first) where T : class where U : class
        => first ? Inner<T>() : Inner<U>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Mixed<X>(bool array) => array ? typeof(X[]) : typeof(X);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type OuterMixed<T, U>(bool array) => Mixed<U>(array);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type OuterArray<T, U>() => Inner<U[]>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Middle<A, B>() => Inner<B>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Swapped<T, U>() => Middle<U, T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Pick<A, B, C>(bool first) => first ? typeof(A) : Inner<C>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Repeat<A, B>(bool first) => Pick<B, B, A>(first);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type ThreeLevels<T, U>(bool first) => Repeat<U, T>(first);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Constructed<T, U>() => Middle<U, T[]>();

    private class Owner<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type Forward<U>(bool owner) => owner ? Inner<T>() : Inner<U>();
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(typeof(string), Outer<string, object>(true));
        Assert.Equal(typeof(object), Outer<string, object>(false));
        Assert.Equal(typeof(object), Outer<object, string>(true));
        Assert.Equal(typeof(string), Outer<object, string>(false));
        Assert.Equal(typeof(object), OuterMixed<string, object>(false));
        Assert.Equal(typeof(object[]), OuterMixed<string, object>(true));
        Assert.Equal(typeof(int), OuterMixed<string, int>(false));
        Assert.Equal(typeof(string[]), OuterArray<object, string>());
        Assert.Equal(typeof(string), Swapped<string, object>());
        Assert.Equal(typeof(object), Swapped<object, string>());
        Assert.Equal(typeof(string), ThreeLevels<string, object>(true));
        Assert.Equal(typeof(object), ThreeLevels<string, object>(false));
        Assert.Equal(typeof(int), ThreeLevels<int, string>(true));
        Assert.Equal(typeof(string), ThreeLevels<int, string>(false));
        Assert.Equal(typeof(object[]), Constructed<object, string>());
        Assert.Equal(typeof(string), Owner<string>.Forward<object>(true));
        Assert.Equal(typeof(object), Owner<string>.Forward<object>(false));
    }
}
