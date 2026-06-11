// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Diagnostics.DataContractReader.Tests.GCStress;

/// <summary>
/// Parses the cdac stress results log file written by the native cdacstress.cpp hook.
/// </summary>
/// <remarks>
/// Native emission format (see <c>src/coreclr/vm/cdacstress.cpp</c>):
/// <list type="bullet">
///   <item><description><c>[PASS] Thread=... IP=... cDAC=N RT=N frames=N</c></description></item>
///   <item><description><c>[FAIL]</c> or <c>[KNOWN_ISSUE] Thread=... IP=... cDAC=N RT=N frames=N (match=N mismatch=N known_nie=N)</c>, followed by:</description></item>
///   <item><description><c>  Frame #N &lt;method&gt; [MISMATCH|KNOWN_NIE] cDAC=N RT=N SP_cDAC=0x... SP_RT=0x... [&lt;-- SP MISMATCH] [(truncated)]</c></description></item>
///   <item><description><c>      [MATCHED|ONLY|NIE(cDAC|RT)] Addr=0x... Obj=0x... Flags=0x... Reg=N Off=N</c> (concise) or with extra HasReg/Reg-name/SP fields (verbose)</description></item>
///   <item><description><c>  [STACK_TRACE] (cDAC=N RT=N frames=N)</c> followed by <c>    #N &lt;method&gt; (cDAC=N RT=N)[ &lt;-- MISMATCH|KNOWN_NIE ...]</c></description></item>
///   <item><description><c>Total verifications: N</c> (final summary)</description></item>
/// </list>
/// </remarks>
internal sealed partial class CdacStressResults
{
    public int TotalVerifications { get; private set; }
    public int Passed { get; private set; }
    public int Failed { get; private set; }
    public int KnownIssues { get; private set; }
    public string LogFilePath { get; private set; } = string.Empty;
    public List<string> FailureDetails { get; } = [];
    public List<FailedVerification> FailedVerifications { get; } = [];

    [GeneratedRegex(@"^\[PASS\]")]
    private static partial Regex PassPattern();

    [GeneratedRegex(@"^\[FAIL\]")]
    private static partial Regex FailPattern();

    [GeneratedRegex(@"^\[KNOWN_ISSUE\]")]
    private static partial Regex KnownIssuePattern();

    [GeneratedRegex(@"^Total verifications:\s*(\d+)")]
    private static partial Regex TotalPattern();

    // "Frame #3 SomeMethod [MISMATCH] cDAC=4 RT=3 SP_cDAC=0x7ff... SP_RT=0x7ff... <-- SP MISMATCH (truncated)"
    // The method name is non-greedy and bounded by " [MISMATCH]" or " [KNOWN_NIE]" so embedded brackets in the name
    // (unlikely but possible for generics) don't confuse the match.
    [GeneratedRegex(@"^Frame\s+#(\d+)\s+(.+?)\s+\[(MISMATCH|KNOWN_NIE)\]\s+cDAC=(\d+)\s+RT=(\d+)\s+SP_cDAC=0x([0-9a-fA-F]+)\s+SP_RT=0x([0-9a-fA-F]+)(.*)$")]
    private static partial Regex FrameHeaderPattern();

    // "[MATCHED(cDAC)] Addr=0x... Obj=0x... Flags=0x...[ Reg=N Off=N | HasReg=Y Reg=name(N) Off=N SP=0x...]"
    // Concise and verbose lines share the prefix; we only capture what AnalyzeFailures needs.
    [GeneratedRegex(@"^\[(MATCHED|ONLY|NIE)\((cDAC|RT)\)\]\s+Addr=0x([0-9a-fA-F]+)\s+Obj=0x([0-9a-fA-F]+)\s+Flags=(\S+)")]
    private static partial Regex RefPattern();

    // "[STACK_TRACE] (cDAC=N RT=N frames=N)" -- section opener; we only use it as a state hint.
    [GeneratedRegex(@"^\[STACK_TRACE\]")]
    private static partial Regex StackTraceHeaderPattern();

    // "#N <method> (cDAC=N RT=N)[ <-- MISMATCH | <-- KNOWN_NIE (...)]"
    [GeneratedRegex(@"^#\d+\s+.+?\s+\(cDAC=\d+\s+RT=\d+\)")]
    private static partial Regex StackTraceLinePattern();

