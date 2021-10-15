// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;

namespace ILTrim
{
    public class Program
    {
        static void Main(string[] args)
        {
            var inputPath = args[0];
            int i = 1;
            List<string> referencePaths = new();
            List<string> trimPaths = new();
            string outputDir = null;
            var logStrategy = LogStrategy.None;
            string logFile = null;
            while (args.Length > i) {
                if (args[i] == "-r")
                {
                    referencePaths.Add(args[i + 1]);
                    i += 2;
                }
                else if (args[i] == "-t")
                {
                    trimPaths.Add(args[i + 1]);
                    i += 2;
                }
                else if (args[i] == "-o")
                {
                    outputDir = args[i + 1];
                    i += 2;
                }
                else if (args[i] == "-l")
                {
                    logStrategy = Enum.Parse<LogStrategy>(args[i + 1]);
                    if (logStrategy == LogStrategy.FirstMark || logStrategy == LogStrategy.FullGraph) {
                        logFile = args[i + 2];
                        i += 1;
                    }
                    i += 2;
                }
                else
                {
                    throw new ArgumentException("Invalid argument");
                }
            }
            outputDir ??= Directory.GetCurrentDirectory();
            var settings = new TrimmerSettings(LogStrategy: logStrategy, LogFile: logFile);
            Trimmer.TrimAssembly(inputPath, trimPaths, outputDir, referencePaths, settings);
        }
    }
}
