// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Loads and pins the two native modules the shim brokers between.
/// </summary>
/// <remarks>
/// Both modules are loaded once and never freed: the COM objects handed out to the debugger are
/// implemented by code inside them, so unloading either one at any point would leave the debugger
/// holding dangling vtables. Process-lifetime pinning is therefore deliberate, not a leak.
/// </remarks>
internal static unsafe class NativeModules
{
    private static readonly object s_lock = new();

    private static bool s_productionLoaded;
    private static IntPtr s_productionHandle;

    private static bool s_legacyLoaded;
    private static IntPtr s_legacyHandle;

    /// <summary>Handle to the canonical production cDAC, or <see cref="IntPtr.Zero"/> when unavailable.</summary>
    internal static IntPtr ProductionCDac
    {
        get
        {
            lock (s_lock)
            {
                if (!s_productionLoaded)
                {
                    s_productionLoaded = true;
                    string? path = ShimEnvironment.GetProductionCDacPath();
                    if (path is null)
                    {
                        ShimLog.Error("Unable to determine the production cDAC path.");
                    }
                    else if (!NativeLibrary.TryLoad(path, out s_productionHandle))
                    {
                        ShimLog.Error($"Failed to load the production cDAC at '{path}'.");
                        s_productionHandle = IntPtr.Zero;
                    }
                    else
                    {
                        ShimLog.Info($"Loaded production cDAC: {path}");
                    }
                }

                return s_productionHandle;
            }
        }
    }

    /// <summary>Handle to the legacy DAC, or <see cref="IntPtr.Zero"/> when it was not configured.</summary>
    internal static IntPtr LegacyDac
    {
        get
        {
            lock (s_lock)
            {
                if (!s_legacyLoaded)
                {
                    s_legacyLoaded = true;
                    string? path = ShimEnvironment.GetLegacyDacPath();
                    if (path is null)
                    {
                        ShimLog.Info(
                            $"{ShimEnvironment.LegacyDacPathVariable} is not set; running without legacy DAC validation.");
                    }
                    else if (!NativeLibrary.TryLoad(path, out s_legacyHandle))
                    {
                        ShimLog.Error($"Failed to load the legacy DAC at '{path}'.");
                        s_legacyHandle = IntPtr.Zero;
                    }
                    else
                    {
                        ShimLog.Info($"Loaded legacy DAC: {path}");
                    }
                }

                return s_legacyHandle;
            }
        }
    }

    internal static void* GetExport(IntPtr module, string name)
    {
        if (module == IntPtr.Zero)
            return null;

        return NativeLibrary.TryGetExport(module, name, out IntPtr address) ? (void*)address : null;
    }

    internal static delegate* unmanaged<Guid*, IntPtr, void**, int> ProductionCLRDataCreateInstance
        => (delegate* unmanaged<Guid*, IntPtr, void**, int>)GetExport(ProductionCDac, "CLRDataCreateInstance");

    internal static delegate* unmanaged<Guid*, IntPtr, void**, int> LegacyCLRDataCreateInstance
        => (delegate* unmanaged<Guid*, IntPtr, void**, int>)GetExport(LegacyDac, "CLRDataCreateInstance");

    internal static delegate* unmanaged<Guid*, IntPtr, ulong, void**, int> ProductionDbgShimCreateInstanceFromContractDescriptor
        => (delegate* unmanaged<Guid*, IntPtr, ulong, void**, int>)GetExport(ProductionCDac, "DbgShimCreateInstanceFromContractDescriptor");

    internal static delegate* unmanaged<IntPtr, ulong, ulong, IntPtr, IntPtr, void**, int> ProductionDacDbiInterfaceInstance
        => (delegate* unmanaged<IntPtr, ulong, ulong, IntPtr, IntPtr, void**, int>)GetExport(ProductionCDac, "DacDbiInterfaceInstance");

    internal static delegate* unmanaged<IntPtr, ulong, ulong, IntPtr, IntPtr, void**, int> LegacyDacDbiInterfaceInstance
        => (delegate* unmanaged<IntPtr, ulong, ulong, IntPtr, IntPtr, void**, int>)GetExport(LegacyDac, "DacDbiInterfaceInstance");
}
