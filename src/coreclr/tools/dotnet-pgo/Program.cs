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
    public enum JitTraceOptions
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

    class PgoDataLoader : IPgoSchemaDataLoader<TypeSystemEntityOrUnknown, TypeSystemEntityOrUnknown>
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

        public TypeSystemEntityOrUnknown MethodFromLong(long input)
        {
            if (input == 0)
                return new TypeSystemEntityOrUnknown(0);

            MethodDesc method = null;

            try
            {
                method = _idParser.ResolveMethodID(input, false);
            }
            catch
            { }
            if (method != null)
            {
                return new TypeSystemEntityOrUnknown(method);
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

                int result = MibcEmitter.GenerateMibcFile(tsc, commandLineOptions.OutputFileName, mergedProfileData.Values, commandLineOptions.ValidateOutputFile, commandLineOptions.Uncompressed);
                if (result == 0 && commandLineOptions.InheritTimestamp)
                {
                    commandLineOptions.OutputFileName.CreationTimeUtc = commandLineOptions.InputFilesToMerge.Max(fi => fi.CreationTimeUtc);
                    commandLineOptions.OutputFileName.LastWriteTimeUtc = commandLineOptions.InputFilesToMerge.Max(fi => fi.LastWriteTimeUtc);
                }

                return result;
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

            // Look for the shortest unique names for the input files.
            string name1 = file1.Name;
            string name2 = file2.Name;
            string path1 = Path.GetDirectoryName(file1.FullName);
            string path2 = Path.GetDirectoryName(file2.FullName);
            while (name1 == name2)
            {
                name1 = Path.Combine(Path.GetFileName(path1), name1);
                name2 = Path.Combine(Path.GetFileName(path2), name2);
                path1 = Path.GetDirectoryName(path1);
                path2 = Path.GetDirectoryName(path2);
            }

            PEReader mibc1 = MIbcProfileParser.OpenMibcAsPEReader(file1.FullName);
            PEReader mibc2 = MIbcProfileParser.OpenMibcAsPEReader(file2.FullName);
            var tsc = new TypeRefTypeSystem.TypeRefTypeSystemContext(new PEReader[] { mibc1, mibc2 });

            ProfileData profile1 = MIbcProfileParser.ParseMIbcFile(tsc, mibc1, null, onlyDefinedInAssembly: null);
            ProfileData profile2 = MIbcProfileParser.ParseMIbcFile(tsc, mibc2, null, onlyDefinedInAssembly: null);
            PrintOutput($"Comparing {name1} to {name2}");
            PrintOutput($"Statistics for {name1}");
            PrintMibcStats(profile1);
            PrintOutput("");
            PrintOutput($"Statistics for {name2}");
            PrintMibcStats(profile2);

            PrintOutput("");
            PrintOutput("Comparison");
            var methods1 = profile1.GetAllMethodProfileData().ToList();
            var methods2 = profile2.GetAllMethodProfileData().ToList();
            var profiledMethods1 = methods1.Where(m => m.SchemaData != null).ToList();
            var profiledMethods2 = methods2.Where(m => m.SchemaData != null).ToList();

            PrintOutput($"# Profiled methods in {name1} not in {name2}: {profiledMethods1.Select(m => m.Method).Except(profiledMethods2.Select(m => m.Method)).Count()}");
            PrintOutput($"# Profiled methods in {name2} not in {name1}: {profiledMethods2.Select(m => m.Method).Except(profiledMethods1.Select(m => m.Method)).Count()}");
            PrintOutput($"# Methods with profile data in both .mibc files: {profiledMethods1.Select(m => m.Method).Intersect(profiledMethods2.Select(m => m.Method)).Count()}");
            var fgMatches = new Dictionary<MethodDesc, PgoCompareMethodFlowGraph>();
            var fgMismatches = new List<(MethodProfileData prof1, MethodProfileData prof2, List<string> mismatches)>();

            foreach (MethodProfileData prof1 in profiledMethods1)
            {
                MethodProfileData prof2 = profile2.GetMethodProfileData(prof1.Method);
                if (prof2?.SchemaData == null)
                    continue;

                PgoCompareMethodFlowGraph graph = PgoCompareMethodFlowGraph.Create(prof1, name1, prof2, name2, out var errors);
                if (graph != null)
                {
                    fgMatches.Add(prof1.Method, graph);
                }
                else
                {
                    fgMismatches.Add((prof1, prof2, errors));
                }
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
                PrintOutput($"Comparing methods with matching flow-graphs");

                var blockOverlaps = new List<(MethodDesc Method, double Overlap)>();
                var edgeOverlaps = new List<(MethodDesc Method, double Overlap)>();

                foreach ((MethodDesc method, PgoCompareMethodFlowGraph fg) in fgMatches)
                {
                    if (fg.ProfilesHadBasicBlocks)
                        blockOverlaps.Add((method, fg.ComputeBlockOverlap()));

                    if (fg.ProfilesHadEdges)
                        edgeOverlaps.Add((method, fg.ComputeEdgeOverlap()));
                }

                void PrintHistogram(List<(MethodDesc Method, double Overlap)> overlaps)
                {
                    int maxWidth = Console.WindowWidth - 10;
                    const int maxLabelWidth = 4; // to print "100%".
                    int barMaxWidth = maxWidth - (maxLabelWidth + 10); // Leave 10 chars for writing other things on the line
                    const int bucketSize = 5;
                    int width = Console.WindowWidth - 10;
                    var sorted = overlaps.OrderByDescending(t => t.Overlap).ToList();

                    void PrintBar(string label, ref int curIndex, Func<double, bool> include, bool forcePrint)
                    {
                        int count = 0;
                        while (curIndex < sorted.Count && include(sorted[curIndex].Overlap))
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

                    PrintOutput(FormattableString.Invariant($"The average overlap is {sorted.Average(t => t.Overlap)*100:F2}% for the {sorted.Count} methods with matching flow graphs and profile data"));
                    double mse = sorted.Sum(t => (100 - t.Overlap*100) * (100 - t.Overlap*100)) / sorted.Count;
                    PrintOutput(FormattableString.Invariant($"The mean squared error is {mse:F2}"));
                    PrintOutput(FormattableString.Invariant($"There are {sorted.Count(t => t.Overlap < 0.5)}/{sorted.Count} methods with overlaps < 50%:"));
                    foreach (var badMethod in sorted.Where(t => t.Overlap < 0.5).OrderBy(t => t.Overlap))
                    {
                        PrintOutput(FormattableString.Invariant($"  {badMethod.Method} ({badMethod.Overlap * 100:F2}%)"));
                    }
                }

                // Need UTF8 for the block chars.
                Console.OutputEncoding = Encoding.UTF8;
                if (blockOverlaps.Count > 0)
                {
                    PrintOutput("The overlap of the block counts break down as follows:");
                    PrintHistogram(blockOverlaps);
                    PrintOutput("");
                }

                if (edgeOverlaps.Count > 0)
                {
                    PrintOutput("The overlap of the edge counts break down as follows:");
                    PrintHistogram(edgeOverlaps);
                    PrintOutput("");
                }

                var changes = new List<(MethodDesc method, int ilOffset, GetLikelyClassResult result1, GetLikelyClassResult result2)>();
                int devirtToSame = 0;
                int devirtToSameLikelihood100 = 0;
                int devirtToSameLikelihood70 = 0;
                foreach ((MethodDesc method, PgoCompareMethodFlowGraph fg) in fgMatches)
                {
                    MethodProfileData prof1 = profile1.GetMethodProfileData(method);
                    MethodProfileData prof2 = profile2.GetMethodProfileData(method);

                    List<int> typeHandleHistogramCallSites =
                        prof1.SchemaData.Concat(prof2.SchemaData)
                        .Where(e => e.InstrumentationKind == PgoInstrumentationKind.GetLikelyClass || e.InstrumentationKind == PgoInstrumentationKind.HandleHistogramTypes)
                        .Select(e => e.ILOffset)
                        .Distinct()
                        .ToList();

                    foreach (int callsite in typeHandleHistogramCallSites)
                    {
                        GetLikelyClassResult result1 = GetLikelyClass(prof1.SchemaData, callsite);
                        GetLikelyClassResult result2 = GetLikelyClass(prof2.SchemaData, callsite);
                        if (result1.Devirtualizes != result2.Devirtualizes || (result1.Devirtualizes && result2.Devirtualizes && result1.Type != result2.Type))
                            changes.Add((prof1.Method, callsite, result1, result2));

                        if (result1.Devirtualizes && result2.Devirtualizes && result1.Type == result2.Type)
                        {
                            devirtToSame++;
                            devirtToSameLikelihood100 += result1.Likelihood == 100 && result2.Likelihood == 100 ? 1 : 0;
                            devirtToSameLikelihood70 += result1.Likelihood >= 70 && result2.Likelihood >= 70 ? 1 : 0;
                        }
                    }
                }

                PrintOutput($"There are {changes.Count(t => t.result1.Devirtualizes && !t.result2.Devirtualizes)} sites that devirtualize with {name1} but not with {name2}");
                PrintOutput($"There are {changes.Count(t => !t.result1.Devirtualizes && t.result2.Devirtualizes)} sites that do not devirtualize with {name1} but do with {name2}");
                PrintOutput($"There are {changes.Count(t => t.result1.Devirtualizes && t.result2.Devirtualizes && t.result1.Type != t.result2.Type)} sites that change devirtualized type");
                PrintOutput($"There are {devirtToSame} sites that devirtualize to the same type before and after");
                PrintOutput($"  Of these, {devirtToSameLikelihood100} have a likelihood of 100 in both .mibc files");
                PrintOutput($"  and {devirtToSameLikelihood70} have a likelihood >= 70 in both .mibc files");

                foreach (var group in changes.GroupBy(g => g.method))
                {
                    PrintOutput($"  In {group.Key}");
                    foreach (var change in group)
                    {
                        string FormatDevirt(GetLikelyClassResult result)
                        {
                            if (result.Type != null)
                                return $"{result.Type}, likelihood {result.Likelihood}{(result.Devirtualizes ? "" : " (does not devirt)")}";

                            return $"(null)";
                        }

                        PrintOutput($"    At +{change.ilOffset:x}: {FormatDevirt(change.result1)} vs {FormatDevirt(change.result2)}");
                    }
                }

                if (options.DumpWorstOverlapGraphsTo != null)
                {
                    IEnumerable<MethodDesc> toDump;
                    if (options.DumpWorstOverlapGraphs == -1)
                    {
                        // Take all with less than 0.5 overlap in order.
                        toDump =
                            blockOverlaps
                            .Concat(edgeOverlaps)
                            .OrderBy(t => t.Overlap)
                            .TakeWhile(t => t.Overlap < 0.5)
                            .Select(t => t.Method)
                            .Distinct();
                    }
                    else
                    {
                        // Take the first N methods ordered by min(blockOverlap, edgeOverlap).
                        toDump =
                            blockOverlaps
                            .Concat(edgeOverlaps)
                            .GroupBy(t => t.Method)
                            .Select(g => (Method: g.Key, Overlap: g.Select(t => t.Overlap).Min()))
                            .OrderBy(t => t.Overlap)
                            .Select(t => t.Method)
                            .Take(options.DumpWorstOverlapGraphs);
                    }

                    foreach (MethodDesc method in toDump)
                    {
                        PgoCompareMethodFlowGraph fg = fgMatches[method];

                        string title = $"Flowgraph for {method}\\n{name1} vs {name2}";
                        if (fg.ProfilesHadBasicBlocks)
                        {
                            title += $"\\nBasic block counts: {fg.TotalBlockCount1} vs {fg.TotalEdgeCount2}";
                            title += $"\\nBasic block count overlap: {fg.ComputeBlockOverlap() * 100:F2}%";
                        }
                        if (fg.ProfilesHadEdges)
                        {
                            title += $"\\nEdge counts: {fg.TotalEdgeCount1} vs {fg.TotalEdgeCount2}";
                            title += $"\\nEdge count overlap: {fg.ComputeEdgeOverlap() * 100:F2}%";
                        }

                        string dot = fg.Dump(title);

                        string fileName = DebugNameFormatter.Instance.FormatName(method.OwningType, DebugNameFormatter.FormatOptions.NamespaceQualify) + "." + method.DiagnosticName;
                        foreach (char c in Path.GetInvalidFileNameChars())
                            fileName = fileName.Replace(c, '_');

                        File.WriteAllText(Path.Combine(options.DumpWorstOverlapGraphsTo.FullName, fileName + ".dot"), dot);
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Prints a chart "Call-sites grouped by number of likely classes seen"
        /// </summary>
        /// <param name="callSites">Each array item represents a unique call-site, and the actual value is
        /// the number of unique classes seen at that call-site</param>
        static void PrintCallsitesByLikelyClassesChart(int[] callSites)
        {
            const int maxLikelyClasses = 10;
            const int tableWidth = 20;

            if (callSites.Length < 1)
                return;

            int[] rows = new int[maxLikelyClasses + 1];
            foreach (var item in callSites
                .GroupBy(k => k > maxLikelyClasses ? maxLikelyClasses : k)
                .OrderBy(d => d.Key)
                .Select(d => new { Row = d.Key, Count = d.Count() })
                .ToArray())
            {
                rows[item.Row] = item.Count;
            }

            int sum = rows.Sum();

            Console.WriteLine();
            Console.WriteLine("Call-sites grouped by number of likely classes seen:");
            Console.WriteLine();

            bool startWithZero = rows[0] > 0;
            for (int i = startWithZero ? 0 : 1; i < rows.Length; i++)
            {
                double share = rows[i] / (double)sum;
                int shareWidth = (int)(Math.Round(share * tableWidth));
                bool lastRow = (i == rows.Length - 1);

                Console.Write($"        {(lastRow ? "≥" : " ")}{i,2}: [");
                Console.Write(new string('#', shareWidth));
                Console.Write(new string('.', tableWidth - shareWidth));
                Console.Write("] ");
                Console.Write($"{share * 100.0,4:F1}%");
                Console.Write($" ({rows[i]})");

                if (i == 0)
                    Console.Write(" - call-sites with 'null's");
                else if (i == 1)
                    Console.Write(" - monomorphic call-sites");

                Console.WriteLine();
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Prints a histogram for "likelihoods" distribution for a specific likely class (e.g. most popular one)
        /// </summary>
        /// <param name="likelihoods">Array of likelihoods 0-100.0</param>
        static void PrintLikelihoodHistogram(double[] likelihoods)
        {
            const int columns = 10;
            const int tableWidth = 20;
            int columnWidth = 100 / columns;

            if (likelihoods.Length == 0)
                return; // Avoid div-by-zero

            Console.WriteLine();
            Console.WriteLine("Likelihoods of the most popular likely classes:");
            Console.WriteLine();

            likelihoods = likelihoods.OrderBy(i => i).ToArray();
            for (int i = 0; i < columns; i++)
            {
                int lowerLimit = i * columnWidth;
                int upperLimit = i * columnWidth + columnWidth;
                int count = likelihoods.Count(l =>
                {
                    if (i == 0) // inclusive for [0..
                        return l <= upperLimit;
                    return l > lowerLimit && l <= upperLimit;
                });

                int shareWidth = (int)Math.Round((double)count / likelihoods.Length * tableWidth);
                Console.Write(i == 0 ? "  [" : "  ("); // inclusive for [0..
                Console.Write($"{i * columnWidth,2}-{i * columnWidth + columnWidth,3}%]: [");
                Console.Write(new string('#', shareWidth));
                Console.Write(new string('.', tableWidth - shareWidth));
                Console.Write($"] {count}");
                Console.WriteLine();
            }
            Console.WriteLine();
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
            int numTypeHandleHistograms = profiledMethods.Sum(spd => spd.SchemaData.Count(elem => elem.InstrumentationKind == PgoInstrumentationKind.HandleHistogramTypes));
            int methodsWithTypeHandleHistograms = profiledMethods.Count(spd => spd.SchemaData.Any(elem => elem.InstrumentationKind == PgoInstrumentationKind.HandleHistogramTypes));
            PrintOutput($"# Type handle histograms: {numTypeHandleHistograms} in {methodsWithTypeHandleHistograms} methods");
            int numGetLikelyClass = profiledMethods.Sum(spd => spd.SchemaData.Count(elem => elem.InstrumentationKind == PgoInstrumentationKind.GetLikelyClass));
            int methodsWithGetLikelyClass = profiledMethods.Count(spd => spd.SchemaData.Any(elem => elem.InstrumentationKind == PgoInstrumentationKind.GetLikelyClass));
            PrintOutput($"# GetLikelyClass data: {numGetLikelyClass} in {methodsWithGetLikelyClass} methods");

            var histogramCallSites = new List<(MethodProfileData mpd, int ilOffset)>();
            foreach (var mpd in profiledMethods)
            {
                var sites =
                    mpd.SchemaData
                    .Where(e => e.InstrumentationKind == PgoInstrumentationKind.HandleHistogramTypes || e.InstrumentationKind == PgoInstrumentationKind.GetLikelyClass)
                    .Select(e => e.ILOffset)
                    .Distinct();

                histogramCallSites.AddRange(sites.Select(ilOffset => (mpd, ilOffset)));
            }

            int CountGetLikelyClass(Func<GetLikelyClassResult, bool> predicate)
                => histogramCallSites.Count(t => predicate(GetLikelyClass(t.mpd.SchemaData, t.ilOffset)));

            PrintOutput($"# Call sites where getLikelyClass is null: {CountGetLikelyClass(r => r.IsNull)}");
            PrintOutput($"# Call sites where getLikelyClass is unknown: {CountGetLikelyClass(r => r.IsUnknown)}");
            PrintOutput($"# Call sites where getLikelyClass returns data that devirtualizes: {CountGetLikelyClass(r => r.Devirtualizes)}");

            static bool PresentAndZero(MethodProfileData mpd, PgoInstrumentationKind kind)
                => mpd.SchemaData.Any(e => e.InstrumentationKind == kind) && mpd.SchemaData.Sum(e => e.InstrumentationKind == kind ? e.DataLong : 0) == 0;

            static bool CountersSumToZero(MethodProfileData data)
                => PresentAndZero(data, PgoInstrumentationKind.BasicBlockIntCount) ||
                   PresentAndZero(data, PgoInstrumentationKind.BasicBlockLongCount) ||
                   PresentAndZero(data, PgoInstrumentationKind.EdgeIntCount) ||
                   PresentAndZero(data, PgoInstrumentationKind.EdgeLongCount);

            List<MethodProfileData> methodsWithZeroCounters = profiledMethods.Where(CountersSumToZero).ToList();
            if (methodsWithZeroCounters.Count > 0)
            {
                PrintOutput($"There are {methodsWithZeroCounters.Count} methods whose counters sum to 0{(methodsWithZeroCounters.Count > 10 ? " (10 shown)" : "")}:");
                foreach (MethodProfileData mpd in methodsWithZeroCounters.Take(10))
                    PrintOutput($"  {mpd.Method}");
            }

            PrintCallsitesByLikelyClassesChart(profiledMethods
                .SelectMany(m => m.SchemaData)
                .Where(sd => sd.InstrumentationKind == PgoInstrumentationKind.HandleHistogramTypes)
                .Select(GetUniqueClassesSeen)
                .ToArray());

            static int GetUniqueClassesSeen(PgoSchemaElem se)
            {
                int uniqueClassesSeen = ((TypeSystemEntityOrUnknown[])se.DataObject)
                    .Where(d => !d.IsNull) // ignore null, don't treat is as a unique call-site
                    .GroupBy(d => d.GetHashCode())
                    .Count();
                return uniqueClassesSeen;
            }

            PrintLikelihoodHistogram(profiledMethods
                .SelectMany(m => m.SchemaData)
                .Where(sd => sd.InstrumentationKind == PgoInstrumentationKind.HandleHistogramTypes)
                .Select(GetLikelihoodOfMostPopularType)
                .ToArray());

            static double GetLikelihoodOfMostPopularType(PgoSchemaElem se)
            {
                int count = ((TypeSystemEntityOrUnknown[])se.DataObject)
                    .GroupBy(d => d.GetHashCode())
                    .OrderByDescending(d => d.Count())
                    .FirstOrDefault(d => d.FirstOrDefault().AsType != null)
                    ?.Count() ?? 0;
                return count * 100.0 / se.DataObject.Length;
            }
        }

        private struct GetLikelyClassResult
        {
            public bool IsNull;
            public bool IsUnknown;
            public TypeDesc Type;
            public int Likelihood;
            public bool Devirtualizes;
        }

        private static GetLikelyClassResult GetLikelyClass(PgoSchemaElem[] schema, int ilOffset)
        {
            const int UNKNOWN_TYPEHANDLE_MIN = 1;
            const int UNKNOWN_TYPEHANDLE_MAX = 33;

            static bool IsUnknownTypeHandle(int handle)
                => handle >= UNKNOWN_TYPEHANDLE_MIN && handle <= UNKNOWN_TYPEHANDLE_MAX;

            for (int i = 0; i < schema.Length; i++)
            {
                var elem = schema[i];
                if (elem.ILOffset != ilOffset)
                    continue;

                if (elem.InstrumentationKind == PgoInstrumentationKind.GetLikelyClass)
                {
                    Trace.Assert(elem.Count == 1);
                    return new GetLikelyClassResult
                    {
                        IsUnknown = IsUnknownTypeHandle(((TypeSystemEntityOrUnknown[])elem.DataObject)[0].AsUnknown),
                        Likelihood = (byte)elem.Other,
                    };
                }

                bool isHistogramCount =
                    elem.InstrumentationKind == PgoInstrumentationKind.HandleHistogramIntCount ||
                    elem.InstrumentationKind == PgoInstrumentationKind.HandleHistogramLongCount;

                if (isHistogramCount && elem.Count == 1 && i + 1 < schema.Length && schema[i + 1].InstrumentationKind == PgoInstrumentationKind.HandleHistogramTypes)
                {
                    var handles = (TypeSystemEntityOrUnknown[])schema[i + 1].DataObject;
                    var histogram = handles.Where(e => !e.IsNull).GroupBy(e => e).ToList();
                    if (histogram.Count == 0)
                        return new GetLikelyClassResult { IsNull = true };

                    int totalCount = histogram.Sum(g => g.Count());
                    // The number of unknown type handles matters for the likelihood, but not for the most likely class that we pick, so we can remove them now.
                    histogram.RemoveAll(e => e.Key.IsNull || e.Key.IsUnknown);
                    if (histogram.Count == 0)
                        return new GetLikelyClassResult { IsUnknown = true };

                    // Now return the most likely one
                    var best = histogram.OrderByDescending(h => h.Count()).First();
                    Trace.Assert(best.Key.AsType != null);
                    int likelihood = best.Count() * 100 / totalCount;
                    // The threshold is different for interfaces and classes.
                    // A flag in the Other field of the TypeHandleHistogram*Count entry indicates which kind of call site this is.
                    bool isInterface = (elem.Other & (uint)ClassProfileFlags.IsInterface) != 0;
                    int threshold = isInterface ? 25 : 30;
                    return new GetLikelyClassResult
                    {
                        Type = best.Key.AsType,
                        Likelihood = likelihood,
                        Devirtualizes = likelihood >= threshold,
                    };
                }
            }

            return new GetLikelyClassResult { IsNull = true };
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
                    foreach (TraceProcess proc in traceLog.Processes.OrderByDescending(proc => proc.CPUMSec))
                    {
                        PrintOutput($"Procname = {proc.Name} Pid = {proc.ProcessID} CPUMsec = {proc.CPUMSec:F0}");
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

                if (!p.EventsInProcess.ByEventType<MethodJittingStartedTraceData>().Any() &&
                    !p.EventsInProcess.ByEventType<R2RGetEntryPointTraceData>().Any() &&
                    !p.EventsInProcess.ByEventType< SampledProfileTraceData>().Any())
                {
                    PrintError($"No data in trace for process\nWas the trace collected with provider at least \"Microsoft-Windows-DotNETRuntime:0x4000080018:5\"?");
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
                        try
                        {
                            if (!File.Exists(fileReference.FullName))
                            {
                                PrintError($"Unable to find reference '{fileReference.FullName}'");
                                filePathError = true;
                            }
                            else
                            {
                                tsc.GetModuleFromPath(fileReference.FullName, throwIfNotLoadable: false);
                            }
                        }
                        catch (Internal.TypeSystem.TypeSystemException.BadImageFormatException)
                        {
                            // Ignore BadImageFormat in order to allow users to use '-r *.dll'
                            // in a folder with native dynamic libraries (which have the same extension on Windows).

                            // We don't need to log a warning here - it's already logged in GetModuleFromPath
                        }
                    }
                }

                if (filePathError)
                    return -6;

                if (!tsc.Initialize())
                    return -12;

                TraceRuntimeDescToTypeSystemDesc idParser = new TraceRuntimeDescToTypeSystemDesc(p, tsc, clrInstanceId.Value);

                int mismatchErrors = 0;
                foreach (var e in p.EventsInProcess.ByEventType<ModuleLoadUnloadTraceData>())
                {
                    ModuleDesc loadedModule = idParser.ResolveModuleID(e.ModuleID, false);
                    if (loadedModule == null)
                    {
                        PrintWarning($"Unable to find loaded module {e.ModuleILFileName} to verify match");
                        continue;
                    }

                    EcmaModule ecmaModule = loadedModule as EcmaModule;
                    if (ecmaModule == null)
                    {
                        continue;
                    }

                    bool matched = false;
                    bool mismatch = false;
                    foreach (var debugEntry in ecmaModule.PEReader.ReadDebugDirectory())
                    {
                        if (debugEntry.Type == DebugDirectoryEntryType.CodeView)
                        {
                            var codeViewData = ecmaModule.PEReader.ReadCodeViewDebugDirectoryData(debugEntry);
                            if (codeViewData.Path.EndsWith("ni.pdb"))
                                continue;
                            if (codeViewData.Guid != e.ManagedPdbSignature)
                            {
                                PrintError($"Dll mismatch between assembly located at \"{e.ModuleILPath}\" during trace collection and module \"{tsc.PEReaderToFilePath(ecmaModule.PEReader)}\"");
                                mismatchErrors++;
                                mismatch = true;
                                continue;
                            }
                            else
                            {
                                matched = true;
                            }
                        }
                    }

                    if (!matched && !mismatch)
                    {
                        PrintMessage($"Unable to validate match between assembly located at \"{e.ModuleILPath}\" during trace collection and module \"{tsc.PEReaderToFilePath(ecmaModule.PEReader)}\"");
                    }

                    // TODO find some way to match on MVID as only some dlls have managed pdbs, and this won't find issues with embedded pdbs
                }

                if (mismatchErrors != 0)
                {
                    PrintError($"{mismatchErrors} mismatch error(s) found");
                    return -1;
                }

                // Now that the modules are validated run Init to prepare for the rest of execution
                idParser.Init();

                SortedDictionary<long, ProcessedMethodData> methodsToAttemptToPrepare = new SortedDictionary<long, ProcessedMethodData>();

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
                            clrInstanceId.Value,
                            commandLineOptions.PreciseDebugInfoFile,
                            s_logger);
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
                        if ((e.TimeStampRelativeMSec < commandLineOptions.ExcludeEventsBefore) && (e.TimeStampRelativeMSec > commandLineOptions.ExcludeEventsAfter))
                            continue;

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
                                methodsToAttemptToPrepare.Add(0x100000000 + (int)e.EventIndex, new ProcessedMethodData(e.TimeStampRelativeMSec, nextMethod, "SampleMethodCaller"));
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

                SampleCorrelator correlator = null;
                if (commandLineOptions.Spgo)
                {
                    correlator = new SampleCorrelator(GetMethodMemMap());

                    Guid lbrGuid = Guid.Parse("99134383-5248-43fc-834b-529454e75df3");
                    bool hasLbr = traceLog.Events.Any(e => e.TaskGuid == lbrGuid);

                    if (!hasLbr)
                    {
                        foreach (SampledProfileTraceData e in p.EventsInProcess.ByEventType<SampledProfileTraceData>())
                        {
                            correlator.AttributeSamplesToIP(e.InstructionPointer, 1);
                        }

                        PrintOutput($"Samples outside managed code: {correlator.SamplesOutsideManagedCode}");
                        PrintOutput($"Samples in managed code that does not have native<->IL mappings: {correlator.SamplesInManagedCodeWithoutAnyMappings}");
                        PrintOutput($"Samples in managed code with mappings that could not be correlated: {correlator.SamplesInManagedCodeOutsideMappings}");
                        PrintOutput($"Samples in inlinees that were not present in ETW events: {correlator.SamplesInUnknownInlinees}");
                        PrintOutput($"Samples in managed code for which we could not get the IL: {correlator.SamplesInManagedCodeWithoutIL}");
                        PrintOutput($"Samples in managed code that could not be attributed to the method's flow graph: {correlator.SamplesInManagedCodeOutsideFlowGraph}");
                        PrintOutput($"Samples successfully attributed: {correlator.TotalAttributedSamples}");
                    }
                    else
                    {
                        long numLbrRecords = 0;
                        foreach (var e in traceLog.Events)
                        {
                            if (e.TaskGuid != lbrGuid)
                                continue;

                            // Opcode is always 32 for the LBR event.
                            if (e.Opcode != (TraceEventOpcode)32)
                                continue;

                            numLbrRecords++;

                            unsafe
                            {
                                if (traceLog.PointerSize == 4)
                                {
                                    // For 32-bit machines we convert the data into a 64-bit format first.
                                    LbrTraceEventData32* data = (LbrTraceEventData32*)e.DataStart;
                                    if (data->ProcessId != p.ProcessID)
                                        continue;

                                    Span<LbrEntry32> lbr32 = data->Entries(e.EventDataLength);
                                    correlator.AttributeSampleToLbrRuns(lbr32);
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

                                    Span<LbrEntry64> lbr64 = data->Entries(e.EventDataLength);
                                    correlator.AttributeSampleToLbrRuns(lbr64);
                                }
                            }
                        }

                        PrintOutput($"Profile is based on {numLbrRecords} LBR records");
                    }

                    correlator.SmoothAllProfiles();
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
                            methodData.InstrumentationData = PgoProcessor.ParsePgoData<TypeSystemEntityOrUnknown, TypeSystemEntityOrUnknown>(pgoDataLoader, intDecompressor, true).ToArray();
                        }
                        else
                        {
                            SampleProfile sp = correlator?.GetProfile(methodData.Method);
                            if (sp != null && sp.AttributedSamples >= commandLineOptions.SpgoMinSamples)
                            {
                                IEnumerable<PgoSchemaElem> schema =
                                    sp.SmoothedSamples
                                    .Select(
                                        kvp =>
                                        new PgoSchemaElem
                                        {
                                            InstrumentationKind = kvp.Value > uint.MaxValue ? PgoInstrumentationKind.BasicBlockLongCount : PgoInstrumentationKind.BasicBlockIntCount,
                                            ILOffset = kvp.Key.Start,
                                            Count = 1,
                                            DataLong = kvp.Value,
                                        });

                                if (commandLineOptions.IncludeFullGraphs)
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

#if DEBUG
                                if (commandLineOptions.IncludeFullGraphs)
                                {
                                    var writtenBlocks =
                                        new HashSet<int>(
                                            methodData.InstrumentationData
                                            .Where(elem => elem.InstrumentationKind == PgoInstrumentationKind.BasicBlockIntCount || elem.InstrumentationKind == PgoInstrumentationKind.BasicBlockLongCount)
                                            .Select(elem => elem.ILOffset));

                                    var writtenEdges =
                                        new HashSet<(int, int)>(
                                            methodData.InstrumentationData
                                            .Where(elem => elem.InstrumentationKind == PgoInstrumentationKind.EdgeIntCount || elem.InstrumentationKind == PgoInstrumentationKind.EdgeLongCount)
                                            .Select(elem => (elem.ILOffset, elem.Other)));

                                    Debug.Assert(writtenBlocks.SetEquals(sp.FlowGraph.BasicBlocks.Select(bb => bb.Start)));
                                    Debug.Assert(writtenEdges.SetEquals(sp.FlowGraph.BasicBlocks.SelectMany(bb => bb.Targets.Select(bbTar => (bb.Start, bbTar.Start)))));
                                }
#endif
                            }
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

        static void GenerateJittraceFile(FileInfo outputFileName, IEnumerable<ProcessedMethodData> methodsToAttemptToPrepare, JitTraceOptions jittraceOptions)
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
                // Format is FriendlyNameOfMethod~typeIndex~ArgCount~GenericParameterCount:genericParamsSeparatedByColons~MethodName
                // This format is not sufficient to exactly describe methods, so the runtime component may compile similar methods
                // In the various strings \ is escaped to \\ and in the outer ~ csv the ~ character is escaped to \s. In the inner csv : is escaped to \s
                try
                {
                    string timeStampAddon = "";
                    if (jittraceOptions.HasFlag(JitTraceOptions.showtimestamp))
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

            if (jittraceOptions.HasFlag(JitTraceOptions.sorted))
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
