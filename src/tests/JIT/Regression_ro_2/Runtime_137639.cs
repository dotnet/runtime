// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_137639
{
    private ref struct S
    {
        public ref int? Field;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<NullReferenceException>(() => Test(default, new string[1], 5));

        int? value = 42;
        Test(new S { Field = ref value }, new[] { nameof(value) }, 0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test(S s, string[] names, int idx)
    {
        ArgumentNullException.ThrowIfNull(s.Field, names[idx]);
    }
}
