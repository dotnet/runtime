// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeFile
    {
        public RuntimeFile(string path, string? assemblyVersion, string? fileVersion)
            : this(path, assemblyVersion, fileVersion, null)
        { }

        public RuntimeFile(string path, string? assemblyVersion, string? fileVersion, string? localPath)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(null, nameof(path));
            }

            Path = path;
            AssemblyVersion = assemblyVersion;
            FileVersion = fileVersion;
            LocalPath = localPath;
        }

        // Depending on the source of the runtime file, this path may be relative to the
        // a referenced NuGet package's root or to the app/component root.
        public string Path { get; }

        public string? AssemblyVersion { get; }

        public string? FileVersion { get; }

        // Path relative to the app/component represented by the dependency context
        public string? LocalPath { get; }
    }
}
