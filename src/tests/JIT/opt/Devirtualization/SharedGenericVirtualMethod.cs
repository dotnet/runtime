// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class SharedGenericVirtualMethod
{
    private interface IReader
    {
        int Constant<T>();
        Type Read<T>();
    }

    private abstract class Base
    {
        public abstract int Constant<T>();
        public abstract Type Read<T>();
    }

    private sealed class Derived : Base, IReader
    {
        public override int Constant<T>() => 42;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override Type Read<T>() => typeof(T);
    }

    private sealed class Other : Base
    {
        public override int Constant<T>() => 7;
        public override Type Read<T>() => typeof(T[]);
    }

    private interface IOwner<T>
    {
        Type Read<U>();
    }

    private class Parent<T> : IOwner<T[]>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        Type IOwner<T[]>.Read<U>() => typeof((T, U));
    }

    private sealed class Child : Parent<int> { }
    private sealed class SharedChild<T> : Parent<T> { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ByClass<T>(Derived d) => ((Base)d).Constant<T>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ByInterface<T>(Derived d) => ((IReader)d).Constant<T>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Kept<T>(Derived d) => ((Base)d).Read<T>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type Unknown<T>(Base d) => d.Read<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Nested<A, B>(Child d) => ((IOwner<int[]>)d).Read<(B, A)>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type FromInlinee<T>(Child d) => Nested<T, string>(d);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type SharedOwner<T, U>(SharedChild<T> d) => ((IOwner<T[]>)d).Read<U>();

    [Fact]
    public static void TestEntryPoint()
    {
        var d = new Derived();
        Assert.Equal(42, ByClass<string>(d));
        Assert.Equal(42, ByInterface<object>(d));
        Assert.Throws<NullReferenceException>(() => ByClass<string>(null));
        Assert.Throws<NullReferenceException>(() => ByInterface<object>(null));
        Assert.Throws<NullReferenceException>(() => Kept<string>(null));
        Assert.Equal(typeof(string), Kept<string>(d));
        Assert.Equal(typeof(object), Kept<object>(d));
        Assert.Equal(typeof(string), Unknown<string>(d));
        Assert.Equal(typeof(string[]), Unknown<string>(new Other()));
        Assert.Equal(typeof((int, (string, object))), FromInlinee<object>(new Child()));
        Assert.Equal(typeof((string, object)), SharedOwner<string, object>(new SharedChild<string>()));
        Assert.Equal(typeof((object, string)), SharedOwner<object, string>(new SharedChild<object>()));
    }
}
