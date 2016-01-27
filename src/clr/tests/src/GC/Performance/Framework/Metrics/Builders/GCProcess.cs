// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if WINDOWS

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Stacks;
using System;
using System.Collections.Generic;
using System.Diagnostics;

#if HAS_PRIVATE_GC_EVENTS
using Microsoft.Diagnostics.Tracing.Parsers.ClrPrivate;
#endif


namespace GCPerfTestFramework.Metrics.Builders
{
    /// <summary>
    /// GCProcess holds information about GCs in a particular process. 
    /// </summary>
    /// 
    // Notes on parsing GC events:
    // GC events need to be interpreted in sequence and if we attach we 
    // may not get the whole sequence of a GC. We discard the incomplete
    // GC from the beginning and ending. 
    //
    // We can make the assumption if we have the events from the private 
    // provider it means we have events from public provider, but not the
    // other way around.
    //
    // All GC events are at informational level except the following:
    // AllocTick from the public provider
    // GCJoin from the private provider
    // We may only have events at the informational level, not verbose level.
    //
    internal class GCProcess
    {
        internal static IDictionary<int, GCProcess> Collect(
            TraceEventSource source, 
            float sampleIntervalMSec, 
            IDictionary<int, GCProcess> perProc = null, 
            MutableTraceEventStackSource stackSource = null,
            Predicate<TraceEvent> filterFunc = null)
        {
            if (perProc == null)
            {
                perProc = new Dictionary<int, GCProcess>();
            }

            source.Kernel.AddCallbackForEvents(delegate (ProcessCtrTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                var stats = perProc.GetOrCreate(data.ProcessID);
                stats.PeakVirtualMB = ((double)data.PeakVirtualSize) / 1000000.0;
                stats.PeakWorkingSetMB = ((double)data.PeakWorkingSetSize) / 1000000.0;
            });

            Action<RuntimeInformationTraceData> doAtRuntimeStart = delegate (RuntimeInformationTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                var stats = perProc.GetOrCreate(data.ProcessID);
                stats.RuntimeVersion = "V " + data.VMMajorVersion.ToString() + "." + data.VMMinorVersion + "." + data.VMBuildNumber
                    + "." + data.VMQfeNumber;
                stats.StartupFlags = data.StartupFlags;
                stats.Bitness = (data.RuntimeDllPath.ToLower().Contains("framework64") ? 64 : 32);
                if (stats.CommandLine == null)
                    stats.CommandLine = data.CommandLine;
            };

            // log at both startup and rundown
            //var clrRundown = new ClrRundownTraceEventParser(source);
            //clrRundown.RuntimeStart += doAtRuntimeStart;
            source.Clr.AddCallbackForEvent("Runtime/Start", doAtRuntimeStart);

            Action<ProcessTraceData> processStartCallback = data =>
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                var stats = perProc.GetOrCreate(data.ProcessID);

                if (!string.IsNullOrEmpty(data.KernelImageFileName))
                {
                    // When we just have an EventSource (eg, the source was created by 
                    // ETWTraceEventSource), we don't necessarily have the process names
                    // decoded - it all depends on whether we have seen a ProcessStartGroup 
                    // event or not. When the trace was taken after the process started we 
                    // know we didn't see such an event.
                    string name = GetImageName(data.KernelImageFileName);

                    // Strictly speaking, this is not really fixing it 'cause 
                    // it doesn't handle when a process with the same name starts
                    // with the same pid. The chance of that happening is really small.
                    if (stats.isDead == true)
                    {
                        stats = new GCProcess();
                        stats.Init(data);
                        perProc[data.ProcessID] = stats;
                    }
                }

                var commandLine = data.CommandLine;
                if (!string.IsNullOrEmpty(commandLine))
                    stats.CommandLine = commandLine;
            };

            source.Kernel.AddCallbackForEvent("Process/Start", processStartCallback);
            source.Kernel.AddCallbackForEvent("Process/DCStart", processStartCallback);

            Action<ProcessTraceData> processEndCallback = delegate (ProcessTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                var stats = perProc.GetOrCreate(data.ProcessID);

                if (string.IsNullOrEmpty(stats.ProcessName))
                {
                    stats.ProcessName = GetImageName(data.KernelImageFileName);
                }

                if (data.OpcodeName == "Stop")
                {
                    stats.isDead = true;
                }
            };

            source.Kernel.AddCallbackForEvent("Process/Stop", processEndCallback);
            source.Kernel.AddCallbackForEvent("Process/DCStop", processEndCallback);

#if (!CAP)
            CircularBuffer<ThreadWorkSpan> RecentThreadSwitches = new CircularBuffer<ThreadWorkSpan>(10000);
            source.Kernel.AddCallbackForEvent("Thread/CSwitch", delegate (CSwitchTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                RecentThreadSwitches.Add(new ThreadWorkSpan(data));
                GCProcess stats;
                if (perProc.TryGetValue(data.ProcessID, out stats))
                {
                    stats.ThreadId2Priority[data.NewThreadID] = data.NewThreadPriority;
                    if (stats.IsServerGCThread(data.ThreadID) > -1)
                    {
                        stats.ServerGcHeap2ThreadId[data.ProcessorNumber] = data.ThreadID;
                    }
                }

                foreach (var gcProcess in perProc.Values)
                {
                    GCEvent _event = gcProcess.GetCurrentGC();
                    // If we are in the middle of a GC.
                    if (_event != null)
                    {
                        if ((_event.Type != GCType.BackgroundGC) && (gcProcess.isServerGCUsed == 1))
                        {
                            _event.AddServerGcThreadSwitch(new ThreadWorkSpan(data));
                        }
                    }
                }
            });

