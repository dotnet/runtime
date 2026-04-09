// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

interface IMyInterface<T>
{
    int MyMethod();
}

sealed class MyInterfaceImpl<T> : IMyInterface<string>
{
    public int MyMethod() => 100;
}

public class Program
{
    [Fact]
    public static int TestEntryPoint() => Test<string>();

    [MethodImpl(MethodImplOptions.NoInlining | 
                MethodImplOptions.AggressiveOptimization)]
    static int Test<T>() => DoWork(new MyInterfaceImpl<T>());

    static int DoWork<T>(IMyInterface<T> a) => a.MyMethod();
}