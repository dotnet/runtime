// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

internal delegate int Deleg(int i, int j, int k, int l, int m, int n);

public class cs1
{
    public static int foo(int i, int j, int k, int l, int m, int n)
    {
        return i * 100;
    }

    public const System.Runtime.CompilerServices.MethodImplOptions s_enum2 = System.Runtime.CompilerServices.MethodImplOptions.NoInlining;

    [System.Runtime.CompilerServices.MethodImpl(
      System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [Fact]
    public static int TestEntryPoint()
    {
        cs1 t = new cs1();

        int result = t.GetType() == typeof(cs1) ? 100 : 1;
        Console.WriteLine(result);
        return result;
    }
}
