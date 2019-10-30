// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        {
            string? assemblyPathString = Marshal.PtrToStringUni(assemblyPath);
            if (assemblyPathString == null)
            {
                throw new ArgumentOutOfRangeException(nameof(assemblyPath));
            }

            // We don't cache the ALCs here since each IJW assembly will call this method at most once
            // (the load process rewrites the stubs that call here to call the actual methods they're supposed to)
            AssemblyLoadContext context = new IsolatedComponentLoadContext(assemblyPathString);
            context.LoadFromInMemoryModule(moduleHandle);
        }
    }
}