    public static CdacStressResults Parse(string logFilePath)
    {
        if (!File.Exists(logFilePath))
            throw new FileNotFoundException($"GC stress results log not found: {logFilePath}");

        var results = new CdacStressResults { LogFilePath = logFilePath };
        FailedVerification? currentFailure = null;
        FrameDiff? currentFrame = null;
        bool inStackTrace = false;

        foreach (string line in File.ReadLines(logFilePath))
        {
            string trimmed = line.Trim();

            if (PassPattern().IsMatch(trimmed))
            {
                currentFailure = null;
                currentFrame = null;
                inStackTrace = false;
                results.Passed++;
                continue;
            }

            if (FailPattern().IsMatch(trimmed) || KnownIssuePattern().IsMatch(trimmed))
            {
                bool isKnown = KnownIssuePattern().IsMatch(trimmed);
                if (isKnown)
                    results.KnownIssues++;
                else
                    results.Failed++;
                results.FailureDetails.Add(trimmed);
                currentFailure = new FailedVerification { Header = trimmed, IsKnownIssue = isKnown };
                results.FailedVerifications.Add(currentFailure);
                currentFrame = null;
                inStackTrace = false;
                continue;
            }

            Match totalMatch = TotalPattern().Match(trimmed);
            if (totalMatch.Success)
            {
                results.TotalVerifications = int.Parse(totalMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                continue;
            }

            if (currentFailure is null)
                continue;

            if (StackTraceHeaderPattern().IsMatch(trimmed))
            {
                inStackTrace = true;
                currentFrame = null;
                continue;
            }

            if (inStackTrace)
            {
                if (StackTraceLinePattern().IsMatch(trimmed))
                    currentFailure.StackTrace.Add(trimmed);
                continue;
            }

            Match frameMatch = FrameHeaderPattern().Match(trimmed);
            if (frameMatch.Success)
            {
                currentFrame = new FrameDiff
                {
                    Index = int.Parse(frameMatch.Groups[1].Value, CultureInfo.InvariantCulture),
                    MethodName = frameMatch.Groups[2].Value,
                    Outcome = frameMatch.Groups[3].Value == "MISMATCH" ? FrameOutcome.Mismatch : FrameOutcome.KnownNie,
                    CdacCount = int.Parse(frameMatch.Groups[4].Value, CultureInfo.InvariantCulture),
                    RtCount = int.Parse(frameMatch.Groups[5].Value, CultureInfo.InvariantCulture),
                    SpCdac = ulong.Parse(frameMatch.Groups[6].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    SpRt = ulong.Parse(frameMatch.Groups[7].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    SpMismatch = frameMatch.Groups[8].Value.Contains("SP MISMATCH"),
                    Truncated = frameMatch.Groups[8].Value.Contains("(truncated)"),
                };
                currentFailure.FrameDiffs.Add(currentFrame);
                continue;
            }

            Match refMatch = RefPattern().Match(trimmed);
            if (refMatch.Success && currentFrame is not null)
            {
                var stackRef = new StackRef
                {
                    Disposition = ParseDisposition(refMatch.Groups[1].Value),
                    Side = refMatch.Groups[2].Value == "cDAC" ? RefSide.Cdac : RefSide.Rt,
                    Address = ulong.Parse(refMatch.Groups[3].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    Object = ulong.Parse(refMatch.Groups[4].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    Flags = refMatch.Groups[5].Value,
                };
                currentFrame.Refs.Add(stackRef);
            }
        }

        if (results.TotalVerifications == 0)
        {
            results.TotalVerifications = results.Passed + results.Failed + results.KnownIssues;
        }

        return results;
    }

    private static RefDisposition ParseDisposition(string value) => value switch
    {
        "MATCHED" => RefDisposition.Matched,
        "ONLY" => RefDisposition.Only,
        "NIE" => RefDisposition.Nie,
        _ => RefDisposition.Unknown,
    };

    public override string ToString() =>
        $"Total={TotalVerifications}, Passed={Passed}, Failed={Failed}, KnownIssues={KnownIssues}";

    /// <summary>
    /// Formats the first N failed verifications using the structured per-frame data
    /// logged by the native code. No re-analysis needed -- just presents what was logged.
    /// </summary>
    public string AnalyzeFailures(int maxFailures = 3)
    {
        var sb = new StringBuilder();

        foreach (FailedVerification failure in FailedVerifications.Take(maxFailures))
        {
            sb.AppendLine(failure.Header);

            foreach (FrameDiff frame in failure.FrameDiffs)
            {
                string outcomeLabel = frame.Outcome == FrameOutcome.Mismatch ? "MISMATCH" : "KNOWN_NIE";
                string suffix = (frame.SpMismatch ? " <-- SP MISMATCH" : string.Empty)
                              + (frame.Truncated ? " (truncated)" : string.Empty);
                sb.AppendLine(
                    $"  Frame #{frame.Index} {frame.MethodName} [{outcomeLabel}] cDAC={frame.CdacCount} RT={frame.RtCount}" +
                    $" SP_cDAC=0x{frame.SpCdac:X} SP_RT=0x{frame.SpRt:X}{suffix}");

                // Only divergent refs (ONLY / NIE) are interesting in the summary. MATCHED refs
                // are logged in verbose mode only and would just add noise here.
                foreach (StackRef r in frame.Refs.Where(static x => x.Disposition != RefDisposition.Matched))
                {
                    sb.AppendLine(
                        $"      [{DispositionLabel(r.Disposition)}({SideLabel(r.Side)})] " +
                        $"Addr=0x{r.Address:X} Obj=0x{r.Object:X} Flags={r.Flags}");
                }
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

    private static string DispositionLabel(RefDisposition d) => d switch
    {
        RefDisposition.Matched => "MATCHED",
        RefDisposition.Only => "ONLY",
        RefDisposition.Nie => "NIE",
        _ => "?",
    };

    private static string SideLabel(RefSide s) => s == RefSide.Cdac ? "cDAC" : "RT";
}

internal enum RefDisposition
{
    Unknown,
    Matched,
    Only,
    Nie,
}

internal enum RefSide
{
    Cdac,
    Rt,
}

internal enum FrameOutcome
{
    Mismatch,
    KnownNie,
}

internal struct StackRef
{
    public RefDisposition Disposition;
    public RefSide Side;
    public ulong Address;
    public ulong Object;
    public string Flags;
}

internal sealed class FrameDiff
{
    public int Index { get; set; }
    public string MethodName { get; set; } = string.Empty;
    public FrameOutcome Outcome { get; set; }
    public int CdacCount { get; set; }
    public int RtCount { get; set; }
    public ulong SpCdac { get; set; }
    public ulong SpRt { get; set; }
    public bool SpMismatch { get; set; }
    public bool Truncated { get; set; }
    public List<StackRef> Refs { get; } = [];
}

internal sealed class FailedVerification
{
    public string Header { get; set; } = string.Empty;
    public bool IsKnownIssue { get; set; }
    public List<FrameDiff> FrameDiffs { get; } = [];
    public List<string> StackTrace { get; } = [];
}
