// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_131635;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public interface IProvider
{
    object Get(Type t);
}

public sealed class NullProvider : IProvider
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public object Get(Type t) => null;
}

public class Runtime_131635
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T GetFrom<T>(IProvider p) => (T)p.Get(typeof(T));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T CastNull<T>(object o) => (T)o;

    [Fact]
    public static void TestEntryPoint()
    {
        IProvider p = new NullProvider();
        Assert.Null(GetFrom<IComparable>(p));
        Assert.Null(GetFrom<string>(p));
        Assert.Null(CastNull<IComparable>(null));
    }
}
