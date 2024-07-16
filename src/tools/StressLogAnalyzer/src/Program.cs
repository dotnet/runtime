// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Win32.SafeHandles;
using StressLogAnalyzer.Filters;
using StressLogAnalyzer.Output;

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
            if (address >= module.baseAddr && address + bytesToRead < module.baseAddr + module.size)
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

    public static async Task<int> Main(string[] args)
    {
        CliConfiguration configuration = new(CreateRootCommand());
        return await configuration.InvokeAsync(args).ConfigureAwait(false);
    }

    public static CliRootCommand CreateRootCommand()
    {
        var inputFile = new CliArgument<FileInfo>("log file")
        {
            Description = "The memory-mapped stress log file to analyze",
        };

        var outputFile = new CliOption<FileInfo>("-output", "-o")
        {
            Description = "Write output to a text file instead of the console",
            HelpName = "output file",
        };

        var valueRanges = new CliOption<IntegerRange[]>("-values", "-v")
        {
            Arity = ArgumentArity.OneOrMore,
            DefaultValueFactory = argument => [],
            CustomParser = argument =>
            {
                IntegerRange[] values = new IntegerRange[argument.Tokens.Count];
                for (int i = 0; i < argument.Tokens.Count; i++)
                {
                    string value = argument.Tokens[i].Value;
                    string[] parts = value.Split('-', '+');
                    if (parts.Length > 2)
                    {
                        argument.AddError($"Invalid value range format in '{value}'");
                        return null;
                    }
                    if (parts.Length == 1)
                    {
                        values[i] = new IntegerRange(ulong.Parse(parts[0], NumberStyles.HexNumber), ulong.Parse(parts[0], NumberStyles.HexNumber));
                    }
                    else if (value.Contains('-'))
                    {
                        values[i] = new IntegerRange(ulong.Parse(parts[0], NumberStyles.HexNumber), ulong.Parse(parts[1], NumberStyles.HexNumber));
                    }
                    else
                    {
                        // argument is in the format of -v:<hexlower>+<hexsize>
                        ulong lower = ulong.Parse(parts[0], NumberStyles.HexNumber);
                        ulong size = ulong.Parse(parts[1], NumberStyles.HexNumber);
                        values[i] = new IntegerRange(lower, lower + size);
                    }
                }
                return values;
            },
            Description = "Look for a specific hex value (often used to look for addresses). Can be a specific address or specified as a 'start-end' or 'start+length' range.",
            HelpName = "hex value or range",
        };

        var timeRanges = new CliOption<TimeRange>("--time", "-t")
        {
            Arity = ArgumentArity.OneOrMore,
            DefaultValueFactory = argument => new TimeRange(0, double.MaxValue),
            CustomParser = argument =>
            {
                string value = argument.Tokens[0].Value;
                if (double.TryParse(value, out double startTimestamp))
                {
                    // Format is either <start time> or -<last seconds>
                    return new TimeRange(startTimestamp, double.MaxValue);
                }
                else if (value.Split('-') is [string start, string end])
                {
                    return new TimeRange(double.Parse(start), double.Parse(end));
                }
                else
                {
                    argument.AddError($"Invalid time range format in '{value}'");
                    return default;
                }
            },
            Description = "Don't consider messages before start time. Only consider messages >= start time and <= end time. Specify a negative number of seconds to only search in the last n seconds.",
            HelpName = "start time or range",
        };

        var allMessagesOption = new CliOption<bool>("--all", "-a")
        {
            Description = "Print all messages from all threads"
        };

        var defaultMessagesOption = new CliOption<bool>("--defaultMessages", "-d")
        {
            Description = "Suppress default messages"
        };

        var levelFilter = new CliOption<IReadOnlyList<IntegerRange>>("--level", "-l")
        {
            Arity = ArgumentArity.OneOrMore,
            DefaultValueFactory = argument => [],
            CustomParser = argument =>
            {
                List<IntegerRange> levels = [];
                foreach (CliToken token in argument.Tokens)
                {
                    foreach (string value in token.Value.Split(','))
                    {
                        if (value == "*")
                        {
                            levels.Add(new IntegerRange(0, 0x7fffffff));
                        }
                        else if (ulong.TryParse(value, out ulong minLevel))
                        {
                            levels.Add(new IntegerRange(minLevel, minLevel));
                        }
                        else if (value.Split('-') is [string min, string max])
                        {
                            levels.Add(new IntegerRange(ulong.Parse(min), ulong.Parse(max)));
                        }
                        else
                        {
                            argument.AddError($"Invalid level range format in '{value}'");
                            return null;
                        }
                    }
                }
                return levels;
            },
            Description = "Print messages at dprint level1,level2,...",
            HelpName = "level or range of levels",
            AllowMultipleArgumentsPerToken = true,
        };

        var prefixOption = new CliOption<string[]>("--prefix", "-p")
        {
            Description = "Search for all format strings with a specific prefix",
            HelpName = "format string"
        };

        var gcIndex = new CliOption<IntegerRange?>("--gc", "-g")
        {
            Arity = ArgumentArity.OneOrMore,
            DefaultValueFactory = argument => null,
            CustomParser = argument =>
            {
                string value = argument.Tokens[0].Value;
                if (value.Split('-') is [string start, string end])
                {
                    return new IntegerRange(ulong.Parse(start), ulong.Parse(end));
                }
                else
                {
                    ulong index = ulong.Parse(value);
                    return new IntegerRange(index, index);
                }
            },
            Description = "Only print messages occurring during GC#gc_index or from GC#gc_index_start to GC#gc_index_end",
            HelpName = "gc index or range",
        };

        var ignoreFacilityOption = new CliOption<ulong?>("--ignore", "-i")
        {
            CustomParser = argument =>
            {
                return ulong.Parse(argument.Tokens.Single().Value, NumberStyles.HexNumber);
            },
            Description = "Ignore messages only from these from log facilities",
            HelpName = "facility bitmap in hex",
        };

        var earliestOption = new CliOption<ThreadFilter>("--earliest", "-e")
        {
            Arity = ArgumentArity.ZeroOrMore,
            CustomParser = argument =>
            {
                return new ThreadFilter(argument.Tokens.Select(token => token.Value));
            },
            Description = "Print earliest message from all threads or from the listed threads",
            HelpName = "thread id or GC heap number",
        };

        var threadFilter = new CliOption<ThreadFilter?>("--threads", "-tid")
        {
            Arity = ArgumentArity.ZeroOrMore,
            DefaultValueFactory = argument => null,
            CustomParser = argument =>
            {
                return new ThreadFilter(argument.Tokens.Select(token => token.Value));
            },
            Description = "Print hex thread ids, e.g. 2a08 instead of GC12. Otherwise, only print messages from the listed threads",
            HelpName = "thread id or GC heap number",
        };

        var hexThreadId = new CliOption<bool?>("--hexThreadId", "--hex")
        {
            DefaultValueFactory = argument => null,
            Description = "Print hex thread ids, e.g. 2a08 instead of GC12",
        };

        var formatFilter = new CliOption<string[]>("--format", "-f")
        {
            Arity = ArgumentArity.ZeroOrMore,
            Description = "Print the raw format strings along with the message. Use -f:<format string> to search for a specific format string",
            HelpName = "format string",
        };

        var printFormatStrings = new CliOption<bool?>("--printFormatStrings", "-pf")
        {
            DefaultValueFactory = argument => null,
            Description = "Print the raw format strings along with the message",
        };

        var rootCommand = new CliRootCommand
        {
            inputFile,
            outputFile,
            valueRanges,
            timeRanges,
            allMessagesOption,
            defaultMessagesOption,
            levelFilter,
            prefixOption,
            gcIndex,
            ignoreFacilityOption,
            earliestOption,
            threadFilter,
            hexThreadId,
            printFormatStrings,
            formatFilter,
            new DiagramDirective(),
        };

        rootCommand.SetAction(async (args, ct) =>
        {
            ThreadFilter? threads = args.GetValue(threadFilter);
            string[]? formats = args.GetResult(formatFilter) is not null ? args.GetValue(formatFilter) : null;
            Options options = new(
                args.GetValue(inputFile)!,
                args.GetValue(outputFile),
                args.GetValue(valueRanges)!,
                args.GetValue(timeRanges)!,
                args.GetValue(allMessagesOption),
                !args.GetValue(defaultMessagesOption), // The option specifies suppressing default messages
                args.GetValue(levelFilter)!,
                args.GetValue(gcIndex),
                args.GetValue(ignoreFacilityOption),
                args.GetValue(earliestOption),
                PrintHexThreadIds: args.GetValue(hexThreadId) ?? threads is { HasAnyFilter: false },
                threads ?? new ThreadFilter([]),
                PrintFormatStrings: args.GetValue(printFormatStrings) ?? formats is [],
                args.GetValue(prefixOption),
                formats);
            return await AnalyzeStressLog(options, ct).ConfigureAwait(false);
        });

        return rootCommand;
    }

    private static async Task<int> AnalyzeStressLog(Options options, CancellationToken token)
    {
        using var stressLogData = MemoryMappedFile.CreateFromFile(options.InputFile.FullName, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        using MemoryMappedViewAccessor accessor = stressLogData.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

        if (accessor.Capacity < Unsafe.SizeOf<StressLogHeader>())
        {
            Console.WriteLine("Invalid memory-mapped stress log");
            return 1;
        }
        try
        {
            (Target target, TargetPointer logs) = CreateTarget(accessor.SafeMemoryMappedViewHandle);

            Registry registry = new(target);
            IStressLog stressLogContract = registry.StressLog;

            bool runAgain = false;
            do
            {
                using TextWriter? outputFile = options.OutputFile is not null ? File.CreateText(options.OutputFile.FullName) : null;

                InterestingStringFinder stringFinder = new(target, options.FormatFilter ?? [], options.FormatPrefixFilter ?? [], options.IncludeDefaultMessages);

                IMessageFilter messageFilter = CreateMessageFilter(options, stringFinder);

                GCThreadMap gcThreadMap = new();

                IThreadNameOutput threadNameOutput = options.PrintHexThreadIds
                    ? new HexThreadNameOutput() : new GCThreadNameOutput(gcThreadMap);

                TimeTracker timeTracker = CreateTimeTracker(accessor.SafeMemoryMappedViewHandle, options);

                var analyzer = new StressLogAnalyzer(
                    stressLogContract,
                    stringFinder,
                    messageFilter,
                    options.ThreadFilter,
                    options.EarliestMessageThreads);
                await analyzer.AnalyzeLogsAsync(
                    logs,
                    timeTracker,
                    gcThreadMap,
                    new StressMessageWriter(
                        threadNameOutput,
                        timeTracker,
                        target,
                        options.PrintFormatStrings,
                        outputFile ?? Console.Out),
                    token).ConfigureAwait(false);

                PrintFooter(accessor.SafeMemoryMappedViewHandle, stressLogContract, logs);

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

    private static unsafe void PrintFooter(SafeMemoryMappedViewHandle handle, IStressLog stressLogContract, TargetPointer logs)
    {
        byte* buffer = null;
        try
        {
            handle.AcquirePointer(ref buffer);
            StressLogHeader* header = (StressLogHeader*)buffer;

            double usedSize = (double)(header->memoryCur - header->memoryBase) / (1024 * 1024 * 1024);
            double availableSize = (double)(header->memoryLimit - header->memoryCur) / (1024 * 1024 * 1024);

            ThreadStressLogData[] threadLogs = [.. stressLogContract.GetThreadStressLogs(logs)];

            Console.WriteLine($"Use file size: {usedSize:F3} GB, still available {availableSize:F3} GB, {threadLogs.Length} threads total, {threadLogs.Count(t => t.WriteHasWrapped)} overwrote earlier messages");
            Console.WriteLine($"{header->threadsWithNoLog} threads did not get a log!");
        }
        finally
        {
            handle.ReleasePointer();
        }
    }

    private static IMessageFilter CreateMessageFilter(Options options, IInterestingStringFinder stringFinder)
    {
        if (options.IncludeAllMessages)
        {
            return new AllMessagesFilter(true);
        }

        IMessageFilter filter = new AllMessagesFilter(false);
        if (options.ValueRanges is not [])
        {
            filter = new ValueFilter(filter, options.ValueRanges);
            filter = new ValueRangeFilter(filter, stringFinder, options.ValueRanges);
        }

        if (options.LevelFilter is not [])
        {
            filter = new DPrintLevelFilter(filter, options.LevelFilter);
        }

        filter = new InterestingMessageFilter(filter, stringFinder);

        if (options.IgnoreFacility is ulong ignore)
        {
            filter = new FacilityMessageFilter(filter, ignore);
        }

        return filter;
    }

    private static unsafe (Target target, TargetPointer logs) CreateTarget(SafeMemoryMappedViewHandle handle)
    {
        byte* buffer = null;
        handle.AcquirePointer(ref buffer);
        StressLogHeader* header = (StressLogHeader*)buffer;

        if (header->headerSize != sizeof(StressLogHeader)
            || !"LRTS"u8.SequenceEqual(header->magic)
            || header->version is not (0x00010001 or 0x00010002))
        {
            throw new InvalidOperationException("Invalid memory-mapped stress log.");
        }

        Target target = Target.Create(
            GetDescriptor((int)(header->version & 0xFFFF)),
            [TargetPointer.Null, new TargetPointer(header->memoryBase + (nuint)((byte*)&header->moduleTable - (byte*)header))],
            &ReadFromMemoryMappedLog,
            header,
            true,
            nuint.Size);

        return (target, header->logs);
    }

    private static unsafe TimeTracker CreateTimeTracker(SafeMemoryMappedViewHandle handle, Options options)
    {
        byte* buffer = null;
        try
        {
            handle.AcquirePointer(ref buffer);
            StressLogHeader* header = (StressLogHeader*)buffer;
            return new TimeTracker(header->startTimeStamp, header->tickFrequency, options.Time, options.GCIndex);
        }
        finally
        {
            handle.ReleasePointer();
        }
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
