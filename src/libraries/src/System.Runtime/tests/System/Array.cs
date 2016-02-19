// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

using Xunit;

public static unsafe class ArrayTests
{
    [Fact]
    public static void TestArrayAsIListOfT()
    {
        string[] sa = { "Hello", "There" };
        string s;
        int idx;

        bool b = (sa is IList<string>);
        Assert.True(b);

        IList<string> ils = sa;
        int len = ils.Count;
        Assert.Equal(len, 2);

        b = ils.Contains(null);
        Assert.False(b);

        b = ils.Contains("There");
        Assert.True(b);

        idx = ils.IndexOf("There");
        Assert.Equal(idx, 1);
        idx = ils.IndexOf(null);
        Assert.Equal(idx, -1);

        string[] sa2 = new string[2];
        ils.CopyTo(sa2, 0);
        Assert.Equal(sa2[0], sa[0]);
        Assert.Equal(sa2[1], sa[1]);

        int[] ia1;
        int[] dst;
        ia1 = new int[] { 1, 2, 3, 4 };
        dst = new int[4];
        ((IList<int>)ia1).CopyTo(dst, 0);
        Assert.Equal(dst, ia1);

        ia1 = new int[] { 1, 2, 3, 4 };
        dst = new int[6];
        ((IList<int>)ia1).CopyTo(dst, 1);
        Assert.Equal(dst, new int[] { 0, 1, 2, 3, 4, 0 });

        b = ils.IsReadOnly;
        Assert.True(b);

        Assert.Throws<NotSupportedException>(() => ils.Add("Hi"));
        Assert.Throws<NotSupportedException>(() => ils.Clear());
        Assert.Throws<NotSupportedException>(() => ils.Remove("There"));
        Assert.Throws<NotSupportedException>(() => ils.RemoveAt(1));
        Assert.Throws<NotSupportedException>(() => ils.Insert(0, "x"));

        IEnumerator<string> e = ils.GetEnumerator();
        b = e.MoveNext();
        Assert.True(b);
        s = e.Current;
        Assert.Equal(s, sa[0]);
        b = e.MoveNext();
        Assert.True(b);
        s = e.Current;
        Assert.Equal(s, sa[1]);
        b = e.MoveNext();
        Assert.False(b);

        s = ils[1];
        Assert.Equal(s, sa[1]);

        ils[1] = "42";
        Assert.Equal(sa[1], "42");
    }

