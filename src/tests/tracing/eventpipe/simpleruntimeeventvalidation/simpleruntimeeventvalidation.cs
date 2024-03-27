// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.NETCore.Client;
using Tracing.Tests.Common;
using Xunit;

namespace Tracing.Tests.SimpleRuntimeEventValidation
{
    public class RuntimeEventValidation
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // This test validates GC and Exception events in the runtime
            var ret = IpcTraceTest.RunAndValidateEventCounts(
                // Validation is done with _DoesTraceContainEvents
                new Dictionary<string, ExpectedEventCount>(){{ "Microsoft-Windows-DotNETRuntime", -1 }},
                _eventGeneratingActionForGC,
                //GCKeyword (0x1): 0b1
                new List<EventPipeProvider>(){new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, 0b1)},
                1024, _DoesTraceContainGCEvents, enableRundownProvider:false);
            if (ret != 100)
                return ret;

            // Run the 2nd test scenario only if the first one passes
            ret = IpcTraceTest.RunAndValidateEventCounts(
                new Dictionary<string, ExpectedEventCount>(){{ "Microsoft-DotNETCore-EventPipe", 1 }},
                _eventGeneratingActionForExceptions,
                //ExceptionKeyword (0x8000): 0b1000_0000_0000_0000
                new List<EventPipeProvider>(){new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Warning, 0b1000_0000_0000_0000)},
                1024, _DoesTraceContainExceptionEvents, enableRundownProvider:false);
            if (ret != 100)
                return ret;

            ret = IpcTraceTest.RunAndValidateEventCounts(
                new Dictionary<string, ExpectedEventCount>(){{ "Microsoft-Windows-DotNETRuntime", -1}},
                _eventGeneratingActionForFinalizers,
                new List<EventPipeProvider>(){new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, 0b1)},
                1024, _DoesTraceContainFinalizerEvents, enableRundownProvider:false);
            if (ret != 100)
                return ret;

            // check that AllocationSampled events are generated and size and type name are correct
            ret = IpcTraceTest.RunAndValidateEventCounts(
                new Dictionary<string, ExpectedEventCount>() { { "Microsoft-Windows-DotNETRuntime", -1 } },
                _eventGeneratingActionForAllocations,                                                                           // AllocationSamplingKeyword
                new List<EventPipeProvider>() { new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, 0b1000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000) },
                1024, _DoesTraceContainAllocationSampledEvents, enableRundownProvider: false);
            if (ret != 100)
                return ret;

            return 100;
        }

        private static Action _eventGeneratingActionForGC = () =>
        {
            for (int i = 0; i < 50; i++)
            {
                if (i % 10 == 0)
                    Logger.logger.Log($"Called GC.Collect() {i} times...");
                RuntimeEventValidation eventValidation = new RuntimeEventValidation();
                eventValidation = null;
                GC.Collect();
            }
        };

        private static Action _eventGeneratingActionForExceptions = () =>
        {
            for (int i = 0; i < 10; i++)
            {
                if (i % 5 == 0)
                    Logger.logger.Log($"Thrown an exception {i} times...");
                try
                {
                    throw new ArgumentNullException("Throw ArgumentNullException");
                }
                catch (Exception e)
                {
                    //Do nothing
                }
            }
        };

        private static Action _eventGeneratingActionForFinalizers = () =>
        {
            for (int i = 0; i < 50; i++)
            {
                if (i % 10 == 0)
                    Logger.logger.Log($"Called GC.WaitForPendingFinalizers() {i} times...");
                GC.WaitForPendingFinalizers();
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        };


        // 2000 instances of 1KB byte arrays should trigger between 10 and 20 events with the default 1/100KB sampling rate
        const int InstanceCount = 2000;
        const int MinExpectedEvents = 15;

        // allocate objects to trigger dynamic allocation sampling events
        private static Action _eventGeneratingActionForAllocations = () =>
        {
            int allocatedSize = 0;
            for (int i = 0; i < InstanceCount; i++)
            {
                if ((i != 0) && (i % InstanceCount/5 == 0))
                    Logger.logger.Log($"Allocated {i* InstanceCount / 5} instances {i} times...");

                byte[] bytes = new byte[1024];
                allocatedSize += bytes.Length;
            }
            Logger.logger.Log($"{InstanceCount} instances allocated");
        };

        private static Func<EventPipeEventSource, Func<int>> _DoesTraceContainGCEvents = (source) =>
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

        private static Func<EventPipeEventSource, Func<int>> _DoesTraceContainExceptionEvents = (source) =>
        {
            int ExStartEvents = 0;
            source.Clr.ExceptionStart += (eventData) =>
            {
                if(eventData.ToString().IndexOf("System.ArgumentNullException")>=0)
                    ExStartEvents += 1;
            };

            return () => {
                Logger.logger.Log("Exception Event counts validation");
                Logger.logger.Log("ExStartEvents: " + ExStartEvents);
                bool ExStartResult = ExStartEvents >= 10;

                return ExStartResult ? 100 : -1;
            };
        };

        private static Func<EventPipeEventSource, Func<int>> _DoesTraceContainFinalizerEvents = (source) =>
        {
            int GCFinalizersEndEvents = 0;
            source.Clr.GCFinalizersStop += (eventData) => GCFinalizersEndEvents += 1;
            int GCFinalizersStartEvents = 0;
            source.Clr.GCFinalizersStart += (eventData) => GCFinalizersStartEvents += 1;
            return () => {
                Logger.logger.Log("Event counts validation");
                Logger.logger.Log("GCFinalizersEndEvents: " + GCFinalizersEndEvents);
                Logger.logger.Log("GCFinalizersStartEvents: " + GCFinalizersStartEvents);
                return GCFinalizersEndEvents >= 50 && GCFinalizersStartEvents >= 50 ? 100 : -1;
            };
        };

        private static Func<EventPipeEventSource, Func<int>> _DoesTraceContainAllocationSampledEvents = (source) =>
        {
            int AllocationSampledEvents = 0;
            int ArrayOfBytesCount = 0;
            source.Dynamic.All += (eventData) =>
            {
                if (eventData.ID == (TraceEventID)303)  // AllocationSampled is not defined in TraceEvent yet
                {
                    AllocationSampledEvents++;

                    AllocationSampledData payload = new AllocationSampledData(eventData, source.PointerSize);
                    // uncomment to see the allocation events payload
                    //Logger.logger.Log($"{payload.HeapIndex} - {payload.AllocationKind} | ({payload.ObjectSize}) {payload.TypeName}  = 0x{payload.Address}");
                    if (payload.TypeName == "System.Byte[]")
                    {
                        ArrayOfBytesCount++;
                    }
                }
            };
            return () => {
                Logger.logger.Log("AllocationSampled counts validation");
                Logger.logger.Log("Nb events: " + AllocationSampledEvents);
                Logger.logger.Log("Nb byte[]: " + ArrayOfBytesCount);
                return (AllocationSampledEvents >= MinExpectedEvents) && (ArrayOfBytesCount != 0) ? 100 : -1;
            };
        };
    }

    // AllocationSampled is not defined in TraceEvent yet
    //
    //  <data name="AllocationKind" inType="win:UInt32" map="GCAllocationKindMap" />
    //  <data name="ClrInstanceID" inType="win:UInt16" />
    //  <data name="TypeID" inType="win:Pointer" />
    //  <data name="TypeName" inType="win:UnicodeString" />
    //  <data name="HeapIndex" inType="win:UInt32" />
    //  <data name="Address" inType="win:Pointer" />
    //  <data name="ObjectSize" inType="win:UInt64" outType="win:HexInt64" />
    //
    class AllocationSampledData
    {
        const int EndOfStringCharLength = 2;
        private TraceEvent _payload;
        private int _pointerSize;
        public AllocationSampledData(TraceEvent payload, int pointerSize)
        {
            _payload = payload;
            _pointerSize = pointerSize;
            TypeName = "?";

            ComputeFields();
        }

        public GCAllocationKind AllocationKind;
        public int ClrInstanceID;
        public UInt64 TypeID;
        public string TypeName;
        public int HeapIndex;
        public UInt64 Address;
        public long ObjectSize;

        private void ComputeFields()
        {
            int offsetBeforeString = 4 + 2 + _pointerSize;

            Span<byte> data = _payload.EventData().AsSpan();
            AllocationKind = (GCAllocationKind)BitConverter.ToInt32(data.Slice(0, 4));
            ClrInstanceID = BitConverter.ToInt16(data.Slice(4, 2));
            TypeID = BitConverter.ToUInt64(data.Slice(6, _pointerSize));                                                    //   \0 should not be included for GetString to work
            TypeName = Encoding.Unicode.GetString(data.Slice(offsetBeforeString, _payload.EventDataLength - offsetBeforeString - EndOfStringCharLength - 4 - _pointerSize - 8));
            HeapIndex = BitConverter.ToInt32(data.Slice(offsetBeforeString + TypeName.Length * 2 + EndOfStringCharLength, 4));
            Address = BitConverter.ToUInt64(data.Slice(offsetBeforeString + TypeName.Length * 2 + EndOfStringCharLength + 4, _pointerSize));
            ObjectSize = BitConverter.ToInt64(data.Slice(offsetBeforeString + TypeName.Length * 2 + EndOfStringCharLength + 4 + 8, 8));
        }
    }
}
