// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

public static class IntPtrTests
{
    private static unsafe bool s_is32Bits = sizeof(void*) == 4; // Skip IntPtr tests on 32-bit platforms

    [Fact]
    public static void TestZero()
    {
        if (s_is32Bits)
            return;

        VerifyPointer(IntPtr.Zero, 0);
    }

    [Fact]
    public static void TestCtor_Int()
    {
        if (s_is32Bits)
            return;

        int i = 42;
        VerifyPointer(new IntPtr(i), i);
        VerifyPointer((IntPtr)i, i);

        i = -1;
        VerifyPointer(new IntPtr(i), i);
        VerifyPointer((IntPtr)i, i);
    }

    [Fact]
    public static void TestCtor_Long()
    {
        if (s_is32Bits)
            return;

        long l = 0x0fffffffffffffff;
        VerifyPointer(new IntPtr(l), l);
        VerifyPointer((IntPtr)l, l);
    }

    [Fact]
    public static unsafe void TestCtor_VoidPointer_ToPointer()
    {
        if (s_is32Bits)
            return;

        void* pv = new IntPtr(42).ToPointer();
        VerifyPointer(new IntPtr(pv), 42);
        VerifyPointer((IntPtr)pv, 42);
    }

    [Fact]
    public static unsafe void TestSize()
    {
        if (s_is32Bits)
            return;

        Assert.Equal(sizeof(void*), IntPtr.Size);
    }

    public static IEnumerable<object[]> Add_TestData()
    {
        yield return new object[] { new IntPtr(42), 6, (long)48 };
        yield return new object[] { new IntPtr(40), 0, (long)40 };
        yield return new object[] { new IntPtr(38), -2, (long)36 };

        yield return new object[] { new IntPtr(0x7fffffffffffffff), 5, unchecked((long)0x8000000000000004) }; /// Add should not throw an OverflowException
    }

    [Theory]
    [MemberData(nameof(Add_TestData))]
    public static void TestAdd(IntPtr ptr, int offset, long expected)
    {
        if (s_is32Bits)
            return;

        IntPtr p1 = IntPtr.Add(ptr, offset);
        VerifyPointer(p1, expected);

        IntPtr p2 = ptr + offset;
        VerifyPointer(p2, expected);

        IntPtr p3 = ptr;
        p3 += offset;
        VerifyPointer(p3, expected);
    }

    public static IEnumerable<object[]> Subtract_TestData()
    {
        yield return new object[] { new IntPtr(42), 6, (long)36 };
        yield return new object[] { new IntPtr(40), 0, (long)40 };
        yield return new object[] { new IntPtr(38), -2, (long)40 };
    }

    [Theory]
    [MemberData(nameof(Subtract_TestData))]
    public static void TestSubtract(IntPtr ptr, int offset, long expected)
    {
        if (s_is32Bits)
            return;

        IntPtr p1 = IntPtr.Subtract(ptr, offset);
        VerifyPointer(p1, expected);

        IntPtr p2 = ptr - offset;
        VerifyPointer(p2, expected);

        IntPtr p3 = ptr;
        p3 -= offset;
        VerifyPointer(p3, expected);
    }

    public static IEnumerable<object[]> Equals_TestData()
    {
        yield return new object[] { new IntPtr(42), new IntPtr(42), true };
        yield return new object[] { new IntPtr(42), new IntPtr(43), false };
        yield return new object[] { new IntPtr(42), 42, false };
        yield return new object[] { new IntPtr(42), null, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public static void TestEquals(IntPtr ptr1, object obj, bool expected)
    {
        if (s_is32Bits)
            return;

        if (obj is IntPtr)
        {
            IntPtr ptr2 = (IntPtr)obj;
            Assert.Equal(expected, ptr1 == ptr2);
            Assert.Equal(!expected, ptr1 != ptr2);
            Assert.Equal(expected, ptr1.GetHashCode().Equals(ptr2.GetHashCode()));
        }
        Assert.Equal(expected, ptr1.Equals(obj));
        Assert.Equal(ptr1.GetHashCode(), ptr1.GetHashCode());
    }

    [Fact]
    public static unsafe void TestImplicitCast()
    {
        if (s_is32Bits)
            return;

        var ptr = new IntPtr(42);

        uint i = (uint)ptr;
        Assert.Equal(42u, i);
        Assert.Equal(ptr, (IntPtr)i);

        ulong l = (ulong)ptr;
        Assert.Equal(42u, l);
        Assert.Equal(ptr, (IntPtr)l);

        void* v = (void*)ptr;
        Assert.Equal(ptr, (IntPtr)v);

        ptr = new IntPtr(0x7fffffffffffffff);
        Assert.Throws<OverflowException>(() => (int)ptr);
    }

    private static void VerifyPointer(IntPtr ptr, long expected)
    {
        Assert.Equal(expected, ptr.ToInt64());

        int expected32 = (int)expected;
        if (expected32 != expected)
        {
            Assert.Throws<OverflowException>(() => ptr.ToInt32());
            return;
        }

        int i = ptr.ToInt32();
        Assert.Equal(expected32, ptr.ToInt32());

        Assert.Equal(expected.ToString(), ptr.ToString());
        Assert.Equal(expected.ToString("x"), ptr.ToString("x"));

        Assert.Equal(ptr, new IntPtr(expected));
        Assert.True(ptr == new IntPtr(expected));
        Assert.False(ptr != new IntPtr(expected));

        Assert.NotEqual(ptr, new IntPtr(expected + 1));
        Assert.False(ptr == new IntPtr(expected + 1));
        Assert.True(ptr != new IntPtr(expected + 1));
    }
}
