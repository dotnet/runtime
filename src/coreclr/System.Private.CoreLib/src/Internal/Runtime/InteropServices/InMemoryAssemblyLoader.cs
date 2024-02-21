// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;

namespace Internal.Runtime.InteropServices
{
    /// <summary>
    /// This class enables the .NET IJW host to load an in-memory module as a .NET assembly
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class InMemoryAssemblyLoader
    {
        [FeatureCheck(typeof(RequiresUnreferencedCodeAttribute))]
        private static bool IsSupported { get; } = InitializeIsSupported();
        private static bool InitializeIsSupported() => AppContext.TryGetSwitch("System.Runtime.InteropServices.EnableCppCLIHostActivation", out bool isSupported) ? isSupported : true;

        /// <summary>
        /// Loads an assembly that has already been loaded into memory by the OS loader as a native module
        /// into an isolated AssemblyLoadContext.
        /// </summary>
        /// <param name="moduleHandle">The native module handle for the assembly.</param>
        /// <param name="assemblyPath">The path to the assembly (as a pointer to a UTF-16 C string).</param>
        public static unsafe void LoadInMemoryAssembly(IntPtr moduleHandle, IntPtr assemblyPath)
        {
            if (!IsSupported)
                throw new NotSupportedException(SR.NotSupported_CppCli);

            LoadInMemoryAssemblyInContextWhenSupported(moduleHandle, assemblyPath);
        }

        // The call to `LoadInMemoryAssemblyInContextImpl` will produce a warning IL2026.
        // It is intentionally left in the product, so developers get a warning when trimming an app which enabled `Internal.Runtime.InteropServices.InMemoryAssemblyLoader.IsSupported`.
        // For runtime build the warning is suppressed in the ILLink.Suppressions.LibraryBuild.xml, but we only want to suppress it if the feature is enabled (IsSupported is true).
        // The call is extracted into a separate method which is the sole target of the suppression.
        private static unsafe void LoadInMemoryAssemblyInContextWhenSupported(IntPtr moduleHandle, IntPtr assemblyPath)
        {
#pragma warning disable IL2026 // suppressed in ILLink.Suppressions.LibraryBuild.xml
            LoadInMemoryAssemblyInContextImpl(moduleHandle, assemblyPath);
#pragma warning restore IL2026
        }

        /// <summary>
        /// Loads into an assembly that has already been loaded into memory by the OS loader as a native module
        /// into the specified load context.
        /// </summary>
        /// <param name="moduleHandle">The native module handle for the assembly.</param>
        /// <param name="assemblyPath">The path to the assembly (as a pointer to a UTF-16 C string).</param>
        /// <param name="loadContext">Load context (currently must be IntPtr.Zero)</param>
        [UnmanagedCallersOnly]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The same C++/CLI feature switch applies to LoadInMemoryAssembly and this function. We rely on the warning from LoadInMemoryAssembly.")]
        public static unsafe void LoadInMemoryAssemblyInContext(IntPtr moduleHandle, IntPtr assemblyPath, IntPtr loadContext)
        {
            if (!IsSupported)
                throw new NotSupportedException(SR.NotSupported_CppCli);

            ArgumentOutOfRangeException.ThrowIfNotEqual(loadContext, IntPtr.Zero);

            LoadInMemoryAssemblyInContextImpl(moduleHandle, assemblyPath, AssemblyLoadContext.Default);
        }

        [RequiresUnreferencedCode("C++/CLI is not trim-compatible", Url = "https://aka.ms/dotnet-illink/nativehost")]
        private static void LoadInMemoryAssemblyInContextImpl(IntPtr moduleHandle, IntPtr assemblyPath, AssemblyLoadContext? alc = null)
        {
            string? assemblyPathString = Marshal.PtrToStringUni(assemblyPath);
            if (assemblyPathString == null)
                throw new ArgumentOutOfRangeException(nameof(assemblyPath));

            // We don't cache the ALCs or resolvers here since each IJW assembly will call this method at most once
            // (the load process rewrites the stubs that call here to call the actual methods they're supposed to)
            if (alc is null)
            {
                alc = new IsolatedComponentLoadContext(assemblyPathString);
            }
            else if (alc == AssemblyLoadContext.Default)
            {
                var resolver = new AssemblyDependencyResolver(assemblyPathString);
                AssemblyLoadContext.Default.Resolving +=
                    [RequiresUnreferencedCode("C++/CLI is not trim-compatible", Url = "https://aka.ms/dotnet-illink/nativehost")]
                    (context, assemblyName) =>
                    {
                        string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
                        return assemblyPath != null
                            ? context.LoadFromAssemblyPath(assemblyPath)
                            : null;
                    };
            }

            alc.LoadFromInMemoryModule(moduleHandle);
        }
    }
}
