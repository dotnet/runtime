// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.NET.Build.Tasks;

internal sealed class InvocationDescription
{
    internal InvocationInputs Inputs { get; }
    internal string[] Context { get; }
    internal InvocationOutputs Outputs { get; }

    /// <summary>
    /// The directory the tool runs in, which resolves any relative paths in <see cref="Context"/>.
    /// </summary>
    internal string WorkingDirectory { get; }

    internal InvocationDescription(InvocationInputs inputs, string[] context, InvocationOutputs outputs,
        string? workingDirectory = null)
    {
        Inputs = inputs;
        Context = context;
        Outputs = outputs;
        WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory;
    }
}
