// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;

using LibraryNameVariation = System.Runtime.Loader.LibraryNameVariation;

namespace System.Runtime.InteropServices
{
    public static partial class NativeLibrary
    {
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

        internal static IntPtr LoadLibraryByName(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, bool throwOnError)
        {
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
            if (throwOnError && ret == IntPtr.Zero)
            {
                errorTracker.Throw(libraryName);
            }

            return ret;
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
    }
}
