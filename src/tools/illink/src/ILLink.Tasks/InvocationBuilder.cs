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

    internal SourceRootPathMap Paths { get; private set; }
    internal string BypassReason { get; private set; }
    internal string ToolPath { get; }
    internal string WorkingDirectory { get; }
    internal string OutputDirectory { get; private set; }
    internal List<string> Files { get; } = new();
    internal List<(string Path, bool CheckOutputSymbols)> Assemblies { get; } = new();
    internal List<(string Option, string Path)> ExtraPaths { get; }
    internal string CommandLine => Quote(ToolPath);
    internal string GetCommandIdentity(Func<string, string> mapPath) => Quote(mapPath(ToolPath));
    internal string GetResponseIdentity(Func<string, string> mapPath) => Render(mapPath);

    internal InvocationBuilder(string toolPath, SourceRootPathMap paths,
        List<(string Option, string Path)> extraPaths, string bypassReason = null)
    {
        Paths = paths;
        ExtraPaths = extraPaths;
        BypassReason = bypassReason;
        WorkingDirectory = Environment.CurrentDirectory;
        ToolPath = NormalizePath(toolPath);
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
        _arguments.Add((normalized, ArgumentKind.Path));
        return normalized;
    }

    internal void AppendExtraArgs(string extraArgs)
    {
        if (extraArgs is null)
            return;
        _arguments.Add((extraArgs, ArgumentKind.ExtraArgs));
        if (ExtraPaths is null || string.IsNullOrWhiteSpace(extraArgs))
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
            return SourceRootPathMap.GetFullPath(path, WorkingDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Paths = null;
            BypassReason = ex.Message;
            return path;
        }
    }

    private string Render(Func<string, string> mapPath)
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
                    args.Append(Quote(mapPath(argument.Value)));
                    break;
                case ArgumentKind.ExtraArgs:
                    if (ExtraPaths is null)
                    {
                        args.AppendLine(argument.Value);
                    }
                    else if (!string.IsNullOrWhiteSpace(argument.Value))
                    {
                        args.AppendLine("--ignore-link-attributes true");
                        foreach (var extra in ExtraPaths)
                            args.Append(extra.Option).Append(' ').AppendLine(Quote(mapPath(extra.Path)));
                    }
                    break;
            }
        }
        return args.ToString();
    }

    public override string ToString() => Render(static path => path);

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
