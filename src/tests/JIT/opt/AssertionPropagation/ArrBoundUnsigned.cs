// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Check if conditions like (uint)i < (uint)a.len generate correct "no throw" assertions

using System;
using System.Runtime.CompilerServices;

class ArrBoundUnsigned
{
    // The method names indicate when the array access takes place e.g i_LT_UN_len executes a[i] if (uint)i < (uint)a.len.
    // If the condition is true and the array index is invalid then an IndexOutOfRangeException is expected.

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int i_LT_UN_len(int[] a, int i)
    {
        if ((uint)i < (uint)a.Length)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int len_GT_UN_i(int[] a, int i)
    {
        if ((uint)a.Length > (uint)i)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int i_LE_UN_len(int[] a, int i)
    {
        if ((uint)i <= (uint)a.Length)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int len_GE_UN_i(int[] a, int i)
    {
        if ((uint)a.Length >= (uint)i)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int i_GE_UN_len(int[] a, int i)
    {
        if ((uint)i >= (uint)a.Length)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int len_LE_UN_i(int[] a, int i)
    {
        if ((uint)a.Length <= (uint)i)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int i_GT_UN_len(int[] a, int i)
    {
        if ((uint)i > (uint)a.Length)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int len_LT_UN_i(int[] a, int i)
    {
        if ((uint)a.Length < (uint)i)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int i_LT_UN_len_next_edge(int[] a, int i)
    {
        if ((uint)i < (uint)a.Length)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int len_GT_UN_i_next_edge(int[] a, int i)
    {
        if ((uint)a.Length > (uint)i)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int i_LE_UN_len_next_edge(int[] a, int i)
    {
        if ((uint)i <= (uint)a.Length)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int len_GE_UN_i_next_edge(int[] a, int i)
    {
        if ((uint)a.Length >= (uint)i)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int i_GE_UN_len_next_edge(int[] a, int i)
    {
        if ((uint)i >= (uint)a.Length)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int len_LE_UN_i_next_edge(int[] a, int i)
    {
        if ((uint)a.Length <= (uint)i)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int i_GT_UN_len_next_edge(int[] a, int i)
    {
        if ((uint)i > (uint)a.Length)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static int len_LT_UN_i_next_edge(int[] a, int i)
    {
        if ((uint)a.Length < (uint)i)
            return 9999;
        else
            return a[i];
    }

    // tests for constant input and indexes
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int i_LT_UN_len(int[] a, int i, int lenTest)
    {
        if ((uint)lenTest < (uint)a.Length)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int len_LT_UN_i(int[] a, int i, int lenTest)
    {
        if ((uint)a.Length < (uint)lenTest)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int len_GT_UN_i(int[] a, int i, int lenTest)
    {
        if ((uint)a.Length > (uint)lenTest)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int i_GT_UN_len(int[] a, int i, int lenTest)
    {
        if ((uint)lenTest > (uint)a.Length)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int i_LE_UN_len(int[] a, int i, int lenTest)
    {
        if ((uint)lenTest <= (uint)a.Length)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int len_LE_UN_i(int[] a, int i, int lenTest)
    {
        if ((uint)a.Length <= (uint)lenTest)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int len_GE_UN_i(int[] a, int i, int lenTest)
    {
        if ((uint)a.Length >= (uint)lenTest)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int i_GE_UN_len(int[] a, int i, int lenTest)
    {
        if ((uint)lenTest >= (uint)a.Length)
            return a[i];
        else
            return 9999;
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int i_LT_UN_len_next_edge(int[] a, int i, int lenTest)
    {
        if ((uint)lenTest < (uint)a.Length)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int len_LT_UN_i_next_edge(int[] a, int i, int lenTest)
    {
        if ((uint)a.Length < (uint)lenTest)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int len_GT_UN_i_next_edge(int[] a, int i, int lenTest)
    {
        if ((uint)a.Length > (uint)lenTest)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int i_GT_UN_len_next_edge(int[] a, int i, int lenTest)
    {
        if ((uint)lenTest > (uint)a.Length)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int i_LE_UN_len_next_edge(int[] a, int i, int lenTest)
    {
        if ((uint)lenTest <= (uint)a.Length)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int len_LE_UN_i_next_edge(int[] a, int i, int lenTest)
    {
        if ((uint)a.Length <= (uint)lenTest)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int len_GE_UN_i_next_edge(int[] a, int i, int lenTest)
    {
        if ((uint)a.Length >= (uint)lenTest)
            return 9999;
        else
            return a[i];
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    static int i_GE_UN_len_next_edge(int[] a, int i, int lenTest)
    {
        if ((uint)lenTest >= (uint)a.Length)
            return 9999;
        else
            return a[i];
    }

    static int Main()
    {
        const int Pass = 100;
        const int Fail = -1;

        var empty = new int[] { };
        var arr = new int[] { 1, 42, 3000 };

        // i_LT_UN_len

        if (i_LT_UN_len(arr, 1) != arr[1]) return Fail;
        if (i_LT_UN_len(arr, arr.Length) != 9999) return Fail;
        if (i_LT_UN_len(arr, -1) != 9999) return Fail;
        if (i_LT_UN_len(empty, 1) != 9999) return Fail;

        // len_GT_UN_i

        if (len_GT_UN_i(arr, 1) != arr[1]) return Fail;
        if (len_GT_UN_i(arr, arr.Length) != 9999) return Fail;
        if (len_GT_UN_i(arr, -1) != 9999) return Fail;
        if (len_GT_UN_i(empty, 1) != 9999) return Fail;


        // i_LE_UN_len

        if (i_LE_UN_len(arr, -1) != 9999) return Fail;
        if (i_LE_UN_len(arr, arr.Length + 1) != 9999) return Fail;
        try { i_LE_UN_len(arr, arr.Length); return Fail; } catch (IndexOutOfRangeException) { }
        try { i_LE_UN_len(empty, 0); return Fail; } catch (IndexOutOfRangeException) { }

        // len_GE_UN_i

        if (len_GE_UN_i(arr, -1) != 9999) return Fail;
        if (len_GE_UN_i(arr, arr.Length + 1) != 9999) return Fail;
        try { len_GE_UN_i(arr, arr.Length); return Fail; } catch (IndexOutOfRangeException) { }
        try { len_GE_UN_i(empty, 0); return Fail; } catch (IndexOutOfRangeException) { }


        // i_GE_UN_len

        try { i_GE_UN_len(arr, arr.Length); return Fail; } catch (IndexOutOfRangeException) { }
        try { i_GE_UN_len(arr, arr.Length + 3); return Fail; } catch (IndexOutOfRangeException) { }
        try { i_GE_UN_len(empty, 0); return Fail; } catch (IndexOutOfRangeException) { }

        // len_LE_UN_i

        try { len_LE_UN_i(arr, arr.Length); return Fail; } catch (IndexOutOfRangeException) { }
        try { len_LE_UN_i(arr, arr.Length + 3); return Fail; } catch (IndexOutOfRangeException) { }
        try { len_LE_UN_i(empty, 0); return Fail; } catch (IndexOutOfRangeException) { }


        // i_GT_UN_len

        if (i_GT_UN_len(arr, arr.Length) != 9999) return Fail;
        try { i_GT_UN_len(arr, -1); return Fail; } catch (IndexOutOfRangeException) { }
        try { i_GT_UN_len(arr, arr.Length + 2); return Fail; } catch (IndexOutOfRangeException) { }
        if (i_GT_UN_len(empty, 0) != 9999) return Fail;

        // len_LT_UN_i

        if (len_LT_UN_i(arr, arr.Length) != 9999) return Fail;
        try { len_LT_UN_i(arr, -1); return Fail; } catch (IndexOutOfRangeException) { }
        try { len_LT_UN_i(arr, arr.Length + 2); return Fail; } catch (IndexOutOfRangeException) { }
        if (len_LT_UN_i(empty, 0) != 9999) return Fail;

        // when the array dereference is on the next edge
        // i_LT_UN_len

        if (i_LT_UN_len_next_edge(arr, 1) != 9999) return Fail;
        try { i_LT_UN_len_next_edge(arr, arr.Length); return Fail; } catch (IndexOutOfRangeException) {}
        try { i_LT_UN_len_next_edge(arr, -1); return Fail; } catch (IndexOutOfRangeException) {}
        try { i_LT_UN_len_next_edge(empty, 1); return Fail; } catch (IndexOutOfRangeException) {}

        // len_GT_UN_i

        if (len_GT_UN_i_next_edge(arr, 1) != 9999) return Fail;
        try { len_GT_UN_i_next_edge(arr, arr.Length); return Fail; } catch (IndexOutOfRangeException) {}
        try { len_GT_UN_i_next_edge(arr, -1); return Fail; } catch (IndexOutOfRangeException) {}
        try { len_GT_UN_i_next_edge(empty, 1); return Fail; } catch (IndexOutOfRangeException) {}

        // i_LE_UN_len

        try { i_LE_UN_len_next_edge(arr, -1); return Fail; } catch (IndexOutOfRangeException) {}
        try { i_LE_UN_len_next_edge(arr, arr.Length + 1); return Fail; } catch (IndexOutOfRangeException) {}
        if (i_LE_UN_len_next_edge(arr, arr.Length) != 9999) return Fail;
        if (i_LE_UN_len_next_edge(empty, 0) != 9999) return Fail;

        // len_GE_UN_i

        try { len_GE_UN_i_next_edge(arr, -1); return Fail; } catch (IndexOutOfRangeException) {}
        try { len_GE_UN_i_next_edge(arr, arr.Length + 1); return Fail; } catch (IndexOutOfRangeException) {}
        if (len_GE_UN_i_next_edge(arr, arr.Length) != 9999) return Fail;
        if (len_GE_UN_i_next_edge(empty, 0) != 9999) return Fail;

        // i_GE_UN_len

        if (i_GE_UN_len_next_edge(arr, arr.Length) != 9999) return Fail;
        if (i_GE_UN_len_next_edge(arr, 0) != 1) return Fail;
        if (i_GE_UN_len_next_edge(arr, arr.Length -1) != 3000) return Fail;

        // len_LE_UN_i

        if (len_LE_UN_i_next_edge(arr, arr.Length) != 9999) return Fail;
        if (len_LE_UN_i_next_edge(arr, 0) != 1) return Fail;
        if (len_LE_UN_i_next_edge(arr, arr.Length -1) != 3000) return Fail;

        // i_GT_UN_len

        try { i_GT_UN_len_next_edge(arr, arr.Length); return Fail; } catch(IndexOutOfRangeException) {}
        if (i_GT_UN_len_next_edge(arr, -1) != 9999) return Fail;

        // len_LT_UN_i

        try { len_LT_UN_i_next_edge(arr, arr.Length); return Fail; } catch(IndexOutOfRangeException) {}
        if (len_LT_UN_i_next_edge(arr, -1) != 9999) return Fail;

        // constant index tests (these are inlined)
        if (new Func<int>(() => i_LT_UN_len(arr, 0, 2))() != 1) return Fail;
        if (new Func<int>(() => i_LT_UN_len(arr, 0, 3))() != 9999) return Fail;
        try {new Func<int>(() => i_LT_UN_len(arr, 3, 2))(); return Fail; } catch (IndexOutOfRangeException) {}
        if (new Func<int>(() => i_LT_UN_len(empty, 0, 0))() != 9999) return Fail;

        if (new Func<int>(() => len_LT_UN_i(arr, 3, 0))() != 9999) return Fail;
        try { new Func<int>(() => len_LT_UN_i(arr, 3, 4))(); return Fail; } catch (IndexOutOfRangeException) {}
        if (new Func<int>(() => len_LT_UN_i(arr, 0, -1))() != 1) return Fail;
        try { new Func<int>(() => len_LT_UN_i(arr, 3, -1))(); return Fail; } catch (IndexOutOfRangeException) {}
        if (new Func<int>(() => len_LT_UN_i(empty, 0, 0))() != 9999) return Fail;

        if (new Func<int>(() => len_GT_UN_i(arr, 2, 2))() != 3000) return Fail;
        try { new Func<int>(() => len_GT_UN_i(arr, 3, 2))(); return Fail; } catch (IndexOutOfRangeException) {}
        if (new Func<int>(() => len_GT_UN_i(arr, 0, 3))() != 9999) return Fail;

        if (new Func<int>(() => i_GT_UN_len(arr, 0, 4))() != 1) return Fail;
        if (new Func<int>(() => i_GT_UN_len(arr, 0, 3))() != 9999) return Fail;
        try { new Func<int>(() => i_GT_UN_len(arr, 3, 4))(); return Fail; } catch (IndexOutOfRangeException) {}

        if (new Func<int>(() => i_LE_UN_len(arr, 2, 3))() != 3000) return Fail;
        try { new Func<int>(() => i_LE_UN_len(arr, 3, 3))(); return Fail; } catch (IndexOutOfRangeException) {}
        if (new Func<int>(() => i_LE_UN_len(arr, 3, 4))() != 9999) return Fail;

        if (new Func<int>(() => len_LE_UN_i(arr, 0, 3))() != 1) return Fail;
        if (new Func<int>(() => len_LE_UN_i(arr, 0, 2))() != 9999) return Fail;
        try { new Func<int>(() => len_LE_UN_i(arr, 3, 3))(); return Fail; } catch (IndexOutOfRangeException) {}

        if (new Func<int>(() => len_GE_UN_i(arr, 0, 3))() != 1) return Fail;
        try { new Func<int>(() => len_GE_UN_i(arr, 3, 3))(); return Fail; } catch (IndexOutOfRangeException) {}
        if (new Func<int>(() => len_GE_UN_i(arr, 2, 4))() != 9999) return Fail;

        if (new Func<int>(() => i_GE_UN_len(arr, 2, 3))() != 3000) return Fail;
        if (new Func<int>(() => i_GE_UN_len(arr, 3, 2))() != 9999) return Fail;
        try { new Func<int>(() => i_GE_UN_len(arr, 3, 4))(); return Fail; } catch (IndexOutOfRangeException) {}

        if (new Func<int>(() => i_LT_UN_len_next_edge(arr, 0, 2))() != 9999) return Fail;
        if (new Func<int>(() => i_LT_UN_len_next_edge(arr, 0, 3))() != 1) return Fail;
        try {new Func<int>(() => i_LT_UN_len_next_edge(arr, 3, 3))(); return Fail; } catch (IndexOutOfRangeException) {}
        try {new Func<int>(() => i_LT_UN_len_next_edge(empty, 0, 0))(); return Fail; } catch (IndexOutOfRangeException) {}

        if (new Func<int>(() => len_LT_UN_i_next_edge(arr, 2, 3))() != 3000) return Fail;
        try { new Func<int>(() => len_LT_UN_i_next_edge(arr, 3, 3))(); return Fail; } catch (IndexOutOfRangeException) {}
        try { new Func<int>(() => len_LT_UN_i_next_edge(arr, 3, 0))(); return Fail; } catch (IndexOutOfRangeException) {}

        if (new Func<int>(() => len_GT_UN_i_next_edge(arr, 2, 2))() != 9999) return Fail;
        try { new Func<int>(() => len_GT_UN_i_next_edge(arr, 3, 3))(); return Fail; } catch (IndexOutOfRangeException) {}
        if (new Func<int>(() => len_GT_UN_i_next_edge(arr, 0, 3))() != 1) return Fail;

        if (new Func<int>(() => i_GT_UN_len_next_edge(arr, 0, 3))() != 1) return Fail;
        if (new Func<int>(() => i_GT_UN_len_next_edge(arr, 2, 2))() != 3000) return Fail;
        try { new Func<int>(() => i_GT_UN_len_next_edge(arr, 3, 2))(); return Fail; } catch (IndexOutOfRangeException) {}

        if (new Func<int>(() => i_LE_UN_len_next_edge(arr, 2, 2))() != 9999) return Fail;
        try { new Func<int>(() => i_LE_UN_len_next_edge(arr, 3, 4))(); return Fail; } catch (IndexOutOfRangeException) {}
        if (new Func<int>(() => i_LE_UN_len_next_edge(arr, 0, 4))() != 1) return Fail;

        if (new Func<int>(() => len_LE_UN_i_next_edge(arr, 0, 2))() != 1) return Fail;
        if (new Func<int>(() => len_LE_UN_i_next_edge(arr, 2, 3))() != 9999) return Fail;
        try { new Func<int>(() => len_LE_UN_i_next_edge(arr, 3, 2))(); return Fail; } catch (IndexOutOfRangeException) {}

        if (new Func<int>(() => len_GE_UN_i_next_edge(arr, 2, 2))() != 9999) return Fail;
        try { new Func<int>(() => len_GE_UN_i_next_edge(arr, 3, 4))(); return Fail; } catch (IndexOutOfRangeException) {}
        if (new Func<int>(() => len_GE_UN_i_next_edge(arr, 0, 4))() != 1) return Fail;

        if (new Func<int>(() => i_GE_UN_len_next_edge(arr, 0, 2))() != 1) return Fail;
        if (new Func<int>(() => i_GE_UN_len_next_edge(arr, 2, 3))() != 9999) return Fail;
        try { new Func<int>(() => i_GE_UN_len_next_edge(arr, 3, 2))(); return Fail; } catch (IndexOutOfRangeException) {}


        return Pass;
    }
}
