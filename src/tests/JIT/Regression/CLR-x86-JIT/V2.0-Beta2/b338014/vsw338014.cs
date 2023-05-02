// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// This is a coverage test...
//  The "fat" gc encoding was assumed to be dead code, but this test hits it
//
//  We want to hit PendingArgsStack::pasEnumGCoffs
//                 PendingArgsStack::pasEnumGCoffsCount

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class My
{

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static string foo(
        Object o0, Object o1, Object o2, Object o3, Object o4, Object o5, Object o6, Object o7, Object o8, Object o9,
        Object o10, Object o11, Object o12, Object o13, Object o14, Object o15, Object o16, Object o17, Object o18, Object o19,
        Object o20, Object o21, Object o22, Object o23, Object o24, Object o25, Object o26, Object o27, Object o28, Object o29,
        Object o30, Object o31, Object o32, Object o33, Object o34, Object o35, Object o36, Object o37, Object o38, Object o39)
    {
        return null;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static Object bar(Object o)
    {
        return null;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Object o = new Object();
        foo(o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, o, bar(o), o);

        return 100;
    }
}

