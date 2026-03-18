// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the BuiltInCOM contract's RCW APIs.
/// Creates two kinds of RCW:
/// 1. A plain RCW for the StdGlobalInterfaceTable COM object (not aggregated).
/// 2. A contained RCW via a managed class extending a [ComImport] base class.
///    The GIT singleton doesn't support aggregation, so the runtime falls back
///    to containment.
/// Both are pinned in GC handles so dump tests can find them.
/// This debuggee is Windows-only, as RCW support requires FEATURE_COMINTEROP.
/// </summary>
internal static partial class Program
{
    private const int S_OK = 0;
    private const int S_FALSE = 1;
    private const int RpcEChangedMode = unchecked((int)0x80010106);
    private const uint CoInitMultithreaded = 0;

    private static readonly Guid CLSID_StdGlobalInterfaceTable = new("00000323-0000-0000-C000-000000000046");
    private static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");

    [ComImport]
    [Guid("00000146-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGlobalInterfaceTable
    {
        [PreserveSig]
        int RegisterInterfaceInGlobal(IntPtr pUnk, ref Guid riid, out int pdwCookie);

        [PreserveSig]
        int RevokeInterfaceFromGlobal(int dwCookie);

        [PreserveSig]
        int GetInterfaceFromGlobal(int dwCookie, ref Guid riid, out IntPtr ppv);
    }

    // A [ComImport] base class for the StdGlobalInterfaceTable CLSID.
    // Managed classes that extend this produce an extensible (aggregated) RCW.
    [ComImport]
    [Guid("00000323-0000-0000-C000-000000000046")]
    private class StdGlobalInterfaceTableClass
    {
    }

    // Extending the [ComImport] class makes this an "extensible RCW".
    // The GIT is a singleton that doesn't support aggregation, so the runtime
    // falls back to containment (RCW::MarkURTContained is called).
    private class ContainedGlobalInterfaceTable : StdGlobalInterfaceTableClass
    {
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [LibraryImport("ole32.dll")]
    private static partial void CoUninitialize();

    private static void Main()
    {
        if (OperatingSystem.IsWindows())
        {
            CreateRcwOnWindows();
        }

        Environment.FailFast("cDAC dump test: RCW debuggee intentional crash");
    }

    [SupportedOSPlatform("windows")]
    private static void CreateRcwOnWindows()
    {
        bool callCoUninitialize = false;
        int coInitializeResult = CoInitializeEx(IntPtr.Zero, CoInitMultithreaded);
        if (coInitializeResult == S_OK || coInitializeResult == S_FALSE)
        {
            callCoUninitialize = true;
        }
        else if (coInitializeResult != RpcEChangedMode)
        {
            Marshal.ThrowExceptionForHR(coInitializeResult);
        }

        IntPtr rcwIUnknown = IntPtr.Zero;
        IntPtr fetchedIUnknown = IntPtr.Zero;
        GCHandle rcwHandle = default;
        GCHandle containedHandle = default;

        try
        {
            // --- Plain RCW (not aggregated) ---
            Type comType = Type.GetTypeFromCLSID(CLSID_StdGlobalInterfaceTable, throwOnError: true)!;
            object rcwObject = Activator.CreateInstance(comType)!;

            IGlobalInterfaceTable globalInterfaceTable = (IGlobalInterfaceTable)rcwObject;

            rcwIUnknown = Marshal.GetIUnknownForObject(rcwObject);

            Guid iidIUnknown = IID_IUnknown;
            int registerResult = globalInterfaceTable.RegisterInterfaceInGlobal(rcwIUnknown, ref iidIUnknown, out int cookie);
            Marshal.ThrowExceptionForHR(registerResult);

            int getResult = globalInterfaceTable.GetInterfaceFromGlobal(cookie, ref iidIUnknown, out fetchedIUnknown);
            Marshal.ThrowExceptionForHR(getResult);

            int revokeResult = globalInterfaceTable.RevokeInterfaceFromGlobal(cookie);
            Marshal.ThrowExceptionForHR(revokeResult);

            rcwHandle = GCHandle.Alloc(rcwObject, GCHandleType.Normal);

            // --- Contained RCW ---
            // ContainedGlobalInterfaceTable extends StdGlobalInterfaceTableClass ([ComImport]),
            // but the GIT singleton doesn't support aggregation, so the runtime
            // falls back to containment (RCW::MarkURTContained).
            object containedObject = new ContainedGlobalInterfaceTable();
            containedHandle = GCHandle.Alloc(containedObject, GCHandleType.Normal);

            GC.KeepAlive(globalInterfaceTable);
            GC.KeepAlive(rcwHandle);
            GC.KeepAlive(rcwObject);
            GC.KeepAlive(containedHandle);
            GC.KeepAlive(containedObject);
        }
        finally
        {
            if (fetchedIUnknown != IntPtr.Zero)
            {
                Marshal.Release(fetchedIUnknown);
            }

            if (rcwIUnknown != IntPtr.Zero)
            {
                Marshal.Release(rcwIUnknown);
            }

            GC.KeepAlive(rcwHandle);
            GC.KeepAlive(containedHandle);

            if (callCoUninitialize)
            {
                CoUninitialize();
            }
        }
    }
}
