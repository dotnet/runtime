// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.NET.Build.Tasks;

internal sealed class InvocationDescription
{
    internal InvocationInputs Inputs { get; }
    internal string[] Context { get; }
    internal InvocationOutputs Outputs { get; }

    internal InvocationDescription(InvocationInputs inputs, string[] context, InvocationOutputs outputs)
    {
        Inputs = inputs;
        Context = context;
        Outputs = outputs;
    }
}
