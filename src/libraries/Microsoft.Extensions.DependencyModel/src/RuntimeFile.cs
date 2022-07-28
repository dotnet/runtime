// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeFile
    {
        public RuntimeFile(string path, string? assemblyVersion, string? fileVersion)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(null, nameof(path));
            }

            Path = path;
            AssemblyVersion = assemblyVersion;
            FileVersion = fileVersion;
        }

        public string Path { get; }

        public string? AssemblyVersion { get; }

        public string? FileVersion { get; }
    }
}
