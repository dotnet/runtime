// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace R2RTest
{
    public class Buckets
    {
        private Dictionary<string, List<ProcessInfo>> _bucketMap;

        public Buckets()
        {
            _bucketMap = new Dictionary<string, List<ProcessInfo>>(StringComparer.OrdinalIgnoreCase);
        }

        public void AddCompilation(ProcessInfo process) => Add(AnalyzeCompilationFailure(process), process);
        public void AddExecution(ProcessInfo process) => Add(AnalyzeExecutionFailure(process), process);

        public void Add(string bucket, ProcessInfo process)
        {
            List<ProcessInfo> processes;
            if (!_bucketMap.TryGetValue(bucket, out processes))
            {
                processes = new List<ProcessInfo>();
                _bucketMap.Add(bucket, processes);
            }
            processes.Add(process);
        }

        public void WriteToFile(string outputFile, bool detailed)
        {
            using (StreamWriter outputStream = new StreamWriter(outputFile))
            {
                WriteToStream(outputStream, detailed);
            }
        }

        public void WriteToStream(StreamWriter output, bool detailed)
        {
            output.WriteLine($@"#buckets: {_bucketMap.Count}, #failures: {_bucketMap.Sum(b => b.Value.Count)}");

            if (_bucketMap.Count == 0)
            {
                // No bucketing info to display
                return;
            }

            IEnumerable<KeyValuePair<string, List<ProcessInfo>>> orderedBuckets = _bucketMap.OrderByDescending(bucket => bucket.Value.Count);
            foreach (KeyValuePair<string, List<ProcessInfo>> bucketKvp in orderedBuckets)
            {
                bucketKvp.Value.Sort((a, b) => a.Parameters.OutputFileName.CompareTo(b.Parameters.OutputFileName));
                output.WriteLine($@"    [{bucketKvp.Value.Count} failures] {bucketKvp.Key}");
            }

            output.WriteLine();
            output.WriteLine("Detailed bucket info:");

            foreach (KeyValuePair<string, List<ProcessInfo>> bucketKvp in orderedBuckets)
            {
                output.WriteLine("");
                output.WriteLine($@"Bucket name: {bucketKvp.Key}");
                output.WriteLine($@"Failing tests ({bucketKvp.Value.Count} total):");

                foreach (ProcessInfo failure in bucketKvp.Value)
                {
                    output.WriteLine($@"   {failure.Parameters.OutputFileName}");
                }

                if (detailed)
                {
                    output.WriteLine();
                    output.WriteLine($@"Detailed test failures:");

                    foreach (ProcessInfo failure in bucketKvp.Value)
                    {
                        output.WriteLine($@"Test: {failure.Parameters.OutputFileName}");
                        try
                        {
                            output.WriteLine(File.ReadAllText(failure.Parameters.LogPath));
                        }
                        catch (Exception ex)
                        {
                            output.WriteLine($"Error reading file {failure.Parameters.LogPath}: {ex.Message}");
                        }
                        output.WriteLine();
                    }
                }
            }
        }

        private static string AnalyzeCompilationFailure(ProcessInfo process)
        {
            try
            {
                if (process.TimedOut)
                {
                    return "Timed out";
                }

                string[] lines = File.ReadAllLines(process.Parameters.LogPath);

                for (int lineIndex = 2; lineIndex < lines.Length; lineIndex++)
                {
                    string line = lines[lineIndex];
                    if (line.Length == 0 ||
                        line.StartsWith("EXEC : warning") ||
                        line.StartsWith("To repro,") ||
                        line.StartsWith("Emitting R2R PE file") ||
                        line.StartsWith("Moving R2R PE file") ||
                        line.StartsWith("Warning: ") ||
                        line.StartsWith("Info: ") ||
                        line == "Assertion Failed")
                    {
                        continue;
                    }
                    return line;
                }
                return string.Join("; ", lines);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string AnalyzeExecutionFailure(ProcessInfo process)
        {
            try
            {
                if (process.TimedOut)
                {
                    return "Timed out";
                }

                string[] lines = File.ReadAllLines(process.Parameters.LogPath);

                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    string line = lines[lineIndex];
                    if (line.StartsWith("Assert failure"))
                    {
                        int openParen = line.IndexOf('(');
                        int closeParen = line.IndexOf(')', openParen + 1);
                        if (openParen > 0 && closeParen > openParen)
                        {
                            line = line.Substring(0, openParen) + line.Substring(closeParen + 1);
                        }
                        return line;
                    }
                    else if (line.StartsWith("Unhandled exception", StringComparison.OrdinalIgnoreCase))
                    {
                        int leftBracket = line.IndexOf('[');
                        int rightBracket = line.IndexOf(']', leftBracket + 1);
                        if (leftBracket >= 0 && rightBracket > leftBracket)
                        {
                            line = line.Substring(0, leftBracket) + line.Substring(rightBracket + 1);
                        }
                        for (int detailLineIndex = lineIndex + 1; detailLineIndex < lines.Length; detailLineIndex++)
                        {
                            string detailLine = lines[detailLineIndex].TrimStart();
                            if (!detailLine.StartsWith("--->"))
                            {
                                break;
                            }
                            line += " " + detailLine;
                        }
                        return line;
                    }
                    else if (line.StartsWith("Fatal error", StringComparison.OrdinalIgnoreCase))
                    {
                        if (lineIndex + 1 < lines.Length && lines[lineIndex + 1].TrimStart().StartsWith("at "))
                        {
                            line += lines[lineIndex + 1];
                        }
                        return line;
                    }
                }

                return $"Exit code: {process.ExitCode} = 0x{process.ExitCode:X8}, expected {process.Parameters.ExpectedExitCode}";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
