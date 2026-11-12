// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.NET.Build.Tasks;

internal sealed class InvocationDescription
{
    private readonly SourceRootPathMap? _pathMap;

    internal InvocationInputs Inputs { get; }
    internal string[] Context { get; }
    internal InvocationOutputs Outputs { get; }
    internal string WorkingDirectory { get; }

    internal InvocationDescription(InvocationInputs inputs, string[] context, InvocationOutputs outputs,
        string? workingDirectory = null, SourceRootPathMap? pathMap = null)
    {
        _pathMap = pathMap;
        Inputs = inputs;
        Context = context;
        Outputs = outputs;
        WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory;
    }

    internal string ResolvePath(string identity) => _pathMap is null ? identity : _pathMap.Resolve(identity);
}
