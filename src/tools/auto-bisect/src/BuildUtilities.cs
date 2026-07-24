using System;
using System.Threading;
using System.Threading.Tasks;
using AutoBisect;
using Spectre.Console;

namespace AutoBisect;

internal static class BuildUtilities
{
    public static void PrintBuildInfo(Build build)
    {
        var shortSha =
            build.SourceVersion?.Substring(0, Math.Min(12, build.SourceVersion?.Length ?? 0))
            ?? "unknown";
        var statusColor = build.Status == BuildStatus.Completed ? "green" : "yellow";
        var resultColor =
            build.Result == BuildResult.Succeeded ? "green"
            : build.Result == BuildResult.Failed ? "red"
            : "yellow";

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .Title($"[bold]Build {build.Id}[/]")
            .AddColumn("[bold]Property[/]")
            .AddColumn("[bold]Value[/]")
            .AddRow("Status", $"[{statusColor}]{build.Status}[/]")
            .AddRow("Result", $"[{resultColor}]{build.Result}[/]")
            .AddRow("Commit", $"[cyan]{shortSha}[/]")
            .AddRow("Queued", build.QueueTime?.ToString() ?? "N/A")
            .AddRow("Started", build.StartTime?.ToString() ?? "N/A")
            .AddRow("Finished", build.FinishTime?.ToString() ?? "N/A");

        if (build.Links?.Web?.Href is not null)
        {
            table.AddRow("URL", $"[link]{build.Links.Web.Href}[/]");
        }

        AnsiConsole.Write(table);
    }

    public static async Task<Build?> WaitForBuildAsync(
        AzDoClient client,
        int buildId,
        int pollIntervalSeconds,
        string? testName = null,
        CancellationToken cancellationToken = default
    )
    {
        var startTime = DateTime.UtcNow;
        Build? build = null;
        var earlyExit = false;

        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                $"[yellow]Waiting for build {buildId}...[/]",
                async ctx =>
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        build = await client.GetBuildAsync(buildId, cancellationToken);
                        if (build is null)
                        {
                            return;
                        }

                        if (build.Status is BuildStatus.Completed)
                        {
                            return;
                        }

                        // If a test name is provided, check if it has already failed
                        if (!string.IsNullOrEmpty(testName))
                        {
                            var hasTestFailed = await client.HasTestFailedAsync(
                                buildId,
                                testName,
                                cancellationToken
                            );
                            if (hasTestFailed)
                            {
                                AnsiConsole.MarkupLine(
                                    $"[yellow]⚡[/] Test '{testName.EscapeMarkup()}' has already failed - stopping early"
                                );
                                earlyExit = true;
                                return;
                            }
                        }

                        var elapsed = DateTime.UtcNow - startTime;
                        ctx.Status(
                            $"[yellow]Waiting for build {buildId}...[/] ({elapsed:hh\\:mm\\:ss} elapsed, status: {build.Status})"
                        );

                        await Task.Delay(
                            TimeSpan.FromSeconds(pollIntervalSeconds),
                            cancellationToken
                        );
                    }
                }
            );

        // If we exited early due to test failure, treat the build as if it completed with failure
        if (earlyExit && build is not null)
        {
            // We'll still return the build object, but mark it conceptually as failed
            // The calling code will check the actual test results anyway
            AnsiConsole.MarkupLine($"[dim]Build {buildId} still running, but test failure detected[/]");
        }

        return build;
    }
}
