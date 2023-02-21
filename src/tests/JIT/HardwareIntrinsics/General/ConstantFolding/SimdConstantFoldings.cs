// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;
using Xunit;

public class SimdConstantFoldings
{
    [Fact]
    public static void NegateTests()
    {
        Assert.Equal(
            Vector128.Create((byte)(0xFF), 0x02, 0xFD, 0x04, 0xFB, 0x06, 0xF9, 0x08, 0xF7, 0x0A, 0xF5, 0x0C, 0xF3, 0x0E, 0xF1, 0x10),
           -Vector128.Create((byte)(0x01), 0xFE, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0)
        );

        Assert.Equal(
            Vector128.Create((ushort)(0xFFFF), 0x0002, 0xFFFD, 0x0004, 0xFFFB, 0x0006, 0xFFF9, 0x0008),
           -Vector128.Create((ushort)(0x0001), 0xFFFE, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8)
        );

        Assert.Equal(
            Vector128.Create((uint)(0xFFFF_FFFF), 0x0000_0002, 0xFFFF_FFFD, 0x0000_0004),
           -Vector128.Create((uint)(0x0000_0001), 0xFFFF_FFFE, 0x0000_0003, 0xFFFF_FFFC)
        );

        Assert.Equal(
            Vector128.Create((ulong)(0xFFFF_FFFF_FFFF_FFFF), 0x0000_0000_0000_0002),
           -Vector128.Create((ulong)(0x0000_0000_0000_0001), 0xFFFF_FFFF_FFFF_FFFE)
        );

        Assert.Equal(
            Vector128.Create((sbyte)(-1), +2, -3, +4, -5, +6, -7, +8, -9, +10, -11, +12, -13, +14, -15, +16),
           -Vector128.Create((sbyte)(+1), -2, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16)
        );

        Assert.Equal(
            Vector128.Create((short)(-1), +2, -3, +4, -5, +6, -7, +8),
           -Vector128.Create((short)(+1), -2, +3, -4, +5, -6, +7, -8)
        );

        Assert.Equal(
            Vector128.Create((int)(-1), +2, -3, +4),
           -Vector128.Create((int)(+1), -2, +3, -4)
        );

        Assert.Equal(
            Vector128.Create((long)(-1), +2),
           -Vector128.Create((long)(+1), -2)
        );

        Assert.Equal(
            Vector128.Create((float)(-1), +2, -3, +4),
           -Vector128.Create((float)(+1), -2, +3, -4)
        );

        Assert.Equal(
            Vector128.Create((double)(-1), +2),
           -Vector128.Create((double)(+1), -2)
        );
    }

    [Fact]
    public static void AddTests()
    {
        Assert.Equal(
            Vector128.Create((byte)(0x02), 0xFF, 0x06, 0xF8, 0x0A, 0xF4, 0x0E, 0xF0, 0x12, 0xEC, 0x16, 0xE8, 0x1A, 0xE4, 0x1E, 0xE0),
            Vector128.Create((byte)(0x01), 0xFE, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0)
          + Vector128.Create((byte)(0x01), 0x01, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0)
        );

        Assert.Equal(
            Vector128.Create((ushort)(0x0002), 0xFFFF, 0x0006, 0xFFF8, 0x000A, 0xFFF4, 0x000E, 0xFFF0),
            Vector128.Create((ushort)(0x0001), 0xFFFE, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8)
          + Vector128.Create((ushort)(0x0001), 0x0001, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8)
        );

        Assert.Equal(
            Vector128.Create((uint)(0x0000_0002), 0xFFFF_FFFF, 0x0000_0006, 0xFFFF_FFF8),
            Vector128.Create((uint)(0x0000_0001), 0xFFFF_FFFE, 0x0000_0003, 0xFFFF_FFFC)
          + Vector128.Create((uint)(0x0000_0001), 0x0000_0001, 0x0000_0003, 0xFFFF_FFFC)
        );

        Assert.Equal(
            Vector128.Create((ulong)(0x0000_0000_0000_0002), 0xFFFF_FFFF_FFFF_FFFF),
            Vector128.Create((ulong)(0x0000_0000_0000_0001), 0xFFFF_FFFF_FFFF_FFFE)
          + Vector128.Create((ulong)(0x0000_0000_0000_0001), 0x0000_0000_0000_0001)
        );

        Assert.Equal(
            Vector128.Create((sbyte)(+2), -1, +6, -8, +10, -12, +14, -16, +18, -20, +22, -24, +26, -28, +30, -32),
            Vector128.Create((sbyte)(+1), -2, +3, -4, +05, -06, +07, -08, +09, -10, +11, -12, +13, -14, +15, -16)
          + Vector128.Create((sbyte)(+1), +1, +3, -4, +05, -06, +07, -08, +09, -10, +11, -12, +13, -14, +15, -16)
        );

        Assert.Equal(
            Vector128.Create((short)(+2), -1, +6, -8, +10, -12, +14, -16),
            Vector128.Create((short)(+1), -2, +3, -4, +05, -06, +07, -08)
          + Vector128.Create((short)(+1), +1, +3, -4, +05, -06, +07, -08)
        );

        Assert.Equal(
            Vector128.Create((int)(+2), -1, +6, -8),
            Vector128.Create((int)(+1), -2, +3, -4)
          + Vector128.Create((int)(+1), +1, +3, -4)
        );

        Assert.Equal(
            Vector128.Create((long)(+2), -1),
            Vector128.Create((long)(+1), -2)
          + Vector128.Create((long)(+1), +1)
        );

        Assert.Equal(
            Vector128.Create((float)(+2), -1, +6, -8),
            Vector128.Create((float)(+1), -2, +3, -4)
          + Vector128.Create((float)(+1), +1, +3, -4)
        );

        Assert.Equal(
            Vector128.Create((double)(+2), -1),
            Vector128.Create((double)(+1), -2)
          + Vector128.Create((double)(+1), +1)
        );
    }