            CircularBuffer<ThreadWorkSpan> RecentCpuSamples = new CircularBuffer<ThreadWorkSpan>(1000);
            StackSourceSample sample = new StackSourceSample(stackSource);

            source.Kernel.AddCallbackForEvent("PerfInfo/Sample", delegate (SampledProfileTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                RecentCpuSamples.Add(new ThreadWorkSpan(data));
                GCProcess processWithGc = null;
                foreach (var gcProcess in perProc.Values)
                {
                    GCEvent e = gcProcess.GetCurrentGC();
                    // If we are in the middle of a GC.
                    if (e != null)
                    {
                        if ((e.Type != GCType.BackgroundGC) && (gcProcess.isServerGCUsed == 1))
                        {
                            e.AddServerGcSample(new ThreadWorkSpan(data));
                            processWithGc = gcProcess;
                        }
                    }
                }

                if (stackSource != null && processWithGc != null)
                {
                    GCEvent e = processWithGc.GetCurrentGC();
                    sample.Metric = 1;
                    sample.TimeRelativeMSec = data.TimeStampRelativeMSec;
                    var nodeName = string.Format("Server GCs #{0} in {1} (PID:{2})", e.GCNumber, processWithGc.ProcessName, processWithGc.ProcessID);
                    var nodeIndex = stackSource.Interner.FrameIntern(nodeName);
                    sample.StackIndex = stackSource.Interner.CallStackIntern(nodeIndex, stackSource.GetCallStack(data.CallStackIndex(), data));
                    stackSource.AddSample(sample);
                }

                GCProcess stats;
                if (perProc.TryGetValue(data.ProcessID, out stats))
                {
                    if (stats.IsServerGCThread(data.ThreadID) > -1)
                    {
                        stats.ServerGcHeap2ThreadId[data.ProcessorNumber] = data.ThreadID;
                    }

                    var cpuIncrement = sampleIntervalMSec;
                    stats.ProcessCpuMSec += cpuIncrement;

                    GCEvent _event = stats.GetCurrentGC();
                    // If we are in the middle of a GC.
                    if (_event != null)
                    {
                        bool isThreadDoingGC = false;
                        if ((_event.Type != GCType.BackgroundGC) && (stats.isServerGCUsed == 1))
                        {
                            int heapIndex = stats.IsServerGCThread(data.ThreadID);
                            if (heapIndex != -1)
                            {
                                _event.AddServerGCThreadTime(heapIndex, cpuIncrement);
                                isThreadDoingGC = true;
                            }
                        }
                        else if (data.ThreadID == stats.suspendThreadIDGC)
                        {
                            _event.GCCpuMSec += cpuIncrement;
                            isThreadDoingGC = true;
                        }
                        else if (stats.IsBGCThread(data.ThreadID))
                        {
                            Debug.Assert(stats.currentBGC != null);
                            if (stats.currentBGC != null)
                                stats.currentBGC.GCCpuMSec += cpuIncrement;
                            isThreadDoingGC = true;
                        }

                        if (isThreadDoingGC)
                        {
                            stats.GCCpuMSec += cpuIncrement;
                        }
                    }
                }
            });
#endif


            source.Clr.AddCallbackForEvent("GC/SuspendEEStart", delegate (GCSuspendEETraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                var stats = perProc.GetOrCreate(data.ProcessID);
                switch (data.Reason)
                {
                    case GCSuspendEEReason.SuspendForGC:
                        stats.suspendThreadIDGC = data.ThreadID;
                        break;
                    case GCSuspendEEReason.SuspendForGCPrep:
                        stats.suspendThreadIDBGC = data.ThreadID;
                        break;
                    default:
                        stats.suspendThreadIDOther = data.ThreadID;
                        break;
                }

                stats.suspendTimeRelativeMSec = data.TimeStampRelativeMSec;
            });

            // In 2.0 we didn't have this event.

            source.Clr.AddCallbackForEvent("GC/SuspendEEStop", delegate (GCNoUserDataTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc.GetOrCreate(data.ProcessID);

                if ((stats.suspendThreadIDBGC > 0) && (stats.currentBGC != null))
                {
                    stats.currentBGC._SuspendDurationMSec += data.TimeStampRelativeMSec - stats.suspendTimeRelativeMSec;
                }

                stats.suspendEndTimeRelativeMSec = data.TimeStampRelativeMSec;
            });

