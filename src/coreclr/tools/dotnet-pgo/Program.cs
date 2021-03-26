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
using System.Reflection.PortableExecutable;
using ILCompiler.IBC;
using ILCompiler;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using System.Text.Encodings.Web;

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

            TypeDesc type = null;
            
            try
            {
                type = _idParser.ResolveTypeHandle(input, false);
            }
            catch
            {}
            if (type != null)
            {
                return new TypeSystemEntityOrUnknown(type);
            }
            // Unknown type, apply unique value, but keep the upper byte zeroed so that it can be distinguished from a token
            return new TypeSystemEntityOrUnknown(System.HashCode.Combine(input) & 0x7FFFFF | 0x800000);
        }
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

        public static void PrintWarning(string warning)
        {
            s_logger.PrintWarning(warning);
        }

        public static void PrintError(string error)
        {
            s_logger.PrintError(error);
        }

        public static void PrintMessage(string message)
        {
            s_logger.PrintMessage(message);
        }

        public static void PrintDetailedMessage(string message)
        {
            s_logger.PrintDetailedMessage(message);
        }

        public static void PrintOutput(string output)
        {
            s_logger.PrintOutput(output);
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

            if (!commandLineOptions.DetailedProgressMessages)
                s_logger.HideDetailedMessages();

            if (commandLineOptions.DumpMibc)
            {
                return InnerDumpMain(commandLineOptions);
            }
            if (commandLineOptions.InputFilesToMerge != null)
            {
                return InnerMergeMain(commandLineOptions);
            }
            else
            {
                return InnerProcessTraceFileMain(commandLineOptions);
            }
        }

        static int InnerDumpMain(CommandLineOptions commandLineOptions)
        {
            if ((commandLineOptions.InputFileToDump == null) || (!commandLineOptions.InputFileToDump.Exists))
            {
                PrintUsage(commandLineOptions, "Valid input file must be specified");
                return -8;
            }

            if (commandLineOptions.OutputFileName == null)
            {
                PrintUsage(commandLineOptions, "Output filename must be specified");
                return -8;
            }

            PrintDetailedMessage($"Opening {commandLineOptions.InputFileToDump}");
            var mibcPeReader = MIbcProfileParser.OpenMibcAsPEReader(commandLineOptions.InputFileToDump.FullName);
            var tsc = new TypeRefTypeSystem.TypeRefTypeSystemContext(new PEReader[] { mibcPeReader });

            PrintDetailedMessage($"Parsing {commandLineOptions.InputFileToDump}");
            var profileData = MIbcProfileParser.ParseMIbcFile(tsc, mibcPeReader, null, onlyDefinedInAssembly: null);

            using (FileStream outputFile = new FileStream(commandLineOptions.OutputFileName.FullName, FileMode.Create, FileAccess.Write))
            {
                JsonWriterOptions options = new JsonWriterOptions();
                options.Indented = true;
                options.SkipValidation = false;
                options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

                using Utf8JsonWriter jsonWriter = new Utf8JsonWriter(outputFile, options);
                jsonWriter.WriteStartObject();
                jsonWriter.WriteStartArray("Methods");
                foreach (MethodProfileData data in profileData.GetAllMethodProfileData())
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WriteString("Method", data.Method.ToString());
                    if (data.CallWeights != null)
                    {
                        jsonWriter.WriteStartArray("CallWeights");
                        foreach (var callWeight in data.CallWeights)
                        {
                            jsonWriter.WriteString("Method", callWeight.Key.ToString());
                            jsonWriter.WriteNumber("Weight", callWeight.Value);
                        }
                        jsonWriter.WriteEndArray();
                    }
                    if (data.ExclusiveWeight != 0)
                    {
                        jsonWriter.WriteNumber("ExclusiveWeight", data.ExclusiveWeight);
                    }
                    if (data.SchemaData != null)
                    {
                        jsonWriter.WriteStartArray("InstrumentationData");
                        foreach (var schemaElem in data.SchemaData)
                        {
                            jsonWriter.WriteStartObject();
                            jsonWriter.WriteNumber("ILOffset", schemaElem.ILOffset);
                            jsonWriter.WriteString("InstrumentationKind", schemaElem.InstrumentationKind.ToString());
                            jsonWriter.WriteNumber("Other", schemaElem.Other);
                            if (schemaElem.DataHeldInDataLong)
                            {
                                jsonWriter.WriteNumber("Data", schemaElem.DataLong);
                            }
                            else
                            {
                                if (schemaElem.DataObject == null)
                                {
                                    // No data associated with this item
                                }
                                else if (schemaElem.DataObject.Length == 1)
                                {
                                    jsonWriter.WriteString("Data", schemaElem.DataObject.GetValue(0).ToString());
                                }
                                else
                                {
                                    jsonWriter.WriteStartArray("Data");
                                    foreach (var dataElem in schemaElem.DataObject)
                                    {
                                        jsonWriter.WriteStringValue(dataElem.ToString());
                                    }
                                    jsonWriter.WriteEndArray();
                                }
                            }
                            jsonWriter.WriteEndObject();
                        }
                        jsonWriter.WriteEndArray();
                    }

                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndArray();
                jsonWriter.WriteEndObject();
            }
            PrintMessage($"Generated {commandLineOptions.OutputFileName}");

            return 0;
        }


        static int InnerMergeMain(CommandLineOptions commandLineOptions)
        {
            if (commandLineOptions.InputFilesToMerge.Count == 0)
            {
                PrintUsage(commandLineOptions, "--input must be specified");
                return -8;
            }

            if (commandLineOptions.OutputFileName == null)
            {
                PrintUsage(commandLineOptions, "--output must be specified");
                return -8;
            }

            PEReader[] mibcReaders = new PEReader[commandLineOptions.InputFilesToMerge.Count];
            for (int i = 0; i < mibcReaders.Length; i++)
            {
                PrintMessage($"Opening {commandLineOptions.InputFilesToMerge[i].FullName}");
                mibcReaders[i] = MIbcProfileParser.OpenMibcAsPEReader(commandLineOptions.InputFilesToMerge[i].FullName);
            }

            HashSet<string> assemblyNamesInBubble = null;
            if (commandLineOptions.IncludedAssemblies.Count > 0)
            {
                assemblyNamesInBubble = new HashSet<string>();
                foreach (var asmName in commandLineOptions.IncludedAssemblies)
                {
                    assemblyNamesInBubble.Add(asmName.Name);
                }
            }

            try
            {
                var tsc = new TypeRefTypeSystem.TypeRefTypeSystemContext(mibcReaders);

                bool partialNgen = false;
                Dictionary<MethodDesc, MethodProfileData> mergedProfileData = new Dictionary<MethodDesc, MethodProfileData>();
                for (int i = 0; i < mibcReaders.Length; i++)
                {
                    var peReader = mibcReaders[i];
                    PrintDetailedMessage($"Merging {commandLineOptions.InputFilesToMerge[i].FullName}");
                    ProfileData.MergeProfileData(ref partialNgen, mergedProfileData, MIbcProfileParser.ParseMIbcFile(tsc, peReader, assemblyNamesInBubble, onlyDefinedInAssembly: null));
                }

                return MibcEmitter.GenerateMibcFile(tsc, commandLineOptions.OutputFileName, mergedProfileData.Values, commandLineOptions.ValidateOutputFile, commandLineOptions.Uncompressed);
            }
            finally
            {
                foreach (var peReader in mibcReaders)
                {
                    peReader.Dispose();
                }
            }
        }

        static int InnerProcessTraceFileMain(CommandLineOptions commandLineOptions)
        { 
            if (commandLineOptions.TraceFile == null)
            {
                PrintUsage(commandLineOptions, "--trace must be specified");
                return -8;
            }

            if (commandLineOptions.OutputFileName == null)
            {
                PrintUsage(commandLineOptions, "--output must be specified");
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
                            if ((e.MethodNamespace == "dynamicClass") || !commandLineOptions.Warnings)
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
                {
                    ILCompiler.MethodProfileData[] methodProfileData = new ILCompiler.MethodProfileData[methodsUsedInProcess.Count];
                    for (int i = 0; i < methodProfileData.Length; i++)
                    {
                        ProcessedMethodData processedData = methodsUsedInProcess[i];
                        methodProfileData[i] = new ILCompiler.MethodProfileData(processedData.Method, ILCompiler.MethodProfilingDataFlags.ReadMethodCode, processedData.ExclusiveWeight, processedData.WeightedCallData, 0xFFFFFFFF, processedData.InstrumentationData);
                    }
                    return MibcEmitter.GenerateMibcFile(tsc, commandLineOptions.OutputFileName, methodProfileData, commandLineOptions.ValidateOutputFile, commandLineOptions.Uncompressed);
                }
            }
            return 0;
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
