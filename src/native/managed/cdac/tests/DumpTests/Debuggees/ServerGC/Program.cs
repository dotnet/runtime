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
    public const string TestStringValue = "cDAC-ServerGC-test-string";
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

        // Create weak and strong handles
        var weakRef = new WeakReference(new object());
        var strongHandle = GCHandle.Alloc(TestStringValue, GCHandleType.Normal);
        var weakLongHandle = GCHandle.Alloc(new object(), GCHandleType.WeakTrackResurrection);

        // Create dependent handle
        object dependentTarget = new object();
        object dependentValue = new byte[16];
        DependentHandle dependentHandle = new(dependentTarget, dependentValue);


        GC.KeepAlive(roots);
        GC.KeepAlive(pinnedHandles);
        GC.KeepAlive(weakRef);
        GC.KeepAlive(strongHandle);
        GC.KeepAlive(weakLongHandle);
        GC.KeepAlive(dependentTarget);
        GC.KeepAlive(dependentValue);
        GC.KeepAlive(dependentHandle);

        Environment.FailFast("cDAC dump test: ServerGC debuggee intentional crash");
    }
}