            source.Clr.AddCallbackForEvent("GC/RestartEEStop", delegate (GCNoUserDataTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc.GetOrCreate(data.ProcessID);
                GCEvent _event = stats.GetCurrentGC();
                if (_event != null)
                {
                    if (_event.Type == GCType.BackgroundGC)
                    {
                        stats.AddConcurrentPauseTime(_event, data.TimeStampRelativeMSec);
                    }
                    else
                    {
                        Debug.Assert(_event.PauseStartRelativeMSec != 0);
                        // In 2.0 Concurrent GC, since we don't know the GC's type we can't tell if it's concurrent 
                        // or not. But we know we don't have nested GCs there so simply check if we have received the
                        // GCStop event; if we have it means it's a blocking GC; otherwise it's a concurrent GC so 
                        // simply add the pause time to the GC without making the GC complete.
                        if (_event.GCDurationMSec == 0)
                        {
                            Debug.Assert(_event.is20Event);
                            _event.isConcurrentGC = true;
                            stats.AddConcurrentPauseTime(_event, data.TimeStampRelativeMSec);
                        }
                        else
                        {
                            _event.PauseDurationMSec = data.TimeStampRelativeMSec - _event.PauseStartRelativeMSec;
                            if (_event.HeapStats != null)
                            {
                                _event.isComplete = true;
                                stats.lastCompletedGC = _event;
                            }
                        }
                    }
                }

                // We don't change between a GC end and the pause resume.   
                //Debug.Assert(stats.allocTickAtLastGC == stats.allocTickCurrentMB);
                // Mark that we are not in suspension anymore.  
                stats.suspendTimeRelativeMSec = -1;
                stats.suspendThreadIDOther = -1;
                stats.suspendThreadIDBGC = -1;
                stats.suspendThreadIDGC = -1;
            });

            source.Clr.AddCallbackForEvent("GC/AllocationTick", delegate (GCAllocationTickTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc.GetOrCreate(data.ProcessID);

                if (stats.HasAllocTickEvents == false)
                {
                    stats.HasAllocTickEvents = true;
                }

                double valueMB = data.GetAllocAmount(ref stats.SeenBadAllocTick) / 1000000.0;

                if (data.AllocationKind == GCAllocationKind.Small)
                {
                    // Would this do the right thing or is it always 0 for SOH since AllocationAmount 
                    // is an int??? 
                    stats.allocTickCurrentMB[0] += valueMB;
                }
                else
                {
                    stats.allocTickCurrentMB[1] += valueMB;
                }
            });

            source.Clr.AddCallbackForEvent("GC/Start", delegate (GCStartTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc.GetOrCreate(data.ProcessID);

                // We need to filter the scenario where we get 2 GCStart events for each GC.
                if ((stats.suspendThreadIDGC > 0) &&
                    !((stats.events.Count > 0) && stats.events[stats.events.Count - 1].GCNumber == data.Count))
                {
                    GCEvent _event = new GCEvent(stats);
                    Debug.Assert(0 <= data.Depth && data.Depth <= 2);
                    // _event.GCGeneration = data.Depth;   Old style events only have this in the GCStop event.  
                    _event.Reason = data.Reason;
                    _event.GCNumber = data.Count;
                    _event.Type = data.Type;
                    _event.Index = stats.events.Count;
                    _event.is20Event = data.IsClassicProvider;
                    bool isEphemeralGCAtBGCStart = false;
                    // Detecting the ephemeral GC that happens at the beginning of a BGC.
                    if (stats.events.Count > 0)
                    {
                        GCEvent lastGCEvent = stats.events[stats.events.Count - 1];
                        if ((lastGCEvent.Type == GCType.BackgroundGC) &&
                            (!lastGCEvent.isComplete) &&
                            (data.Type == GCType.NonConcurrentGC))
                        {
                            isEphemeralGCAtBGCStart = true;
                        }
                    }

                    Debug.Assert(stats.suspendTimeRelativeMSec != -1);
                    if (isEphemeralGCAtBGCStart)
                    {
                        _event.PauseStartRelativeMSec = data.TimeStampRelativeMSec;
                    }
                    else
                    {
                        _event.PauseStartRelativeMSec = stats.suspendTimeRelativeMSec;
                        if (stats.suspendEndTimeRelativeMSec == -1)
                        {
                            stats.suspendEndTimeRelativeMSec = data.TimeStampRelativeMSec;
                        }

                        _event._SuspendDurationMSec = stats.suspendEndTimeRelativeMSec - stats.suspendTimeRelativeMSec;
                    }

                    _event.GCStartRelativeMSec = data.TimeStampRelativeMSec;
                    stats.events.Add(_event);

                    if (_event.Type == GCType.BackgroundGC)
                    {
                        stats.currentBGC = _event;
                        _event.ProcessCpuAtLastGC = stats.ProcessCpuAtLastGC;
                    }

#if (!CAP)
                    if ((_event.Type != GCType.BackgroundGC) && (stats.isServerGCUsed == 1))
                    {
                        _event.SetUpServerGcHistory();
                        foreach (var s in RecentCpuSamples)
                            _event.AddServerGcSample(s);
                        foreach (var s in RecentThreadSwitches)
                            _event.AddServerGcThreadSwitch(s);
                    }
#endif 
                }
            });

            source.Clr.AddCallbackForEvent("GC/PinObjectAtGCTime", delegate (PinObjectAtGCTimeTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc.GetOrCreate(data.ProcessID);
                GCEvent _event = stats.GetCurrentGC();
                if (_event != null)
                {
                    if (!_event.PinnedObjects.ContainsKey(data.ObjectID))
                    {
                        _event.PinnedObjects.Add(data.ObjectID, data.ObjectSize);
                    }
                    else
                    {
                        _event.duplicatedPinningReports++;
                    }
                }
            });

