// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

public static unsafe class MarshalUnalignedStructArrayTest
{
    [Fact]
    public static void TestEntryPoint()
    {
        /*
         * This test validates that the size and offsets of InnerStruct and OuterStruct are as expected.
         * It also demonstrates accessing unaligned data in an array.
         */
        // Validate that both InnerStruct and OuterStruct have the correct size
        Assert.Equal(12, sizeof(InnerStruct));
        Assert.Equal(24, sizeof(OuterStruct));

        // Validate that the fields of InnerStruct are at the expected offsets
        Assert.Equal(0, Marshal.OffsetOf<InnerStruct>("F0").ToInt32());
        Assert.Equal(8, Marshal.OffsetOf<InnerStruct>("F1").ToInt32());

        // Validate that the fields of OuterStruct are at the expected offsets
        Assert.Equal(0, Marshal.OffsetOf<OuterStruct>("F0").ToInt32());
        Assert.Equal(8, Marshal.OffsetOf<OuterStruct>("F1").ToInt32());
        Assert.Equal(20, Marshal.OffsetOf<OuterStruct>("F2").ToInt32());

        // Validate that we are able to access unaligned in an array
        InnerStruct[] arrStructs = new InnerStruct[]
        {
            new InnerStruct(1, 2),
            new InnerStruct(3, 4),
            new InnerStruct(5, 6),
        };

        fixed (InnerStruct* pStruct = &arrStructs[0])
        {
            byte* ptr = (byte*)pStruct;
            ptr += 12;
            Assert.Equal(3, *(long*)ptr);
            Assert.Equal(4, *(int*)(ptr + 8));
        }
        
    }
}

[StructLayout(LayoutKind.Sequential, Size = 12)]
struct InnerStruct
{
    public long F0;
    public uint F1;

    public InnerStruct(long f0, uint f1)
    {
        F0 = f0;
        F1 = f1;
    }
}

[StructLayout(LayoutKind.Sequential, Size = 24)]
struct OuterStruct
{
    public sbyte F0;
    public InnerStruct F1;
    public uint F2;

    public OuterStruct(sbyte f0, InnerStruct f1, uint f2)
    {
        F0 = f0;
        F1 = f1;
        F2 = f2;
    }
}
