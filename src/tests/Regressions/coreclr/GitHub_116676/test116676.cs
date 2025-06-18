// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

public class Test116676
{
    [UnsafeAccessor(UnsafeAccessorKind.Method)]
    extern static void DoesNotExist([UnsafeAccessorType("DoesNotExist")] object a);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Work(bool f)
    {
        if (f)
            DoesNotExist(null);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 200; i++)
        {
            Thread.Sleep(10);
            Work(false);
        }
    }
}

