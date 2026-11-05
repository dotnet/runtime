// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.EventPipe;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using TestLibrary;
using Tracing.Tests.Common;
using Microsoft.Diagnostics.NETCore.Client;
using Xunit;

namespace Tracing.Tests.GCEvents
{
    public class ProviderValidation
    {
        [Fact]
        public static int TestEntryPoint()
        {
            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose),
                //GCKeyword (0x1): 0b1
                new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, 0b1)
            };

            return IpcTraceTest.RunAndValidateEventCounts(_expectedEventCounts, _eventGeneratingAction, providers, 1024, _DoesTraceContainEvents);
        }

        [ConditionalFact(typeof(Utilities), nameof(Utilities.IsNotMonoRuntime), nameof(Utilities.IsNotNativeAot))]
        public static int SampledStacksOmitGCPollHelpers()
        {
            using var eventSource = new PollingEventSource();
            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider(SampleProfilerTraceEventParser.ProviderName, EventLevel.Verbose),
                new EventPipeProvider(ClrTraceEventParser.ProviderName, EventLevel.Verbose, (long)ClrTraceEventParser.Keywords.Jit),
                new EventPipeProvider(eventSource.Name, EventLevel.Verbose)
            };
            var expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
            {
                { ClrRundownTraceEventParser.ProviderName, -1 },
                { eventSource.Name, 1 }
            };

            return IpcTraceTest.RunAndValidateEventCounts(
                expectedEventCounts,
                () => PollGCWorker(eventSource),
                providers,
                32,
                source =>
                {
                    var pollingMethods = new List<(ulong Start, uint Size)>();
                    var workloadMethods = new List<(ulong Start, uint Size)>();
                    var sampleTopIPs = new List<ulong>();
                    var managedSampleIPs = new HashSet<ulong>();
                    var markerIPs = new HashSet<ulong>();
                    bool sawPollingWorker = false;

                    // Retain live code ranges as well as rundown ranges to cover every sampled tier.
                    source.Clr.MethodLoadVerbose += AddMethod;
                    var rundownParser = new ClrRundownTraceEventParser(source);
                    rundownParser.MethodDCStopVerbose += AddMethod;
                    var sampleParser = new SampleProfilerTraceEventParser(source);
                    sampleParser.ThreadSample += data =>
                    {
                        ulong[] frames = RawEventPipeStack.Read(data);
                        sampleTopIPs.Add(frames[0]);
                        if (data.Type == ClrThreadSampleType.Managed)
                        {
                            managedSampleIPs.UnionWith(frames);
                        }
                    };
                    source.Dynamic.All += data =>
                    {
                        if (data.ProviderName == eventSource.Name)
                        {
                            markerIPs.UnionWith(RawEventPipeStack.Read(data));
                        }
                    };

                    return () =>
                    {
                        Assert.Equal(0, source.EventsLost);
                        Assert.True(sawPollingWorker, "JIT/rundown events must describe the native GC polling worker.");
                        Assert.NotEmpty(workloadMethods);
                        Assert.NotEmpty(sampleTopIPs);
                        foreach (ulong ip in sampleTopIPs)
                        {
                            Assert.False(ContainsIP(pollingMethods, ip), $"Unexpected GC polling frame at the top of a sample: 0x{ip:x}");
                        }

                        Assert.Contains(managedSampleIPs, ip => ContainsIP(workloadMethods, ip));
                        Assert.Contains(markerIPs, ip => ContainsIP(workloadMethods, ip));
                        Logger.logger.Log($"Sample stacks: {sampleTopIPs.Count}, polling code ranges: {pollingMethods.Count}, workload code ranges: {workloadMethods.Count}");
                        return 100;
                    };

                    void AddMethod(MethodLoadUnloadVerboseTraceData data)
                    {
                        bool isPollingMethod = data.MethodNamespace == "System.Threading.Thread" &&
                            (data.MethodName is "PollGC" or "PollGCWorker" ||
                             data.MethodName.StartsWith("<PollGC>g__PollGCWorker|", StringComparison.Ordinal));
                        bool isWorkloadMethod = data.MethodNamespace == typeof(ProviderValidation).FullName &&
                            data.MethodName == nameof(PollGCWorker);
                        if (isPollingMethod || isWorkloadMethod)
                        {
                            Assert.True(data.MethodSize > 0);
                            (isPollingMethod ? pollingMethods : workloadMethods).Add((data.MethodStartAddress, (uint)data.MethodSize));
                            sawPollingWorker |= isPollingMethod && data.MethodName != "PollGC";
                        }
                    }
                });

            static bool ContainsIP(List<(ulong Start, uint Size)> methods, ulong ip)
            {
                return methods.Exists(method => ip >= method.Start && ip - method.Start < method.Size);
            }
        }

        private static class RawEventPipeStack
        {
            private static readonly FieldInfo s_eventRecord = GetField(typeof(TraceEvent), "eventRecord");
            private static readonly int s_extendedDataCountOffset;
            private static readonly int s_extendedDataOffset;
            private static readonly int s_itemSize;
            private static readonly int s_typeOffset;
            private static readonly int s_sizeOffset;
            private static readonly int s_pointerOffset;
            private static readonly ushort s_stackTrace32;
            private static readonly ushort s_stackTrace64;

            static RawEventPipeStack()
            {
                Type recordType = GetPointedType(s_eventRecord);
                Type itemType = GetPointedType(GetField(recordType, "ExtendedData"));
                s_extendedDataCountOffset = GetFieldOffset(recordType, "ExtendedDataCount", typeof(ushort));
                s_extendedDataOffset = (int)Marshal.OffsetOf(recordType, "ExtendedData");
                s_itemSize = Marshal.SizeOf(itemType);
                s_typeOffset = GetFieldOffset(itemType, "ExtType", typeof(ushort));
                s_sizeOffset = GetFieldOffset(itemType, "DataSize", typeof(ushort));
                s_pointerOffset = GetFieldOffset(itemType, "DataPtr", typeof(ulong));
                Type nativeTypes = typeof(TraceEvent).Assembly.GetType("Microsoft.Diagnostics.Tracing.TraceEventNativeMethods", throwOnError: true);
                s_stackTrace32 = Convert.ToUInt16(GetField(nativeTypes, "EVENT_HEADER_EXT_TYPE_STACK_TRACE32").GetRawConstantValue());
                s_stackTrace64 = Convert.ToUInt16(GetField(nativeTypes, "EVENT_HEADER_EXT_TYPE_STACK_TRACE64").GetRawConstantValue());
            }

            public static unsafe ulong[] Read(TraceEvent data)
            {
                // TraceEvent owns these buffers only for the duration of the event callback.
                IntPtr record = (IntPtr)Pointer.Unbox(s_eventRecord.GetValue(data));
                Assert.NotEqual(IntPtr.Zero, record);
                ushort count = unchecked((ushort)Marshal.ReadInt16(record, s_extendedDataCountOffset));
                IntPtr items = Marshal.ReadIntPtr(record, s_extendedDataOffset);
                if (count > 0)
                {
                    Assert.NotEqual(IntPtr.Zero, items);
                }

                for (int i = 0; i < count; i++)
                {
                    IntPtr item = IntPtr.Add(items, checked(i * s_itemSize));
                    ushort type = unchecked((ushort)Marshal.ReadInt16(item, s_typeOffset));
                    if (type != s_stackTrace32 && type != s_stackTrace64)
                    {
                        continue;
                    }

                    int pointerSize = type == s_stackTrace32 ? sizeof(uint) : sizeof(ulong);
                    int size = unchecked((ushort)Marshal.ReadInt16(item, s_sizeOffset));
                    // ETW-compatible stack attachments begin with a 64-bit MatchId.
                    const int HeaderSize = sizeof(ulong);
                    Assert.True(size > HeaderSize && (size - HeaderSize) % pointerSize == 0, $"Invalid raw stack attachment size: {size}");
                    ulong address = unchecked((ulong)Marshal.ReadInt64(item, s_pointerOffset));
                    IntPtr stack = (IntPtr)checked((nuint)address);
                    Assert.NotEqual(IntPtr.Zero, stack);
                    var frames = new ulong[(size - HeaderSize) / pointerSize];
                    for (int frame = 0; frame < frames.Length; frame++)
                    {
                        int offset = HeaderSize + frame * pointerSize;
                        frames[frame] = pointerSize == sizeof(uint)
                            ? unchecked((uint)Marshal.ReadInt32(stack, offset))
                            : unchecked((ulong)Marshal.ReadInt64(stack, offset));
                    }

                    return frames;
                }

                throw new InvalidOperationException($"No raw stack attachment found for {data.ProviderName}/{data.EventName}.");
            }

            private static int GetFieldOffset(Type type, string name, Type fieldType)
            {
                Assert.Equal(fieldType, GetField(type, name).FieldType);
                return (int)Marshal.OffsetOf(type, name);
            }

            private static FieldInfo GetField(Type type, string name) =>
                type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                    throw new MissingFieldException(type.FullName, name);

            private static Type GetPointedType(FieldInfo field)
            {
                if (!field.FieldType.IsPointer)
                {
                    throw new InvalidOperationException($"Expected a pointer field in TraceEvent: {field}");
                }

                return field.FieldType.GetElementType();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void PollGCWorker(PollingEventSource eventSource)
        {
            object[] source = new object[128];
            object[] destination = new object[128];
            eventSource.Marker();
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < 3000)
            {
                source.AsSpan().CopyTo(destination);
            }
        }

        [EventSource(Name = "SampleProfiler-PollGC")]
        private sealed class PollingEventSource : EventSource
        {
            [Event(1)]
            public void Marker() => WriteEvent(1);
        }

        private static Dictionary<string, ExpectedEventCount> _expectedEventCounts = new Dictionary<string, ExpectedEventCount>()
        {
            { "Microsoft-Windows-DotNETRuntime", -1 },
            { "Microsoft-Windows-DotNETRuntimeRundown", -1 },
            { "Microsoft-DotNETCore-SampleProfiler", -1 }
        };

        private static Action _eventGeneratingAction = () =>
        {
            for (int i = 0; i < 50; i++)
            {
                if (i % 10 == 0)
                    Logger.logger.Log($"Called GC.Collect() {i} times...");
                ProviderValidation providerValidation = new ProviderValidation();
                providerValidation = null;
                GC.Collect();
            }
        };

        private static Func<EventPipeEventSource, Func<int>> _DoesTraceContainEvents = (source) =>
        {
            int GCStartEvents = 0;
            int GCEndEvents = 0;
            source.Clr.GCStart += (eventData) => GCStartEvents += 1;
            source.Clr.GCStop += (eventData) => GCEndEvents += 1;

            int GCRestartEEStartEvents = 0;
            int GCRestartEEStopEvents = 0;
            source.Clr.GCRestartEEStart += (eventData) => GCRestartEEStartEvents += 1;
            source.Clr.GCRestartEEStop += (eventData) => GCRestartEEStopEvents += 1;

            int GCSuspendEEEvents = 0;
            int GCSuspendEEEndEvents = 0;
            source.Clr.GCSuspendEEStart += (eventData) => GCSuspendEEEvents += 1;
            source.Clr.GCSuspendEEStop += (eventData) => GCSuspendEEEndEvents += 1;

            return () => {
                Logger.logger.Log("Event counts validation");

                Logger.logger.Log("GCStartEvents: " + GCStartEvents);
                Logger.logger.Log("GCEndEvents: " + GCEndEvents);
                bool GCStartStopResult = GCStartEvents >= 50 && GCEndEvents >= 50 && Math.Abs(GCStartEvents - GCEndEvents) <=2;
                Logger.logger.Log("GCStartStopResult check: " + GCStartStopResult);

                Logger.logger.Log("GCRestartEEStartEvents: " + GCRestartEEStartEvents);
                Logger.logger.Log("GCRestartEEStopEvents: " + GCRestartEEStopEvents);
                bool GCRestartEEStartStopResult = GCRestartEEStartEvents >= 50 && GCRestartEEStopEvents >= 50;
                Logger.logger.Log("GCRestartEEStartStopResult check: " + GCRestartEEStartStopResult);

                Logger.logger.Log("GCSuspendEEEvents: " + GCSuspendEEEvents);
                Logger.logger.Log("GCSuspendEEEndEvents: " + GCSuspendEEEndEvents);
                bool GCSuspendEEStartStopResult = GCSuspendEEEvents >= 50 && GCSuspendEEEndEvents >= 50;
                Logger.logger.Log("GCSuspendEEStartStopResult check: " + GCSuspendEEStartStopResult);

                return GCStartStopResult && GCRestartEEStartStopResult && GCSuspendEEStartStopResult ? 100 : -1;
            };
        };
    }
}
