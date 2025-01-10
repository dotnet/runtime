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

namespace Tracing.Tests
{
    public class AllocationSamplingValidation
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // check that AllocationSampled events are generated and size + type name are correct
            var ret = IpcTraceTest.RunAndValidateEventCounts(
                new Dictionary<string, ExpectedEventCount>() { { "Microsoft-Windows-DotNETRuntime", -1 } },
                _eventGeneratingActionForAllocations,
                // AllocationSamplingKeyword (0x80000000000): 0b1000_0000_0000_0000_0000_0000_0000_0000_0000_0000_0000
                new List<EventPipeProvider>() { new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, 0x80000000000) },
                1024, _DoesTraceContainEnoughAllocationSampledEvents, enableRundownProvider: false);
            if (ret != 100)
                return ret;

            return 100;
        }

        const int InstanceCount = 2000000;
        const int MinExpectedEvents = 1;
        static List<Object128> _objects128s = new List<Object128>(InstanceCount);

        // allocate objects to trigger dynamic allocation sampling events
        private static Action _eventGeneratingActionForAllocations = () =>
        {
            _objects128s.Clear();
            for (int i = 0; i < InstanceCount; i++)
            {
                if ((i != 0) && (i % (InstanceCount/5) == 0))
                    Logger.logger.Log($"Allocated {i} instances...");

                Object128 obj = new Object128();
                _objects128s.Add(obj);
            }

            Logger.logger.Log($"{_objects128s.Count} instances allocated");
        };

        private static Func<EventPipeEventSource, Func<int>> _DoesTraceContainEnoughAllocationSampledEvents = (source) =>
        {
            int AllocationSampledEvents = 0;
            int Object128Count = 0;
            source.Dynamic.All += (eventData) =>
            {
                if (eventData.ID == (TraceEventID)303)  // AllocationSampled is not defined in TraceEvent yet
                {
                    AllocationSampledEvents++;

                    AllocationSampledData payload = new AllocationSampledData(eventData, source.PointerSize);
                    // uncomment to see the allocation events payload
                    //Logger.logger.Log($"{payload.AllocationKind} | ({payload.ObjectSize}) {payload.TypeName}  = 0x{payload.Address}");
                    if (payload.TypeName == "Tracing.Tests.Object128" ||
                        (payload.TypeName == "NULL" && payload.ObjectSize >= 128))  // NativeAOT doesn't report type names but we can use the size as a good proxy
                                                                                    // A real profiler would resolve the TypeID from PDBs but replicating that would
                                                                                    // make the test more complicated
                    {
                        Object128Count++;
                    }
                }
            };
            return () => {
                Logger.logger.Log("AllocationSampled counts validation");
                Logger.logger.Log("Nb events: " + AllocationSampledEvents);
                Logger.logger.Log("Nb object128: " + Object128Count);
                return (AllocationSampledEvents >= MinExpectedEvents) && (Object128Count != 0) ? 100 : -1;
            };
        };
    }

    internal class Object0
    {
    }

    internal class Object128 : Object0
    {
        private readonly UInt64 _x1;
        private readonly UInt64 _x2;
        private readonly UInt64 _x3;
        private readonly UInt64 _x4;
        private readonly UInt64 _x5;
        private readonly UInt64 _x6;
        private readonly UInt64 _x7;
        private readonly UInt64 _x8;
        private readonly UInt64 _x9;
        private readonly UInt64 _x10;
        private readonly UInt64 _x11;
        private readonly UInt64 _x12;
        private readonly UInt64 _x13;
        private readonly UInt64 _x14;
        private readonly UInt64 _x15;
        private readonly UInt64 _x16;
    }

    // AllocationSampled is not defined in TraceEvent yet
    //
    //  <data name="AllocationKind" inType="win:UInt32" map="GCAllocationKindMap" />
    //  <data name="ClrInstanceID" inType="win:UInt16" />
    //  <data name="TypeID" inType="win:Pointer" />
    //  <data name="TypeName" inType="win:UnicodeString" />
    //  <data name="Address" inType="win:Pointer" />
    //  <data name="ObjectSize" inType="win:UInt64" outType="win:HexInt64" />
    //  <data name="SampledByteOffset" inType="win:UInt64" outType="win:HexInt64" />
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
        public UInt64 Address;
        public long ObjectSize;
        public long SampledByteOffset;

        private void ComputeFields()
        {
            int offsetBeforeString = 4 + 2 + _pointerSize;

            Span<byte> data = _payload.EventData().AsSpan();
            AllocationKind = (GCAllocationKind)BitConverter.ToInt32(data.Slice(0, 4));
            ClrInstanceID = BitConverter.ToInt16(data.Slice(4, 2));
            if (_pointerSize == 4)
            {
                TypeID = BitConverter.ToUInt32(data.Slice(6, _pointerSize));
            }
            else
            {
                TypeID = BitConverter.ToUInt64(data.Slice(6, _pointerSize));
            }
            TypeName = Encoding.Unicode.GetString(data.Slice(offsetBeforeString, _payload.EventDataLength - offsetBeforeString - EndOfStringCharLength - _pointerSize - 8 - 8));
            if (_pointerSize == 4)
            {
                Address = BitConverter.ToUInt32(data.Slice(offsetBeforeString + TypeName.Length * 2 + EndOfStringCharLength, _pointerSize));
            }
            else
            {
                Address = BitConverter.ToUInt64(data.Slice(offsetBeforeString + TypeName.Length * 2 + EndOfStringCharLength, _pointerSize));
            }
            ObjectSize = BitConverter.ToInt64(data.Slice(offsetBeforeString + TypeName.Length * 2 + EndOfStringCharLength + _pointerSize, 8));
            SampledByteOffset = BitConverter.ToInt64(data.Slice(offsetBeforeString + TypeName.Length * 2 + EndOfStringCharLength + _pointerSize + 8, 8));
        }
    }
}
