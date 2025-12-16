using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoBisect;

namespace AutoBisect.Commands;

internal static class BuildCommand
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
    }
}
