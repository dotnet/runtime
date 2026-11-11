// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ILLink.Tasks;

internal sealed class InvocationBuilder
{
    // Keep an untouched fallback alongside execution arguments and logical cache identities.
    private readonly StringBuilder _original = new();
    private readonly StringBuilder _execution = new();
    private readonly StringBuilder _identity = new();
    private readonly string _originalCommand;
    private string _executionCommand;
    private string _commandIdentity;

    internal SourceRootPathMap Paths { get; private set; }
    internal string BypassReason { get; private set; }
    internal string ToolPath { get; }
    internal string OutputDirectory { get; private set; }
    internal List<string> Files { get; } = new();
    internal List<(string Path, bool CheckOutputSymbols)> Assemblies { get; } = new();
    internal List<(string Option, string Path)> ExtraPaths { get; }
    internal string CommandLine => Paths?.ExecutionRoot is not null ? _executionCommand : _originalCommand;
    internal string CommandIdentity => Paths is not null ? _commandIdentity : _originalCommand;
    internal string ResponseIdentity => Paths is not null ? _identity.ToString() : _original.ToString();

    internal InvocationBuilder(string toolPath, SourceRootPathMap paths,
        List<(string Option, string Path)> extraPaths, string bypassReason = null)
    {
        ToolPath = toolPath;
        Paths = paths;
        ExtraPaths = extraPaths;
        BypassReason = bypassReason;
        _originalCommand = Quote(toolPath);
        if (Paths is not null)
        {
            try
            {
                _executionCommand = Quote(Paths.MapForExecution(toolPath));
                _commandIdentity = Quote(Paths.Map(toolPath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                DisableNormalization(ex.Message);
            }
        }
    }

    internal InvocationBuilder Append(string value)
    {
        _original.Append(value);
        _execution.Append(value);
        _identity.Append(value);
        return this;
    }

    internal InvocationBuilder Append(char value)
    {
        _original.Append(value);
        _execution.Append(value);
        _identity.Append(value);
        return this;
    }

    internal InvocationBuilder AppendLine(string value = null)
    {
        _original.AppendLine(value);
        _execution.AppendLine(value);
        _identity.AppendLine(value);
        return this;
    }

    internal InvocationBuilder AppendFile(string path)
    {
        Files.Add(path);
        return AppendPath(path);
    }

    internal InvocationBuilder AppendAssembly(string path, bool checkOutputSymbols = true)
    {
        Assemblies.Add((path, checkOutputSymbols));
        return AppendPath(path);
    }

    internal InvocationBuilder AppendOutputDirectory(string path)
    {
        OutputDirectory = path;
        return AppendPath(path);
    }

    internal InvocationBuilder AppendPath(string path)
    {
        _original.Append(Quote(path));
        if (Paths is not null)
        {
            try
            {
                _execution.Append(Quote(Paths.MapForExecution(path)));
                _identity.Append(Quote(Paths.Map(path)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                DisableNormalization(ex.Message);
            }
        }
        return this;
    }

    internal void AppendExtraArgs(string extraArgs)
    {
        if (extraArgs is null)
            return;
        _original.AppendLine(extraArgs);
        if (Paths is null || string.IsNullOrWhiteSpace(extraArgs))
            return;
        _execution.AppendLine("--ignore-link-attributes true");
        _identity.AppendLine("--ignore-link-attributes true");
        try
        {
            foreach (var argument in ExtraPaths)
            {
                _execution.Append(argument.Option).Append(' ').AppendLine(Quote(Paths.MapForExecution(argument.Path)));
                _identity.Append(argument.Option).Append(' ').AppendLine(Quote(Paths.Map(argument.Path)));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            DisableNormalization(ex.Message);
        }
    }

    private void DisableNormalization(string reason)
    {
        Paths = null;
        BypassReason = reason;
    }

    public override string ToString() => Paths?.ExecutionRoot is not null ? _execution.ToString() : _original.ToString();

    internal static string Quote(string path)
    {
        int trailingBackslashes = path.Length - path.TrimEnd('\\').Length;
        // ToolTask's Unix normalization preserves the last backslash before a quote as an escape.
        if (Path.DirectorySeparatorChar == '/')
            return $"\"{path.TrimEnd('\\')}{new string('/', trailingBackslashes)}\"";

        // Double trailing backslashes so they do not escape the closing quote on Windows.
        return $"\"{path}{new string('\\', trailingBackslashes)}\"";
    }
}
