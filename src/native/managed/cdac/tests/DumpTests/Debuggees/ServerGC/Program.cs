// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.InteropServices;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the GC contract in server GC mode.
/// Allocates objects across heaps, pins some, then crashes.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        // Verify server GC is enabled
        if (!GCSettings.IsServerGC)
        {
            Console.Error.WriteLine("ERROR: Server GC is not enabled.");
            Environment.Exit(1);
        }

        // Allocate objects to populate multiple heaps
        object[] roots = new object[100];
        for (int i = 0; i < roots.Length; i++)
        {
            roots[i] = new byte[1024 * (i + 1)];
        }

        // Create pinned handles
        GCHandle[] pinnedHandles = new GCHandle[10];
        for (int i = 0; i < pinnedHandles.Length; i++)
        {
            pinnedHandles[i] = GCHandle.Alloc(new byte[256], GCHandleType.Pinned);
        }

        GC.KeepAlive(roots);
        GC.KeepAlive(pinnedHandles);

        Environment.FailFast("cDAC dump test: ServerGC debuggee intentional crash");
    }
}
