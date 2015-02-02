// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

public static unsafe class ArraySegmentTests
{
    [Fact]
    public static void TestCtors()
    {
        int i;
        int[] ia2;

        int[] ia = { 7, 8, 9, 10, 11 };
        ArraySegment<int> a;

        a = new ArraySegment<int>(ia);
        ia2 = a.Array;
        Assert.True(Object.ReferenceEquals(ia2, ia));
        i = a.Offset;
        Assert.Equal(i, 0);
        i = a.Count;
        Assert.Equal(i, 5);

        a = new ArraySegment<int>();
        ia2 = a.Array;
        Assert.Null(ia2);
        i = a.Offset;
        Assert.Equal(i, 0);
        i = a.Count;
        Assert.Equal(i, 0);
    }

    [Fact]
    public static void TestBasics()
    {
        int[] ia = { 7, 8, 9, 10, 11 };
        ArraySegment<int> a = new ArraySegment<int>(ia, 2, 3);
        bool b;
        int c;

        int[] ia2 = a.Array;
        Assert.True(Object.ReferenceEquals(ia, ia2));

        c = a.Offset;
        Assert.Equal(c, 2);

        c = a.Count;
        Assert.Equal(c, 3);

        b = a.Equals(a);
        Assert.True(b);

        ArraySegment<int> a2 = new ArraySegment<int>(ia, 2, 3);
        b = a.Equals(a2);
        Assert.True(b);

        int[] ia3 = (int[])(ia.Clone());
        ArraySegment<int> a3 = new ArraySegment<int>(ia3, 2, 3);
        b = a.Equals(a3);
        Assert.False(b);

        Object o;
        o = null;
        b = a.Equals(null);
        Assert.False(b);

        o = a2;
        b = a.Equals(o);
        Assert.True(b);

        int h1 = a.GetHashCode();
        int h2 = a.GetHashCode();
        int h3 = a2.GetHashCode();
        Assert.Equal(h1, h2);
        Assert.Equal(h1, h3);
    }

    [Fact]
    public static void TestIList()
    {
        int[] ia = { 7, 8, 9, 10, 11, 12, 13 };
        ArraySegment<int> a = new ArraySegment<int>(ia, 2, 3);
        IList<int> il = a;
        bool b;
        int i;

        b = il.IsReadOnly;
        Assert.True(b);

        i = il[1];
        Assert.Equal(i, 10);

        il[1] = 99;
        Assert.Equal(ia[3], 99);

        Assert.Throws<NotSupportedException>(() => il.Add(2));
        Assert.Throws<NotSupportedException>(() => il.Clear());

        b = il.Contains(11);
        Assert.True(b);

        b = il.Contains(8788);
        Assert.False(b);

        int[] dst = new int[10];
        il.CopyTo(dst, 5);
        Assert.Equal(dst[0], 0);
        Assert.Equal(dst[1], 0);
        Assert.Equal(dst[2], 0);
        Assert.Equal(dst[3], 0);
        Assert.Equal(dst[4], 0);
        Assert.Equal(dst[5], 9);
        Assert.Equal(dst[6], 99);
        Assert.Equal(dst[7], 11);
        Assert.Equal(dst[8], 0);
        Assert.Equal(dst[9], 0);

        Assert.Throws<NotSupportedException>(() => il.Remove(2));
        Assert.Throws<NotSupportedException>(() => il.RemoveAt(2));

        int idx;
        idx = il.IndexOf(99);
        Assert.Equal(idx, 1);

        idx = il.IndexOf(99999);
        Assert.Equal(idx, -1);

        IEnumerator<int> e = il.GetEnumerator();
        b = e.MoveNext();
        Assert.True(b);
        i = e.Current;
        Assert.Equal(i, 9);
        b = e.MoveNext();
        Assert.True(b);
        i = e.Current;
        Assert.Equal(i, 99);
        b = e.MoveNext();
        Assert.True(b);
        i = e.Current;
        Assert.Equal(i, 11);
        b = e.MoveNext();
        Assert.False(b);
    }

    [Fact]
    public static void TestCopyTo()
    {
        {
            String[] src;
            IList<String> seg;

            src = new String[] { "0", "1", "2", "3", "4" };
            seg = new ArraySegment<String>(src, 1, 3);
            seg.CopyTo(src, 2);
            Assert.Equal(src, new String[] { "0", "1", "1", "2", "3" });

            src = new String[] { "0", "1", "2", "3", "4" };
            seg = new ArraySegment<String>(src, 1, 3);
            seg.CopyTo(src, 0);
            Assert.Equal(src, new String[] { "1", "2", "3", "3", "4" });
        }

        {
            int[] src;
            IList<int> seg;

            src = new int[] { 0, 1, 2, 3, 4 };
            seg = new ArraySegment<int>(src, 1, 3);
            seg.CopyTo(src, 2);
            Assert.Equal(src, new int[] { 0, 1, 1, 2, 3 });

            src = new int[] { 0, 1, 2, 3, 4 };
            seg = new ArraySegment<int>(src, 1, 3);
            seg.CopyTo(src, 0);
            Assert.Equal(src, new int[] { 1, 2, 3, 3, 4 });
        }
    }
}

