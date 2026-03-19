#!/usr/bin/env dotnet run
#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0
#:property LangVersion=preview
#:property Nullable=enable

// Get-CIStatus.cs — C# port of Get-CIStatus.ps1
// Retrieves test failures from Azure DevOps builds and Helix test runs.
//
// Usage:
//   dotnet run Get-CIStatus.cs -- -BuildId 1276327
//   dotnet run Get-CIStatus.cs -- -PRNumber 123445 -ShowLogs
//   dotnet run Get-CIStatus.cs -- -HelixJob "4b24b2c2-..." -WorkItem "System.Net.Http.Tests"
//   dotnet run Get-CIStatus.cs -- -PRNumber 12345 -Repository dotnet/aspnetcore
//   dotnet run Get-CIStatus.cs -- -ClearCache

using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

// ── CLI Argument Parsing ─────────────────────────────────────────────────────

var options = ParseArgs(args);
var cacheDir = Path.Combine(Path.GetTempPath(), "ci-analysis-cache");

if (options.ClearCache)
{
    ClearCache();
    return 0;
}

if (options.BuildId is null && options.PRNumber is null && options.HelixJob is null)
{
    PrintUsage();
    return 1;
}

// ── Global State ─────────────────────────────────────────────────────────────

using var http = new HttpClient();
http.Timeout = TimeSpan.FromSeconds(options.TimeoutSec);
http.DefaultRequestHeaders.Add("User-Agent", "Get-CIStatus/1.0");

if (!options.NoCache)
{
    Directory.CreateDirectory(cacheDir);
    CleanExpiredCache(options.CacheTTLSeconds);
}

// ── Main Execution ───────────────────────────────────────────────────────────

