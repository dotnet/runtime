// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using Xunit;

public class Runtime_67223
{
    [Fact]
    public static int TestEntryPoint()
    {
        short[] foo = { short.MinValue };
        int test = BinaryPrimitives.ReverseEndianness(foo[0]);
        return test == 0x80 ? 100 : -1;
    }
}