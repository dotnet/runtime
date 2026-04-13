// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Diagnostics.DataContractReader.Tests.GCStress;

/// <summary>
/// Parses the cdac stress results log file written by the native cdacstress.cpp hook.
/// </summary>
internal sealed partial class CdacStressResults
{
    public int TotalVerifications { get; private set; }
    public int Passed { get; private set; }
    public int Failed { get; private set; }
    public int Skipped { get; private set; }
    public int RtDiffs { get; private set; }
    public string LogFilePath { get; private set; } = string.Empty;
    public List<string> FailureDetails { get; } = [];
    public List<string> SkipDetails { get; } = [];
    public List<FailedVerification> FailedVerifications { get; } = [];

    [GeneratedRegex(@"^\[PASS\]")]
    private static partial Regex PassPattern();

    [GeneratedRegex(@"^\[FAIL\]")]
    private static partial Regex FailPattern();

    [GeneratedRegex(@"^\[SKIP\]")]
    private static partial Regex SkipPattern();

    [GeneratedRegex(@"^Total verifications:\s*(\d+)")]
    private static partial Regex TotalPattern();

    [GeneratedRegex(@"\[RT_DIFF\]")]
    private static partial Regex RtDiffPattern();

    [GeneratedRegex(@"\[FRAME_DIFF\]\s+Source=0x(\w+)\s+\(([^)]+)\):\s+(\w+)=(\d+)\s+(\w+)=(\d+)")]
    private static partial Regex FrameDiffPattern();

    [GeneratedRegex(@"\[FRAME_(\w+)_ONLY\]\s+Source=0x(\w+)\s+\(([^)]+)\):\s+\w+=(\d+)")]
    private static partial Regex FrameOnlyPattern();

    [GeneratedRegex(@"\[(cDAC|DAC|RT)_ONLY\]\s+Addr=0x(\w+)\s+Obj=0x(\w+)\s+Flags=0x(\w+)")]
    private static partial Regex RefOnlyPattern();

    [GeneratedRegex(@"cDAC \[\d+\]:\s+Addr=0x(\w+)\s+Obj=0x(\w+)\s+Flags=0x(\w+)\s+Src=(.+)")]
    private static partial Regex CdacRefPattern();

    [GeneratedRegex(@"RT\s+\[\d+\]:\s+Addr=0x(\w+)\s+Obj=0x(\w+)\s+Flags=0x(\w+)")]
    private static partial Regex RtRefPattern();

