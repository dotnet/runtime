// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

// These are not supported to be automatically executed: look at README.md
namespace Tracing.Tests.Manual
{
    public class DynamicSamplingValidation
    {
        // variables used to compare results of different tests
        static long _warmupDuration = 0;
        static long _noSamplingDuration = 0;
        static long _allocationTickDuration = 0;
        static long _allocationSampledDuration = 0;

        [Fact]
        public static int TestEntryPoint()
        {
            _arrays = new List<byte[]>(ArrayInstanceCount);

            // measure impact of AllocationTick and AllocationSampled
            var ret = RunAllocationSamplers();
            if (ret != 100)
                return ret;

            return 100;
        }

        private static int RunAllocationSamplers()
        {
            // warm up the allocator 
            Stopwatch clock = new Stopwatch();
            clock.Start();
            int allocatedSize = AllocateObjects(ArrayInstanceCount);
            clock.Stop();
            var duration = clock.ElapsedMilliseconds;
            Logger.logger.Log($" #GCs: {GC.CollectionCount(0)}");
            Logger.logger.Log($" Warmup: {ArrayInstanceCount} instances allocated for {allocatedSize} bytes in {duration} ms");
            _warmupDuration = duration;
            Logger.logger.Log("-");

            // trigger GC to avoid impacting the measurements
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

            // run the same allocations test with no sampling, AllocationTick and AllocationSampled
            clock = new Stopwatch();
            clock.Start();
            allocatedSize = AllocateObjects(ArrayInstanceCount);
            clock.Stop();
             duration = clock.ElapsedMilliseconds;
            Logger.logger.Log($" #GCs: {GC.CollectionCount(0)}");
            Logger.logger.Log($" No Sampling: {ArrayInstanceCount} instances allocated for {allocatedSize} bytes in {duration} ms");
            _noSamplingDuration = duration;
            Logger.logger.Log("-");

            // trigger GC to avoid impacting the measurements
            var ret = IpcTraceTest.RunAndValidateEventCounts(
                new Dictionary<string, ExpectedEventCount>() { { "Microsoft-Windows-DotNETRuntime", -1 } },
                _eventGeneratingActionForAllocationsWithAllocationTick,
                // GCKeyword (0x1): 0b1
                new List<EventPipeProvider>() { new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Verbose, 0b1) },
                1024, _DoesTraceContainAllocationTickEvents, enableRundownProvider: false);
            if (ret != 100)
                return ret;
            Logger.logger.Log("-");

            // trigger GC to avoid impacting the measurements
            ret = IpcTraceTest.RunAndValidateEventCounts(
                new Dictionary<string, ExpectedEventCount>() { { "Microsoft-Windows-DotNETRuntime", -1 } },
                _eventGeneratingActionForAllocationsWithAllocationSampled,
                // AllocationSamplingKeyword (0x80000000000): 0b1000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000
                new List<EventPipeProvider>() { new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, 0x80000000000) },
                1024, _DoesTraceContainAllocationSampledEvents, enableRundownProvider: false);
            if (ret != 100)
                return ret;

            return 100;
        }

        const int ArrayInstanceCount = 100000;
        static List<byte[]> _arrays;

        private static int AllocateObjects(int instanceCount)
        {
            _arrays.Clear();
            int size = 0;
            for (int i = 0; i < instanceCount; i++)
            {
                byte[] bytes = new byte[35];
                size += bytes.Length;
                _arrays.Add(bytes);
            }

            Logger.logger.Log($"{_arrays.Count} allocated arrays");
            return size;
        }

        // allocate the same number of objects to compare AllocationTick vs AllocationSampled
        private static Action _eventGeneratingActionForAllocationsWithAllocationTick = () =>
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            Stopwatch clock = new Stopwatch();
            clock.Start();
            int allocatedSize = AllocateObjects(ArrayInstanceCount);
            clock.Stop();
            var duration = clock.ElapsedMilliseconds;
            Logger.logger.Log($" #GCs: {GC.CollectionCount(0)}");
            Logger.logger.Log($" AllocationTick: {ArrayInstanceCount} instances allocated for {allocatedSize} bytes in {duration} ms");
            _allocationTickDuration = duration;
        };

        private static Action _eventGeneratingActionForAllocationsWithAllocationSampled = () =>
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            Stopwatch clock = new Stopwatch();
            clock.Start();
            int allocatedSize = AllocateObjects(ArrayInstanceCount);
            clock.Stop();
            var duration = clock.ElapsedMilliseconds;
            Logger.logger.Log($" #GCs: {GC.CollectionCount(0)}");
            Logger.logger.Log($" AllocationSampled: {ArrayInstanceCount} instances allocated for {allocatedSize} bytes in {duration} ms");
            _allocationSampledDuration = duration;
        };

        private static Func<EventPipeEventSource, Func<int>> _DoesTraceContainAllocationTickEvents = (source) =>
        {
            int allocationTickEvents = 0;
            source.Clr.GCAllocationTick += (eventData) =>
            {
                allocationTickEvents++;
            };
            return () => {
                Logger.logger.Log("AllocationTick counts validation");
                Logger.logger.Log("Nb events: " + allocationTickEvents);
                return (allocationTickEvents > 0) ? 100 : -1;
            };
        };

        private static Func<EventPipeEventSource, Func<int>> _DoesTraceContainAllocationSampledEvents = (source) =>
        {
            int allocationSampledEvents = 0;
            source.Dynamic.All += (eventData) =>
            {
                if (eventData.ID == (TraceEventID)303)  // AllocationSampled is not defined in TraceEvent yet
                {
                    allocationSampledEvents++;
                }
            };
            return () => {
                Logger.logger.Log("AllocationSampled counts validation");
                Logger.logger.Log("Nb events: " + allocationSampledEvents);
                return (allocationSampledEvents > 0) ? 100 : -1;
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