            // Some builds have this as a public event, and some have it as a private event.
            // All will move to the private event, so we'll remove this code afterwards.
            source.Clr.AddCallbackForEvent("GC/PinPlugAtGCTime", delegate (PinPlugAtGCTimeTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc.GetOrCreate(data.ProcessID);
                GCEvent _event = stats.GetCurrentGC();
                if (_event != null)
                {
                    // ObjectID is supposed to be an IntPtr. But "Address" is defined as UInt64 in 
                    // TraceEvent.
                    _event.PinnedPlugs.Add(new GCEvent.PinnedPlug(data.PlugStart, data.PlugEnd));
                }
            });

            source.Clr.AddCallbackForEvent("GC/Mark", delegate (GCMarkWithTypeTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc.GetOrCreate(data.ProcessID);
                stats.AddServerGCThreadFromMark(data.ThreadID, data.HeapNum);

                GCEvent _event = stats.GetCurrentGC();
                if (_event != null)
                {
                    if (_event.PerHeapMarkTimes == null)
                    {
                        _event.PerHeapMarkTimes = new Dictionary<int, GCEvent.MarkInfo>();
                    }

                    if (!_event.PerHeapMarkTimes.ContainsKey(data.HeapNum))
                    {
                        _event.PerHeapMarkTimes.Add(data.HeapNum, new GCEvent.MarkInfo());
                    }

                    _event.PerHeapMarkTimes[data.HeapNum].MarkTimes[(int)data.Type] = data.TimeStampRelativeMSec;
                    _event.PerHeapMarkTimes[data.HeapNum].MarkPromoted[(int)data.Type] = data.Promoted;
                }
            });

            source.Clr.AddCallbackForEvent("GC/GlobalHeapHistory", delegate (GCGlobalHeapHistoryTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc.GetOrCreate(data.ProcessID);
                stats.ProcessGlobalHistory(data);
            });

            source.Clr.AddCallbackForEvent("GC/PerHeapHistory", delegate (GCPerHeapHistoryTraceData3 data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc.GetOrCreate(data.ProcessID);
                stats.ProcessPerHeapHistory(data);
            });

#if HAS_PRIVATE_GC_EVENTS
            // See if the source knows about the CLR Private provider, if it does, then 
            var gcPrivate = new ClrPrivateTraceEventParser(source);

            gcPrivate.GCPinPlugAtGCTime += delegate (PinPlugAtGCTimeTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc[data];
                GCEvent _event = stats.GetCurrentGC();
                if (_event != null)
                {
                    // ObjectID is supposed to be an IntPtr. But "Address" is defined as UInt64 in 
                    // TraceEvent.
                    _event.PinnedPlugs.Add(new GCEvent.PinnedPlug(data.PlugStart, data.PlugEnd));
                }
            };

            // Sometimes at the end of a trace I see only some mark events are included in the trace and they
            // are not in order, so need to anticipate that scenario.
            gcPrivate.GCMarkStackRoots += delegate (GCMarkTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc[data];
                stats.AddServerGCThreadFromMark(data.ThreadID, data.HeapNum);

                GCEvent _event = stats.GetCurrentGC();
                if (_event != null)
                {
                    if (_event.PerHeapMarkTimes == null)
                    {
                        _event.PerHeapMarkTimes = new Dictionary<int, GCEvent.MarkInfo>();
                    }

                    if (!_event.PerHeapMarkTimes.ContainsKey(data.HeapNum))
                    {
                        _event.PerHeapMarkTimes.Add(data.HeapNum, new GCEvent.MarkInfo(false));
                    }

                    _event.PerHeapMarkTimes[data.HeapNum].MarkTimes[(int)MarkRootType.MarkStack] = data.TimeStampRelativeMSec;
                }
            };

            gcPrivate.GCMarkFinalizeQueueRoots += delegate (GCMarkTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc[data];
                GCEvent _event = stats.GetCurrentGC();
                if (_event != null)
                {
                    if ((_event.PerHeapMarkTimes != null) && _event.PerHeapMarkTimes.ContainsKey(data.HeapNum))
                    {
                        _event.PerHeapMarkTimes[data.HeapNum].MarkTimes[(int)MarkRootType.MarkFQ] =
                            data.TimeStampRelativeMSec;
                    }
                }
            };

            gcPrivate.GCMarkHandles += delegate (GCMarkTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc[data];
                GCEvent _event = stats.GetCurrentGC();
                if (_event != null)
                {
                    if ((_event.PerHeapMarkTimes != null) && _event.PerHeapMarkTimes.ContainsKey(data.HeapNum))
                    {
                        _event.PerHeapMarkTimes[data.HeapNum].MarkTimes[(int)MarkRootType.MarkHandles] =
                           data.TimeStampRelativeMSec;
                    }
                }
            };

