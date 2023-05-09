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
            Vector128.Create((sbyte)(+0), -3, +0, +0, +0, +0, +0, +0, +0, +00, +00, +00, +00, +00, +00, +00),
            Vector128.Create((sbyte)(+1), -2, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16)
          - Vector128.Create((sbyte)(+1), +1, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16)
        );

        Assert.Equal(
            Vector128.Create((short)(+0), -3, +0, +0, +0, +0, +0, +0),
            Vector128.Create((short)(+1), -2, +3, -4, +5, -6, +7, -8)
          - Vector128.Create((short)(+1), +1, +3, -4, +5, -6, +7, -8)
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

    [Fact]
    public static void NotTests()
    {
        Assert.Equal(
            Vector128.Create((byte)(0xFE), 0x01, 0xFC, 0x03, 0xFA, 0x05, 0xF8, 0x07, 0xF6, 0x09, 0xF4, 0x0B, 0xF2, 0x0D, 0xF0, 0x0F),
           ~Vector128.Create((byte)(0x01), 0xFE, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0)
        );

        Assert.Equal(
            Vector128.Create((ushort)(0xFFFE), 0x0001, 0xFFFC, 0x0003, 0xFFFA, 0x0005, 0xFFF8, 0x0007),
           ~Vector128.Create((ushort)(0x0001), 0xFFFE, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8)
        );

        Assert.Equal(
            Vector128.Create((uint)(0xFFFF_FFFE), 0x0000_0001, 0xFFFF_FFFC, 0x0000_0003),
           ~Vector128.Create((uint)(0x0000_0001), 0xFFFF_FFFE, 0x0000_0003, 0xFFFF_FFFC)
        );

        Assert.Equal(
            Vector128.Create((ulong)(0xFFFF_FFFF_FFFF_FFFE), 0x0000_0000_0000_0001),
           ~Vector128.Create((ulong)(0x0000_0000_0000_0001), 0xFFFF_FFFF_FFFF_FFFE)
        );

        Assert.Equal(
            Vector128.Create((sbyte)(-2), +1, -4, +3, -6, +5, -8, +7, -10, +9, -12, +11, -14, +13, -16, +15),
           ~Vector128.Create((sbyte)(+1), -2, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16)
        );

        Assert.Equal(
            Vector128.Create((short)(-2), +1, -4, +3, -6, +5, -8, +7),
           ~Vector128.Create((short)(+1), -2, +3, -4, +5, -6, +7, -8)
        );

        Assert.Equal(
            Vector128.Create((int)(-2), +1, -4, +3),
           ~Vector128.Create((int)(+1), -2, +3, -4)
        );

        Assert.Equal(
            Vector128.Create((long)(-2), +1),
           ~Vector128.Create((long)(+1), -2)
        );

        Assert.Equal(
            Vector128.Create((float)(-3.9999998f), +1.9999999f, -1.4999999f, +0.99999994f),
           ~Vector128.Create((float)(+1),          -2,          +3,          -4)
        );

        Assert.Equal(
            Vector128.Create((double)(-3.9999999999999996), +1.9999999999999998),
           ~Vector128.Create((double)(+1),                  -2)
        );
    }

    [Fact]
    public static void AndTests()
    {
        Assert.Equal(
            Vector128.Create((byte)(0x01), 0x00, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0),
            Vector128.Create((byte)(0x01), 0xFE, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0)
          & Vector128.Create((byte)(0x01), 0x01, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0)
        );

        Assert.Equal(
            Vector128.Create((ushort)(0x0001), 0x0000, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8),
            Vector128.Create((ushort)(0x0001), 0xFFFE, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8)
          & Vector128.Create((ushort)(0x0001), 0x0001, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8)
        );

        Assert.Equal(
            Vector128.Create((uint)(0x0000_0001), 0x0000_0000, 0x0000_0003, 0xFFFF_FFFC),
            Vector128.Create((uint)(0x0000_0001), 0xFFFF_FFFE, 0x0000_0003, 0xFFFF_FFFC)
          & Vector128.Create((uint)(0x0000_0001), 0x0000_0001, 0x0000_0003, 0xFFFF_FFFC)
        );

        Assert.Equal(
            Vector128.Create((ulong)(0x0000_0000_0000_0001), 0x0000_0000_0000_0000),
            Vector128.Create((ulong)(0x0000_0000_0000_0001), 0xFFFF_FFFF_FFFF_FFFE)
          & Vector128.Create((ulong)(0x0000_0000_0000_0001), 0x0000_0000_0000_0001)
        );

        Assert.Equal(
            Vector128.Create((sbyte)(+1), +0, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16),
            Vector128.Create((sbyte)(+1), -2, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16)
          & Vector128.Create((sbyte)(+1), +1, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16)
        );

        Assert.Equal(
            Vector128.Create((short)(+1), +0, +3, -4, +5, -6, +7, -8),
            Vector128.Create((short)(+1), -2, +3, -4, +5, -6, +7, -8)
          & Vector128.Create((short)(+1), +1, +3, -4, +5, -6, +7, -8)
        );

        Assert.Equal(
            Vector128.Create((int)(+1), +0, +3, -4),
            Vector128.Create((int)(+1), -2, +3, -4)
          & Vector128.Create((int)(+1), +1, +3, -4)
        );

        Assert.Equal(
            Vector128.Create((long)(+1), +0),
            Vector128.Create((long)(+1), -2)
          & Vector128.Create((long)(+1), +1)
        );

        Assert.Equal(
            Vector128.Create((float)(+1), +0, +3, -4),
            Vector128.Create((float)(+1), -2, +3, -4)
          & Vector128.Create((float)(+1), +1, +3, -4)
        );

        Assert.Equal(
            Vector128.Create((double)(+1), +0),
            Vector128.Create((double)(+1), -2)
          & Vector128.Create((double)(+1), +1)
        );
    }

    [Fact]
    public static void AndNotTests()
    {
        Assert.Equal(
            Vector128.Create((byte)(0x00), 0xFE, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00),
            Vector128.AndNot(
                Vector128.Create((byte)(0x01), 0xFE, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0),
                Vector128.Create((byte)(0x01), 0x01, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0)
            )
        );

        Assert.Equal(
            Vector128.Create((ushort)(0x0000), 0xFFFE, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000),
            Vector128.AndNot(
                Vector128.Create((ushort)(0x0001), 0xFFFE, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8),
                Vector128.Create((ushort)(0x0001), 0x0001, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8)
            )
        );

        Assert.Equal(
            Vector128.Create((uint)(0x0000_0000), 0xFFFF_FFFE, 0x0000_0000, 0x0000_0000),
            Vector128.AndNot(
                Vector128.Create((uint)(0x0000_0001), 0xFFFF_FFFE, 0x0000_0003, 0xFFFF_FFFC),
                Vector128.Create((uint)(0x0000_0001), 0x0000_0001, 0x0000_0003, 0xFFFF_FFFC)
            )
        );

        Assert.Equal(
            Vector128.Create((ulong)(0x0000_0000_0000_0000), 0xFFFF_FFFF_FFFF_FFFE),
            Vector128.AndNot(
                Vector128.Create((ulong)(0x0000_0000_0000_0001), 0xFFFF_FFFF_FFFF_FFFE),
                Vector128.Create((ulong)(0x0000_0000_0000_0001), 0x0000_0000_0000_0001)
            )
        );

        Assert.Equal(
            Vector128.Create((sbyte)(+0), -2, +0, +0, +0, +0, +0, +0, +0, +00, +00, +00, +00, +00, +00, +00),
            Vector128.AndNot(
                Vector128.Create((sbyte)(+1), -2, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16),
                Vector128.Create((sbyte)(+1), +1, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16)
            )
        );

        Assert.Equal(
            Vector128.Create((short)(+0), -2, +0, +0, +0, +0, +0, +0),
            Vector128.AndNot(
                Vector128.Create((short)(+1), -2, +3, -4, +5, -6, +7, -8),
                Vector128.Create((short)(+1), +1, +3, -4, +5, -6, +7, -8)
            )
        );

        Assert.Equal(
            Vector128.Create((int)(+0), -2, +0, +0),
            Vector128.AndNot(
                Vector128.Create((int)(+1), -2, +3, -4),
                Vector128.Create((int)(+1), +1, +3, -4)
            )
        );

        Assert.Equal(
            Vector128.Create((long)(+0), -2),
            Vector128.AndNot(
                Vector128.Create((long)(+1), -2),
                Vector128.Create((long)(+1), +1)
            )
        );

        Assert.Equal(
            Vector128.Create((float)(+0), -2, +0, +0),
            Vector128.AndNot(
                Vector128.Create((float)(+1), -2, +3, -4),
                Vector128.Create((float)(+1), +1, +3, -4)
            )
        );

        Assert.Equal(
            Vector128.Create((double)(+0), -2),
            Vector128.AndNot(
                Vector128.Create((double)(+1), -2),
                Vector128.Create((double)(+1), +1)
            )
        );
    }

    [Fact]
    public static void LeftShiftTests()
    {
        Assert.Equal(
            Vector128.Create((byte)(0x02), 0xFC, 0x06, 0xF8, 0x0A, 0xF4, 0x0E, 0xF0, 0x12, 0xEC, 0x16, 0xE8, 0x1A, 0xE4, 0x1E, 0xE0),
            Vector128.Create((byte)(0x01), 0xFE, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0) << 1
        );

        Assert.Equal(
            Vector128.Create((ushort)(0x0002), 0xFFFC, 0x0006, 0xFFF8, 0x000A, 0xFFF4, 0x000E, 0xFFF0),
            Vector128.Create((ushort)(0x0001), 0xFFFE, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8) << 1
        );

        Assert.Equal(
            Vector128.Create((uint)(0x0000_0002), 0xFFFF_FFFC, 0x0000_0006, 0xFFFF_FFF8),
            Vector128.Create((uint)(0x0000_0001), 0xFFFF_FFFE, 0x0000_0003, 0xFFFF_FFFC) << 1
        );

        Assert.Equal(
            Vector128.Create((ulong)(0x0000_0000_0000_0002), 0xFFFF_FFFF_FFFF_FFFC),
            Vector128.Create((ulong)(0x0000_0000_0000_0001), 0xFFFF_FFFF_FFFF_FFFE) << 1
        );

        Assert.Equal(
            Vector128.Create((sbyte)(+2), -4, +6, -8, +10, -12, +14, -16, +18, -20, +22, -24, +26, -28, +30, -32),
            Vector128.Create((sbyte)(+1), -2, +3, -4, +05, -06, +07, -08, +09, -10, +11, -12, +13, -14, +15, -16) << 1
        );

        Assert.Equal(
            Vector128.Create((short)(+2), -4, +6, -8, +10, -12, +14, -16),
            Vector128.Create((short)(+1), -2, +3, -4, +05, -06, +07, -08) << 1
        );

        Assert.Equal(
            Vector128.Create((int)(+2), -4, +6, -8),
            Vector128.Create((int)(+1), -2, +3, -4) << 1
        );

        Assert.Equal(
            Vector128.Create((long)(+2), -4),
            Vector128.Create((long)(+1), -2) << 1
        );

        Assert.Equal(
            Vector128.Create((float)(+1.7014118E+38f), -0.0f, -1.1754944E-38f, -2.3509887E-38f),
            Vector128.Create((float)(+1),              -2,    +3,              -4) << 1
        );

        Assert.Equal(
            Vector128.Create((double)(+8.98846567431158E+307), -0.0),
            Vector128.Create((double)(+1),                     -2) << 1
        );
    }

    [Fact]
    public static void OrTests()
    {
        Assert.Equal(
            Vector128.Create((byte)(0x01), 0xFF, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0),
            Vector128.Create((byte)(0x01), 0xFE, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0)
          | Vector128.Create((byte)(0x01), 0x01, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0)
        );

        Assert.Equal(
            Vector128.Create((ushort)(0x0001), 0xFFFF, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8),
            Vector128.Create((ushort)(0x0001), 0xFFFE, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8)
          | Vector128.Create((ushort)(0x0001), 0x0001, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8)
        );

        Assert.Equal(
            Vector128.Create((uint)(0x0000_0001), 0xFFFF_FFFF, 0x0000_0003, 0xFFFF_FFFC),
            Vector128.Create((uint)(0x0000_0001), 0xFFFF_FFFE, 0x0000_0003, 0xFFFF_FFFC)
          | Vector128.Create((uint)(0x0000_0001), 0x0000_0001, 0x0000_0003, 0xFFFF_FFFC)
        );

        Assert.Equal(
            Vector128.Create((ulong)(0x0000_0000_0000_0001), 0xFFFF_FFFF_FFFF_FFFF),
            Vector128.Create((ulong)(0x0000_0000_0000_0001), 0xFFFF_FFFF_FFFF_FFFE)
          | Vector128.Create((ulong)(0x0000_0000_0000_0001), 0x0000_0000_0000_0001)
        );

        Assert.Equal(
            Vector128.Create((sbyte)(+1), -1, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16),
            Vector128.Create((sbyte)(+1), -2, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16)
          | Vector128.Create((sbyte)(+1), +1, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16)
        );

        Assert.Equal(
            Vector128.Create((short)(+1), -1, +3, -4, +5, -6, +7, -8),
            Vector128.Create((short)(+1), -2, +3, -4, +5, -6, +7, -8)
          | Vector128.Create((short)(+1), +1, +3, -4, +5, -6, +7, -8)
        );

        Assert.Equal(
            Vector128.Create((int)(+1), -1, +3, -4),
            Vector128.Create((int)(+1), -2, +3, -4)
          | Vector128.Create((int)(+1), +1, +3, -4)
        );

        Assert.Equal(
            Vector128.Create((long)(+1), -1),
            Vector128.Create((long)(+1), -2)
          | Vector128.Create((long)(+1), +1)
        );

        Assert.Equal(
            Vector128.Create((float)(+1), +float.NegativeInfinity, +3, -4),
            Vector128.Create((float)(+1), -2,                      +3, -4)
          | Vector128.Create((float)(+1), +1,                      +3, -4)
        );

        Assert.Equal(
            Vector128.Create((double)(+1), +double.NegativeInfinity),
            Vector128.Create((double)(+1), -2)
          | Vector128.Create((double)(+1), +1)
        );
    }

    [Fact]
    public static void RightShiftTests()
    {
        Assert.Equal(
            Vector128.Create((byte)(0x00), 0x7F, 0x01, 0x7E, 0x02, 0x7D, 0x03, 0x7C, 0x04, 0x7B, 0x05, 0x7A, 0x06, 0x79, 0x07, 0x78),
            Vector128.Create((byte)(0x01), 0xFE, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0) >> 1
        );

        Assert.Equal(
            Vector128.Create((ushort)(0x0000), 0x7FFF, 0x0001, 0x7FFE, 0x0002, 0x7FFD, 0x0003, 0x7FFC),
            Vector128.Create((ushort)(0x0001), 0xFFFE, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8) >> 1
        );

        Assert.Equal(
            Vector128.Create((uint)(0x0000_0000), 0x7FFF_FFFF, 0x0000_0001, 0x7FFF_FFFE),
            Vector128.Create((uint)(0x0000_0001), 0xFFFF_FFFE, 0x0000_0003, 0xFFFF_FFFC) >> 1
        );

        Assert.Equal(
            Vector128.Create((ulong)(0x0000_0000_0000_0000), 0x7FFF_FFFF_FFFF_FFFF),
            Vector128.Create((ulong)(0x0000_0000_0000_0001), 0xFFFF_FFFF_FFFF_FFFE) >> 1
        );

        Assert.Equal(
            Vector128.Create((sbyte)(+0), -1, +1, -2, +2, -3, +3, -4, +4, -05, +05, -06, +06, -07, +07, -08),
            Vector128.Create((sbyte)(+1), -2, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16) >> 1
        );

        Assert.Equal(
            Vector128.Create((short)(+0), -1, +1, -2, +2, -3, +3, -4),
            Vector128.Create((short)(+1), -2, +3, -4, +5, -6, +7, -8) >> 1
        );

        Assert.Equal(
            Vector128.Create((int)(+0), -1, +1, -2),
            Vector128.Create((int)(+1), -2, +3, -4) >> 1
        );

        Assert.Equal(
            Vector128.Create((long)(+0), -1),
            Vector128.Create((long)(+1), -2) >> 1
        );

        Assert.Equal(
            Vector128.Create((float)(+8.131516E-20f), -3.689349E+19f, +1.3552527E-19f, -5.5340232E+19f),
            Vector128.Create((float)(+1),             -2,             +3,              -4) >> 1
        );

        Assert.Equal(
            Vector128.Create((double)(+1.118751109680031E-154), -2.6815615859885194E+154),
            Vector128.Create((double)(+1),                      -2) >> 1
        );
    }

    [Fact]
    public static void UnsignedRightShiftTests()
    {
        Assert.Equal(
            Vector128.Create((byte)(0x00), 0x7F, 0x01, 0x7E, 0x02, 0x7D, 0x03, 0x7C, 0x04, 0x7B, 0x05, 0x7A, 0x06, 0x79, 0x07, 0x78),
            Vector128.Create((byte)(0x01), 0xFE, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0) >>> 1
        );

        Assert.Equal(
            Vector128.Create((ushort)(0x0000), 0x7FFF, 0x0001, 0x7FFE, 0x0002, 0x7FFD, 0x0003, 0x7FFC),
            Vector128.Create((ushort)(0x0001), 0xFFFE, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8) >>> 1
        );

        Assert.Equal(
            Vector128.Create((uint)(0x0000_0000), 0x7FFF_FFFF, 0x0000_0001, 0x7FFF_FFFE),
            Vector128.Create((uint)(0x0000_0001), 0xFFFF_FFFE, 0x0000_0003, 0xFFFF_FFFC) >>> 1
        );

        Assert.Equal(
            Vector128.Create((ulong)(0x0000_0000_0000_0000), 0x7FFF_FFFF_FFFF_FFFF),
            Vector128.Create((ulong)(0x0000_0000_0000_0001), 0xFFFF_FFFF_FFFF_FFFE) >>> 1
        );

        Assert.Equal(
            Vector128.Create((sbyte)(+0), +127, +1, +126, +2, +125, +3, +124, +4, +123, +05, +122, +06, +121, +07, +120),
            Vector128.Create((sbyte)(+1), -002, +3, -004, +5, -006, +7, -008, +9, -010, +11, -012, +13, -014, +15, -016) >>> 1
        );

        Assert.Equal(
            Vector128.Create((short)(+0), +32767, +1, +32766, +2, +32765, +3, +32764),
            Vector128.Create((short)(+1), -00002, +3, -00004, +5, -00006, +7, -00008) >>> 1
        );

        Assert.Equal(
            Vector128.Create((int)(+0), +2147483647, +1, +2147483646),
            Vector128.Create((int)(+1), -0000000002, +3, -0000000004) >>> 1
        );

        Assert.Equal(
            Vector128.Create((long)(+0), +9223372036854775807),
            Vector128.Create((long)(+1), -0000000000000000002) >>> 1
        );

        Assert.Equal(
            Vector128.Create((float)(+8.131516E-20f), +3.689349E+19f, +1.3552527E-19f, +5.5340232E+19f),
            Vector128.Create((float)(+1),             -2,             +3,              -4) >>> 1
        );

        Assert.Equal(
            Vector128.Create((double)(+1.118751109680031E-154), +2.6815615859885194E+154),
            Vector128.Create((double)(+1),                      -2) >>> 1
        );
    }

    [Fact]
    public static void XorTests()
    {
        Assert.Equal(
            Vector128.Create((byte)(0x00), 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00),
            Vector128.Create((byte)(0x01), 0xFE, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0)
          ^ Vector128.Create((byte)(0x01), 0x01, 0x03, 0xFC, 0x05, 0xFA, 0x07, 0xF8, 0x09, 0xF6, 0x0B, 0xF4, 0x0D, 0xF2, 0x0F, 0xF0)
        );

        Assert.Equal(
            Vector128.Create((ushort)(0x0000), 0xFFFF, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000),
            Vector128.Create((ushort)(0x0001), 0xFFFE, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8)
          ^ Vector128.Create((ushort)(0x0001), 0x0001, 0x0003, 0xFFFC, 0x0005, 0xFFFA, 0x0007, 0xFFF8)
        );

        Assert.Equal(
            Vector128.Create((uint)(0x0000_0000), 0xFFFF_FFFF, 0x0000_0000, 0x0000_0000),
            Vector128.Create((uint)(0x0000_0001), 0xFFFF_FFFE, 0x0000_0003, 0xFFFF_FFFC)
          ^ Vector128.Create((uint)(0x0000_0001), 0x0000_0001, 0x0000_0003, 0xFFFF_FFFC)
        );

        Assert.Equal(
            Vector128.Create((ulong)(0x0000_0000_0000_0000), 0xFFFF_FFFF_FFFF_FFFF),
            Vector128.Create((ulong)(0x0000_0000_0000_0001), 0xFFFF_FFFF_FFFF_FFFE)
          ^ Vector128.Create((ulong)(0x0000_0000_0000_0001), 0x0000_0000_0000_0001)
        );

        Assert.Equal(
            Vector128.Create((sbyte)(+0), -1, +0, +0, +0, +0, +0, +0, +0, +00, +00, +00, +00, +00, +00, +00),
            Vector128.Create((sbyte)(+1), -2, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16)
          ^ Vector128.Create((sbyte)(+1), +1, +3, -4, +5, -6, +7, -8, +9, -10, +11, -12, +13, -14, +15, -16)
        );

        Assert.Equal(
            Vector128.Create((short)(+0), -1, +0, +0, +0, +0, +0, +0),
            Vector128.Create((short)(+1), -2, +3, -4, +5, -6, +7, -8)
          ^ Vector128.Create((short)(+1), +1, +3, -4, +5, -6, +7, -8)
        );

        Assert.Equal(
            Vector128.Create((int)(+0), -1, +0, +0),
            Vector128.Create((int)(+1), -2, +3, -4)
          ^ Vector128.Create((int)(+1), +1, +3, -4)
        );

        Assert.Equal(
            Vector128.Create((long)(+0), -1),
            Vector128.Create((long)(+1), -2)
          ^ Vector128.Create((long)(+1), +1)
        );

        Assert.Equal(
            Vector128.Create((float)(+0), +float.NegativeInfinity, +0, +0),
            Vector128.Create((float)(+1), -2,                      +3, -4)
          ^ Vector128.Create((float)(+1), +1,                      +3, -4)
        );

        Assert.Equal(
            Vector128.Create((double)(+0), +double.NegativeInfinity),
            Vector128.Create((double)(+1), -2)
          ^ Vector128.Create((double)(+1), +1)
        );
    }
}
