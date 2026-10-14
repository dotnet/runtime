// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_130135;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

public static class Runtime_130135
{
    [Fact]
    public static void TestEntryPoint()
    {
        V2<int> result = Unbox(GetV2());

        Assert.Equal(1, result.X);
        Assert.Equal(2, result.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static V2<int> GetV2() =>
        Unsafe.BitCast<Vector64<int>, V2<int>>(CreateVector());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector64<int> CreateVector() => Vector64.Create(1, 2);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static V2<int> Unbox(object value) => (V2<int>)value;

    [StructLayout(LayoutKind.Sequential)]
    private struct V2<T>(T x, T y)
    {
        public T X = x;
        public T Y = y;
    }
}
