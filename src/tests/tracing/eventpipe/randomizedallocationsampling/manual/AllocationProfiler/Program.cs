// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing;
using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System.Text;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace DynamicAllocationSampling
{
    internal class TypeInfo
    {
        public string TypeName = "?";
        public int Count;
        public long Size;
        public long TotalSize;
        public long RemainderSize;

        public override int GetHashCode()
        {
            return (TypeName+Size).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is TypeInfo))
            {
                return false;
            }

            return (TypeName+Size).Equals(((TypeInfo)obj).TypeName+Size);
        }
    }

    internal class Program
    {
        private static Dictionary<string, TypeInfo> _sampledTypes = new Dictionary<string, TypeInfo>();
        private static Dictionary<string, TypeInfo> _tickTypes = new Dictionary<string, TypeInfo>();
        private static List<Dictionary<string, TypeInfo>> _sampledTypesInRun = null;
        private static List<Dictionary<string, TypeInfo>> _tickTypesInRun = null;
        private static int _allocationsCount = 0;
        private static List<string> _allocatedTypes = new List<string>();
        private static EventPipeEventSource _source;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No process ID specified");
                return;
            }

            int pid = -1;
            if (!int.TryParse(args[0], out pid))
            {
                Console.WriteLine($"Invalid specified process ID '{args[0]}'");
                return;
            }

            try
            {
                PrintEventsLive(pid);
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
        }


        public static void PrintEventsLive(int processId)
        {
            var providers = new List<EventPipeProvider>()
            {
                new EventPipeProvider(
                        "Microsoft-Windows-DotNETRuntime",
                        EventLevel.Verbose, // verbose is required for AllocationTick
                        (long)0x80000000001 // new AllocationSamplingKeyword + GCKeyword
                        ),
                new EventPipeProvider(
                        "Allocations-Run",
                        EventLevel.Informational
                        ),
            };
            var client = new DiagnosticsClient(processId);

            using (var session = client.StartEventPipeSession(providers, false))
            {
                Console.WriteLine();

                Task streamTask = Task.Run(() =>
                {
                    var source = new EventPipeEventSource(session.EventStream);
                    _source = source;

                    ClrTraceEventParser clrParser = new ClrTraceEventParser(source);
                    clrParser.GCAllocationTick += OnAllocationTick;
                    source.Dynamic.All += OnEvents;

                    try
                    {
                        source.Process();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error encountered while processing events: {e.Message}");
                    }
                });

                Task inputTask = Task.Run(() =>
                {
                    while (Console.ReadKey().Key != ConsoleKey.Enter)
                    {
                        Thread.Sleep(100);
                    }
                    session.Stop();
                });

                Task.WaitAny(streamTask, inputTask);
            }

            // not all cases are emitting allocations run events
            if ((_sampledTypesInRun == null) && (_sampledTypes.Count > 0))
            {
                ShowIterationResults();
            }
        }

        private const long SAMPLING_MEAN = 100 * 1024;
        private const double SAMPLING_RATIO = 0.999990234375 / 0.000009765625;
        private static long UpscaleSize(long totalSize, int count, long mean, long sizeRemainder)
        {
            //// This is the Poisson process based scaling
            //var averageSize = (double)totalSize / (double)count;
            //var scale = 1 / (1 - Math.Exp(-averageSize / mean));
            //return (long)(totalSize * scale);

            // use the upscaling method detailed in the PR
            // = sq/p + u
            //   s = # of samples for a type
            //   q = 1 - 1/102400
            //   p = 1/102400
            //   u = sum of object remainders = Sum(object_size - sampledByteOffset) for all samples
            return (long)(SAMPLING_RATIO * count + sizeRemainder);
        }

        private static void OnAllocationTick(GCAllocationTickTraceData payload)
        {
            // skip unexpected types
            if (!_allocatedTypes.Contains(payload.TypeName)) return;

            if (!_tickTypes.TryGetValue(payload.TypeName + payload.ObjectSize, out TypeInfo typeInfo))
            {
                typeInfo = new TypeInfo() { TypeName = payload.TypeName, Count = 0, Size = payload.ObjectSize, TotalSize = 0 };
                _tickTypes.Add(payload.TypeName + payload.ObjectSize, typeInfo);
            }
            typeInfo.Count++;
            typeInfo.TotalSize += (int)payload.ObjectSize;
        }

        private static void OnEvents(TraceEvent eventData)
        {
            if (eventData.ID == (TraceEventID)303)
            {
                AllocationSampledData payload = new AllocationSampledData(eventData, _source.PointerSize);

                // skip unexpected types
                if (!_allocatedTypes.Contains(payload.TypeName)) return;

                if (!_sampledTypes.TryGetValue(payload.TypeName+payload.ObjectSize, out TypeInfo typeInfo))
                {
                    typeInfo = new TypeInfo() { TypeName = payload.TypeName, Count = 0, Size = (int)payload.ObjectSize, TotalSize = 0, RemainderSize = payload.ObjectSize - payload.SampledByteOffset };
                    _sampledTypes.Add(payload.TypeName + payload.ObjectSize, typeInfo);
                }
                typeInfo.Count++;
                typeInfo.TotalSize += (int)payload.ObjectSize;
                typeInfo.RemainderSize += (payload.ObjectSize - payload.SampledByteOffset);

                return;
            }

            if (eventData.ID == (TraceEventID)600)
            {
                AllocationsRunData payload = new AllocationsRunData(eventData);
                Console.WriteLine($"> starts {payload.Iterations} iterations allocating {payload.Count} instances");

                _sampledTypesInRun = new List<Dictionary<string, TypeInfo>>(payload.Iterations);
                _tickTypesInRun = new List<Dictionary<string, TypeInfo>>(payload.Iterations);
                _allocationsCount = payload.Count;
                string allocatedTypes = payload.AllocatedTypes;
                if (allocatedTypes.Length > 0)
                {
                    _allocatedTypes = allocatedTypes.Split(';').ToList();
                }

                return;
            }

            if (eventData.ID == (TraceEventID)601)
            {
                Console.WriteLine("\n< run stops\n");

                ShowRunResults();
                return;
            }

            if (eventData.ID == (TraceEventID)602)
            {
                AllocationsRunIterationData payload = new AllocationsRunIterationData(eventData);
                Console.Write($"{payload.Iteration}");

                _sampledTypes.Clear();
                _tickTypes.Clear();
                return;
            }

            if (eventData.ID == (TraceEventID)603)
            {
                Console.WriteLine("|");
                ShowIterationResults();

                _sampledTypesInRun.Add(_sampledTypes);
                _sampledTypes = new Dictionary<string, TypeInfo>();
                _tickTypesInRun.Add(_tickTypes);
                _tickTypes = new Dictionary<string, TypeInfo>();
                return;
            }
        }

        private static void ShowRunResults()
        {
            var iterations = _sampledTypesInRun.Count;

            // for each type, get the percent diff between upscaled count and expected _allocationsCount
            Dictionary<TypeInfo, List<double>> typeDistribution = new Dictionary<TypeInfo, List<double>>();
            foreach (var iteration in _sampledTypesInRun)
            {
                foreach (var info in iteration.Values)
                {
                    // ignore types outside of the allocations run
                    if (info.Count < 16) continue;

                    if (!typeDistribution.TryGetValue(info, out List<double> distribution))
                    {
                        distribution = new List<double>(iterations);
                        typeDistribution.Add(info, distribution);
                    }

                    var upscaledCount = (long)info.Count * UpscaleSize(info.TotalSize, info.Count, SAMPLING_MEAN, info.RemainderSize) / info.TotalSize;
                    var percentDiff = (double)(upscaledCount - _allocationsCount) / (double)_allocationsCount;
                    distribution.Add(percentDiff);
                }
            }

            foreach (var type in typeDistribution.Keys.OrderBy(t => t.Size))
            {
                var distribution = typeDistribution[type];

                string typeName = type.TypeName;
                if (typeName.Contains("[]"))
                {
                    typeName += $" ({type.Size} bytes)";
                }
                Console.WriteLine(typeName);
                Console.WriteLine("-------------------------");
                int current = 1;
                foreach (var diff in distribution.OrderBy(v => v))
                {
                    if (iterations > 20)
                    {
                        if ((current <= 5) || ((current >= 49) && (current < 52)) || (current >= 96))
                        {
                            Console.WriteLine($"{current,4} {diff,8:0.0 %}");
                        }
                        else
                        if ((current == 6) || (current == 95))
                        {
                            Console.WriteLine("        ...");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{current,4} {diff,8:0.0 %}");
                    }

                    current++;
                }
                Console.WriteLine();
            }
        }

        private static void ShowIterationResults()
        {
            // NOTE: need to take the size into account for array types
            // print the sampled types for both AllocationTick and AllocationSampled
            Console.WriteLine("Tag  SCount  TCount          SSize          TSize   UnitSize     UpscaledSize  UpscaledCount  Name");
            Console.WriteLine("--------------------------------------------------------------------------------------------------");
            foreach (var type in _sampledTypes.Values.OrderBy(v => v.Size))
            {
                string tag = "S";
                if (_tickTypes.TryGetValue(type.TypeName + type.Size, out TypeInfo tickType))
                {
                    tag += "T";
                }

                Console.Write($"{tag,3}  {type.Count,6}");
                if (tag == "S")
                {
                    Console.Write($"  {0,6}");
                }
                else
                {
                    Console.Write($"  {tickType.Count,6}");
                }

                Console.Write($"  {type.TotalSize,13}");
                if (tag == "S")
                {
                    Console.Write($"  {0,13}");
                }
                else
                {
                    Console.Write($"  {tickType.TotalSize,13}");
                }

                string typeName = type.TypeName;
                if (typeName.Contains("[]"))
                {
                    typeName += $" ({type.Size} bytes)";
                }

                if (type.Count != 0)
                {
                    Console.WriteLine($"  {type.TotalSize / type.Count,9}    {UpscaleSize(type.TotalSize, type.Count, SAMPLING_MEAN, type.RemainderSize),13}     {(long)type.Count * UpscaleSize(type.TotalSize, type.Count, SAMPLING_MEAN, type.RemainderSize) / type.TotalSize,10}  {typeName}");
                }
            }

            foreach (var type in _tickTypes.Values)
            {
                string tag = "T";

                if (!_sampledTypes.ContainsKey(type.TypeName + type.Size))
                {
                    string typeName = type.TypeName;
                    if (typeName.Contains("[]"))
                    {
                        typeName += $" ({type.Size} bytes)";
                    }

                    Console.WriteLine($"{tag,3}  {"0",6}  {type.Count,6}  {"0",13}  {type.TotalSize,13}  {type.TotalSize / type.Count,9}    {"0",13}     {"0",10}  {typeName}");
                }
            }
        }
    }


    //  <data name="AllocationKind" inType="win:UInt32" map="GCAllocationKindMap" />
    //  <data name="ClrInstanceID" inType="win:UInt16" />
    //  <data name="TypeID" inType="win:Pointer" />
    //  <data name="TypeName" inType="win:UnicodeString" />
    //  <data name="Address" inType="win:Pointer" />
    //  <data name="ObjectSize" inType="win:UInt64" outType="win:HexInt64" />
    //  <data name="SampledByteOffset" inType="win:UInt64" outType="win:HexInt64" />
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
            //   \0 should not be included for GetString to work
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

    class AllocationsRunData
    {
        const int EndOfStringCharLength = 2;
        private TraceEvent _payload;

        public AllocationsRunData(TraceEvent payload)
        {
            _payload = payload;

            ComputeFields();
        }

        public int Iterations;
        public int Count;
        public string AllocatedTypes;

        private void ComputeFields()
        {
            int offsetBeforeString = 4 + 4;

            Span<byte> data = _payload.EventData().AsSpan();
            Iterations = BitConverter.ToInt32(data.Slice(0, 4));
            Count = BitConverter.ToInt32(data.Slice(4, 4));
            AllocatedTypes = Encoding.Unicode.GetString(data.Slice(offsetBeforeString, _payload.EventDataLength - offsetBeforeString - EndOfStringCharLength));
        }
    }

    class AllocationsRunIterationData
    {
        private TraceEvent _payload;
        public AllocationsRunIterationData(TraceEvent payload)
        {
            _payload = payload;

            ComputeFields();
        }

        public int Iteration;

        private void ComputeFields()
        {
            Span<byte> data = _payload.EventData().AsSpan();
            Iteration = BitConverter.ToInt32(data.Slice(0, 4));
        }
    }
}
