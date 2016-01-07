// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeAssembly
    {
        public RuntimeAssembly(string path)
            : this(new AssemblyName(System.IO.Path.GetFileNameWithoutExtension(path)), path)
        {
        }

        public RuntimeAssembly(AssemblyName name, string path)
        {
            Name = name;
            Path = path;
        }

        public AssemblyName Name { get; }

        public string Path { get; }
    }
}