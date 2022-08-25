// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class Program
{
    public static int Main()
    {
        Console.WriteLine(CallFoo(new C()));
	return 100;
    }

    // CHECK: Assembly listing for method Program:CallFoo
    // This is testing that a constrained.callvirt through a T variable doesn't use a helper lookup.
    // CHECK-NOT: CORINFO_HELP
    // CHECK: Total bytes
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallFoo<T>(T val) where T : IFace
    {
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
