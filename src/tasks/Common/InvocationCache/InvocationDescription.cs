// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;

namespace Microsoft.NET.Build.Tasks;

internal sealed class InvocationDescription
{
    private readonly Func<string, string>? _pathMap;

    internal InvocationInputs Inputs { get; }
    internal string[] Context { get; }
    internal InvocationOutputs Outputs { get; }
    internal string WorkingDirectory { get; }

    internal InvocationDescription(InvocationInputs inputs, string[] context, InvocationOutputs outputs,
        string? workingDirectory = null, Func<string, string>? pathMap = null)
    {
        Inputs = inputs;
        Context = context;
        Outputs = outputs;
        WorkingDirectory = Path.GetFullPath(workingDirectory ?? Environment.CurrentDirectory);
        _pathMap = pathMap;
    }

    internal string GetPathIdentity(string path) => _pathMap is null ? path : _pathMap(path);
}
