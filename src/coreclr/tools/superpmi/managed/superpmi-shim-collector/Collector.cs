// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Internal.JitInterface;

namespace SuperPmi.Shim;

// superpmi-shim-collector: a JIT that compiles with the real JIT (SuperPMIShimPath), and records the JIT-EE
// interface calls of each compilation as a method context, appended to a file in SuperPMIShimLogPath.
internal static unsafe class Collector
{
    private static string s_collectionFilter;

    static Collector()
    {
        string logFilePath = Environment.GetEnvironmentVariable("SuperPMIShimLogFilePath");
        if (logFilePath != null)
            Logger.OpenLogFile(logFilePath);
    }

    // The global context records the JIT's configuration queries, which are copied to each method context.
    public static MethodContext GlobalContext { get; private set; }

    public static string CollectionFilter => s_collectionFilter;

    private static void SetCollectionFilter()
    {
        s_collectionFilter = Environment.GetEnvironmentVariable("SuperPMIShimFilter");
        if (s_collectionFilter != null)
            Logger.Error.WriteLine($"*** SPMI filter '{s_collectionFilter}'");
    }

    [UnmanagedCallersOnly(EntryPoint = "jitStartup")]
    private static void jitStartup(IntPtr host)
    {
        ShimEnvironment.SetDefaultPaths();
        ShimEnvironment.SetLibName();
        SetCollectionFilter();

        if (!ShimEnvironment.LoadRealJitLib())
            return;

        var realJitStartup = (delegate* unmanaged<IntPtr, void>)ShimEnvironment.GetRealJitExport("jitStartup");
        if (realJitStartup == null)
        {
            // This portion of the interface is not used by the JIT under test.
            return;
        }

        GlobalContext = new MethodContext();
        realJitStartup(CollectorJitHost.Create(host));
    }

    [UnmanagedCallersOnly(EntryPoint = "getJit")]
    private static IntPtr getJit()
    {
        ShimEnvironment.SetDefaultPaths();
        ShimEnvironment.SetLibName();
        ShimEnvironment.SetLogPath();
        string dataFileName = ShimEnvironment.GetResultFileName(ShimEnvironment.LogPath, ShimEnvironment.GetProcessCommandLine(), ".mc");

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

        return new CollectorJitCompiler(original, dataFileName).Native;
    }
}

// The ICorJitCompiler that the runtime calls, which records each compilation into a method context. The native
// RecordingJitCompiler compiles with the real JIT, and calls this to set up and to finish the recording.
internal sealed unsafe class CollectorJitCompiler
{
    private static readonly ShimInterop.RecordingCompilerCallbacks* s_callbacks = ShimInterop.Allocate(new ShimInterop.RecordingCompilerCallbacks
    {
        beginCompile = &BeginCompileCallback,
        endCompile = &EndCompileCallback,
        setTargetOS = &SetTargetOSCallback,
        pendingCallNotTaken = &PendingCallNotTakenCallback,
        recordUpdateEntryPointForTailCall = &CorInfoImpl.RecordUpdateEntryPointForTailCallCallback,
    });

    private readonly FileStream _file;
    private CORINFO_OS _currentOs = OperatingSystem.IsWindows() ? CORINFO_OS.CORINFO_WINNT :
        OperatingSystem.IsMacOS() ? CORINFO_OS.CORINFO_APPLE : CORINFO_OS.CORINFO_UNIX;

    public CollectorJitCompiler(IntPtr original, string dataFileName)
    {
        try
        {
            _file = new FileStream(dataFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.SequentialScan);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Logger.LogError($"Couldn't open file '{dataFileName}': error {ErrorHandling.LastError(e)}");
        }

        // The JIT is used for the lifetime of the process.
        Native = ShimInterop.Shim_CreateRecordingJitCompiler(original, GCHandle.ToIntPtr(GCHandle.Alloc(this)), s_callbacks);
    }

    public IntPtr Native { get; }

    private static CollectorJitCompiler Get(IntPtr thisHandle) => (CollectorJitCompiler)GCHandle.FromIntPtr(thisHandle).Target;