            gcPrivate.GCMarkCards += delegate (GCMarkTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc[data];
                GCEvent _event = stats.GetCurrentGC();
                if (_event != null)
                {
                    if ((_event.PerHeapMarkTimes != null) && _event.PerHeapMarkTimes.ContainsKey(data.HeapNum))
                    {
                        _event.PerHeapMarkTimes[data.HeapNum].MarkTimes[(int)MarkRootType.MarkOlder] =
                            data.TimeStampRelativeMSec;
                    }
                }
            };

            gcPrivate.GCGlobalHeapHistory += delegate (GCGlobalHeapHistoryTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc[data];
                stats.ProcessGlobalHistory(data);
            };

            gcPrivate.GCPerHeapHistory += delegate (GCPerHeapHistoryTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc[data];
                stats.ProcessPerHeapHistory(data);
            };

            gcPrivate.GCBGCStart += delegate (GCNoUserDataTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc[data];
                if (stats.currentBGC != null)
                {
                    if (stats.backgroundGCThreads == null)
                    {
                        stats.backgroundGCThreads = new Dictionary<int, object>(16);
                    }
                    stats.backgroundGCThreads[data.ThreadID] = null;
                }
            };
#endif

            source.Clr.AddCallbackForEvent("GC/Stop", delegate (GCEndTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc.GetOrCreate(data.ProcessID);
                GCEvent _event = stats.GetCurrentGC();
                if (_event != null)
                {
                    _event.GCDurationMSec = data.TimeStampRelativeMSec - _event.GCStartRelativeMSec;
                    _event.GCGeneration = data.Depth;
                    _event.GCEnd();
                }
            });

            source.Clr.AddCallbackForEvent("GC/HeapStats", delegate (GCHeapStatsTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc.GetOrCreate(data.ProcessID);
                GCEvent _event = stats.GetCurrentGC();

                var sizeAfterMB = (data.GenerationSize1 + data.GenerationSize2 + data.GenerationSize3) / 1000000.0;
                if (_event != null)
                {
                    _event.HeapStats = (GCHeapStatsTraceData)data.Clone();

                    if (_event.Type == GCType.BackgroundGC)
                    {
                        _event.ProcessCpuMSec = stats.ProcessCpuMSec - _event.ProcessCpuAtLastGC;
                        _event.DurationSinceLastRestartMSec = data.TimeStampRelativeMSec - stats.lastRestartEndTimeRelativeMSec;
                    }
                    else
                    {
                        _event.ProcessCpuMSec = stats.ProcessCpuMSec - stats.ProcessCpuAtLastGC;
                        _event.DurationSinceLastRestartMSec = _event.PauseStartRelativeMSec - stats.lastRestartEndTimeRelativeMSec;
                    }

                    if (stats.HasAllocTickEvents)
                    {
                        _event.HasAllocTickEvents = true;
                        _event.AllocedSinceLastGCBasedOnAllocTickMB[0] = stats.allocTickCurrentMB[0] - stats.allocTickAtLastGC[0];
                        _event.AllocedSinceLastGCBasedOnAllocTickMB[1] = stats.allocTickCurrentMB[1] - stats.allocTickAtLastGC[1];
                    }

                    // This is where a background GC ends.
                    if ((_event.Type == GCType.BackgroundGC) && (stats.currentBGC != null))
                    {
                        stats.currentBGC.isComplete = true;
                        stats.lastCompletedGC = stats.currentBGC;
                        stats.currentBGC = null;
                    }

                    if (_event.isConcurrentGC)
                    {
                        Debug.Assert(_event.is20Event);
                        _event.isComplete = true;
                        stats.lastCompletedGC = _event;
                    }
                }

                stats.ProcessCpuAtLastGC = stats.ProcessCpuMSec;
                stats.allocTickAtLastGC[0] = stats.allocTickCurrentMB[0];
                stats.allocTickAtLastGC[1] = stats.allocTickCurrentMB[1];
                stats.lastRestartEndTimeRelativeMSec = data.TimeStampRelativeMSec;
            });

            source.Clr.AddCallbackForEvent("GC/TerminateConcurrentThread", delegate (GCTerminateConcurrentThreadTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc.GetOrCreate(data.ProcessID);
                if (stats.backgroundGCThreads != null)
                {
                    stats.backgroundGCThreads = null;
                }
            });

#if HAS_PRIVATE_GC_EVENTS
            gcPrivate.GCBGCAllocWaitStart += delegate (BGCAllocWaitTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc[data];
                Debug.Assert(stats.currentBGC != null);

                if (stats.currentBGC != null)
                {
                    stats.currentBGC.AddLOHWaitThreadInfo(data.ThreadID, data.TimeStampRelativeMSec, data.Reason, true);
                }
            };

            gcPrivate.GCBGCAllocWaitStop += delegate (BGCAllocWaitTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess stats = perProc[data];

                GCEvent _event = stats.GetLastBGC();

                if (_event != null)
                {
                    _event.AddLOHWaitThreadInfo(data.ThreadID, data.TimeStampRelativeMSec, data.Reason, false);
                }
            };

            gcPrivate.GCJoin += delegate (GCJoinTraceData data)
            {
                if (filterFunc != null && !filterFunc.Invoke(data))
                {
                    return;
                }

                GCProcess gcProcess = perProc[data];
                GCEvent _event = gcProcess.GetCurrentGC();
                if (_event != null)
                {
                    _event.AddGcJoin(data);
                }
            };

