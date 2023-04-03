// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine(CallFoo(new C()));
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallFoo<T>(T val) where T : IFace
    {
        // This is testing that a constrained.callvirt through a T variable doesn't use a helper lookup.
        // CHECK-NOT: CORINFO_HELP
        return val.Foo();
    }
}

public interface IFace
{
    int Foo();
}

public class C : IFace
{
    public int Foo() => 0;
}
