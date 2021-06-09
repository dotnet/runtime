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
            { }
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
        static int Main(string[] args)
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
            if (commandLineOptions.CompareMibc != null)
            {
                return InnerCompareMibcMain(commandLineOptions);
            }

            return InnerProcessTraceFileMain(commandLineOptions);
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
            PrintMibcStats(profileData);

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

        static int InnerCompareMibcMain(CommandLineOptions options)
        {
            // Command line parser should require exactly 2 files
            Trace.Assert(options.CompareMibc?.Count == 2);
            FileInfo file1 = options.CompareMibc[0];
            FileInfo file2 = options.CompareMibc[1];

            PEReader mibc1 = MIbcProfileParser.OpenMibcAsPEReader(file1.FullName);
            PEReader mibc2 = MIbcProfileParser.OpenMibcAsPEReader(file2.FullName);
            var tsc = new TypeRefTypeSystem.TypeRefTypeSystemContext(new PEReader[] { mibc1, mibc2 });

            ProfileData profile1 = MIbcProfileParser.ParseMIbcFile(tsc, mibc1, null, onlyDefinedInAssembly: null);
            ProfileData profile2 = MIbcProfileParser.ParseMIbcFile(tsc, mibc2, null, onlyDefinedInAssembly: null);
            PrintOutput($"Comparing {file1.Name} to {file2.Name}");
            PrintOutput($"Statistics for {file1.Name}");
            PrintStats(profile1, file1.Name, profile2, file2.Name);
            PrintOutput("");
            PrintOutput($"Statistics for {file2.Name}");
            PrintStats(profile2, file2.Name, profile1, file1.Name);

            static void PrintStats(ProfileData self, string selfName, ProfileData other, string otherName)
            {
                var methods = self.GetAllMethodProfileData().ToList();
                var profiledMethods = methods.Where(spd => spd.SchemaData != null).ToList();
                var otherMethods = other.GetAllMethodProfileData().ToList();
                var otherProfiledMethods = otherMethods.Where(spd => spd.SchemaData != null).ToList();
                PrintMibcStats(self);
                PrintOutput($"# Profiled methods in {selfName} not in {otherName}: {profiledMethods.Select(m => m.Method).Except(otherProfiledMethods.Select(m => m.Method)).Count()}");
            }

            PrintOutput("");
            PrintOutput("Comparison");
            var methods1 = profile1.GetAllMethodProfileData().ToList();
            var methods2 = profile2.GetAllMethodProfileData().ToList();
            var profiledMethods1 = methods1.Where(m => m.SchemaData != null).ToList();
            var profiledMethods2 = methods2.Where(m => m.SchemaData != null).ToList();

            PrintOutput($"# Methods with profile data in both .mibc files: {profiledMethods1.Select(m => m.Method).Intersect(profiledMethods2.Select(m => m.Method)).Count()}");
            var fgMatches = new List<(MethodProfileData prof1, MethodProfileData prof2)>();
            var fgMismatches = new List<(MethodProfileData prof1, MethodProfileData prof2, List<string> mismatches)>();

            foreach (MethodProfileData prof1 in profiledMethods1)
            {
                MethodProfileData prof2 = profile2.GetMethodProfileData(prof1.Method);
                if (prof2?.SchemaData == null)
                    continue;

                Dictionary<int, PgoSchemaElem> GroupBlocks(MethodProfileData data)
                    => data.SchemaData
                       .Where(e => e.InstrumentationKind == PgoInstrumentationKind.BasicBlockIntCount || e.InstrumentationKind == PgoInstrumentationKind.BasicBlockLongCount)
                       .ToDictionary(e => e.ILOffset);

                Dictionary<(int, int), PgoSchemaElem> GroupEdges(MethodProfileData data)
                    => data.SchemaData
                       .Where(e => e.InstrumentationKind == PgoInstrumentationKind.EdgeIntCount || e.InstrumentationKind == PgoInstrumentationKind.EdgeLongCount)
                       .ToDictionary(e => (e.ILOffset, e.Other));

                var (blocks1, blocks2) = (GroupBlocks(prof1), GroupBlocks(prof2));
                var (edges1, edges2) = (GroupEdges(prof1), GroupEdges(prof2));

                List<string> mismatches = new List<string>();
                if (blocks1.Count > 0 && blocks2.Count > 0)
                {
                    var in1 = blocks1.Keys.Where(k => !blocks2.ContainsKey(k)).ToList();
                    var in2 = blocks2.Keys.Where(k => !blocks1.ContainsKey(k)).ToList();

                    foreach (var m1 in in1)
                        mismatches.Add($"{file1.Name} has a block at {m1:x} not present in {file2.Name}");
                    foreach (var m2 in in2)
                        mismatches.Add($"{file2.Name} has a block at {m2:x} not present in {file1.Name}");
                }

                if (edges1.Count > 0 && edges2.Count > 0)
                {
                    var in1 = edges1.Keys.Where(k => !edges2.ContainsKey(k)).ToList();
                    var in2 = edges2.Keys.Where(k => !edges1.ContainsKey(k)).ToList();

                    foreach (var (from, to) in in1)
                        mismatches.Add($"{file1.Name} has an edge {from:x}->{to:x} not present in {file2.Name}");
                    foreach (var (from, to) in in2)
                        mismatches.Add($"{file2.Name} has an edge {from:x}->{to:x} not present in {file1.Name}");
                }

                if (mismatches.Count > 0)
                    fgMismatches.Add((prof1, prof2, mismatches));
                else
                    fgMatches.Add((prof1, prof2));
            }

            PrintOutput($"  Of these, {fgMatches.Count} have matching flow-graphs and the remaining {fgMismatches.Count} do not");

            if (fgMismatches.Count > 0)
            {
                PrintOutput("");
                PrintOutput("Methods with mismatched flow-graphs:");
                foreach ((MethodProfileData prof1, MethodProfileData prof2, List<string> mismatches) in fgMismatches)
                {
                    PrintOutput($"{prof1.Method}");
                    foreach (string s in mismatches)
                        PrintOutput($"  {s}");
                }
            }

            if (fgMatches.Count > 0)
            {
                PrintOutput("");
                PrintOutput($"When comparing the flow-graphs of the matching methods, their overlaps break down as follows:");

                List<double> blockOverlaps = new List<double>();
                List<double> edgeOverlaps = new List<double>();

                foreach ((MethodProfileData prof1, MethodProfileData prof2) in fgMatches)
                {
                    var (blocks1, blocks2) = (GroupBlocks(prof1), GroupBlocks(prof2));
                    var (edges1, edges2) = (GroupEdges(prof1), GroupEdges(prof2));

                    double Overlap<TKey>(Dictionary<TKey, PgoSchemaElem> left, Dictionary<TKey, PgoSchemaElem> right)
                    {
                        long leftTotal = left.Values.Sum(e => e.DataLong);
                        long rightTotal = right.Values.Sum(e => e.DataLong);
                        Debug.Assert(left.Keys.All(k => right.ContainsKey(k)));
                        Debug.Assert(right.Keys.All(k => left.ContainsKey(k)));

                        var leftPW = left.ToDictionary(k => k.Key, k => k.Value.DataLong / (double)leftTotal);
                        var rightPW = right.ToDictionary(k => k.Key, k => k.Value.DataLong / (double)rightTotal);

                        double overlap = leftPW.Sum(k => Math.Min(k.Value, rightPW[k.Key]));
                        return overlap;
                    }

                    if (blocks1.Count > 0 && blocks2.Count > 0)
                        blockOverlaps.Add(Overlap(blocks1, blocks2));

                    if (edges1.Count > 0 && edges2.Count > 0)
                        edgeOverlaps.Add(Overlap(edges1, edges2));
                }

                void PrintHistogram(List<double> overlaps)
                {
                    int maxWidth = Console.WindowWidth - 10;
                    const int maxLabelWidth = 4; // to print "100%".
                    int barMaxWidth = maxWidth - (maxLabelWidth + 10); // Leave 10 chars for writing other things on the line
                    const int bucketSize = 5;
                    int width = Console.WindowWidth - 10;
                    var sorted = overlaps.OrderByDescending(d => d).ToList();

                    void PrintBar(string label, ref int curIndex, Func<double, bool> include, bool forcePrint)
                    {
                        int count = 0;
                        while (curIndex < sorted.Count && include(sorted[curIndex]))
                        {
                            count++;
                            curIndex++;
                        }

                        if (count == 0 && !forcePrint)
                            return;

                        double proportion = count / (double)sorted.Count;

                        int numFullBlocks = (int)(proportion * barMaxWidth);
                        double fractionalPart = proportion * barMaxWidth - numFullBlocks;

                        const char fullBlock = '\u2588';
                        string bar = new string(fullBlock, numFullBlocks);
                        if ((int)(fractionalPart * 8) != 0)
                        {
                            // After full block comes a 7/8 block, then 6/8, then 5/8 etc.
                            bar += (char)(fullBlock + (8 - (int)(fractionalPart * 8)));
                        }

                        // If empty, use the left one-eight block to show a line of where 0 is.
                        if (bar == "")
                            bar = "\u258f";

                        string line = FormattableString.Invariant($"{label,-maxLabelWidth} {bar} ({proportion*100:F1}%)");
                        PrintOutput(line);
                    }

                    // If there are any at 100%, then print those separately
                    int curIndex = 0;
                    PrintBar("100%", ref curIndex, d => d >= (1 - 0.000000001), false);
                    for (int proportion = 100 - bucketSize; proportion >= 0; proportion -= bucketSize)
                        PrintBar($">{(int)proportion,2}%", ref curIndex, d => d * 100 > proportion, true);
                    PrintBar("0%", ref curIndex, d => true, false);
                }

                // Need UTF8 for the block chars.
                Console.OutputEncoding = Encoding.UTF8;
                if (blockOverlaps.Count > 0)
                {
                    PrintHistogram(blockOverlaps);
                    PrintOutput("(using block counts)");
                }

                if (edgeOverlaps.Count > 0)
                {
                    if (blockOverlaps.Count > 0)
                        PrintOutput("");

                    PrintHistogram(edgeOverlaps);
                    PrintOutput("(using edge counts)");
                }
            }

            return 0;
        }

        static void PrintMibcStats(ProfileData data)
        {
            List<MethodProfileData> methods = data.GetAllMethodProfileData().ToList();
            List<MethodProfileData> profiledMethods = methods.Where(spd => spd.SchemaData != null).ToList();
            PrintOutput($"# Methods: {methods.Count}");
            PrintOutput($"# Methods with any profile data: {profiledMethods.Count(spd => spd.SchemaData.Length > 0)}");
            PrintOutput($"# Methods with 32-bit block counts: {profiledMethods.Count(spd => spd.SchemaData.Any(elem => elem.InstrumentationKind == PgoInstrumentationKind.BasicBlockIntCount))}");
            PrintOutput($"# Methods with 64-bit block counts: {profiledMethods.Count(spd => spd.SchemaData.Any(elem => elem.InstrumentationKind == PgoInstrumentationKind.BasicBlockLongCount))}");
            PrintOutput($"# Methods with 32-bit edge counts: {profiledMethods.Count(spd => spd.SchemaData.Any(elem => elem.InstrumentationKind == PgoInstrumentationKind.EdgeIntCount))}");
            PrintOutput($"# Methods with 64-bit edge counts: {profiledMethods.Count(spd => spd.SchemaData.Any(elem => elem.InstrumentationKind == PgoInstrumentationKind.EdgeLongCount))}");
            PrintOutput($"# Methods with type handle histograms: {profiledMethods.Count(spd => spd.SchemaData.Any(elem => elem.InstrumentationKind == PgoInstrumentationKind.TypeHandleHistogramTypeHandle))}");
            PrintOutput($"# Methods with GetLikelyClass data: {profiledMethods.Count(spd => spd.SchemaData.Any(elem => elem.InstrumentationKind == PgoInstrumentationKind.GetLikelyClass))}");
        }

        private static Dictionary<int, PgoSchemaElem> GroupBlocks(MethodProfileData data)
        {
            return data.SchemaData
               .Where(e => e.InstrumentationKind == PgoInstrumentationKind.BasicBlockIntCount || e.InstrumentationKind == PgoInstrumentationKind.BasicBlockLongCount)
               .ToDictionary(e => e.ILOffset);
        }

        private static Dictionary<(int, int), PgoSchemaElem> GroupEdges(MethodProfileData data)
        {
            return data.SchemaData
               .Where(e => e.InstrumentationKind == PgoInstrumentationKind.EdgeIntCount || e.InstrumentationKind == PgoInstrumentationKind.EdgeLongCount)
               .ToDictionary(e => (e.ILOffset, e.Other));
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

            string etlFileName = commandLineOptions.TraceFile.FullName;
            foreach (string nettraceExtension in new string[] { ".netperf", ".netperf.zip", ".nettrace" })
            {
                if (commandLineOptions.TraceFile.FullName.EndsWith(nettraceExtension))
                {
                    etlFileName = Path.ChangeExtension(commandLineOptions.TraceFile.FullName, ".etlx");
                    PrintMessage($"Creating ETLX file {etlFileName} from {commandLineOptions.TraceFile.FullName}");
                    TraceLog.CreateFromEventPipeDataFile(commandLineOptions.TraceFile.FullName, etlFileName);
                }
            }

            string lttngExtension = ".trace.zip";
            if (commandLineOptions.TraceFile.FullName.EndsWith(lttngExtension))
            {
                etlFileName = Path.ChangeExtension(commandLineOptions.TraceFile.FullName, ".etlx");
                PrintMessage($"Creating ETLX file {etlFileName} from {commandLineOptions.TraceFile.FullName}");
                TraceLog.CreateFromLttngTextDataFile(commandLineOptions.TraceFile.FullName, etlFileName);
            }

            UnZipIfNecessary(ref etlFileName, commandLineOptions.BasicProgressMessages ? Console.Out : new StringWriter());

            // For SPGO we need to be able to map raw IPs back to IL offsets in methods.
            // Normally TraceEvent facilitates this remapping automatically and discards the IL<->IP mapping table events.
            // However, we have found TraceEvent's remapping to be imprecise (see https://github.com/microsoft/perfview/issues/1410).
            // Thus, when SPGO is requested, we need to keep these events.
            // Note that we always request these events to be kept because if one switches back and forth between SPGO and non-SPGO,
            // the cached .etlx file will not update.
            using (var traceLog = TraceLog.OpenOrConvert(etlFileName, new TraceLogOptions { KeepAllEvents = true }))
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

                MethodMemoryMap methodMemMap = null;
                MethodMemoryMap GetMethodMemMap()
                {
                    if (methodMemMap == null)
                    {
                        methodMemMap = new MethodMemoryMap(
                            p,
                            tsc,
                            idParser,
                            clrInstanceId.Value);
                    }

                    return methodMemMap;
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

                    MethodMemoryMap mmap = GetMethodMemMap();
                    foreach (var e in p.EventsInProcess.ByEventType<SampledProfileTraceData>())
                    {
                        var callstack = e.CallStack();
                        if (callstack == null)
                            continue;

                        ulong address1 = callstack.CodeAddress.Address;
                        MethodDesc topOfStackMethod = mmap.GetMethod(address1);
                        MethodDesc nextMethod = null;
                        if (callstack.Caller != null)
                        {
                            ulong address2 = callstack.Caller.CodeAddress.Address;
                            nextMethod = mmap.GetMethod(address2);
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

                Dictionary<MethodDesc, SampleProfile> sampleProfiles = new Dictionary<MethodDesc, SampleProfile>();
                if (commandLineOptions.Spgo)
                {
                    MethodMemoryMap mmap = GetMethodMemMap();
                    Dictionary<MethodDesc, MethodIL> ils = new Dictionary<MethodDesc, MethodIL>();
                    Dictionary<MethodDesc, FlowGraph> flowGraphs = new Dictionary<MethodDesc, FlowGraph>();

                    MethodIL GetMethodIL(MethodDesc desc)
                    {
                        if (!ils.TryGetValue(desc, out MethodIL il))
                        {
                            il = desc switch
                            {
                                EcmaMethod em => EcmaMethodIL.Create(em),
                                var m => new InstantiatedMethodIL(m, EcmaMethodIL.Create((EcmaMethod)m.GetTypicalMethodDefinition())),
                            };

                            ils.Add(desc, il);
                        }

                        return il;
                    }

                    FlowGraph GetFlowGraph(MethodDesc desc)
                    {
                        if (!flowGraphs.TryGetValue(desc, out FlowGraph fg))
                        {
                            flowGraphs.Add(desc, fg = FlowGraph.Create(GetMethodIL(desc)));
                        }

                        return fg;
                    }

                    Guid lbrGuid = Guid.Parse("99134383-5248-43fc-834b-529454e75df3");
                    bool hasLbr = traceLog.Events.Any(e => e.TaskGuid == lbrGuid);

                    if (!hasLbr)
                    {
                        // No LBR data, use standard IP samples. First convert each sample to a tuple of (Method, raw IP, IL offset).
                        (MethodDesc Method, ulong IP, int Offset) GetTuple(SampledProfileTraceData e)
                        {
                            MemoryRegionInfo info = mmap.GetInfo(e.InstructionPointer);
                            if (info == null)
                                return (null, e.InstructionPointer, -1);

                            int offset = info.NativeToILMap?.Lookup(checked((uint)(e.InstructionPointer - info.StartAddress))) ?? -1;
                            return (info.Method, e.InstructionPointer, offset);
                        }

                        var samples =
                            p.EventsInProcess.ByEventType<SampledProfileTraceData>()
                            .Select(GetTuple)
                            .Where(t => t.Method != null && t.Offset >= 0)
                            .ToList();

                        // Now find all samples in each method.
                        foreach (var g in samples.GroupBy(t => t.Method))
                        {
                            // SPGO is quite sensitive with low counts, so check if we should not generate SPGO data for this function.
                            if (g.Count() < commandLineOptions.SpgoMinSamples)
                                continue;

                            MethodIL il = GetMethodIL(g.Key);
                            SampleProfile sp = SampleProfile.Create(il, GetFlowGraph(g.Key), g.Select(t => t.Offset));
                            sampleProfiles.Add(g.Key, sp);
                        }
                    }
                    else
                    {
                        // We have LBR data. We use the LBR data to collect straight-line runs that the CPU did in this process inside managed methods.
                        // That is, if we first see a branch from A -> B followed by a branch from C -> D, then we can conclude that the CPU executed
                        // code from B -> C. We call this a 'run' and collect each run and its multiplicity.
                        // Later, we will find all IL offsets on this path and assign samples to the distinct basic blocks corresponding to those IL offsets.
                        Dictionary<(ulong startRun, ulong endRun), long> runs = new Dictionary<(ulong startRun, ulong endRun), long>();
                        List<(ulong start, ulong end)> lbrRuns = new List<(ulong start, ulong end)>();
                        LbrEntry64[] lbr64Arr = null;
                        foreach (var e in traceLog.Events)
                        {
                            if (e.TaskGuid != lbrGuid)
                                continue;

                            // Opcode is always 32 for the LBR event.
                            if (e.Opcode != (TraceEventOpcode)32)
                                continue;

                            unsafe
                            {
                                Span<LbrEntry64> lbr;
                                if (traceLog.PointerSize == 4)
                                {
                                    // For 32-bit machines we convert the data into a 64-bit format first.
                                    LbrTraceEventData32* data = (LbrTraceEventData32*)e.DataStart;
                                    if (data->ProcessId != p.ProcessID)
                                        continue;

                                    Span<LbrEntry32> lbr32 = data->Entries(e.EventDataLength);
                                    if (lbr64Arr == null || lbr64Arr.Length < lbr32.Length)
                                        lbr64Arr = new LbrEntry64[lbr32.Length];

                                    for (int i = 0; i < lbr32.Length; i++)
                                    {
                                        ref LbrEntry64 entry = ref lbr64Arr[i];
                                        entry.FromAddress = lbr32[i].FromAddress;
                                        entry.ToAddress = lbr32[i].ToAddress;
                                        entry.Reserved = lbr32[i].Reserved;
                                    }

                                    lbr = lbr64Arr[0..lbr32.Length];
                                }
                                else
                                {
                                    Trace.Assert(traceLog.PointerSize == 8, $"Unexpected PointerSize {traceLog.PointerSize}");

                                    LbrTraceEventData64* data = (LbrTraceEventData64*)e.DataStart;
                                    // TODO: The process ID check is not sufficient as PIDs can be reused, so we need to use timestamps too,
                                    // but we do not have access to PerfView functions to convert it. Hopefully TraceEvent will handle this
                                    // for us in the future.
                                    if (data->ProcessId != p.ProcessID)
                                        continue;

                                    lbr = data->Entries(e.EventDataLength);
                                }

                                // Store runs. LBR is chronological with most recent branches first.
                                // To avoid double-counting blocks containing calls when the LBR buffer contains
                                // both the call and the return from the call, we have to do some fancy things
                                // when seeing cross-function branches, so we use a temporary list of runs
                                // that we assign into the global dictionary.
                                lbrRuns.Clear();
                                for (int i = lbr.Length - 2; i >= 0; i--)
                                {
                                    ulong prevFrom = lbr[i + 1].FromAddress;
                                    ulong prevTo = lbr[i + 1].ToAddress;
                                    ulong curFrom = lbr[i].FromAddress;
                                    MemoryRegionInfo prevFromMeth = methodMemMap.GetInfo(prevFrom);
                                    MemoryRegionInfo prevToMeth = methodMemMap.GetInfo(prevTo);
                                    MemoryRegionInfo curFromMeth = methodMemMap.GetInfo(curFrom);
                                    // If this run is not in the same function then ignore it.
                                    if (prevToMeth == null || prevToMeth != curFromMeth)
                                        continue;

                                    // Otherwise, if this run follows right after jumping back into the function, we might need to extend
                                    // a previous run instead. This happens if we previously did a call out of this function and now returned back.
                                    // TODO: Handle recursion here. The same function could return to itself and we wouldn't realize it from this check.
                                    if (prevFromMeth != prevToMeth)
                                    {
                                        bool extendedPrevRun = false;
                                        // Try to find a previous run. Iterate in reverse to simulate stack behavior of calls.
                                        FlowGraph toFG = null;
                                        for (int j = lbrRuns.Count - 1; j >= 0; j--)
                                        {
                                            MemoryRegionInfo endRunMeth = methodMemMap.GetInfo(lbrRuns[j].end);
                                            if (endRunMeth != prevToMeth)
                                                continue;

                                            // Same function at least, check for same basic block
                                            toFG ??= GetFlowGraph(endRunMeth.Method);
                                            BasicBlock endRunBB = toFG.Lookup(endRunMeth.NativeToILMap.Lookup((uint)(lbrRuns[j].end - endRunMeth.StartAddress)));
                                            BasicBlock toBB = toFG.Lookup(endRunMeth.NativeToILMap.Lookup((uint)(prevTo - endRunMeth.StartAddress)));
                                            if (endRunBB == toBB && prevTo > lbrRuns[j].end)
                                            {
                                                // Same BB and the jump is to after where the previous run ends. Take that as a return to after that call and extend the previous run.
                                                lbrRuns[j] = (lbrRuns[j].start, curFrom);
                                                extendedPrevRun = true;
                                                break;
                                            }
                                        }

                                        if (extendedPrevRun)
                                            continue;
                                    }

                                    lbrRuns.Add((prevTo, curFrom));
                                }

                                // Now insert runs.
                                foreach (var pair in lbrRuns)
                                {
                                    if (runs.TryGetValue(pair, out long count))
                                        runs[pair] = count + 1;
                                    else
                                        runs.Add(pair, 1);
                                }
                            }
                        }

                        // Group runs by memory region info, which corresponds to each .NET method.
                        var groupedRuns =
                            runs
                            .Select(r => (start: r.Key.startRun, end: r.Key.endRun, count: r.Value, info: methodMemMap.GetInfo(r.Key.startRun)))
                            .GroupBy(t => t.info);

                        foreach (var g in groupedRuns)
                        {
                            if (g.Key == null || g.Key.NativeToILMap == null)
                                continue;

                            // Collect relative IPs of samples. Note that we cannot translate the end-points of runs from IPs to IL offsets
                            // as we cannot assume that a straight-line execution between two IPs corresponds to a straight-line execution between
                            // two IL offsets. SampleProfile.CreateFromLbr will be responsible for assigning samples based on the flow graph relative IPs,
                            // the IP<->IL mapping and the flow graph.
                            List<(uint start, uint end, long count)> samples =
                                g
                                .Where(t => t.end >= t.start && t.end < g.Key.EndAddress)
                                .Select(t => ((uint)(t.start - g.Key.StartAddress), (uint)(t.end - g.Key.StartAddress), t.count))
                                .ToList();

                            if (samples.Sum(t => t.count) < commandLineOptions.SpgoMinSamples)
                                continue;

                            SampleProfile ep = SampleProfile.CreateFromLbr(
                                GetMethodIL(g.Key.Method),
                                GetFlowGraph(g.Key.Method),
                                g.Key.NativeToILMap,
                                samples);

                            sampleProfiles.Add(g.Key.Method, ep);
                        }
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
                        else if (sampleProfiles.TryGetValue(methodData.Method, out SampleProfile sp))
                        {
                            IEnumerable<PgoSchemaElem> schema = Enumerable.Empty<PgoSchemaElem>();

                            if (commandLineOptions.SpgoIncludeBlockCounts)
                            {
                                schema = schema.Concat(
                                    sp.SmoothedSamples
                                    .Select(kvp =>
                                        new PgoSchemaElem
                                        {
                                            InstrumentationKind = kvp.Value > uint.MaxValue ? PgoInstrumentationKind.BasicBlockLongCount : PgoInstrumentationKind.BasicBlockIntCount,
                                            ILOffset = kvp.Key.Start,
                                            Count = 1,
                                            DataLong = kvp.Value,
                                        }));
                            }

                            if (commandLineOptions.SpgoIncludeEdgeCounts)
                            {
                                schema = schema.Concat(
                                    sp.SmoothedEdgeSamples
                                    .Select(kvp =>
                                        new PgoSchemaElem
                                        {
                                            InstrumentationKind = kvp.Value > uint.MaxValue ? PgoInstrumentationKind.EdgeLongCount : PgoInstrumentationKind.EdgeIntCount,
                                            ILOffset = kvp.Key.Item1.Start,
                                            Other = kvp.Key.Item2.Start,
                                            Count = 1,
                                            DataLong = kvp.Value
                                        }));
                            }

                            methodData.InstrumentationData = schema.ToArray();
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