    public static CdacStressResults Parse(string logFilePath)
    {
        if (!File.Exists(logFilePath))
            throw new FileNotFoundException($"GC stress results log not found: {logFilePath}");

        var results = new CdacStressResults { LogFilePath = logFilePath };
        FailedVerification? currentFailure = null;
        FrameDiff? currentFrame = null;

        foreach (string line in File.ReadLines(logFilePath))
        {
            string trimmed = line.Trim();

            if (PassPattern().IsMatch(trimmed))
            {
                currentFailure = null;
                currentFrame = null;
                results.Passed++;
            }
            else if (FailPattern().IsMatch(trimmed))
            {
                results.Failed++;
                results.FailureDetails.Add(trimmed);
                currentFailure = new FailedVerification { Header = trimmed };
                results.FailedVerifications.Add(currentFailure);
                currentFrame = null;
            }
            else if (SkipPattern().IsMatch(trimmed))
            {
                currentFailure = null;
                currentFrame = null;
                results.Skipped++;
                results.SkipDetails.Add(trimmed);
            }
            else if (RtDiffPattern().IsMatch(trimmed))
            {
                results.RtDiffs++;
            }
            else if (currentFailure is not null)
            {
                // Parse structured per-frame output
                Match frameDiff = FrameDiffPattern().Match(trimmed);
                if (frameDiff.Success)
                {
                    currentFrame = new FrameDiff
                    {
                        Source = ulong.Parse(frameDiff.Groups[1].Value, System.Globalization.NumberStyles.HexNumber),
                        MethodName = frameDiff.Groups[2].Value,
                        CdacCount = int.Parse(frameDiff.Groups[4].Value),
                        DacCount = int.Parse(frameDiff.Groups[6].Value),
                        Kind = FrameDiffKind.Different,
                    };
                    currentFailure.FrameDiffs.Add(currentFrame);
                    continue;
                }

                Match frameOnly = FrameOnlyPattern().Match(trimmed);
                if (frameOnly.Success)
                {
                    string ownerLabel = frameOnly.Groups[1].Value;
                    currentFrame = new FrameDiff
                    {
                        Source = ulong.Parse(frameOnly.Groups[2].Value, System.Globalization.NumberStyles.HexNumber),
                        MethodName = frameOnly.Groups[3].Value,
                        Kind = ownerLabel == "cDAC" ? FrameDiffKind.CdacOnly : FrameDiffKind.DacOnly,
                    };
                    int count = int.Parse(frameOnly.Groups[4].Value);
                    if (currentFrame.Kind == FrameDiffKind.CdacOnly)
                        currentFrame.CdacCount = count;
                    else
                        currentFrame.DacCount = count;
                    currentFailure.FrameDiffs.Add(currentFrame);
                    continue;
                }

                Match refOnly = RefOnlyPattern().Match(trimmed);
                if (refOnly.Success && currentFrame is not null)
                {
                    var r = new StackRef
                    {
                        Address = ulong.Parse(refOnly.Groups[2].Value, System.Globalization.NumberStyles.HexNumber),
                        Object = ulong.Parse(refOnly.Groups[3].Value, System.Globalization.NumberStyles.HexNumber),
                        Flags = uint.Parse(refOnly.Groups[4].Value, System.Globalization.NumberStyles.HexNumber),
                    };
                    currentFrame.UnmatchedRefs.Add(($"{refOnly.Groups[1].Value}_ONLY", r));
                    continue;
                }

                // Parse flat cDAC/RT ref lines (for cDAC-vs-RT comparison)
                Match cdacRef = CdacRefPattern().Match(trimmed);
                if (cdacRef.Success)
                {
                    currentFailure.CdacRefs.Add(new StackRef
                    {
                        Address = ulong.Parse(cdacRef.Groups[1].Value, System.Globalization.NumberStyles.HexNumber),
                        Object = ulong.Parse(cdacRef.Groups[2].Value, System.Globalization.NumberStyles.HexNumber),
                        Flags = uint.Parse(cdacRef.Groups[3].Value, System.Globalization.NumberStyles.HexNumber),
                    });
                    continue;
                }

                Match rtRef = RtRefPattern().Match(trimmed);
                if (rtRef.Success)
                {
                    currentFailure.RtRefs.Add(new StackRef
                    {
                        Address = ulong.Parse(rtRef.Groups[1].Value, System.Globalization.NumberStyles.HexNumber),
                        Object = ulong.Parse(rtRef.Groups[2].Value, System.Globalization.NumberStyles.HexNumber),
                        Flags = uint.Parse(rtRef.Groups[3].Value, System.Globalization.NumberStyles.HexNumber),
                    });
                    continue;
                }

                // Parse [STACK_TRACE] frame lines: #N MethodName (cDAC=X DAC=Y)
                if (trimmed.StartsWith("#") && trimmed.Contains("(cDAC="))
                {
                    currentFailure.StackTrace.Add(trimmed);
                }
            }

            Match totalMatch = TotalPattern().Match(trimmed);
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
        $"Total={TotalVerifications}, Passed={Passed}, Failed={Failed}, Skipped={Skipped}, RtDiffs={RtDiffs}";

    /// <summary>
    /// Formats the first N failed verifications using the structured per-frame data
    /// logged by the native code. No re-analysis needed — just presents what was logged.
    /// </summary>
    public string AnalyzeFailures(int maxFailures = 3)
    {
        var sb = new System.Text.StringBuilder();

        foreach (var failure in FailedVerifications.Take(maxFailures))
        {
            sb.AppendLine(failure.Header);

            if (failure.FrameDiffs.Count > 0)
            {
                sb.AppendLine("  Per-frame diff (cDAC vs DAC):");
                foreach (var frame in failure.FrameDiffs)
                {
                    string kindLabel = frame.Kind switch
                    {
                        FrameDiffKind.Different => $"cDAC={frame.CdacCount} DAC={frame.DacCount}",
                        FrameDiffKind.CdacOnly => $"cDAC={frame.CdacCount} (cDAC-only frame)",
                        FrameDiffKind.DacOnly => $"DAC={frame.DacCount} (DAC-only frame)",
                        _ => "unknown",
                    };
                    sb.AppendLine($"    {frame.MethodName}: {kindLabel}");
                    foreach (var (label, r) in frame.UnmatchedRefs)
                        sb.AppendLine($"      [{label}] Addr=0x{r.Address:X} Obj=0x{r.Object:X} Flags=0x{r.Flags:X}");
                }
            }

            if (failure.CdacRefs.Count > 0 || failure.RtRefs.Count > 0)
            {
                sb.AppendLine($"  cDAC vs RT: cDAC={failure.CdacRefs.Count} RT={failure.RtRefs.Count}");
            }

            if (failure.StackTrace.Count > 0)
            {
                sb.AppendLine("  Stack trace:");
                foreach (string frame in failure.StackTrace)
                    sb.AppendLine($"    {frame}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}

internal struct StackRef
{
    public ulong Address;
    public ulong Object;
    public uint Flags;
}

internal enum FrameDiffKind
{
    Different,
    CdacOnly,
    DacOnly,
}

internal sealed class FrameDiff
{
    public ulong Source { get; set; }
    public string MethodName { get; set; } = "";
    public int CdacCount { get; set; }
    public int DacCount { get; set; }
    public FrameDiffKind Kind { get; set; }
    public List<(string Label, StackRef Ref)> UnmatchedRefs { get; } = [];
}

internal sealed class FailedVerification
{
    public string Header { get; set; } = "";
    public List<FrameDiff> FrameDiffs { get; } = [];
    public List<StackRef> CdacRefs { get; } = [];
    public List<StackRef> RtRefs { get; } = [];
    public List<string> StackTrace { get; } = [];
}
