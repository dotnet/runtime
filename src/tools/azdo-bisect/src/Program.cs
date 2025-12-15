using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.CommandLine;
using AzdoBisect;

var rootCommand = new RootCommand("Azure DevOps bisect tool for finding test regressions");

// Global options
var orgOption = new Option<string>(
    aliases: new[] { "--organization", "-o" },
    description: "Azure DevOps organization name")
{
    IsRequired = true
};

var projectOption = new Option<string>(
    aliases: new[] { "--project", "-p" },
    description: "Azure DevOps project name")
{
    IsRequired = true
};

var patOption = new Option<string>(
    aliases: new[] { "--pat" },
    description: "Personal Access Token (or set AZDO_PAT environment variable)",
    getDefaultValue: () => Environment.GetEnvironmentVariable("AZDO_PAT") ?? "");

// Add global options to root
rootCommand.AddGlobalOption(orgOption);
rootCommand.AddGlobalOption(projectOption);
rootCommand.AddGlobalOption(patOption);

// Build command
var buildCommand = new Command("build", "Get information about a build");

var buildIdArgument = new Argument<int>(
    name: "build-id",
    description: "The build ID to fetch");

buildCommand.AddArgument(buildIdArgument);

buildCommand.SetHandler(async (org, project, pat, buildId) =>
{
    if (string.IsNullOrWhiteSpace(pat))
    {
        Console.Error.WriteLine("Error: PAT is required. Use --pat or set AZDO_PAT environment variable.");
        Environment.ExitCode = 1;
        return;
    }

    using var client = new AzDoClient(org, project, pat);
    var build = await client.GetBuildAsync(buildId);

    if (build == null)
    {
        Console.Error.WriteLine($"Build {buildId} not found.");
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine($"Build ID:       {build.Id}");
    Console.WriteLine($"Build Number:   {build.BuildNumber}");
    Console.WriteLine($"Status:         {build.Status}");
    Console.WriteLine($"Result:         {build.Result}");
    Console.WriteLine($"Source Version: {build.SourceVersion}");
    Console.WriteLine($"Source Branch:  {build.SourceBranch}");
    Console.WriteLine($"Definition:     {build.Definition?.Name} ({build.Definition?.Id})");
    Console.WriteLine($"Queue Time:     {build.QueueTime}");
    Console.WriteLine($"Start Time:     {build.StartTime}");
    Console.WriteLine($"Finish Time:    {build.FinishTime}");
    Console.WriteLine($"Web URL:        {build.Links?.Web?.Href}");

    // Fetch and display test failures if the build has completed
    if (build.Status == BuildStatus.Completed)
    {
        Console.WriteLine();
        Console.WriteLine("Fetching failed tests...");
        try
        {
            var failedTests = await client.GetFailedTestsAsync(buildId);

            Console.WriteLine($"Failed Tests:   {failedTests.Count}");

            if (failedTests.Count > 0)
            {
                Console.WriteLine();
                foreach (var test in failedTests.OrderBy(t => t.FullyQualifiedName))
                {
                    Console.WriteLine($"  ✗ {test.FullyQualifiedName}");
                    if (!string.IsNullOrWhiteSpace(test.ErrorMessage))
                    {
                        // Show first line of error message, indented
                        var firstLine = test.ErrorMessage.Split('\n')[0].Trim();
                        if (firstLine.Length > 100)
                        {
                            firstLine = firstLine.Substring(0, 97) + "...";
                        }
                        Console.WriteLine($"      {firstLine}");
                    }
                }
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            Console.WriteLine("  (Unable to fetch test results - PAT needs 'Test Management (Read)' scope)");
        }
    }

}, orgOption, projectOption, patOption, buildIdArgument);

rootCommand.AddCommand(buildCommand);

// Tests command
var testsCommand = new Command("tests", "Get failed test results for a build");

var testsBuildIdArgument = new Argument<int>(
    name: "build-id",
    description: "The build ID to fetch failed test results for");

testsCommand.AddArgument(testsBuildIdArgument);

testsCommand.SetHandler(async (org, project, pat, buildId) =>
{
    if (string.IsNullOrWhiteSpace(pat))
    {
        Console.Error.WriteLine("Error: PAT is required. Use --pat or set AZDO_PAT environment variable.");
        Environment.ExitCode = 1;
        return;
    }

    using var client = new AzDoClient(org, project, pat);
    var results = await client.GetFailedTestsAsync(buildId);

    Console.WriteLine($"Found {results.Count} failed test(s):");
    Console.WriteLine();

    foreach (var result in results.OrderBy(r => r.FullyQualifiedName))
    {
        Console.WriteLine($"  ✗ {result.FullyQualifiedName}");
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            var firstLine = result.ErrorMessage.Split('\n')[0].Trim();
            if (firstLine.Length > 100)
            {
                firstLine = firstLine.Substring(0, 97) + "...";
            }
            Console.WriteLine($"      {firstLine}");
        }
    }

}, orgOption, projectOption, patOption, testsBuildIdArgument);

rootCommand.AddCommand(testsCommand);

// Diff command
var diffCommand = new Command("diff", "Compare test results between two builds");

var goodBuildOption = new Option<int>(
    aliases: new[] { "--good", "-g" },
    description: "Build ID of the known good build")
{
    IsRequired = true
};

var badBuildOption = new Option<int>(
    aliases: new[] { "--bad", "-b" },
    description: "Build ID of the known bad build")
{
    IsRequired = true
};

diffCommand.AddOption(goodBuildOption);
diffCommand.AddOption(badBuildOption);

diffCommand.SetHandler(async (org, project, pat, goodBuildId, badBuildId) =>
{
    if (string.IsNullOrWhiteSpace(pat))
    {
        Console.Error.WriteLine("Error: PAT is required. Use --pat or set AZDO_PAT environment variable.");
        Environment.ExitCode = 1;
        return;
    }

    using var client = new AzDoClient(org, project, pat);

    Console.WriteLine($"Fetching failed tests for build {goodBuildId} (good)...");
    var goodFailures = await client.GetFailedTestsAsync(goodBuildId);

    Console.WriteLine($"Fetching failed tests for build {badBuildId} (bad)...");
    var badFailures = await client.GetFailedTestsAsync(badBuildId);

    var diff = TestDiffer.ComputeDiff(goodFailures, badFailures);

    Console.WriteLine();
    Console.WriteLine($"New failures ({diff.NewFailures.Count}):");
    foreach (var test in diff.NewFailures)
    {
        Console.WriteLine($"  ✗ {test}");
    }

    Console.WriteLine();
    Console.WriteLine($"Consistent failures ({diff.ConsistentFailures.Count}):");
    foreach (var test in diff.ConsistentFailures)
    {
        Console.WriteLine($"  - {test}");
    }

}, orgOption, projectOption, patOption, goodBuildOption, badBuildOption);

rootCommand.AddCommand(diffCommand);

// Active command - show running/queued builds
var activeCommand = new Command("active", "Show active (running/queued) builds for a definition");

var definitionIdOption = new Option<int>(
    aliases: new[] { "--definition", "-d" },
    description: "Build definition ID to filter by")
{
    IsRequired = true
};

var showAllOption = new Option<bool>(
    aliases: new[] { "--all", "-a" },
    description: "Show recent completed builds as well");

activeCommand.AddOption(definitionIdOption);
activeCommand.AddOption(showAllOption);

activeCommand.SetHandler(async (org, project, pat, definitionId, showAll) =>
{
    if (string.IsNullOrWhiteSpace(pat))
    {
        Console.Error.WriteLine("Error: PAT is required. Use --pat or set AZDO_PAT environment variable.");
        Environment.ExitCode = 1;
        return;
    }

    using var client = new AzDoClient(org, project, pat);

    Console.WriteLine($"Fetching builds for definition {definitionId}...");
    Console.WriteLine();

    var activeBuilds = await client.GetActiveBuildsAsync(definitionId);

    if (activeBuilds.Count > 0)
    {
        Console.WriteLine($"Active builds ({activeBuilds.Count}):");
        Console.WriteLine();
        foreach (var build in activeBuilds.OrderByDescending(b => b.QueueTime))
        {
            PrintBuildInfo(build);
        }
    }
    else
    {
        Console.WriteLine("No active builds.");
        Console.WriteLine();
    }

    if (showAll)
    {
        var recentBuilds = await client.GetRecentBuildsAsync(definitionId, top: 10);
        if (recentBuilds.Count > 0)
        {
            Console.WriteLine($"Recent completed builds ({recentBuilds.Count}):");
            Console.WriteLine();
            foreach (var build in recentBuilds)
            {
                PrintBuildInfo(build);
            }
        }
    }

    static void PrintBuildInfo(Build build)
    {
        var shortSha = build.SourceVersion?.Substring(0, Math.Min(12, build.SourceVersion?.Length ?? 0)) ?? "unknown";
        Console.WriteLine($"  Build {build.Id}:");
        Console.WriteLine($"    Status:   {build.Status}");
        Console.WriteLine($"    Result:   {build.Result}");
        Console.WriteLine($"    Commit:   {shortSha}");
        Console.WriteLine($"    Queued:   {build.QueueTime}");
        Console.WriteLine($"    Started:  {build.StartTime}");
        Console.WriteLine($"    Finished: {build.FinishTime}");
        Console.WriteLine($"    URL:      {build.Links?.Web?.Href}");
        Console.WriteLine();
    }

}, orgOption, projectOption, patOption, definitionIdOption, showAllOption);

rootCommand.AddCommand(activeCommand);

// Bisect command
var bisectCommand = new Command("bisect", "Find the commit that introduced a test failure");

var bisectGoodBuildOption = new Option<int>(
    aliases: ["--good", "-g"],
    description: "Build ID of the known good build (test passes)")
{
    IsRequired = true
};

var bisectBadBuildOption = new Option<int>(
    aliases: ["--bad", "-b"],
    description: "Build ID of the known bad build (test fails)")
{
    IsRequired = true
};

var testNameOption = new Option<string>(
    aliases: ["--test", "-t"],
    description: "Fully qualified name of the test to track (or substring to match)")
{
    IsRequired = true
};

var repoPathOption = new Option<string>(
    aliases: ["--repo", "-r"],
    description: "Path to the git repository (defaults to current directory)",
    getDefaultValue: () => Environment.CurrentDirectory);

var manualOption = new Option<bool>(
    aliases: ["--manual", "-m"],
    description: "Don't auto-queue builds; just report what needs to be done");

var pollIntervalOption = new Option<int>(
    aliases: ["--poll-interval"],
    description: "Seconds between polling for build completion",
    getDefaultValue: () => 300);

bisectCommand.AddOption(bisectGoodBuildOption);
bisectCommand.AddOption(bisectBadBuildOption);
bisectCommand.AddOption(testNameOption);
bisectCommand.AddOption(repoPathOption);
bisectCommand.AddOption(manualOption);
bisectCommand.AddOption(pollIntervalOption);

bisectCommand.SetHandler(async (context) =>
{
    var org = context.ParseResult.GetValueForOption(orgOption)!;
    var project = context.ParseResult.GetValueForOption(projectOption)!;
    var pat = context.ParseResult.GetValueForOption(patOption)!;
    var goodBuildId = context.ParseResult.GetValueForOption(bisectGoodBuildOption);
    var badBuildId = context.ParseResult.GetValueForOption(bisectBadBuildOption);
    var testName = context.ParseResult.GetValueForOption(testNameOption)!;
    var repoPath = context.ParseResult.GetValueForOption(repoPathOption)!;
    var manual = context.ParseResult.GetValueForOption(manualOption);
    var pollInterval = context.ParseResult.GetValueForOption(pollIntervalOption);
    var cancellationToken = context.GetCancellationToken();

    if (string.IsNullOrWhiteSpace(pat))
    {
        Console.Error.WriteLine("Error: PAT is required. Use --pat or set AZDO_PAT environment variable.");
        Environment.ExitCode = 1;
        return;
    }

    using var client = new AzDoClient(org, project, pat);

    // Get build info for good and bad builds
    Console.WriteLine("Fetching build information...");
    var goodBuild = await client.GetBuildAsync(goodBuildId, cancellationToken);
    var badBuild = await client.GetBuildAsync(badBuildId, cancellationToken);

    if (goodBuild == null)
    {
        Console.Error.WriteLine($"Good build {goodBuildId} not found.");
        Environment.ExitCode = 1;
        return;
    }

    if (badBuild == null)
    {
        Console.Error.WriteLine($"Bad build {badBuildId} not found.");
        Environment.ExitCode = 1;
        return;
    }

    var goodCommit = goodBuild.SourceVersion;
    var badCommit = badBuild.SourceVersion;
    var definitionId = badBuild.Definition?.Id;
    var sourceBranch = badBuild.SourceBranch;

    if (string.IsNullOrEmpty(goodCommit) || string.IsNullOrEmpty(badCommit))
    {
        Console.Error.WriteLine("Could not determine source commits for builds.");
        Environment.ExitCode = 1;
        return;
    }

    if (definitionId == null)
    {
        Console.Error.WriteLine("Could not determine pipeline definition ID.");
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine($"Good commit: {goodCommit[..12]} (build {goodBuildId})");
    Console.WriteLine($"Bad commit:  {badCommit[..12]} (build {badBuildId})");
    Console.WriteLine($"Pipeline:    {badBuild.Definition?.Name} ({definitionId})");
    Console.WriteLine($"Test:        {testName}");
    Console.WriteLine($"Mode:        {(manual ? "Manual" : "Auto-queue")}");
    Console.WriteLine();

    // Verify the test actually fails in the bad build
    Console.WriteLine("Verifying test fails in bad build...");
    var badFailures = await client.GetFailedTestsAsync(badBuildId, cancellationToken);
    var matchingFailure = badFailures.FirstOrDefault(t =>
        t.FullyQualifiedName.Contains(testName, StringComparison.OrdinalIgnoreCase));

    if (matchingFailure == null)
    {
        Console.Error.WriteLine($"Test '{testName}' is not failing in the bad build.");
        Console.Error.WriteLine("Available failing tests:");
        foreach (var test in badFailures.Take(10))
        {
            Console.Error.WriteLine($"  - {test.FullyQualifiedName}");
        }
        if (badFailures.Count > 10)
        {
            Console.Error.WriteLine($"  ... and {badFailures.Count - 10} more");
        }
        Environment.ExitCode = 1;
        return;
    }

    var fullTestName = matchingFailure.FullyQualifiedName;
    Console.WriteLine($"Matched test: {fullTestName}");

    // Verify the test passes (or doesn't exist) in the good build
    Console.WriteLine("Verifying test passes in good build...");
    var goodFailures = await client.GetFailedTestsAsync(goodBuildId, cancellationToken);
    var goodMatchingFailure = goodFailures.FirstOrDefault(t =>
        t.FullyQualifiedName.Equals(fullTestName, StringComparison.OrdinalIgnoreCase));

    if (goodMatchingFailure != null)
    {
        Console.Error.WriteLine($"Test '{fullTestName}' is also failing in the good build. Cannot bisect.");
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine("Test status verified.");
    Console.WriteLine();

    // Get the list of commits between good and bad
    Console.WriteLine("Enumerating commits...");
    var commits = await GitHelper.GetCommitRangeAsync(goodCommit, badCommit, repoPath, cancellationToken);

    if (commits.Count == 0)
    {
        Console.Error.WriteLine("No commits found between good and bad builds.");
        Console.Error.WriteLine("Make sure you're in the correct git repository and have fetched all commits.");
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine($"Found {commits.Count} commit(s) to search.");
    Console.WriteLine($"Bisect will require at most {Math.Ceiling(Math.Log2(commits.Count + 1))} build(s).");
    Console.WriteLine();

    // Binary search through commits
    var remaining = commits.ToList();
    var testedCommits = new Dictionary<string, bool>(); // commit -> failed

    // We know the endpoints
    testedCommits[goodCommit] = false;
    testedCommits[badCommit] = true;

    var step = 1;
    while (remaining.Count > 1)
    {
        var midIndex = remaining.Count / 2;
        var midCommit = remaining[midIndex];
        var shortSha = await GitHelper.GetShortShaAsync(midCommit, repoPath, cancellationToken);
        var subject = await GitHelper.GetCommitSubjectAsync(midCommit, repoPath, cancellationToken);

        Console.WriteLine($"[Step {step}] Testing commit {shortSha} ({remaining.Count} commits remaining)");
        Console.WriteLine($"         {subject}");

        // Check if we already have a build for this commit
        Console.WriteLine($"         Searching for builds: definition={definitionId.Value}, commit={midCommit[..12]}...");
        var existingBuilds = await client.FindBuildsAsync(midCommit, definitionId.Value, cancellationToken);

        // Debug: show what builds we found
        if (existingBuilds.Count > 0)
        {
            Console.WriteLine($"         Found {existingBuilds.Count} existing build(s) for this commit:");
            foreach (var b in existingBuilds)
            {
                Console.WriteLine($"           - Build {b.Id}: Status={b.Status}, Result={b.Result}");
            }
        }
        else
        {
            Console.WriteLine($"         No existing builds found for commit {shortSha}");
        }

        Build? buildToCheck = existingBuilds.FirstOrDefault(b =>
            b.Status == BuildStatus.Completed &&
            (b.Result == BuildResult.Succeeded || b.Result == BuildResult.PartiallySucceeded || b.Result == BuildResult.Failed));

        if (buildToCheck == null)
        {
            // Check for in-progress builds
            var inProgressBuild = existingBuilds.FirstOrDefault(b =>
                b.Status == BuildStatus.InProgress || b.Status == BuildStatus.NotStarted);

            if (inProgressBuild != null)
            {
                Console.WriteLine($"         Build {inProgressBuild.Id} is in progress...");
                buildToCheck = await WaitForBuildAsync(client, inProgressBuild.Id, pollInterval, cancellationToken);
            }
            else if (manual)
            {
                Console.WriteLine($"         No existing build found. Queue a build for commit: {midCommit}");
                Console.WriteLine();
                Console.WriteLine("Once the build completes, re-run this command to continue bisecting.");
                return;
            }
            else
            {
                // Auto-queue a new build
                Console.WriteLine($"         Queuing new build...");
                try
                {
                    var newBuild = await client.QueueBuildAsync(definitionId.Value, midCommit, sourceBranch, cancellationToken);
                    Console.WriteLine($"         Queued build {newBuild.Id}");
                    if (newBuild.Links?.Web?.Href != null)
                    {
                        Console.WriteLine($"         {newBuild.Links.Web.Href}");
                    }
                    buildToCheck = await WaitForBuildAsync(client, newBuild.Id, pollInterval, cancellationToken);
                }
                catch (HttpRequestException ex)
                {
                    Console.Error.WriteLine($"         Failed to queue build: {ex.Message}");
                    Console.Error.WriteLine("         Try running with --manual mode or queue the build manually.");
                    Environment.ExitCode = 1;
                    return;
                }
            }
        }
        else
        {
            Console.WriteLine($"         Found existing build: {buildToCheck.Id} ({buildToCheck.Result})");
        }

        if (buildToCheck == null)
        {
            Console.Error.WriteLine("         Build did not complete successfully.");
            Environment.ExitCode = 1;
            return;
        }

        // Check if build was successful enough to have test results
        if (buildToCheck.Result == BuildResult.Failed)
        {
            Console.WriteLine($"         Build failed (not test failures). Treating as inconclusive, trying next commit...");
            // Remove this commit from consideration and continue
            remaining.RemoveAt(midIndex);
            step++;
            continue;
        }

        var failures = await client.GetFailedTestsAsync(buildToCheck.Id, cancellationToken);
        var testFailed = failures.Any(t => t.FullyQualifiedName.Equals(fullTestName, StringComparison.OrdinalIgnoreCase));

        testedCommits[midCommit] = testFailed;
        Console.WriteLine($"         Test result: {(testFailed ? "FAILED ✗" : "PASSED ✓")}");
        Console.WriteLine();

        if (testFailed)
        {
            // Bug was introduced at or before this commit
            remaining = remaining.Take(midIndex + 1).ToList();
        }
        else
        {
            // Bug was introduced after this commit
            remaining = remaining.Skip(midIndex).ToList();
        }

        step++;
    }

    // Found the culprit
    var culpritCommit = remaining[0];
    var culpritShortSha = await GitHelper.GetShortShaAsync(culpritCommit, repoPath, cancellationToken);
    var culpritSubject = await GitHelper.GetCommitSubjectAsync(culpritCommit, repoPath, cancellationToken);

    Console.WriteLine("═══════════════════════════════════════════════════════════════════");
    Console.WriteLine($"FOUND: First bad commit is {culpritShortSha}");
    Console.WriteLine($"       {culpritSubject}");
    Console.WriteLine($"       Full SHA: {culpritCommit}");
    Console.WriteLine("═══════════════════════════════════════════════════════════════════");
});

rootCommand.AddCommand(bisectCommand);

return await rootCommand.InvokeAsync(args);

// Helper function to wait for a build to complete
static async Task<Build?> WaitForBuildAsync(AzDoClient client, int buildId, int pollIntervalSeconds, CancellationToken cancellationToken)
{
    var startTime = DateTime.UtcNow;
    while (true)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var build = await client.GetBuildAsync(buildId, cancellationToken);
        if (build == null)
        {
            return null;
        }

        if (build.Status == BuildStatus.Completed)
        {
            return build;
        }

        var elapsed = DateTime.UtcNow - startTime;
        Console.WriteLine($"         Waiting for build... ({elapsed:hh\\:mm\\:ss} elapsed, status: {build.Status})");

        await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), cancellationToken);
    }
}

