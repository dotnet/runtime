// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Internal.JitInterface;

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
        ForwardingShim.OnCall = s_summarizer.AddCall;
        ShimInterop.Shim_SetUnloadCallback(&SaveTextFile);
    };

    public static void AddCall(string name) => s_summarizer.AddCall(name);

    [UnmanagedCallersOnly(EntryPoint = "jitStartup")]
    private static void jitStartup(IntPtr host) => ForwardingShim.JitStartup(host);

    [UnmanagedCallersOnly(EntryPoint = "getJit")]
    private static IntPtr getJit() => ForwardingShim.GetJit();

    [UnmanagedCallersOnly]
    private static void SaveTextFile() => s_summarizer.SaveTextFile();
}

internal sealed class MethodCallSummarizer
{
    private readonly string _dataFileName;
    private readonly ConcurrentDictionary<string, StrongBox<int>> _namesAndCounts = new(StringComparer.Ordinal);

    public MethodCallSummarizer(string logPath)
        => _dataFileName = ShimEnvironment.GetResultFileName(logPath, ShimEnvironment.GetProcessCommandLine(), ".csv");

    public void AddCall(string name) => Interlocked.Increment(ref _namesAndCounts.GetOrAdd(name, static _ => new StrongBox<int>()).Value);

    public void SaveTextFile()
    {
        try
        {
            using var writer = new StreamWriter(_dataFileName);
            writer.WriteLine("FunctionName,Count");
            foreach ((string name, StrongBox<int> count) in _namesAndCounts.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                writer.WriteLine($"{name},{count.Value}");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.LogError($"Couldn't open file '{_dataFileName}': error {ErrorHandling.LastError(e)}");
        }
    }
}
