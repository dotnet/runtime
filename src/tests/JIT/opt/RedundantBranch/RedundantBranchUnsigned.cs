// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

// Runtime 65327
// M1 and M2 should generate the same code

public class RedundantBranchUnsigned
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public ReadOnlySpan<char> M1(ReadOnlySpan<char> span, int i)
    {
        // Note `<` here instead of `<=`
        //
        if ((uint)i < (uint)span.Length)
        {
            return span.Slice(i);
        }
        return default;
    }
  
    [MethodImpl(MethodImplOptions.NoInlining)] 
    public ReadOnlySpan<char> M2(ReadOnlySpan<char> span, int i)
    {
        if ((uint)i <= (uint)span.Length)
        {
            return span.Slice(i);
        }
        return default;
    }

    public static int Main()
    {
        C c = new C();
        var m1 = c.M1("hello", 2);
        var m2 = c.M2("hello", 3);

        return m1.Length + m2.Length + 95;
    }
}
