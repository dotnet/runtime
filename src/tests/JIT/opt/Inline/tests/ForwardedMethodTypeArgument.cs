// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Kept<X>() => typeof(X);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type MiddleKept<A, B>() => Kept<B>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type NestedKept<T, U>() => MiddleKept<U, T>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Recursive<T, U>(int depth) => depth == 0 ? typeof(U) : Recursive<T, U>(depth - 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type ArrayOfSecond<A, B>() => Inner<B[]>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type NestedArray<T, U>() => ArrayOfSecond<U, T>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Jagged<T, U>(bool wrap) => Mixed<U[]>(wrap);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Rectangular<T, U>() => Inner<U[,]>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type GenericWrapper<T, U>() => Inner<List<U[]>>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Pair<T, U>() => ArrayOfSecond<T, Dictionary<U, T>>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Compose<A, B>() => Inner<KeyValuePair<B, List<A[]>>>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Named<T, U>() => Compose<U, T>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Compound<T>() => Inner<Dictionary<string, T[]>>();

    private class PairType<T, U> { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type CrossModule<T, U>() => ForwardedArgumentBridge.Wrap<PairType<T, U>>();

    private class Owner<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type Forward<U>(bool owner) => owner ? Inner<T>() : Inner<U>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type Both<U>() => Inner<Dictionary<T, U>>();
    }

    private sealed class Box<T>
    {
        public Box() => s_constructions++;
    }

    private static int s_constructions;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object Make<T>() => new Box<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBox<U>(object value) => value is Box<U>;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Same<T>() => IsBox<T>(Make<T>());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Other<T, U>() => IsBox<U>(Make<T>());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Observe(object value) => GC.KeepAlive(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Copied<T>()
    {
        object value = Make<T>();
        Observe(value);
        return IsBox<T>(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Choose<T, U>(bool first) => IsBox<T>(first ? Make<T>() : Make<U>());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Clear(ref object value) => value = null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Cleared<T>()
    {
        object value = Make<T>();
        Clear(ref value);
        return IsBox<T>(value);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        int before = s_constructions;
        Assert.True(Same<string>());
        Assert.True(Copied<string>());
        Assert.True(Other<string, string>());
        Assert.False(Other<string, object>());
        Assert.True(Choose<string, object>(true));
        Assert.False(Choose<string, object>(false));
        Assert.False(Cleared<string>());
        Assert.Equal(before + 7, s_constructions);

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
        Assert.Equal(typeof(object), NestedKept<object, string>());
        Assert.Equal(typeof(string), NestedKept<string, object>());
        Assert.Equal(typeof(string), Recursive<object, string>(2));
        Assert.Equal(typeof(string[]), NestedArray<string, object>());
        Assert.Equal(typeof(object[]), NestedArray<object, string>());
        Assert.Equal(typeof(int[]), NestedArray<int, string>());
        Assert.Equal(typeof(string[]), Jagged<object, string>(false));
        Assert.Equal(typeof(string[][]), Jagged<object, string>(true));
        Assert.Equal(typeof(int[][]), Jagged<string, int>(true));
        Assert.Equal(typeof(string[,]), Rectangular<object, string>());
        Assert.Equal(typeof(List<string[]>), GenericWrapper<object, string>());
        Assert.Equal(typeof(Dictionary<string, object>[]), Pair<object, string>());
        Assert.Equal(typeof(KeyValuePair<string, List<object[]>>), Named<string, object>());
        Assert.Equal(typeof(KeyValuePair<object, List<string[]>>), Named<object, string>());
        Assert.Equal(typeof(Dictionary<string, int[]>), Compound<int>());
        Assert.Equal(typeof(ForwardedArgumentBox<PairType<string, object>>), CrossModule<string, object>());
        Assert.Equal(typeof(ForwardedArgumentBox<PairType<object, string>>), CrossModule<object, string>());
        Assert.Equal(typeof(Dictionary<string, object>), Owner<string>.Both<object>());
        Assert.Equal(typeof(Dictionary<object, string>), Owner<object>.Both<string>());
        Assert.Equal(typeof(string), Owner<string>.Forward<object>(true));
        Assert.Equal(typeof(object), Owner<string>.Forward<object>(false));
    }
}
