// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class SpanAccessor : IReturnSpan
{
    public static ReadOnlySpan<byte> RawData => new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    public ReadOnlySpan<byte> GetSpan()
    {
        return RawData;
    }
}

public class ThreadStaticSpanAccessor : IReturnSpan
{
    [ThreadStatic]
    public static byte ThreadStaticByte = 7;

    public unsafe ReadOnlySpan<byte> GetSpan()
    {
        return new ReadOnlySpan<byte>(Unsafe.AsPointer(ref ThreadStaticByte), 1);
    }
}