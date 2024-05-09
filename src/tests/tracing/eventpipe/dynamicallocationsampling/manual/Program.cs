using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing;
using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System.Text;

namespace DynamicAllocationSampling
{
    internal class TypeInfo
    {
        public string TypeName = "?";
        public int Count;
        public int Size;
        public int TotalSize;

        public override int GetHashCode()
        {
            return TypeName.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is TypeInfo))
            {
                return false;
            }

            return TypeName.Equals(((TypeInfo)obj).TypeName);
        }
    }

    internal class Program
    {
        // TODO: for percentiles, we will need to keep track of all results
        //       but which value should be used to compute the percentile? probably the count but for AllocationTick or AllocationSampled? Both?
        private static Dictionary<string, TypeInfo> _sampledTypes = new Dictionary<string, TypeInfo>();
        private static Dictionary<string, TypeInfo> _tickTypes = new Dictionary<string, TypeInfo>();
        private static List<Dictionary<string, TypeInfo>> _sampledTypesInRun = null;
        private static List<Dictionary<string, TypeInfo>> _tickTypesInRun = null;
        private static int _allocationsCount = 0;

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

        private static long UpscaleSize(long totalSize, int count, long mean)
        {
            var averageSize = (double)totalSize / (double)count;
            var scale = 1 / (1 - Math.Exp(-averageSize / mean));
            return (long)(totalSize * scale);
        }

        private static void OnAllocationTick(GCAllocationTickTraceData payload)
        {
            if (!_tickTypes.TryGetValue(payload.TypeName, out TypeInfo typeInfo))
            {
                typeInfo = new TypeInfo() { TypeName = payload.TypeName, Count = 0, Size = (int)payload.ObjectSize, TotalSize = 0 };
                _tickTypes.Add(payload.TypeName, typeInfo);
            }
            typeInfo.Count++;
            typeInfo.TotalSize += (int)payload.ObjectSize;
        }

        private static void OnEvents(TraceEvent eventData)
        {
            if (eventData.ID == (TraceEventID)303)
            {
                AllocationSampledData payload = new AllocationSampledData(eventData, 8); // assume 64-bit pointers

                if (!_sampledTypes.TryGetValue(payload.TypeName, out TypeInfo typeInfo))
                {
                    typeInfo = new TypeInfo() { TypeName = payload.TypeName, Count = 0, Size = (int)payload.ObjectSize, TotalSize = 0 };
                    _sampledTypes.Add(payload.TypeName, typeInfo);
                }
                typeInfo.Count++;
                typeInfo.TotalSize += (int)payload.ObjectSize;

                return;
            }

            if (eventData.ID == (TraceEventID)600)
            {
                AllocationsRunData payload = new AllocationsRunData(eventData);
                Console.WriteLine($"> starts {payload.Iterations} iterations allocating {payload.Count} instances");

                _sampledTypesInRun = new List<Dictionary<string, TypeInfo>>(payload.Iterations);
                _tickTypesInRun = new List<Dictionary<string, TypeInfo>>(payload.Iterations);
                _allocationsCount = payload.Count;
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

                    var upscaledCount = (long)info.Count * UpscaleSize(info.TotalSize, info.Count, SAMPLING_MEAN) / info.TotalSize;
                    var percentDiff = (double)(upscaledCount - _allocationsCount) / (double)_allocationsCount;
                    distribution.Add(percentDiff);
                }
            }

            foreach (var type in typeDistribution.Keys.OrderBy(t => t.Size))
            {
                var distribution = typeDistribution[type];

                Console.WriteLine(type.TypeName);
                Console.WriteLine("-------------------------");
                foreach (var diff in distribution.OrderBy(v => v))
                {
                    Console.WriteLine($"{diff,8:0.0 %}");
                }
                Console.WriteLine();
            }
        }

        private static void ShowIterationResults()
        {
            // TODO: need to compute the mean size in case of array types
            // print the sampled types for both AllocationTick and AllocationSampled
            Console.WriteLine("Tag  SCount  TCount       SSize       TSize  UnitSize  UpscaledSize  UpscaledCount  Name");
            Console.WriteLine("-------------------------------------------------------------------------------------------");
            foreach (var type in _sampledTypes.Values.OrderBy(v => v.Size))
            {
                string tag = "S";
                if (_tickTypes.TryGetValue(type.TypeName, out TypeInfo tickType))
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

                Console.Write($"  {type.TotalSize,10}");
                if (tag == "S")
                {
                    Console.Write($"  {0,10}");
                }
                else
                {
                    Console.Write($"  {tickType.TotalSize,10}");
                }

                if (type.Count != 0)
                {
                    Console.WriteLine($"  {type.TotalSize / type.Count,8}    {UpscaleSize(type.TotalSize, type.Count, SAMPLING_MEAN),10}     {(long)type.Count * UpscaleSize(type.TotalSize, type.Count, SAMPLING_MEAN) / type.TotalSize,10}  {type.TypeName}");
                }
            }

            foreach (var type in _tickTypes.Values)
            {
                string tag = "T";

                if (!_sampledTypes.ContainsKey(type.TypeName))
                {
                    Console.WriteLine($"{tag,3}  {"0",6}  {type.Count,6}  {"0",10}  {type.TotalSize,10}  {type.TotalSize / type.Count,8}    {"0",10}     {"0",10}  {type.TypeName}");
                }
            }
        }
    }


    //  <data name="AllocationKind" inType="win:UInt32" map="GCAllocationKindMap" />
    //  <data name="ClrInstanceID" inType="win:UInt16" />
    //  <data name="TypeID" inType="win:Pointer" />
    //  <data name="TypeName" inType="win:UnicodeString" />
    //  <data name="HeapIndex" inType="win:UInt32" />
    //  <data name="Address" inType="win:Pointer" />
    //  <data name="ObjectSize" inType="win:UInt64" outType="win:HexInt64" />
    //  <data name="SamplingBudget" inType="win:UInt64" outType="win:HexInt64" />
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
        public long SamplingBudget;

        private void ComputeFields()
        {
            int offsetBeforeString = 4 + 2 + _pointerSize;

            Span<byte> data = _payload.EventData().AsSpan();
            AllocationKind = (GCAllocationKind)BitConverter.ToInt32(data.Slice(0, 4));
            ClrInstanceID = BitConverter.ToInt16(data.Slice(4, 2));
            TypeID = BitConverter.ToUInt64(data.Slice(6, _pointerSize));                                                    //   \0 should not be included for GetString to work
            TypeName = Encoding.Unicode.GetString(data.Slice(offsetBeforeString, _payload.EventDataLength - offsetBeforeString - EndOfStringCharLength - 4 - _pointerSize - 8 - 8));
            HeapIndex = BitConverter.ToInt32(data.Slice(offsetBeforeString + TypeName.Length * 2 + EndOfStringCharLength, 4));
            Address = BitConverter.ToUInt64(data.Slice(offsetBeforeString + TypeName.Length * 2 + EndOfStringCharLength + 4, _pointerSize));
            ObjectSize = BitConverter.ToInt64(data.Slice(offsetBeforeString + TypeName.Length * 2 + EndOfStringCharLength + 4 + 8, 8));
            SamplingBudget = BitConverter.ToInt64(data.Slice(offsetBeforeString + TypeName.Length * 2 + EndOfStringCharLength + 4 + 8 + 8, 8));
        }
    }

    class AllocationsRunData
    {
        private TraceEvent _payload;
        public AllocationsRunData(TraceEvent payload)
        {
            _payload = payload;

            ComputeFields();
        }

        public int Iterations;
        public int Count;

        private void ComputeFields()
        {
            Span<byte> data = _payload.EventData().AsSpan();
            Iterations = BitConverter.ToInt32(data.Slice(0, 4));
            Count = BitConverter.ToInt32(data.Slice(4, 4));
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