            source.Process();
#endif
            return perProc;
        }

        public int ProcessID { get; set; }
        public string ProcessName { get; set; }
        bool isDead;
        public bool Interesting { get { return Total.GCCount > 0 || RuntimeVersion != null; } }
        public bool InterestingForAnalysis
        {
            get
            {
                return ((Total.GCCount > 3) &&
                        ((GetGCPauseTimePercentage() > 1.0) || (Total.MaxPauseDurationMSec > 200.0)));
            }
        }

        // A process can have one or more SBAs associated with it.
        public GCInfo[] Generations = new GCInfo[3] { new GCInfo(), new GCInfo(), new GCInfo() };
        public GCInfo Total = new GCInfo();
        public float ProcessCpuMSec;     // Total CPU time used in process (approximate)
        public float GCCpuMSec;          // CPU time used in the GC (approximate)
        public int NumberOfHeaps = 1;

        // Of all the CPU, how much as a percentage is spent in the GC. 
        public float PercentTimeInGC { get { return GCCpuMSec * 100 / ProcessCpuMSec; } }

        public static string GetImageName(string path)
        {
            int startIdx = path.LastIndexOf('\\');
            if (0 <= startIdx)
                startIdx++;
            else
                startIdx = 0;
            int endIdx = path.LastIndexOf('.');
            if (endIdx <= startIdx)
                endIdx = path.Length;
            string name = path.Substring(startIdx, endIdx - startIdx);
            return name;
        }

        // This is calculated based on the GC events which is fine for the GC analysis purpose.
        public double ProcessDuration
        {
            get
            {
                double startRelativeMSec = 0.0;

                for (int i = 0; i < events.Count; i++)
                {
                    if (events[i].isComplete)
                    {
                        startRelativeMSec = events[i].PauseStartRelativeMSec;
                        break;
                    }
                }

                if (startRelativeMSec == 0.0)
                    return 0;

                // Get the end time of the last GC.
                double endRelativeMSec = lastRestartEndTimeRelativeMSec;
                return (endRelativeMSec - startRelativeMSec);
            }
        }

        public StartupFlags StartupFlags;
        public string RuntimeVersion;
        public int Bitness = -1;
        public Dictionary<int, int> ThreadId2Priority = new Dictionary<int, int>();
        public Dictionary<int, int> ServerGcHeap2ThreadId = new Dictionary<int, int>();

        public string CommandLine { get; set; }

        public double PeakWorkingSetMB { get; set; }
        public double PeakVirtualMB { get; set; }

        /// <summary>
        /// Means it detected that the ETW information is in a format it does not understand.
        /// </summary>
        public bool GCVersionInfoMismatch { get; private set; }

        public double GetGCPauseTimePercentage()
        {
            return ((ProcessDuration == 0) ? 0.0 : ((Total.TotalPauseTimeMSec * 100) / ProcessDuration));
        }

