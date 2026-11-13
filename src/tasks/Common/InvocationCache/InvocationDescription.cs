// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.NET.Build.Tasks;

internal sealed class InvocationDescription
{
    private readonly SourceRootPathMap? _pathMap;

    internal InvocationInputs Inputs { get; }
    internal InvocationOutputs Outputs { get; }
    internal string?[] Context { get; }

    internal InvocationDescription(InvocationInputs inputs, string?[] context, InvocationOutputs outputs,
        SourceRootPathMap? pathMap = null)
    {
        _pathMap = pathMap;
        Context = context;
        Inputs = inputs;
        Outputs = outputs;
    }

    internal string MapPath(string path) => _pathMap is null ? path : _pathMap.Map(path);
}
