// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Reference source for Runtime_73615.il.

using InlineIL;
using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_73615
{
    [Fact]
    public static int TestEntryPoint()
    {
        Foo(new C(101));
        return Result;
    }

    public static int Result;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Foo(C arg)
    {
        IL.Emit.Ldarga(nameof(arg));
        IL.Emit.Ldarga(nameof(arg));
        IL.Emit.Call(new MethodRef(typeof(Runtime_73615), nameof(Bar)));
        IL.Emit.Constrained<C>();
        IL.Emit.Callvirt(new MethodRef(typeof(C), nameof(arg.Baz)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Bar(ref C o)
    {
        o = new C(100);
        return 0;
    }

    public class C
    {
        public C(int result) => Result = result;
        public int Result;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Baz(int arg)
        {
            Runtime_73615.Result = Result;
        }
    }
}
