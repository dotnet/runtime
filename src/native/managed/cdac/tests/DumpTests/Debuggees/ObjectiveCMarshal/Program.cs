// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ObjectiveC;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the ObjectiveCMarshal contract's tagged memory APIs.
/// Creates an Objective-C tracked reference object with tagged memory allocated and keeps it
/// alive via a strong GC handle so dump tests can find it. This debuggee is macOS-only, as
/// tagged memory support requires FEATURE_OBJCMARSHAL.
/// </summary>
internal static partial class Program
{
    [ObjectiveCTrackedTypeAttribute]
    private sealed class TrackedObject
    {
#pragma warning disable CA1821 // Intentionally empty — the runtime requires a finalizer for IsTrackedReferenceWithFinalizer
        ~TrackedObject() { }
#pragma warning restore CA1821
    }

    private static unsafe void Main()
    {
        if (OperatingSystem.IsMacOS())
        {
            SetupAndCrash();
        }

        Environment.FailFast("cDAC dump test: ObjectiveCMarshal debuggee intentional crash");
    }

    [System.Runtime.Versioning.SupportedOSPlatform("macos")]
    private static unsafe void SetupAndCrash()
    {
        var obj = new TrackedObject();

        // Initialize the ObjectiveC marshal runtime
        ObjectiveCMarshal.Initialize(
            &BeginEndCallback,
            &IsReferencedCallback,
            &TrackedObjectEnteredFinalization,
            OnUnhandledExceptionPropagationHandler);

        // Create a reference tracking handle — this allocates tagged memory
        GCHandle handle = ObjectiveCMarshal.CreateReferenceTrackingHandle(obj, out _);

        // Keep the object alive via a strong handle so the dump test can find it
        GCHandle strongHandle = GCHandle.Alloc(obj, GCHandleType.Normal);

        GC.KeepAlive(handle);
        GC.KeepAlive(strongHandle);
        GC.KeepAlive(obj);

        Environment.FailFast("cDAC dump test: ObjectiveCMarshal debuggee intentional crash");
    }

    [System.Runtime.InteropServices.UnmanagedCallersOnly]
    private static void BeginEndCallback() { }

    [System.Runtime.InteropServices.UnmanagedCallersOnly]
    private static int IsReferencedCallback(IntPtr ptr) => 1;

    [System.Runtime.InteropServices.UnmanagedCallersOnly]
    private static void TrackedObjectEnteredFinalization(IntPtr ptr) { }

    private static unsafe delegate* unmanaged<IntPtr, void> OnUnhandledExceptionPropagationHandler(
        Exception e,
        System.RuntimeMethodHandle lastMethod,
        out IntPtr context)
    {
        context = IntPtr.Zero;
        return null;
    }
}
