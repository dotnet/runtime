// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class MaxArrayElementSize
{
    [InlineArray(65535)]
    struct MaxSizedArrayElement
    {
        byte b;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return sizeof(MaxSizedArrayElement) == ushort.MaxValue
            && new MaxSizedArrayElement[1].Length == 1
            ? 100
            : 101;
    }
}
