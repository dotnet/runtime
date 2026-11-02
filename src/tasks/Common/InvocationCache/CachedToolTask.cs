// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks;

/// <summary>
/// Runs cacheable tools while preserving normal <see cref="ToolTask"/> logging and replaying restored diagnostics.
/// </summary>
public abstract class CachedToolTask : ToolTask
{
    private List<string>? _standardOutput;
    private List<string>? _standardError;

    private protected abstract InvocationCacheConfiguration CacheConfiguration { get; }

    private protected abstract InvocationDescription? DescribeInvocation(
        string pathToTool, string responseFileCommands, string commandLineCommands);

    protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        => InvocationCache.TryExecuteCached(Log, CacheConfiguration,
            () => DescribeInvocation(pathToTool, responseFileCommands, commandLineCommands),
            () => ExecuteAndCaptureOutput(pathToTool, responseFileCommands, commandLineCommands),
            ReplayOutput, out InvocationResult result)
            ? result.ExitCode
            : ExecuteToolCore(pathToTool, responseFileCommands, commandLineCommands);

    /// <summary>
    /// Executes the tool on cache misses and bypasses, including any tool-specific output preparation.
    /// </summary>
    /// <param name="pathToTool">The path to the tool executable.</param>
    /// <param name="responseFileCommands">The commands to write to the response file.</param>
    /// <param name="commandLineCommands">The commands to pass on the command line.</param>
    /// <returns>The tool execution exit code.</returns>
    protected virtual int ExecuteToolCore(string pathToTool, string responseFileCommands, string commandLineCommands)
        => base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

    private InvocationResult ExecuteAndCaptureOutput(
        string pathToTool, string responseFileCommands, string commandLineCommands)
    {
        _standardOutput = new List<string>();
        _standardError = new List<string>();
        try
        {
            int exitCode = ExecuteToolCore(pathToTool, responseFileCommands, commandLineCommands);
            return new InvocationResult(exitCode, _standardOutput, _standardError);
        }
        finally
        {
            _standardOutput = null;
            _standardError = null;
        }
    }

    protected override Process StartToolProcess(Process process)
    {
        if (_standardOutput is { } standardOutput && _standardError is { } standardError)
        {
            // Capture before ToolTask applies stream logging policy, including LogStandardErrorAsError.
            // Each stream has one reader; ToolTask drains both before execution returns.
            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                    standardOutput.Add(args.Data);
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                    standardError.Add(args.Data);
            };
        }

        return base.StartToolProcess(process);
    }

    private InvocationResult ReplayOutput(InvocationResult result)
    {
        foreach (string line in result.StandardOutput)
            LogEventsFromTextOutput(line, StandardOutputImportanceToUse);
        foreach (string line in result.StandardError)
        {
            if (LogStandardErrorAsError)
                Log.LogError(line);
            else
                LogEventsFromTextOutput(line, StandardErrorImportanceToUse);
        }

        return new InvocationResult(Log.HasLoggedErrors ? -1 : result.ExitCode,
            result.StandardOutput, result.StandardError);
    }
}
