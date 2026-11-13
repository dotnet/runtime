// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.NET.Build.Tasks;

internal readonly struct InvocationArgument
{
    internal string Value { get; }
    internal bool IsPath { get; }

    internal InvocationArgument(string value, bool isPath = false)
    {
        Value = value;
        IsPath = isPath;
    }
}

internal sealed class InvocationDescription
{
    private readonly SourceRootPathMap? _pathMap;

    internal InvocationInputs Inputs { get; }
    internal InvocationOutputs Outputs { get; }
    internal InvocationArgument[] Context { get; }

    internal InvocationDescription(InvocationInputs inputs, string[] context, InvocationOutputs outputs,
        SourceRootPathMap? pathMap = null)
        : this(inputs, Array.ConvertAll(context, static value => new InvocationArgument(value)), outputs, pathMap)
    {
    }

    internal InvocationDescription(InvocationInputs inputs, InvocationArgument[] context,
        InvocationOutputs outputs, SourceRootPathMap? pathMap = null)
    {
        _pathMap = pathMap;
        Context = context;
        Inputs = inputs;
        Outputs = outputs;
    }

    internal string MapPath(string path) => _pathMap is null ? path : _pathMap.Map(path);
}
