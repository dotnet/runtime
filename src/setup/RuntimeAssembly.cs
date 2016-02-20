// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeAssembly
    {
        private readonly string _assemblyName;

        public RuntimeAssembly(string path)
            : this(System.IO.Path.GetFileNameWithoutExtension(path), path)
        {
        }

        public RuntimeAssembly(string assemblyName, string path)
        {
            _assemblyName = assemblyName;
            Path = path;
        }

        public AssemblyName Name => new AssemblyName(_assemblyName);

        public string Path { get; }
    }
}