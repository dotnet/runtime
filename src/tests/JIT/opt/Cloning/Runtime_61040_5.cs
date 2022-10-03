// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

unsafe class Runtime_61040_5
{
    private const int ArrLen = 10;

    public static int Main()
    {
        int[] arr = new int[ArrLen];

        try
        {
            ProblemWithBlkAsg(arr, ArrLen);
            return 101;
        }
        catch (IndexOutOfRangeException) { }

        try
        {
            ProblemWithLclFldAsg(arr, ArrLen);
            return 102;
        }
        catch (IndexOutOfRangeException) { }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ProblemWithBlkAsg(int[] arr, int idx)
    {
        for (int i = 0; i < ArrLen; i++)
        {
            Unsafe.InitBlock(&i, (byte)idx, 1);
            arr[i] = 0;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ProblemWithLclFldAsg(int[] arr, int idx)
    {
        for (int i = 0; i < ArrLen; i++)
        {
            *(byte*)&i = (byte)idx;
            arr[i] = 0;
        }
    }
}
