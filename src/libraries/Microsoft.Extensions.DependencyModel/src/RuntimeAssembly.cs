// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                throw new ArgumentException(null, nameof(assemblyName));
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(null, nameof(path));
            }
            _assemblyName = assemblyName;
            Path = path;
        }

        public AssemblyName Name => new(_assemblyName);

        public string Path { get; }

        public static RuntimeAssembly Create(string path)
        {
            string assemblyName = System.IO.Path.GetFileNameWithoutExtension(path);
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
