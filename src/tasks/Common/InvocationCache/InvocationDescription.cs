// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;

namespace Microsoft.NET.Build.Tasks;

internal sealed class InvocationDescription
{
    internal InvocationInputs Inputs { get; }
    internal string[] Context { get; }
    internal InvocationOutputs Outputs { get; }
    internal string WorkingDirectory { get; }
    internal string WorkingDirectoryIdentity { get; }
    internal string[] InputIdentities { get; }
    internal string? OutputDirectoryIdentity { get; }
    internal string[] OutputFileIdentities { get; }

    internal InvocationDescription(InvocationInputs inputs, string[] context, InvocationOutputs outputs,
        string? workingDirectory = null, Func<string, string>? pathMap = null)
    {
        Inputs = inputs;
        Context = context;
        Outputs = outputs;
        WorkingDirectory = Path.GetFullPath(workingDirectory ?? Environment.CurrentDirectory);
        WorkingDirectoryIdentity = Map(WorkingDirectory);
        InputIdentities = Array.ConvertAll(inputs.Files, Map);
        OutputDirectoryIdentity = outputs.DirectoryPath is null ? null : Map(outputs.DirectoryPath);
        OutputFileIdentities = Array.ConvertAll(outputs.Files, Map);

        string Map(string path) => pathMap is null ? path : pathMap(path);
    }
}
