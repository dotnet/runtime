// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Runtime.Loader;

namespace Internal.Runtime.InteropServices
{
    /// <summary>
    /// An <see cref="IsolatedComponentLoadContext" /> is an AssemblyLoadContext that can be used to isolate components such as COM components
    /// or IJW components loaded from native. It provides a load context that uses an <see cref="AssemblyDependencyResolver" /> to resolve the component's
    /// dependencies within the ALC and not pollute the default ALC.
    ///</summary>
    internal sealed class IsolatedComponentLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public IsolatedComponentLoadContext(string componentAssemblyPath) : base($"IsolatedComponentLoadContext({componentAssemblyPath})")
        {
            _resolver = new AssemblyDependencyResolver(componentAssemblyPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