try
{
    if (options.HelixJob is not null)
    {
        return await RunHelixJobMode(options);
    }

    var buildIds = new List<int>();
    var knownIssuesFromBuildAnalysis = new List<KnownIssue>();
    var prChangedFiles = new List<string>();

    if (options.PRNumber is not null)
    {
        buildIds.AddRange(await GetBuildIdsFromPR(options.PRNumber.Value));
        knownIssuesFromBuildAnalysis.AddRange(await GetBuildAnalysisKnownIssues(options.PRNumber.Value));
        prChangedFiles.AddRange(await GetPRChangedFiles(options.PRNumber.Value));
    }
    else
    {
        buildIds.Add(options.BuildId!.Value);
    }

    var totalFailedJobs = 0;
    var totalLocalFailures = 0;
    var allFailuresForCorrelation = new List<FailureInfo>();

    foreach (var currentBuildId in buildIds)
    {
        WriteColor($"\n=== Azure DevOps Build {currentBuildId} ===", ConsoleColor.Yellow);
        WriteColor($"URL: https://dev.azure.com/{options.Organization}/{options.Project}/_build/results?buildId={currentBuildId}", ConsoleColor.Gray);

        var buildStatus = await GetBuildStatus(currentBuildId);
        if (buildStatus is not null)
        {
            var (statusText, statusColor) = FormatBuildStatus(buildStatus);
            WriteColor($"Status: {statusText}", statusColor);
        }

        var isInProgress = buildStatus?.Status == "inProgress";
        var timeline = await GetTimeline(currentBuildId, skipCacheWrite: isInProgress);
        if (timeline is null)
        {
            WriteColor("\nCould not fetch build timeline", ConsoleColor.Red);
            continue;
        }

        var failedJobs = GetJobsByResult(timeline, "failed");
        var canceledJobs = GetJobsByResult(timeline, "canceled");
        var localTestFailures = GetLocalTestFailures(timeline, currentBuildId);

        if (failedJobs.Count == 0 && localTestFailures.Count == 0)
        {
            if (isInProgress)
            {
                WriteColor("\nNo failures yet - build still in progress", ConsoleColor.Cyan);
            }
            else
            {
                WriteColor($"\nNo failed jobs found in build {currentBuildId}", ConsoleColor.Green);
            }
            ShowCanceledJobs(canceledJobs, 5);
            continue;
        }

        // Local test failures
        if (localTestFailures.Count > 0)
        {
            WriteColor("\n=== Local Test Failures (non-Helix) ===", ConsoleColor.Yellow);
            foreach (var failure in localTestFailures)
            {
                WriteColor($"\n--- {failure.TaskName} ---", ConsoleColor.Cyan);
                var logUrl = $"https://dev.azure.com/{options.Organization}/{options.Project}/_build/results?buildId={currentBuildId}&view=logs&j={failure.ParentJobId}";
                if (failure.TaskId is not null) logUrl += $"&t={failure.TaskId}";
                WriteColor($"  Log: {logUrl}", ConsoleColor.Gray);
                foreach (var issue in failure.Issues)
                    WriteColor($"  {issue}", ConsoleColor.Red);

                allFailuresForCorrelation.Add(new FailureInfo(failure.TaskName, "Local Test", failure.Issues, [], []));

                if (failure.LogId is not null)
                {
                    var logContent = await GetBuildLog(currentBuildId, failure.LogId.Value);
                    if (logContent is not null)
                    {
                        var buildErrors = ExtractBuildErrors(logContent);
                        if (buildErrors.Count > 0)
                            await ShowKnownIssues("", string.Join("\n", buildErrors));
                    }
                }
            }
        }

        if (failedJobs.Count == 0)
        {
            WriteColor("\n=== Summary ===", ConsoleColor.Yellow);
            WriteColor($"Local test failures: {localTestFailures.Count}", ConsoleColor.Red);
            totalLocalFailures += localTestFailures.Count;
            continue;
        }

        WriteColor($"\nFound {failedJobs.Count} failed job(s):", ConsoleColor.Red);
        ShowCanceledJobs(canceledJobs, 3);

        var processedJobs = 0;
        foreach (var job in failedJobs)
        {
            if (processedJobs >= options.MaxJobs)
            {
                WriteColor($"\n... and {failedJobs.Count - options.MaxJobs} more failed jobs (use -MaxJobs to see more)", ConsoleColor.Yellow);
                break;
            }

            try
            {
                var jobName = job.Name ?? "unknown";
                var jobId = job.Id ?? "";
                WriteColor($"\n--- {jobName} ---", ConsoleColor.Cyan);
                WriteColor($"  Build: https://dev.azure.com/{options.Organization}/{options.Project}/_build/results?buildId={currentBuildId}&view=logs&j={jobId}", ConsoleColor.Gray);

                var helixTasks = GetHelixTasks(timeline, jobId);
                if (helixTasks.Count > 0)
                {
                    foreach (var task in helixTasks)
                    {
                        var logId = task.Log?.Id;
                        if (logId is null) continue;

                        WriteColor("  Fetching Helix task log...", ConsoleColor.Gray);
                        var logContent = await GetBuildLog(currentBuildId, logId.Value);
                        if (logContent is null) continue;

                        var failures = ExtractTestFailures(logContent);
                        if (failures.Count > 0)
                        {
                            WriteColor("  Failed tests:", ConsoleColor.Red);
                            foreach (var f in failures)
                                WriteColor($"    - {f}", ConsoleColor.White);

                            allFailuresForCorrelation.Add(new FailureInfo(
                                task.Name ?? "", jobName, [], [], failures));
                        }

                        var helixUrls = ExtractHelixConsoleUrls(logContent);
                        if (helixUrls.Count > 0 && options.ShowLogs)
                        {
                            WriteColor("\n  Helix Console Logs:", ConsoleColor.Yellow);
                            foreach (var url in helixUrls.Take(3))
                            {
                                WriteColor($"\n  {url}", ConsoleColor.Gray);
                                var workItemName = ExtractWorkItemFromUrl(url);
                                var helixLog = await CachedGet(url);
                                if (helixLog is not null)
                                {
                                    var failureInfo = FormatTestFailure(helixLog);
                                    if (failureInfo is not null)
                                    {
                                        WriteColor(failureInfo, ConsoleColor.White);
                                        await ShowKnownIssues(workItemName, failureInfo);
                                    }
                                }
                            }
                        }
                        else if (helixUrls.Count > 0)
                        {
                            WriteColor("\n  Helix logs available (use -ShowLogs to fetch):", ConsoleColor.Yellow);
                            foreach (var url in helixUrls.Take(3))
                                WriteColor($"    {url}", ConsoleColor.Gray);
                        }
                    }
                }
                else
                {
                    // No Helix — build failure
                    var buildTasks = GetFailedTasks(timeline, jobId);
                    foreach (var task in buildTasks.Take(3))
                    {
                        var taskName = task.Name ?? "unknown";
                        var taskId = task.Id ?? "";
                        WriteColor($"  Failed task: {taskName}", ConsoleColor.Red);

                        var logId = task.Log?.Id;
                        if (logId is null) continue;

                        WriteColor($"  Log: https://dev.azure.com/{options.Organization}/{options.Project}/_build/results?buildId={currentBuildId}&view=logs&j={jobId}&t={taskId}", ConsoleColor.Gray);
                        var logContent = await GetBuildLog(currentBuildId, logId.Value);
                        if (logContent is null) continue;

                        var buildErrors = ExtractBuildErrors(logContent);
                        if (buildErrors.Count > 0)
                        {
                            allFailuresForCorrelation.Add(new FailureInfo(taskName, jobName, buildErrors, [], []));

                            var helixLogUrls = ExtractHelixLogUrls(logContent);
                            if (helixLogUrls.Count > 0)
                            {
                                WriteColor($"  Helix failures ({helixLogUrls.Count}):", ConsoleColor.Red);
                                foreach (var h in helixLogUrls.Take(5))
                                {
                                    WriteColor($"    - {h.WorkItem}", ConsoleColor.White);
                                    WriteColor($"      Log: {h.Url}", ConsoleColor.Gray);
                                }
                            }
                            else
                            {
                                WriteColor("  Build errors:", ConsoleColor.Red);
                                foreach (var err in buildErrors.Take(5))
                                    WriteColor($"    {err}", ConsoleColor.White);
                            }
                            await ShowKnownIssues("", string.Join("\n", buildErrors));
                        }
                        else
                        {
                            WriteColor("  (No specific errors extracted from log)", ConsoleColor.Gray);
                        }
                    }
                }
                processedJobs++;
            }
            catch (Exception ex) when (options.ContinueOnError)
            {
                WriteColor($"  Error processing job: {ex.Message}", ConsoleColor.Yellow);
            }
        }

        totalFailedJobs += failedJobs.Count;
        totalLocalFailures += localTestFailures.Count;

        // Build summary
        var allJobs = GetAllJobs(timeline);
        var succeeded = allJobs.Count(j => j.Result == "succeeded");
        var warnings = allJobs.Count(j => j.Result == "succeededWithIssues");
        var pending = allJobs.Count(j => j.Result is null || j.State is "pending" or "inProgress");
        var canceled = allJobs.Count(j => j.Result == "canceled");
        var skipped = allJobs.Count(j => j.Result == "skipped");

        WriteColor($"\n=== Build {currentBuildId} Summary ===", ConsoleColor.Yellow);
        var parts = new List<string>();
        if (succeeded > 0) parts.Add($"{succeeded} passed");
        if (warnings > 0) parts.Add($"{warnings} passed with warnings");
        if (failedJobs.Count > 0) parts.Add($"{failedJobs.Count} failed");
        if (canceled > 0) parts.Add($"{canceled} canceled");
        if (skipped > 0) parts.Add($"{skipped} skipped");
        if (pending > 0) parts.Add($"{pending} pending");
        var summaryColor = failedJobs.Count > 0 ? ConsoleColor.Red : pending > 0 ? ConsoleColor.Cyan : ConsoleColor.Green;
        WriteColor($"Jobs: {allJobs.Count} total ({string.Join(", ", parts)})", summaryColor);
        if (localTestFailures.Count > 0)
            WriteColor($"Local test failures: {localTestFailures.Count}", ConsoleColor.Red);
        WriteColor($"Build URL: https://dev.azure.com/{options.Organization}/{options.Project}/_build/results?buildId={currentBuildId}", ConsoleColor.Cyan);
    }

    // PR correlation
    if (prChangedFiles.Count > 0 && allFailuresForCorrelation.Count > 0)
        ShowPRCorrelation(prChangedFiles, allFailuresForCorrelation);

    // Multi-build summary
    if (buildIds.Count > 1)
    {
        WriteColor("\n=== Overall Summary ===", ConsoleColor.Magenta);
        WriteColor($"Analyzed {buildIds.Count} builds", ConsoleColor.White);
        WriteColor($"Total failed jobs: {totalFailedJobs}", ConsoleColor.Red);
        if (knownIssuesFromBuildAnalysis.Count > 0)
        {
            WriteColor("\nKnown Issues (from Build Analysis):", ConsoleColor.Yellow);
            foreach (var issue in knownIssuesFromBuildAnalysis)
            {
                WriteColor($"  - #{issue.Number}: {issue.Title}", ConsoleColor.Gray);
                WriteColor($"    {issue.Url}", ConsoleColor.DarkGray);
            }
        }
    }

    // Recommendation
    WriteColor("\n=== Recommendation ===", ConsoleColor.Magenta);
    if (knownIssuesFromBuildAnalysis.Count > 0)
    {
        WriteColor("KNOWN ISSUES DETECTED", ConsoleColor.Yellow);
        WriteColor($"{knownIssuesFromBuildAnalysis.Count} tracked issue(s) found that may correlate with failures above.", ConsoleColor.White);
    }
    else if (totalFailedJobs == 0 && totalLocalFailures == 0)
    {
        WriteColor("BUILD SUCCESSFUL", ConsoleColor.Green);
        WriteColor("No failures detected.", ConsoleColor.White);
    }
    else if (prChangedFiles.Count > 0 && HasPRCorrelation(prChangedFiles, allFailuresForCorrelation))
    {
        WriteColor("LIKELY PR-RELATED", ConsoleColor.Red);
        WriteColor("Failures appear to correlate with files changed in this PR.", ConsoleColor.White);
    }
    else if (prChangedFiles.Count > 0)
    {
        WriteColor("POSSIBLY TRANSIENT", ConsoleColor.Yellow);
        WriteColor("No known issues matched, but failures don't clearly correlate with PR changes.", ConsoleColor.White);
        WriteColor("Consider:", ConsoleColor.Gray);
        WriteColor("  1. Check if same tests are failing on main branch", ConsoleColor.Gray);
        WriteColor("  2. Search for existing issues: gh issue list --label 'Known Build Error' --search '<test name>'", ConsoleColor.Gray);
    }
    else
    {
        WriteColor("REVIEW REQUIRED", ConsoleColor.Yellow);
        WriteColor("Could not automatically determine failure cause.", ConsoleColor.White);
    }

    return 0;
}
catch (Exception ex)
{
    WriteColor($"Error: {ex.Message}", ConsoleColor.Red);
    return 1;
}

