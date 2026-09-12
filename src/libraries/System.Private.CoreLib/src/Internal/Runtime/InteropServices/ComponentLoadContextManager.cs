// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;

namespace Internal.Runtime.InteropServices
{
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("tvos")]
    internal static class ComponentLoadContextManager
    {
        internal const nint IsolatedContext = -1;

        private static readonly StringComparer s_pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        private static readonly Dictionary<string, ComponentLoadContext> s_isolatedLoadContextsByPath = new(s_pathComparer);
        private static readonly Dictionary<string, ComponentLoadContext> s_loadContextsByIdentifier = new();

        private static readonly HashSet<string> s_defaultResolversByPath = new(s_pathComparer);

        // Keep in sync with coreclr_load_context in src/native/corehost/coreclr_delegates.h.
        [StructLayout(LayoutKind.Sequential)]
        private struct LoadContext
        {
            public nuint Size;
            public IntPtr Identifier;
        }

        /// <summary>
        /// Gets the assembly load context for a component.
        /// </summary>
        /// <param name="loadContext">The load context specification.</param>
        /// <param name="componentAssemblyPath">The path to the component assembly.</param>
        /// <param name="cacheIsolatedContext">Whether to cache an isolated load context by component path.</param>
        /// <returns>The assembly load context for the component.</returns>
        /// <remarks>
        /// <paramref name="loadContext" /> supports the following values:
        ///   - <c>IntPtr.Zero</c>: Default ALC
        ///   - <see cref="IsolatedContext" />: ALC associated with the component path, reusing it if <paramref name="cacheIsolatedContext" /> is true
        ///   - A <c>coreclr_load_context*</c>: ALC shared by all components with the same identifier
        /// </remarks>
        [RequiresUnreferencedCode("The trimmer might remove assemblies that are loaded by this method", Url = "https://aka.ms/dotnet-illink/nativehost")]
        internal static unsafe AssemblyLoadContext Get(IntPtr loadContext, string componentAssemblyPath, bool cacheIsolatedContext = true)
        {
            if (loadContext == IntPtr.Zero)
            {
                AddResolverToDefaultContext(componentAssemblyPath);
                return AssemblyLoadContext.Default;
            }

            if (loadContext == IsolatedContext)
            {
                if (cacheIsolatedContext)
                {
                    lock (s_isolatedLoadContextsByPath)
                    {
                        if (!s_isolatedLoadContextsByPath.TryGetValue(componentAssemblyPath, out ComponentLoadContext? alc))
                        {
                            alc = ComponentLoadContext.CreateIsolated(componentAssemblyPath);
                            s_isolatedLoadContextsByPath.Add(componentAssemblyPath, alc);
                        }

                        return alc;
                    }
                }

                return ComponentLoadContext.CreateIsolated(componentAssemblyPath);
            }

            ref LoadContext context = ref *(LoadContext*)loadContext;
            ArgumentOutOfRangeException.ThrowIfLessThan(context.Size, (nuint)sizeof(LoadContext), nameof(loadContext));

            string identifier = Marshal.PtrToStringAuto(context.Identifier) ??
                throw new ArgumentNullException(nameof(loadContext));
            ArgumentException.ThrowIfNullOrEmpty(identifier, nameof(loadContext));

            lock (s_loadContextsByIdentifier)
            {
                if (!s_loadContextsByIdentifier.TryGetValue(identifier, out ComponentLoadContext? alc))
                {
                    alc = ComponentLoadContext.CreateNamed(identifier, componentAssemblyPath);
                    s_loadContextsByIdentifier.Add(identifier, alc);
                }
                else
                {
                    alc.AddComponent(componentAssemblyPath);
                }

                return alc;
            }
        }

        [RequiresUnreferencedCode("The trimmer might remove assemblies that are loaded by this method", Url = "https://aka.ms/dotnet-illink/nativehost")]
        internal static void AddResolverToDefaultContext(string componentAssemblyPath)
        {
            lock (s_defaultResolversByPath)
            {
                if (s_defaultResolversByPath.Contains(componentAssemblyPath))
                    return;

                AssemblyDependencyResolver resolver = new(componentAssemblyPath);
                AssemblyLoadContext.Default.Resolving +=
                    (context, assemblyName) =>
                    {
                        string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
                        return assemblyPath is not null
                            ? context.LoadFromAssemblyPath(assemblyPath)
                            : null;
                    };

                s_defaultResolversByPath.Add(componentAssemblyPath);
            }
        }
    }
}
