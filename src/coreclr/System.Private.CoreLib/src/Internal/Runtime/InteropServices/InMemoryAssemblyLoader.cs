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
        [FeatureSwitchDefinition("System.Runtime.InteropServices.EnableCppCLIHostActivation")]
        private static bool IsSupported { get; } = AppContext.TryGetSwitch("System.Runtime.InteropServices.EnableCppCLIHostActivation", out bool isSupported) ? isSupported : true;

        /// <summary>
        /// Loads an assembly that has already been loaded into memory by the OS loader as a native module
        /// into an isolated AssemblyLoadContext.
        /// </summary>
        /// <param name="moduleHandle">The native module handle for the assembly.</param>
        /// <param name="assemblyPath">The path to the assembly (as a pointer to a UTF-16 C string).</param>
        public static void LoadInMemoryAssembly(IntPtr moduleHandle, IntPtr assemblyPath)
        {
            if (!IsSupported)
                throw new NotSupportedException(SR.NotSupported_CppCli);

            LoadInMemoryAssemblyInContextWhenSupported(moduleHandle, assemblyPath);
        }

        // The call to `LoadInMemoryAssemblyInContextImpl` will produce a warning IL2026.
        // It is intentionally left in the product, so developers get a warning when trimming an app which enabled `Internal.Runtime.InteropServices.InMemoryAssemblyLoader.IsSupported`.
        // For runtime build the warning is suppressed in the ILLink.Suppressions.LibraryBuild.xml, but we only want to suppress it if the feature is enabled (IsSupported is true).
        // The call is extracted into a separate method which is the sole target of the suppression.
        private static void LoadInMemoryAssemblyInContextWhenSupported(IntPtr moduleHandle, IntPtr assemblyPath)
        {
#pragma warning disable IL2026 // suppressed in ILLink.Suppressions.LibraryBuild.xml
            LoadInMemoryAssemblyInContextImpl(moduleHandle, assemblyPath, ComponentLoadContextManager.IsolatedContext);
#pragma warning restore IL2026
        }

        /// <summary>
        /// Loads into an assembly that has already been loaded into memory by the OS loader as a native module
        /// into the specified load context.
        /// </summary>
        /// <param name="moduleHandle">The native module handle for the assembly.</param>
        /// <param name="assemblyPath">The path to the assembly (as a pointer to a UTF-16 C string).</param>
        /// <param name="loadContext">Load context specification.</param>
        [UnmanagedCallersOnly]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The same C++/CLI feature switch applies to LoadInMemoryAssembly and this function. We rely on the warning from LoadInMemoryAssembly.")]
        public static void LoadInMemoryAssemblyInContext(IntPtr moduleHandle, IntPtr assemblyPath, IntPtr loadContext)
        {
            if (!IsSupported)
                throw new NotSupportedException(SR.NotSupported_CppCli);

            LoadInMemoryAssemblyInContextImpl(moduleHandle, assemblyPath, loadContext);
        }

        [RequiresUnreferencedCode("C++/CLI is not trim-compatible", Url = "https://aka.ms/dotnet-illink/nativehost")]
        private static void LoadInMemoryAssemblyInContextImpl(IntPtr moduleHandle, IntPtr assemblyPath, IntPtr loadContext)
        {
            string assemblyPathString = Marshal.PtrToStringUni(assemblyPath) ??
                throw new ArgumentOutOfRangeException(nameof(assemblyPath));

            // We don't cache isolated ALCs here since each IJW assembly will call this method at most once
            // (the load process rewrites the stubs that call here to call the actual methods they're supposed to)
            AssemblyLoadContext alc = ComponentLoadContextManager.Get(loadContext, assemblyPathString, cacheIsolatedContext: false);
            alc.LoadFromInMemoryModule(moduleHandle);
        }
    }
}