    [Fact]
    public static void TestTrivials()
    {
        // Check a number of the simple APIs on Array for dimensions up to 4.
        Array a = new int[] { 1, 2, 3 };
        Assert.Equal(a.Length, 3);
        Assert.Equal(a.GetLength(0), 3);
        Assert.Throws<IndexOutOfRangeException>(() => a.GetLength(-1));
        Assert.Throws<IndexOutOfRangeException>(() => a.GetLength(1));
        Assert.Equal(a.GetLowerBound(0), 0);
        Assert.Throws<IndexOutOfRangeException>(() => a.GetLowerBound(1));
        Assert.Equal(a.GetUpperBound(0), 2);
        Assert.Throws<IndexOutOfRangeException>(() => a.GetUpperBound(1));
        Assert.Equal(a.Rank, 1);
        IList il = (IList)a;
        Assert.Equal(il.Count, 3);
        Assert.Equal(il.SyncRoot, a);
        Assert.False(il.IsSynchronized);
        Assert.True(il.IsFixedSize);
        Assert.False(il.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => il.Add(2));
        Assert.Throws<NotSupportedException>(() => il.Insert(0, 2));
        Assert.Throws<NotSupportedException>(() => il.Remove(0));
        Assert.Throws<NotSupportedException>(() => il.RemoveAt(0));
        Assert.True(il.Contains(1));
        Assert.False(il.Contains(999));
        Assert.Equal(il.IndexOf(1), 0);
        Assert.Equal(il.IndexOf(999), -1);
        object v = il[0];
        Assert.Equal(v, 1);
        v = il[1];
        Assert.Equal(v, 2);
        v = il[2];
        Assert.Equal(v, 3);
        il[2] = 42;
        Assert.Equal(((int[])a)[2], 42);

        Array a2 = new int[,] { { 1, 2, 3 }, { 4, 5, 6 } };
        Assert.Equal(a2.GetLength(0), 2);
        Assert.Equal(a2.GetLength(1), 3);
        Assert.Throws<IndexOutOfRangeException>(() => a2.GetLength(-1));
        Assert.Throws<IndexOutOfRangeException>(() => a2.GetLength(2));
        Assert.Equal(a2.GetLowerBound(0), 0);
        Assert.Equal(a2.GetLowerBound(1), 0);
        Assert.Throws<IndexOutOfRangeException>(() => a2.GetLowerBound(2));
        Assert.Equal(a2.GetUpperBound(0), 1);
        Assert.Equal(a2.GetUpperBound(1), 2);
        Assert.Throws<IndexOutOfRangeException>(() => a2.GetUpperBound(2));
        Assert.Equal(a2.Rank, 2);
        IList il2 = (IList)a2;
        Assert.Equal(il2.Count, 6);
        Assert.False(il2.IsSynchronized);
        Assert.True(il2.IsFixedSize);
        Assert.False(il2.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => il2.Add(2));
        Assert.Throws<NotSupportedException>(() => il2.Insert(0, 2));
        Assert.Throws<NotSupportedException>(() => il2.Remove(0));
        Assert.Throws<NotSupportedException>(() => il2.RemoveAt(0));
        Assert.Throws<RankException>(() => il2.Contains(1));
        Assert.Throws<RankException>(() => il2.IndexOf(1));

        Array a3 = new int[2, 3, 4];
        int tracer = 0; // makes it easier to confirm row major ordering
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    a3.SetValue(tracer++, i, j, k);
                }
            }
        }
        Assert.Equal(a3.GetLength(0), 2);
        Assert.Equal(a3.GetLength(1), 3);
        Assert.Equal(a3.GetLength(2), 4);
        Assert.Throws<IndexOutOfRangeException>(() => a3.GetLength(-1));
        Assert.Throws<IndexOutOfRangeException>(() => a3.GetLength(3));
        Assert.Equal(a3.GetLowerBound(0), 0);
        Assert.Equal(a3.GetLowerBound(1), 0);
        Assert.Equal(a3.GetLowerBound(2), 0);
        Assert.Throws<IndexOutOfRangeException>(() => a3.GetLowerBound(3));
        Assert.Equal(a3.GetUpperBound(0), 1);
        Assert.Equal(a3.GetUpperBound(1), 2);
        Assert.Equal(a3.GetUpperBound(2), 3);
        Assert.Throws<IndexOutOfRangeException>(() => a3.GetUpperBound(3));
        Assert.Equal(a3.Rank, 3);
        IList il3 = (IList)a3;
        Assert.Equal(il3.Count, 24);
        Assert.False(il3.IsSynchronized);
        Assert.True(il3.IsFixedSize);
        Assert.False(il3.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => il3.Add(2));
        Assert.Throws<NotSupportedException>(() => il3.Insert(0, 2));
        Assert.Throws<NotSupportedException>(() => il3.Remove(0));
        Assert.Throws<NotSupportedException>(() => il3.RemoveAt(0));
        Assert.Throws<RankException>(() => il3.Contains(1));
        Assert.Throws<RankException>(() => il3.IndexOf(0));

        Array a4 = new int[2, 3, 4, 5];

        tracer = 0; // makes it easier to confirm row major ordering
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 4; k++)
                {
                    for (int l = 0; l < 5; l++)
                    {
                        a4.SetValue(tracer++, i, j, k, l);
                    }
                }
            }
        }
        Assert.Equal(a4.GetLength(0), 2);
        Assert.Equal(a4.GetLength(1), 3);
        Assert.Equal(a4.GetLength(2), 4);
        Assert.Equal(a4.GetLength(3), 5);
        Assert.Throws<IndexOutOfRangeException>(() => a4.GetLength(-1));
        Assert.Throws<IndexOutOfRangeException>(() => a4.GetLength(4));
        Assert.Equal(a4.GetLowerBound(0), 0);
        Assert.Equal(a4.GetLowerBound(1), 0);
        Assert.Equal(a4.GetLowerBound(2), 0);
        Assert.Equal(a4.GetLowerBound(3), 0);
        Assert.Throws<IndexOutOfRangeException>(() => a4.GetLowerBound(4));
        Assert.Equal(a4.GetUpperBound(0), 1);
        Assert.Equal(a4.GetUpperBound(1), 2);
        Assert.Equal(a4.GetUpperBound(2), 3);
        Assert.Equal(a4.GetUpperBound(3), 4);
        Assert.Throws<IndexOutOfRangeException>(() => a4.GetUpperBound(4));
        Assert.Equal(a4.Rank, 4);
        IList il4 = (IList)a4;
        Assert.Equal(il4.Count, 120);
        Assert.False(il4.IsSynchronized);
        Assert.True(il4.IsFixedSize);
        Assert.False(il4.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => il4.Add(2));
        Assert.Throws<NotSupportedException>(() => il4.Insert(0, 2));
        Assert.Throws<NotSupportedException>(() => il4.Remove(0));
        Assert.Throws<NotSupportedException>(() => il4.RemoveAt(0));
        Assert.Throws<RankException>(() => il4.Contains(1));
        Assert.Throws<RankException>(() => il4.IndexOf(0));
    }

    public static IEnumerable<object[]> BinarySearchTestData
    {
        get
        {
            int[] intArray = { 1, 3, 6, 6, 8, 10, 12, 16 };
            IComparer intComparer = new IntegerComparer();
            IComparer<int> intGenericComparer = new IntegerComparer();

            string[] strArray = { null, "aa", "bb", "bb", "cc", "dd", "ee" };
            IComparer strComparer = new StringComparer();
            IComparer<string> strGenericComparer = new StringComparer();

            return new[]
            {
               new object[] { intArray, 8, intComparer, intGenericComparer, new Func<int, bool>(i => i == 4) },
               new object[] { intArray, 99, intComparer, intGenericComparer, new Func<int, bool>(i => i == ~(intArray.Length))  },
               new object[] { intArray, 6, intComparer, intGenericComparer, new Func<int, bool>(i => i == 2 || i == 3)  },
               new object[] { strArray, "bb", strComparer, strGenericComparer, new Func<int, bool>(i => i == 2 || i == 3)  },
               new object[] { strArray, null, strComparer, null, new Func<int, bool>(i => i == 0)  },
           };
        }
    }

    [Theory, MemberData(nameof(BinarySearchTestData))]
    public static void TestBinarySearch<T>(T[] array, T value, IComparer comparer, IComparer<T> genericComparer, Func<int, bool> verifier)
    {
        int idx = Array.BinarySearch((Array)array, value, comparer);
        Assert.True(verifier(idx));

        idx = Array.BinarySearch<T>(array, value, genericComparer);
        Assert.True(verifier(idx));

        idx = Array.BinarySearch((Array)array, value);
        Assert.True(verifier(idx));

        idx = Array.BinarySearch<T>(array, value);
        Assert.True(verifier(idx));
    }

    public static IEnumerable<object[]> BinarySearchTestDataInRange
    {
        get
        {
            int[] intArray = { 1, 3, 6, 6, 8, 10, 12, 16 };
            IComparer intComparer = new IntegerComparer();
            IComparer<int> intGenericComparer = new IntegerComparer();

            string[] strArray = { null, "aa", "bb", "bb", "cc", "dd", "ee" };
            IComparer strComparer = new StringComparer();
            IComparer<string> strGenericComparer = new StringComparer();

            return new[]
            {
               new object[] { intArray, 0, 8, 99, intComparer, intGenericComparer, new Func<int, bool>(i => i == ~(intArray.Length))  },
               new object[] { intArray, 0, 8, 6, intComparer, intGenericComparer, new Func<int, bool>(i => i == 2 || i == 3)  },
               new object[] { intArray, 1, 5, 16, intComparer, intGenericComparer, new Func<int, bool>(i => i == -7)  },
               new object[] { strArray, 0, strArray.Length, "bb", strComparer, strGenericComparer, new Func<int, bool>(i => i == 2 || i == 3)  },
               new object[] { strArray, 3, 4, "bb", strComparer, strGenericComparer, new Func<int, bool>(i => i == 3)  },
               new object[] { strArray, 4, 3, "bb", strComparer, strGenericComparer, new Func<int, bool>(i => i == -5)  },
               new object[] { strArray, 4, 0, "bb", strComparer, strGenericComparer, new Func<int, bool>(i => i == -5)  },
               new object[] { strArray, 0, 7, null, strComparer, null, new Func<int, bool>(i => i == 0)  },
           };
        }
    }

    [Theory, MemberData(nameof(BinarySearchTestDataInRange))]
    public static void TestBinarySearchInRange<T>(T[] array, int index, int length, T value, IComparer comparer, IComparer<T> genericComparer, Func<int, bool> verifier)
    {
        int idx = Array.BinarySearch((Array)array, index, length, value, comparer);
        Assert.True(verifier(idx));

        idx = Array.BinarySearch<T>(array, index, length, value, genericComparer);
        Assert.True(verifier(idx));

        idx = Array.BinarySearch((Array)array, index, length, value);
        Assert.True(verifier(idx));

        idx = Array.BinarySearch<T>(array, index, length, value);
        Assert.True(verifier(idx));
    }

    [Fact]
    public static void TestGetAndSetValue()
    {
        int[] idirect = new int[3] { 7, 8, 9 };
        Array a = idirect;

        object seven = a.GetValue(0);
        Assert.Equal(7, seven);
        a.SetValue(41, 0);
        Assert.Equal(41, idirect[0]);

        object eight = a.GetValue(1);
        Assert.Equal(8, eight);
        a.SetValue(42, 1);
        Assert.Equal(42, idirect[1]);

        object nine = a.GetValue(2);
        Assert.Equal(9, nine);
        a.SetValue(43, 2);
        Assert.Equal(43, idirect[2]);

        int[,] idirect2 = new int[,] { { 1, 2, 3 }, { 4, 5, 6 } };
        Array b = idirect2;
        Assert.Equal(1, b.GetValue(0, 0));
        Assert.Equal(6, b.GetValue(1, 2));
        b.SetValue(42, 1, 2);
        Assert.Equal(42, b.GetValue(1, 2));

        int[] nullIndices = null;
        Assert.Throws<ArgumentNullException>(() => b.GetValue(nullIndices));

        int[] tooManyIndices = new int[] { 1, 2, 3, 4 };
        Assert.Throws<ArgumentException>(() => b.GetValue(tooManyIndices));
    }

    [Fact]
    public static void TestClear()
    {
        //----------------------------------------------------------
        // Primitives/valuetypes with no gc-ref pointers
        //----------------------------------------------------------
        int[] idirect;
        idirect = new int[] { 7, 8, 9 };

        Array.Clear((Array)idirect, 0, 3);
        Assert.Equal<int>(idirect[0], 0);
        Assert.Equal<int>(idirect[1], 0);
        Assert.Equal<int>(idirect[2], 0);

        idirect = new int[] { 7, 8, 9 };

        ((IList)idirect).Clear();
        Assert.Equal<int>(idirect[0], 0);
        Assert.Equal<int>(idirect[1], 0);
        Assert.Equal<int>(idirect[2], 0);

        idirect = new int[] { 0x1234567, 0x789abcde, 0x22334455, 0x66778899, 0x11335577, 0x22446688 };
        Array.Clear((Array)idirect, 2, 3);
        Assert.Equal<int>(idirect[0], 0x1234567);
        Assert.Equal<int>(idirect[1], 0x789abcde);
        Assert.Equal<int>(idirect[2], 0);
        Assert.Equal<int>(idirect[3], 0);
        Assert.Equal<int>(idirect[4], 0);
        Assert.Equal<int>(idirect[5], 0x22446688);

        idirect = new int[] { 0x1234567, 0x789abcde, 0x22334455, 0x66778899, 0x11335577, 0x22446688 };
        Array.Clear((Array)idirect, 0, 6);
        Assert.Equal<int>(idirect[0], 0);
        Assert.Equal<int>(idirect[1], 0);
        Assert.Equal<int>(idirect[2], 0);
        Assert.Equal<int>(idirect[3], 0);
        Assert.Equal<int>(idirect[4], 0);
        Assert.Equal<int>(idirect[5], 0);

        idirect = new int[] { 0x1234567, 0x789abcde, 0x22334455, 0x66778899, 0x11335577, 0x22446688 };
        Array.Clear((Array)idirect, 6, 0);
        Assert.Equal<int>(idirect[0], 0x1234567);
        Assert.Equal<int>(idirect[1], 0x789abcde);
        Assert.Equal<int>(idirect[2], 0x22334455);
        Assert.Equal<int>(idirect[3], 0x66778899);
        Assert.Equal<int>(idirect[4], 0x11335577);
        Assert.Equal<int>(idirect[5], 0x22446688);

        idirect = new int[] { 0x1234567, 0x789abcde, 0x22334455, 0x66778899, 0x11335577, 0x22446688 };
        Array.Clear((Array)idirect, 0, 0);
        Assert.Equal<int>(idirect[0], 0x1234567);
        Assert.Equal<int>(idirect[1], 0x789abcde);
        Assert.Equal<int>(idirect[2], 0x22334455);
        Assert.Equal<int>(idirect[3], 0x66778899);
        Assert.Equal<int>(idirect[4], 0x11335577);
        Assert.Equal<int>(idirect[5], 0x22446688);

        //----------------------------------------------------------
        // GC-refs
        //----------------------------------------------------------
        string[] sdirect;

        sdirect = new string[] { "7", "8", "9" };

        Array.Clear((Array)sdirect, 0, 3);
        Assert.Null(sdirect[0]);
        Assert.Null(sdirect[1]);
        Assert.Null(sdirect[2]);

        sdirect = new string[] { "7", "8", "9" };

        ((IList)sdirect).Clear();
        Assert.Null(sdirect[0]);
        Assert.Null(sdirect[1]);
        Assert.Null(sdirect[2]);

        sdirect = new string[] { "0x1234567", "0x789abcde", "0x22334455", "0x66778899", "0x11335577", "0x22446688" };
        Array.Clear((Array)sdirect, 2, 3);
        Assert.Equal<string>(sdirect[0], "0x1234567");
        Assert.Equal<string>(sdirect[1], "0x789abcde");
        Assert.Null(sdirect[2]);
        Assert.Null(sdirect[3]);
        Assert.Null(sdirect[4]);
        Assert.Equal<string>(sdirect[5], "0x22446688");

        sdirect = new string[] { "0x1234567", "0x789abcde", "0x22334455", "0x66778899", "0x11335577", "0x22446688" };
        Array.Clear((Array)sdirect, 0, 6);
        Assert.Null(sdirect[0]);
        Assert.Null(sdirect[1]);
        Assert.Null(sdirect[2]);
        Assert.Null(sdirect[3]);
        Assert.Null(sdirect[4]);
        Assert.Null(sdirect[5]);

        sdirect = new string[] { "0x1234567", "0x789abcde", "0x22334455", "0x66778899", "0x11335577", "0x22446688" };
        Array.Clear((Array)sdirect, 6, 0);
        Assert.Equal<string>(sdirect[0], "0x1234567");
        Assert.Equal<string>(sdirect[1], "0x789abcde");
        Assert.Equal<string>(sdirect[2], "0x22334455");
        Assert.Equal<string>(sdirect[3], "0x66778899");
        Assert.Equal<string>(sdirect[4], "0x11335577");
        Assert.Equal<string>(sdirect[5], "0x22446688");

        sdirect = new string[] { "0x1234567", "0x789abcde", "0x22334455", "0x66778899", "0x11335577", "0x22446688" };
        Array.Clear((Array)sdirect, 0, 0);
        Assert.Equal<string>(sdirect[0], "0x1234567");
        Assert.Equal<string>(sdirect[1], "0x789abcde");
        Assert.Equal<string>(sdirect[2], "0x22334455");
        Assert.Equal<string>(sdirect[3], "0x66778899");
        Assert.Equal<string>(sdirect[4], "0x11335577");
        Assert.Equal<string>(sdirect[5], "0x22446688");

        //----------------------------------------------------------
        // Valuetypes with embedded GC-refs
        //----------------------------------------------------------
        G[] g;
        g = new G[5];
        g[0].x = 7;
        g[0].s = "Hello";
        g[0].z = 8;
        g[1].x = 7;
        g[1].s = "Hello";
        g[1].z = 8;
        g[2].x = 7;
        g[2].s = "Hello";
        g[2].z = 8;
        g[3].x = 7;
        g[3].s = "Hello";
        g[3].z = 8;
        g[4].x = 7;
        g[4].s = "Hello";
        g[4].z = 8;

        Array.Clear((Array)g, 0, 5);
        for (int i = 0; i < g.Length; i++)
        {
            Assert.Equal<int>(g[i].x, 0);
            Assert.Null(g[i].s);
            Assert.Equal<int>(g[i].z, 0);
        }

        g = new G[5];
        g[0].x = 7;
        g[0].s = "Hello";
        g[0].z = 8;
        g[1].x = 7;
        g[1].s = "Hello";
        g[1].z = 8;
        g[2].x = 7;
        g[2].s = "Hello";
        g[2].z = 8;
        g[3].x = 7;
        g[3].s = "Hello";
        g[3].z = 8;
        g[4].x = 7;
        g[4].s = "Hello";
        g[4].z = 8;

        Array.Clear((Array)g, 2, 3);
        Assert.Equal<int>(g[0].x, 7);
        Assert.Equal<string>(g[0].s, "Hello");
        Assert.Equal<int>(g[0].z, 8);
        Assert.Equal<int>(g[1].x, 7);
        Assert.Equal<string>(g[1].s, "Hello");
        Assert.Equal<int>(g[1].z, 8);
        for (int i = 2; i < 2 + 3; i++)
        {
            Assert.Equal<int>(g[i].x, 0);
            Assert.Null(g[i].s);
            Assert.Equal<int>(g[i].z, 0);
        }

        //----------------------------------------------------------
        // Range-checks
        //----------------------------------------------------------
        Assert.Throws<ArgumentNullException>(() => Array.Clear((Array)null, 0, 0));

        Assert.Throws<IndexOutOfRangeException>(() => Array.Clear((Array)idirect, -1, 1));

        Assert.Throws<IndexOutOfRangeException>(() => Array.Clear((Array)idirect, 0, 7));

        Assert.Throws<IndexOutOfRangeException>(() => Array.Clear((Array)idirect, 7, 0));

        Assert.Throws<IndexOutOfRangeException>(() => Array.Clear((Array)idirect, 5, 2));

        Assert.Throws<IndexOutOfRangeException>(() => Array.Clear((Array)idirect, 6, 2));

        Assert.Throws<IndexOutOfRangeException>(() => Array.Clear((Array)idirect, 6, 0x7fffffff));
    }

    [Fact]
    public static void TestCopy_GCRef()
    {
        string[] s;
        string[] d;

        s = new string[] { "Red", "Green", null, "Blue" };
        d = new string[] { "X", "X", "X", "X" };
        Array.Copy(s, 0, d, 0, 4);
        Assert.Equal<string>(d[0], "Red");
        Assert.Equal<string>(d[1], "Green");
        Assert.Null(d[2]);
        Assert.Equal<string>(d[3], "Blue");

        // With reverse overlap
        s = new string[] { "Red", "Green", null, "Blue" };
        Array.Copy(s, 1, s, 2, 2);
        Assert.Equal<string>(s[0], "Red");
        Assert.Equal<string>(s[1], "Green");
        Assert.Equal<string>(s[2], "Green");
        Assert.Null(s[3]);
    }

    [Fact]
    public static void TestCopy_VTToObj()
    {
        // Test the Array.Copy code for value-type arrays => Object[]
        G[] s;
        object[] d;
        s = new G[5];
        d = new object[5];

        s[0].x = 7;
        s[0].s = "Hello0";
        s[0].z = 8;

        s[1].x = 9;
        s[1].s = "Hello1";
        s[1].z = 10;

        s[2].x = 11;
        s[2].s = "Hello2";
        s[2].z = 12;

        s[3].x = 13;
        s[3].s = "Hello3";
        s[3].z = 14;

        s[4].x = 15;
        s[4].s = "Hello4";
        s[4].z = 16;

        Array.Copy(s, 0, d, 0, 5);
        for (int i = 0; i < d.Length; i++)
        {
            Assert.True(d[i] is G);
            G g = (G)(d[i]);
            Assert.Equal<int>(g.x, s[i].x);
            Assert.Equal<string>(g.s, s[i].s);
            Assert.Equal<int>(g.z, s[i].z);
        }
    }

    [Fact]
    public static void TestCopy_VTWithGCRef()
    {
        // Test the Array.Copy code for value-type arrays with no internal GC-refs.

        G[] s;
        G[] d;
        s = new G[5];
        d = new G[5];

        s[0].x = 7;
        s[0].s = "Hello0";
        s[0].z = 8;

        s[1].x = 9;
        s[1].s = "Hello1";
        s[1].z = 10;

        s[2].x = 11;
        s[2].s = "Hello2";
        s[2].z = 12;

        s[3].x = 13;
        s[3].s = "Hello3";
        s[3].z = 14;

        s[4].x = 15;
        s[4].s = "Hello4";
        s[4].z = 16;

        Array.Copy(s, 0, d, 0, 5);
        for (int i = 0; i < d.Length; i++)
        {
            Assert.Equal<int>(d[i].x, s[i].x);
            Assert.Equal<string>(d[i].s, s[i].s);
            Assert.Equal<int>(d[i].z, s[i].z);
        }

        // With overlap
        Array.Copy(s, 1, s, 2, 3);
        Assert.Equal<int>(s[0].x, 7);
        Assert.Equal<string>(s[0].s, "Hello0");
        Assert.Equal<int>(s[0].z, 8);

        Assert.Equal<int>(s[1].x, 9);
        Assert.Equal<string>(s[1].s, "Hello1");
        Assert.Equal<int>(s[1].z, 10);

        Assert.Equal<int>(s[2].x, 9);
        Assert.Equal<string>(s[2].s, "Hello1");
        Assert.Equal<int>(s[2].z, 10);

        Assert.Equal<int>(s[3].x, 11);
        Assert.Equal<string>(s[3].s, "Hello2");
        Assert.Equal<int>(s[3].z, 12);

        Assert.Equal<int>(s[4].x, 13);
        Assert.Equal<string>(s[4].s, "Hello3");
        Assert.Equal<int>(s[4].z, 14);
    }

    [Fact]
    public static void TestCopy_VTNoGCRef()
    {
        // Test the Array.Copy code for value-type arrays with no internal GC-refs.

        int[] s;
        int[] d;
        s = new int[] { 0x12345678, 0x22334455, 0x778899aa };
        d = new int[3];

        // Value-type to value-type array copy.
        Array.Copy(s, 0, d, 0, 3);
        Assert.Equal<int>(d[0], 0x12345678);
        Assert.Equal<int>(d[1], 0x22334455);
        Assert.Equal<int>(d[2], 0x778899aa);

        s = new int[] { 0x12345678, 0x22334455, 0x778899aa, 0x55443322, 0x33445566 };
        // Value-type to value-type array copy (in place, with overlap)
        Array.Copy(s, 3, s, 2, 2);
        Assert.Equal<int>(s[0], 0x12345678);
        Assert.Equal<int>(s[1], 0x22334455);
        Assert.Equal<int>(s[2], 0x55443322);
        Assert.Equal<int>(s[3], 0x33445566);
        Assert.Equal<int>(s[4], 0x33445566);

        s = new int[] { 0x12345678, 0x22334455, 0x778899aa, 0x55443322, 0x33445566 };
        // Value-type to value-type array copy (in place, with reverse overlap)
        Array.Copy(s, 2, s, 3, 2);
        Assert.Equal<int>(s[0], 0x12345678);
        Assert.Equal<int>(s[1], 0x22334455);
        Assert.Equal<int>(s[2], 0x778899aa);
        Assert.Equal<int>(s[3], 0x778899aa);
        Assert.Equal<int>(s[4], 0x55443322);
    }

    [Fact]
    public static void TestConstrainedCopy_GCRef()
    {
        string[] s;
        string[] d;

        s = new string[] { "Red", "Green", null, "Blue" };
        d = new string[] { "X", "X", "X", "X" };
        Array.ConstrainedCopy(s, 0, d, 0, 4);
        Assert.Equal<string>(d[0], "Red");
        Assert.Equal<string>(d[1], "Green");
        Assert.Null(d[2]);
        Assert.Equal<string>(d[3], "Blue");

        // With reverse overlap
        s = new string[] { "Red", "Green", null, "Blue" };
        Array.ConstrainedCopy(s, 1, s, 2, 2);
        Assert.Equal<string>(s[0], "Red");
        Assert.Equal<string>(s[1], "Green");
        Assert.Equal<string>(s[2], "Green");
        Assert.Null(s[3]);
    }

    [Fact]
    public static void TestConstrainedCopy_VTWithGCRef()
    {
        // Test the Array.ConstrainedCopy code for value-type arrays with no internal GC-refs.

        G[] s;
        G[] d;
        s = new G[5];
        d = new G[5];

        s[0].x = 7;
        s[0].s = "Hello0";
        s[0].z = 8;

        s[1].x = 9;
        s[1].s = "Hello1";
        s[1].z = 10;

        s[2].x = 11;
        s[2].s = "Hello2";
        s[2].z = 12;

        s[3].x = 13;
        s[3].s = "Hello3";
        s[3].z = 14;

        s[4].x = 15;
        s[4].s = "Hello4";
        s[4].z = 16;

        Array.ConstrainedCopy(s, 0, d, 0, 5);
        for (int i = 0; i < d.Length; i++)
        {
            Assert.Equal<int>(d[i].x, s[i].x);
            Assert.Equal<string>(d[i].s, s[i].s);
            Assert.Equal<int>(d[i].z, s[i].z);
        }

        // With overlap
        Array.ConstrainedCopy(s, 1, s, 2, 3);
        Assert.Equal<int>(s[0].x, 7);
        Assert.Equal<string>(s[0].s, "Hello0");
        Assert.Equal<int>(s[0].z, 8);

        Assert.Equal<int>(s[1].x, 9);
        Assert.Equal<string>(s[1].s, "Hello1");
        Assert.Equal<int>(s[1].z, 10);

        Assert.Equal<int>(s[2].x, 9);
        Assert.Equal<string>(s[2].s, "Hello1");
        Assert.Equal<int>(s[2].z, 10);

        Assert.Equal<int>(s[3].x, 11);
        Assert.Equal<string>(s[3].s, "Hello2");
        Assert.Equal<int>(s[3].z, 12);

        Assert.Equal<int>(s[4].x, 13);
        Assert.Equal<string>(s[4].s, "Hello3");
        Assert.Equal<int>(s[4].z, 14);
    }

    [Fact]
    public static void TestConstrainedCopy_VTNoGCRef()
    {
        // Test the Array.ConstrainedCopy code for value-type arrays with no internal GC-refs.

        int[] s;
        int[] d;
        s = new int[] { 0x12345678, 0x22334455, 0x778899aa };
        d = new int[3];

        // Value-type to value-type array ConstrainedCopy.
        Array.ConstrainedCopy(s, 0, d, 0, 3);
        Assert.Equal<int>(d[0], 0x12345678);
        Assert.Equal<int>(d[1], 0x22334455);
        Assert.Equal<int>(d[2], 0x778899aa);

        s = new int[] { 0x12345678, 0x22334455, 0x778899aa, 0x55443322, 0x33445566 };
        // Value-type to value-type array ConstrainedCopy (in place, with overlap)
        Array.ConstrainedCopy(s, 3, s, 2, 2);
        Assert.Equal<int>(s[0], 0x12345678);
        Assert.Equal<int>(s[1], 0x22334455);
        Assert.Equal<int>(s[2], 0x55443322);
        Assert.Equal<int>(s[3], 0x33445566);
        Assert.Equal<int>(s[4], 0x33445566);

        s = new int[] { 0x12345678, 0x22334455, 0x778899aa, 0x55443322, 0x33445566 };
        // Value-type to value-type array ConstrainedCopy (in place, with reverse overlap)
        Array.ConstrainedCopy(s, 2, s, 3, 2);
        Assert.Equal<int>(s[0], 0x12345678);
        Assert.Equal<int>(s[1], 0x22334455);
        Assert.Equal<int>(s[2], 0x778899aa);
        Assert.Equal<int>(s[3], 0x778899aa);
        Assert.Equal<int>(s[4], 0x55443322);
    }

    [Fact]
    public static void TestFind()
    {
        int[] ia = { 7, 8, 9 };
        bool b;

        // Exists included here since it's a trivial wrapper around FindIndex
        b = Array.Exists<int>(ia, i => i == 8);
        Assert.True(b);

        b = Array.Exists<int>(ia, i => i == -1);
        Assert.False(b);

        int[] results;
        results = Array.FindAll<int>(ia, i => (i % 2) != 0);
        Assert.Equal(results.Length, 2);
        Assert.True(Array.Exists<int>(results, i => i == 7));
        Assert.True(Array.Exists<int>(results, i => i == 9));

        string[] sa = { "7", "8", "88", "888", "9" };
        string elem;
        elem = Array.Find<String>(sa, s => s.StartsWith("8"));
        Assert.Equal(elem, "8");

        elem = Array.Find<String>(sa, s => s == "X");
        Assert.Null(elem);

        ia = new int[] { 40, 41, 42, 43, 44, 45, 46, 47, 48, 49 };
        int idx;
        idx = Array.FindIndex<int>(ia, i => i >= 43);
        Assert.Equal(idx, 3);

        idx = Array.FindIndex<int>(ia, i => i == 99);
        Assert.Equal(idx, -1);

        idx = Array.FindIndex<int>(ia, 3, i => i == 43);
        Assert.Equal(idx, 3);

        idx = Array.FindIndex<int>(ia, 4, i => i == 43);
        Assert.Equal(idx, -1);

        idx = Array.FindIndex<int>(ia, 1, 3, i => i == 43);
        Assert.Equal(idx, 3);

        idx = Array.FindIndex<int>(ia, 1, 2, i => i == 43);
        Assert.Equal(idx, -1);

        sa = new string[] { "7", "8", "88", "888", "9" };
        elem = Array.FindLast<String>(sa, s => s.StartsWith("8"));
        Assert.Equal(elem, "888");

        elem = Array.FindLast<String>(sa, s => s == "X");
        Assert.Null(elem);

        ia = new int[] { 40, 41, 42, 43, 44, 45, 46, 47, 48, 49 };
        idx = Array.FindLastIndex<int>(ia, i => i >= 43);
        Assert.Equal(idx, 9);

        idx = Array.FindLastIndex<int>(ia, i => i == 99);
        Assert.Equal(idx, -1);

        idx = Array.FindLastIndex<int>(ia, 3, i => i == 43);
        Assert.Equal(idx, 3);

        idx = Array.FindLastIndex<int>(ia, 2, i => i == 43);
        Assert.Equal(idx, -1);

        idx = Array.FindLastIndex<int>(ia, 5, 3, i => i == 43);
        Assert.Equal(idx, 3);

        idx = Array.FindLastIndex<int>(ia, 5, 2, i => i == 43);
        Assert.Equal(idx, -1);
    }

    [Fact]
    public static void TestGetEnumerator()
    {
        int[] i = { 7, 8, 9 };

        IEnumerator ie = i.GetEnumerator();
        bool b;
        object v;

        b = ie.MoveNext();
        Assert.True(b);
        v = ie.Current;
        Assert.Equal(v, 7);

        b = ie.MoveNext();
        Assert.True(b);
        v = ie.Current;
        Assert.Equal(v, 8);

        b = ie.MoveNext();
        Assert.True(b);
        v = ie.Current;
        Assert.Equal(v, 9);

        b = ie.MoveNext();
        Assert.False(b);

        ie.Reset();
        b = ie.MoveNext();
        Assert.True(b);
        v = ie.Current;
        Assert.Equal(v, 7);
    }

    [Fact]
    public static void TestIndexOf()
    {
        Array a;

        a = new int[] { 7, 7, 8, 8, 9, 9 };
        int idx;
        idx = Array.LastIndexOf(a, 8);
        Assert.Equal(idx, 3);

        idx = Array.LastIndexOf(a, 8, 3);
        Assert.Equal(idx, 3);

        idx = Array.IndexOf(a, 8, 4);
        Assert.Equal(idx, -1);

        idx = Array.IndexOf(a, 9, 2, 3);
        Assert.Equal(idx, 4);

        idx = Array.IndexOf(a, 9, 2, 2);
        Assert.Equal(idx, -1);

        int[] ia = (int[])a;
        idx = Array.IndexOf<int>(ia, 8);
        Assert.Equal(idx, 2);

        idx = Array.IndexOf<int>(ia, 8, 3);
        Assert.Equal(idx, 3);

        idx = Array.IndexOf<int>(ia, 8, 4);
        Assert.Equal(idx, -1);

        idx = Array.IndexOf<int>(ia, 9, 2, 3);
        Assert.Equal(idx, 4);

        idx = Array.IndexOf<int>(ia, 9, 2, 2);
        Assert.Equal(idx, -1);

        a = new string[] { null, null, "Hello", "Hello", "Goodbye", "Goodbye", null, null };
        idx = Array.IndexOf(a, null);
        Assert.Equal(idx, 0);
        idx = Array.IndexOf(a, "Hello");
        Assert.Equal(idx, 2);
        idx = Array.IndexOf(a, "Goodbye");
        Assert.Equal(idx, 4);
        idx = Array.IndexOf(a, "Nowhere");
        Assert.Equal(idx, -1);
        idx = Array.IndexOf(a, "Hello", 3);
        Assert.Equal(idx, 3);
        idx = Array.IndexOf(a, "Hello", 4);
        Assert.Equal(idx, -1);
        idx = Array.IndexOf(a, "Goodbye", 2, 3);
        Assert.Equal(idx, 4);
        idx = Array.IndexOf(a, "Goodbye", 2, 2);
        Assert.Equal(idx, -1);

        string[] sa = (string[])a;
        idx = Array.IndexOf<String>(sa, null);
        Assert.Equal(idx, 0);
        idx = Array.IndexOf<String>(sa, "Hello");
        Assert.Equal(idx, 2);
        idx = Array.IndexOf<String>(sa, "Goodbye");
        Assert.Equal(idx, 4);
        idx = Array.IndexOf<String>(sa, "Nowhere");
        Assert.Equal(idx, -1);
        idx = Array.IndexOf<String>(sa, "Hello", 3);
        Assert.Equal(idx, 3);
        idx = Array.IndexOf<String>(sa, "Hello", 4);
        Assert.Equal(idx, -1);
        idx = Array.IndexOf<String>(sa, "Goodbye", 2, 3);
        Assert.Equal(idx, 4);
        idx = Array.IndexOf<String>(sa, "Goodbye", 2, 2);
        Assert.Equal(idx, -1);
    }

    [Fact]
    public static void TestLastIndexOf()
    {
        Array a;

        a = new int[] { 7, 7, 8, 8, 9, 9 };
        int idx;
        idx = Array.LastIndexOf(a, 8);
        Assert.Equal(idx, 3);

        idx = Array.LastIndexOf(a, 8, 3);
        Assert.Equal(idx, 3);

        idx = Array.LastIndexOf(a, 8, 1);
        Assert.Equal(idx, -1);

        idx = Array.LastIndexOf(a, 7, 3, 3);
        Assert.Equal(idx, 1);

        idx = Array.LastIndexOf(a, 7, 3, 2);
        Assert.Equal(idx, -1);

        int[] ia = (int[])a;
        idx = Array.LastIndexOf<int>(ia, 8);
        Assert.Equal(idx, 3);

        idx = Array.LastIndexOf<int>(ia, 8, 3);
        Assert.Equal(idx, 3);

        idx = Array.LastIndexOf<int>(ia, 8, 1);
        Assert.Equal(idx, -1);

        idx = Array.LastIndexOf<int>(ia, 7, 3, 3);
        Assert.Equal(idx, 1);

        idx = Array.LastIndexOf<int>(ia, 7, 3, 2);
        Assert.Equal(idx, -1);

        a = new string[] { null, null, "Hello", "Hello", "Goodbye", "Goodbye", null, null };
        idx = Array.LastIndexOf(a, null);
        Assert.Equal(idx, 7);
        idx = Array.LastIndexOf(a, "Hello");
        Assert.Equal(idx, 3);
        idx = Array.LastIndexOf(a, "Goodbye");
        Assert.Equal(idx, 5);
        idx = Array.LastIndexOf(a, "Nowhere");
        Assert.Equal(idx, -1);
        idx = Array.LastIndexOf(a, "Hello", 3);
        Assert.Equal(idx, 3);
        idx = Array.LastIndexOf(a, "Hello", 2);
        Assert.Equal(idx, 2);
        idx = Array.LastIndexOf(a, "Goodbye", 7, 3);
        Assert.Equal(idx, 5);
        idx = Array.LastIndexOf(a, "Goodbye", 7, 2);
        Assert.Equal(idx, -1);

        string[] sa = (string[])a;
        idx = Array.LastIndexOf<String>(sa, null);
        Assert.Equal(idx, 7);
        idx = Array.LastIndexOf<String>(sa, "Hello");
        Assert.Equal(idx, 3);
        idx = Array.LastIndexOf<String>(sa, "Goodbye");
        Assert.Equal(idx, 5);
        idx = Array.LastIndexOf<String>(sa, "Nowhere");
        Assert.Equal(idx, -1);
        idx = Array.LastIndexOf<String>(sa, "Hello", 3);
        Assert.Equal(idx, 3);
        idx = Array.LastIndexOf<String>(sa, "Hello", 2);
        Assert.Equal(idx, 2);
        idx = Array.LastIndexOf<String>(sa, "Goodbye", 7, 3);
        Assert.Equal(idx, 5);
        idx = Array.LastIndexOf<String>(sa, "Goodbye", 7, 2);
        Assert.Equal(idx, -1);
    }

    [Fact]
    public static void TestIStructural()
    {
        int result;
        int[] ia = new int[] { 2, 3, 4, 5 };
        IStructuralComparable isc = ia;
        result = isc.CompareTo(new int[] { 2, 3, 4, 5 }, new IntegerComparer());
        Assert.Equal(result, 0);

        result = isc.CompareTo(new int[] { 1, 3, 4, 5 }, new IntegerComparer());
        Assert.Equal(result, 1);

        result = isc.CompareTo(new int[] { 2, 3, 4, 6 }, new IntegerComparer());
        Assert.Equal(result, -1);

        bool b;
        IStructuralEquatable ise = ia;
        b = ise.Equals(new int[] { 2, 3, 4, 5 }, new IntegerComparer());
        Assert.True(b);
        b = ise.Equals(new int[] { 2 }, new IntegerComparer());
        Assert.False(b);

        int hash1 = ise.GetHashCode(new IntegerComparer());
        int hash2 = ((IStructuralEquatable)(ia.Clone())).GetHashCode(new IntegerComparer());
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public static void TestResize()
    {
        int[] i;

        i = new int[] { 1, 2, 3, 4, 5 };
        Array.Resize<int>(ref i, 7);
        Assert.Equal(i.Length, 7);
        Assert.Equal(i[0], 1);
        Assert.Equal(i[1], 2);
        Assert.Equal(i[2], 3);
        Assert.Equal(i[3], 4);
        Assert.Equal(i[4], 5);
        Assert.Equal(i[5], default(int));
        Assert.Equal(i[6], default(int));

        i = new int[] { 1, 2, 3, 4, 5 };
        Array.Resize<int>(ref i, 3);
        Assert.Equal(i.Length, 3);
        Assert.Equal(i[0], 1);
        Assert.Equal(i[1], 2);
        Assert.Equal(i[2], 3);

        i = null;
        Array.Resize<int>(ref i, 3);
        Assert.Equal(i.Length, 3);
        Assert.Equal(i[0], default(int));
        Assert.Equal(i[1], default(int));
        Assert.Equal(i[2], default(int));
    }

    [Fact]
    public static void TestReverse()
    {
        int[] i;

        i = new int[] { 1, 2, 3, 4, 5 };
        Array.Reverse((Array)i);
        Assert.Equal(i[0], 5);
        Assert.Equal(i[1], 4);
        Assert.Equal(i[2], 3);
        Assert.Equal(i[3], 2);
        Assert.Equal(i[4], 1);

        i = new int[] { 1, 2, 3, 4, 5 };
        Array.Reverse((Array)i, 2, 3);
        Assert.Equal(i[0], 1);
        Assert.Equal(i[1], 2);
        Assert.Equal(i[2], 5);
        Assert.Equal(i[3], 4);
        Assert.Equal(i[4], 3);

        string[] s;

        s = new string[] { "1", "2", "3", "4", "5" };
        Array.Reverse((Array)s);
        Assert.Equal(s[0], "5");
        Assert.Equal(s[1], "4");
        Assert.Equal(s[2], "3");
        Assert.Equal(s[3], "2");
        Assert.Equal(s[4], "1");

        s = new string[] { "1", "2", "3", "4", "5" };
        Array.Reverse((Array)s, 2, 3);
        Assert.Equal(s[0], "1");
        Assert.Equal(s[1], "2");
        Assert.Equal(s[2], "5");
        Assert.Equal(s[3], "4");
        Assert.Equal(s[4], "3");
    }

    [Fact]
    public static void TestSort()
    {
        IComparer<int> icomparer = new IntegerComparer();

        TestSortHelper<int>(new int[] { }, 0, 0, icomparer);
        TestSortHelper<int>(new int[] { 5 }, 0, 1, icomparer);
        TestSortHelper<int>(new int[] { 5, 2 }, 0, 2, icomparer);

        TestSortHelper<int>(new int[] { 5, 2, 9, 8, 4, 3, 2, 4, 6 }, 0, 9, icomparer);
        TestSortHelper<int>(new int[] { 5, 2, 9, 8, 4, 3, 2, 4, 6 }, 3, 4, icomparer);
        TestSortHelper<int>(new int[] { 5, 2, 9, 8, 4, 3, 2, 4, 6 }, 3, 6, icomparer);

        IComparer<string> scomparer = new StringComparer();
        TestSortHelper<String>(new string[] { }, 0, 0, scomparer);
        TestSortHelper<String>(new string[] { "5" }, 0, 1, scomparer);
        TestSortHelper<String>(new string[] { "5", "2" }, 0, 2, scomparer);

        TestSortHelper<String>(new string[] { "5", "2", null, "8", "4", "3", "2", "4", "6" }, 0, 9, scomparer);
        TestSortHelper<String>(new string[] { "5", "2", null, "8", "4", "3", "2", "4", "6" }, 3, 4, scomparer);
        TestSortHelper<String>(new string[] { "5", "2", null, "8", "4", "3", "2", "4", "6" }, 3, 6, scomparer);
    }

    private static void TestSortHelper<T>(T[] array, int index, int length, IComparer<T> comparer)
    {
        T[] control = SimpleSort<T>(array, index, length, comparer);

        {
            Array spawn1 = (Array)(array.Clone());
            Array.Sort(spawn1, index, length, (IComparer)comparer);
            Assert.True(ArraysAreEqual<T>((T[])spawn1, control, comparer));
        }

        {
            T[] spawn2 = (T[])(array.Clone());
            Array.Sort<T>(spawn2, index, length, comparer);
            Assert.True(ArraysAreEqual<T>((T[])spawn2, control, comparer));
        }
    }

    private static T[] SimpleSort<T>(T[] a, int index, int length, IComparer<T> comparer)
    {
        T[] result = (T[])(a.Clone());
        if (length < 2)
            return result;

        for (int i = index; i < index + length - 1; i++)
        {
            T tmp = result[i];
            for (int j = i + 1; j < index + length; j++)
            {
                if (comparer.Compare(tmp, result[j]) > 0)
                {
                    result[i] = result[j];
                    result[j] = tmp;
                    tmp = result[i];
                }
            }
        }
        return result;
    }

    private static bool ArraysAreEqual<T>(T[] a, T[] b, IComparer<T> comparer)
    {
        // If the same instances were passed, this is unlikely what the test intended.
        Assert.False(Object.ReferenceEquals(a, b));

        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (0 != comparer.Compare(a[i], b[i]))
                return false;
        }
        return true;
    }

    [Fact]
    public static void TestTrueForAll()
    {
        int[] ia;
        bool b;

        ia = new int[] { 1, 2, 3, 4, 5 };

        b = Array.TrueForAll<int>(ia, i => i > 0);
        Assert.True(b);

        b = Array.TrueForAll<int>(ia, i => i == 3);
        Assert.False(b);

        ia = new int[0];
        b = Array.TrueForAll<int>(ia, i => false);
        Assert.True(b);
    }

    [Fact]
    public static void TestArrayCreateInstance()
    {
        {
            int[] lengths = { 5 };
            int[] ia = (int[])Array.CreateInstance(typeof(int), lengths);
            Assert.Equal(ia, new int[5]);
        }

        {
            int[] lengths = { 10 };
            string[] sa = (string[])Array.CreateInstance(typeof(string), lengths);
            Assert.Equal(sa, new string[10]);
        }

        {
            int[] lengths = { 0 };
            string[] sa = (string[])Array.CreateInstance(typeof(string), lengths);
            Assert.Equal(sa, new string[0]);
        }

        {
            int[] lengths = { 5 };
            Assert.Throws<ArgumentNullException>(() => Array.CreateInstance(null, lengths));
        }

        {
            int[] lengths = { 5 };
            Assert.Throws<ArgumentNullException>(() => Array.CreateInstance(typeof(int), null));
        }

        {
            int[] lengths = { };
            Assert.Throws<ArgumentException>(() => Array.CreateInstance(typeof(int), lengths));
        }

        {
            int[] lengths = { -1 };
            Assert.Throws<ArgumentOutOfRangeException>(() => Array.CreateInstance(typeof(int), lengths));
        }

        {
            int[] lengths = { 1, 2 };
            int[,] ia = (int[,])Array.CreateInstance(typeof(int), lengths);
            Assert.Equal(2, ia.Rank);
            ia[0, 1] = 42;
            Assert.Equal(42, ia[0, 1]); // if it quacks like an array...
        }

        {
            int[] lengths = { 1, 2, 3 };
            int[,,] ia = (int[,,])Array.CreateInstance(typeof(int), lengths);
            Assert.Equal(3, ia.Rank);
            ia[0, 1, 2] = 42;
            Assert.Equal(42, ia[0, 1, 2]); // if it quacks like an array...
        }

        {
            int[] lengths = { 1, 2, 3, 4 };
            int[,,,] ia = (int[,,,])Array.CreateInstance(typeof(int), lengths);
            Assert.Equal(4, ia.Rank);
            ia[0, 1, 2, 3] = 42;
            Assert.Equal(42, ia[0, 1, 2, 3]); // if it quacks like an array...
        }

        return;
    }

    [Fact]
    public static void TestArrayCreateInstanceMulti()
    {
        {
            int[] lengths = { 5 };
            int[] lbounds = { 0 };
            int[] ia = (int[])Array.CreateInstance(typeof(int), lengths, lbounds);
            Assert.Equal(ia, new int[5]);
        }

        {
            int[] lengths = { 5 };
            int[] lbounds = { 5 };
            Assert.Throws<ArgumentNullException>(() => Array.CreateInstance(null, lengths, lbounds));
        }

        {
            int[] lengths = { 5 };
            int[] lbounds = { 5 };
            Assert.Throws<ArgumentNullException>(() => Array.CreateInstance(typeof(int), null, lbounds));
        }

        {
            int[] lengths = { 5 };
            int[] lbounds = { 5 };
            Assert.Throws<ArgumentNullException>(() => Array.CreateInstance(typeof(int), lengths, null));
        }

        {
            int[] lengths = { 5 };
            int[] lbounds = { 5, 6 };
            Assert.Throws<ArgumentException>(() => Array.CreateInstance(typeof(int), lengths, lbounds));
        }

        {
            int[] lengths = { };
            int[] lbounds = { };
            Assert.Throws<ArgumentException>(() => Array.CreateInstance(typeof(int), lengths, lbounds));
        }

        {
            int[] lengths = { -1 };
            int[] lbounds = { 5 };
            Assert.Throws<ArgumentOutOfRangeException>(() => Array.CreateInstance(typeof(int), lengths, lbounds));
        }

        {
            int[] lengths = { 7, 8, 9 };
            int[,,] ia = (int[,,])Array.CreateInstance(typeof(int), lengths);
            Assert.Equal(ia.Rank, 3);
            Assert.Equal(ia.GetLength(0), 7);
            Assert.Equal(ia.GetLength(1), 8);
            Assert.Equal(ia.GetLength(2), 9);
            Assert.Equal(ia.GetLowerBound(0), 0);
            Assert.Equal(ia.GetLowerBound(1), 0);
            Assert.Equal(ia.GetLowerBound(2), 0);
        }

        {
            int[] lengths = { 7, 8, 9 };
            int[] lowerBounds = new int[3];
            int[,,] ia = (int[,,])Array.CreateInstance(typeof(int), lengths, lowerBounds);
            Assert.Equal(ia.Rank, 3);
            Assert.Equal(ia.GetLength(0), 7);
            Assert.Equal(ia.GetLength(1), 8);
            Assert.Equal(ia.GetLength(2), 9);
            Assert.Equal(ia.GetLowerBound(0), 0);
            Assert.Equal(ia.GetLowerBound(1), 0);
            Assert.Equal(ia.GetLowerBound(2), 0);
        }

        {
            int[] lengths = { -1, 8, 9 };
            int[] lowerBounds = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => Array.CreateInstance(typeof(int), lengths));
        }

        {
            int[] lengths = { -1, 8, 9 };
            int[] lowerBounds = new int[3];
            Assert.Throws<ArgumentOutOfRangeException>(() => Array.CreateInstance(typeof(int), lengths, lowerBounds));
        }
    }

    private struct G
    {
        public int x;
        public string s;
        public int z;
    }

    private class IntegerComparer : IComparer, IComparer<int>, IEqualityComparer
    {
        public int Compare(object x, object y)
        {
            return Compare((int)x, (int)y);
        }

        public int Compare(int x, int y)
        {
            return x - y;
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return ((int)x) == ((int)y);
        }

        public int GetHashCode(object obj)
        {
            return ((int)obj) >> 2;
        }
    }

    private class StringComparer : IComparer, IComparer<string>
    {
        public int Compare(object x, object y)
        {
            return Compare((string)x, (string)y);
        }

        public int Compare(string x, string y)
        {
            if (x == y)
                return 0;
            if (x == null)
                return -1;
            if (y == null)
                return 1;
            return x.CompareTo(y);
        }
    }

    [Fact]
    public static void TestSetValueCasting()
    {
        int[] indices = { 1 };
        {
            // null -> default(null)
            S[] a = new S[3];
            a[1].X = 0x22222222;
            a.SetValue(null, indices);
            Assert.Equal(a[1].X, 0);
        }

        {
            // T -> Nullable<T>
            Nullable<int>[] a = new Nullable<int>[3];
            a.SetValue(42, indices);
            Nullable<int> ni = a[1];
            Assert.Equal(ni.HasValue, true);
            Assert.Equal(ni.Value, 42);
        }

        {
            // null -> Nullable<T>
            Nullable<int>[] a = new Nullable<int>[3];
            Nullable<int> orig = 42;
            a[1] = orig;
            a.SetValue(null, indices);
            Nullable<int> ni = a[1];
            Assert.Equal(ni.HasValue, false);
        }

        {
            // T -> Nullable<T>  T must be exact
            Nullable<int>[] a = new Nullable<int>[3];
            Assert.Throws<InvalidCastException>(() => a.SetValue((short)42, indices));
        }

        {
            // primitive widening
            int[] a = new int[3];
            a.SetValue((short)42, indices);
            Assert.Equal(a[1], 42);
        }

        {
            // primitive widening must be value-preserving
            int[] a = new int[3];
            Assert.Throws<ArgumentException>(() => a.SetValue((uint)42, indices));
        }

        {
            // widening from enum to primitive
            int[] a = new int[3];
            a.SetValue(E1.MinusTwo, indices);
            Assert.Equal(a[1], -2);
        }

        {
            // unlike most of the other reflection apis, converting or widening a primitive to an enum is NOT allowed.
            E1[] a = new E1[3];
            Assert.Throws<InvalidCastException>(() => a.SetValue((sbyte)1, indices));
        }
    }

    private enum E1 : sbyte
    {
        MinusTwo = -2
    }

    private struct S
    {
        public int X;
    }

    [Fact]
    public static void TestValueTypeToReferenceCopy()
    {
        {
            int[] s = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            object[] d = new IEnumerable<int>[10];

            Assert.Throws<ArrayTypeMismatchException>(() => Array.Copy(s, 0, d, 0, 10));
        }

        {
            int[] s = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            object[] d = new object[10];

            Array.Copy(s, 2, d, 5, 3);

            Assert.Equal(d[0], null);
            Assert.Equal(d[1], null);
            Assert.Equal(d[2], null);
            Assert.Equal(d[3], null);
            Assert.Equal(d[4], null);
            Assert.Equal(d[5], 2);
            Assert.Equal(d[6], 3);
            Assert.Equal(d[7], 4);
            Assert.Equal(d[8], null);
            Assert.Equal(d[9], null);
        }

        {
            int[] s = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            object[] d = new IEquatable<int>[10];

            Array.Copy(s, 2, d, 5, 3);

            Assert.Equal(d[0], null);
            Assert.Equal(d[1], null);
            Assert.Equal(d[2], null);
            Assert.Equal(d[3], null);
            Assert.Equal(d[4], null);
            Assert.Equal(d[5], 2);
            Assert.Equal(d[6], 3);
            Assert.Equal(d[7], 4);
            Assert.Equal(d[8], null);
            Assert.Equal(d[9], null);
        }

        {
            Nullable<int>[] s = { 0, 1, 2, default(Nullable<int>), 4, 5, 6, 7, 8, 9 };
            object[] d = new object[10];

            Array.Copy(s, 2, d, 5, 3);

            Assert.Equal(d[0], null);
            Assert.Equal(d[1], null);
            Assert.Equal(d[2], null);
            Assert.Equal(d[3], null);
            Assert.Equal(d[4], null);
            Assert.Equal(d[5], 2);
            Assert.Equal(d[6], null);
            Assert.Equal(d[7], 4);
            Assert.Equal(d[8], null);
            Assert.Equal(d[9], null);
        }

        return;
    }

    [Fact]
    public static void TestReferenceToValueTypeCopy()
    {
        const int cc = unchecked((int)0xcccccccc);

        {
            object[] s = new string[10];
            int[] d = new int[10];
            for (int i = 0; i < d.Length; i++)
                d[i] = cc;

            Assert.Throws<ArrayTypeMismatchException>(() => Array.Copy(s, 0, d, 0, 10));
        }

        {
            object[] s = new object[10];
            for (int i = 0; i < s.Length; i++)
                s[i] = i;

            int[] d = new int[10];
            for (int i = 0; i < d.Length; i++)
                d[i] = cc;

            Array.Copy(s, 2, d, 5, 3);
            Assert.Equal(d[0], cc);
            Assert.Equal(d[1], cc);
            Assert.Equal(d[2], cc);
            Assert.Equal(d[3], cc);
            Assert.Equal(d[4], cc);
            Assert.Equal(d[5], 2);
            Assert.Equal(d[6], 3);
            Assert.Equal(d[7], 4);
            Assert.Equal(d[8], cc);
            Assert.Equal(d[9], cc);
        }

        {
            object[] s = new IEquatable<int>[10];
            for (int i = 0; i < s.Length; i++)
                s[i] = i;

            int[] d = new int[10];
            for (int i = 0; i < d.Length; i++)
                d[i] = cc;

            Array.Copy(s, 2, d, 5, 3);
            Assert.Equal(d[0], cc);
            Assert.Equal(d[1], cc);
            Assert.Equal(d[2], cc);
            Assert.Equal(d[3], cc);
            Assert.Equal(d[4], cc);
            Assert.Equal(d[5], 2);
            Assert.Equal(d[6], 3);
            Assert.Equal(d[7], 4);
            Assert.Equal(d[8], cc);
            Assert.Equal(d[9], cc);
        }

        {
            object[] s = new IEquatable<int>[10];
            for (int i = 0; i < s.Length; i++)
                s[i] = i;
            s[1] = new NotInt32();
            s[5] = new NotInt32();

            int[] d = new int[10];
            for (int i = 0; i < d.Length; i++)
                d[i] = cc;

            Array.Copy(s, 2, d, 5, 3);
            Assert.Equal(d[0], cc);
            Assert.Equal(d[1], cc);
            Assert.Equal(d[2], cc);
            Assert.Equal(d[3], cc);
            Assert.Equal(d[4], cc);
            Assert.Equal(d[5], 2);
            Assert.Equal(d[6], 3);
            Assert.Equal(d[7], 4);
            Assert.Equal(d[8], cc);
            Assert.Equal(d[9], cc);
        }

        {
            object[] s = new IEquatable<int>[10];
            for (int i = 0; i < s.Length; i++)
                s[i] = i;
            s[4] = new NotInt32();

            int[] d = new int[10];
            for (int i = 0; i < d.Length; i++)
                d[i] = cc;

            Assert.Throws<InvalidCastException>(() => Array.Copy(s, 2, d, 5, 3));
            // Legacy: Note that the cast checks are done during copying, so some elements in the destination
            // array may already have been overwritten.
        }

        {
            object[] s = new IEquatable<int>[10];
            for (int i = 0; i < s.Length; i++)
                s[i] = i;
            s[4] = null;

            int[] d = new int[10];
            for (int i = 0; i < d.Length; i++)
                d[i] = cc;

            Assert.Throws<InvalidCastException>(() => Array.Copy(s, 2, d, 5, 3));
            // Legacy: Note that the cast checks are done during copying, so some elements in the destination
            // array may already have been overwritten.
        }

        {
            object[] s = new object[10];
            for (int i = 0; i < s.Length; i++)
                s[i] = i;
            s[4] = null;

            Nullable<int>[] d = new Nullable<int>[10];
            for (int i = 0; i < d.Length; i++)
                d[i] = cc;

            Array.Copy(s, 2, d, 5, 3);
            Assert.True(d[0].HasValue && d[0].Value == cc);
            Assert.True(d[1].HasValue && d[1].Value == cc);
            Assert.True(d[2].HasValue && d[2].Value == cc);
            Assert.True(d[3].HasValue && d[3].Value == cc);
            Assert.True(d[4].HasValue && d[4].Value == cc);
            Assert.True(d[5].HasValue && d[5].Value == 2);
            Assert.True(d[6].HasValue && d[6].Value == 3);
            Assert.True(!d[7].HasValue);
            Assert.True(d[8].HasValue && d[8].Value == cc);
            Assert.True(d[9].HasValue && d[9].Value == cc);
        }

        return;
    }

    private class NotInt32 : IEquatable<int>
    {
        public bool Equals(int other)
        {
            throw new NotImplementedException();
        }
    }

    private class B1 { }
    private class D1 : B1 { }
    private class B2 { }
    private class D2 : B2 { }
    private interface I1 { }
    private interface I2 { }

    [Fact]
    public static void TestArrayTypeMismatchVsInvalidCast()
    {
        Array s, d;

        s = new B1[10];
        d = new B2[10];
        Assert.Throws<ArrayTypeMismatchException>(() => s.CopyTo(d, 0));

        s = new B1[10];
        d = new D1[10];
        s.CopyTo(d, 0);

        s = new D1[10];
        d = new B1[10];
        s.CopyTo(d, 0);

        s = new I1[10];
        d = new B1[10];
        s.CopyTo(d, 0);

        s = new B1[10];
        d = new I1[10];
        s.CopyTo(d, 0);

        s = new B1[] { new B1() };
        d = new I1[1];
        Assert.Throws<InvalidCastException>(() => s.CopyTo(d, 0));

        return;
    }

    [Fact]
    public static void TestArrayConstructionMultidimArrays()
    {
        // This C# initialization syntax generates some peculiar looking IL.
        // Initializations of this form are handled specially on Desktop and
        // in .NET Native by UTC.
        int[,,,] arr = new int[,,,] { { { { 1, 2, 3 }, { 1, 2, 3 } }, { { 1, 2, 3 }, { 1, 2, 3 } } },
                                        { { { 1, 2, 3 }, { 1, 2, 3 } }, { { 1, 2, 3 }, { 1, 2, 3 } } } };
        Assert.NotNull(arr);
        Assert.Equal(arr.GetValue(0, 0, 0, 0), 1);
        Assert.Equal(arr.GetValue(0, 0, 0, 1), 2);
        Assert.Equal(arr.GetValue(0, 0, 0, 2), 3);
    }
}
