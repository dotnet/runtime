// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace SuperPmi.Shim;

// superpmi-shim-counter: a JIT that forwards the JIT-EE interface calls to the real JIT (SuperPMIShimPath), and
// counts them; the counts are written to a .csv file in SuperPMIShimLogPath when the shim is unloaded.
internal static unsafe class Counter
{
    private static MethodCallSummarizer s_summarizer;

    static Counter() => ForwardingShim.OnStartup = static () =>
    {
        if (s_summarizer != null)
            return;

        ShimEnvironment.SetLogPath();
        s_summarizer = new MethodCallSummarizer(ShimEnvironment.LogPath);
        ForwardingShim.OnCall = &AddCall;
        ShimInterop.Shim_SetUnloadCallback(&SaveTextFile);
    };

    [UnmanagedCallersOnly(EntryPoint = "jitStartup")]
    private static void jitStartup(IntPtr host) => ForwardingShim.JitStartup(host);

    [UnmanagedCallersOnly(EntryPoint = "getJit")]
    private static IntPtr getJit() => ForwardingShim.GetJit();

    [UnmanagedCallersOnly]
    private static void AddCall(byte* name) => s_summarizer.AddCall(name);

    [UnmanagedCallersOnly]
    private static void SaveTextFile() => s_summarizer.SaveTextFile();
}

internal sealed unsafe class MethodCallSummarizer
{
    private readonly string _dataFileName;

    // By the address of the name, a string literal of the native shim.
    private readonly ConcurrentDictionary<IntPtr, StrongBox<int>> _namesAndCounts = new();

    public MethodCallSummarizer(string logPath)
        => _dataFileName = ShimEnvironment.GetResultFileName(logPath, ShimEnvironment.GetProcessCommandLine(), ".csv");

    public void AddCall(byte* name) => Interlocked.Increment(ref _namesAndCounts.GetOrAdd((IntPtr)name, static _ => new StrongBox<int>()).Value);

    public void SaveTextFile()
    {
        try
        {
            using var writer = new StreamWriter(_dataFileName);
            writer.WriteLine("FunctionName,Count");
            var counts = _namesAndCounts
                .GroupBy(entry => Marshal.PtrToStringUTF8(entry.Key), entry => entry.Value.Value, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal);
            foreach (IGrouping<string, int> count in counts)
                writer.WriteLine($"{count.Key},{count.Sum()}");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Couldn't open file '{_dataFileName}': error {ErrorHandling.LastError(e)}");
        }
    }
}
