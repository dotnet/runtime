// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ConstrainedStaticInterfaceContext
{
    public interface IOwner { static abstract Type Owner(); }
    public interface IOperation : IOwner
    {
        static abstract Type WithArgument<TArg>();
    }
    public interface IDefault<T> : IOperation
    {
        static Type IOwner.Owner() => typeof(T);
        static Type IOperation.WithArgument<TArg>() => typeof((T, TArg));
    }
    public readonly struct Direct<T> : IDefault<T> { }
    public readonly struct Nested<T> : IDefault<T[]> { }
    public readonly struct Swapped<A, B> : IDefault<B> { }
    public interface IInherited<A, B> : IDefault<B> { }
    public interface INested<T> : IInherited<T, T[]> { }
    public interface ILeft<T> : IDefault<T> { }
    public interface IRight<T> : IDefault<T> { }
    public interface IDiamond<T> : ILeft<T>, IRight<T> { }
    public class Base<T> : IOwner
    {
        public static Type Owner() => typeof(T);
    }
    public class Derived<A, B> : Base<B> { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type Call<T>() where T : IOwner => T.Owner();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type CallWithArgument<T, TArg>() where T : IOperation => T.WithArgument<TArg>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<Type> Callback<T>() where T : IOwner => T.Owner;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Func<Type> CallbackWithArgument<T, TArg>() where T : IOperation => T.WithArgument<TArg>;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type Forward<T>() where T : IOwner => T.Owner();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type ForwardWithArgument<T, TArg>() where T : IOperation => T.WithArgument<TArg>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Func<Type> ForwardCallback<T>() where T : IOwner => T.Owner;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckHierarchies<A, B>()
    {
        Assert.Equal(typeof(B), Forward<Derived<A, B>>());
        Assert.Equal(typeof(B), Forward<IInherited<A, B>>());
        Assert.Equal(typeof(A[]), Forward<INested<A>>());
        Assert.Equal(typeof(B), Forward<IDiamond<B>>());
        Assert.Equal(typeof((B, int)), ForwardWithArgument<IInherited<A, B>, int>());
        Assert.Equal(typeof((B, string)), ForwardWithArgument<IInherited<A, B>, string>());
        Assert.Equal(typeof(B), ForwardCallback<IInherited<A, B>>()());
        Assert.Equal(typeof(B), ForwardCallback<IDiamond<B>>()());
    }

    [Fact]
    public static void TestEntryPoint()
    {
        // #137846: the implementation's argument is string[], not the constrained type's string.
        Assert.Equal(typeof(string[]), Call<Nested<string>>());
        Assert.Equal(typeof(object[]), Call<Nested<object>>());
        Assert.Equal(typeof(string), Call<Direct<string>>());
        Assert.Equal(typeof(object), Call<Direct<object>>());
        Assert.Equal(typeof(string), Call<Swapped<object, string>>());
        Assert.Equal(typeof(object), Call<Swapped<string, object>>());
        Assert.Equal(typeof(string[]), Callback<Nested<string>>()());
        Assert.Equal(typeof(object[]), Callback<Nested<object>>()());
        Assert.Equal(typeof(string), Callback<Swapped<object, string>>()());
        Assert.Equal(typeof(object), Callback<Swapped<string, object>>()());
        Assert.Equal(typeof((string[], int)), CallWithArgument<Nested<string>, int>());
        Assert.Equal(typeof((string, int)), CallWithArgument<Swapped<object, string>, int>());
        Assert.Equal(typeof((object, string)), CallWithArgument<Swapped<string, object>, string>());
        Assert.Equal(typeof((string[], int)), CallbackWithArgument<Nested<string>, int>()());
        Assert.Equal(typeof((string, int)), CallbackWithArgument<Swapped<object, string>, int>()());
        Assert.Equal(typeof((object, string)), CallbackWithArgument<Swapped<string, object>, string>()());
        CheckHierarchies<object, string>();
        CheckHierarchies<string, object>();
    }
}
