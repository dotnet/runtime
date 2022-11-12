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
    internal sealed class PgoRootCommand : RootCommand
    {
        public Option<List<string>> InputFilesToMerge { get; } =
            new(new[] { "--input", "-i" }, result => Helpers.BuildPathList(result.Tokens), false, "Input .mibc files to be merged. Multiple input arguments are specified as --input file1.mibc --input file2.mibc") { IsRequired = true, Arity = ArgumentArity.OneOrMore };
        public Option<string[]> InputFilesToCompare { get; } =
            new(new[] { "--input", "-i" }, "The input .mibc files to be compared. Specify as --input file1.mibc --input file2.mibc") { IsRequired = true, Arity = new ArgumentArity(2, 2) /* exactly two */ };
        public Option<string> InputFileToDump { get; } =
            new(new[] { "--input", "-i" }, "Name of the input mibc file to dump") { IsRequired = true, Arity = ArgumentArity.ExactlyOne };
        public Option<string> TraceFilePath { get; } =
            new(new[] { "--trace", "-t" }, "Specify the trace file to be parsed");
        public Option<string> OutputFilePath { get; } =
            new(new[] { "--output", "-o" }, "Specify the output filename to be created");
        public Option<string> PreciseDebugInfoFile { get; } =
            new(new[] { "--precise-debug-info-file" }, "Name of file of newline separated JSON objects containing precise debug info");
        public Option<int> Pid { get; } =
            new(new[] { "--pid" }, "The pid within the trace of the process to examine. If this is a multi-process trace, at least one of --pid or --process-name must be specified");
        public Option<string> ProcessName { get; } =
            new(new[] { "--process-name" }, "The process name within the trace of the process to examine. If this is a multi-process trace, at least one of --pid or --process-name must be specified");
        public Option<List<string>> Reference =
            new(new[] { "--reference", "-r" }, result => Helpers.BuildPathList(result.Tokens), true, "If a reference is not located on disk at the same location as used in the process, it may be specified with a --reference parameter. Multiple --reference parameters may be specified. The wild cards * and ? are supported by this option");
        public Option<int> ClrInstanceId { get; } =
            new("--clr-instance-id", "If the process contains multiple .NET runtimes, the instance ID must be specified");
        public Option<bool> Spgo { get; } =
            new("--spgo", "Base profile on samples in the input. Uses last branch records if available and otherwise raw IP samples");
        public Option<int> SpgoMinSamples { get; } =
            new("--spgo-min-samples", () => 50, "The minimum number of total samples a function must have before generating profile data for it with SPGO. Default: 50");
        public Option<bool> IncludeFullGraphs { get; } =
            new("--include-full-graphs", "Include all blocks and edges in the written .mibc file, regardless of profile counts");
        public Option<double> ExcludeEventsBefore { get; } =
            new("--exclude-events-before", () => Double.MinValue, "Exclude data from events before specified time. Time is specified as milliseconds from the start of the trace");
        public Option<double> ExcludeEventsAfter { get; } =
            new("--exclude-events-after", () => Double.MaxValue, "Exclude data from events after specified time. Time is specified as milliseconds from the start of the trace");
        public Option<bool> Compressed { get; } =
            new("--compressed", () => true, "Generate compressed mibc");
        public Option<int> DumpWorstOverlapGraphs { get; } =
            new("--dump-worst-overlap-graphs", () => -1, "Number of graphs to dump to .dot format in dump-worst-overlap-graphs-to directory");
        public Option<string> DumpWorstOverlapGraphsTo { get; } =
            new("--dump-worst-overlap-graphs-to", "Number of graphs to dump to .dot format in dump-worst-overlap-graphs-to directory");
        public Option<bool> InheritTimestamp { get; } =
            new("--inherit-timestamp", "If specified, set the output's timestamp to the max timestamp of the input files");
        public Option<bool> AutomaticReferences { get; } =
            new("--automatic-references", () => true, "Attempt to find references by using paths embedded in the trace file. Defaults to true");
        public Option<AssemblyName[]> IncludedAssemblies { get; } =
            new("--include-reference", result =>
            {
                if (result.Tokens.Count > 0)
                {
                    var includedAssemblies = new List<AssemblyName>();
                    foreach (Token token in result.Tokens)
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
                }

                return Array.Empty<AssemblyName>();
            }, true, "If specified, include in Mibc file only references to the specified assemblies. Assemblies are specified as assembly names, not filenames. For instance, `System.Private.CoreLib` not `System.Private.CoreLib.dll`. Multiple --include-reference options may be specified.");

        private Option<bool> _includeReadyToRun { get; } =
            new("--includeReadyToRun", "Include ReadyToRun methods in the trace file");
        private Option<Verbosity> _verbosity { get; } =
            new(new[] { "--verbose", "-v" }, () => Verbosity.normal, "Adjust verbosity level. Supported levels are minimal, normal, detailed, and diagnostic");
        private Option<bool> _isSorted { get; } =
            new("--sorted", "Generate sorted output.");
        private Option<bool> _showTimestamp { get; } =
            new("--showtimestamp", "Show timestamps in output");

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
            Command createMbicCommand = new("create-mibc", "Transform a trace file into a Mibc profile data file")
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
                Compressed,
                PreciseDebugInfoFile,
                Spgo,
                SpgoMinSamples,
                IncludeFullGraphs
            };

            createMbicCommand.SetHandler(context =>
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

                TryExecuteWithContext(context, true);
            });

            AddCommand(createMbicCommand);

            JitTraceOptions = JitTraceOptions.none;