// ═════════════════════════════════════════════════════════════════════════════
// Helper Methods
// ═════════════════════════════════════════════════════════════════════════════

// ── Argument Parsing ─────────────────────────────────────────────────────────

Options ParseArgs(string[] args)
{
    var opts = new Options();

    string NextValue(int index, string flag)
    {
        if (index + 1 >= args.Length)
        {
            WriteColor($"Error: {flag} requires a value", ConsoleColor.Red);
            PrintUsage();
            Environment.Exit(1);
        }
        return args[index + 1];
    }

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        switch (arg.ToLowerInvariant())
        {
            case "-buildid": opts.BuildId = int.Parse(NextValue(i++, arg)); break;
            case "-prnumber": opts.PRNumber = int.Parse(NextValue(i++, arg)); break;
            case "-helixjob": opts.HelixJob = NextValue(i++, arg); break;
            case "-workitem": opts.WorkItem = NextValue(i++, arg); break;
            case "-repository": opts.Repository = NextValue(i++, arg); break;
            case "-organization": opts.Organization = NextValue(i++, arg); break;
            case "-project": opts.Project = NextValue(i++, arg); break;
            case "-showlogs": opts.ShowLogs = true; break;
            case "-maxjobs": opts.MaxJobs = int.Parse(NextValue(i++, arg)); break;
            case "-maxfailurelines": opts.MaxFailureLines = int.Parse(NextValue(i++, arg)); break;
            case "-timeoutsec": opts.TimeoutSec = int.Parse(NextValue(i++, arg)); break;
            case "-nocache": opts.NoCache = true; break;
            case "-cachettlseconds": opts.CacheTTLSeconds = int.Parse(NextValue(i++, arg)); break;
            case "-clearcache": opts.ClearCache = true; break;
            case "-continueonerror": opts.ContinueOnError = true; break;
            case "-searchmihubot": opts.SearchMihuBot = true; break;
            case "-findbinlogs": opts.FindBinlogs = true; break;
            default:
                WriteColor($"Unknown argument: {arg}", ConsoleColor.Red);
                PrintUsage();
                Environment.Exit(1);
                break;
        }
    }
    return opts;
}

void PrintUsage()
{
    Console.WriteLine("""
        Get-CIStatus.cs — Analyze CI build failures from Azure DevOps and Helix.

        Usage:
          dotnet run Get-CIStatus.cs -- -BuildId <id> [-ShowLogs] [-MaxJobs N]
          dotnet run Get-CIStatus.cs -- -PRNumber <pr> [-ShowLogs] [-Repository owner/repo]
          dotnet run Get-CIStatus.cs -- -HelixJob <guid> [-WorkItem <name>]
          dotnet run Get-CIStatus.cs -- -ClearCache

        Options:
          -BuildId <id>          Azure DevOps build ID
          -PRNumber <pr>         GitHub PR number
          -HelixJob <guid>       Helix job ID (GUID)
          -WorkItem <name>       Helix work item name (requires -HelixJob)
          -Repository <r>        GitHub repo (default: dotnet/runtime)
          -Organization <org>    AzDO organization (default: dnceng-public)
          -ShowLogs              Fetch and display Helix console logs
          -MaxJobs <n>           Max failed jobs to show (default: 5)
          -TimeoutSec <n>        HTTP timeout in seconds (default: 30)
          -NoCache               Bypass response cache
          -ClearCache            Clear cache and exit
          -ContinueOnError       Continue on API errors
          -SearchMihuBot         Search MihuBot for related issues
          -FindBinlogs           Scan Helix work items for binlog files
        """);
}

// ── Console Output ───────────────────────────────────────────────────────────

void WriteColor(string text, ConsoleColor color)
{
    var prev = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine(text);
    Console.ForegroundColor = prev;
}

// ── Caching ──────────────────────────────────────────────────────────────────

string GetUrlHash(string url)
{
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(url));
    return Convert.ToHexStringLower(hash);
}

string? GetCachedResponse(string url)
{
    if (options.NoCache) return null;
    var file = Path.Combine(cacheDir, $"{GetUrlHash(url)}.json");
    if (!File.Exists(file)) return null;
    var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(file);
    if (age.TotalSeconds >= options.CacheTTLSeconds) return null;
    return File.ReadAllText(file);
}

void SetCachedResponse(string url, string content)
{
    if (options.NoCache) return;
    var file = Path.Combine(cacheDir, $"{GetUrlHash(url)}.json");
    var tmp = Path.Combine(cacheDir, $"{GetUrlHash(url)}.tmp.{Guid.NewGuid():N}");
    try
    {
        File.WriteAllText(tmp, content);
        File.Move(tmp, file, overwrite: true);
    }
    catch
    {
        try { File.Delete(tmp); } catch { }
    }
}

async Task<string?> CachedGet(string url, bool skipCache = false, bool skipCacheWrite = false)
{
    if (!skipCache)
    {
        var cached = GetCachedResponse(url);
        if (cached is not null) return cached;
    }

    var response = await http.GetAsync(url);
    if (!response.IsSuccessStatusCode) return null;
    var content = await response.Content.ReadAsStringAsync();

    if (!skipCache && !skipCacheWrite)
        SetCachedResponse(url, content);

    return content;
}

async Task<T?> CachedGetJson<T>(string url, JsonTypeInfo<T> typeInfo, bool skipCache = false, bool skipCacheWrite = false) where T : class
{
    var content = await CachedGet(url, skipCache, skipCacheWrite);
    if (content is null) return null;
    try { return JsonSerializer.Deserialize(content, typeInfo); }
    catch { return null; }
}

