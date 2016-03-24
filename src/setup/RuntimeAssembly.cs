// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeAssembly
    {
        private const string NativeImageSufix = ".ni";
        private readonly string _assemblyName;

        public RuntimeAssembly(string assemblyName, string path)
        {
            if (string.IsNullOrEmpty(assemblyName))
            {
                throw new ArgumentException(nameof(assemblyName));
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(nameof(path));
            }
            _assemblyName = assemblyName;
            Path = path;
        }

        public AssemblyName Name => new AssemblyName(_assemblyName);

        public string Path { get; }

        public static RuntimeAssembly Create(string path)
        {
            var assemblyName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (assemblyName == null)
            {
                throw new ArgumentException($"Provided path has empty file name '{path}'", nameof(path));
            }

            if (assemblyName.EndsWith(NativeImageSufix))
            {
                assemblyName = assemblyName.Substring(0, assemblyName.Length - NativeImageSufix.Length);
            }
            return new RuntimeAssembly(assemblyName, path);
        }
    }
}