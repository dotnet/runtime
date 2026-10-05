// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Stack allocating a box retargets its unbox to the void UNBOX_TYPETEST helper, but the call kept
// its byref type, so wasm emitted a value-returning call_indirect and trapped. The non-Guid arg is
// load bearing: a same-type unbox skips the helper. Only reproduces under crossgen wasm R2R.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_131377
{
    [Fact]
    public static void TestEntryPoint()
    {
        Unbox(null);
        Assert.Throws<InvalidCastException>(() => Unbox("not a guid"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Unbox(object o)
    {
        if (o is null)
        {
            o = new Guid();
        }

        Consume((Guid)o);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T _)
    {
    }
}