        internal static void ComputeRollup(IDictionary<int, GCProcess> perProc)
        {
            // Compute rollup information.  
            foreach (GCProcess stats in perProc.Values)
            {
                for (int i = 0; i < stats.events.Count; i++)
                {
                    GCEvent _event = stats.events[i];
                    if (!_event.isComplete)
                    {
                        continue;
                    }

                    _event.Index = i;
                    if (_event.DetailedGenDataAvailable())  //per heap histories is not null
                        stats.m_detailedGCInfo = true;

                    // Update the per-generation information 
                    stats.Generations[_event.GCGeneration].GCCount++;
                    bool isInduced = ((_event.Reason == GCReason.Induced) || (_event.Reason == GCReason.InducedNotForced));
                    if (isInduced)
                        (stats.Generations[_event.GCGeneration].NumInduced)++;

                    long PinnedObjectSizes = _event.GetPinnedObjectSizes();
                    if (PinnedObjectSizes != 0)
                    {
                        stats.Generations[_event.GCGeneration].PinnedObjectSizes += PinnedObjectSizes;
                        stats.Generations[_event.GCGeneration].NumGCWithPinEvents++;
                    }

                    int PinnedObjectPercentage = _event.GetPinnedObjectPercentage();
                    if (PinnedObjectPercentage != -1)
                    {
                        stats.Generations[_event.GCGeneration].PinnedObjectPercentage += _event.GetPinnedObjectPercentage();
                        stats.Generations[_event.GCGeneration].NumGCWithPinPlugEvents++;
                    }

                    stats.Generations[_event.GCGeneration].TotalGCCpuMSec += _event.GetTotalGCTime();
                    stats.Generations[_event.GCGeneration].TotalSizeAfterMB += _event.HeapSizeAfterMB;

                    stats.Generations[_event.GCGeneration].TotalSizePeakMB += _event.HeapSizePeakMB;
                    stats.Generations[_event.GCGeneration].TotalPromotedMB += _event.PromotedMB;
                    stats.Generations[_event.GCGeneration].TotalPauseTimeMSec += _event.PauseDurationMSec;
                    stats.Generations[_event.GCGeneration].TotalAllocatedMB += _event.AllocedSinceLastGCMB;
                    stats.Generations[_event.GCGeneration].MaxPauseDurationMSec = Math.Max(stats.Generations[_event.GCGeneration].MaxPauseDurationMSec, _event.PauseDurationMSec);
                    stats.Generations[_event.GCGeneration].MaxSizePeakMB = Math.Max(stats.Generations[_event.GCGeneration].MaxSizePeakMB, _event.HeapSizePeakMB);
                    stats.Generations[_event.GCGeneration].MaxAllocRateMBSec = Math.Max(stats.Generations[_event.GCGeneration].MaxAllocRateMBSec, _event.AllocRateMBSec);
                    stats.Generations[_event.GCGeneration].MaxPauseDurationMSec = Math.Max(stats.Generations[_event.GCGeneration].MaxPauseDurationMSec, _event.PauseDurationMSec);
                    stats.Generations[_event.GCGeneration].MaxSuspendDurationMSec = Math.Max(stats.Generations[_event.GCGeneration].MaxSuspendDurationMSec, _event._SuspendDurationMSec);

                    // And the totals 
                    stats.Total.GCCount++;
                    if (isInduced)
                        stats.Total.NumInduced++;
                    if (PinnedObjectSizes != 0)
                    {
                        stats.Total.PinnedObjectSizes += PinnedObjectSizes;
                        stats.Total.NumGCWithPinEvents++;
                    }
                    if (PinnedObjectPercentage != -1)
                    {
                        stats.Total.PinnedObjectPercentage += _event.GetPinnedObjectPercentage();
                        stats.Total.NumGCWithPinPlugEvents++;
                    }
                    stats.Total.TotalGCCpuMSec += _event.GetTotalGCTime();
                    stats.Total.TotalSizeAfterMB += _event.HeapSizeAfterMB;
                    stats.Total.TotalPromotedMB += _event.PromotedMB;
                    stats.Total.TotalSizePeakMB += _event.HeapSizePeakMB;
                    stats.Total.TotalPauseTimeMSec += _event.PauseDurationMSec;
                    stats.Total.TotalAllocatedMB += _event.AllocedSinceLastGCMB;
                    stats.Total.MaxPauseDurationMSec = Math.Max(stats.Total.MaxPauseDurationMSec, _event.PauseDurationMSec);
                    stats.Total.MaxSizePeakMB = Math.Max(stats.Total.MaxSizePeakMB, _event.HeapSizePeakMB);
                    stats.Total.MaxAllocRateMBSec = Math.Max(stats.Total.MaxAllocRateMBSec, _event.AllocRateMBSec);
                    stats.Total.MaxSuspendDurationMSec = Math.Max(stats.Total.MaxSuspendDurationMSec, _event._SuspendDurationMSec);
                }
            }
        }

#region private
        protected virtual void Init(TraceEvent data)
        {
            ProcessID = data.ProcessID;
            ProcessName = data.ProcessName;
            isDead = false;
        }
        private GCEvent GetCurrentGC()
        {
            if (events.Count > 0)
            {
                if (!events[events.Count - 1].isComplete)
                {
                    return events[events.Count - 1];
                }
                else if (currentBGC != null)
                {
                    return currentBGC;
                }
            }

            return null;
        }

        // This is the last GC in progress. We need this for server Background GC.
        // See comments for lastCompletedGC.
        private GCEvent GetLastGC()
        {
            GCEvent _event = GetCurrentGC();
            if ((isServerGCUsed == 1) &&
                (_event == null))
            {
                if (lastCompletedGC != null)
                {
                    Debug.Assert(lastCompletedGC.Type == GCType.BackgroundGC);
                    _event = lastCompletedGC;
                }
            }

            return _event;
        }

        private GCEvent GetLastBGC()
        {
            if (currentBGC != null)
            {
                return currentBGC;
            }

            if ((lastCompletedGC != null) && (lastCompletedGC.Type == GCType.BackgroundGC))
            {
                return lastCompletedGC;
            }

            // Otherwise we search till we find the last BGC if we have seen one.
            for (int i = (events.Count - 1); i >= 0; i--)
            {
                if (events[i].Type == GCType.BackgroundGC)
                {
                    return events[i];
                }
            }

            return null;
        }

        private void AddConcurrentPauseTime(GCEvent _event, double RestartEEMSec)
        {
            if (suspendThreadIDBGC > 0)
            {
                _event.PauseDurationMSec += RestartEEMSec - suspendTimeRelativeMSec;
            }
            else
            {
                Debug.Assert(_event.PauseDurationMSec == 0);
                _event.PauseDurationMSec = RestartEEMSec - _event.PauseStartRelativeMSec;
            }
        }

        private void AddServerGCThreadFromMark(int ThreadID, int HeapNum)
        {
            if (isServerGCUsed == 1)
            {
                Debug.Assert(heapCount > 1);

                if (serverGCThreads.Count < heapCount)
                {
                    // I am seeing that sometimes we are not getting these events from all heaps
                    // for a complete GC so I have to check for that.
                    if (!serverGCThreads.ContainsKey(ThreadID))
                    {
                        serverGCThreads.Add(ThreadID, HeapNum);
                    }
                }
            }
        }

