// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class Program
{
    public static void Main()
    {
        Console.WriteLine(CallFoo(new C()));
    }

    // CHECK: Assembly listing for method Program::CallFoo
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
