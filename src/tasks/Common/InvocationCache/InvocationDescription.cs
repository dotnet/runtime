// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;

namespace Microsoft.NET.Build.Tasks;

internal sealed class InvocationDescription
{
    private readonly InvocationPathMap? _pathMap;

    internal InvocationInputs Inputs { get; }
    internal string[] Context { get; }
    internal InvocationOutputs Outputs { get; }
    internal string WorkingDirectory { get; }

    internal InvocationDescription(InvocationInputs inputs, string[] context, InvocationOutputs outputs,
        string? workingDirectory = null, InvocationPathMap? pathMap = null)
    {
        _pathMap = pathMap;
        Inputs = pathMap is null ? inputs : inputs.Map(pathMap);
        Context = context;
        Outputs = pathMap is null ? outputs : outputs.Map(pathMap);
        WorkingDirectory = Path.GetFullPath(workingDirectory ?? Environment.CurrentDirectory);
        if (pathMap is not null)
            WorkingDirectory = pathMap.Map(WorkingDirectory);
    }

    internal string ResolvePath(string identity) => _pathMap is null ? identity : _pathMap.Resolve(identity);
}
