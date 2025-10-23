// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// A delegate used to resolve native libraries via callback.
    /// </summary>
    /// <param name="libraryName">The native library to resolve.</param>
    /// <param name="assembly">The assembly requesting the resolution.</param>
    /// <param name="searchPath">
    ///     The DllImportSearchPathsAttribute on the PInvoke, if any.
    ///     Otherwise, the DllImportSearchPathsAttribute on the assembly, if any.
    ///     Otherwise null.
    /// </param>
    /// <returns>The handle for the loaded native library on success, null on failure.</returns>
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
        /// <param name="libraryPath">The name of the native library to be loaded.</param>
        /// <returns>The handle for the loaded native library.</returns>
        /// <exception cref="ArgumentNullException">If libraryPath is null</exception>
        /// <exception cref="DllNotFoundException ">If the library can't be found.</exception>
        /// <exception cref="BadImageFormatException">If the library is not valid.</exception>
        public static IntPtr Load(string libraryPath)
        {
            ArgumentNullException.ThrowIfNull(libraryPath);

            return LoadFromPath(libraryPath, throwOnError: true);
        }

        /// <summary>
        /// NativeLibrary Loader: Simple API that doesn't throw
        /// </summary>
        /// <param name="libraryPath">The name of the native library to be loaded.</param>
        /// <param name="handle">The out-parameter for the loaded native library handle.</param>
        /// <returns>True on successful load, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">If libraryPath is null</exception>
        public static bool TryLoad(string libraryPath, out IntPtr handle)
        {
            ArgumentNullException.ThrowIfNull(libraryPath);

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
        /// This method follows the native library resolution for the AssemblyLoadContext of the
        /// specified assembly. It will invoke the managed extension points:
        /// * AssemblyLoadContext.LoadUnmanagedDll()
        /// * AssemblyLoadContext.ResolvingUnmanagedDllEvent
        /// It does not invoke extension points that are not tied to the AssemblyLoadContext:
        /// * The per-assembly registered DllImportResolver callback
        /// </summary>
        /// <param name="libraryName">The name of the native library to be loaded.</param>
        /// <param name="assembly">The assembly loading the native library.</param>
        /// <param name="searchPath">The search path.</param>
        /// <returns>The handle for the loaded library.</returns>
        /// <exception cref="ArgumentNullException">If libraryPath or assembly is null</exception>
        /// <exception cref="ArgumentException">If assembly is not a RuntimeAssembly</exception>
        /// <exception cref="DllNotFoundException">If the library can't be found.</exception>
        /// <exception cref="BadImageFormatException">If the library is not valid.</exception>
        public static IntPtr Load(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            ArgumentNullException.ThrowIfNull(libraryName);
            ArgumentNullException.ThrowIfNull(assembly);

            if (assembly is not RuntimeAssembly)
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);

            return LoadLibraryByName(libraryName,
                              assembly,
                              searchPath,
                              throwOnError: true);
        }

        /// <summary>
        /// NativeLibrary Loader: High-level API that doesn't throw.
        /// Given a library name, this function searches specific paths based on the
        /// runtime configuration, input parameters, and attributes of the calling assembly.
        /// If DllImportSearchPath parameter is non-null, the flags in this enumeration are used.
        /// Otherwise, the flags specified by the DefaultDllImportSearchPaths attribute on the
        /// calling assembly (if any) are used.
        /// This method follows the native library resolution for the AssemblyLoadContext of the
        /// specified assembly. It will invoke the managed extension points:
        /// * AssemblyLoadContext.LoadUnmanagedDll()
        /// * AssemblyLoadContext.ResolvingUnmanagedDllEvent
        /// It does not invoke extension points that are not tied to the AssemblyLoadContext:
        /// * The per-assembly registered DllImportResolver callback
        /// </summary>
        /// <param name="libraryName">The name of the native library to be loaded.</param>
        /// <param name="assembly">The assembly loading the native library.</param>
        /// <param name="searchPath">The search path.</param>
        /// <param name="handle">The out-parameter for the loaded native library handle.</param>
        /// <returns>True on successful load, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">If libraryPath or assembly is null</exception>
        /// <exception cref="ArgumentException">If assembly is not a RuntimeAssembly</exception>
        public static bool TryLoad(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, out IntPtr handle)
        {
            ArgumentNullException.ThrowIfNull(libraryName);
            ArgumentNullException.ThrowIfNull(assembly);

            if (assembly is not RuntimeAssembly)
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);

            handle = LoadLibraryByName(libraryName,
                                assembly,
                                searchPath,
                                throwOnError: false);
            return handle != IntPtr.Zero;
        }

        // Not a public API. We expose this so that it's possible to bypass the codepath that tries to read search path
        // from custom attributes.
        internal static bool TryLoad(string libraryName, Assembly assembly, DllImportSearchPath searchPath, out IntPtr handle)
        {
            handle = LoadLibraryByName(libraryName,
                                assembly,
                                userSpecifiedSearchFlags: true,
                                searchPath,
                                throwOnError: false);
            return handle != IntPtr.Zero;
        }

        /// <summary>
        /// Free a loaded library
        /// Given a library handle, free it.
        /// No action if the input handle is null.
        /// </summary>
        /// <param name="handle">The native library handle to be freed.</param>
        public static void Free(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return;
            FreeLib(handle);
        }

        /// <summary>
        /// Get the address of an exported Symbol
        /// This is a simple wrapper around OS calls, and does not perform any name mangling.
        /// </summary>
        /// <param name="handle">The native library handle.</param>
        /// <param name="name">The name of the exported symbol.</param>
        /// <returns>The address of the symbol.</returns>
        /// <exception cref="ArgumentNullException">If handle or name is null</exception>
        /// <exception cref="EntryPointNotFoundException">If the symbol is not found</exception>
        public static IntPtr GetExport(IntPtr handle, string name)
        {
            ArgumentNullException.ThrowIfNull(handle);
            ArgumentNullException.ThrowIfNull(name);

            return GetSymbol(handle, name, throwOnError: true);
        }

        /// <summary>
        /// Get the address of an exported Symbol, but do not throw
        /// </summary>
        /// <param name="handle">The  native library handle.</param>
        /// <param name="name">The name of the exported symbol.</param>
        /// <param name="address"> The out-parameter for the symbol address, if it exists.</param>
        /// <returns>True on success, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">If handle or name is null</exception>
        public static bool TryGetExport(IntPtr handle, string name, out IntPtr address)
        {
            ArgumentNullException.ThrowIfNull(handle);
            ArgumentNullException.ThrowIfNull(name);

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
        /// <param name="assembly">The assembly for which the resolver is registered.</param>
        /// <param name="resolver">The resolver callback to register.</param>
        /// <exception cref="ArgumentNullException">If assembly or resolver is null</exception>
        /// <exception cref="ArgumentException">If a resolver is already set for this assembly</exception>
        public static void SetDllImportResolver(Assembly assembly, DllImportResolver resolver)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(resolver);

            if (assembly is not RuntimeAssembly)
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);

            if (s_nativeDllResolveMap == null)
            {
                Interlocked.CompareExchange(ref s_nativeDllResolveMap,
                    new ConditionalWeakTable<Assembly, DllImportResolver>(), null);
            }

            if (!s_nativeDllResolveMap.TryAdd(assembly, resolver))
            {
                throw new InvalidOperationException(SR.InvalidOperation_CannotRegisterSecondResolver);
            }
        }

        /// <summary>
        /// The helper function that calls the per-assembly native-library resolver
        /// if one is registered for this assembly.
        /// </summary>
        /// <param name="libraryName">The native library to load.</param>
        /// <param name="assembly">The assembly trying load the native library.</param>
        /// <param name="hasDllImportSearchPathFlags">If the pInvoke has DefaultDllImportSearchPathAttribute.</param>
        /// <param name="dllImportSearchPathFlags">If <paramref name="hasDllImportSearchPathFlags"/> is true, the flags in
        ///                                       DefaultDllImportSearchPathAttribute; meaningless otherwise </param>
        /// <returns>The handle for the loaded library on success. Null on failure.</returns>
        internal static IntPtr LoadLibraryCallbackStub(string libraryName, Assembly assembly,
                                                       bool hasDllImportSearchPathFlags, uint dllImportSearchPathFlags)
        {
            if (s_nativeDllResolveMap == null)
            {
                return IntPtr.Zero;
            }

            if (!s_nativeDllResolveMap.TryGetValue(assembly, out DllImportResolver? resolver))
            {
                return IntPtr.Zero;
            }

            return resolver(libraryName, assembly, hasDllImportSearchPathFlags ? (DllImportSearchPath?)dllImportSearchPathFlags : null);
        }

        /// <summary>
        /// Get a handle that can be used with <see cref="GetExport" /> or <see cref="TryGetExport" /> to resolve exports from the entry point module.
        /// </summary>
        /// <returns> The handle that can be used to resolve exports from the entry point module.</returns>
        public static IntPtr GetMainProgramHandle()
        {
            IntPtr result = IntPtr.Zero;
#if TARGET_WINDOWS
            result = Interop.Kernel32.GetModuleHandle(null);
#else
            result = Interop.Sys.GetDefaultSearchOrderPseudoHandle();
#endif
            // I don't know when a failure case can occur here, but checking for it and throwing an exception
            // if we encounter it.
            if (result == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError());
            }
            return result;
        }

