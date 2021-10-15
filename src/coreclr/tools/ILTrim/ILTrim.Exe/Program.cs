// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

namespace ILTrim
{
    public class Program
    {
        static void Main(string[] args)
        {
            RootCommand root = new RootCommand()
            {
                new Argument<string>("input"),
                new Option<string[]>(new [] { "--reference", "-r" }, "Reference assembly"),
                new Option<string[]>(new [] { "--trim", "-t"}, "Trim assembly"),
                new Option<string>(new [] { "--output", "-o" }, "Output path"),
                new Option(new [] { "--log", "-l" }, "Log strategy", typeof(string[]), arity: new ArgumentArity(0, 2)),
                new Option<int?>("--parallelism", "Degree of parallelism")
            };
            root.Handler = CommandHandler.Create(Run);

            root.Invoke(args);
        }

        static void Run(string input, string[] reference, string[] trim, string output, string[] log, int? parallelism)
        {
            LogStrategy logStrategy = LogStrategy.None;
            string logFile = null;
            if (log is { Length: > 0 })
            {
                if (!Enum.TryParse<LogStrategy>(log[0], out logStrategy))
                {
                    throw new ArgumentException("Invalid log strategy");
                }

                if (logStrategy == LogStrategy.FullGraph || logStrategy == LogStrategy.FirstMark)
                {
                    if (log.Length != 2)
                        throw new ArgumentException("Specified log strategy requires a file path parameter");

                    logFile = log[1];
                }
                else
                {
                    if (log.Length != 1)
                        throw new ArgumentException("Specified log strategy doesn't need value");
                }
            }

            var settings = new TrimmerSettings(
                MaxDegreeOfParallelism: parallelism,
                LogStrategy: logStrategy,
                LogFile: logFile);
            Trimmer.TrimAssembly(
                input.Trim(),
                trim.Select(p => p.Trim()).ToList(),
                output?.Trim() ?? Directory.GetCurrentDirectory(),
                reference.Select(p => p.Trim()).ToList(),
                settings);
        }
    }
}
