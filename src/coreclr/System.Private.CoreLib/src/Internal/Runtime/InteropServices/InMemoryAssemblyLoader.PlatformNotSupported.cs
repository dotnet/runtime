// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Internal.Runtime.InteropServices
{
    /// <summary>
    /// This class enables the .NET IJW host to load an in-memory module as a .NET assembly
    /// </summary>
    public static class InMemoryAssemblyLoader
    {
        /// <summary>
        /// Loads into an isolated AssemblyLoadContext an assembly that has already been loaded into memory by the OS loader as a native module.
        /// </summary>
        /// <param name="moduleHandle">The native module handle for the assembly.</param>
        /// <param name="assemblyPath">The path to the assembly (as a pointer to a UTF-16 C string).</param>
        public static unsafe void LoadInMemoryAssembly(IntPtr moduleHandle, IntPtr assemblyPath)
            => throw new PlatformNotSupportedException();
    }
}
