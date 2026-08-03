// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The JIT expands multi-dimensional array accesses (NI_Array_Get/Set/Address) into IR. This test
// exercises that expansion for element sizes that do not fit in a byte and for stores of
// struct-typed elements.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MDArrayIntrinsicExpansion
{
    [InlineArray(300)]
    public struct Payload
    {
        private byte _element0;
    }

    public struct SmallStruct
    {
        public int A;
        public object B;
        public double C;
    }

    public struct LargeStruct
    {
        public Payload Data;
        public int Value;
    }

    public struct LargeStructWithRef
    {
        public Payload Data;
        public string Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SetSmall(SmallStruct[,] a, int i, int j, SmallStruct value) => a[i, j] = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SmallStruct GetSmall(SmallStruct[,] a, int i, int j) => a[i, j];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref SmallStruct AddressSmall(SmallStruct[,] a, int i, int j) => ref a[i, j];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SetLarge(LargeStruct[,,] a, int i, int j, int k, LargeStruct value) => a[i, j, k] = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static LargeStruct GetLarge(LargeStruct[,,] a, int i, int j, int k) => a[i, j, k];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref LargeStruct AddressLarge(LargeStruct[,,] a, int i, int j, int k) => ref a[i, j, k];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SetLargeWithRef(LargeStructWithRef[,] a, int i, int j, LargeStructWithRef value) =>
        a[i, j] = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static LargeStructWithRef GetLargeWithRef(LargeStructWithRef[,] a, int i, int j) => a[i, j];

    [Fact]
    public static void SmallStructElements()
    {
        SmallStruct[,] a = new SmallStruct[3, 4];

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                SetSmall(a, i, j, new SmallStruct { A = i * 4 + j, B = (i, j), C = i - j });
            }
        }

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                SmallStruct s = GetSmall(a, i, j);
                Assert.Equal(i * 4 + j, s.A);
                Assert.Equal((i, j), s.B);
                Assert.Equal((double)(i - j), s.C);
                Assert.Equal(s.A, AddressSmall(a, i, j).A);
            }
        }
    }

    [Fact]
    public static void LargeStructElements()
    {
        LargeStruct[,,] a = new LargeStruct[2, 3, 4];

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    LargeStruct s = default;
                    s.Value = i * 100 + j * 10 + k;
                    s.Data[299] = (byte)(i * 100 + j * 10 + k);
                    SetLarge(a, i, j, k, s);
                }
            }
        }

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    LargeStruct s = GetLarge(a, i, j, k);
                    Assert.Equal(i * 100 + j * 10 + k, s.Value);
                    Assert.Equal((byte)(i * 100 + j * 10 + k), s.Data[299]);
                    Assert.Equal(s.Value, AddressLarge(a, i, j, k).Value);
                }
            }
        }
    }

    [Fact]
    public static void LargeStructWithRefElements()
    {
        LargeStructWithRef[,] a = new LargeStructWithRef[2, 3];

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                LargeStructWithRef s = default;
                s.Value = $"{i}-{j}";
                s.Data[299] = (byte)(i * 10 + j);
                SetLargeWithRef(a, i, j, s);
            }
        }

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                LargeStructWithRef s = GetLargeWithRef(a, i, j);
                Assert.Equal($"{i}-{j}", s.Value);
                Assert.Equal((byte)(i * 10 + j), s.Data[299]);
            }
        }
    }

    [Fact]
    public static void OutOfRangeStillThrows()
    {
        LargeStruct[,,] a = new LargeStruct[2, 3, 4];
        Assert.Throws<IndexOutOfRangeException>(() => SetLarge(a, 0, 3, 0, default));
        Assert.Throws<IndexOutOfRangeException>(() => GetLarge(a, 2, 0, 0));

        SmallStruct[,] b = new SmallStruct[2, 3];
        Assert.Throws<IndexOutOfRangeException>(() => SetSmall(b, 0, 3, default));
    }
}
