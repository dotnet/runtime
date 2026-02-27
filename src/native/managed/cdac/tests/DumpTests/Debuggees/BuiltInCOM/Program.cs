// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the BuiltInCOM contract's GetRCWInterfaces API.
/// Creates a COM RCW with a populated inline interface entry cache, then crashes
/// to produce a dump for analysis.
/// This debuggee is Windows-only, as RCW support requires FEATURE_COMINTEROP.
/// </summary>
internal static class Program
{
    [ComVisible(true)]
    [Guid("6B29FC40-CA47-1067-B31D-00DD010662DA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ITestInterface
    {
        void DoNothing();
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("EA5B4A62-3F3F-4B3B-8D7C-1234567890AB")]
    public class TestComObject : ITestInterface
    {
        public void DoNothing() { }
    }

    private static void Main()
    {
        if (OperatingSystem.IsWindows())
        {
            CreateRCWOnWindows();
        }

        Environment.FailFast("cDAC dump test: BuiltInCOM debuggee intentional crash");
    }

    [SupportedOSPlatform("windows")]
    private static void CreateRCWOnWindows()
    {
        // Create a managed COM-visible object and get its CCW IUnknown
        var comObject = new TestComObject();
        IntPtr pUnknown = Marshal.GetIUnknownForObject(comObject);

        try
        {
            // GetUniqueObjectForIUnknown creates a real RCW (bypasses identity lookup),
            // so we get an actual RCW wrapping the CCW. Casting to ITestInterface
            // triggers a QueryInterface which populates the inline interface entry cache.
            object rcwObject = Marshal.GetUniqueObjectForIUnknown(pUnknown);

            if (rcwObject is ITestInterface iface)
            {
                iface.DoNothing();
            }

            // Pin the RCW object in a strong GC handle so the dump test can find it
            // by walking the strong handle table (matching how GCRoots debuggee works).
            GCHandle rcwHandle = GCHandle.Alloc(rcwObject, GCHandleType.Normal);
            GC.KeepAlive(rcwHandle);
            GC.KeepAlive(rcwObject);
        }
        finally
        {
            Marshal.Release(pUnknown);
        }

        GC.KeepAlive(comObject);
    }
}
