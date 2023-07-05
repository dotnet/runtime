// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

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

    [Fact]
    public static int TestEntryPoint()
    {
        var rbu = new RedundantBranchUnsigned();
        var m1 = rbu.M1("hello", 2);
        var m2 = rbu.M2("hello", 3);

        return m1.Length + m2.Length + 95;
    }
}
