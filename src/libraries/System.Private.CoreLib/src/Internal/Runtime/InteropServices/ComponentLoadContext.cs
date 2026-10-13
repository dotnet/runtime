// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Threading;

namespace Internal.Runtime.InteropServices
{
    /// <summary>
    /// A <see cref="ComponentLoadContext" /> is an AssemblyLoadContext that can be used to isolate managed components.
    /// It uses an <see cref="AssemblyDependencyResolver" /> per component to resolve dependencies within the ALC and not pollute the default ALC.
    ///</summary>
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("tvos")]
    [RequiresUnreferencedCode("The trimmer might remove assemblies that are loaded by this class", Url = "https://aka.ms/dotnet-illink/nativehost")]
    internal sealed class ComponentLoadContext : AssemblyLoadContext
    {
        private static readonly StringComparer s_pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        private AssemblyDependencyResolver[] _resolvers;
        private readonly HashSet<string> _componentAssemblyPaths;

        private ComponentLoadContext(string name, string componentAssemblyPath) : base(name)
        {
            _resolvers = [new AssemblyDependencyResolver(componentAssemblyPath)];
            _componentAssemblyPaths = new(s_pathComparer) { componentAssemblyPath };
        }

        internal static ComponentLoadContext CreateIsolated(string componentAssemblyPath) =>
            new($"IsolatedComponentLoadContext({componentAssemblyPath})", componentAssemblyPath);

        internal static ComponentLoadContext CreateNamed(string identifier, string componentAssemblyPath) =>
            new($"ComponentLoadContext({identifier})", componentAssemblyPath);

        internal void AddComponent(string componentAssemblyPath)
        {
            lock (_componentAssemblyPaths)
            {
                if (!_componentAssemblyPaths.Contains(componentAssemblyPath))
                {
                    AssemblyDependencyResolver resolver = new(componentAssemblyPath);
                    AssemblyDependencyResolver[] updatedResolvers = [.. _resolvers, resolver];
                    _componentAssemblyPaths.Add(componentAssemblyPath);
                    Volatile.Write(ref _resolvers, updatedResolvers);
                }
            }
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            foreach (AssemblyDependencyResolver resolver in Volatile.Read(ref _resolvers))
            {
                string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
                if (assemblyPath is not null)
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            foreach (AssemblyDependencyResolver resolver in Volatile.Read(ref _resolvers))
            {
                string? libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
                if (libraryPath is not null)
                {
                    return LoadUnmanagedDllFromPath(libraryPath);
                }
            }

            return IntPtr.Zero;
        }
    }
}
