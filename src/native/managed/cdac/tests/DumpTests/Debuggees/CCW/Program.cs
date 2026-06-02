// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the BuiltInCOM contract.
/// On Windows: creates managed objects with COM callable wrappers (CCWs),
/// including both regular and aggregated CCWs, then crashes via FailFast.
/// On other platforms: crashes immediately without creating COM objects.
/// </summary>
internal static class Program
{
    public const int RegularObjectCount = 3;
    public const int AggregatedObjectCount = 1;
    public const int TotalObjectCount = RegularObjectCount + AggregatedObjectCount;

    // Static fields keep the COM interface pointers (and therefore the CCWs) alive
    // until the process crashes, so they are visible in the dump.
    private static readonly IntPtr[] s_comPointers = new IntPtr[TotalObjectCount];

    // Strong GC handles so tests can discover the objects via handle enumeration
    // using the GC contract instead of a heap scan.
    private static readonly GCHandle[] s_gcHandles = new GCHandle[TotalObjectCount];

    private static void Main()
    {
        if (OperatingSystem.IsWindows())
        {
            // Create regular (non-aggregated) CCWs.
            for (int i = 0; i < RegularObjectCount; i++)
            {
                var obj = new ComObject();
                s_gcHandles[i] = GCHandle.Alloc(obj, GCHandleType.Normal);
                s_comPointers[i] = Marshal.GetIUnknownForObject(obj);
            }

            // Create an aggregated CCW via Marshal.CreateAggregatedObject.
            // The outer IUnknown wraps a regular CCW that acts as the controlling unknown.
            int idx = RegularObjectCount;
            var outerObj = new ComObject();
            IntPtr outerIUnknown = Marshal.GetIUnknownForObject(outerObj);

            var innerObj = new AggregatedComObject();
            s_gcHandles[idx] = GCHandle.Alloc(innerObj, GCHandleType.Normal);
            s_comPointers[idx] = Marshal.CreateAggregatedObject(outerIUnknown, innerObj);

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Environment.FailFast("cDAC dump test: CCW debuggee intentional crash");
    }
}

/// <summary>
/// COM interface exposed by the test objects.
/// </summary>
[ComVisible(true)]
[Guid("d9f48a91-5a3c-4d2e-8b0f-1234567890ab")]
internal interface IComTestInterface
{
    int GetValue();
}

/// <summary>
/// Managed class that implements <see cref="IComTestInterface"/> and acts as a regular CCW source.
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal class ComObject : IComTestInterface
{
    public int GetValue() => 42;
}

/// <summary>
/// Managed class used as the inner object in a COM aggregation scenario.
/// When wrapped with <see cref="Marshal.CreateAggregatedObject"/>, the resulting
/// CCW has the <c>IsAggregated</c> flag set.
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal class AggregatedComObject : IComTestInterface
{
    public int GetValue() => 99;
}
