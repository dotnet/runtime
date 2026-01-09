using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace AutoBisect.Commands;

internal static class BisectCommand
{
    public static async Task<int> HandleAsync(
        CancellationToken cancellationToken,
        string org,
        string project,
        string pat,
        int goodBuildId,
        int badBuildId,
        string testName,
        string repoPath,
        bool manual,
        int pollInterval
    )
    {
        using var client = new AzDoClient(org, project, pat);

        // Get build info for good and bad builds
        Build? goodBuild = null;
        Build? badBuild = null;
        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                "[yellow]Fetching build information...[/]",
                async ctx =>
                {
                    goodBuild = await client.GetBuildAsync(goodBuildId, cancellationToken);
                    badBuild = await client.GetBuildAsync(badBuildId, cancellationToken);
                }
            );

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

        var configTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .AddColumn("[bold]Property[/]")
            .AddColumn("[bold]Value[/]")
            .AddRow("Good commit", $"[green]{goodCommit[..12]}[/] (build {goodBuildId})")
            .AddRow("Bad commit", $"[red]{badCommit[..12]}[/] (build {badBuildId})")
            .AddRow("Pipeline", $"{badBuild.Definition?.Name} ({definitionId})")
            .AddRow("Test", $"[yellow]{testName}[/]")
            .AddRow("Mode", manual ? "Manual" : "Auto-queue");

        AnsiConsole.Write(configTable);
        AnsiConsole.WriteLine();

