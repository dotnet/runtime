// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

namespace ILCompiler
{
    internal sealed class ILTrimRootCommand : RootCommand
    {
        public Argument<string> InputPath { get; } =
            new("input") { Description = "The input assembly" };
        public Option<string[]> References { get; } =
            new("--reference", "-r") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Reference assemblies" };
        public Option<string[]> TrimAssemblies { get; } =
            new("--trim", "-t") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Trim assemblies" };
        public Option<string> OutputPath { get; } =
            new("--out", "-o") { Description = "Output path" };
        public Option<string> LogStrategy { get; } =
            new("--log", "-l") { Description = "Logging strategy (None, FullGraph, FirstMark)" };
        public Option<string> LogFile { get; } =
            new("--logFile") { Description = "Path to the log file" };
        public Option<int> Parallelism { get; } =
            new("--parallelism") { DefaultValueFactory = _ => -1, Description = "Degree of parallelism" };
        public Option<bool> LibraryMode { get; } =
            new("--library") { Description = "Use library mode for the input assembly" };
        public Option<string[]> FeatureSwitches { get; } =
            new("--feature") { DefaultValueFactory = _ => Array.Empty<string>(), Description = "Feature switch in the format <name>=<value>" };

        public ILTrimRootCommand() : base("ILTrim - IL-level assembly trimmer")
        {
            Arguments.Add(InputPath);
            Options.Add(References);
            Options.Add(TrimAssemblies);
            Options.Add(OutputPath);
            Options.Add(LogStrategy);
            Options.Add(LogFile);
            Options.Add(Parallelism);
            Options.Add(LibraryMode);
            Options.Add(FeatureSwitches);

            this.SetAction(result =>
            {
                string input = result.GetValue(InputPath);
                if (string.IsNullOrEmpty(input))
                {
                    Console.Error.WriteLine("Input assembly is required");
                    return 1;
                }

                global::ILCompiler.LogStrategy logStrategy = global::ILCompiler.LogStrategy.None;
                string logStrategyName = result.GetValue(LogStrategy);
                string logFile = result.GetValue(LogFile);
                if (logStrategyName != null)
                {
                    if (!Enum.TryParse(logStrategyName, out logStrategy))
                    {
                        Console.Error.WriteLine("Unknown log strategy");
                        return 1;
                    }

                    if (logStrategy == global::ILCompiler.LogStrategy.FullGraph || logStrategy == global::ILCompiler.LogStrategy.FirstMark)
                    {
                        if (logFile == null)
                        {
                            Console.Error.WriteLine("Specified log strategy requires a logFile option");
                            return 1;
                        }
                    }
                    else if (logFile != null)
                    {
                        Console.Error.WriteLine("Specified log strategy can't use logFile option");
                        return 1;
                    }
                }
                else if (logFile != null)
                {
                    Console.Error.WriteLine("Log file can only be specified with logging strategy selection.");
                    return 1;
                }

                int p = result.GetValue(Parallelism);
                int? parallelism = p == -1 ? null : p;

                string[] featureSwitchArgs = result.GetValue(FeatureSwitches);
                var featureSwitchesDictionary = new Dictionary<string, bool>();
                foreach (string value in featureSwitchArgs)
                {
                    int sep = value.IndexOf('=');
                    if (sep == -1)
                    {
                        Console.Error.WriteLine("The format of --feature value is <featureswitch>=<value>");
                        return 1;
                    }

                    string fsName = value.Substring(0, sep);
                    string fsValue = value.Substring(sep + 1);
                    featureSwitchesDictionary[fsName] = bool.Parse(fsValue);
                }

                var settings = new TrimmerSettings(
                    MaxDegreeOfParallelism: parallelism,
                    LogStrategy: logStrategy,
                    LogFile: logFile,
                    LibraryMode: result.GetValue(LibraryMode),
                    FeatureSwitches: featureSwitchesDictionary);

                string[] references = result.GetValue(References);
                string[] trimAssemblies = result.GetValue(TrimAssemblies);

                Trimmer.TrimAssembly(
                    input.Trim(),
                    trimAssemblies,
                    result.GetValue(OutputPath) ?? Directory.GetCurrentDirectory(),
                    references,
                    settings);

                return 0;
            });
        }
    }

    public class Program
    {
        private static int Main(string[] args) =>
            new ILTrimRootCommand()
                .Parse(args)
                .Invoke();
    }
}
