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
    }

    internal class Program
    {
        private static Dictionary<string, TypeInfo> _sampledTypes = new Dictionary<string, TypeInfo>();
        private static Dictionary<string, TypeInfo> _tickTypes = new Dictionary<string, TypeInfo>();

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
                    source.Dynamic.All += OnAllocationSampled;

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

                Console.Write($"{tag, 3}  {type.Count, 6}");
                if (tag == "S")
                {
                    Console.Write($"  {0, 6}");
                }
                else
                {
                    Console.Write($"  {tickType.Count, 6}");
                }

                Console.Write($"  {type.TotalSize, 10}");
                if (tag == "S")
                {
                    Console.Write($"  {0, 10}");
                }
                else
                {
                    Console.Write($"  {tickType.TotalSize, 10}");
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

        private const long SAMPLING_MEAN = 100 * 1024;

        private static long UpscaleSize(long totalSize, int count, long mean)
        {
            var averageSize = (double)totalSize / (double)count;
            var scale = 1 / (1 - Math.Exp(-averageSize / mean));
            return (long)(totalSize * scale);
        }

        private static void OnAllocationTick(GCAllocationTickTraceData payload)
        {
            //Console.WriteLine($"  {payload.HeapIndex} - {payload.AllocationKind} | ({payload.ObjectSize}) {payload.TypeName}  = 0x{payload.Address}");

            if (!_tickTypes.TryGetValue(payload.TypeName, out TypeInfo typeInfo))
            {
                typeInfo = new TypeInfo() { TypeName = payload.TypeName, Count = 0, Size = (int)payload.ObjectSize, TotalSize = 0 };
                _tickTypes.Add(payload.TypeName, typeInfo);
            }
            typeInfo.Count++;
            typeInfo.TotalSize += (int)payload.ObjectSize;
        }

        private static void OnAllocationSampled(TraceEvent eventData)
        {
            //Console.WriteLine($"{eventData.Opcode,4} - {eventData.OpcodeName} {eventData.EventDataLength} bytes");
            //Console.WriteLine(eventData.Dump());

            if (eventData.ID == (TraceEventID)303)
            {
                AllocationSampledData payload = new AllocationSampledData(eventData, 8); // assume 64-bit pointers
                //Console.WriteLine($"{payload.HeapIndex} - {payload.AllocationKind} | ({payload.ObjectSize}) {payload.TypeName}  = 0x{payload.Address}");

                if (!_sampledTypes.TryGetValue(payload.TypeName, out TypeInfo typeInfo))
                {
                    typeInfo = new TypeInfo() { TypeName = payload.TypeName, Count = 0, Size = (int)payload.ObjectSize, TotalSize = 0 };
                    _sampledTypes.Add(payload.TypeName, typeInfo);
                }
                typeInfo.Count++;
                typeInfo.TotalSize += (int)payload.ObjectSize;
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
