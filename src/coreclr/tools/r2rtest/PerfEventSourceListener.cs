// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom.Compiler;
using System.Threading;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

/// <summary>
/// Listens to the events logged by PerfEventSource in Crossgen2. Only one executable should be compiled at a time to get accurate
/// performance measurements. WaitForWarmupFinished must be called between the warmup runs and the real runs.
/// </summary>
public sealed class PerfEventSourceListener
{
    private ManualResetEvent _doneWarmup = new ManualResetEvent(false);
    private ManualResetEvent _doneCompilation = new ManualResetEvent(false);
    private const string providerName = "Microsoft-ILCompiler-Perf";
    private const string graphProviderName = "Microsoft-ILCompiler-Graph-Perf";
    private readonly int _totalRealRuns;
    private int _doneCompilationCount = 0;
    private int _doneWarmupCount = 0;
    private double _commandlineParseMsec = 0;
    private double _compilationMsec = 0;
    private double _loadingMsec = 0;
    private double _graphProcessingMsec = 0;
    private double _emittingMsec = 0;
    private double _jitMsec = 0; // Wall clock time spent JITing methods
    private double _totalJitMsec = 0; // CPU time spent JITing methods (sum of all threads)
    private double _dependencyAnalysisMsec = 0;
    private int _methodsJitted = 0;
    private int nodesAddedToMarkStack = 0;

    public PerfEventSourceListener(TraceEventSession traceEventSession, int totalWarmups, int totalRealRuns)
    {
        _totalRealRuns = totalRealRuns;
        traceEventSession.EnableProvider(providerName, TraceEventLevel.Verbose);
        traceEventSession.EnableProvider(graphProviderName, TraceEventLevel.Verbose);

        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(providerName, "Compilation/Start", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _compilationMsec -= traceEvent.TimeStampRelativeMSec;
        });
        // Use the Compilation/Stop events to count when a run ends, since events arrive in order.
        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(providerName, "Compilation/Stop", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
            {
                _compilationMsec += traceEvent.TimeStampRelativeMSec;
                _doneCompilationCount++;
                if (_doneCompilationCount == totalRealRuns)
                {
                    _doneCompilation.Set();
                }
            }
            else
            {
                _doneWarmupCount++;
                if (_doneWarmupCount == totalWarmups)
                {
                    _doneWarmup.Set();
                }
            }
        });

        // For all of the events below, we only want to process them after the warmup is complete. We can't just start this listener after
        // we have started the warmup because those runs might take some time, and we don't want to erroneously process warmup events when
        // trying to measure the real runs.
        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(providerName, "CommandLineProcessing/Start", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _commandlineParseMsec -= traceEvent.TimeStampRelativeMSec;
        });
        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(providerName, "CommandLineProcessing/Stop", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _commandlineParseMsec += traceEvent.TimeStampRelativeMSec;
        });
        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(providerName, "Loading/Start", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _loadingMsec -= traceEvent.TimeStampRelativeMSec;
        });
        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(providerName, "Loading/Stop", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _loadingMsec += traceEvent.TimeStampRelativeMSec;
        });

        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(graphProviderName, "GraphProcessing/Start", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _graphProcessingMsec -= traceEvent.TimeStampRelativeMSec;
        });
        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(graphProviderName, "GraphProcessing/Stop", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _graphProcessingMsec += traceEvent.TimeStampRelativeMSec;
        });

        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(providerName, "Emitting/Start", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _emittingMsec -= traceEvent.TimeStampRelativeMSec;
        });
        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(providerName, "Emitting/Stop", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _emittingMsec += traceEvent.TimeStampRelativeMSec;
        });

        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(providerName, "Jit/Start", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _jitMsec -= traceEvent.TimeStampRelativeMSec;
        });
        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(providerName, "Jit/Stop", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _jitMsec += traceEvent.TimeStampRelativeMSec;
        });

        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(providerName, "JitMethod/Start", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
            {
                _totalJitMsec -= traceEvent.TimeStampRelativeMSec;
                ++_methodsJitted;
            }
        });
        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(providerName, "JitMethod/Stop", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _totalJitMsec += traceEvent.TimeStampRelativeMSec;
        });


        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(graphProviderName, "DependencyAnalysis/Start", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _dependencyAnalysisMsec -= traceEvent.TimeStampRelativeMSec;
        });
        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(graphProviderName, "DependencyAnalysis/Stop", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                _dependencyAnalysisMsec += traceEvent.TimeStampRelativeMSec;
        });

        traceEventSession.Source.Dynamic.AddCallbackForProviderEvent(graphProviderName, "AddedNodeToMarkStack", delegate (TraceEvent traceEvent)
        {
            if (_doneWarmup.WaitOne(0))
                nodesAddedToMarkStack++;
        });
    }

    /// <summary>
    /// Prints the recorded perf results. This function should only be called after the real run has been dispatched.
    /// </summary>
    public void PrintPerfResults()
    {
        _doneCompilation.WaitOne();
        IndentedTextWriter writer = new IndentedTextWriter(Console.Out);
        writer.WriteLine($"Command line processing time: {_commandlineParseMsec / _totalRealRuns:F2} ms");
        writer.WriteLine($"Total average compilation time: {_compilationMsec / _totalRealRuns:F2} ms");
        writer.WriteLine($"Phase breakdown (average):");
        writer.Indent++;

        writer.WriteLine($"Loading time: {_loadingMsec / _totalRealRuns:F2} ms");

        writer.WriteLine($"Graph processing time: {_graphProcessingMsec / _totalRealRuns:F2} ms");
        writer.Indent++;
        writer.WriteLine($"Added {nodesAddedToMarkStack / _totalRealRuns} nodes to mark stack");
        writer.WriteLine($"Dependency analysis time: {_dependencyAnalysisMsec / _totalRealRuns:F2} ms");
        writer.WriteLine($"Wall clock JIT time: {_jitMsec / _totalRealRuns:F2} ms");
        writer.WriteLine($"Total JIT time: {_totalJitMsec / _totalRealRuns:F2} ms (sum of all threads)");
        writer.WriteLine($"{_methodsJitted/ _totalRealRuns} methods JITed");
        writer.Indent--;

        writer.WriteLine($"Emitting time: {_emittingMsec / _totalRealRuns:F2} ms");
    }

    /// <summary>
    /// Waits for the warmup runs to finish. This must be called before the real runs are dispatched to avoid ignoring events from real runs.
    /// </summary>
    public void WaitForWarmupFinished()
    {
        _doneWarmup.WaitOne();
    }
}
