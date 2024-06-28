// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace StressLogAnalyzer;

public static class Program
{
    [UnmanagedCallersOnly]
    private static unsafe int ReadFromMemoryMappedLog(ulong address, byte* buffer, uint bytesToRead, void* state)
    {
        StressLogHeader* header = (StressLogHeader*)state;

        // First look at module data. This will translate all addresses that point to static data (like string literals).
        ulong cumulativeSize = 0;
        Span<byte> moduleImageData = header->moduleImageData;
        foreach (StressLogHeader.ModuleDesc module in header->moduleTable)
        {
            if (address >= module.baseAddr && address < module.baseAddr + module.size)
            {
                ulong moduleOffset = address - module.baseAddr;
                Debug.Assert(cumulativeSize + moduleOffset < (ulong)moduleImageData.Length, "Address is out of bounds");
                ref byte moduleData = ref moduleImageData[(int)(cumulativeSize + moduleOffset)];
                ulong bytesToCopy = ulong.Min(bytesToRead, module.size - moduleOffset);
                Unsafe.CopyBlock(ref *buffer, ref moduleData, (uint)bytesToCopy);
                return (int)bytesToCopy;
            }
            else
            {
                cumulativeSize += module.size;
            }
        }

        // Otherwise, translate the signature for dynamically-allocated data based on the memory-mapped base address.
        if (address >= header->memoryBase && address + bytesToRead < header->memoryLimit)
        {
            ulong offset = address - header->memoryBase;
            Unsafe.CopyBlock(ref *buffer, ref *((byte*)header + offset), bytesToRead);
            return (int)bytesToRead;
        }

        return -1;
    }

