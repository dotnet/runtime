// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;

using LibraryNameVariation = System.Runtime.Loader.LibraryNameVariation;

namespace System.Runtime.InteropServices
{
    public static partial class NativeLibrary
    {
        internal static IntPtr LoadLibraryByName(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, bool throwOnError)
        {
            // First checks if a default dllImportSearchPathFlags was passed in, if so, use that value.
            // Otherwise checks if the assembly has the DefaultDllImportSearchPathsAttribute attribute.
            // If so, use that value.

            int searchPathFlags;
            bool searchAssemblyDirectory;
            if (searchPath.HasValue)
            {
                searchPathFlags = (int)(searchPath.Value & ~DllImportSearchPath.AssemblyDirectory);
                searchAssemblyDirectory = (searchPath.Value & DllImportSearchPath.AssemblyDirectory) != 0;
            }
            else
            {
                GetDllImportSearchPathFlags(assembly, out searchPathFlags, out searchAssemblyDirectory);
            }

            LoadLibErrorTracker errorTracker = default;
            IntPtr ret = LoadBySearch(assembly, searchAssemblyDirectory, searchPathFlags, ref errorTracker, libraryName);
            if (throwOnError && ret == IntPtr.Zero)
            {
                errorTracker.Throw(libraryName);
            }

            return ret;
        }

        internal static void GetDllImportSearchPathFlags(Assembly callingAssembly, out int searchPathFlags, out bool searchAssemblyDirectory)
        {
            var searchPath = DllImportSearchPath.AssemblyDirectory;

            foreach (CustomAttributeData cad in callingAssembly.CustomAttributes)
            {
                if (cad.AttributeType == typeof(DefaultDllImportSearchPathsAttribute))
                {
                    searchPath = (DllImportSearchPath)cad.ConstructorArguments[0].Value!;
                }
            }

            searchPathFlags = (int)(searchPath & ~DllImportSearchPath.AssemblyDirectory);
            searchAssemblyDirectory = (searchPath & DllImportSearchPath.AssemblyDirectory) != 0;
        }

        internal static IntPtr LoadBySearch(Assembly callingAssembly, bool searchAssemblyDirectory, int dllImportSearchPathFlags, ref LoadLibErrorTracker errorTracker, string libraryName)
        {
            IntPtr ret;

            int loadWithAlteredPathFlags = LoadWithAlteredSearchPathFlag;
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
                    int flags = loadWithAlteredPathFlags;
                    if ((dllImportSearchPathFlags & (int)DllImportSearchPath.UseDllDirectoryForDependencies) != 0)
                    {
                        // LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR is the only flag affecting absolute path. Don't OR the flags
                        // unconditionally as all absolute path P/Invokes could then lose LOAD_WITH_ALTERED_SEARCH_PATH.
                        flags |= dllImportSearchPathFlags;
                    }

                    ret = LoadLibraryHelper(currLibNameVariation, flags, ref errorTracker);
                    if (ret != IntPtr.Zero)
                    {
                        return ret;
                    }
                }
                else if ((callingAssembly != null) && searchAssemblyDirectory)
                {
                    // Try to load the module alongside the assembly where the PInvoke was declared.
                    // This only makes sense in dynamic scenarios (JIT/interpreter), so leaving this out for now.
                }

                ret = LoadLibraryHelper(currLibNameVariation, dllImportSearchPathFlags, ref errorTracker);
                if (ret != IntPtr.Zero)
                {
                    return ret;
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
