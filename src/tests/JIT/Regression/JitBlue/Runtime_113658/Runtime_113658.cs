// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class Runtime_113658
{
    [Fact]
    public static int TestEntryPoint()
    {
        FillStackWithGarbage();
        long? nullable = FaultyDefaultNullable<long?>();
        return (int)(100 + nullable.GetValueOrDefault());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FillStackWithGarbage()
    {
        stackalloc byte[256].Fill(0xcc);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    private static T FaultyDefaultNullable<T>()
    {
        // When T is a Nullable<T> (and only in that case), we support returning null
        if (default(T) is null && typeof(T).IsValueType)
            return default!;

        throw new InvalidOperationException("Not nullable");
    }
}
