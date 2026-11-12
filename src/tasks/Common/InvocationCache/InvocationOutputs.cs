// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;

namespace Microsoft.NET.Build.Tasks;

internal sealed class InvocationOutputs
{
    private readonly SourceRootPathMap? _pathMap;

    internal string? DirectoryPath { get; }
    internal string[] Files { get; }

    internal InvocationOutputs(string? directoryPath, string[] files, SourceRootPathMap? pathMap = null)
    {
        DirectoryPath = directoryPath;
        Files = files;
        _pathMap = pathMap;
    }

    internal static InvocationOutputs FromDirectory(string path) =>
        new InvocationOutputs(Path.GetFullPath(path), Array.Empty<string>());

    internal static InvocationOutputs FromFiles(params string[] files) =>
        new InvocationOutputs(null, Array.ConvertAll(files, Path.GetFullPath));

    internal string ResolvePath(string identity) => _pathMap is null ? identity : _pathMap.Resolve(identity);
}