    // Returns the handle of the JIT-EE interface that records the compilation, or 0 not to record it.
    [UnmanagedCallersOnly]
    private static IntPtr BeginCompileCallback(IntPtr thisHandle, IntPtr comp, ShimInterop.PendingCall* pending, CORINFO_METHOD_INFO* info,
        uint flags, IntPtr* pJitInfoCallbacks)
    {
        try
        {
            CorInfoImpl ourJitInfo = Get(thisHandle).beginCompile(comp, pending, info, flags);
            if (ourJitInfo == null)
                return 0;

            *pJitInfoCallbacks = CorInfoImpl.UnmanagedCallbacks;
            return GCHandle.ToIntPtr(GCHandle.Alloc(ourJitInfo));
        }
        catch (EEException)
        {
            // The EE threw for a call that the collector made itself: the compilation isn't recorded.
            return 0;
        }
        catch (Exception ex)
        {
            return ShimInterop.Fail(ex);
        }
    }

    [UnmanagedCallersOnly]
    private static void EndCompileCallback(IntPtr thisHandle, IntPtr jitInfoHandle, CorJitResult result, byte* nativeEntry, uint nativeSizeOfCode)
    {
        GCHandle handle = GCHandle.FromIntPtr(jitInfoHandle);
        try
        {
            var ourJitInfo = (CorInfoImpl)handle.Target;
            if (!ourJitInfo.SavedCollectionEarly)
                Get(thisHandle).finalizeAndCommitCollection(ourJitInfo.MethodContext, result, nativeEntry, nativeSizeOfCode);
        }
        catch (Exception ex)
        {
            ShimInterop.Fail(ex);
        }
        finally
        {
            handle.Free();
        }
    }

    [UnmanagedCallersOnly]
    private static void SetTargetOSCallback(IntPtr thisHandle, CORINFO_OS os) => Get(thisHandle)._currentOs = os;

    [UnmanagedCallersOnly]
    private static void PendingCallNotTakenCallback(int index)
        => ShimInterop.Fail(new InvalidOperationException($"The collector didn't take the pending call to {OriginalJitInfo.GetMethodName(index)}"));

    private CorInfoImpl beginCompile(IntPtr comp, ShimInterop.PendingCall* pending, CORINFO_METHOD_INFO* info, uint flags)
    {
        var original = new OriginalJitInfo(comp, ShimInterop.Forwarders, pending);

        // See if we are filtering the collection (currently by simple substring match).
        if (Collector.CollectionFilter != null)
        {
            byte* className = null;
            byte* methodName = original.getMethodNameFromMetadata(info->ftn, &className, null, null, 0);
            if (!Contains(methodName, Collector.CollectionFilter) && !Contains(className, Collector.CollectionFilter))
                return null;
        }

        var mc = new MethodContext();
        var ourJitInfo = new CorInfoImpl(this, original, mc);

        mc.cr.recProcessName(ShimEnvironment.GetProcessCommandLine());
        mc.recCompileMethod(info, flags, _currentOs);
        ourJitInfo.RecordExtraData(info);

        // Record data from the global context, if any.
        if (Collector.GlobalContext != null)
            mc.recGlobalContext(Collector.GlobalContext);

        return ourJitInfo;
    }

    public void finalizeAndCommitCollection(MethodContext mc, CorJitResult result, byte* nativeEntry, uint nativeSizeOfCode)
    {
        mc.cr.recCompileMethod(nativeEntry, nativeSizeOfCode, result);

        if (result == CorJitResult.CORJIT_OK)
        {
            mc.cr.recAllocMemCapture();
            mc.cr.recAllocGCInfoCapture();
        }

        if (_file != null)
        {
            // The JIT compiles methods concurrently; each method context is written at once.
            lock (_file)
            {
                mc.saveToFile(_file);
            }
        }
    }

    private static bool Contains(byte* s, string value)
        => s != null && MemoryMarshal.CreateReadOnlySpanFromNullTerminated(s).IndexOf(Encoding.UTF8.GetBytes(value)) >= 0;
}

// The ICorJitHost that the real JIT gets: its configuration queries are recorded in the global method context.
//
// This works because the JIT eagerly asks for all config values once, in jitStartup. If the JIT were to ask for
// config values later, during the compilations, they would get recorded in the global context too, and copied
// to all the later method contexts, which would be incorrect.
internal static unsafe class CollectorJitHost
{
    private static readonly ShimInterop.JitHostCallbacks* s_callbacks = ShimInterop.Allocate(new ShimInterop.JitHostCallbacks
    {
        getIntConfigValue = &GetIntConfigValue,
        getStringConfigValue = &GetStringConfigValue,
        freeStringConfigValue = &FreeStringConfigValue,
    });

    private static IntPtr s_original;

