// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class BoundsCheck
{
    [Fact]
    public static int TestEntryPoint()
    {
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[7]);
        return (int)GetKey(span) + 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong GetKey(ReadOnlySpan<byte> propertyName)
    {
        const int BitsInByte = 8;
        ulong key = 0;
        int length = propertyName.Length;

        if (length > 3)
        {
            key = MemoryMarshal.Read<uint>(propertyName);

            if (length == 7)
            {
                key |= (ulong)propertyName[6] << (6 * BitsInByte)
                    | (ulong)propertyName[5] << (5 * BitsInByte)
                    | (ulong)propertyName[4] << (4 * BitsInByte)
                    | (ulong)7 << (7 * BitsInByte);
            }
        }

        return key;
    }
}
