using System;
using System.Linq;
using System.Threading.Tasks;
using AutoBisect;

namespace AutoBisect.Commands;

internal static class TestsCommand
{
    public static async Task HandleAsync(string org, string project, string pat, int buildId)
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
    }
}