        // Verify the test actually fails in the bad build
        List<TestResult> badFailures = [];
        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                "[yellow]Verifying test fails in bad build...[/]",
                async ctx =>
                {
                    await foreach (var test in client.GetFailedTestsAsync(badBuildId, cancellationToken))
                    {
                        badFailures.Add(test);
                    }
                }
            );
        var matchingFailure = badFailures.FirstOrDefault(t =>
            t.FullyQualifiedName.Contains(testName, StringComparison.OrdinalIgnoreCase)
        );

        if (matchingFailure is null)
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
        AnsiConsole.MarkupLine($"[green]✓[/] Matched test: [cyan]{fullTestName.EscapeMarkup()}[/]");

        // Verify the test passes (or doesn't exist) in the good build
        List<TestResult> goodFailures = [];
        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                "[yellow]Verifying test passes in good build...[/]",
                async ctx =>
                {
                    await foreach (var test in client.GetFailedTestsAsync(goodBuildId, cancellationToken))
                    {
                        goodFailures.Add(test);
                    }
                }
            );
        var goodMatchingFailure = goodFailures.FirstOrDefault(t =>
            t.FullyQualifiedName.Equals(fullTestName, StringComparison.OrdinalIgnoreCase)
        );

        if (goodMatchingFailure is not null)
        {
            Console.Error.WriteLine(
                $"Test '{fullTestName}' is also failing in the good build. Cannot bisect."
            );
            Environment.ExitCode = 1;
            return 1;
        }

        AnsiConsole.MarkupLine("[green]✓[/] Test status verified.");
        AnsiConsole.WriteLine();

        // Get the list of commits between good and bad
        List<string> commits = [];
        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                "[yellow]Enumerating commits...[/]",
                async ctx =>
                {
                    commits = (
                        await GitHelper.GetCommitRangeAsync(
                            goodCommit,
                            badCommit,
                            repoPath,
                            cancellationToken
                        )
                    ).ToList();
                }
            );

        if (commits.Count == 0)
        {
            Console.Error.WriteLine("No commits found between good and bad builds.");
            Console.Error.WriteLine(
                "Make sure you're in the correct git repository and have fetched all commits."
            );
            Environment.ExitCode = 1;
            return 1;
        }

        var panel = new Panel(
            $"[bold]Found {commits.Count} commit(s) to search.[/]\nBisect will require at most [yellow]{Math.Ceiling(Math.Log2(commits.Count + 1))}[/] build(s)."
        )
            .BorderColor(Color.Green)
            .Header("[bold blue]Bisect Plan[/]");
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Binary search through commits using the BisectAlgorithm
        var bisect = new BisectAlgorithm(commits);

        var step = 1;
        while (true)
        {
            var bisectStep = bisect.GetNextStep();
            if (bisectStep.IsComplete)
            {
                // Found the culprit
                var culpritCommit = bisect.GetCommit(bisectStep.FirstBadCommitIndex);
                var culpritShortSha = await GitHelper.GetShortShaAsync(
                    culpritCommit,
                    repoPath,
                    cancellationToken
                );
                var culpritSubject = await GitHelper.GetCommitSubjectAsync(
                    culpritCommit,
                    repoPath,
                    cancellationToken
                );

                var resultPanel = new Panel(
                    new Markup(
                        $"[bold red]First bad commit:[/] [cyan]{culpritShortSha}[/]\n\n"
                            + $"[dim]{culpritSubject.EscapeMarkup()}[/]\n\n"
                            + $"[bold]Full SHA:[/] [cyan]{culpritCommit}[/]"
                    )
                )
                    .BorderColor(Color.Red)
                    .Header("[bold red]🔍 BISECT RESULT[/]");

                AnsiConsole.Write(resultPanel);
                AnsiConsole.WriteLine();

                // Display summary for filing an issue
                var summaryPanel = new Panel(
                    $"[bold]Test:[/] {fullTestName.EscapeMarkup()}\n" +
                    $"[bold]First bad commit:[/] {culpritShortSha} ({culpritCommit})\n" +
                    $"[bold]Good build:[/] {goodBuildId} (commit {goodCommit[..12]})\n" +
                    $"[bold]Bad build:[/] {badBuildId} (commit {badCommit[..12]})\n" +
                    $"[bold]Commits searched:[/] {bisect.TotalCommits}"
                )
                    .BorderColor(Color.Blue)
                    .Header("[bold blue]📋 Issue Summary[/]");

                AnsiConsole.Write(summaryPanel);
                return 0;
            }

            var midCommit = bisect.GetCommit(bisectStep.CommitIndexToTest);
            var shortSha = await GitHelper.GetShortShaAsync(midCommit, repoPath, cancellationToken);
            var subject = await GitHelper.GetCommitSubjectAsync(
                midCommit,
                repoPath,
                cancellationToken
            );

            AnsiConsole.Write(new Rule($"[bold yellow]Step {step}[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine(
                $"Testing commit [cyan]{shortSha}[/] ([yellow]{bisect.RemainingCount}[/] commits remaining)"
            );
            AnsiConsole.MarkupLine($"[dim]{subject.EscapeMarkup()}[/]");

            // Check if we already have a build for this commit
            AnsiConsole.MarkupLine(
                $"[dim]Searching for builds: definition={definitionId.Value}, commit={midCommit[..12]}...[/]"
            );
            var existingBuilds = await client.FindBuildsAsync(
                midCommit,
                definitionId.Value,
                cancellationToken
            );

            // Debug: show what builds we found
            if (existingBuilds.Count > 0)
            {
                AnsiConsole.MarkupLine(
                    $"[green]✓[/] Found {existingBuilds.Count} existing build(s) for this commit:"
                );
                var tree = new Tree("[bold]Builds[/]");
                foreach (var b in existingBuilds)
                {
                    var statusColor = b.Status == BuildStatus.Completed ? "green" : "yellow";
                    var resultColor =
                        b.Result == BuildResult.Succeeded ? "green"
                        : b.Result == BuildResult.Failed ? "red"
                        : "yellow";
                    tree.AddNode(
                        $"[{statusColor}]Build {b.Id}[/]: Status=[{statusColor}]{b.Status}[/], Result=[{resultColor}]{b.Result}[/]"
                    );
                }
                AnsiConsole.Write(tree);
            }
            else
            {
                AnsiConsole.MarkupLine($"[dim]No existing builds found for commit {shortSha}[/]");
            }

            Build? buildToCheck = existingBuilds.FirstOrDefault(b =>
                b.Status == BuildStatus.Completed
                && (
                    b.Result == BuildResult.Succeeded
                    || b.Result == BuildResult.PartiallySucceeded
                    || b.Result == BuildResult.Failed
                )
            );

            if (buildToCheck is null)
            {
                // Check for in-progress builds
                var inProgressBuild = existingBuilds.FirstOrDefault(b =>
                    b.Status == BuildStatus.InProgress || b.Status == BuildStatus.NotStarted
                );

                if (inProgressBuild is not null)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]⏳[/] Build {inProgressBuild.Id} is in progress..."
                    );
                    buildToCheck = await BuildUtilities.WaitForBuildAsync(
                        client,
                        inProgressBuild.Id,
                        pollInterval,
                        fullTestName,
                        cancellationToken
                    );
                }
                else if (manual)
                {
                    var manualPanel = new Panel(
                        $"[yellow]No existing build found.[/]\n\nQueue a build for commit: [cyan]{midCommit}[/]\n\nOnce the build completes, re-run this command to continue bisecting."
                    )
                        .BorderColor(Color.Yellow)
                        .Header("[bold]Manual Action Required[/]");
                    AnsiConsole.Write(manualPanel);
                    return 0;
                }
                else
                {
                    // Auto-queue a new build
                    try
                    {
                        Build? newBuild = null;
                        await AnsiConsole
                            .Status()
                            .Spinner(Spinner.Known.Dots)
                            .StartAsync(
                                "[yellow]Queuing new build...[/]",
                                async ctx =>
                                {
                                    newBuild = await client.QueueBuildAsync(
                                        definitionId.Value,
                                        midCommit,
                                        sourceBranch,
                                        cancellationToken
                                    );
                                }
                            );

                        if (newBuild is not null)
                        {
                            AnsiConsole.MarkupLine(
                                $"[green]✓[/] Queued build [cyan]{newBuild.Id}[/]"
                            );
                            if (newBuild.Links?.Web?.Href is not null)
                            {
                                AnsiConsole.MarkupLine($"[link]{newBuild.Links.Web.Href}[/]");
                            }
                            buildToCheck = await BuildUtilities.WaitForBuildAsync(
                                client,
                                newBuild.Id,
                                pollInterval,
                                fullTestName,
                                cancellationToken
                            );
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        AnsiConsole.MarkupLine(
                            $"[red]✗[/] Failed to queue build: {ex.Message.EscapeMarkup()}"
                        );
                        AnsiConsole.MarkupLine(
                            "[yellow]Try running with --manual mode or queue the build manually.[/]"
                        );
                        Environment.ExitCode = 1;
                        return 1;
                    }
                }
            }
            else
            {
                var resultColor =
                    buildToCheck.Result == BuildResult.Succeeded ? "green"
                    : buildToCheck.Result == BuildResult.Failed ? "red"
                    : "yellow";
                AnsiConsole.MarkupLine(
                    $"[green]✓[/] Found existing build: [cyan]{buildToCheck.Id}[/] ([{resultColor}]{buildToCheck.Result}[/])"
                );
            }

            if (buildToCheck is null)
            {
                Console.Error.WriteLine("         Build did not complete successfully.");
                Environment.ExitCode = 1;
                return 1;
            }

            // Check test results - build failures can be due to test failures
            var testFailed = false;
            await foreach (var failure in client.GetFailedTestsAsync(buildToCheck.Id, cancellationToken))
            {
                if (failure.FullyQualifiedName.Equals(fullTestName, StringComparison.OrdinalIgnoreCase))
                {
                    testFailed = true;
                    break;
                }
            }

            var resultIcon = testFailed ? "[red]✗ FAILED[/]" : "[green]✓ PASSED[/]";
            AnsiConsole.MarkupLine($"Test result: {resultIcon}");
            AnsiConsole.WriteLine();

            bisect.RecordResult(bisectStep.CommitIndexToTest, testFailed);

            step++;
        }
    }
}
