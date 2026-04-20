// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncCompositeContLib
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CaptureRefComposite()
    {
        object o = new object();
        string s = "composite_ref";
        await Task.Yield();
        return s + o.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CaptureArrayComposite()
    {
        int[] arr = new int[] { 5, 10, 15 };
        string label = "total";
        await Task.Yield();
        return arr[0] + arr[1] + label.Length;
    }
}
