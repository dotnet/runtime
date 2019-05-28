// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// A delegate used to resolve native libraries via callback.
    /// </summary>
    /// <param name="libraryName">The native library to resolve</param>
    /// <param name="assembly">The assembly requesting the resolution</param>
    /// <param name="searchPath">
    ///     The DllImportSearchPathsAttribute on the PInvoke, if any. 
    ///     Otherwise, the DllImportSearchPathsAttribute on the assembly, if any. 
    ///     Otherwise null.
    /// </param>
    /// <returns>The handle for the loaded native library on success, null on failure</returns>  
    public delegate IntPtr DllImportResolver(string libraryName,
                                             Assembly assembly,
                                             DllImportSearchPath? searchPath);

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
        /// * The per-assembly registered callback 
        /// * AssemblyLoadContext.LoadUnmanagedDll()
        /// * AssemblyLoadContext.ResolvingUnmanagedDllEvent
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
            if (!assembly.IsRuntimeImplemented())
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
            
            return LoadLibraryByName(libraryName, 
                              assembly,
                              searchPath,
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
            if (!assembly.IsRuntimeImplemented())
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
            
            handle = LoadLibraryByName(libraryName,
                                assembly,
                                searchPath,
                                throwOnError: false);
            return handle != IntPtr.Zero;
        }

        /// <summary>
        /// Free a loaded library
        /// Given a library handle, free it.
        /// No action if the input handle is null.
        /// </summary>
        /// <param name="handle">The native library handle to be freed</param>
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

        /// <summary>
        /// Map from assembly to native-library resolver.
        /// Interop specific fields and properties are generally not added to Assembly class.
        /// Therefore, this table uses weak assembly pointers to indirectly achieve 
        /// similar behavior.
        /// </summary>
        private static ConditionalWeakTable<Assembly, DllImportResolver>? s_nativeDllResolveMap;

        /// <summary>
        /// Set a callback for resolving native library imports from an assembly.
        /// This per-assembly resolver is the first attempt to resolve native library loads 
        /// initiated by this assembly.
        ///
        /// Only one resolver can be registered per assembly. 
        /// Trying to register a second resolver fails with InvalidOperationException.
        /// </summary>
        /// <param name="assembly">The assembly for which the resolver is registered</param>
        /// <param name="resolver">The resolver callback to register</param>
        /// <exception cref="System.ArgumentNullException">If assembly or resolver is null</exception>
        /// <exception cref="System.ArgumentException">If a resolver is already set for this assembly</exception>
        public static void SetDllImportResolver(Assembly assembly, DllImportResolver resolver)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
            if (!assembly.IsRuntimeImplemented())
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);

            if (s_nativeDllResolveMap == null)
            {
                Interlocked.CompareExchange(ref s_nativeDllResolveMap,
                    new ConditionalWeakTable<Assembly, DllImportResolver>(), null);
            }

            try
            {
                s_nativeDllResolveMap!.Add(assembly, resolver); // TODO-NULLABLE: Remove ! when compiler specially-recognizes CompareExchange for nullability
            }
            catch (ArgumentException)
            {
                // ConditionalWealTable throws ArgumentException if the Key already exists
                throw new InvalidOperationException(SR.InvalidOperation_CannotRegisterSecondResolver);
            }
        }

        /// <summary>
        /// The helper function that calls the per-assembly native-library resolver 
        /// if one is registered for this assembly.
        /// </summary>
        /// <param name="libraryName">The native library to load</param>
        /// <param name="assembly">The assembly trying load the native library</param>
        /// <param name="hasDllImportSearchPathFlags">If the pInvoke has DefaultDllImportSearchPathAttribute</param>
        /// <param name="dllImportSearchPathFlags">If hasdllImportSearchPathFlags is true, the flags in 
        ///                                       DefaultDllImportSearchPathAttribute; meaningless otherwise </param>
        /// <returns>The handle for the loaded library on success. Null on failure.</returns>  
        internal static IntPtr LoadLibraryCallbackStub(string libraryName, Assembly assembly,
                                                       bool hasDllImportSearchPathFlags, uint dllImportSearchPathFlags)
        {
            if (s_nativeDllResolveMap == null)
            {
                return IntPtr.Zero;
            }

            if (!s_nativeDllResolveMap.TryGetValue(assembly, out DllImportResolver resolver))
            {
                return IntPtr.Zero;
            }

            return resolver(libraryName, assembly, hasDllImportSearchPathFlags ? (DllImportSearchPath?)dllImportSearchPathFlags : null);
        }
    }
}
