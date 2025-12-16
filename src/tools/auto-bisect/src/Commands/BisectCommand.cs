using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoBisect;

namespace AutoBisect.Commands;

internal static class BisectCommand
{
    public static async Task<int> HandleAsync(CancellationToken cancellationToken,
        string org, string project, string pat,
        int goodBuildId, int badBuildId,
        string testName, string repoPath,
        bool manual, int pollInterval)
    {

        if (string.IsNullOrWhiteSpace(pat))
        {
            Console.Error.WriteLine("Error: PAT is required. Use --pat or set AZDO_PAT environment variable.");
            Environment.ExitCode = 1;
            return 1;
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
            return 1;
        }

        if (badBuild == null)
        {
            Console.Error.WriteLine($"Bad build {badBuildId} not found.");
            Environment.ExitCode = 1;
            return 1;
        }

        var goodCommit = goodBuild.SourceVersion;
        var badCommit = badBuild.SourceVersion;
        var definitionId = badBuild.Definition?.Id;
        var sourceBranch = badBuild.SourceBranch;

        if (string.IsNullOrEmpty(goodCommit) || string.IsNullOrEmpty(badCommit))
        {
            Console.Error.WriteLine("Could not determine source commits for builds.");
            Environment.ExitCode = 1;
            return 1;
        }

        if (definitionId == null)
        {
            Console.Error.WriteLine("Could not determine pipeline definition ID.");
            Environment.ExitCode = 1;
            return 1;
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
            return 1;
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
            return 1;
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
            return 1;
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
                    buildToCheck = await BuildUtilities.WaitForBuildAsync(client, inProgressBuild.Id, pollInterval, cancellationToken);
                }
                else if (manual)
                {
                    Console.WriteLine($"         No existing build found. Queue a build for commit: {midCommit}");
                    Console.WriteLine();
                    Console.WriteLine("Once the build completes, re-run this command to continue bisecting.");
                    return 0;
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
                        buildToCheck = await BuildUtilities.WaitForBuildAsync(client, newBuild.Id, pollInterval, cancellationToken);
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.Error.WriteLine($"         Failed to queue build: {ex.Message}");
                        Console.Error.WriteLine("         Try running with --manual mode or queue the build manually.");
                        Environment.ExitCode = 1;
                        return 1;
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
                return 1;
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
        return 0;
    }
}
