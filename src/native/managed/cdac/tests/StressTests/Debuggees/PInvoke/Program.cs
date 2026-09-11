// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Exercises P/Invoke transitions with GC references before and after native calls,
/// and pinned GC handles.
/// </summary>
internal static class Program
{
    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PInvokeScenario()
    {
        object before = new object();
        uint tid = GetCurrentThreadId();
        object after = new object();
        GC.KeepAlive(before);
        GC.KeepAlive(after);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PinnedScenario()
    {
        byte[] buffer = new byte[64];
        GCHandle pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            object other = new object();
            GC.KeepAlive(other);
            GC.KeepAlive(buffer);
        }
        finally
        {
            pin.Free();
        }
    }

    struct LargeStruct
    {
        public object A, B, C, D;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void StructWithRefsScenario()
    {
        LargeStruct ls;
        ls.A = new object();
        ls.B = "struct-string";
        ls.C = new int[] { 10, 20 };
        ls.D = new object();
        GC.KeepAlive(ls.A);
        GC.KeepAlive(ls.B);
        GC.KeepAlive(ls.C);
        GC.KeepAlive(ls.D);
    }

    static int Main()
    {
        for (int i = 0; i < 2; i++)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                PInvokeScenario();
            PinnedScenario();
            StructWithRefsScenario();
        }
        return 100;
    }
}
