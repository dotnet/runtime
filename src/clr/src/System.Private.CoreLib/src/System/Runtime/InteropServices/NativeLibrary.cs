// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using Win32Native = Microsoft.Win32.Win32Native;
using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// APIs for managing Native Libraries 
    /// </summary>
    public static partial class NativeLibrary
    {
        /// <summary>
        /// NativeLibrary Loader: Simple API
        /// This method is a wrapper around OS loader, using "default" flags.
        /// </summary>
        /// <param name="libraryPath">The name of the native library to be loaded</param>
        /// <returns>The handle for the loaded native library</returns>  
        /// <exception cref="System.ArgumentNullException">If libraryPath is null</exception>
        /// <exception cref="System.DllNotFoundException ">If the library can't be found.</exception>
        /// <exception cref="System.BadImageFormatException">If the library is not valid.</exception>
        public static IntPtr Load(string libraryPath)
        {
            if (libraryPath == null)
                throw new ArgumentNullException(nameof(libraryPath));

            return LoadFromPath(libraryPath, throwOnError: true);
        }

        /// <summary>
        /// NativeLibrary Loader: Simple API that doesn't throw
        /// </summary>
        /// <param name="libraryPath">The name of the native library to be loaded</param>
        /// <param name="handle">The out-parameter for the loaded native library handle</param>
        /// <returns>True on successful load, false otherwise</returns>  
        /// <exception cref="System.ArgumentNullException">If libraryPath is null</exception>
        public static bool TryLoad(string libraryPath, out IntPtr handle)
        {
            if (libraryPath == null)
                throw new ArgumentNullException(nameof(libraryPath));

            handle = LoadFromPath(libraryPath, throwOnError: false);
            return handle != IntPtr.Zero;
        }

        /// <summary>
        /// NativeLibrary Loader: High-level API
        /// Given a library name, this function searches specific paths based on the 
        /// runtime configuration, input parameters, and attributes of the calling assembly.
        /// If DllImportSearchPath parameter is non-null, the flags in this enumeration are used.
        /// Otherwise, the flags specified by the DefaultDllImportSearchPaths attribute on the 
        /// calling assembly (if any) are used. 
        /// This LoadLibrary() method does not invoke the managed call-backs for native library resolution: 
        /// * AssemblyLoadContext.LoadUnmanagedDll()
        /// </summary>
        /// <param name="libraryName">The name of the native library to be loaded</param>
        /// <param name="assembly">The assembly loading the native library</param>
        /// <param name="searchPath">The search path</param>
        /// <returns>The handle for the loaded library</returns>  
        /// <exception cref="System.ArgumentNullException">If libraryPath or assembly is null</exception>
        /// <exception cref="System.ArgumentException">If assembly is not a RuntimeAssembly</exception>
        /// <exception cref="System.DllNotFoundException ">If the library can't be found.</exception>
        /// <exception cref="System.BadImageFormatException">If the library is not valid.</exception>        
        public static IntPtr Load(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == null)
                throw new ArgumentNullException(nameof(libraryName));
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (!(assembly is RuntimeAssembly))
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
            
            return LoadByName(libraryName, 
                              ((RuntimeAssembly)assembly).GetNativeHandle(), 
                              searchPath.HasValue, 
                              (uint) searchPath.GetValueOrDefault(), 
                              throwOnError: true);
        }

        /// <summary>
        /// NativeLibrary Loader: High-level API that doesn't throw.
        /// </summary>
        /// <param name="libraryName">The name of the native library to be loaded</param>
        /// <param name="searchPath">The search path</param>
        /// <param name="assembly">The assembly loading the native library</param>
        /// <param name="handle">The out-parameter for the loaded native library handle</param>
        /// <returns>True on successful load, false otherwise</returns>  
        /// <exception cref="System.ArgumentNullException">If libraryPath or assembly is null</exception>
        /// <exception cref="System.ArgumentException">If assembly is not a RuntimeAssembly</exception>
        public static bool TryLoad(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, out IntPtr handle)
        {
            if (libraryName == null)
                throw new ArgumentNullException(nameof(libraryName));
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (!(assembly is RuntimeAssembly))
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
            
            handle = LoadByName(libraryName, 
                                ((RuntimeAssembly)assembly).GetNativeHandle(), 
                                searchPath.HasValue, 
                                (uint) searchPath.GetValueOrDefault(),
                                throwOnError: false);
            return handle != IntPtr.Zero;
        }

        /// <summary>
        /// Free a loaded library
        /// Given a library handle, free it.
        /// No action if the input handle is null.
        /// </summary>
        /// <param name="handle">The native library handle to be freed</param>
        /// <exception cref="System.InvalidOperationException">If the operation fails</exception>
        public static void Free(IntPtr handle)
        {
            FreeLib(handle);
        }

        /// <summary>
        /// Get the address of an exported Symbol
        /// This is a simple wrapper around OS calls, and does not perform any name mangling.
        /// </summary>
        /// <param name="handle">The native library handle</param>
        /// <param name="name">The name of the exported symbol</param>
        /// <returns>The address of the symbol</returns>  
        /// <exception cref="System.ArgumentNullException">If handle or name is null</exception>
        /// <exception cref="System.EntryPointNotFoundException">If the symbol is not found</exception>
        public static IntPtr GetExport(IntPtr handle, string name)
        {
            if (handle == IntPtr.Zero) 
                throw new ArgumentNullException(nameof(handle));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return GetSymbol(handle, name, throwOnError: true);
        }

        /// <summary>
        /// Get the address of an exported Symbol, but do not throw
        /// </summary>
        /// <param name="handle">The  native library handle</param>
        /// <param name="name">The name of the exported symbol</param>
        /// <param name="address"> The out-parameter for the symbol address, if it exists</param>
        /// <returns>True on success, false otherwise</returns>  
        /// <exception cref="System.ArgumentNullException">If handle or name is null</exception>
        public static bool TryGetExport(IntPtr handle, string name, out IntPtr address)
        {
            if (handle == IntPtr.Zero) 
                throw new ArgumentNullException(nameof(handle));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            address = GetSymbol(handle, name, throwOnError: false);
            return address != IntPtr.Zero;
        }

        /// External functions that implement the NativeLibrary interface

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadFromPath(string libraryName, bool throwOnError);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadByName(string libraryName, RuntimeAssembly callingAssembly,
                                                 bool hasDllImportSearchPathFlag, uint dllImportSearchPathFlag, 
                                                 bool throwOnError);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern void FreeLib(IntPtr handle);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        internal static extern IntPtr GetSymbol(IntPtr handle, string symbolName, bool throwOnError);
    }
}
