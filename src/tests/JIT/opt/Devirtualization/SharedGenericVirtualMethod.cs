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

    private class Caller<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type FromThis(Derived d) => ((Base)d).Read<T>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type FromType(Derived d) => ((IReader)d).Read<T[]>();
    }

    private class OrderBase
    {
        public virtual int Apply<T>(int arg) => -1;
    }

    private sealed class OrderDerived : OrderBase
    {
        public override int Apply<T>(int arg) => arg;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static R Receiver<R>(R receiver, ref int order)
    {
        order = order * 10 + 1;
        return receiver;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Argument(ref int order) => order = order * 10 + 2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ExactOrder<T>(OrderDerived receiver, ref int order)
        => ((OrderBase)Receiver(receiver, ref order)).Apply<T>(Argument(ref order));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int VirtualOrder<T>(OrderBase receiver, ref int order)
        => Receiver(receiver, ref order).Apply<T>(Argument(ref order));

    [Fact]
    public static void TestEntryPoint()
    {
        var receiver = new OrderDerived();
        int order = 0;
        Assert.Equal(12, ExactOrder<string>(receiver, ref order));
        Assert.Equal(12, order);
        order = 0;
        Assert.Equal(12, VirtualOrder<string>(receiver, ref order));
        Assert.Equal(12, order);
        order = 0;
        Assert.Throws<NullReferenceException>(() => ExactOrder<string>(null, ref order));
        Assert.Equal(12, order);
        order = 0;
        Assert.Throws<NullReferenceException>(() => VirtualOrder<string>(null, ref order));
        Assert.Equal(12, order);

        var d = new Derived();
        Assert.Equal(42, ByClass<string>(d));
        Assert.Equal(42, ByInterface<object>(d));
        Assert.Throws<NullReferenceException>(() => ByClass<string>(null));
        Assert.Throws<NullReferenceException>(() => ByInterface<object>(null));
        Assert.Throws<NullReferenceException>(() => Kept<string>(null));
        Assert.Equal(typeof(object), new Caller<object>().FromThis(d));
        Assert.Equal(typeof(string), new Caller<string>().FromThis(d));
        Assert.Equal(typeof(object[]), Caller<object>.FromType(d));
        Assert.Equal(typeof(string[]), Caller<string>.FromType(d));
        Assert.Equal(typeof(string), Kept<string>(d));
        Assert.Equal(typeof(object), Kept<object>(d));
        Assert.Equal(typeof(string), Unknown<string>(d));
        Assert.Equal(typeof(string[]), Unknown<string>(new Other()));
        Assert.Equal(typeof((int, (string, object))), FromInlinee<object>(new Child()));
        Assert.Equal(typeof((string, object)), SharedOwner<string, object>(new SharedChild<string>()));
        Assert.Equal(typeof((object, string)), SharedOwner<object, string>(new SharedChild<object>()));
    }
}
