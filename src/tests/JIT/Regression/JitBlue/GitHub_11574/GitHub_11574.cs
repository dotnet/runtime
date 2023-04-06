// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    static byte[] s_arr2;
    static byte[] s_arr3;

    static void Init()
    {
        s_arr2 = new byte[] { 0x11, 0x12, 0x13 };
        s_arr3 = new byte[] { 0x21, 0x22, 0x33 };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Check(int actual, int added, int expected, int rv)
    {
        return (actual == expected) ? rv : 0;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Init();

        byte[] arr1 = new byte[] { 2 };
        byte[] arr2 = s_arr2;
        byte[] arr3 = s_arr3;

        int rv = 100;
        int len = arr1.Length + arr2.Length + arr3.Length;
        int cur = 0;
        rv = Check(cur, 0, 0, rv);
        cur += arr1.Length;
        rv = Check(cur, arr1.Length, 1, rv);
        cur += arr2.Length;
        rv = Check(cur, arr2.Length, 4, rv);
        cur += arr3.Length;
        rv = Check(cur, arr3.Length, 7, rv);
        return Check(cur, 0, len, rv);
    }
}