void CleanExpiredCache(int ttlSeconds)
{
    if (!Directory.Exists(cacheDir)) return;
    var cutoff = DateTime.UtcNow.AddSeconds(-ttlSeconds * 2);
    foreach (var file in Directory.GetFiles(cacheDir))
    {
        if (File.GetLastWriteTimeUtc(file) < cutoff)
        {
            try { File.Delete(file); } catch { }
        }
    }
}

void ClearCache()
{
    if (Directory.Exists(cacheDir))
    {
        var count = Directory.GetFiles(cacheDir).Length;
        Directory.Delete(cacheDir, recursive: true);
        WriteColor($"Cleared {count} cached files from {cacheDir}", ConsoleColor.Green);
    }
    else
    {
        WriteColor($"Cache directory does not exist: {cacheDir}", ConsoleColor.Yellow);
    }
}

// ── Process Execution ────────────────────────────────────────────────────────

(string stdout, string stderr, int exitCode) RunProcess(string command, string arguments)
{
    var psi = new ProcessStartInfo(command, arguments)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    using var proc = Process.Start(psi)!;
    var stdout = proc.StandardOutput.ReadToEnd();
    var stderr = proc.StandardError.ReadToEnd();
    proc.WaitForExit();
    return (stdout, stderr, proc.ExitCode);
}

// ── Validation ───────────────────────────────────────────────────────────────

void ValidateRepository(string repo)
{
    if (!Regex.IsMatch(repo, @"^[a-zA-Z0-9_.-]+/[a-zA-Z0-9_.-]+$"))
        throw new ArgumentException($"Invalid repository format '{repo}'. Expected 'owner/repo'.");
}

string SanitizeSearchTerm(string term) =>
    Regex.Replace(term, @"[^\w\s\-.:/]", "").Trim();

// ── Azure DevOps APIs ────────────────────────────────────────────────────────

