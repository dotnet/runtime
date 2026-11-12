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

    private readonly List<(string Value, ArgumentKind Kind)> _arguments = new();
    private readonly string _workingDirectory = Environment.CurrentDirectory;

    internal SourceRootPathMap Paths { get; private set; }
    internal string BypassReason { get; private set; }
    internal string ToolPath { get; }
    internal string OutputDirectory { get; private set; }
    internal List<string> Files { get; } = new();
    internal List<(string Path, bool CheckOutputSymbols)> Assemblies { get; } = new();
    internal List<(string Option, string Path)> ExtraPaths { get; }
    internal string CommandLine => Quote(Paths?.ExecutionRoot is not null ? Paths.MapForExecution(GetFullPath(ToolPath)) : ToolPath);
    internal string CommandIdentity => Quote(Paths is not null ? Paths.Map(GetFullPath(ToolPath)) : ToolPath);
    internal string ResponseIdentity => Render(Paths is not null ? Paths.Map : null);

    internal InvocationBuilder(string toolPath, SourceRootPathMap paths,
        List<(string Option, string Path)> extraPaths, string bypassReason = null)
    {
        ToolPath = toolPath;
        Paths = paths;
        ExtraPaths = extraPaths;
        BypassReason = bypassReason;
        ValidatePath(toolPath);
    }

    internal InvocationBuilder Append(string value)
    {
        _arguments.Add((value, ArgumentKind.Text));
        return this;
    }

    internal InvocationBuilder Append(char value) => Append(value.ToString());

    internal InvocationBuilder AppendLine(string value = null) => Append(value + Environment.NewLine);

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
        _arguments.Add((path, ArgumentKind.Path));
        ValidatePath(path);
        return this;
    }

    internal void AppendExtraArgs(string extraArgs)
    {
        if (extraArgs is null)
            return;
        _arguments.Add((extraArgs, ArgumentKind.ExtraArgs));
        if (Paths is null || string.IsNullOrWhiteSpace(extraArgs))
            return;
        foreach (var argument in ExtraPaths)
            ValidatePath(argument.Path);
    }

    private void ValidatePath(string path)
    {
        try
        {
            Paths?.Map(GetFullPath(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Paths = null;
            BypassReason = ex.Message;
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
                    args.Append(Quote(map is null ? argument.Value : map(GetFullPath(argument.Value))));
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
                            args.Append(extra.Option).Append(' ').AppendLine(Quote(map(GetFullPath(extra.Path))));
                    }
                    break;
            }
        }
        return args.ToString();
    }

    public override string ToString() => Render(Paths?.ExecutionRoot is not null ? Paths.MapForExecution : null);

    private string GetFullPath(string path) =>
        Path.GetFullPath(string.IsNullOrEmpty(path) ? path : Path.Combine(_workingDirectory, path));

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
