// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Xunit;

public class Runtime_39823
{
    struct IntsWrapped
    {
        public int i1;
        public int i2;
        public int i3;
        public int i4;
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe int TestUnusedObjCopy(IntsWrapped* ps)
    {
        IntsWrapped s = *ps;
        return 100;
    }


    [Fact]
    public static unsafe int TestEntryPoint()
    {
        try
        {
            TestUnusedObjCopy((IntsWrapped*)0);
            Debug.Assert(false, "unreachable");
        }
        catch
        {
            return 100;
        }
        return -1;
    }
}
