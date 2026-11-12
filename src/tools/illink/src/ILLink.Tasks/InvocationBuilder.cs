// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.NET.Build.Tasks;

namespace ILLink.Tasks;

internal sealed class InvocationBuilder
{
    private enum ArgumentKind
    {
        Text,
        Path,
        ExtraArgs
    }

    private readonly List<(string Value, string Path, ArgumentKind Kind)> _arguments = new();
    private readonly string _originalToolPath;

    internal SourceRootPathMap Paths { get; private set; }
    internal string BypassReason { get; private set; }
    internal string ToolPath { get; }
    internal string WorkingDirectory { get; }
    internal string OutputDirectory { get; private set; }
    internal List<string> Files { get; } = new();
    internal List<(string Path, bool CheckOutputSymbols)> Assemblies { get; } = new();
    internal List<(string Option, string Path)> ExtraPaths { get; }
    internal string CommandLine => Quote(Paths?.ExecutionRoot is not null ? Paths.MapForExecution(ToolPath) : _originalToolPath);
    internal string CommandIdentity => Quote(Paths is not null ? ToolPath : _originalToolPath);
    internal string ResponseIdentity => Render(Paths is not null ? static path => path : null);

    internal InvocationBuilder(string toolPath, SourceRootPathMap paths,
        List<(string Option, string Path)> extraPaths, string bypassReason = null)
    {
        _originalToolPath = toolPath;
        Paths = paths;
        ExtraPaths = extraPaths;
        BypassReason = bypassReason;
        ToolPath = NormalizePath(toolPath);
        WorkingDirectory = NormalizePath(paths?.ExecutionRoot ?? paths?.WorkingDirectory ?? Environment.CurrentDirectory);
    }

    internal InvocationBuilder Append(string value)
    {
        _arguments.Add((value, null, ArgumentKind.Text));
        return this;
    }

    internal InvocationBuilder Append(char value) => Append(value.ToString());

    internal InvocationBuilder AppendLine(string value = null) => Append(value + Environment.NewLine);

    internal InvocationBuilder AppendFile(string path)
    {
        Files.Add(AppendPath(path));
        return this;
    }

    internal InvocationBuilder AppendAssembly(string path, bool checkOutputSymbols = true)
    {
        Assemblies.Add((AppendPath(path), checkOutputSymbols));
        return this;
    }

    internal InvocationBuilder AppendOutputDirectory(string path)
    {
        OutputDirectory = AppendPath(path);
        return this;
    }

    private string AppendPath(string path)
    {
        string normalized = NormalizePath(path);
        _arguments.Add((path, normalized, ArgumentKind.Path));
        return normalized;
    }

    internal void AppendExtraArgs(string extraArgs)
    {
        if (extraArgs is null)
            return;
        _arguments.Add((extraArgs, null, ArgumentKind.ExtraArgs));
        if (Paths is null || string.IsNullOrWhiteSpace(extraArgs))
            return;
        for (int i = 0; i < ExtraPaths.Count; i++)
        {
            var argument = ExtraPaths[i];
            ExtraPaths[i] = (argument.Option, NormalizePath(argument.Path));
        }
    }

    private string NormalizePath(string path)
    {
        try
        {
            return Paths?.Map(path) ?? path;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Paths = null;
            BypassReason = ex.Message;
            return path;
        }
    }

    private string Render(Func<string, string> map)
    {
        var args = new StringBuilder();
        foreach (var argument in _arguments)
        {
            switch (argument.Kind)
            {
                case ArgumentKind.Text:
                    args.Append(argument.Value);
                    break;
                case ArgumentKind.Path:
                    args.Append(Quote(map is null ? argument.Value : map(argument.Path)));
                    break;
                case ArgumentKind.ExtraArgs:
                    if (map is null)
                    {
                        args.AppendLine(argument.Value);
                    }
                    else if (!string.IsNullOrWhiteSpace(argument.Value))
                    {
                        args.AppendLine("--ignore-link-attributes true");
                        foreach (var extra in ExtraPaths)
                            args.Append(extra.Option).Append(' ').AppendLine(Quote(map(extra.Path)));
                    }
                    break;
            }
        }
        return args.ToString();
    }

    public override string ToString() => Render(Paths?.ExecutionRoot is not null ? Paths.MapForExecution : null);

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
