using System;
using System.CommandLine;
using AutoBisect.Commands;

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

rootCommand.AddGlobalOption(orgOption);
rootCommand.AddGlobalOption(projectOption);
rootCommand.AddGlobalOption(patOption);

// Add commands
rootCommand.AddCommand(CreateBuildCommand());
rootCommand.AddCommand(CreateTestsCommand());
rootCommand.AddCommand(CreateDiffCommand());
rootCommand.AddCommand(CreateQueuedCommand());
rootCommand.AddCommand(CreateBisectCommand());

return await rootCommand.InvokeAsync(args);

Command CreateBuildCommand()
{
    var command = new Command("build", "Get information about a build");
    var buildIdArgument = new Argument<int>(
        name: "build-id",
        description: "The build ID to fetch");

    command.AddArgument(buildIdArgument);
    command.SetHandler(async (org, project, pat, buildId) =>
    {
        await BuildCommand.HandleAsync(org, project, pat, buildId);
    }, orgOption, projectOption, patOption, buildIdArgument);

    return command;
}

Command CreateTestsCommand()
{
    var command = new Command("tests", "Get failed test results for a build");
    var buildIdArgument = new Argument<int>(
        name: "build-id",
        description: "The build ID to fetch failed test results for");

    command.AddArgument(buildIdArgument);
    command.SetHandler(async (org, project, pat, buildId) =>
    {
        await TestsCommand.HandleAsync(org, project, pat, buildId);
    }, orgOption, projectOption, patOption, buildIdArgument);

    return command;
}

Command CreateDiffCommand()
{
    var command = new Command("diff", "Compare test results between two builds");

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

    command.AddOption(goodBuildOption);
    command.AddOption(badBuildOption);
    command.SetHandler(async (org, project, pat, goodBuildId, badBuildId) =>
    {
        await DiffCommand.HandleAsync(org, project, pat, goodBuildId, badBuildId);
    }, orgOption, projectOption, patOption, goodBuildOption, badBuildOption);

    return command;
}

Command CreateQueuedCommand()
{
    var command = new Command("queued", "Show active (running/queued) builds for a definition");

    var definitionIdOption = new Option<int>(
        aliases: new[] { "--definition", "-d" },
        description: "Build definition ID to filter by")
    {
        IsRequired = true
    };
    var showAllOption = new Option<bool>(
        aliases: new[] { "--all", "-a" },
        description: "Show recent completed builds as well");

    command.AddOption(definitionIdOption);
    command.AddOption(showAllOption);
    command.SetHandler(async (org, project, pat, definitionId, showAll) =>
    {
        await QueuedCommand.HandleAsync(org, project, pat, definitionId, showAll);
    }, orgOption, projectOption, patOption, definitionIdOption, showAllOption);

    return command;
}

Command CreateBisectCommand()
{
    var command = new Command("bisect", "Find the commit that introduced a test failure");

    var goodBuildOption = new Option<int>(
        aliases: ["--good", "-g"],
        description: "Build ID of the known good build (test passes)")
    {
        IsRequired = true
    };
    var badBuildOption = new Option<int>(
        aliases: ["--bad", "-b"],
        description: "Build ID of the known bad build (test fails)")
    {
        IsRequired = true
    };
    var testNameOption = new Option<string>(
        aliases: ["--test", "-t"],
        description: "Fully qualified name of the test to track (or substring to match)")
    {
        IsRequired = true
    };
    var repoPathOption = new Option<string>(
        aliases: ["--repo", "-r"],
        description: "Path to the git repository (defaults to current directory)",
        getDefaultValue: () => Environment.CurrentDirectory);
    var manualOption = new Option<bool>(
        aliases: ["--manual", "-m"],
        description: "Don't auto-queue builds; just report what needs to be done");
    var pollIntervalOption = new Option<int>(
        aliases: ["--poll-interval"],
        description: "Seconds between polling for build completion",
        getDefaultValue: () => 300);

    command.AddOption(goodBuildOption);
    command.AddOption(badBuildOption);
    command.AddOption(testNameOption);
    command.AddOption(repoPathOption);
    command.AddOption(manualOption);
    command.AddOption(pollIntervalOption);

    command.SetHandler(async (context) =>
    {
        var org = context.ParseResult.GetValueForOption(orgOption)!;
        var project = context.ParseResult.GetValueForOption(projectOption)!;
        var pat = context.ParseResult.GetValueForOption(patOption)!;
        var goodBuildId = context.ParseResult.GetValueForOption(goodBuildOption);
        var badBuildId = context.ParseResult.GetValueForOption(badBuildOption);
        var testName = context.ParseResult.GetValueForOption(testNameOption)!;
        var repoPath = context.ParseResult.GetValueForOption(repoPathOption)!;
        var manual = context.ParseResult.GetValueForOption(manualOption);
        var pollInterval = context.ParseResult.GetValueForOption(pollIntervalOption);

        await BisectCommand.HandleAsync(context, org, project, pat,
            goodBuildId, badBuildId, testName, repoPath, manual, pollInterval);
    });

    return command;
}

