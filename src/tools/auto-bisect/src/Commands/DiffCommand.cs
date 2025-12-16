using System;
using System.Threading.Tasks;
using AutoBisect;

namespace AutoBisect.Commands;

internal static class DiffCommand
{
    public static async Task HandleAsync(string org, string project, string pat, int goodBuildId, int badBuildId)
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
    }
}
