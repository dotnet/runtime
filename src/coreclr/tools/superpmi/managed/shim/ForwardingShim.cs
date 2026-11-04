// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Internal.JitInterface;

namespace SuperPmi.Shim;

// The shims that forward all the JIT-EE interface calls between the runtime and the real JIT (SuperPMIShimPath),
// optionally counting them.
internal static unsafe class ForwardingShim
{
    private static readonly ShimInterop.JitCompilerCallbacks* s_compilerCallbacks = ShimInterop.Allocate(new ShimInterop.JitCompilerCallbacks
    {
        compileMethod = &CompileMethod,
        ProcessShutdownWork = &ProcessShutdownWork,
        getVersionIdentifier = &GetVersionIdentifier,
        setTargetOS = &SetTargetOS,
    });

    private static readonly ShimInterop.JitHostCallbacks* s_hostCallbacks = ShimInterop.Allocate(new ShimInterop.JitHostCallbacks
    {
        getIntConfigValue = &GetIntConfigValue,
        getStringConfigValue = &GetStringConfigValue,
        freeStringConfigValue = &FreeStringConfigValue,
    });

    private static IntPtr s_originalHost;

    static ForwardingShim()
    {
        string logFilePath = Environment.GetEnvironmentVariable("SuperPMIShimLogFilePath");
        if (logFilePath != null)
            Logger.OpenLogFile(logFilePath);
    }

    // Counts the calls (other than those of the JIT-EE interface itself), for the counter shim.
    public static Action<string> OnCall { get; set; }

    // Called once the real JIT is loaded, before the JIT gets the shim's objects.
    public static Action OnStartup { get; set; }

    public static void JitStartup(IntPtr host)
    {
        ShimEnvironment.SetDefaultPaths();
        ShimEnvironment.SetLibName();

        if (!ShimEnvironment.LoadRealJitLib())
            return;

        var realJitStartup = (delegate* unmanaged<IntPtr, void>)ShimEnvironment.GetRealJitExport("jitStartup");
        if (realJitStartup == null)
        {
            // This portion of the interface is not used by the JIT under test.
            return;
        }

        s_originalHost = host;
        IntPtr ourJitHost = ShimInterop.Shim_CreateJitHost(host, 0, s_hostCallbacks);
        OnStartup?.Invoke();
        realJitStartup(ourJitHost);
    }

    public static IntPtr GetJit()
    {
        ShimEnvironment.SetDefaultPaths();
        ShimEnvironment.SetLibName();

        if (!ShimEnvironment.LoadRealJitLib())
            return 0;

        var realGetJit = (delegate* unmanaged<IntPtr>)ShimEnvironment.GetRealJitExport("getJit");
        if (realGetJit == null)
        {
            Logger.LogError("getJit() - GetProcAddress 'getJit' failed (0x0000007f)");
            return 0;
        }

        IntPtr original = realGetJit();
        if (original == 0)
        {
            Logger.LogError("getJit() - pngetJit gave us null");
            return 0;
        }

        // The JIT is used for the lifetime of the process.
        IntPtr ourJit = ShimInterop.Shim_CreateJitCompiler(original, s_compilerCallbacks);
        OnStartup?.Invoke();
        return ourJit;
    }

    [UnmanagedCallersOnly]
    private static CorJitResult CompileMethod(IntPtr original, IntPtr* ppException, IntPtr comp, CORINFO_METHOD_INFO* info,
        uint flags, byte** nativeEntry, uint* nativeSizeOfCode)
    {
        try
        {
            OnCall?.Invoke("compileMethod");
            return new CorInfoImpl(new OriginalJitInfo(comp, ShimInterop.Forwarders)).CompileMethod(original, info, flags, nativeEntry, nativeSizeOfCode);
        }
        catch (Exception ex)
        {
            *ppException = ShimInterop.ToNativeException(ex);
            return CorJitResult.CORJIT_INTERNALERROR;
        }
    }

    [UnmanagedCallersOnly]
    private static void ProcessShutdownWork(IntPtr original, IntPtr info)
    {
        OnCall?.Invoke("ProcessShutdownWork");
        ShimInterop.Shim_ProcessShutdownWork(original, info);
    }

    [UnmanagedCallersOnly]
    private static void GetVersionIdentifier(IntPtr original, Guid* versionIdentifier)
    {
        OnCall?.Invoke("getVersionIdentifier");
        ShimInterop.Shim_GetVersionIdentifier(original, versionIdentifier);
    }

    [UnmanagedCallersOnly]
    private static void SetTargetOS(IntPtr original, CORINFO_OS os) => ShimInterop.Shim_SetTargetOS(original, os);

    [UnmanagedCallersOnly]
    private static int GetIntConfigValue(IntPtr thisHandle, byte* name, int defaultValue)
    {
        OnCall?.Invoke("getIntConfigValue");
        return ShimInterop.Shim_HostGetIntConfigValue(s_originalHost, name, defaultValue);
    }

    [UnmanagedCallersOnly]
    private static byte* GetStringConfigValue(IntPtr thisHandle, byte* name)
    {
        OnCall?.Invoke("getStringConfigValue");
        return ShimInterop.Shim_HostGetStringConfigValue(s_originalHost, name);
    }

    [UnmanagedCallersOnly]
    private static void FreeStringConfigValue(IntPtr thisHandle, byte* value)
    {
        OnCall?.Invoke("freeStringConfigValue");
        ShimInterop.Shim_HostFreeStringConfigValue(s_originalHost, value);
    }
}
