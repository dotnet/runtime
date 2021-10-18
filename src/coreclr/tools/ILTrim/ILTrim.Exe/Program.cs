// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

using Internal.CommandLine;

namespace ILTrim
{
    public class Program
    {
        static void Main(string[] args)
        {
            string input = null;
            IReadOnlyList<string> references = null;
            IReadOnlyList<string> trimAssemblies = null;
            string outputPath = null;
            LogStrategy logStrategy = LogStrategy.None;
            string logFile = null;
            int? parallelism = null;
            bool libraryMode = false;
            IReadOnlyList<KeyValuePair<string, bool>> featureSwitches = null;

            ArgumentSyntax argSyntax = ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.ApplicationName = typeof(Program).Assembly.GetName().Name;

                syntax.DefineOptionList("r|reference", ref references, requireValue: false, "Reference assemblies");
                syntax.DefineOptionList("t|trim", ref trimAssemblies, requireValue: false, "Trim assemblies");
                syntax.DefineOption("o|out", ref outputPath, requireValue: false, "Output path");

                string logStrategyName = null;
                syntax.DefineOption("l|log", ref logStrategyName, requireValue: false, "Logging strategy");
                syntax.DefineOption("logFile", ref logFile, requireValue: false, "Path to the log file");
                if (logStrategyName != null)
                {
                    if (!Enum.TryParse<LogStrategy>(logStrategyName, out logStrategy))
                    {
                        throw new CommandLineException("Unknown log strategy");
                    }

                    if (logStrategy == LogStrategy.FullGraph || logStrategy == LogStrategy.FirstMark)
                    {
                        if (logFile == null)
                            throw new CommandLineException("Specified log strategy requires a logFile option");
                    }
                    else
                    {
                        if (logFile != null)
                            throw new CommandLineException("Specified log strategy can't use logFile option");
                    }
                }
                else if (logFile != null)
                {
                    throw new CommandLineException("Log file can only be specified with logging strategy selection.");
                }

                int p = -1;
                syntax.DefineOption("parallelism", ref p, requireValue: false, "Degree of parallelism");
                parallelism = p == -1 ? null : p;

                syntax.DefineOption("library", ref libraryMode, "Use library mode for the input assembly");

                syntax.DefineOptionList<KeyValuePair<string, bool>>("feature", ref featureSwitches, requireValue: false, help: "Feature switch", valueConverter: (value) =>
                {
                    int sep = value.IndexOf('=');
                    if (sep == -1)
                        throw new CommandLineException("The format of --feature value is <featureswitch>=<value>");

                    string fsName = value.Substring(0, sep);
                    string fsValue = value.Substring(sep + 1);
                    return new KeyValuePair<string, bool>(fsName, bool.Parse(fsValue));
                });

                syntax.DefineParameter("input", ref input, "The input assembly");
            });

            if (input == null)
                throw new CommandLineException("Input assembly is required");

            Dictionary<string, bool> featureSwitchesDictionary = new(featureSwitches);
            var settings = new TrimmerSettings(
                MaxDegreeOfParallelism: parallelism,
                LogStrategy: logStrategy,
                LogFile: logFile,
                LibraryMode: libraryMode,
                FeatureSwitches: featureSwitchesDictionary);
            Trimmer.TrimAssembly(
                input.Trim(),
                trimAssemblies,
                outputPath ?? Directory.GetCurrentDirectory(),
                references,
                settings);
        }
    }
}
