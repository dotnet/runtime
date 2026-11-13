// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.NET.Build.Tasks;

internal sealed class InvocationDescription
{
    private readonly SourceRootPathMap? _pathMap;
    private readonly Func<Func<string, string>, string[]> _getContext;

    internal InvocationInputs Inputs { get; }
    internal InvocationOutputs Outputs { get; }
    internal string WorkingDirectory { get; }

    internal InvocationDescription(InvocationInputs inputs, string[] context, InvocationOutputs outputs,
        string? workingDirectory = null, SourceRootPathMap? pathMap = null)
        : this(inputs, _ => context, outputs, workingDirectory, pathMap)
    {
    }

    internal InvocationDescription(InvocationInputs inputs, Func<Func<string, string>, string[]> getContext,
        InvocationOutputs outputs, string? workingDirectory = null, SourceRootPathMap? pathMap = null)
    {
        _pathMap = pathMap;
        _getContext = getContext;
        Inputs = inputs;
        Outputs = outputs;
        WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory;
    }

    internal string MapPath(string path) => _pathMap is null ? path : _pathMap.Map(path);

    internal string[] GetContext(Func<string, string> mapPath) => _getContext(mapPath);
}
