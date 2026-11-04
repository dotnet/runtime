// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;

namespace Microsoft.NET.Build.Tasks;

internal sealed class InvocationInputs
{
    internal string[] Files { get; }

    private InvocationInputs(string[] files)
    {
        Files = files;
    }

    internal static InvocationInputs FromFiles(params string[] files) =>
        new InvocationInputs(Array.ConvertAll(files, Path.GetFullPath));
}
