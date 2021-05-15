// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
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

        [RequiresUnreferencedCode("The trimmer might remove assemblies that are loaded by this class", Url = "https://aka.ms/dotnet-illink/nativehost")]
        public IsolatedComponentLoadContext(string componentAssemblyPath) : base($"IsolatedComponentLoadContext({componentAssemblyPath})")
        {
            _resolver = new AssemblyDependencyResolver(componentAssemblyPath);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The trimmer warning is added to the constructor of this class since this method " +
                "is a virtual one.")]
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
