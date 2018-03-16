// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeFile
    {
        public RuntimeFile(string path, string assemblyVersion, string fileVersion)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(nameof(path));
            }

            Path = path;
            AssemblyVersion = assemblyVersion;
            FileVersion = fileVersion;
        }

        public string Path { get; }

        public string AssemblyVersion { get; }

        public string FileVersion { get; }
    }
}
