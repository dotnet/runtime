// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Two shared-generic interface call sites that canonicalize to __Canon must
// not be merged by head/tail merge: each call has a distinct virtual stub
// dispatch cell, so the calls are not interchangeable.

namespace Runtime_128631;

using System.Runtime.CompilerServices;
using Xunit;

public interface IFoo<T> { object Get(); }

public class Impl : IFoo<string>, IFoo<byte[]>
{
    [MethodImpl(MethodImplOptions.NoInlining)] object IFoo<string>.Get() => "STRING_VALUE";
    [MethodImpl(MethodImplOptions.NoInlining)] object IFoo<byte[]>.Get() => new byte[] { 1, 2, 3 };

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public object Dispatch(string tag)
    {
        if (tag == "bytes")  return ((IFoo<byte[]>)this).Get();
        if (tag == "string") return ((IFoo<string>)this).Get();
        return null;
    }
}

public class Runtime_128631
{
    [Fact]
    public static int TestEntryPoint()
    {
        var d = new Impl();
        for (int i = 0; i < 200; i++)
        {
            d.Dispatch("bytes");
            d.Dispatch("string");
        }
        object r = d.Dispatch("string");
        return r is string ? 100 : -1;
    }
}