#if DEBUG
            Command createJitTraceCommand = new("create-jittrace","Transform a trace file into a jittrace runtime file")
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

            createJitTraceCommand.SetHandler(context =>
            {
                FileType = PgoFileType.jittrace;
                ProcessJitEvents = true;
                ValidateOutputFile = false;
                ProcessR2REvents = context.ParseResult.GetValueForOption(_includeReadyToRun);

                if (context.ParseResult.GetValueForOption(_isSorted))
                {
                    JitTraceOptions |= JitTraceOptions.sorted;
                }

                if (context.ParseResult.GetValueForOption(_showTimestamp))
                {
                    JitTraceOptions |= JitTraceOptions.showtimestamp;
                }

                TryExecuteWithContext(context, true);
            });

            AddCommand(createJitTraceCommand);
#endif

            Command mergeCommand = new("merge", "Merge multiple Mibc profile data files into one file")
            {
                InputFilesToMerge,
                OutputFilePath,
                IncludedAssemblies,
                InheritTimestamp,
                _verbosity,
                Compressed
            };

            mergeCommand.SetHandler(context =>
            {
#if DEBUG
                ValidateOutputFile = true;
#else
                ValidateOutputFile = false;
#endif

                TryExecuteWithContext(context, true);
            });

            AddCommand(mergeCommand);

            Command dumpCommand = new("dump", "Dump the contents of a Mibc file")
            {
                _verbosity,
                InputFileToDump,
                OutputFilePath,
            };

            dumpCommand.SetHandler(context =>
            {
                DumpMibc = true;
                TryExecuteWithContext(context, true);
            });

            AddCommand(dumpCommand);

            Command compareMbicCommand = new Command("compare-mibc", "Compare two .mibc files")
            {
                InputFilesToCompare,
                DumpWorstOverlapGraphs,
                DumpWorstOverlapGraphsTo
            };

            compareMbicCommand.SetHandler(context => TryExecuteWithContext(context, false));

            AddCommand(compareMbicCommand);

            void TryExecuteWithContext(InvocationContext context, bool setVerbosity)
            {
                Result = context.ParseResult;

                if (setVerbosity)
                {
                    Verbosity verbosity = context.ParseResult.GetValueForOption(_verbosity);
                    BasicProgressMessages = (int)verbosity >= (int)Verbosity.normal;
                    Warnings = (int)verbosity >= (int)Verbosity.normal;
                    VerboseWarnings = (int)verbosity >= (int)Verbosity.detailed;
                    DetailedProgressMessages = (int)verbosity >= (int)Verbosity.detailed;
                    DisplayProcessedEvents = (int)verbosity >= (int)Verbosity.diagnostic;
                }

                try
                {
                    context.ExitCode = new Program(this).Run();
                }
                catch (Exception e)
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.Error.WriteLine("Error: " + e.Message);
                    Console.Error.WriteLine(e.ToString());

                    Console.ResetColor();

                    context.ExitCode = 1;
                }
            }
        }

        public static IEnumerable<HelpSectionDelegate> GetExtendedHelp(HelpContext context)
        {
            foreach (HelpSectionDelegate sectionDelegate in HelpBuilder.Default.GetLayout())
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
                };
            }
        }
    }
}