    [Fact]
    public static void SubtractTests()
    {
        Assert.Equal(
            Vector128.Create((byte)(0x00), 0xFD, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00),
            Vector128.Create((byte)(0x01), 0xFE, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0)
          - Vector128.Create((byte)(0x01), 0x01, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0)
        );

        Assert.Equal(
            Vector128.Create((ushort)(0x0000), 0xFFFD, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000),
            Vector128.Create((ushort)(0x0001), 0xFFFE, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8)
          - Vector128.Create((ushort)(0x0001), 0x0001, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8)
        );

        Assert.Equal(
            Vector128.Create((uint)(0x0000_0000), 0xFFFF_FFFD, 0x0000_0000, 0x0000_0000),
            Vector128.Create((uint)(0x0000_0001), 0xFFFF_FFFE, 0x0000_0003, 0xFFFF_FFFC)
          - Vector128.Create((uint)(0x0000_0001), 0x0000_0001, 0x0000_0003, 0xFFFF_FFFC)
        );

        Assert.Equal(
            Vector128.Create((ulong)(0x0000_0000_0000_0000), 0xFFFF_FFFF_FFFF_FFFD),
            Vector128.Create((ulong)(0x0000_0000_0000_0001), 0xFFFF_FFFF_FFFF_FFFE)
          - Vector128.Create((ulong)(0x0000_0000_0000_0001), 0x0000_0000_0000_0001)
        );

        Assert.Equal(
            Vector128.Create((sbyte)(+0), -3, +0, +0, +00, +00, +00, +00, +00, +00, +00, +00, +00, +00, +00, +00),
            Vector128.Create((sbyte)(+1), -2, +3, -4, +05, -06, +07, -08, +09, -10, +11, -12, +13, -14, +15, -16)
          - Vector128.Create((sbyte)(+1), +1, +3, -4, +05, -06, +07, -08, +09, -10, +11, -12, +13, -14, +15, -16)
        );

        Assert.Equal(
            Vector128.Create((short)(+0), -3, +0, +0, +00, +00, +00, +00),
            Vector128.Create((short)(+1), -2, +3, -4, +05, -06, +07, -08)
          - Vector128.Create((short)(+1), +1, +3, -4, +05, -06, +07, -08)
        );

        Assert.Equal(
            Vector128.Create((int)(+0), -3, +0, +0),
            Vector128.Create((int)(+1), -2, +3, -4)
          - Vector128.Create((int)(+1), +1, +3, -4)
        );

        Assert.Equal(
            Vector128.Create((long)(+0), -3),
            Vector128.Create((long)(+1), -2)
          - Vector128.Create((long)(+1), +1)
        );

        Assert.Equal(
            Vector128.Create((float)(+0), -3, +0, +0),
            Vector128.Create((float)(+1), -2, +3, -4)
          - Vector128.Create((float)(+1), +1, +3, -4)
        );

        Assert.Equal(
            Vector128.Create((double)(+0), -3),
            Vector128.Create((double)(+1), -2)
          - Vector128.Create((double)(+1), +1)
        );
    }

    [Fact]
    public static void GetElementTests()
    {
        Assert.Equal(
            (byte)(0xFE),
            Vector128.Create((byte)(0x01), 0xFE, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0).GetElement(1)
        );

        Assert.Equal(
            (ushort)(0xFFFE),
            Vector128.Create((ushort)(0x0001), 0xFFFE, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8).GetElement(1)
        );

        Assert.Equal(
            (uint)(0xFFFF_FFFE),
            Vector128.Create((uint)(0x0000_0001), 0xFFFF_FFFE, 0x0000_0003, 0xFFFF_FFFC).GetElement(1)
        );

        Assert.Equal(
            (ulong)(0xFFFF_FFFF_FFFF_FFFE),
            Vector128.Create((ulong)(0x0000_0000_0000_0001), 0xFFFF_FFFF_FFFF_FFFE).GetElement(1)
        );

        Assert.Equal(
            (sbyte)(-2),
            Vector128.Create((sbyte)(+1), -2, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16).GetElement(1)
        );

        Assert.Equal(
            (short)(-2),
            Vector128.Create((short)(+1), -2, +3, -4, +5, -6, +7, -8).GetElement(1)
        );

        Assert.Equal(
            (int)(-2),
            Vector128.Create((int)(+1), -2, +3, -4).GetElement(1)
        );

        Assert.Equal(
            (long)(-2),
            Vector128.Create((long)(+1), -2).GetElement(1)
        );

        Assert.Equal(
            (float)(-2),
            Vector128.Create((float)(+1), -2, +3, -4).GetElement(1)
        );

        Assert.Equal(
            (double)(-2),
            Vector128.Create((double)(+1), -2).GetElement(1)
        );
    }
}
