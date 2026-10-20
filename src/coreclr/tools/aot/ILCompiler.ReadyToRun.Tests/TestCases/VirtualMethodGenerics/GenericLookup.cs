// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyMetadata("IsTrimmable", "True")]

// Virtual method discovery for a generic type instantiation that is only ever
// reached through a GenericLookupSignature fixup.
//
// When compiling the shared TestB<__Canon, int>.TestCreate, the instance of
// TestA<T, int> is obtained through a generic dictionary lookup (a
// GenericLookupSignature fixup referencing TestA<__Canon, int>) rather than via
// a TypeFixupSignature. The virtual TestMethod on TestA<__Canon, int> must still
// be discovered and compiled.

public class TestA<T, U>
{
    public virtual void TestMethod(T item) => Console.WriteLine($"{item} / {typeof(U)}");
}

public class TestB<T, U>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public TestA<T, U> TestCreate() => new TestA<T, U>();
}

public static class GenericLookupTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestB<T, U> CreateTestB<T, U>() => new TestB<T, U>();

    public static void Run()
    {
        TestB<string, int> obj = CreateTestB<string, int>();
        obj.TestCreate().TestMethod("hello");
    }
}

interface IStaticFactory<T>
{
    static abstract int Create();
    static abstract int Unused();
}

class StaticFactory : IStaticFactory<StaticFactory>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Create() => 1;
    public static int Unused() => -1;
}

struct StaticValueFactory : IStaticFactory<object>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Create() => 2;
    public static int Unused() => -2;
}

class UndiscoveredStaticFactory : IStaticFactory<UndiscoveredStaticFactory>
{
    public static int Create() => -3;
    public static int Unused() => -4;
}

class StaticBase<T> : IStaticFactory<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Create() => 3;
    public static int Unused() => -5;
}

class StaticDerived : StaticBase<string>
{
}

class ExplicitStaticFactory : IStaticFactory<string>, IStaticFactory<object>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int IStaticFactory<string>.Create() => 4;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int IStaticFactory<object>.Create() => 5;
    static int IStaticFactory<string>.Unused() => -6;
    static int IStaticFactory<object>.Unused() => -7;
}

class StaticLookup<TFactory, T> where TFactory : IStaticFactory<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ClassLookup() => TFactory.Create();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ThisLookup() => TFactory.Create();
}

interface IStaticDefault<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static virtual int Default() => 6;
}

class StaticDefaultFactory : IStaticDefault<object>
{
}

class StaticDefaultLookup<TFactory> where TFactory : IStaticDefault<object>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Call() => TFactory.Default();
}

interface IStaticGenericFactory
{
    static abstract T Echo<T>(T value);
}

class StaticGenericFactory : IStaticGenericFactory
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Echo<T>(T value) => value;
}

class CyclicStaticGenericFactory : IStaticGenericFactory
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Echo<T>(T value)
    {
        if (value is null)
            Echo(new ValueTuple<T>(value));
        return value;
    }
}

class StaticGenericLookup<TFactory> where TFactory : IStaticGenericFactory
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object ReferenceLookup(object value) => TFactory.Echo(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int ValueLookup(int value) => TFactory.Echo(value);
}

public static class StaticLookupRoots
{
    // These types must be discovered without allocating instances or directly calling factories.
    public static Type[] Discover() =>
    [
        typeof(StaticFactory), typeof(StaticValueFactory), typeof(StaticDerived),
        typeof(ExplicitStaticFactory), typeof(StaticDefaultFactory),
        typeof(StaticDefaultLookup<StaticDefaultFactory>), typeof(StaticGenericFactory),
        typeof(StaticLookup<StaticFactory, StaticFactory>),
        typeof(StaticGenericLookup<StaticGenericFactory>), typeof(CyclicStaticGenericFactory),
    ];
}
