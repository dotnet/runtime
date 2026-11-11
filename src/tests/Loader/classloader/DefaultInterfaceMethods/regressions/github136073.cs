// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Github136073
{
    interface ITarget
    {
        Type Identity(Type type);
    }

    interface IDefault<T> : IFormattable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        string IFormattable.ToString(string format, IFormatProvider provider)
            => ((ITarget)this).Identity(typeof(T)).Name;
    }

    class Receiver<T> : IDefault<T>, ITarget
    {
        public Type Identity(Type type) => type;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static IFormattable Create<T>() => new Receiver<T>();

    [Fact]
    public static void TestEntryPoint()
    {
        // The shared default implementation must cast its real receiver even when
        // inlined into the instantiating thunk.
        Assert.Equal("Object", Create<object>().ToString(null, null));
        Assert.Equal("String", Create<string>().ToString(null, null));
    }
}
