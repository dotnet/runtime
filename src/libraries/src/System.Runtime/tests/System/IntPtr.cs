// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

public static unsafe class IntPtrTests
{
    [Fact]
    public static unsafe void TestBasics()
    {
        if (sizeof(void*) == 4)
        {
            // Skip IntPtr tests on 32-bit platforms
            return;
        }

        IntPtr p;
        int i;
        long l;

        int size = IntPtr.Size;
        Assert.Equal(size, sizeof(void*));

        TestPointer(IntPtr.Zero, 0);

        i = 42;
        TestPointer(new IntPtr(i), i);
        TestPointer((IntPtr)i, i);

        i = 42;
        TestPointer(new IntPtr(i), i);

        i = -1;
        TestPointer(new IntPtr(i), i);

        l = 0x0fffffffffffffff;
        TestPointer(new IntPtr(l), l);
        TestPointer((IntPtr)l, l);

        void* pv = new IntPtr(42).ToPointer();
        TestPointer(new IntPtr(pv), 42);
        TestPointer((IntPtr)pv, 42);

        p = IntPtr.Add(new IntPtr(42), 5);
        TestPointer(p, 42 + 5);

        // Add is spected NOT to generate an OverflowException
        p = IntPtr.Add(new IntPtr(0x7fffffffffffffff), 5);
        unchecked
        {
            TestPointer(p, (long)0x8000000000000004);
        }

        p = IntPtr.Subtract(new IntPtr(42), 5);
        TestPointer(p, 42 - 5);

        bool b;
        p = new IntPtr(42);
        b = p.Equals(null);
        Assert.False(b);
        b = p.Equals((Object)42);
        Assert.False(b);
        b = p.Equals((Object)(new IntPtr(42)));
        Assert.True(b);

        int h = p.GetHashCode();
        int h2 = p.GetHashCode();
        Assert.Equal(h, h2);

        p = new IntPtr(42);
        i = (int)p;
        Assert.Equal(i, 42);
        l = (long)p;
        Assert.Equal(l, 42);
        IntPtr p2;
        p2 = (IntPtr)i;
        Assert.Equal(p, p2);
        p2 = (IntPtr)l;
        Assert.Equal(p, p2);
        p2 = (IntPtr)(p.ToPointer());
        Assert.Equal(p, p2);
        p2 = new IntPtr(40) + 2;
        Assert.Equal(p, p2);
        p2 = new IntPtr(44) - 2;
        Assert.Equal(p, p2);

        p = new IntPtr(0x7fffffffffffffff);
        Assert.Throws<OverflowException>(() => { i = (int)p; });
    }

    private static void TestPointer(IntPtr p, long expected)
    {
        long l = p.ToInt64();
        Assert.Equal(l, expected);

        int expected32 = (int)expected;
        if (expected32 != expected)
        {
            Assert.Throws<OverflowException>(() => { int i = p.ToInt32(); });
            return;
        }

        {
            int i = p.ToInt32();
            Assert.Equal(i, expected32);
        }

        String s = p.ToString();
        String sExpected = expected.ToString();
        Assert.Equal(s, sExpected);

        s = p.ToString("x");
        sExpected = expected.ToString("x");
        Assert.Equal(s, sExpected);

        bool b;

        b = (p == new IntPtr(expected));
        Assert.True(b);

        b = (p == new IntPtr(expected + 1));
        Assert.False(b);

        b = (p != new IntPtr(expected));
        Assert.False(b);

        b = (p != new IntPtr(expected + 1));
        Assert.True(b);
    }
}

