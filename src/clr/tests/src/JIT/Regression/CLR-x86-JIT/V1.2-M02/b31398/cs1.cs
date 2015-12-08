// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

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
    public static int Main(String[] args)
    {
        cs1 t = new cs1();

        Console.WriteLine(t.GetType() == typeof(cs1) ? 100 : args.Length);
        return 100;
    }
}