async Task<List<int>> GetBuildIdsFromPR(int pr)
{
    ValidateRepository(options.Repository);
    WriteColor($"Finding builds for PR #{pr} in {options.Repository}...", ConsoleColor.Cyan);

    var (stdout, stderr, exitCode) = RunProcess("gh", $"pr checks {pr} --repo {options.Repository}");
    var combined = stdout + stderr;

    if (exitCode != 0 && !combined.Contains("buildId="))
        throw new Exception($"Failed to fetch CI status for PR #{pr} in {options.Repository}");

    var failingBuilds = new Dictionary<int, string>();
    foreach (var line in combined.Split('\n'))
    {
        var match = Regex.Match(line, @"fail.*buildId=(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var id))
        {
            var pipelineName = line.Split("\tfail")[0].Trim();
            failingBuilds.TryAdd(id, pipelineName);
        }
    }

    if (failingBuilds.Count == 0)
    {
        var anyMatch = Regex.Match(combined, @"buildId=(\d+)");
        if (anyMatch.Success && int.TryParse(anyMatch.Groups[1].Value, out var anyId))
            return [anyId];
        throw new Exception($"No CI build found for PR #{pr}");
    }

    if (failingBuilds.Count > 1)
    {
        WriteColor($"Found {failingBuilds.Count} failing builds:", ConsoleColor.Yellow);
        foreach (var (id, name) in failingBuilds)
            WriteColor($"  - Build {id} ({name})", ConsoleColor.Gray);
    }

    return [.. failingBuilds.Keys.Order()];
}

async Task<List<KnownIssue>> GetBuildAnalysisKnownIssues(int pr)
{
    var issues = new List<KnownIssue>();
    try
    {
        var (sha, _, exitCode) = RunProcess("gh", $"pr view {pr} --repo {options.Repository} --json headRefOid --jq .headRefOid");
        sha = sha.Trim();
        if (exitCode != 0 || !Regex.IsMatch(sha, @"^[a-fA-F0-9]{40}$")) return issues;

        var (json, _, exitCode2) = RunProcess("gh", $"api repos/{options.Repository}/commits/{sha}/check-runs --jq \".check_runs[] | select(.name == \\\"Build Analysis\\\") | .output\"");
        if (exitCode2 != 0 || string.IsNullOrWhiteSpace(json)) return issues;

        var output = JsonSerializer.Deserialize(json, CIStatusJsonContext.Default.CheckRunOutput);
        var text = output?.Text;
        if (text is null) return issues;

        var pattern = new Regex(@"<a href=""(https://github\.com/[^/]+/[^/]+/issues/(\d+))"">([^<]+)</a>");
        var seen = new HashSet<string>();
        foreach (Match m in pattern.Matches(text))
        {
            var num = m.Groups[2].Value;
            if (seen.Add(num))
            {
                issues.Add(new KnownIssue(num, m.Groups[3].Value, m.Groups[1].Value));
            }
        }

        if (issues.Count > 0)
        {
            WriteColor($"\nBuild Analysis found {issues.Count} known issue(s):", ConsoleColor.Yellow);
            foreach (var issue in issues)
            {
                WriteColor($"  - #{issue.Number}: {issue.Title}", ConsoleColor.Gray);
                WriteColor($"    {issue.Url}", ConsoleColor.DarkGray);
            }
        }
    }
    catch { }
    return issues;
}

async Task<List<string>> GetPRChangedFiles(int pr, int maxFiles = 100)
{
    try
    {
        var (countStr, _, ec1) = RunProcess("gh", $"pr view {pr} --repo {options.Repository} --json files --jq \".files | length\"");
        if (ec1 != 0) return [];
        if (int.TryParse(countStr.Trim(), out var count) && count > maxFiles)
        {
            WriteColor($"PR has {count} changed files - skipping detailed correlation (limit: {maxFiles})", ConsoleColor.Gray);
            return [];
        }

        var (filesStr, _, ec2) = RunProcess("gh", $"pr view {pr} --repo {options.Repository} --json files --jq \".files[].path\"");
        if (ec2 != 0) return [];
        return filesStr.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
    }
    catch { return []; }
}

async Task<BuildStatus?> GetBuildStatus(int buildId)
{
    var url = $"https://dev.azure.com/{options.Organization}/{options.Project}/_apis/build/builds/{buildId}?api-version=7.0";
    try
    {
        var cached = GetCachedResponse(url);
        if (cached is not null)
        {
            var cachedBuild = JsonSerializer.Deserialize(cached, CIStatusJsonContext.Default.BuildStatus);
            if (cachedBuild?.Status == "completed")
                return cachedBuild;
        }

        var build = await CachedGetJson(url, CIStatusJsonContext.Default.BuildStatus, skipCache: true);
        if (build is null) return null;

        if (build.Status == "completed")
            SetCachedResponse(url, JsonSerializer.Serialize(build, CIStatusJsonContext.Default.BuildStatus));

        return build;
    }
    catch { return null; }
}

async Task<AzdoTimeline?> GetTimeline(int buildId, bool skipCacheWrite = false)
{
    var url = $"https://dev.azure.com/{options.Organization}/{options.Project}/_apis/build/builds/{buildId}/timeline?api-version=7.0";
    WriteColor("Fetching build timeline...", ConsoleColor.Cyan);
    return await CachedGetJson(url, CIStatusJsonContext.Default.AzdoTimeline, skipCacheWrite: skipCacheWrite);
}

async Task<string?> GetBuildLog(int buildId, int logId)
{
    var url = $"https://dev.azure.com/{options.Organization}/{options.Project}/_apis/build/builds/{buildId}/logs/{logId}?api-version=7.0";
    return await CachedGet(url);
}

(string text, ConsoleColor color) FormatBuildStatus(BuildStatus status) => status.Status switch
{
    "inProgress" => ("IN PROGRESS - showing failures so far", ConsoleColor.Cyan),
    "completed" when status.Result == "succeeded" => ($"completed ({status.Result})", ConsoleColor.Green),
    "completed" => ($"completed ({status.Result})", ConsoleColor.Red),
    _ => (status.Status ?? "unknown", ConsoleColor.Gray),
};

// ── Timeline Queries ─────────────────────────────────────────────────────────

List<TimelineRecord> GetAllJobs(AzdoTimeline timeline) =>
    timeline.Records?
        .Where(r => r.Type == "Job")
        .ToList() ?? [];

List<TimelineRecord> GetJobsByResult(AzdoTimeline timeline, string result) =>
    timeline.Records?
        .Where(r => r.Type == "Job" && r.Result == result)
        .ToList() ?? [];

List<TimelineRecord> GetHelixTasks(AzdoTimeline timeline, string jobId) =>
    timeline.Records?
        .Where(r => r.ParentId == jobId
            && (r.Name ?? "").Contains("Helix", StringComparison.OrdinalIgnoreCase)
            && r.Result == "failed")
        .ToList() ?? [];

List<TimelineRecord> GetFailedTasks(AzdoTimeline timeline, string jobId) =>
    timeline.Records?
        .Where(r => r.ParentId == jobId && r.Result == "failed")
        .ToList() ?? [];

List<LocalTestFailure> GetLocalTestFailures(AzdoTimeline timeline, int buildId)
{
    var failures = new List<LocalTestFailure>();
    var records = timeline.Records;
    if (records is null) return failures;

    foreach (var task in records)
    {
        var issues = task.Issues;
        if (issues is null || issues.Count == 0) continue;

        var testErrors = issues
            .Where(i =>
            {
                var msg = i.Message ?? "";
                return msg.Contains("Tests failed:") || Regex.IsMatch(msg, @"error\s*:.*Test.*failed", RegexOptions.IgnoreCase);
            })
            .ToList();

        if (testErrors.Count == 0) continue;

        var parentId = task.ParentId;
        var parentJob = records.FirstOrDefault(r => r.Id == parentId && r.Type == "Job");

        failures.Add(new LocalTestFailure(
            task.Name ?? "unknown",
            task.Id,
            parentJob?.Id ?? parentId ?? "",
            task.Log?.Id,
            testErrors.Select(e => e.Message ?? "").ToList()));
    }
    return failures;
}

void ShowCanceledJobs(List<TimelineRecord> canceledJobs, int max)
{
    if (canceledJobs.Count == 0) return;
    WriteColor($"\nNote: {canceledJobs.Count} job(s) were canceled (not failed):", ConsoleColor.DarkYellow);
    foreach (var job in canceledJobs.Take(max))
        WriteColor($"  - {job.Name}", ConsoleColor.DarkGray);
    if (canceledJobs.Count > max)
        WriteColor($"  ... and {canceledJobs.Count - max} more", ConsoleColor.DarkGray);
}

// ── Helix APIs ───────────────────────────────────────────────────────────────

async Task<int> RunHelixJobMode(Options opts)
{
    var jobId = opts.HelixJob!;
    WriteColor($"\n=== Helix Job {jobId} ===", ConsoleColor.Yellow);
    WriteColor($"URL: https://helix.dot.net/api/jobs/{jobId}", ConsoleColor.Gray);

    var jobDetails = await CachedGetJson($"https://helix.dot.net/api/2019-06-17/jobs/{jobId}", CIStatusJsonContext.Default.HelixJobInfo);
    if (jobDetails is not null)
    {
        WriteColor($"\nQueue: {jobDetails.QueueId}", ConsoleColor.Cyan);
        WriteColor($"Source: {jobDetails.Source}", ConsoleColor.Cyan);
    }

    if (opts.WorkItem is not null)
    {
        var wi = Uri.EscapeDataString(opts.WorkItem);
        WriteColor($"\n--- Work Item: {opts.WorkItem} ---", ConsoleColor.Cyan);

        var details = await CachedGetJson($"https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems/{wi}", CIStatusJsonContext.Default.HelixWorkItemDetail);
        if (details is not null)
        {
            var state = details.State ?? "unknown";
            WriteColor($"  State: {state}", state == "Passed" ? ConsoleColor.Green : ConsoleColor.Red);
            WriteColor($"  Exit Code: {details.ExitCode}", ConsoleColor.White);
            WriteColor($"  Machine: {details.MachineName}", ConsoleColor.Gray);

            var files = await CachedGetJson($"https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems/{wi}/files", CIStatusJsonContext.Default.ListHelixFile);
            if (files is { Count: > 0 })
            {
                WriteColor("\n  Artifacts:", ConsoleColor.Yellow);
                foreach (var f in files.Take(15))
                {
                    var name = f.Name ?? "";
                    var link = f.Link ?? "";
                    if (name.EndsWith(".binlog", StringComparison.OrdinalIgnoreCase))
                        WriteColor($"    📋 {name}: {link}", ConsoleColor.Cyan);
                    else
                        WriteColor($"    {name}: {link}", ConsoleColor.Gray);
                }
            }

            var consoleUrl = $"https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems/{wi}/console";
            WriteColor($"\n  Console Log: {consoleUrl}", ConsoleColor.Yellow);
            var consoleLog = await CachedGet(consoleUrl);
            if (consoleLog is not null)
            {
                var failureInfo = FormatTestFailure(consoleLog);
                if (failureInfo is not null)
                {
                    WriteColor(failureInfo, ConsoleColor.White);
                    await ShowKnownIssues(opts.WorkItem, failureInfo);
                }
                else
                {
                    var lines = consoleLog.Split('\n');
                    WriteColor(string.Join("\n", lines.TakeLast(50)), ConsoleColor.White);
                }
            }
        }
    }
    else
    {
        WriteColor("\nWork Items:", ConsoleColor.Yellow);
        var workItems = await CachedGetJson($"https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems", CIStatusJsonContext.Default.ListHelixWorkItemSummary);
        if (workItems is not null)
        {
            WriteColor($"  Total: {workItems.Count}", ConsoleColor.Cyan);
            WriteColor("  Checking for failures...", ConsoleColor.Gray);

            var failedItems = new List<(string name, int exitCode, string state)>();
            foreach (var wi in workItems.Take(20))
            {
                var name = wi.Name ?? "";
                var encoded = Uri.EscapeDataString(name);
                var det = await CachedGetJson($"https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems/{encoded}", CIStatusJsonContext.Default.HelixWorkItemDetail);
                if (det is not null)
                {
                    var exitCode = det.ExitCode ?? 0;
                    if (exitCode != 0)
                        failedItems.Add((name, exitCode, det.State ?? ""));
                }
            }

            if (failedItems.Count > 0)
            {
                WriteColor($"\n  Failed Work Items:", ConsoleColor.Red);
                foreach (var (name, exitCode, _) in failedItems.Take(opts.MaxJobs))
                    WriteColor($"    - {name} (Exit: {exitCode})", ConsoleColor.White);
                WriteColor("\n  Use -WorkItem '<name>' to see details", ConsoleColor.Gray);
            }
            else
            {
                WriteColor("  No failures found in first 20 work items", ConsoleColor.Green);
            }

            WriteColor("\n  All work items:", ConsoleColor.Yellow);
            foreach (var wi in workItems.Take(10))
                WriteColor($"    - {wi.Name}", ConsoleColor.White);
            if (workItems.Count > 10)
                WriteColor($"    ... and {workItems.Count - 10} more", ConsoleColor.Gray);

            if (opts.FindBinlogs)
            {
                WriteColor("\n  === Binlog Search ===", ConsoleColor.Yellow);
                WriteColor("  Scanning work items for binlogs...", ConsoleColor.Gray);
                var binlogResults = new List<(string name, List<string> binlogs)>();
                var scanned = 0;
                foreach (var wi in workItems.Take(30))
                {
                    scanned++;
                    var name = wi.Name ?? "";
                    var encoded = Uri.EscapeDataString(name);
                    var files = await CachedGetJson($"https://helix.dot.net/api/2019-06-17/jobs/{jobId}/workitems/{encoded}/files", CIStatusJsonContext.Default.ListHelixFile);
                    if (files is not null)
                    {
                        var binlogs = files
                            .Where(f => (f.Name ?? "").EndsWith(".binlog", StringComparison.OrdinalIgnoreCase))
                            .Select(f => f.Name!)
                            .ToList();
                        if (binlogs.Count > 0)
                            binlogResults.Add((name, binlogs));
                    }
                    if (scanned % 10 == 0)
                        WriteColor($"  Scanned {scanned}/30...", ConsoleColor.DarkGray);
                }

                if (binlogResults.Count > 0)
                {
                    WriteColor($"\n  Work items with binlogs:", ConsoleColor.Cyan);
                    foreach (var (name, binlogs) in binlogResults)
                    {
                        WriteColor($"    {name}", ConsoleColor.White);
                        foreach (var b in binlogs.Take(5))
                            WriteColor($"      - {b}", ConsoleColor.Gray);
                    }
                }
                else
                {
                    WriteColor("  No binlogs found in scanned work items.", ConsoleColor.Yellow);
                }
            }
        }
    }

    return 0;
}

// ── Log Parsing ──────────────────────────────────────────────────────────────

List<string> ExtractHelixConsoleUrls(string logContent)
{
    var normalized = logContent.ReplaceLineEndings("");
    return Regex.Matches(normalized, @"https://helix\.dot\.net/api/[^/]+/jobs/[a-f0-9-]+/workitems/[^/\s]+/console")
        .Select(m => m.Value)
        .Distinct()
        .ToList();
}

List<string> ExtractTestFailures(string logContent) =>
    Regex.Matches(logContent, @"error\s*:\s*.*Test\s+(\S+)\s+has failed", RegexOptions.IgnoreCase)
        .Select(m => m.Groups[1].Value)
        .Distinct()
        .ToList();

List<string> ExtractBuildErrors(string logContent)
{
    var errors = new List<string>();
    var lines = logContent.Split('\n');

    var errorPatterns = new[]
    {
        @"error\s+CS\d+:.*",
        @"error\s+MSB\d+:.*",
        @"error\s+NU\d+:.*",
        @"\.pcm: No such file or directory",
        @"EXEC\s*:\s*error\s*:.*",
        @"fatal error:.*",
        @":\s*error:",
        @"undefined reference to",
        @"cannot find -l",
        @"collect2: error:",
        @"##\[error\].*",
    };
    var combined = string.Join("|", errorPatterns);
    var regex = new Regex(combined);

    var msbWrapperLines = new List<int>();
    var foundRealErrors = false;

    for (int i = 0; i < lines.Length; i++)
    {
        if (!regex.IsMatch(lines[i])) continue;

        if (Regex.IsMatch(lines[i], @"exited with code \d+"))
        {
            msbWrapperLines.Add(i);
            continue;
        }
        if (Regex.IsMatch(lines[i], @"error MSB3073.*exited with code")) continue;

        foundRealErrors = true;
        var clean = Regex.Replace(lines[i], @"^\d{4}-\d{2}-\d{2}T[\d:.]+Z\s*", "");
        clean = clean.Replace("##[error]", "ERROR: ").Trim();
        errors.Add(clean);
    }

    if (!foundRealErrors && msbWrapperLines.Count > 0)
    {
        var wrapperLine = msbWrapperLines[0];
        var searchStart = Math.Max(0, wrapperLine - 50);
        for (int i = searchStart; i < wrapperLine; i++)
        {
            if (Regex.IsMatch(lines[i], @":\s*error:") || lines[i].Contains("fatal error:") || lines[i].Contains("undefined reference"))
            {
                var clean = Regex.Replace(lines[i], @"^\d{4}-\d{2}-\d{2}T[\d:.]+Z\s*", "").Trim();
                errors.Add(clean);
            }
        }
    }

    return errors.Distinct().Take(20).ToList();
}

List<HelixLogUrl> ExtractHelixLogUrls(string logContent)
{
    var pattern = new Regex(@"https://helix\.dot\.net/api/[^/]+/jobs/([a-f0-9-]+)/workitems/([^/\s]+)/console");
    var seen = new HashSet<string>();
    var results = new List<HelixLogUrl>();
    foreach (Match m in pattern.Matches(logContent))
    {
        if (seen.Add(m.Value))
            results.Add(new HelixLogUrl(m.Value, m.Groups[1].Value, m.Groups[2].Value));
    }
    return results;
}

string ExtractWorkItemFromUrl(string url)
{
    var m = Regex.Match(url, @"/workitems/([^/]+)/console");
    return m.Success ? m.Groups[1].Value : "";
}

string? FormatTestFailure(string logContent, int maxLines = 50, int maxFailures = 3)
{
    var lines = logContent.Split('\n');
    var allFailures = new List<string>();
    var currentFailure = new List<string>();
    var inFailure = false;
    var emptyLineCount = 0;

    var failurePattern = new Regex(@"\[FAIL\]|Assert\.\w+\(\)\s+Failure|Expected:.*but was:|BUG:|FAILED\s*$|END EXECUTION - FAILED|System\.\w+Exception:");

    foreach (var line in lines)
    {
        if (failurePattern.IsMatch(line))
        {
            if (currentFailure.Count > 0)
            {
                allFailures.Add(string.Join("\n", currentFailure));
                if (allFailures.Count >= maxFailures) break;
            }
            currentFailure = [line];
            inFailure = true;
            emptyLineCount = 0;
            continue;
        }

        if (inFailure)
        {
            currentFailure.Add(line);
            emptyLineCount = string.IsNullOrWhiteSpace(line) ? emptyLineCount + 1 : 0;

            if (emptyLineCount >= 2 || currentFailure.Count >= maxLines)
            {
                allFailures.Add(string.Join("\n", currentFailure));
                currentFailure = [];
                inFailure = false;
                if (allFailures.Count >= maxFailures) break;
            }
        }
    }

    if (currentFailure.Count > 0 && allFailures.Count < maxFailures)
        allFailures.Add(string.Join("\n", currentFailure));

    if (allFailures.Count == 0) return null;
    var result = string.Join("\n\n--- Next Failure ---\n\n", allFailures);
    if (allFailures.Count >= maxFailures)
        result += $"\n\n... (more failures exist, showing first {maxFailures})";
    return result;
}

// ── Known Issues ─────────────────────────────────────────────────────────────

async Task ShowKnownIssues(string testName, string errorMessage)
{
    var searchTerms = ExtractSearchTerms(testName, errorMessage);
    foreach (var term in searchTerms.Take(3))
    {
        var safe = SanitizeSearchTerm(term);
        if (string.IsNullOrWhiteSpace(safe)) continue;

        var (stdout, _, exitCode) = RunProcess("gh",
            $"issue list --repo {options.Repository} --label \"Known Build Error\" --state open --search \"{safe}\" --limit 3 --json number,title,url");
        if (exitCode != 0) continue;

        try
        {
            var issues = JsonSerializer.Deserialize(stdout, CIStatusJsonContext.Default.ListGitHubIssueInfo);
            if (issues is null || issues.Count == 0) continue;

            WriteColor("\n  Known Issues:", ConsoleColor.Magenta);
            foreach (var issue in issues)
            {
                var title = issue.Title ?? "";
                if (title.Contains(safe, StringComparison.OrdinalIgnoreCase))
                {
                    WriteColor($"    #{issue.Number}: {title}", ConsoleColor.Magenta);
                    WriteColor($"    {issue.Url}", ConsoleColor.Gray);
                }
            }
            break;
        }
        catch { }
    }

    if (options.SearchMihuBot)
        await SearchMihuBot(searchTerms, testName);
}

List<string> ExtractSearchTerms(string testName, string errorMessage)
{
    var terms = new List<string>();

    // [FAIL] test name
    var failMatch = Regex.Match(errorMessage, @"(\S+)\s+\[FAIL\]");
    if (failMatch.Success)
    {
        var fullTest = failMatch.Groups[1].Value;
        var dotMatch = Regex.Match(fullTest, @"\.([^.]+)$");
        if (dotMatch.Success) terms.Add(dotMatch.Groups[1].Value);
        terms.Add(fullTest);
    }

    // Stack trace method
    if (terms.Count == 0)
    {
        var atMatch = Regex.Match(errorMessage, @"at\s+(\w+\.\w+)\(");
        if (atMatch.Success) terms.Add(atMatch.Groups[1].Value);
    }

    // Test name parts
    if (!string.IsNullOrEmpty(testName))
    {
        var methodMatch = Regex.Match(testName, @"\.([^.]+)$");
        if (methodMatch.Success)
        {
            var method = methodMatch.Groups[1].Value;
            if (method != "Tests" && method.Length > 5) terms.Add(method);
        }
        if (testName.Length < 100) terms.Add(testName);
    }

    return terms.Distinct().ToList();
}

async Task SearchMihuBot(List<string> searchTerms, string testName)
{
    if (searchTerms.Count == 0) return;
    try
    {
        var payload = new MihuBotRequest
        {
            Id = Guid.NewGuid().ToString(),
            Method = "tools/call",
            Params = new MihuBotRequestParams
            {
                Name = "search_dotnet_repos",
                Arguments = new MihuBotSearchArguments
                {
                    Repository = options.Repository,
                    SearchTerms = searchTerms,
                    ExtraSearchContext = $"test failure {testName}",
                    IncludeOpen = true,
                    IncludeClosed = true,
                    IncludeIssues = true,
                    IncludePullRequests = true,
                    IncludeComments = false,
                }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload, CIStatusJsonContext.Default.MihuBotRequest),
            Encoding.UTF8, "application/json");
        var resp = await http.PostAsync("https://mihubot.xyz/mcp", content);
        if (!resp.IsSuccessStatusCode) return;

        var response = await resp.Content.ReadFromJsonAsync(CIStatusJsonContext.Default.MihuBotResponse);
        var results = response?.Result?.Content;
        if (results is null) return;

        var found = new List<(string num, string title, string url, string state)>();
        foreach (var item in results)
        {
            if (item.Type != "text") continue;
            if (item.Text is null) continue;
            var issues = JsonSerializer.Deserialize(item.Text, CIStatusJsonContext.Default.ListMihuBotIssue);
            if (issues is null) continue;
            foreach (var issue in issues.Take(5))
            {
                found.Add((
                    issue.Number ?? "",
                    issue.Title ?? "",
                    issue.Url ?? "",
                    issue.State ?? ""));
            }
        }

        if (found.Count > 0)
        {
            WriteColor("\n  Related Issues (MihuBot):", ConsoleColor.Blue);
            foreach (var (num, title, url, state) in found.Take(5))
            {
                WriteColor($"    #{num}: {title} [{state}]", ConsoleColor.Blue);
                WriteColor($"    {url}", ConsoleColor.Gray);
            }
        }
    }
    catch { }
}

// ── PR Correlation ───────────────────────────────────────────────────────────

void ShowPRCorrelation(List<string> changedFiles, List<FailureInfo> failures)
{
    var failureText = string.Join("\n", failures.SelectMany(f =>
        new[] { f.TaskName, f.JobName }
            .Concat(f.Errors)
            .Concat(f.HelixLogs)
            .Concat(f.FailedTests)));

    var correlatedFiles = new List<string>();
    var testFiles = new List<string>();

    foreach (var file in changedFiles)
    {
        var fileName = Path.GetFileNameWithoutExtension(file);
        var fileNameWithExt = Path.GetFileName(file);
        var baseTestName = Regex.Replace(fileName, @"\.[^.]+$", "");

        var isCorrelated = failureText.Contains(fileName, StringComparison.OrdinalIgnoreCase)
            || failureText.Contains(fileNameWithExt, StringComparison.OrdinalIgnoreCase)
            || failureText.Contains(file, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrEmpty(baseTestName) && failureText.Contains(baseTestName, StringComparison.OrdinalIgnoreCase));

        if (!isCorrelated) continue;

        var isTestFile = Regex.IsMatch(file, @"\.Tests?\.|[/\\]tests?[/\\]|Test\.cs$|Tests\.cs$");
        if (isTestFile) testFiles.Add(file);
        else correlatedFiles.Add(file);
    }

    if (correlatedFiles.Count == 0 && testFiles.Count == 0) return;

    WriteColor("\n=== PR Change Correlation ===", ConsoleColor.Magenta);
    if (testFiles.Count > 0)
    {
        WriteColor("⚠️  Test files changed by this PR are failing:", ConsoleColor.Yellow);
        foreach (var f in testFiles.Take(10))
            WriteColor($"    {f}", ConsoleColor.Red);
    }
    if (correlatedFiles.Count > 0)
    {
        WriteColor("⚠️  Files changed by this PR appear in failures:", ConsoleColor.Yellow);
        foreach (var f in correlatedFiles.Take(10))
            WriteColor($"    {f}", ConsoleColor.Red);
    }
    WriteColor("\nThese failures are likely PR-related.", ConsoleColor.Yellow);
}

bool HasPRCorrelation(List<string> changedFiles, List<FailureInfo> failures)
{
    var failureText = string.Join(" ", failures.SelectMany(f => f.Errors.Concat(f.HelixLogs).Concat(f.FailedTests)));
    return changedFiles.Any(file =>
    {
        var fileName = Path.GetFileNameWithoutExtension(file);
        return failureText.Contains(fileName, StringComparison.OrdinalIgnoreCase);
    });
}

// ═════════════════════════════════════════════════════════════════════════════
// Record Types
// ═════════════════════════════════════════════════════════════════════════════

record Options
{
    public int? BuildId;
    public int? PRNumber;
    public string? HelixJob;
    public string? WorkItem;
    public string Repository = "dotnet/runtime";
    public string Organization = "dnceng-public";
    public string Project = "cbb18261-c48f-4abb-8651-8cdcb5474649";
    public bool ShowLogs;
    public int MaxJobs = 5;
    public int MaxFailureLines = 50;
    public int TimeoutSec = 30;
    public bool NoCache;
    public int CacheTTLSeconds = 30;
    public bool ClearCache;
    public bool ContinueOnError;
    public bool SearchMihuBot;
    public bool FindBinlogs;
}

record BuildStatus(string? Status, string? Result, string? StartTime, string? FinishTime);
record KnownIssue(string Number, string Title, string Url);
record HelixLogUrl(string Url, string JobId, string WorkItem);
record FailureInfo(string TaskName, string JobName, List<string> Errors, List<string> HelixLogs, List<string> FailedTests);
record LocalTestFailure(string TaskName, string? TaskId, string ParentJobId, int? LogId, List<string> Issues);

// ═════════════════════════════════════════════════════════════════════════════
// JSON API Models (for STJ source generation)
// ═════════════════════════════════════════════════════════════════════════════

// AzDO Timeline API
record AzdoTimeline
{
    public List<TimelineRecord>? Records { get; init; }
}

record TimelineRecord
{
    public string? Id { get; init; }
    public string? ParentId { get; init; }
    public string? Type { get; init; }
    public string? Name { get; init; }
    public string? Result { get; init; }
    public string? State { get; init; }
    public TimelineLog? Log { get; init; }
    public List<TimelineIssue>? Issues { get; init; }
}

record TimelineLog
{
    public int? Id { get; init; }
}

record TimelineIssue
{
    public string? Message { get; init; }
}

// Helix APIs
record HelixJobInfo
{
    public string? QueueId { get; init; }
    public string? Source { get; init; }
}

record HelixWorkItemDetail
{
    public string? State { get; init; }
    public int? ExitCode { get; init; }
    public string? MachineName { get; init; }
}

record HelixFile
{
    public string? Name { get; init; }
    public string? Link { get; init; }
}

record HelixWorkItemSummary
{
    public string? Name { get; init; }
}

// GitHub CLI output
record GitHubIssueInfo
{
    public int Number { get; init; }
    public string? Title { get; init; }
    public string? Url { get; init; }
}

// Build Analysis (from gh api)
record CheckRunOutput
{
    public string? Text { get; init; }
}

// MihuBot API
record MihuBotRequest
{
    public string Jsonrpc { get; init; } = "2.0";
    public string? Id { get; init; }
    public string? Method { get; init; }
    public MihuBotRequestParams? Params { get; init; }
}

record MihuBotRequestParams
{
    public string? Name { get; init; }
    public MihuBotSearchArguments? Arguments { get; init; }
}

record MihuBotSearchArguments
{
    public string? Repository { get; init; }
    public List<string>? SearchTerms { get; init; }
    public string? ExtraSearchContext { get; init; }
    public bool IncludeOpen { get; init; }
    public bool IncludeClosed { get; init; }
    public bool IncludeIssues { get; init; }
    public bool IncludePullRequests { get; init; }
    public bool IncludeComments { get; init; }
}

record MihuBotResponse
{
    public MihuBotResult? Result { get; init; }
}

record MihuBotResult
{
    public List<MihuBotContentItem>? Content { get; init; }
}

record MihuBotContentItem
{
    public string? Type { get; init; }
    public string? Text { get; init; }
}

record MihuBotIssue
{
    public string? Number { get; init; }
    public string? Title { get; init; }
    public string? Url { get; init; }
    public string? State { get; init; }
}

// ═════════════════════════════════════════════════════════════════════════════
// Source Generation Context
// ═════════════════════════════════════════════════════════════════════════════

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(BuildStatus))]
[JsonSerializable(typeof(AzdoTimeline))]
[JsonSerializable(typeof(HelixJobInfo))]
[JsonSerializable(typeof(HelixWorkItemDetail))]
[JsonSerializable(typeof(List<HelixFile>))]
[JsonSerializable(typeof(List<HelixWorkItemSummary>))]
[JsonSerializable(typeof(List<GitHubIssueInfo>))]
[JsonSerializable(typeof(CheckRunOutput))]
[JsonSerializable(typeof(MihuBotRequest))]
[JsonSerializable(typeof(MihuBotResponse))]
[JsonSerializable(typeof(List<MihuBotIssue>))]
partial class CIStatusJsonContext : JsonSerializerContext { }
