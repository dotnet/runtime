// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Internal;
using Xunit.Runner.Common;
using Xunit.Runner.InProc.SystemConsole;
using Xunit.Sdk;

// @TODO medium-to-longer term, we should try to get rid of the special-unicorn-single-file runner in favor of making the real runner work for single file.
// https://github.com/dotnet/runtime/issues/70432
public static class SingleFileTestRunner
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var testAssembly = typeof(SingleFileTestRunner).Assembly;
        Console.WriteLine("Running assembly:" + testAssembly.FullName);

        // The current RemoteExecutor implementation is not compatible with the SingleFileTestRunner.
        Environment.SetEnvironmentVariable("DOTNET_REMOTEEXECUTOR_SUPPORTED", "0");

#if TEST_READY_TO_RUN_COMPILED
        Environment.SetEnvironmentVariable("TEST_READY_TO_RUN_MODE", "1");
#endif

        // Use Assembly.Location which now returns Environment.ProcessPath in NativeAOT
        var processPath = testAssembly.Location;

        string? xmlResultFileName = null;
        var excludedTraits = new Dictionary<string, HashSet<string>>();

        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i].Equals("-trait-", StringComparison.OrdinalIgnoreCase) ||
                 args[i].Equals("-notrait", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                var parts = args[++i].Split('=', 2);
                if (parts.Length == 2)
                {
                    if (!excludedTraits.TryGetValue(parts[0], out var values))
                        excludedTraits[parts[0]] = values = new HashSet<string>();
                    values.Add(parts[1]);
                }
            }
            else if (args[i].Equals("-xml", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                xmlResultFileName = args[++i];
            }
        }

        var project = new XunitProject();
        var targetFramework = testAssembly.GetTargetFramework();
        var projectAssembly = new XunitProjectAssembly(
            project, processPath, new AssemblyMetadata(3, targetFramework))
        {
            Assembly = testAssembly
        };
        projectAssembly.Configuration.PreEnumerateTheories = false;

        foreach (var (key, values) in excludedTraits)
            foreach (var value in values)
                projectAssembly.Configuration.Filters.AddExcludedTraitFilter(key, value);

        if (xmlResultFileName is not null)
            project.Configuration.Output.Add("xml", xmlResultFileName);

        project.Add(projectAssembly);
        project.RunnerReporter = new DefaultRunnerReporter();

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        var consoleHelper = new ConsoleHelper(Console.In, Console.Out);
        var logger = new ConsoleRunnerLogger(useColors: true, useAnsiColor: false, consoleHelper, waitForAcknowledgment: false);

        var pipelineStartup = await ProjectAssemblyRunner.InvokePipelineStartup(testAssembly, null);

        var reporter = project.RunnerReporter;
        var reporterMessageHandler = await reporter.CreateMessageHandler(logger, null);

        int failCount;
        try
        {
            consoleHelper.WriteLine(ProjectAssemblyRunner.Banner);

            var projectRunner = new ProjectAssemblyRunner(
                testAssembly, AutomatedMode.Off,
                NullSourceInformationProvider.Instance, cts);

            failCount = await projectRunner.Run(
                projectAssembly, reporterMessageHandler, null, logger, pipelineStartup);
        }
        finally
        {
            if (pipelineStartup is not null)
                await pipelineStartup.StopAsync();
            await reporterMessageHandler.DisposeAsync();
        }

        return failCount > 0 ? 1 : 0;
    }
}
