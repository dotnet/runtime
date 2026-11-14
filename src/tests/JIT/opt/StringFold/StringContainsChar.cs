// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

public static class StringContainsChar
{
    public static IEnumerable<object[]> ASCII => Enumerable.Range(0, 256).Select(x => new object[] { (char)x });

    [Theory]
    [MemberData(nameof(ASCII))]
    public static void ContainsRange(char c)
        => Assert.Equal(ContainsABCDEFNoInline(c), ContainsABCDEFInline(c));

    [Theory]
    [MemberData(nameof(ASCII))]
    public static void ContainsMask(char c)
        => Assert.Equal(ContainsHexNoInline(c), ContainsHexInline(c));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ContainsABCDEFNoInline(char c) => "ABCDEF".Contains(c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsABCDEFInline(char c) => "ABCDEF".Contains(c);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ContainsHexNoInline(char c) => "0123456789ABCDEF".Contains(c);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsHexInline(char c) => "0123456789ABCDEF".Contains(c);
}
