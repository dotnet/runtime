// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Runtime_134192
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct S3
    {
        public byte A, B, C;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        S3[] a = new S3[4];
        a[0].A = 42;
        Assert.Equal(42, Get(a, -6148914691236517206L));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static byte Get(S3[] a, long x) => a[x + 6148914691236517206L].A;
}
