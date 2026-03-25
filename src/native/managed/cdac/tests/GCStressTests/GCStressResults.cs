// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Diagnostics.DataContractReader.Tests.GCStress;

/// <summary>
/// Parses the cdac-gcstress results log file written by the native cdacgcstress.cpp hook.
/// </summary>
internal sealed partial class GCStressResults
{
    public int TotalVerifications { get; private set; }
    public int Passed { get; private set; }
    public int Failed { get; private set; }
    public int Skipped { get; private set; }
    public string LogFilePath { get; private set; } = "";
    public List<string> FailureDetails { get; } = [];
    public List<string> SkipDetails { get; } = [];

    [GeneratedRegex(@"^\[PASS\]")]
    private static partial Regex PassPattern();

    [GeneratedRegex(@"^\[FAIL\]")]
    private static partial Regex FailPattern();

    [GeneratedRegex(@"^\[SKIP\]")]
    private static partial Regex SkipPattern();

    [GeneratedRegex(@"^Total verifications:\s*(\d+)")]
    private static partial Regex TotalPattern();

    public static GCStressResults Parse(string logFilePath)
    {
        if (!File.Exists(logFilePath))
            throw new FileNotFoundException($"GC stress results log not found: {logFilePath}");

        var results = new GCStressResults { LogFilePath = logFilePath };

        foreach (string line in File.ReadLines(logFilePath))
        {
            if (PassPattern().IsMatch(line))
            {
                results.Passed++;
            }
            else if (FailPattern().IsMatch(line))
            {
                results.Failed++;
                results.FailureDetails.Add(line);
            }
            else if (SkipPattern().IsMatch(line))
            {
                results.Skipped++;
                results.SkipDetails.Add(line);
            }

            Match totalMatch = TotalPattern().Match(line);
            if (totalMatch.Success)
            {
                results.TotalVerifications = int.Parse(totalMatch.Groups[1].Value);
            }
        }

        if (results.TotalVerifications == 0)
        {
            results.TotalVerifications = results.Passed + results.Failed + results.Skipped;
        }

        return results;
    }

    public override string ToString() =>
        $"Total={TotalVerifications}, Passed={Passed}, Failed={Failed}, Skipped={Skipped}";
}
