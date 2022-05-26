// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

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

            int loadWithAlteredPathFlags = 0;
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
            IntPtr ret = LoadLibraryHelper(libraryName, 0, ref errorTracker);
            if (throwOnError && ret == IntPtr.Zero)
            {
                errorTracker.Throw(libraryName);
            }

            return ret;
        }

        private static IntPtr LoadLibraryHelper(string libraryName, int flags, ref LoadLibErrorTracker errorTracker)
        {
#if TARGET_WINDOWS
            IntPtr ret = Interop.Kernel32.LoadLibraryEx(libraryName, IntPtr.Zero, flags);
            if (ret != IntPtr.Zero)
            {
                return ret;
            }

            int lastError = Marshal.GetLastWin32Error();
            if (lastError != Interop.Errors.ERROR_INVALID_PARAMETER)
            {
                errorTracker.TrackErrorCode(lastError);
            }

            return ret;
#else
            // TODO: FileDosToUnixPathA
            IntPtr ret = Interop.Sys.LoadLibrary(libraryName);
            if (ret == IntPtr.Zero)
            {
                string? message = Marshal.PtrToStringAnsi(Interop.Sys.GetLoadLibraryError());
                errorTracker.TrackErrorMessage(message);
            }

            return ret;
#endif
        }

        private static void FreeLib(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero);

#if !TARGET_UNIX
            bool result = Interop.Kernel32.FreeLibrary(handle);
            if (!result)
                throw new InvalidOperationException();
#else
            Interop.Sys.FreeLibrary(handle);
#endif
        }

        private static unsafe IntPtr GetSymbol(IntPtr handle, string symbolName, bool throwOnError)
        {
            IntPtr ret;
#if !TARGET_UNIX
            var symbolBytes = new byte[Encoding.UTF8.GetByteCount(symbolName) + 1];
            Encoding.UTF8.GetBytes(symbolName, symbolBytes);
            fixed (byte* pSymbolBytes = symbolBytes)
            {
                ret = Interop.Kernel32.GetProcAddress(handle, pSymbolBytes);
            }
#else
            ret = Interop.Sys.GetProcAddress(handle, symbolName);
#endif
            if (throwOnError && ret == IntPtr.Zero)
                throw new EntryPointNotFoundException(SR.Format(SR.Arg_EntryPointNotFoundExceptionParameterizedNoLibrary, symbolName));

            return ret;
        }

        // Preserving good error info from DllImport-driven LoadLibrary is tricky because we keep loading from different places
        // if earlier loads fail and those later loads obliterate error codes.
        //
        // This tracker object will keep track of the error code in accordance to priority:
        //
        //   low-priority:      unknown error code (should never happen)
        //   medium-priority:   dll not found
        //   high-priority:     dll found but error during loading
        //
        // We will overwrite the previous load's error code only if the new error code is higher priority.
        internal struct LoadLibErrorTracker
        {
#if TARGET_WINDOWS
            private int _errorCode;
            private int _priority;

            private const int PriorityNotFound = 10;
            private const int PriorityAccessDenied = 20;
            private const int PriorityCouldNotLoad = 99999;

            public void Throw(string libraryName)
            {
                if (_errorCode == Interop.Errors.ERROR_BAD_EXE_FORMAT)
                {
                    throw new BadImageFormatException();
                }

                string message = Interop.Kernel32.GetMessage(_errorCode);
                throw new DllNotFoundException(SR.Format(SR.DllNotFound_Windows, libraryName, message));
            }

            public void TrackErrorCode(int errorCode)
            {
                int priority = errorCode switch
                {
                    Interop.Errors.ERROR_FILE_NOT_FOUND or
                    Interop.Errors.ERROR_PATH_NOT_FOUND or
                    Interop.Errors.ERROR_MOD_NOT_FOUND or
                    Interop.Errors.ERROR_DLL_NOT_FOUND => PriorityNotFound,

                    // If we can't access a location, we can't know if the dll's there or if it's good.
                    // Still, this is probably more unusual (and thus of more interest) than a dll-not-found
                    // so give it an intermediate priority.
                    Interop.Errors.ERROR_ACCESS_DENIED => PriorityAccessDenied,

                    // Assume all others are "dll found but couldn't load."
                    _ => PriorityCouldNotLoad,
                };

                if (priority > _priority)
                {
                    _errorCode = errorCode;
                    _priority = priority;
                }
            }
#else
            // On Unix systems we don't have detailed programatic information on why load failed
            // so there's no priorities.
            private string? _errorMessage;

            public void Throw(string libraryName)
            {
#if TARGET_OSX
                throw new DllNotFoundException(SR.Format(SR.DllNotFound_Mac, libraryName, _errorMessage));
#else
                throw new DllNotFoundException(SR.Format(SR.DllNotFound_Linux, libraryName, _errorMessage));
#endif
            }

            public void TrackErrorMessage(string? message)
            {
                _errorMessage = message;
            }
#endif
        }
    }
}
