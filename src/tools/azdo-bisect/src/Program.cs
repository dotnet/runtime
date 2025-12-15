using System;
using System.Linq;
using System.Net.Http;
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

return await rootCommand.InvokeAsync(args);

