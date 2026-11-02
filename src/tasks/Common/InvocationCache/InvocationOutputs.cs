// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;

namespace Microsoft.NET.Build.Tasks;

internal sealed class InvocationOutputs
{
    internal string? DirectoryPath { get; }
    internal string[] Files { get; }

    private InvocationOutputs(string? directoryPath, string[] files)
    {
        DirectoryPath = directoryPath;
        Files = files;
    }

    internal static InvocationOutputs FromDirectory(string path) =>
        new InvocationOutputs(Path.GetFullPath(path), Array.Empty<string>());

    internal static InvocationOutputs FromFiles(params string[] files) =>
        new InvocationOutputs(null, Array.ConvertAll(files, Path.GetFullPath));
}
