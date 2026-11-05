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
    private InvocationCache.PendingInvocation? _pendingInvocation;

    private protected abstract InvocationCacheConfiguration CacheConfiguration { get; }

    private protected abstract InvocationDescription? DescribeInvocation(
        string pathToTool, string responseFileCommands, string commandLineCommands);

    // Defer ToolTask's logging-based exit-code adjustment until after cache completion.
    protected override bool HasLoggedErrors => _pendingInvocation is null && base.HasLoggedErrors;

    protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
    {
        try
        {
            _pendingInvocation = InvocationCache.PrepareInvocation(Log, CacheConfiguration,
                () => DescribeInvocation(pathToTool, responseFileCommands, commandLineCommands),
                ReplayOutput, out InvocationResult? restoredResult);
            InvocationResult result;
            if (restoredResult is { } restored)
            {
                result = restored;
            }
            else
            {
                int exitCode = ExecuteToolCore(pathToTool, responseFileCommands, commandLineCommands);
                result = _pendingInvocation?.Complete(exitCode) ?? new InvocationResult(exitCode);
            }

            return result.ExitCode == 0 && base.HasLoggedErrors ? -1 : result.ExitCode;
        }
        finally
        {
            _pendingInvocation = null;
        }
    }

    /// <summary>
    /// Executes the tool on cache misses and bypasses, including any tool-specific output preparation.
    /// </summary>
    /// <param name="pathToTool">The path to the tool executable.</param>
    /// <param name="responseFileCommands">The commands to write to the response file.</param>
    /// <param name="commandLineCommands">The commands to pass on the command line.</param>
    /// <returns>The tool execution exit code.</returns>
    protected virtual int ExecuteToolCore(string pathToTool, string responseFileCommands, string commandLineCommands)
        => base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

    protected override Process StartToolProcess(Process process)
    {
        if (_pendingInvocation is { } pending)
        {
            // Capture before ToolTask applies stream logging policy, including LogStandardErrorAsError.
            // Each stream has one reader; ToolTask drains both before execution returns.
            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                    pending.StandardOutput.Add(args.Data);
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                    pending.StandardError.Add(args.Data);
            };
        }

        return base.StartToolProcess(process);
    }

    private void ReplayOutput(IReadOnlyList<string> standardOutput, IReadOnlyList<string> standardError)
    {
        foreach (string line in standardOutput)
            LogEventsFromTextOutput(line, StandardOutputImportanceToUse);
        foreach (string line in standardError)
        {
            if (LogStandardErrorAsError)
                Log.LogError(line);
            else
                LogEventsFromTextOutput(line, StandardErrorImportanceToUse);
        }
    }
}
