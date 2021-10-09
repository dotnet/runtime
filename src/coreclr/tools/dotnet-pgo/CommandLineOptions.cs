// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using Internal.CommandLine;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    internal class CommandLineOptions
    {
        public bool Help;
        public string HelpText;

        public FileInfo TraceFile;
        public FileInfo OutputFileName;
        public int? Pid;
        public string ProcessName;
        public PgoFileType? FileType;
        public IEnumerable<FileInfo> Reference;
        public int? ClrInstanceId;
        public bool ProcessJitEvents;
        public bool ProcessR2REvents;
        public bool DisplayProcessedEvents;
        public bool ValidateOutputFile;
        public bool GenerateCallGraph;
        public bool Spgo;
        public bool SpgoIncludeBlockCounts;
        public bool SpgoIncludeEdgeCounts;
        public int SpgoMinSamples = 50;
        public bool VerboseWarnings;
        public jittraceoptions JitTraceOptions;
        public double ExcludeEventsBefore;
        public double ExcludeEventsAfter;
        public bool Warnings;
        public bool Uncompressed;
        public bool BasicProgressMessages;
        public bool DetailedProgressMessages;
        public List<FileInfo> InputFilesToMerge;
        public List<AssemblyName> IncludedAssemblies = new List<AssemblyName>();
        public bool DumpMibc = false;
        public FileInfo InputFileToDump;
        public List<FileInfo> CompareMibc;
        public bool InheritTimestamp;

        public string[] HelpArgs = Array.Empty<string>();

        private enum Verbosity
        {
            minimal,
            normal,
            detailed,
            diagnostic
        }

        private Verbosity VerbosityConverter(string s)
        {
            try
            {
                return (Verbosity)Enum.Parse(typeof(Verbosity), s);
            }
            catch 
            {
                throw new FormatException("Must be one of minimal, normal, detailed, or diagnostic");
            }
        }

        private void FailIfUnspecified(Argument arg)
        {
            if (arg.Command.IsActive && !arg.IsSpecified)
                throw new ArgumentSyntaxException($"--{arg.Name} must be specified.");
        }

        void DefineArgumentSyntax(ArgumentSyntax syntax)
        {
            bool activeCommandIsCommandAssociatedWithTraceProcessing = false;
            syntax.ApplicationName = typeof(Program).Assembly.GetName().Name.ToString();

            // HandleHelp writes to error, fails fast with crash dialog and lacks custom formatting.
            syntax.HandleHelp = false;
            syntax.HandleErrors = false;

            string command = "";

            void CommonOptions()
            {
                string traceFile = null;
                syntax.DefineOption(
                    name: "t|trace",
                    value: ref traceFile,
                    help: "Specify the trace file to be parsed.",
                    requireValue: true);
                if (traceFile != null)
                    TraceFile = new FileInfo(traceFile);

                OutputOption();

                int pidLocal = 0;
                if (syntax.DefineOption(
                        name: "pid",
                        value: ref pidLocal,
                        help: "The pid within the trace of the process to examine. If this is a multi-process trace, at least one of --pid or --process-name must be specified",
                        requireValue: true).IsSpecified)
                    Pid = pidLocal;

                syntax.DefineOption(
                    name: "process-name",
                    value: ref ProcessName,
                    help: "The process name within the trace of the process to examine. If this is a multi-process trace, at least one of --pid or --process-name must be specified.",
                    requireValue: false);

                int clrInstanceIdLocal = 0;
                if (syntax.DefineOption(
                        name: "clr-instance-id",
                        value: ref clrInstanceIdLocal,
                        help: "If the process contains multiple .NET runtimes, the instance ID must be specified.",
                        requireValue: true).IsSpecified)
                {
                    ClrInstanceId = clrInstanceIdLocal;
                }

                Reference = DefineFileOptionList(name: "r|reference", help: "If a reference is not located on disk at the same location as used in the process, it may be specified with a --reference parameter. Multiple --reference parameters may be specified. The wild cards * and ? are supported by this option.");

                ExcludeEventsBefore = Double.MinValue;
                syntax.DefineOption(
                    name: "exclude-events-before",
                    value: ref ExcludeEventsBefore,
                    help: "Exclude data from events before specified time. Time is specified as milliseconds from the start of the trace.",
                    valueConverter: Convert.ToDouble,
                    requireValue: true);

                ExcludeEventsAfter = Double.MaxValue;
                syntax.DefineOption(
                    name: "exclude-events-after",
                    value: ref ExcludeEventsAfter,
                    help: "Exclude data from events after specified time. Time is specified as milliseconds from the start of the trace.",
                    valueConverter: Convert.ToDouble,
                    requireValue: true);

                VerbosityOption();
            }

            void OutputOption()
            {
                string outputFile = null;
                syntax.DefineOption(
                    name: "o|output",
                    value: ref outputFile,
                    help: "Specify the output filename to be created.",
                    requireValue: true);
                if (outputFile != null)
                    OutputFileName = new FileInfo(outputFile);
            }

            void VerbosityOption()
            {
                Verbosity verbosity = Verbosity.normal;
                syntax.DefineOption(name: "v|verbosity", value: ref verbosity, help: "Adjust verbosity level. Supported levels are minimal, normal, detailed, and diagnostic.", valueConverter: VerbosityConverter, requireValue: true);
                BasicProgressMessages = (int)verbosity >= (int)Verbosity.normal;
                Warnings = (int)verbosity >= (int)Verbosity.normal;
                VerboseWarnings = (int)verbosity >= (int)Verbosity.detailed;
                DetailedProgressMessages = (int)verbosity >= (int)Verbosity.detailed;
                DisplayProcessedEvents = (int)verbosity >= (int)Verbosity.diagnostic;
            }

            void CompressedOption()
            {
                bool compressed = false;
                syntax.DefineOption(name: "compressed", value: ref compressed, help: "Generate compressed mibc", requireValue: false);
                Uncompressed = !compressed;
            }

            void HelpOption()
            {
                syntax.DefineOption("h|help", ref Help, "Display this usage message.");
            }

            var mibcCommand = syntax.DefineCommand(name: "create-mibc", value: ref command, help: "Transform a trace file into a Mibc profile data file.");
            if (mibcCommand.IsActive)
            {
                activeCommandIsCommandAssociatedWithTraceProcessing = true;
                HelpArgs = new string[] { "create-mibc", "--help", "--trace", "trace", "--output", "output" };
                FileType = PgoFileType.mibc;
                GenerateCallGraph = true;
                ProcessJitEvents = true;
                ProcessR2REvents = true;
#if DEBUG
                ValidateOutputFile = true;
#else
                ValidateOutputFile = false;
#endif
                CommonOptions();
                CompressedOption();

                syntax.DefineOption(name: "spgo", value: ref Spgo, help: "Base profile on samples in the input. Uses last branch records if available and otherwise raw IP samples.", requireValue: false);
                syntax.DefineOption(name: "spgo-with-block-counts", value: ref SpgoIncludeBlockCounts, help: "Include block counts in the written .mibc file. If neither this nor spgo-with-edge-counts are specified, then defaults to true.", requireValue: false);
                syntax.DefineOption(name: "spgo-with-edge-counts", value: ref SpgoIncludeEdgeCounts, help: "Include edge counts in the written .mibc file.", requireValue: false);
                syntax.DefineOption(name: "spgo-min-samples", value: ref SpgoMinSamples, help: $"The minimum number of total samples a function must have before generating profile data for it with SPGO. Default: {SpgoMinSamples}", requireValue: false);

                if (!SpgoIncludeBlockCounts && !SpgoIncludeEdgeCounts)
                    SpgoIncludeBlockCounts = true;

                HelpOption();
            }

            JitTraceOptions = jittraceoptions.none;
#if DEBUG
            // Usage of the jittrace format requires using logic embedded in the runtime repository and isn't suitable for general consumer use at this time
            // Build it in debug and check builds to ensure that it doesn't bitrot, and remains available for use by developers willing to build the repo
            var jittraceCommand = syntax.DefineCommand(name: "create-jittrace", value: ref command, help: "Transform a trace file into a jittrace runtime file.");
            if (jittraceCommand.IsActive)
            {
                activeCommandIsCommandAssociatedWithTraceProcessing = true;
                HelpArgs = new string[] { "create-jittrace", "--help", "--trace", "trace", "--output", "output" };
                FileType = PgoFileType.jittrace;
                ProcessJitEvents = true;
                ProcessR2REvents = false;
                ValidateOutputFile = false;
                CommonOptions();

                bool sorted = false;
                syntax.DefineOption(name: "sorted", value: ref sorted, help: "Generate sorted output.", requireValue: false);
                if (sorted)
                {
                    JitTraceOptions |= jittraceoptions.sorted;
                }

                bool showtimestamp = false;
                syntax.DefineOption(name: "showtimestamp", value: ref showtimestamp, help: "Show timestamps in output.", requireValue: false);
                if (showtimestamp)
                {
                    JitTraceOptions |= jittraceoptions.showtimestamp;
                }

                syntax.DefineOption(name: "includeReadyToRun", value: ref ProcessR2REvents, help: "Include ReadyToRun methods in the trace file.", requireValue: false);
                HelpOption();
            }
#endif

            var mergeCommand = syntax.DefineCommand(name: "merge", value: ref command, help: "Merge multiple Mibc profile data files into one file.");
            if (mergeCommand.IsActive)
            {
                HelpArgs = new string[] { "merge", "--help", "--output", "output", "--input", "input"};

                InputFilesToMerge = DefineFileOptionList(name: "i|input", help: "Input .mibc files to be merged. Multiple input arguments are specified as --input file1.mibc --input file2.mibc");
                OutputOption();

                IReadOnlyList<string> assemblyNamesAsStrings = null;
                syntax.DefineOptionList(name: "include-reference", value: ref assemblyNamesAsStrings, help: "If specified, include in Mibc file only references to the specified assemblies. Assemblies are specified as assembly names, not filenames. For instance, `System.Private.CoreLib` not `System.Private.CoreLib.dll`. Multiple --include-reference options may be specified.", requireValue: true);
                if (assemblyNamesAsStrings != null)
                {
                    foreach (string asmName in assemblyNamesAsStrings)
                    {
                        try
                        {
                            IncludedAssemblies.Add(new AssemblyName(asmName));
                        }
                        catch
                        {
                            throw new FormatException($"Unable to parse '{asmName}' as an Assembly Name.");
                        }
                    }
                }

                syntax.DefineOption(name: "inherit-timestamp", value: ref InheritTimestamp, help: "If specified, set the output's timestamp to the max timestamp of the input files");

                VerbosityOption();
                CompressedOption();
                HelpOption();
#if DEBUG
                ValidateOutputFile = true;
#else
                ValidateOutputFile = false;
#endif
            }

            var dumpCommand = syntax.DefineCommand(name: "dump", value: ref command, help: "Dump the contents of a Mibc file.");
            if (dumpCommand.IsActive)
            {
                DumpMibc = true;
                HelpArgs = new string[] { "dump", "--help", "input", "output" };

                VerbosityOption();
                HelpOption();

                string inputFileToDump = null;
                syntax.DefineParameter(name: "input", ref inputFileToDump, "Name of the input mibc file to dump.");
                if (inputFileToDump != null)
                    InputFileToDump = new FileInfo(inputFileToDump);

                string outputFile = null;
                syntax.DefineParameter(name: "output", ref outputFile, "Name of the output dump file.");
                if (outputFile != null)
                    OutputFileName = new FileInfo(outputFile);
            }

            var compareMibcCommand = syntax.DefineCommand(name: "compare-mibc", value: ref command, help: "Compare two .mibc files");
            if (compareMibcCommand.IsActive)
            {
                HelpArgs = new[] { "compare-mibc", "--input", "first.mibc", "--input", "second.mibc" };
                CompareMibc = DefineFileOptionList(name: "i|input", help: "The input .mibc files to be compared. Specify as --input file1.mibc --input file2.mibc");
                if (CompareMibc.Count != 2)
                    Help = true;
            }

            if (syntax.ActiveCommand == null)
            {
                // No command specified
                Help = true;
            }

            if (activeCommandIsCommandAssociatedWithTraceProcessing)
            {
                HelpText =
@$"{syntax.GetHelpText()}
Example tracing commands used to generate the input to this tool:
""dotnet-trace collect -p 73060 --providers Microsoft-Windows-DotNETRuntime:0x1E000080018:4""
 - Capture events from process 73060 where we capture both JIT and R2R events using EventPipe tracing

""dotnet-trace collect -p 73060 --providers Microsoft-Windows-DotNETRuntime:0x1C000080018:4""
 - Capture events from process 73060 where we capture only JIT events using EventPipe tracing

""perfview collect -LogFile:logOfCollection.txt -DataFile:jittrace.etl -Zip:false -merge:false -providers:Microsoft-Windows-DotNETRuntime:0x1E000080018:4""
 - Capture Jit and R2R events via perfview of all processes running using ETW tracing
";
            }
            else
            {
                HelpText = syntax.GetHelpText();
            }

            List<FileInfo> DefineFileOptionList(string name, string help)
            {
                IReadOnlyList<string> filesAsStrings = null;
                syntax.DefineOptionList(name: name, value: ref filesAsStrings, help: help, requireValue: true);
                List<FileInfo> referenceList = new List<FileInfo>();
                if (filesAsStrings != null)
                {
                    foreach (string pattern in filesAsStrings)
                    {
                        Dictionary<string, string> paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        Helpers.AppendExpandedPaths(paths, pattern, false);
                        foreach (string file in paths.Values)
                            referenceList.Add(new FileInfo(file));
                    }
                }
                return referenceList;
            }
        }

        public static CommandLineOptions ParseCommandLine(string[] args)
        {
            CommandLineOptions realCommandLineParse = new CommandLineOptions();
            try
            {
                realCommandLineParse.ParseCommmandLineHelper(args);
                return realCommandLineParse;
            }
            catch (ArgumentSyntaxException e)
            {
                ConsoleColor oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(Internal.CommandLine.Strings.ErrorWithMessageFmt, e.Message);
                Console.ForegroundColor = oldColor;

                CommandLineOptions helpParse = new CommandLineOptions();
                try
                {
                    helpParse.ParseCommmandLineHelper(realCommandLineParse.HelpArgs);
                }
                catch (ArgumentSyntaxException)
                {
                    // This exists to allow the command list help to work
                }
                Debug.Assert(helpParse.Help == true);
                return helpParse;
            }
        }

        private CommandLineOptions()
        {
        }

        private void ParseCommmandLineHelper(string[] args)
        {
            ArgumentSyntax argSyntax = ArgumentSyntax.Parse(args, DefineArgumentSyntax);
            if (Help || (!FileType.HasValue && (InputFilesToMerge == null) && !DumpMibc && CompareMibc == null))
            {
                Help = true;
            }
        }
    }
}