        private void ProcessGlobalHistory(GCGlobalHeapHistoryTraceData data)
        {
            if (isServerGCUsed == -1)
            {
                // We detected whether we are using Server GC now.
                isServerGCUsed = ((data.NumHeaps > 1) ? 1 : 0);
                if (heapCount == -1)
                {
                    heapCount = data.NumHeaps;
                }

                if (isServerGCUsed == 1)
                {
                    serverGCThreads = new Dictionary<int, int>(data.NumHeaps);
                }
            }

            GCEvent _event = GetLastGC();
            if (_event != null)
            {
                _event.GlobalHeapHistory = (GCGlobalHeapHistoryTraceData)data.Clone();
                _event.SetHeapCount(heapCount);
            }
        }

        private void ProcessPerHeapHistory(GCPerHeapHistoryTraceData data)
        {
            if (!data.VersionRecognized)
            {
                GCVersionInfoMismatch = true;
                return;
            }

            GCEvent _event = GetLastGC();
            if (_event != null)
            {
                if (_event.PerHeapHistories == null)
                    _event.PerHeapHistories = new List<GCPerHeapHistoryTraceData>();
                _event.PerHeapHistories.Add((GCPerHeapHistoryTraceData)data.Clone());
            }
        }

        public IList<GCEvent> Events
        {
            get
            {
                return events;
            }
        }

        private List<GCEvent> events = new List<GCEvent>();

        // The amount of memory allocated by the user threads. So they are divided up into gen0 and LOH allocations.
        double[] allocTickCurrentMB = { 0.0, 0.0 };
        double[] allocTickAtLastGC = { 0.0, 0.0 };
        bool HasAllocTickEvents = false;
        bool SeenBadAllocTick = false;

        double lastRestartEndTimeRelativeMSec;

        // EE can be suspended via different reasons. The only ones we care about are
        // SuspendForGC(1) - suspending for GC start
        // SuspendForGCPrep(6) - BGC uses it in the middle of a BGC.
        // We need to filter out the rest of the suspend/resume events.
        // Keep track of the last time we started suspending the EE.  Will use in 'Start' to set PauseStartRelativeMSec
        int suspendThreadIDOther = -1;
        int suspendThreadIDBGC = -1;
        // This is either the user thread (in workstation case) or a server GC thread that called SuspendEE to do a GC
        int suspendThreadIDGC = -1;
        double suspendTimeRelativeMSec = -1;
        double suspendEndTimeRelativeMSec = -1;

        // This records the amount of CPU time spent at the end of last GC.
        float ProcessCpuAtLastGC = 0;

        // This is the BGC that's in progress as we are parsing. We need to remember this 
        // so we can correctly attribute the suspension time.
        GCEvent currentBGC = null;
        Dictionary<int, object> backgroundGCThreads = null;
        bool IsBGCThread(int threadID)
        {
            if (backgroundGCThreads != null)
                return backgroundGCThreads.ContainsKey(threadID);
            return false;
        }

        // I keep this for the purpose of server Background GC. Unfortunately for server background 
        // GC we are firing the GCEnd/GCHeaps events and Global/Perheap events in the reversed order.
        // This is so that the Global/Perheap events can still be attributed to the right BGC.
        GCEvent lastCompletedGC = null;
        // We don't necessarily have the GCSettings event (only fired at the beginning if we attach)
        // So we have to detect whether we are running server GC or not.
        // Till we get our first GlobalHeapHistory event which indicates whether we use server GC 
        // or not this remains -1.
        public int isServerGCUsed = -1;
        public int heapCount = -1;
        // This is the server GC threads. It's built up in the 2nd server GC we see. 
        Dictionary<int, int> serverGCThreads = null;


        internal bool m_detailedGCInfo;
        int IsServerGCThread(int threadID)
        {
            int heapIndex;
            if (serverGCThreads != null)
            {
                if (serverGCThreads.TryGetValue(threadID, out heapIndex))
                {
                    return heapIndex;
                }
            }
            return -1;
        }
#endregion
    }

#if HAS_PRIVATE_GC_EVENTS
    class BGCAllocWaitInfo
    {
        public double WaitStartRelativeMSec;
        public double WaitStopRelativeMSec;
        public BGCAllocWaitReason Reason;

        public bool GetWaitTime(ref double pauseMSec)
        {
            if ((WaitStartRelativeMSec != 0) &&
                (WaitStopRelativeMSec != 0))
            {
                pauseMSec = WaitStopRelativeMSec - WaitStartRelativeMSec;
                return true;
            }
            return false;
        }

        public bool IsLOHWaitLong(double pauseMSecMin)
        {
            double pauseMSec = 0;
            if (GetWaitTime(ref pauseMSec))
            {
                return (pauseMSec > pauseMSecMin);
            }
            return false;
        }

        public override string ToString()
        {
            if ((Reason == BGCAllocWaitReason.GetLOHSeg) ||
                (Reason == BGCAllocWaitReason.AllocDuringSweep))
            {
                return "Waiting for BGC to thread free lists";
            }
            else
            {
                Debug.Assert(Reason == BGCAllocWaitReason.AllocDuringBGC);
                return "Allocated too much during BGC, waiting for BGC to finish";
            }
        }
    }
#endif
}

#endif