#if !MONO
        internal static IntPtr LoadLibraryByName(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, bool throwOnError)
        {
#if !NATIVEAOT
            // Resolve using the AssemblyLoadContext.LoadUnmanagedDll implementation
            IntPtr mod = LoadNativeLibraryViaAssemblyLoadContext(assembly, libraryName);
            if (mod != IntPtr.Zero)
                return mod;
#endif

            // First checks if a default dllImportSearchPathFlags was passed in, if so, use that value.
            // Otherwise checks if the assembly has the DefaultDllImportSearchPathsAttribute attribute.
            // If so, use that value.
            bool userSpecifiedSearchFlags = searchPath.HasValue;
            if (!userSpecifiedSearchFlags)
            {
                searchPath = GetDllImportSearchPath(assembly, out userSpecifiedSearchFlags);
            }
            return LoadLibraryByName(libraryName, assembly, userSpecifiedSearchFlags, searchPath!.Value, throwOnError);
        }

        private static IntPtr LoadLibraryByName(string libraryName, Assembly assembly, bool userSpecifiedSearchFlags, DllImportSearchPath searchPath, bool throwOnError)
        {
            int searchPathFlags = (int)(searchPath & ~DllImportSearchPath.AssemblyDirectory);
            bool searchAssemblyDirectory = (searchPath & DllImportSearchPath.AssemblyDirectory) != 0;

            LoadLibErrorTracker errorTracker = default;
            IntPtr ret = LoadBySearch(assembly, userSpecifiedSearchFlags, searchAssemblyDirectory, searchPathFlags, ref errorTracker, libraryName);

            // Resolve using the AssemblyLoadContext.ResolvingUnmanagedDll event
            if (ret == IntPtr.Zero)
            {
                ret = LoadNativeLibraryViaAssemblyLoadContextEvent(assembly, libraryName);
            }

            if (throwOnError && ret == IntPtr.Zero)
            {
                errorTracker.Throw(libraryName);
            }

            return ret;
        }

