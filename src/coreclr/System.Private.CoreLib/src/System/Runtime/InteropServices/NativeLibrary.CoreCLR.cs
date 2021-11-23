// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public static partial class NativeLibrary
    {
        internal static IntPtr LoadLibraryByName(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, bool throwOnError)
        {
            RuntimeAssembly rtAsm = (RuntimeAssembly)assembly;
            return LoadByName(libraryName,
                              new QCallAssembly(ref rtAsm),
                              searchPath.HasValue,
                              (uint)searchPath.GetValueOrDefault(),
                              throwOnError);
        }

        /// External functions that implement the NativeLibrary interface

        [DllImport(RuntimeHelpers.QCall, EntryPoint = "NativeLibrary_LoadFromPath", CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadFromPath(string libraryName, bool throwOnError);

        [DllImport(RuntimeHelpers.QCall, EntryPoint = "NativeLibrary_LoadByName", CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadByName(string libraryName, QCallAssembly callingAssembly,
                                                 bool hasDllImportSearchPathFlag, uint dllImportSearchPathFlag,
                                                 bool throwOnError);

        [DllImport(RuntimeHelpers.QCall, EntryPoint = "NativeLibrary_FreeLib")]
        internal static extern void FreeLib(IntPtr handle);

        [DllImport(RuntimeHelpers.QCall, EntryPoint = "NativeLibrary_GetSymbol", CharSet = CharSet.Unicode)]
        internal static extern IntPtr GetSymbol(IntPtr handle, string symbolName, bool throwOnError);
    }
}
