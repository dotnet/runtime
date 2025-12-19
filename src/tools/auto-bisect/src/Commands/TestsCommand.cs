using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoBisect;
using Spectre.Console;

namespace AutoBisect.Commands;

internal static class TestsCommand
{
    public static async Task HandleAsync(string org, string project, string pat, int buildId)
    {
        using var client = new AzDoClient(org, project, pat);
        List<TestResult> results = [];
        await AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(
                $"[yellow]Fetching failed tests for build {buildId}...[/]",
                async ctx =>
                {
                    await foreach (var test in client.GetFailedTestsAsync(buildId))
                    {
                        results.Add(test);
                    }
                }
            );

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] No failed tests found in build {buildId}.");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Red)
            .Title($"[bold red]Failed Tests ({results.Count})[/]")
            .AddColumn("[bold]Test Name[/]")
            .AddColumn("[bold]Error Message[/]");

        foreach (var result in results.OrderBy(r => r.FullyQualifiedName))
        {
            var errorMsg = "";
            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                var firstLine = result.ErrorMessage.Split('\n')[0].Trim();
                if (firstLine.Length > 100)
                {
                    firstLine = firstLine.Substring(0, 97) + "...";
                }
                errorMsg = $"[dim]{firstLine.EscapeMarkup()}[/]";
            }
            table.AddRow($"[red]✗[/] {result.FullyQualifiedName.EscapeMarkup()}", errorMsg);
        }

        AnsiConsole.Write(table);
    }
}
