// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This test verifies if we correctly value number the operation of 
// x ^ x to zero.
//
// Found by Antigen

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Issue_91252
{
    static Vector64<int> s_v64_int_22 = Vector64.Create(-5);
    Vector64<int> v64_int_72 = Vector64.Create(-1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Repro()
    {
        s_v64_int_22 = v64_int_72;
        return Check(v64_int_72 ^ v64_int_72);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Check(Vector64<int> a)
    {
        return (a == Vector64<int>.Zero) ? 100 : 101;
    }

    [Fact]
    public static int EntryPoint()
    {
        var obj = new Issue_91252();
        return obj.Repro();
    }
}
