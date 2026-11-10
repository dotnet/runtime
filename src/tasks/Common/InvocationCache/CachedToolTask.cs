// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks;

/// <summary>
/// Runs cacheable tools and replays captured diagnostics through normal <see cref="ToolTask"/> logging.
/// </summary>
public abstract class CachedToolTask : ToolTask
{
    private List<string>? _capturedStandardOutput;
    private List<string>? _capturedStandardError;

    private protected abstract InvocationCacheConfiguration CacheConfiguration { get; }

    private protected abstract InvocationDescription? DescribeInvocation(
        string pathToTool, string responseFileCommands, string commandLineCommands);

    protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
    {
        var result = new InvocationResult(-1);
        try
        {
            InvocationCache? cache = InvocationCache.TryCreateFromEnvironment(Log, CacheConfiguration);
            if (cache is not null &&
                DescribeInvocation(pathToTool, responseFileCommands, commandLineCommands) is { } invocation &&
                cache.TryExecuteCached(invocation, ExecuteAndCapture, out InvocationResult cachedResult))
            {
                result = cachedResult;
            }
            else
            {
                result = new InvocationResult(ExecuteToolCore(pathToTool, responseFileCommands, commandLineCommands));
            }
        }
        finally
        {
            // Execution may throw before returning its captured output.
            IReadOnlyList<string> standardOutput = _capturedStandardOutput ?? result.StandardOutput;
            IReadOnlyList<string> standardError = _capturedStandardError ?? result.StandardError;
            _capturedStandardOutput = null;
            _capturedStandardError = null;
            ReplayOutput(standardOutput, standardError);
        }

        return result.ExitCode == 0 && base.HasLoggedErrors ? -1 : result.ExitCode;

        InvocationResult ExecuteAndCapture()
        {
            var standardOutput = new List<string>();
            var standardError = new List<string>();
            _capturedStandardOutput = standardOutput;
            _capturedStandardError = standardError;
            int exitCode = ExecuteToolCore(pathToTool, responseFileCommands, commandLineCommands);
            return new InvocationResult(exitCode, standardOutput, standardError);
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
        if (_capturedStandardOutput is { } standardOutput && _capturedStandardError is { } standardError)
        {
            // Replace live logging, but preserve ToolTask's EOF notifications.
            process.OutputDataReceived -= ReceiveStandardOutputData;
            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                    standardOutput.Add(args.Data);
                else
                    ReceiveStandardOutputData(process, args);
            };
            process.ErrorDataReceived -= ReceiveStandardErrorData;
            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                    standardError.Add(args.Data);
                else
                    ReceiveStandardErrorData(process, args);
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
