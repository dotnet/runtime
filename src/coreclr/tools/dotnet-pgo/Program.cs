// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.IL;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.IO.Compression;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System.Diagnostics.CodeAnalysis;
using ILCompiler.Reflection.ReadyToRun;
using Microsoft.Diagnostics.Tools.Pgo;
using Internal.Pgo;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    public enum PgoFileType
    {
        jittrace = 1,
        mibc = 2,
    }

    [Flags]
    public enum jittraceoptions
    {
        none = 0,
        sorted = 1,
        showtimestamp = 2,
    }

    class MethodChunks
    {
        public bool Done = false;
        public List<byte[]> InstrumentationData = new List<byte[]>();
        public int LastChunk = -1;
    }

    class PgoDataLoader : IPgoSchemaDataLoader<TypeSystemEntityOrUnknown>
    {
        private TraceRuntimeDescToTypeSystemDesc _idParser;

        public PgoDataLoader(TraceRuntimeDescToTypeSystemDesc idParser)
        {
            _idParser = idParser;
        }

        public TypeSystemEntityOrUnknown TypeFromLong(long input)
        {
            if (input == 0)
                return new TypeSystemEntityOrUnknown(0);

            TypeDesc type = _idParser.ResolveTypeHandle(input, false);
            if (type != null)
            {
                return new TypeSystemEntityOrUnknown(type);
            }
            return new TypeSystemEntityOrUnknown(System.HashCode.Combine(input) | 0x7F000000);
        }
    }


    class Program
    {
        static Logger s_logger = new Logger();
        static int Main(string []args)
        {
            var options = CommandLineOptions.ParseCommandLine(args);

            if (options.Help)
            {
                PrintOutput(options.HelpText);
                return 1;
            }
            else
            {
                return InnerMain(options);
            }
        }

        static void PrintUsage(CommandLineOptions commandLineOptions, string argValidationIssue)
        {
            if (argValidationIssue != null)
            {
                PrintError(argValidationIssue);
            }
            Main(commandLineOptions.HelpArgs);
        }

        static void PrintWarning(string warning)
        {
            s_logger.PrintWarning(warning);
        }

        static void PrintError(string error)
        {
            s_logger.PrintError(error);
        }

        static void PrintMessage(string message)
        {
            s_logger.PrintMessage(message);
        }

        static void PrintOutput(string output)
        {
            s_logger.PrintOutput(output);
        }

        struct ProcessedMethodData
        {
            public ProcessedMethodData(double millisecond, MethodDesc method, string reason)
            {
                Millisecond = millisecond;
                Method = method;
                Reason = reason;
                WeightedCallData = null;
                ExclusiveWeight = 0;
                InstrumentationData = null;
            }

            public readonly double Millisecond;
            public readonly MethodDesc Method;
            public readonly string Reason;
            public Dictionary<MethodDesc, int> WeightedCallData;
            public int ExclusiveWeight;
            public PgoSchemaElem[] InstrumentationData;

            public override string ToString()
            {
                return Method.ToString();
            }
        }

        struct InstructionPointerRange : IComparable<InstructionPointerRange>
        {
            public InstructionPointerRange(ulong startAddress, int size)
            {
                StartAddress = startAddress;
                EndAddress = startAddress + (ulong)size;
            }

            public ulong StartAddress;
            public ulong EndAddress;

            public int CompareTo(InstructionPointerRange other)
            {
                if (StartAddress < other.StartAddress)
                {
                    return -1;
                }
                if (StartAddress > other.StartAddress)
                {
                    return 1;
                }
                return (int)((long)EndAddress - (long)other.EndAddress);
            }
        }

        internal static void UnZipIfNecessary(ref string inputFileName, TextWriter log)
        {
            if (inputFileName.EndsWith(".trace.zip", StringComparison.OrdinalIgnoreCase))
            {
                log.WriteLine($"'{inputFileName}' is a linux trace.");
                return;
            }

            var extension = Path.GetExtension(inputFileName);
            if (string.Compare(extension, ".zip", StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(extension, ".vspx", StringComparison.OrdinalIgnoreCase) == 0)
            {
                string unzipedEtlFile;
                if (inputFileName.EndsWith(".etl.zip", StringComparison.OrdinalIgnoreCase))
                {
                    unzipedEtlFile = inputFileName.Substring(0, inputFileName.Length - 4);
                }
                else if (inputFileName.EndsWith(".vspx", StringComparison.OrdinalIgnoreCase))
                {
                    unzipedEtlFile = Path.ChangeExtension(inputFileName, ".etl");
                }
                else
                {
                    throw new ApplicationException("File does not end with the .etl.zip file extension");
                }

                ZippedETLReader etlReader = new ZippedETLReader(inputFileName, log);
                etlReader.EtlFileName = unzipedEtlFile;

                // Figure out where to put the symbols.  
                var inputDir = Path.GetDirectoryName(inputFileName);
                if (inputDir.Length == 0)
                {
                    inputDir = ".";
                }

                var symbolsDir = Path.Combine(inputDir, "symbols");
                etlReader.SymbolDirectory = symbolsDir;
                if (!Directory.Exists(symbolsDir))
                    Directory.CreateDirectory(symbolsDir);
                log.WriteLine("Putting symbols in {0}", etlReader.SymbolDirectory);

                etlReader.UnpackArchive();
                inputFileName = unzipedEtlFile;
            }
        }

        static int InnerMain(CommandLineOptions commandLineOptions)
        {
            if (!commandLineOptions.BasicProgressMessages)
                s_logger.HideMessages();

            if (commandLineOptions.TraceFile == null)
            {
                PrintUsage(commandLineOptions, "--trace-file must be specified");
                return -8;
            }

            if (commandLineOptions.OutputFileName != null)
            {
                if (!commandLineOptions.FileType.HasValue)
                {
                    PrintUsage(commandLineOptions, $"--pgo-file-type must be specified");
                    return -9;
                }
                if ((commandLineOptions.FileType.Value != PgoFileType.jittrace) && (commandLineOptions.FileType != PgoFileType.mibc))
                {
                    PrintUsage(commandLineOptions, $"Invalid output pgo type {commandLineOptions.FileType} specified.");
                    return -9;
                }
                if (commandLineOptions.FileType == PgoFileType.jittrace)
                {
                    if (!commandLineOptions.OutputFileName.Name.EndsWith(".jittrace"))
                    {
                        PrintUsage(commandLineOptions, $"jittrace output file name must end with .jittrace");
                        return -9;
                    }
                }
                if (commandLineOptions.FileType == PgoFileType.mibc)
                {
                    if (!commandLineOptions.OutputFileName.Name.EndsWith(".mibc"))
                    {
                        PrintUsage(commandLineOptions, $"mibc output file name must end with .mibc");
                        return -9;
                    }
                }
            }

            string etlFileName = commandLineOptions.TraceFile.FullName;
            foreach (string nettraceExtension in new string[] { ".netperf", ".netperf.zip", ".nettrace" })
            {
                if (commandLineOptions.TraceFile.FullName.EndsWith(nettraceExtension))
                {
                    etlFileName = commandLineOptions.TraceFile.FullName.Substring(0, commandLineOptions.TraceFile.FullName.Length - nettraceExtension.Length) + ".etlx";
                    PrintMessage($"Creating ETLX file {etlFileName} from {commandLineOptions.TraceFile.FullName}");
                    TraceLog.CreateFromEventPipeDataFile(commandLineOptions.TraceFile.FullName, etlFileName);
                }
            }

            string lttngExtension = ".trace.zip";
            if (commandLineOptions.TraceFile.FullName.EndsWith(lttngExtension))
            {
                etlFileName = commandLineOptions.TraceFile.FullName.Substring(0, commandLineOptions.TraceFile.FullName.Length - lttngExtension.Length) + ".etlx";
                PrintMessage($"Creating ETLX file {etlFileName} from {commandLineOptions.TraceFile.FullName}");
                TraceLog.CreateFromLttngTextDataFile(commandLineOptions.TraceFile.FullName, etlFileName);
            }

            UnZipIfNecessary(ref etlFileName, commandLineOptions.BasicProgressMessages ? Console.Out : new StringWriter());

            using (var traceLog = TraceLog.OpenOrConvert(etlFileName))
            {
                if ((!commandLineOptions.Pid.HasValue && commandLineOptions.ProcessName == null) && traceLog.Processes.Count != 1)
                {
                    PrintError("Trace file contains multiple processes to distinguish between");
                    PrintOutput("Either a pid or process name from the following list must be specified");
                    foreach (TraceProcess proc in traceLog.Processes)
                    {
                        PrintOutput($"Procname = {proc.Name} Pid = {proc.ProcessID}");
                    }
                    return 1;
                }

                if (commandLineOptions.Pid.HasValue && (commandLineOptions.ProcessName != null))
                {
                    PrintError("--pid and --process-name cannot be specified together");
                    return -1;
                }

                // For a particular process
                TraceProcess p;
                if (commandLineOptions.Pid.HasValue)
                {
                    p = traceLog.Processes.LastProcessWithID(commandLineOptions.Pid.Value);
                }
                else if (commandLineOptions.ProcessName != null)
                {
                    List<TraceProcess> matchingProcesses = new List<TraceProcess>();
                    foreach (TraceProcess proc in traceLog.Processes)
                    {
                        if (String.Compare(proc.Name, commandLineOptions.ProcessName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            matchingProcesses.Add(proc);
                        }
                    }

                    if (matchingProcesses.Count == 0)
                    {
                        PrintError("Unable to find matching process in trace");
                        return -1;
                    }
                    if (matchingProcesses.Count > 1)
                    {
                        StringBuilder errorMessage = new StringBuilder();

                        errorMessage.AppendLine("Found multiple matching processes in trace");
                        foreach (TraceProcess proc in matchingProcesses)
                        {
                            errorMessage.AppendLine($"{proc.Name}\tpid={proc.ProcessID}\tCPUMSec={proc.CPUMSec}");
                        }
                        PrintError(errorMessage.ToString());
                        return -2;
                    }
                    p = matchingProcesses[0];
                }
                else
                {
                    p = traceLog.Processes.First();
                }

                if (!p.EventsInProcess.ByEventType<MethodDetailsTraceData>().Any())
                {
                    PrintError($"No MethodDetails\nWas the trace collected with provider at least \"Microsoft-Windows-DotNETRuntime:0x4000080018:5\"?");
                    return -3;
                }

                if (!p.EventsInProcess.ByEventType<GCBulkTypeTraceData>().Any())
                {
                    PrintError($"No BulkType data\nWas the trace collected with provider at least \"Microsoft-Windows-DotNETRuntime:0x4000080018:5\"?");
                    return -4;
                }

                if (!p.EventsInProcess.ByEventType<ModuleLoadUnloadTraceData>().Any())
                {
                    PrintError($"No managed module load data\nWas the trace collected with provider at least \"Microsoft-Windows-DotNETRuntime:0x4000080018:5\"?");
                    return -5;
                }

                if (!p.EventsInProcess.ByEventType<MethodJittingStartedTraceData>().Any())
                {
                    PrintError($"No managed jit starting data\nWas the trace collected with provider at least \"Microsoft-Windows-DotNETRuntime:0x4000080018:5\"?");
                    return -5;
                }

                if (commandLineOptions.ProcessR2REvents)
                {
                    if (!p.EventsInProcess.ByEventType<R2RGetEntryPointTraceData>().Any())
                    {
                        PrintError($"No r2r entrypoint data. This is not an error as in this case we can examine the jitted methods only\nWas the trace collected with provider at least \"Microsoft-Windows-DotNETRuntime:0x6000080018:5\"?");
                    }
                }

                PgoTraceProcess pgoProcess = new PgoTraceProcess(p);
                int? clrInstanceId = commandLineOptions.ClrInstanceId;
                if (!clrInstanceId.HasValue)
                {
                    HashSet<int> clrInstanceIds = new HashSet<int>();
                    HashSet<int> examinedClrInstanceIds = new HashSet<int>();
                    foreach (var assemblyLoadTrace in p.EventsInProcess.ByEventType<AssemblyLoadUnloadTraceData>())
                    {
                        if (examinedClrInstanceIds.Add(assemblyLoadTrace.ClrInstanceID))
                        {
                            if (pgoProcess.ClrInstanceIsCoreCLRInstance(assemblyLoadTrace.ClrInstanceID))
                                clrInstanceIds.Add(assemblyLoadTrace.ClrInstanceID);
                        }
                    }

                    if (clrInstanceIds.Count != 1)
                    {
                        if (clrInstanceIds.Count == 0)
                        {
                            PrintError($"No managed CLR in target process, or per module information could not be loaded from the trace.");
                        }
                        else
                        {
                            // There are multiple clr processes... search for the one that implements
                            int[] clrInstanceIdsArray = clrInstanceIds.ToArray();
                            Array.Sort(clrInstanceIdsArray);
                            StringBuilder errorMessage = new StringBuilder();
                            errorMessage.AppendLine("Multiple CLR instances used in process. Choose one to examine with -clrInstanceID:<id> Valid ids:");
                            foreach (int instanceID in clrInstanceIds)
                            {
                                errorMessage.AppendLine(instanceID.ToString());
                            }
                            PrintError(errorMessage.ToString());
                        }
                        return -10;
                    }
                    else
                    {
                        clrInstanceId = clrInstanceIds.First();
                    }
                }

                var tsc = new TraceTypeSystemContext(pgoProcess, clrInstanceId.Value, s_logger);

                if (commandLineOptions.VerboseWarnings)
                    PrintWarning($"{traceLog.EventsLost} Lost events");

                bool filePathError = false;
                if (commandLineOptions.Reference != null)
                {
                    foreach (FileInfo fileReference in commandLineOptions.Reference)
                    {
                        if (!File.Exists(fileReference.FullName))
                        {
                            PrintError($"Unable to find reference '{fileReference.FullName}'");
                            filePathError = true;
                        }
                        else
                            tsc.GetModuleFromPath(fileReference.FullName);
                    }
                }

                if (filePathError)
                    return -6;

                if (!tsc.Initialize())
                    return -12;

                TraceRuntimeDescToTypeSystemDesc idParser = new TraceRuntimeDescToTypeSystemDesc(p, tsc, clrInstanceId.Value);

                SortedDictionary<int, ProcessedMethodData> methodsToAttemptToPrepare = new SortedDictionary<int, ProcessedMethodData>();

                if (commandLineOptions.ProcessR2REvents)
                {
                    foreach (var e in p.EventsInProcess.ByEventType<R2RGetEntryPointTraceData>())
                    {
                        int parenIndex = e.MethodSignature.IndexOf('(');
                        string retArg = e.MethodSignature.Substring(0, parenIndex);
                        string paramsArgs = e.MethodSignature.Substring(parenIndex);
                        string methodNameFromEventDirectly = retArg + e.MethodNamespace + "." + e.MethodName + paramsArgs;
                        if (e.ClrInstanceID != clrInstanceId.Value)
                        {
                            if (!commandLineOptions.Warnings)
                                continue;

                            PrintWarning($"Skipped R2REntryPoint {methodNameFromEventDirectly} due to ClrInstanceID of {e.ClrInstanceID}");
                            continue;
                        }
                        MethodDesc method = null;
                        string extraWarningText = null;
                        try
                        {
                            method = idParser.ResolveMethodID(e.MethodID, commandLineOptions.VerboseWarnings);
                        }
                        catch (Exception exception)
                        {
                            extraWarningText = exception.ToString();
                        }

                        if (method == null)
                        {
                            if ((e.MethodNamespace == "dynamicClass") || !commandLineOptions.Warnings)
                                continue;

                            PrintWarning($"Unable to parse {methodNameFromEventDirectly} when looking up R2R methods");
                            if (extraWarningText != null)
                                PrintWarning(extraWarningText);
                            continue;
                        }

                        if ((e.TimeStampRelativeMSec >= commandLineOptions.ExcludeEventsBefore) && (e.TimeStampRelativeMSec <= commandLineOptions.ExcludeEventsAfter))
                            methodsToAttemptToPrepare.Add((int)e.EventIndex, new ProcessedMethodData(e.TimeStampRelativeMSec, method, "R2RLoad"));
                    }
                }

                // Find all the jitStart events.
                if (commandLineOptions.ProcessJitEvents)
                {
                    foreach (var e in p.EventsInProcess.ByEventType<MethodJittingStartedTraceData>())
                    {
                        int parenIndex = e.MethodSignature.IndexOf('(');
                        string retArg = e.MethodSignature.Substring(0, parenIndex);
                        string paramsArgs = e.MethodSignature.Substring(parenIndex);
                        string methodNameFromEventDirectly = retArg + e.MethodNamespace + "." + e.MethodName + paramsArgs;
                        if (e.ClrInstanceID != clrInstanceId.Value)
                        {
                            if (!commandLineOptions.Warnings)
                                continue;

                            PrintWarning($"Skipped {methodNameFromEventDirectly} due to ClrInstanceID of {e.ClrInstanceID}");
                            continue;
                        }

                        MethodDesc method = null;
                        string extraWarningText = null;
                        try
                        {
                            method = idParser.ResolveMethodID(e.MethodID, commandLineOptions.VerboseWarnings);
                        }
                        catch (Exception exception)
                        {
                            extraWarningText = exception.ToString();
                        }

                        if (method == null)
                        {
                            if (!commandLineOptions.Warnings)
                                continue;

                            PrintWarning($"Unable to parse {methodNameFromEventDirectly}");
                            if (extraWarningText != null)
                                PrintWarning(extraWarningText);
                            continue;
                        }

                        if ((e.TimeStampRelativeMSec >= commandLineOptions.ExcludeEventsBefore) && (e.TimeStampRelativeMSec <= commandLineOptions.ExcludeEventsAfter))
                            methodsToAttemptToPrepare.Add((int)e.EventIndex, new ProcessedMethodData(e.TimeStampRelativeMSec, method, "JitStart"));
                    }
                }

                Dictionary<MethodDesc, Dictionary<MethodDesc, int>> callGraph = null;
                Dictionary<MethodDesc, int> exclusiveSamples = null;
                if (commandLineOptions.GenerateCallGraph)
                {
                    HashSet<MethodDesc> methodsListedToPrepare = new HashSet<MethodDesc>();
                    foreach (var entry in methodsToAttemptToPrepare)
                    {
                        methodsListedToPrepare.Add(entry.Value.Method);
                    }

                    callGraph = new Dictionary<MethodDesc, Dictionary<MethodDesc, int>>();
                    exclusiveSamples = new Dictionary<MethodDesc, int>();
                    // Capture the addresses of jitted code
                    List<ValueTuple<InstructionPointerRange, MethodDesc>> codeLocations = new List<(InstructionPointerRange, MethodDesc)>();
                    foreach (var e in p.EventsInProcess.ByEventType<MethodLoadUnloadTraceData>())
                    {
                        if (e.ClrInstanceID != clrInstanceId.Value)
                        {
                            continue;
                        }

                        MethodDesc method = null;
                        try
                        {
                            method = idParser.ResolveMethodID(e.MethodID, commandLineOptions.VerboseWarnings);
                        }
                        catch (Exception)
                        {
                        }

                        if (method != null)
                        {
                            codeLocations.Add((new InstructionPointerRange(e.MethodStartAddress, e.MethodSize), method));
                        }
                    }
                    foreach (var e in p.EventsInProcess.ByEventType<MethodLoadUnloadVerboseTraceData>())
                    {
                        if (e.ClrInstanceID != clrInstanceId.Value)
                        {
                            continue;
                        }

                        MethodDesc method = null;
                        try
                        {
                            method = idParser.ResolveMethodID(e.MethodID, commandLineOptions.VerboseWarnings);
                        }
                        catch (Exception)
                        {
                        }

                        if (method != null)
                        {
                            codeLocations.Add((new InstructionPointerRange(e.MethodStartAddress, e.MethodSize), method));
                        }
                    }

                    var sigProvider = new R2RSignatureTypeProvider(tsc);
                    foreach (var module in p.LoadedModules)
                    {
                        if (module.FilePath == "")
                            continue;

                        if (!File.Exists(module.FilePath))
                            continue;

                        try
                        {
                            byte[] image = File.ReadAllBytes(module.FilePath);
                            using (FileStream fstream = new FileStream(module.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                var r2rCheckPEReader = new System.Reflection.PortableExecutable.PEReader(fstream, System.Reflection.PortableExecutable.PEStreamOptions.LeaveOpen);

                                if (!ILCompiler.Reflection.ReadyToRun.ReadyToRunReader.IsReadyToRunImage(r2rCheckPEReader))
                                    continue;
                            }

                            var reader = new ILCompiler.Reflection.ReadyToRun.ReadyToRunReader(tsc, module.FilePath);
                            foreach (var methodEntry in reader.GetCustomMethodToRuntimeFunctionMapping<TypeDesc, MethodDesc, R2RSigProviderContext>(sigProvider))
                            {
                                foreach (var runtimeFunction in methodEntry.Value.RuntimeFunctions)
                                {
                                    codeLocations.Add((new InstructionPointerRange(module.ImageBase + (ulong)runtimeFunction.StartAddress, runtimeFunction.Size), methodEntry.Key));
                                }
                            }
                        }
                        catch { }
                    }

                    InstructionPointerRange[] instructionPointerRanges = new InstructionPointerRange[codeLocations.Count];
                    MethodDesc[] methods = new MethodDesc[codeLocations.Count];
                    for (int i = 0; i < codeLocations.Count; i++)
                    {
                        instructionPointerRanges[i] = codeLocations[i].Item1;
                        methods[i] = codeLocations[i].Item2;
                    }

                    Array.Sort(instructionPointerRanges, methods);

                    foreach (var e in p.EventsInProcess.ByEventType<SampledProfileTraceData>())
                    {
                        var callstack = e.CallStack();
                        if (callstack == null)
                            continue;

                        ulong address1 = callstack.CodeAddress.Address;
                        MethodDesc topOfStackMethod = LookupMethodByAddress(address1);
                        MethodDesc nextMethod = null;
                        if (callstack.Caller != null)
                        {
                            ulong address2 = callstack.Caller.CodeAddress.Address;
                            nextMethod = LookupMethodByAddress(address2);
                        }

                        if (topOfStackMethod != null)
                        {
                            if (!methodsListedToPrepare.Contains(topOfStackMethod))
                            {
                                methodsListedToPrepare.Add(topOfStackMethod);
                                methodsToAttemptToPrepare.Add((int)e.EventIndex, new ProcessedMethodData(e.TimeStampRelativeMSec, topOfStackMethod, "SampleMethod"));
                            }

                            if (exclusiveSamples.TryGetValue(topOfStackMethod, out int count))
                            {
                                exclusiveSamples[topOfStackMethod] = count + 1;
                            }
                            else
                            {
                                exclusiveSamples[topOfStackMethod] = 1;
                            }
                        }

                        if (topOfStackMethod != null && nextMethod != null)
                        {
                            if (!methodsListedToPrepare.Contains(nextMethod))
                            {
                                methodsListedToPrepare.Add(nextMethod);
                                methodsToAttemptToPrepare.Add((int)e.EventIndex, new ProcessedMethodData(e.TimeStampRelativeMSec, nextMethod, "SampleMethodCaller"));
                            }

                            if (!callGraph.TryGetValue(nextMethod, out var innerDictionary))
                            {
                                innerDictionary = new Dictionary<MethodDesc, int>();
                                callGraph[nextMethod] = innerDictionary;
                            }
                            if (innerDictionary.TryGetValue(topOfStackMethod, out int count))
                            {
                                innerDictionary[topOfStackMethod] = count + 1;
                            }
                            else
                            {
                                innerDictionary[topOfStackMethod] = 1;
                            }
                        }
                    }

                    MethodDesc LookupMethodByAddress(ulong address)
                    {
                        int index = Array.BinarySearch(instructionPointerRanges, new InstructionPointerRange(address, 1));

                        if (index >= 0)
                        {
                            return methods[index];
                        }
                        else
                        {
                            index = ~index;
                            if (index >= instructionPointerRanges.Length)
                                return null;

                            if (instructionPointerRanges[index].StartAddress < address)
                            {
                                if (instructionPointerRanges[index].EndAddress > address)
                                {
                                    return methods[index];
                                }
                            }

                            if (index == 0)
                                return null;

                            index--;

                            if (instructionPointerRanges[index].StartAddress < address)
                            {
                                if (instructionPointerRanges[index].EndAddress > address)
                                {
                                    return methods[index];
                                }
                            }

                            return null;
                        }
                    }
                }

                Dictionary<MethodDesc, MethodChunks> instrumentationDataByMethod = new Dictionary<MethodDesc, MethodChunks>();

                foreach (var e in p.EventsInProcess.ByEventType<JitInstrumentationDataVerboseTraceData>())
                {
                    AddToInstrumentationData(e.ClrInstanceID, e.MethodID, e.MethodFlags, e.Data);
                }
                foreach (var e in p.EventsInProcess.ByEventType<JitInstrumentationDataTraceData>())
                {
                    AddToInstrumentationData(e.ClrInstanceID, e.MethodID, e.MethodFlags, e.Data);
                }

                // Local function used with the above two loops as the behavior is supposed to be identical
                void AddToInstrumentationData(int eventClrInstanceId, long methodID, int methodFlags, byte[] data)
                {
                    if (eventClrInstanceId != clrInstanceId.Value)
                    {
                        return;
                    }

                    MethodDesc method = null;
                    try
                    {
                        method = idParser.ResolveMethodID(methodID, commandLineOptions.VerboseWarnings);
                    }
                    catch (Exception)
                    {
                    }

                    if (method != null)
                    {
                        if (!instrumentationDataByMethod.TryGetValue(method, out MethodChunks perMethodChunks))
                        {
                            perMethodChunks = new MethodChunks();
                            instrumentationDataByMethod.Add(method, perMethodChunks);
                        }
                        const int FinalChunkFlag = unchecked((int)0x80000000);
                        int chunkIndex = methodFlags & ~FinalChunkFlag;
                        if ((chunkIndex != (perMethodChunks.LastChunk + 1)) || perMethodChunks.Done)
                        {
                            instrumentationDataByMethod.Remove(method);
                            return;
                        }
                        perMethodChunks.LastChunk = perMethodChunks.InstrumentationData.Count;
                        perMethodChunks.InstrumentationData.Add(data);
                        if ((methodFlags & FinalChunkFlag) == FinalChunkFlag)
                            perMethodChunks.Done = true;
                    }
                }


                if (commandLineOptions.DisplayProcessedEvents)
                {
                    foreach (var entry in methodsToAttemptToPrepare)
                    {
                        MethodDesc method = entry.Value.Method;
                        string reason = entry.Value.Reason;
                        PrintOutput($"{entry.Value.Millisecond.ToString("F4")} {reason} {method}");
                    }
                }

                PrintMessage($"Done processing input file");

                if (commandLineOptions.OutputFileName == null)
                {
                    return 0;
                }

                // Deduplicate entries
                HashSet<MethodDesc> methodsInListAlready = new HashSet<MethodDesc>();
                List<ProcessedMethodData> methodsUsedInProcess = new List<ProcessedMethodData>();

                PgoDataLoader pgoDataLoader = new PgoDataLoader(idParser);

                foreach (var entry in methodsToAttemptToPrepare)
                {
                    if (methodsInListAlready.Add(entry.Value.Method))
                    {
                        var methodData = entry.Value;
                        if (commandLineOptions.GenerateCallGraph)
                        {
                            exclusiveSamples.TryGetValue(methodData.Method, out methodData.ExclusiveWeight);
                            callGraph.TryGetValue(methodData.Method, out methodData.WeightedCallData);
                        }
                        if (instrumentationDataByMethod.TryGetValue(methodData.Method, out MethodChunks chunks))
                        {
                            int size = 0;
                            foreach (byte[] arr in chunks.InstrumentationData)
                            {
                                size += arr.Length;
                            }

                            byte[] instrumentationData = new byte[size];
                            int offset = 0;

                            foreach (byte[] arr in chunks.InstrumentationData)
                            {
                                arr.CopyTo(instrumentationData, offset);
                                offset += arr.Length;
                            }

                            var intDecompressor = new PgoProcessor.PgoEncodedCompressedIntParser(instrumentationData, 0);
                            methodData.InstrumentationData = PgoProcessor.ParsePgoData<TypeSystemEntityOrUnknown>(pgoDataLoader, intDecompressor, true).ToArray();
                        }
                        methodsUsedInProcess.Add(methodData);
                    }
                }

                 if (commandLineOptions.FileType.Value == PgoFileType.jittrace)
                    GenerateJittraceFile(commandLineOptions.OutputFileName, methodsUsedInProcess, commandLineOptions.JitTraceOptions);
                else if (commandLineOptions.FileType.Value == PgoFileType.mibc)
                    return GenerateMibcFile(tsc, commandLineOptions.OutputFileName, methodsUsedInProcess, commandLineOptions.ValidateOutputFile, commandLineOptions.Uncompressed);
            }
            return 0;
        }

        class MIbcGroup : IPgoEncodedValueEmitter<TypeSystemEntityOrUnknown>
        {
            private static int s_emitCount = 0;

            public MIbcGroup(string name, TypeSystemMetadataEmitter emitter)
            {
                _buffer = new BlobBuilder();
                _il = new InstructionEncoder(_buffer);
                _name = name;
                _emitter = emitter;
            }

            private BlobBuilder _buffer;
            private InstructionEncoder _il;
            private string _name;
            private TypeSystemMetadataEmitter _emitter;

            public void AddProcessedMethodData(ProcessedMethodData processedMethodData)
            {
                MethodDesc method = processedMethodData.Method;
                string reason = processedMethodData.Reason;

                // Format is 
                // ldtoken method
                // variable amount of extra metadata about the method, Extension data is encoded via ldstr "id"
                // pop

                // Extensions generated by this emitter:
                //
                // ldstr "ExclusiveWeight"
                // Any ldc.i4 or ldc.r4 or ldc.r8 instruction to indicate the exclusive weight
                //
                // ldstr "WeightedCallData"
                // ldc.i4 <Count of methods called>
                // Repeat <Count of methods called times>
                //  ldtoken <Method called from this method>
                //  ldc.i4 <Weight associated with calling the <Method called from this method>>
                //
                // ldstr "InstrumentationDataStart"
                // Encoded ints and longs, using ldc.i4, and ldc.i8 instructions as well as ldtoken <type> instructions
                // ldstr "InstrumentationDataEnd" as a terminator
                try
                {
                    EntityHandle methodHandle = _emitter.GetMethodRef(method);
                    _il.OpCode(ILOpCode.Ldtoken);
                    _il.Token(methodHandle);
                    if (processedMethodData.ExclusiveWeight != 0)
                    {
                        _il.LoadString(_emitter.GetUserStringHandle("ExclusiveWeight"));
                        _il.LoadConstantI4(processedMethodData.ExclusiveWeight);
                    }
                    if (processedMethodData.WeightedCallData != null)
                    {
                        _il.LoadString(_emitter.GetUserStringHandle("WeightedCallData"));
                        _il.LoadConstantI4(processedMethodData.WeightedCallData.Count);
                        foreach (var entry in processedMethodData.WeightedCallData)
                        {
                            EntityHandle calledMethod = _emitter.GetMethodRef(entry.Key);
                            _il.OpCode(ILOpCode.Ldtoken);
                            _il.Token(calledMethod);
                            _il.LoadConstantI4(entry.Value);
                        }
                    }
                    if (processedMethodData.InstrumentationData != null)
                    {
                        _il.LoadString(_emitter.GetUserStringHandle("InstrumentationDataStart"));
                        PgoProcessor.EncodePgoData<TypeSystemEntityOrUnknown>(processedMethodData.InstrumentationData, this, true);
                    }
                    _il.OpCode(ILOpCode.Pop);
                }
                catch (Exception ex)
                {
                    PrintWarning($"Exception {ex} while attempting to generate method lists");
                }
            }

            public MethodDefinitionHandle EmitMethod()
            {
                s_emitCount++;
                string basicName = "Assemblies_" + _name;
                if (_name.Length > 200)
                    basicName = basicName.Substring(0, 200); // Cap length of name at 200, which is reasonably small.

                string methodName = basicName + "_" + s_emitCount.ToString(CultureInfo.InvariantCulture);
                return _emitter.AddGlobalMethod(methodName, _il, 8);
            }

            bool IPgoEncodedValueEmitter<TypeSystemEntityOrUnknown>.EmitDone()
            {
                _il.LoadString(_emitter.GetUserStringHandle("InstrumentationDataEnd"));
                return true;
            }

            void IPgoEncodedValueEmitter<TypeSystemEntityOrUnknown>.EmitLong(long value, long previousValue)
            {
                if ((value <= int.MaxValue) && (value >= int.MinValue))
                {
                    _il.LoadConstantI4(checked((int)value));
                }
                else
                {
                    _il.LoadConstantI8(value);
                }
            }

            void IPgoEncodedValueEmitter<TypeSystemEntityOrUnknown>.EmitType(TypeSystemEntityOrUnknown type, TypeSystemEntityOrUnknown previousValue)
            {
                if (type.AsType != null)
                {
                    _il.OpCode(ILOpCode.Ldtoken);
                    _il.Token(_emitter.GetTypeRef(type.AsType));
                }
                else
                    _il.LoadConstantI4(type.AsUnknown);
            }

        }

        private static void AddAssembliesAssociatedWithType(TypeDesc type, HashSet<string> assemblies, out string definingAssembly)
        {
            definingAssembly = ((MetadataType)type).Module.Assembly.GetName().Name;
            assemblies.Add(definingAssembly);
            AddAssembliesAssociatedWithType(type, assemblies);
        }

        private static void AddAssembliesAssociatedWithType(TypeDesc type, HashSet<string> assemblies)
        {
            if (type.IsPrimitive)
                return;

            if (type.Context.IsCanonicalDefinitionType(type, CanonicalFormKind.Any))
                return;

            if (type.IsParameterizedType)
            {
                AddAssembliesAssociatedWithType(type.GetParameterType(), assemblies);
            }
            else
            {
                assemblies.Add(((MetadataType)type).Module.Assembly.GetName().Name);
                foreach (var instantiationType in type.Instantiation)
                {
                    AddAssembliesAssociatedWithType(instantiationType, assemblies);
                }
            }
        }

        private static void AddAssembliesAssociatedWithMethod(MethodDesc method, HashSet<string> assemblies, out string definingAssembly)
        {
            AddAssembliesAssociatedWithType(method.OwningType, assemblies, out definingAssembly);
            foreach (var instantiationType in method.Instantiation)
            {
                AddAssembliesAssociatedWithType(instantiationType, assemblies);
            }
        }

        static int GenerateMibcFile(TraceTypeSystemContext tsc, FileInfo outputFileName, ICollection<ProcessedMethodData> methodsToAttemptToPlaceIntoProfileData, bool validate, bool uncompressed)
        {
            TypeSystemMetadataEmitter emitter = new TypeSystemMetadataEmitter(new AssemblyName(outputFileName.Name), tsc);

            SortedDictionary<string, MIbcGroup> groups = new SortedDictionary<string, MIbcGroup>();
            StringBuilder mibcGroupNameBuilder = new StringBuilder();
            HashSet<string> assembliesAssociatedWithMethod = new HashSet<string>();

            foreach (var entry in methodsToAttemptToPlaceIntoProfileData)
            {
                MethodDesc method = entry.Method;
                assembliesAssociatedWithMethod.Clear();
                AddAssembliesAssociatedWithMethod(method, assembliesAssociatedWithMethod, out string definingAssembly);

                string[] assemblyNames = new string[assembliesAssociatedWithMethod.Count];
                int i = 1;
                assemblyNames[0] = definingAssembly;

                foreach (string s in assembliesAssociatedWithMethod)
                {
                    if (s.Equals(definingAssembly))
                        continue;
                    assemblyNames[i++] = s;
                }

                // Always keep the defining assembly as the first name
                Array.Sort(assemblyNames, 1, assemblyNames.Length - 1);
                mibcGroupNameBuilder.Clear();
                foreach (string s in assemblyNames)
                {
                    mibcGroupNameBuilder.Append(s);
                    mibcGroupNameBuilder.Append(';');
                }

                string mibcGroupName = mibcGroupNameBuilder.ToString();
                if (!groups.TryGetValue(mibcGroupName, out MIbcGroup mibcGroup))
                {
                    mibcGroup = new MIbcGroup(mibcGroupName, emitter);
                    groups.Add(mibcGroupName, mibcGroup);
                }
                mibcGroup.AddProcessedMethodData(entry);
            }

            var buffer = new BlobBuilder();
            var il = new InstructionEncoder(buffer);

            foreach (var entry in groups)
            {
                il.LoadString(emitter.GetUserStringHandle(entry.Key));
                il.OpCode(ILOpCode.Ldtoken);
                il.Token(entry.Value.EmitMethod());
                il.OpCode(ILOpCode.Pop);
            }

            emitter.AddGlobalMethod("AssemblyDictionary", il, 8);
            MemoryStream peFile = new MemoryStream();
            emitter.SerializeToStream(peFile);
            peFile.Position = 0;

            if (outputFileName.Exists)
            {
                outputFileName.Delete();
            }

            if (uncompressed)
            {
                using (FileStream file = new FileStream(outputFileName.FullName, FileMode.Create))
                {
                    peFile.CopyTo(file);
                }
            }
            else
            {
                using (ZipArchive file = ZipFile.Open(outputFileName.FullName, ZipArchiveMode.Create))
                {
                    var entry = file.CreateEntry(outputFileName.Name + ".dll", CompressionLevel.Optimal);
                    using (Stream archiveStream = entry.Open())
                    {
                        peFile.CopyTo(archiveStream);
                    }
                }
            }

            PrintMessage($"Generated {outputFileName.FullName}");

            if (validate)
                return ValidateMIbcData(tsc, outputFileName, peFile.ToArray(), methodsToAttemptToPlaceIntoProfileData);
            else
                return 0;
        }

        struct MIbcData
        {
            public object MetadataObject;
        }

        static int ValidateMIbcData(TraceTypeSystemContext tsc, FileInfo outputFileName, byte[] moduleBytes, ICollection<ProcessedMethodData> methodsToAttemptToPrepare)
        {
            var mibcLoadedData = ReadMIbcData(tsc, outputFileName, moduleBytes).ToArray();
            Dictionary<MethodDesc, MIbcData> mibcDict = new Dictionary<MethodDesc, MIbcData>();

            foreach (var mibcData in mibcLoadedData)
            {
                mibcDict.Add((MethodDesc)mibcData.MetadataObject, mibcData);
            }

            bool failure = false;
            if (methodsToAttemptToPrepare.Count != mibcLoadedData.Length)
            {
                PrintError($"Not same count of methods {methodsToAttemptToPrepare.Count} != {mibcLoadedData.Length}");
                failure = true;
            }

            foreach (var entry in methodsToAttemptToPrepare)
            {
                MethodDesc method = entry.Method;
                if (!mibcDict.ContainsKey(method))
                {
                    PrintError($"{method} not found in mibcEntryData");
                    failure = true;
                    continue;
                }
            }

            if (failure)
            {
                return -1;
            }
            else
            {
                PrintMessage($"Validated {outputFileName.FullName}");
                return 0;
            }
        }

        static IEnumerable<MIbcData> ReadMIbcGroup(TypeSystemContext tsc, EcmaMethod method)
        {
            EcmaMethodIL ilBody = EcmaMethodIL.Create((EcmaMethod)method);
            byte[] ilBytes = ilBody.GetILBytes();
            int currentOffset = 0;
            object metadataObject = null;
            while (currentOffset < ilBytes.Length)
            {
                ILOpcode opcode = (ILOpcode)ilBytes[currentOffset];
                if (opcode == ILOpcode.prefix1)
                    opcode = 0x100 + (ILOpcode)ilBytes[currentOffset + 1];
                switch (opcode)
                {
                    case ILOpcode.ldtoken:
                        UInt32 token = BinaryPrimitives.ReadUInt32LittleEndian(ilBytes.AsSpan(currentOffset + 1));

                        if (metadataObject == null)
                            metadataObject = ilBody.GetObject((int)token);
                        break;
                    case ILOpcode.pop:
                        MIbcData mibcData = new MIbcData();
                        mibcData.MetadataObject = metadataObject;
                        yield return mibcData;

                        metadataObject = null;
                        break;
                }

                // This isn't correct if there is a switch opcode, but since we won't do that, its ok
                currentOffset += opcode.GetSize();
            }
        }

        class CanonModule : ModuleDesc, IAssemblyDesc
        {
            public CanonModule(TypeSystemContext wrappedContext) : base(wrappedContext, null)
            {
            }

            public override IEnumerable<MetadataType> GetAllTypes()
            {
                throw new NotImplementedException();
            }

            public override MetadataType GetGlobalModuleType()
            {
                throw new NotImplementedException();
            }

            public override MetadataType GetType(string nameSpace, string name, bool throwIfNotFound = true)
            {
                TypeSystemContext context = Context;

                if (context.SupportsCanon && (nameSpace == context.CanonType.Namespace) && (name == context.CanonType.Name))
                    return Context.CanonType;
                if (context.SupportsUniversalCanon && (nameSpace == context.UniversalCanonType.Namespace) && (name == context.UniversalCanonType.Name))
                    return Context.UniversalCanonType;
                else
                {
                    if (throwIfNotFound)
                    {
                        throw new TypeLoadException($"{nameSpace}.{name}");
                    }
                    return null;
                }
            }

            public AssemblyName GetName()
            {
                return new AssemblyName("System.Private.Canon");
            }
        }

        class CustomCanonResolver : IModuleResolver
        {
            CanonModule _canonModule;
            AssemblyName _canonModuleName;
            IModuleResolver _wrappedResolver;

            public CustomCanonResolver(TypeSystemContext wrappedContext)
            {
                _canonModule = new CanonModule(wrappedContext);
                _canonModuleName = _canonModule.GetName();
                _wrappedResolver = wrappedContext;
            }

            ModuleDesc IModuleResolver.ResolveAssembly(AssemblyName name, bool throwIfNotFound)
            {
                if (name.Name == _canonModuleName.Name)
                    return _canonModule;
                else
                    return _wrappedResolver.ResolveAssembly(name, throwIfNotFound);
            }

            ModuleDesc IModuleResolver.ResolveModule(IAssemblyDesc referencingModule, string fileName, bool throwIfNotFound)
            {
                return _wrappedResolver.ResolveModule(referencingModule, fileName, throwIfNotFound);
            }
        }

        static IEnumerable<MIbcData> ReadMIbcData(TraceTypeSystemContext tsc, FileInfo outputFileName, byte[] moduleBytes)
        {
            var peReader = new System.Reflection.PortableExecutable.PEReader(System.Collections.Immutable.ImmutableArray.Create<byte>(moduleBytes));
            var module = EcmaModule.Create(tsc, peReader, null, null, new CustomCanonResolver(tsc));

            var loadedMethod = (EcmaMethod)module.GetGlobalModuleType().GetMethod("AssemblyDictionary", null);
            EcmaMethodIL ilBody = EcmaMethodIL.Create(loadedMethod);
            byte[] ilBytes = ilBody.GetILBytes();
            int currentOffset = 0;
            while (currentOffset < ilBytes.Length)
            {
                ILOpcode opcode = (ILOpcode)ilBytes[currentOffset];
                if (opcode == ILOpcode.prefix1)
                    opcode = 0x100 + (ILOpcode)ilBytes[currentOffset + 1];
                switch (opcode)
                {
                    case ILOpcode.ldtoken:
                        UInt32 token = BinaryPrimitives.ReadUInt32LittleEndian(ilBytes.AsSpan(currentOffset + 1));
                        foreach (var data in ReadMIbcGroup(tsc, (EcmaMethod)ilBody.GetObject((int)token)))
                            yield return data;
                        break;
                    case ILOpcode.pop:
                        break;
                }

                // This isn't correct if there is a switch opcode, but since we won't do that, its ok
                currentOffset += opcode.GetSize();
            }
            GC.KeepAlive(peReader);
        }

        static void GenerateJittraceFile(FileInfo outputFileName, IEnumerable<ProcessedMethodData> methodsToAttemptToPrepare, jittraceoptions jittraceOptions)
        {
            PrintMessage($"JitTrace options {jittraceOptions}");

            List<string> methodsToPrepare = new List<string>();
            HashSet<string> prepareMethods = new HashSet<string>();

            Dictionary<TypeDesc, string> typeStringCache = new Dictionary<TypeDesc, string>();
            StringBuilder methodPrepareInstruction = new StringBuilder();

            StringBuilder instantiationBuilder = new StringBuilder();
            const string outerCsvEscapeChar = "~";
            const string innerCsvEscapeChar = ":";
            foreach (var entry in methodsToAttemptToPrepare)
            {
                MethodDesc method = entry.Method;
                string reason = entry.Reason;
                double time = entry.Millisecond;

                methodPrepareInstruction.Clear();
                instantiationBuilder.Clear();
                // Format is FriendlyNameOfMethod~typeIndex~ArgCount~GenericParameterCount:genericParamsSeperatedByColons~MethodName
                // This format is not sufficient to exactly describe methods, so the runtime component may compile similar methods
                // In the various strings \ is escaped to \\ and in the outer ~ csv the ~ character is escaped to \s. In the inner csv : is escaped to \s
                try
                {
                    string timeStampAddon = "";
                    if (jittraceOptions.HasFlag(jittraceoptions.showtimestamp))
                        timeStampAddon = time.ToString("F4") + "-";

                    methodPrepareInstruction.Append(CsvEscape(timeStampAddon + method.ToString(), outerCsvEscapeChar));
                    methodPrepareInstruction.Append(outerCsvEscapeChar);
                    methodPrepareInstruction.Append(CsvEscape(GetStringForType(method.OwningType, typeStringCache), outerCsvEscapeChar));
                    methodPrepareInstruction.Append(outerCsvEscapeChar);
                    methodPrepareInstruction.Append(method.Signature.Length);
                    methodPrepareInstruction.Append(outerCsvEscapeChar);

                    instantiationBuilder.Append(method.Instantiation.Length);
                    foreach (TypeDesc methodInstantiationType in method.Instantiation)
                    {
                        instantiationBuilder.Append(innerCsvEscapeChar);
                        instantiationBuilder.Append(CsvEscape(GetStringForType(methodInstantiationType, typeStringCache), innerCsvEscapeChar));
                    }

                    methodPrepareInstruction.Append(CsvEscape(instantiationBuilder.ToString(), outerCsvEscapeChar));
                    methodPrepareInstruction.Append(outerCsvEscapeChar);
                    methodPrepareInstruction.Append(CsvEscape(method.Name, outerCsvEscapeChar));
                }
                catch (Exception ex)
                {
                    PrintWarning($"Exception {ex} while attempting to generate method lists");
                    continue;
                }

                string prepareInstruction = methodPrepareInstruction.ToString();
                if (!prepareMethods.Contains(prepareInstruction))
                {
                    prepareMethods.Add(prepareInstruction);
                    methodsToPrepare.Add(prepareInstruction);
                }
            }

            if (jittraceOptions.HasFlag(jittraceoptions.sorted))
            {
                methodsToPrepare.Sort();
            }

            using (TextWriter tw = new StreamWriter(outputFileName.FullName))
            {
                foreach (string methodString in methodsToPrepare)
                {
                    tw.WriteLine(methodString);
                }
            }

            PrintMessage($"Generated {outputFileName.FullName}");
        }

        static string CsvEscape(string input, string separator)
        {
            Debug.Assert(separator.Length == 1);
            return input.Replace("\\", "\\\\").Replace(separator, "\\s");
        }

        static string GetStringForType(TypeDesc type, Dictionary<TypeDesc, string> typeStringCache)
        {
            string str;
            if (typeStringCache.TryGetValue(type, out str))
            {
                return str;
            }

            CustomAttributeTypeNameFormatter caFormat = new CustomAttributeTypeNameFormatter();
            str = caFormat.FormatName(type, true);
            typeStringCache.Add(type, str);
            return str;
        }
    }
}
