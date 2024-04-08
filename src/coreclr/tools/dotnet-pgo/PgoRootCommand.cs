// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    internal sealed class PgoRootCommand : CliRootCommand
    {
        public CliOption<List<string>> InputFilesToMerge { get; } =
            new("--input", "-i") { CustomParser = result => Helpers.BuildPathList(result.Tokens), Description = "Input .mibc files to be merged. Multiple input arguments are specified as --input file1.mibc --input file2.mibc", Required = true, Arity = ArgumentArity.OneOrMore };
        public CliOption<string[]> InputFilesToCompare { get; } =
            new("--input", "-i") { Description = "The input .mibc files to be compared. Specify as --input file1.mibc --input file2.mibc", Required = true, Arity = new ArgumentArity(2, 2) /* exactly two */ };
        public CliOption<string> InputFileToDump { get; } =
            new("--input", "-i") { Description = "Name of the input mibc file to dump", Required = true, Arity = ArgumentArity.ExactlyOne };
        public CliOption<string> TraceFilePath { get; } =
            new("--trace", "-t") { Description = "Specify the trace file to be parsed" };
        public CliOption<string> OutputFilePath { get; } =
            new("--output", "-o") { Description = "Specify the output filename to be created" };
        public CliOption<string> PreciseDebugInfoFile { get; } =
            new("--precise-debug-info-file") { Description = "Name of file of newline separated JSON objects containing precise debug info" };
        public CliOption<int> Pid { get; } =
            new("--pid") { Description = "The pid within the trace of the process to examine. If this is a multi-process trace, at least one of --pid or --process-name must be specified" };
        public CliOption<string> ProcessName { get; } =
            new("--process-name") { Description = "The process name within the trace of the process to examine. If this is a multi-process trace, at least one of --pid or --process-name must be specified" };
        public CliOption<List<string>> Reference =
            new("--reference", "-r") { CustomParser = result => Helpers.BuildPathList(result.Tokens), DefaultValueFactory = result => Helpers.BuildPathList(result.Tokens), Description = "If a reference is not located on disk at the same location as used in the process, it may be specified with a --reference parameter. Multiple --reference parameters may be specified. The wild cards * and ? are supported by this option" };
        public CliOption<int> ClrInstanceId { get; } =
            new("--clr-instance-id") { Description = "If the process contains multiple .NET runtimes, the instance ID must be specified" };
        public CliOption<bool> Spgo { get; } =
            new("--spgo") { Description = "Base profile on samples in the input. Uses last branch records if available and otherwise raw IP samples" };
        public CliOption<int> SpgoMinSamples { get; } =
            new("--spgo-min-samples") { DefaultValueFactory = _ => 50, Description = "The minimum number of total samples a function must have before generating profile data for it with SPGO. Default: 50" };
        public CliOption<bool> IncludeFullGraphs { get; } =
            new("--include-full-graphs") { Description = "Include all blocks and edges in the written .mibc file, regardless of profile counts" };
        public CliOption<double> ExcludeEventsBefore { get; } =
            new("--exclude-events-before") { DefaultValueFactory = _ => Double.MinValue, Description = "Exclude data from events before specified time. Time is specified as milliseconds from the start of the trace" };
        public CliOption<double> ExcludeEventsAfter { get; } =
            new("--exclude-events-after") { DefaultValueFactory = _ => Double.MaxValue, Description = "Exclude data from events after specified time. Time is specified as milliseconds from the start of the trace" };
        public CliOption<string> ExcludeEventsBeforeJittingMethod { get; } =
            new("--exclude-events-before-jitting-method") { DefaultValueFactory = _ => string.Empty, Description = "Exclude data from events before observing a specific method getting jitted. Method is matched using a regular expression against the method name. Note that the method name is formatted the same as in PerfView which includes typed parameters." };
        public CliOption<string> ExcludeEventsAfterJittingMethod { get; } =
            new("--exclude-events-after-jitting-method") { DefaultValueFactory = _ => string.Empty, Description = "Exclude data from events after observing a specific method getting jitted. Method is matched using a regular expression against the method name. Note that the method name is formatted the same as in PerfView which includes typed parameters." };
        public CliOption<string> IncludeMethods { get; } =
            new("--include-methods") { DefaultValueFactory = _ => string.Empty, Description = "Include methods with names matching regular expression. Note that the method names are formatted the same as in PerfView which includes typed parameters." };
        public CliOption<string> ExcludeMethods { get; } =
            new("--exclude-methods") { DefaultValueFactory = _ => string.Empty, Description = "Exclude methods with names matching regular expression. Note that the method names are formatted the same as in PerfView which includes typed parameters." };
        public CliOption<bool> Compressed { get; } =
            new("--compressed") { DefaultValueFactory = _ => true, Description = "Generate compressed mibc" };
        public CliOption<int> DumpWorstOverlapGraphs { get; } =
            new("--dump-worst-overlap-graphs") { DefaultValueFactory = _ => -1, Description = "Number of graphs to dump to .dot format in dump-worst-overlap-graphs-to directory" };
        public CliOption<string> DumpWorstOverlapGraphsTo { get; } =
            new("--dump-worst-overlap-graphs-to") { Description = "Number of graphs to dump to .dot format in dump-worst-overlap-graphs-to directory" };
        public CliOption<bool> AutomaticReferences { get; } =
            new("--automatic-references") { DefaultValueFactory = _ => true, Description = "Attempt to find references by using paths embedded in the trace file. Defaults to true" };
        public CliOption<AssemblyName[]> IncludedAssemblies { get; } =
            new("--include-reference") { CustomParser = MakeAssemblyNameArray, DefaultValueFactory = MakeAssemblyNameArray, Description = "If specified, include in Mibc file only references to the specified assemblies. Assemblies are specified as assembly names, not filenames. For instance, `System.Private.CoreLib` not `System.Private.CoreLib.dll`. Multiple --include-reference options may be specified." };

        private CliOption<bool> _includeReadyToRun { get; } =
            new("--includeReadyToRun") { Description = "Include ReadyToRun methods in the trace file" };
        private CliOption<Verbosity> _verbosity { get; } =
            new("--verbose") { DefaultValueFactory = _ => Verbosity.normal, Description = "Adjust verbosity level. Supported levels are minimal, normal, detailed, and diagnostic" };
        private CliOption<bool> _isSorted { get; } =
            new("--sorted") { Description = "Generate sorted output." };
        private CliOption<bool> _showTimestamp { get; } =
            new("--showtimestamp") { Description = "Show timestamps in output" };

        public PgoFileType? FileType;
        public bool ProcessJitEvents;
        public bool DisplayProcessedEvents;
        public bool ValidateOutputFile;
        public bool GenerateCallGraph;
        public bool VerboseWarnings;
        public JitTraceOptions JitTraceOptions;
        public bool Warnings;
        public bool BasicProgressMessages;
        public bool DetailedProgressMessages;
        public bool DumpMibc;
        public ParseResult Result;
        public bool ProcessR2REvents;

        private enum Verbosity
        {
            minimal,
            normal,
            detailed,
            diagnostic
        }

        public PgoRootCommand(string[] args) : base(".NET PGO Tool")
        {
            CliCommand createMbicCommand = new("create-mibc", "Transform a trace file into a Mibc profile data file")
            {
                TraceFilePath,
                OutputFilePath,
                Pid,
                ProcessName,
                Reference,
                ClrInstanceId,
                ExcludeEventsBefore,
                ExcludeEventsAfter,
                ExcludeEventsBeforeJittingMethod,
                ExcludeEventsAfterJittingMethod,
                IncludeMethods,
                ExcludeMethods,
                AutomaticReferences,
                _verbosity,
                Compressed,
                PreciseDebugInfoFile,
                Spgo,
                SpgoMinSamples,
                IncludeFullGraphs
            };

            createMbicCommand.SetAction(result =>
            {
                FileType = PgoFileType.mibc;
                GenerateCallGraph = true;
                ProcessJitEvents = true;
                ProcessR2REvents = true;
#if DEBUG
                ValidateOutputFile = true;
#else
                ValidateOutputFile = false;
#endif

                return ExecuteWithContext(result, true);
            });

            Subcommands.Add(createMbicCommand);

            JitTraceOptions = JitTraceOptions.none;
#if DEBUG
            CliCommand createJitTraceCommand = new("create-jittrace","Transform a trace file into a jittrace runtime file")
            {
                TraceFilePath,
                OutputFilePath,
                Pid,
                ProcessName,
                Reference,
                ClrInstanceId,
                ExcludeEventsBefore,
                ExcludeEventsAfter,
                AutomaticReferences,
                _verbosity,
                _isSorted,
                _showTimestamp,
                _includeReadyToRun
            };

            createJitTraceCommand.SetAction(result =>
            {
                FileType = PgoFileType.jittrace;
                ProcessJitEvents = true;
                ValidateOutputFile = false;
                ProcessR2REvents = result.GetValue(_includeReadyToRun);

                if (result.GetValue(_isSorted))
                {
                    JitTraceOptions |= JitTraceOptions.sorted;
                }

                if (result.GetValue(_showTimestamp))
                {
                    JitTraceOptions |= JitTraceOptions.showtimestamp;
                }

                return ExecuteWithContext(result, true);
            });

            Subcommands.Add(createJitTraceCommand);
#endif

            CliCommand mergeCommand = new("merge", "Merge multiple Mibc profile data files into one file")
            {
                InputFilesToMerge,
                OutputFilePath,
                IncludedAssemblies,
                _verbosity,
                Compressed
            };

            mergeCommand.SetAction(result =>
            {
#if DEBUG
                ValidateOutputFile = true;
#else
                ValidateOutputFile = false;
#endif

                return ExecuteWithContext(result, true);
            });

            Subcommands.Add(mergeCommand);

            CliCommand dumpCommand = new("dump", "Dump the contents of a Mibc file")
            {
                _verbosity,
                InputFileToDump,
                OutputFilePath,
            };

            dumpCommand.SetAction(result =>
            {
                DumpMibc = true;
                return ExecuteWithContext(result, true);
            });

            Subcommands.Add(dumpCommand);

            CliCommand compareMbicCommand = new("compare-mibc", "Compare two .mibc files")
            {
                InputFilesToCompare,
                DumpWorstOverlapGraphs,
                DumpWorstOverlapGraphsTo
            };

            compareMbicCommand.SetAction(result => ExecuteWithContext(result, false));

            Subcommands.Add(compareMbicCommand);

            int ExecuteWithContext(ParseResult result, bool setVerbosity)
            {
                Result = result;

                if (setVerbosity)
                {
                    Verbosity verbosity = Result.GetValue(_verbosity);
                    BasicProgressMessages = (int)verbosity >= (int)Verbosity.normal;
                    Warnings = (int)verbosity >= (int)Verbosity.normal;
                    VerboseWarnings = (int)verbosity >= (int)Verbosity.detailed;
                    DetailedProgressMessages = (int)verbosity >= (int)Verbosity.detailed;
                    DisplayProcessedEvents = (int)verbosity >= (int)Verbosity.diagnostic;
                }

                try
                {
                    return new Program(this).Run();
                }
                catch (Exception e)
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.Error.WriteLine("Error: " + e.Message);
                    Console.Error.WriteLine(e.ToString());

                    Console.ResetColor();
                }

                return 1;
            }
        }

        public static IEnumerable<Func<HelpContext, bool>> GetExtendedHelp(HelpContext context)
        {
            foreach (Func<HelpContext, bool> sectionDelegate in HelpBuilder.Default.GetLayout())
                yield return sectionDelegate;

            if (context.Command.Name == "create-mibc" || context.Command.Name == "create-jittrace")
            {
                yield return _ =>
                {
                    Console.WriteLine(
@"Example tracing commands used to generate the input to this tool:
""dotnet-trace collect -p 73060 --providers Microsoft-Windows-DotNETRuntime:0x1E000080018:4""
- Capture events from process 73060 where we capture both JIT and R2R events using EventPipe tracing

""dotnet-trace collect -p 73060 --providers Microsoft-Windows-DotNETRuntime:0x1C000080018:4""
- Capture events from process 73060 where we capture only JIT events using EventPipe tracing

""perfview collect -LogFile:logOfCollection.txt -DataFile:jittrace.etl -Zip:false -merge:false -providers:Microsoft-Windows-DotNETRuntime:0x1E000080018:4""
- Capture Jit and R2R events via perfview of all processes running using ETW tracing
");
                    return true;
                };
            }
        }

        private static AssemblyName[] MakeAssemblyNameArray(ArgumentResult result)
        {
            if (result.Tokens.Count > 0)
            {
                var includedAssemblies = new List<AssemblyName>();
                foreach (CliToken token in result.Tokens)
                {
                    try
                    {
                        includedAssemblies.Add(new AssemblyName(token.Value));
                    }
                    catch
                    {
                        throw new FormatException($"Unable to parse '{token.Value}' as an Assembly Name.");
                    }
                }
                return includedAssemblies.ToArray();
            }

            return Array.Empty<AssemblyName>();
        }
    }
}
