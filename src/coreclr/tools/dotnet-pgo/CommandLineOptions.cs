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
        public bool VerboseWarnings;
        public jittraceoptions JitTraceOptions;
        public double ExcludeEventsBefore;
        public double ExcludeEventsAfter;
        public bool Warnings;
        public bool Uncompressed;
        public bool BasicProgressMessages;

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
                FailIfUnspecified(syntax.DefineOption(
                    name: "t|trace",
                    value: ref traceFile,
                    help: "Specify the trace file to be parsed.",
                    requireValue: true));
                if (traceFile != null)
                    TraceFile = new FileInfo(traceFile);

                string outputFile = null;
                FailIfUnspecified(syntax.DefineOption(
                    name: "o|output",
                    value: ref outputFile,
                    help: "Specify the output filename to be created.",
                    requireValue: true));
                if (outputFile != null)
                    OutputFileName = new FileInfo(outputFile);

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

                IReadOnlyList<string> referencesAsStrings = null;
                syntax.DefineOptionList(name: "r|reference", value: ref referencesAsStrings, help: "If a reference is not located on disk at the same location as used in the process, it may be specified with a --reference parameter. Multiple --reference parameters may be specified. The wild cards * and ? are supported by this option.", requireValue: true);
                List<FileInfo> referenceList = new List<FileInfo>();
                Reference = referenceList;
                if (referencesAsStrings != null)
                {
                    foreach (string pattern in referencesAsStrings)
                    {
                        Dictionary<string, string> paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        Helpers.AppendExpandedPaths(paths, pattern, false);
                        foreach (string file in paths.Values)
                            referenceList.Add(new FileInfo(file));
                    }
                }

                syntax.DefineOption(
                    name: "exclude-events-before",
                    value: ref ExcludeEventsBefore,
                    help: "Exclude data from events before specified time. Time is specified as milliseconds from the start of the trace.",
                    valueConverter: Convert.ToDouble,
                    requireValue: true);

                syntax.DefineOption(
                    name: "exclude-events-after",
                    value: ref ExcludeEventsAfter,
                    help: "Exclude data from events after specified time. Time is specified as milliseconds from the start of the trace.",
                    valueConverter: Convert.ToDouble,
                    requireValue: true);

                Verbosity verbosity = default(Verbosity);
                syntax.DefineOption(name: "v|verbosity", value: ref verbosity, help: "Adjust verbosity level. Supported levels are minimal, normal, detailed, and diagnostic.", valueConverter: VerbosityConverter, requireValue: true);
                BasicProgressMessages = (int)verbosity >= (int)Verbosity.normal;
                Warnings = (int)verbosity >= (int)Verbosity.normal;
                VerboseWarnings = (int)verbosity >= (int)Verbosity.detailed;
                DisplayProcessedEvents = (int)verbosity >= (int)Verbosity.diagnostic;
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
#if Debug
                    ValidateOutputFile = true;
#else
                ValidateOutputFile = false;
#endif
                CommonOptions();
                bool compressed = false;
                syntax.DefineOption(name: "compressed", value: ref compressed, help: "Generate compressed mibc", requireValue: false);
                Uncompressed = !compressed;

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
            if (Help || !FileType.HasValue)
            {
                Help = true;
            }
        }
    }
}
