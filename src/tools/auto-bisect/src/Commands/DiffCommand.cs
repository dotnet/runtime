using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoBisect;
using Spectre.Console;

namespace AutoBisect.Commands;

internal static class DiffCommand
{
    public static async Task HandleAsync(
        string org,
        string project,
        string pat,
        int goodBuildId,
        int badBuildId
    )
    {
        using var client = new AzDoClient(org, project, pat);

        List<TestResult> goodFailures = [];
        List<TestResult> badFailures = [];

        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                "[yellow]Fetching test results...[/]",
                async ctx =>
                {
                    ctx.Status(
                        $"[yellow]Fetching failed tests for build {goodBuildId} (good)...[/]"
                    );
                    await foreach (var test in client.GetFailedTestsAsync(goodBuildId))
                    {
                        goodFailures.Add(test);
                    }

                    ctx.Status($"[yellow]Fetching failed tests for build {badBuildId} (bad)...[/]");
                    await foreach (var test in client.GetFailedTestsAsync(badBuildId))
                    {
                        badFailures.Add(test);
                    }
                }
            );

        var diff = TestDiffer.ComputeDiff(goodFailures, badFailures);

        var newFailuresTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Red)
            .Title($"[bold red]New Failures ({diff.NewFailures.Count})[/]")
            .AddColumn("[bold]Test Name[/]");

        if (diff.NewFailures.Count > 0)
        {
            foreach (var test in diff.NewFailures)
            {
                newFailuresTable.AddRow($"[red]✗[/] {test.EscapeMarkup()}");
            }
        }
        else
        {
            newFailuresTable.AddRow("[dim]No new failures[/]");
        }

        AnsiConsole.Write(newFailuresTable);
        AnsiConsole.WriteLine();

        var consistentFailuresTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Title($"[bold yellow]Consistent Failures ({diff.ConsistentFailures.Count})[/]")
            .AddColumn("[bold]Test Name[/]");

        if (diff.ConsistentFailures.Count > 0)
        {
            foreach (var test in diff.ConsistentFailures)
            {
                consistentFailuresTable.AddRow($"[yellow]─[/] {test.EscapeMarkup()}");
            }
        }
        else
        {
            consistentFailuresTable.AddRow("[dim]No consistent failures[/]");
        }

        AnsiConsole.Write(consistentFailuresTable);
    }
}