    public static unsafe int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: StressLog <log file> <options>");
            Console.WriteLine("       StressLog <log file> -? for list of options");
            return 1;
        }
        else if (args.Contains("-?"))
        {
            Console.WriteLine("""      
                Usage:

                 -o:<outputfile.txt>: write output to a text file instead of the console

                 -v:<hexvalue>: look for a specific hex value (often used to look for addresses
                 -v:<hexlower>-<hexupper>: look for values >= hexlower and <= hexupper
                 -v:<hexlower>+<hexsize>: look for values >= hexlower and <= hexlower+hexsize

                 -t:<start time>: don't consider messages before start time
                 -t:<start time>-<end time>: only consider messages >= start time and <= end time
                 -t:-<last seconds>: only consider messages in the last seconds

                 -l:<level1>,<level2>,... : print messages at dprint level1,level2,...

                 -g:<gc_index>: only print messages occurring during GC#gc_index
                 -g:<gc_index1>-<gc_index_2>: as above, for a range of GC indices

                 -f: print the raw format strings along with the message
                     (useful to search for the format string in the source code
                 -f:<format string>: search for a specific format string
                    e.g. '-f:\"<%zx>:%zx\"'
                 -p:<format string>: search for all format strings with a specific prefix
                    e.g. '-p:\"commit-accounting\"'

                 -i:<hex facility code>: ignore messages from log facilities
                   e.g. '-i:7ffe' means ignore messages from anything but LF_GC

                 -tid: print hex thread ids, e.g. 2a08 instead of GC12
                 -tid:<thread id1>,<thread id2>,...: only print messages from the listed
                     threads. Thread ids are in hex, given as GC<decimal heap number>,
                     or BG<decimal heap number>
                     e.g. '-tid:2bc8,GC3,BG14' would print messages from thread 2bc8, the gc thread
                     associated with heap 3, and the background GC thread for heap 14

                 -e: print earliest messages from all threads
                 -e:<thread id1>,<thread id2>,...: print earliest messages from the listed
                     threads. Thread ids are in hex, given as GC<decimal heap number>,
                     or BG<decimal heap number>
                     e.g. '-e:2bc8,GC3,BG14' would print the earliest messages from thread 2bc8,
                     the gc thread associated with heap 3, and the background GC thread for heap 14

                 -a: print all messages from all threads

                 -d: suppress default messages
                
                """);
            return 0;
        }

        using var stressLogData = MemoryMappedFile.CreateFromFile(args[0], FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        using MemoryMappedViewAccessor accessor = stressLogData.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

        if (accessor.Capacity < sizeof(StressLogHeader))
        {
            Console.WriteLine("Invalid memory-mapped stress log");
            return 1;
        }
        byte* buffer = null;
        try
        {
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref buffer);
            StressLogHeader* header = (StressLogHeader*)buffer;

            if (header->headerSize != sizeof(StressLogHeader)
                || !"LRTS"u8.SequenceEqual(header->magic)
                || header->version is not (0x00010001 or 0x00010002))
            {
                Console.WriteLine("Invalid StressLogHeader");
                return 1;
            }

            Target target = Target.Create(
                GetDescriptor((int)(header->version & 0xFFFF)),
                [TargetPointer.Null, new TargetPointer(header->memoryBase + (nuint)((byte*)&header->moduleTable - (byte*)header))],
                &ReadFromMemoryMappedLog,
                header,
                true,
                nuint.Size);

            Registry registry = new(target);
            IStressLog stressLogContract = registry.StressLog;

            bool runAgain = false;
            do
            {
                ProcessStressLog(header, stressLogContract, args[1..]);

                Console.Write("'q' to quit, 'r' to run again\n>");

                while (true)
                {
                    string command = Console.ReadLine()!;
                    if (command.Equals("q", StringComparison.OrdinalIgnoreCase))
                    {
                        return 0;
                    }
                    else if (command.Equals("r", StringComparison.OrdinalIgnoreCase))
                    {
                        runAgain = true;
                        break;
                    }
                }
            } while (runAgain);

            return 0;
        }
        finally
        {
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    private static unsafe void ProcessStressLog(StressLogHeader* header, IStressLog stressLogContract, string[] args)
    {
        List<ThreadStressLogData> logs = [.. stressLogContract.GetThreadStressLogs(new TargetPointer(header->logs))];

        List<StressMsgData> messages = [.. logs.SelectMany(stressLogContract.GetStressMessages)];
    }

    private static ContractDescriptorParser.ContractDescriptor GetDescriptor(int stressLogVersion)
    {
        return new ContractDescriptorParser.ContractDescriptor
        {
            Baseline = BaseContractDescriptor.Baseline,
            Version = BaseContractDescriptor.Version,
            Contracts = new(){ { "StressLog", stressLogVersion } },
            Types = BaseContractDescriptor.Types,
            Globals = BaseContractDescriptor.Globals,
        };
    }

    private static ContractDescriptorParser.ContractDescriptor BaseContractDescriptor = ContractDescriptorParser.ParseCompact(
            """"
            {
                "version": 0,
                "baseline": "empty",
                "types": {
                    "StressLog": {
                        "!": 184,
                        "LoggedFacilities": 0,
                        "Level": 4,
                        "MaxSizePerThread": 8,
                        "MaxSizeTotal": 12,
                        "TotalChunks": 16,
                        "Logs": 24,
                        "TickFrequency": 48,
                        "StartTimestamp": 56,
                        "ModuleOffset": 72
                    },
                    "StressLogModuleDesc": {
                        "!": 16,
                        "BaseAddress": [ 0, "pointer" ],
                        "Size": [ 8, "nuint" ]
                    },
                    "ThreadStressLog": {
                        "Next": 0,
                        "ThreadId": [ 8, "uint64" ],
                        "WriteHasWrapped": [ 18, "uint8" ],
                        "CurrentPtr": [ 24, "pointer" ],
                        "ChunkListHead": 40,
                        "ChunkListTail": 48,
                        "CurrentWriteChunk": 64
                    },
                    "StressLogChunk": {
                        "!": 32792,
                        "Prev": 0,
                        "Next": 8,
                        "Buf": 16,
                        "Sig1": 32784,
                        "Sig2": 32788
                    },
                    "StressMsgHeader": {
                        "!": 16
                    },
                    "StressMsg": {
                        "Header": [ 0, "StressMsgHeader" ],
                        "Args": 16
                    }
                },
                "globals": {
                    "StressLogEnabled": [ "0x1", "uint8" ],
                    "StressLogMaxModules": [ "0x5", "uint64" ],
                    "StressLogChunkSize": [ "0x8000", "uint32" ],
                    "StressLogMaxMessageSize": [ "0x208", "uint64" ],
                    "StressMsgHeaderSize": [ "0x10", "uint32" ],
                    "StressLogModuleTable": [[ 1 ], "pointer" ],
                },
                "contracts": {
                    "StressLog": 2,
                }
            }
            """"u8)!;
}
