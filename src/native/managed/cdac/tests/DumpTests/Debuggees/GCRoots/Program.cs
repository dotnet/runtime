// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.InteropServices;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the Object and GC contracts.
/// Pins objects, creates GC handles, then crashes.
/// </summary>
internal static class Program
{
    public const int PinnedObjectCount = 5;
    public const string TestStringValue = "cDAC-GCRoots-test-string";

    private static void Main()
    {
        // Allocate objects of various types
        string testString = TestStringValue;
        byte[] byteArray = new byte[1024];
        object boxedInt = 42;

        // Create pinned handles
        GCHandle[] pinnedHandles = new GCHandle[PinnedObjectCount];
        byte[][] pinnedArrays = new byte[PinnedObjectCount][];

        for (int i = 0; i < PinnedObjectCount; i++)
        {
            pinnedArrays[i] = new byte[64];
            pinnedHandles[i] = GCHandle.Alloc(pinnedArrays[i], GCHandleType.Pinned);
        }

        // Create weak and strong handles
        var weakRef = new WeakReference(new object());
        var strongHandle = GCHandle.Alloc(testString, GCHandleType.Normal);
        var weakLongHandle = GCHandle.Alloc(new object(), GCHandleType.WeakTrackResurrection);

        // Create dependent handle
        object dependentTarget = new object();
        object dependentValue = new byte[16];
        DependentHandle dependentHandle = new(dependentTarget, dependentValue);

        // Keep references alive
        GC.KeepAlive(testString);
        GC.KeepAlive(byteArray);
        GC.KeepAlive(boxedInt);
        GC.KeepAlive(pinnedHandles);
        GC.KeepAlive(pinnedArrays);
        GC.KeepAlive(weakRef);
        GC.KeepAlive(strongHandle);
        GC.KeepAlive(weakLongHandle);
        GC.KeepAlive(dependentTarget);
        GC.KeepAlive(dependentValue);
        GC.KeepAlive(dependentHandle);
        Environment.FailFast("cDAC dump test: GCRoots debuggee intentional crash");
    }
}