#if !NATIVEAOT
        private static IntPtr LoadNativeLibraryViaAssemblyLoadContext(Assembly callingAssembly, string libraryName)
        {
#if TARGET_WINDOWS
            // This is replicating quick check from the OS implementation of api sets.
            if (libraryName.StartsWithOrdinalIgnoreCase("api-") ||
                libraryName.StartsWithOrdinalIgnoreCase("ext-"))
            {
                // Prevent Overriding of Windows API sets.
                return IntPtr.Zero;
            }
#endif
            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(callingAssembly);
            if (alc is null || alc == AssemblyLoadContext.Default)
            {
                // For assemblies bound via default binder, we should use the standard mechanism to make the pinvoke call.
                return IntPtr.Zero;
            }

            // Call System.Runtime.Loader.AssemblyLoadContext.LoadUnmanagedDll to give
            // The custom assembly context a chance to load the unmanaged dll.
            return alc.InvokeLoadUnmanagedDll(libraryName);
        }
#endif

        private static IntPtr LoadNativeLibraryViaAssemblyLoadContextEvent(Assembly callingAssembly, string libraryName)
        {
            AssemblyLoadContext? alc = AssemblyLoadContext.GetLoadContext(callingAssembly);
            return alc?.GetResolvedUnmanagedDll(callingAssembly, libraryName) ?? IntPtr.Zero;
        }

        private static DllImportSearchPath GetDllImportSearchPath(Assembly callingAssembly, out bool userSpecifiedSearchFlags)
        {
            foreach (CustomAttributeData cad in callingAssembly.CustomAttributes)
            {
                if (cad.AttributeType == typeof(DefaultDllImportSearchPathsAttribute))
                {
                    userSpecifiedSearchFlags = true;
                    return (DllImportSearchPath)cad.ConstructorArguments[0].Value!;
                }
            }

            userSpecifiedSearchFlags = false;
            return DllImportSearchPath.AssemblyDirectory;
        }

        internal static IntPtr LoadBySearch(Assembly callingAssembly, bool userSpecifiedSearchFlags, bool searchAssemblyDirectory, int dllImportSearchPathFlags, ref LoadLibErrorTracker errorTracker, string libraryName)
        {
            IntPtr ret;

            int loadWithAlteredPathFlags = LoadWithAlteredSearchPathFlag;
            const int loadLibrarySearchFlags = (int)DllImportSearchPath.UseDllDirectoryForDependencies
                | (int)DllImportSearchPath.ApplicationDirectory
                | (int)DllImportSearchPath.UserDirectories
                | (int)DllImportSearchPath.System32
                | (int)DllImportSearchPath.SafeDirectories;
            bool libNameIsRelativePath = !Path.IsPathFullyQualified(libraryName);

            // P/Invokes are often declared with variations on the actual library name.
            // For example, it's common to leave off the extension/suffix of the library
            // even if it has one, or to leave off a prefix like "lib" even if it has one
            // (both of these are typically done to smooth over cross-platform differences).
            // We try to dlopen with such variations on the original.
            foreach (LibraryNameVariation libraryNameVariation in LibraryNameVariation.DetermineLibraryNameVariations(libraryName, libNameIsRelativePath))
            {
                string currLibNameVariation = libraryNameVariation.Prefix + libraryName + libraryNameVariation.Suffix;

                if (!libNameIsRelativePath)
                {
                    // LOAD_WITH_ALTERED_SEARCH_PATH is incompatible with LOAD_LIBRARY_SEARCH flags. Remove those flags if they are set.
                    int flags = loadWithAlteredPathFlags | (dllImportSearchPathFlags & ~loadLibrarySearchFlags);
                    ret = LoadLibraryHelper(currLibNameVariation, flags, ref errorTracker);
                    if (ret != IntPtr.Zero)
                    {
                        return ret;
                    }
                }
                else if ((callingAssembly != null) && searchAssemblyDirectory)
                {
                    // LOAD_WITH_ALTERED_SEARCH_PATH is incompatible with LOAD_LIBRARY_SEARCH flags. Remove those flags if they are set.
                    int flags = loadWithAlteredPathFlags | (dllImportSearchPathFlags & ~loadLibrarySearchFlags);

                    // Try to load the module alongside the assembly where the PInvoke was declared.
                    // For PInvokes where the DllImportSearchPath.AssemblyDirectory is specified, look next to the application.
                    ret = LoadLibraryHelper(Path.Combine(AppContext.BaseDirectory, currLibNameVariation), flags, ref errorTracker);
                    if (ret != IntPtr.Zero)
                    {
                        return ret;
                    }
                }

                // Internally, search path flags and whether or not to search the assembly directory are
                // tracked separately. However, on the API level, DllImportSearchPath represents them both.
                // When unspecified, the default is to search the assembly directory and all OS defaults,
                // which maps to searchAssemblyDirectory being true and dllImportSearchPathFlags being 0.
                // When a user specifies DllImportSearchPath.AssemblyDirectory, searchAssemblyDirectory is
                // true, dllImportSearchPathFlags is 0, and the desired logic is to only search the assembly
                // directory (handled above), so we avoid doing any additional load search in that case.
                if (!userSpecifiedSearchFlags || !searchAssemblyDirectory || dllImportSearchPathFlags != 0)
                {
                    ret = LoadLibraryHelper(currLibNameVariation, dllImportSearchPathFlags, ref errorTracker);
                    if (ret != IntPtr.Zero)
                    {
                        return ret;
                    }
                }
            }

            return IntPtr.Zero;
        }

        private static IntPtr LoadFromPath(string libraryName, bool throwOnError)
        {
            LoadLibErrorTracker errorTracker = default;
            IntPtr ret = LoadLibraryHelper(libraryName, LoadWithAlteredSearchPathFlag, ref errorTracker);
            if (throwOnError && ret == IntPtr.Zero)
            {
                errorTracker.Throw(libraryName);
            }

            return ret;
        }

        private static unsafe IntPtr GetSymbol(IntPtr handle, string symbolName, bool throwOnError)
        {
            IntPtr ret = GetSymbolOrNull(handle, symbolName);
            if (throwOnError && ret == IntPtr.Zero)
                throw new EntryPointNotFoundException(SR.Format(SR.Arg_EntryPointNotFoundExceptionParameterizedNoLibrary, symbolName));

            return ret;
        }
#endif
    }
}
