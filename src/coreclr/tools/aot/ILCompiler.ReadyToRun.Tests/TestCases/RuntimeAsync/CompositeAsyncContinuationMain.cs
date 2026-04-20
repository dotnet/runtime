// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class CompositeAsyncContinuationMain
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallCaptureRefComposite()
    {
        return await AsyncCompositeContLib.CaptureRefComposite();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallCaptureArrayComposite()
    {
        return await AsyncCompositeContLib.CaptureArrayComposite();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> LocalCaptureAcrossAwait()
    {
        object o = new object();
        string s = "local";
        await Task.Yield();
        return s.Length + o.GetHashCode();
    }
}
