using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoBisect;
using Spectre.Console;

namespace AutoBisect.Commands;

internal static class BuildCommand
{
    public static async Task HandleAsync(string org, string project, string pat, int buildId)
    {
        using var client = new AzDoClient(org, project, pat);
        Build? build = null;
        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                "[yellow]Fetching build information...[/]",
                async ctx =>
                {
                    build = await client.GetBuildAsync(buildId);
                }
            );

        if (build == null)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Build {buildId} not found.");
            Environment.ExitCode = 1;
            return;
        }

        var statusColor = build.Status == BuildStatus.Completed ? "green" : "yellow";
        var resultColor =
            build.Result == BuildResult.Succeeded ? "green"
            : build.Result == BuildResult.Failed ? "red"
            : "yellow";

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .AddColumn("[bold]Property[/]")
            .AddColumn("[bold]Value[/]")
            .AddRow("Build ID", $"[cyan]{build.Id}[/]")
            .AddRow("Build Number", build.BuildNumber ?? "N/A")
            .AddRow("Status", $"[{statusColor}]{build.Status}[/]")
            .AddRow("Result", $"[{resultColor}]{build.Result}[/]")
            .AddRow("Source Version", $"[cyan]{build.SourceVersion?[..12]}[/]")
            .AddRow("Source Branch", build.SourceBranch ?? "N/A")
            .AddRow("Definition", $"{build.Definition?.Name} ({build.Definition?.Id})")
            .AddRow("Queue Time", build.QueueTime?.ToString() ?? "N/A")
            .AddRow("Start Time", build.StartTime?.ToString() ?? "N/A")
            .AddRow("Finish Time", build.FinishTime?.ToString() ?? "N/A");

        if (build.Links?.Web?.Href != null)
        {
            table.AddRow("Web URL", $"[link]{build.Links.Web.Href}[/]");
        }

        AnsiConsole.Write(table);

        // Fetch and display test failures if the build has completed
        if (build.Status == BuildStatus.Completed)
        {
            AnsiConsole.WriteLine();
            List<TestResult> failedTests = [];
            await AnsiConsole
                .Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync(
                    "[yellow]Fetching failed tests...[/]",
                    async ctx =>
                    {
                        try
                        {
                            await foreach (var test in client.GetFailedTestsAsync(buildId))
                            {
                                failedTests.Add(test);
                            }
                        }
                        catch (HttpRequestException ex)
                            when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            // Will handle below
                        }
                    }
                );

            if (failedTests.Count > 0)
            {
                var testTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Red)
                    .AddColumn("[bold red]Failed Tests[/]")
                    .AddColumn("[bold]Error Message[/]");

                foreach (var test in failedTests.OrderBy(t => t.FullyQualifiedName))
                {
                    var errorMsg = "";
                    if (!string.IsNullOrWhiteSpace(test.ErrorMessage))
                    {
                        var firstLine = test.ErrorMessage.Split('\n')[0].Trim();
                        if (firstLine.Length > 100)
                        {
                            firstLine = firstLine.Substring(0, 97) + "...";
                        }
                        errorMsg = $"[dim]{firstLine.EscapeMarkup()}[/]";
                    }
                    testTable.AddRow(
                        $"[red]✗[/] {test.FullyQualifiedName.EscapeMarkup()}",
                        errorMsg
                    );
                }

                AnsiConsole.Write(testTable);
            }
            else if (failedTests.Count == 0)
            {
                AnsiConsole.MarkupLine("[green]✓[/] No failed tests found.");
            }
        }
    }
}
