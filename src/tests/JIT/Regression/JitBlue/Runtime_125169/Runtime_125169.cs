// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public readonly struct ByteArrayWrapper
{
    public readonly byte[] _innerArray;

    public ByteArrayWrapper(scoped in byte[] innerArray)
    {
        _innerArray = innerArray;
    }
}

public readonly ref struct RefWrapper
{
    public static readonly ByteArrayWrapper _data = new ByteArrayWrapper(new byte[] { 0, 1, 2, 3, 4 });
}

public class Runtime_125169
{
    // Regression test for bug in JIT where ref struct static fields of reference type initialized in
    // its static constructor were missing calls to JIT_ByRefWriteBarrier for each such static field.
    [Fact]
    public static void TestEntryPoint()
    {
        byte[][] arr = new byte[100][];
        // Generate enough garbage
        for (int i = 0; i < 100; i++)
        {
            arr[i] = new byte[100000];
        }

        ReadOnlySpan<byte> testSpan = new(RefWrapper._data._innerArray);
        // Without the fix, the following GC.Collect call causes fatal error during heap verification with DOTNET_HeapVerify=1
        GC.Collect(0);

        // Prevent the compiler from optimizing out the loop above.
        GC.KeepAlive(arr);

        for (int i = 0; i < testSpan.Length; i++)
        {
            Assert.Equal(i, testSpan[i]);
        }
    }
}
