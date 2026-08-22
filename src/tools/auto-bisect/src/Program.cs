using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using AutoBisect.Commands;

var rootCommand = new RootCommand("Azure DevOps bisect tool for finding test regressions");

// Global options
var orgOption = new Option<string>("--organization")
{
    Description = "Azure DevOps organization name",
    Required = true,
};
orgOption.Aliases.Add("-o");

var projectOption = new Option<string>("--project")
{
    Description = "Azure DevOps project name",
    Required = true,
};
projectOption.Aliases.Add("-p");

var patOption = new Option<string>("--pat")
{
    Description = "Personal Access Token (or set AZDO_PAT environment variable)",
    DefaultValueFactory = _ => Environment.GetEnvironmentVariable("AZDO_PAT") ?? "",
};
patOption.Validators.Add(result =>
{
    var value = result.GetValueOrDefault<string>();
    if (string.IsNullOrWhiteSpace(value))
    {
        result.AddError("PAT is required. Use --pat or set AZDO_PAT environment variable.");
    }
});

orgOption.Recursive = true;
projectOption.Recursive = true;
patOption.Recursive = true;

rootCommand.Options.Add(orgOption);
rootCommand.Options.Add(projectOption);
rootCommand.Options.Add(patOption);

// Add commands
rootCommand.Subcommands.Add(CreateBuildCommand());
rootCommand.Subcommands.Add(CreateTestsCommand());
rootCommand.Subcommands.Add(CreateDiffCommand());
rootCommand.Subcommands.Add(CreateQueuedCommand());
rootCommand.Subcommands.Add(CreateBisectCommand());

return rootCommand.Parse(args).InvokeAsync().GetAwaiter().GetResult();

Command CreateBuildCommand()
{
    var command = new Command("build", "Get information about a build");
    var buildIdArgument = new Argument<int>("build-id") { Description = "The build ID to fetch" };

    command.Arguments.Add(buildIdArgument);
    command.SetAction(parseResult =>
    {
        var buildId = parseResult.GetValue(buildIdArgument);
        return BuildCommand.HandleAsync(
            parseResult.GetValue(orgOption)!,
            parseResult.GetValue(projectOption)!,
            parseResult.GetValue(patOption)!,
            buildId);
    });

    return command;
}

Command CreateTestsCommand()
{
    var command = new Command("tests", "Get failed test results for a build");
    var buildIdArgument = new Argument<int>("build-id")
    {
        Description = "The build ID to fetch failed test results for",
    };

    command.Arguments.Add(buildIdArgument);
    command.SetAction(parseResult =>
    {
        var buildId = parseResult.GetValue(buildIdArgument);
        return TestsCommand.HandleAsync(
            parseResult.GetValue(orgOption)!,
            parseResult.GetValue(projectOption)!,
            parseResult.GetValue(patOption)!,
            buildId);
    });

    return command;
}

Command CreateDiffCommand()
{
    var command = new Command("diff", "Compare test results between two builds");

    var goodBuildOption = new Option<int>("--good")
    {
        Description = "Build ID of the known good build",
        Required = true,
    };
    goodBuildOption.Aliases.Add("-g");

    var badBuildOption = new Option<int>("--bad")
    {
        Description = "Build ID of the known bad build",
        Required = true,
    };
    badBuildOption.Aliases.Add("-b");

    command.Options.Add(goodBuildOption);
    command.Options.Add(badBuildOption);
    command.SetAction(parseResult =>
    {
        return DiffCommand.HandleAsync(
            parseResult.GetValue(orgOption)!,
            parseResult.GetValue(projectOption)!,
            parseResult.GetValue(patOption)!,
            parseResult.GetValue(goodBuildOption),
            parseResult.GetValue(badBuildOption));
    });

    return command;
}

Command CreateQueuedCommand()
{
    var command = new Command("queued", "Show active (running/queued) builds for a definition");

    var definitionIdOption = new Option<int>("--definition")
    {
        Description = "Build definition ID to filter by",
        Required = true,
    };
    definitionIdOption.Aliases.Add("-d");

    var showAllOption = new Option<bool>("--all")
    {
        Description = "Show recent completed builds as well",
    };
    showAllOption.Aliases.Add("-a");

    command.Options.Add(definitionIdOption);
    command.Options.Add(showAllOption);
    command.SetAction(parseResult =>
    {
        return QueuedCommand.HandleAsync(
            parseResult.GetValue(orgOption)!,
            parseResult.GetValue(projectOption)!,
            parseResult.GetValue(patOption)!,
            parseResult.GetValue(definitionIdOption),
            parseResult.GetValue(showAllOption));
    });

    return command;
}

Command CreateBisectCommand()
{
    var command = new Command("bisect", "Find the commit that introduced a test failure");

    var goodBuildOption = new Option<int>("--good")
    {
        Description = "Build ID of the known good build (test passes)",
        Required = true,
    };
    goodBuildOption.Aliases.Add("-g");

    var badBuildOption = new Option<int>("--bad")
    {
        Description = "Build ID of the known bad build (test fails)",
        Required = true,
    };
    badBuildOption.Aliases.Add("-b");

    var testNameOption = new Option<string>("--test")
    {
        Description = "Fully qualified name of the test to track (or substring to match)",
        Required = true,
    };
    testNameOption.Aliases.Add("-t");

    var repoPathOption = new Option<string>("--repo")
    {
        Description = "Path to the git repository (defaults to current directory)",
        DefaultValueFactory = _ => Environment.CurrentDirectory,
    };
    repoPathOption.Aliases.Add("-r");

    var manualOption = new Option<bool>("--manual")
    {
        Description = "Don't auto-queue builds; just report what needs to be done",
    };
    manualOption.Aliases.Add("-m");

    var pollIntervalOption = new Option<int>("--poll-interval")
    {
        Description = "Seconds between polling for build completion",
        DefaultValueFactory = _ => 300,
    };

    command.Options.Add(goodBuildOption);
    command.Options.Add(badBuildOption);
    command.Options.Add(testNameOption);
    command.Options.Add(repoPathOption);
    command.Options.Add(manualOption);
    command.Options.Add(pollIntervalOption);

    command.SetAction(
        (parseResult, cancellationToken) =>
        {
            return BisectCommand.HandleAsync(
                cancellationToken,
                parseResult.GetValue(orgOption)!,
                parseResult.GetValue(projectOption)!,
                parseResult.GetValue(patOption)!,
                parseResult.GetValue(goodBuildOption),
                parseResult.GetValue(badBuildOption),
                parseResult.GetValue(testNameOption)!,
                parseResult.GetValue(repoPathOption)!,
                parseResult.GetValue(manualOption),
                parseResult.GetValue(pollIntervalOption)
            );
        }
    );

    return command;
}