    // The DOTNET variables that aren't recorded: those of file paths, and those it doesn't make sense to replay with.
    private static readonly string[] s_ignoredVars =
    [
        "EnableExtraSuperPmiQueries",
        "JitDisasm",
        "JitDump",
        "JitDisasmWithAlignmentBoundaries",
        "JitDumpASCII",
        "JitHashBreak",
        "JitHashDump",
        "JitHashHalt",
        "JitOrder",
        "JitPrintInlinedMethods",
        "JitPrintDevirtualizedMethods",
        "JitBreak",
        "JitDebugBreak",
        "JitDisasmAssemblies",
        "JitDisasmWithGC",
        "JitDisasmWithDebugInfo",
        "JitDisasmSpilled",
        "JitDumpTier0",
        "JitDumpAtOSROffset",
        "JitDumpInlinePhases",
        "JitEHDump",
        "JitExclude",
        "JitGCDump",
        "JitDebugDump",
        "JitHalt",
        "JitImportBreak",
        "JitInclude",
        "JitLateDisasm",
        "JitUnwindDump",
        "JitDumpFg",
        "JitDumpFgDir",
        "JitDumpFgPhase",
        "JitDumpFgPrePhase",
        "JitDumpFgDot",
        "JitDumpFgEH",
        "JitDumpFgLoops",
        "JitDumpFgConstrained",
        "JitDumpFgBlockID",
        "JitDumpFgBlockFlags",
        "JitDumpFgLoopFlags",
        "JitDumpFgBlockOrder",
        "JITLateDisasmTo",
        "JitDisasmSummary",
        "JitStdOutFile",
        "WriteRichDebugInfoFile",
        "JitFuncInfoLogFile",
        "JitTimeLogCsv",
        "JitMeasureNowayAssertFile",
        "JitInlineDumpData",
        "JitInlineDumpXml",
        "JitInlineDumpXmlFile",
        "JitInlinePolicyDumpXml",
        "JitInlineReplayFile",
        "JitFunctionFile",
        "JitRawHexCode",
        "JitRawHexCodeFile",
    ];

    public static IntPtr Create(IntPtr original)
    {
        s_original = original;
        return ShimInterop.Shim_CreateJitHost(original, 0, s_callbacks);
    }

    private static bool RecordVariable(byte* key)
    {
        string name = Marshal.PtrToStringUTF8((IntPtr)key);
        foreach (string ignoredVar in s_ignoredVars)
        {
            if (string.Equals(name, ignoredVar, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    private static bool IsKey(byte* key, ReadOnlySpan<byte> name) => MemoryMarshal.CreateReadOnlySpanFromNullTerminated(key).SequenceEqual(name);

    [UnmanagedCallersOnly]
    private static int GetIntConfigValue(IntPtr thisHandle, byte* key, int defaultValue)
    {
        // Don't collect these pseudo-variables, and don't even record that they were asked for: superpmi
        // answers them itself.
        if (IsKey(key, "SuperPMIMethodContextNumber"u8) || IsKey(key, "JitReportMetrics"u8))
            return defaultValue;

        MethodContext mc = Collector.GlobalContext;
        lock (mc)
        {
            mc.cr.AddCall("getIntConfigValue");
            int result = ShimInterop.Shim_HostGetIntConfigValue(s_original, key, defaultValue);

            // The JIT eagerly asks about every config value. If we store all these queries, it takes almost half the
            // MC file space. So only store the non-default answers.
            if (RecordVariable(key) && result != defaultValue)
                mc.recGetIntConfigValue(key, defaultValue, result);
            return result;
        }
    }

    [UnmanagedCallersOnly]
    private static byte* GetStringConfigValue(IntPtr thisHandle, byte* key)
    {
        MethodContext mc = Collector.GlobalContext;
        lock (mc)
        {
            mc.cr.AddCall("getStringConfigValue");
            byte* result = ShimInterop.Shim_HostGetStringConfigValue(s_original, key);

            // Don't store null returns, which is the default.
            if (RecordVariable(key) && result != null)
                mc.recGetStringConfigValue(key, result);
            return result;
        }
    }

    [UnmanagedCallersOnly]
    private static void FreeStringConfigValue(IntPtr thisHandle, byte* value)
    {
        Collector.GlobalContext.cr.AddCall("freeStringConfigValue");
        ShimInterop.Shim_HostFreeStringConfigValue(s_original, value);
    }
}
