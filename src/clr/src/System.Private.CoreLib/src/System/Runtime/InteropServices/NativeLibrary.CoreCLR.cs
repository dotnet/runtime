// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices
{
    public static partial class NativeLibrary
    {
        internal static IntPtr LoadLibraryByName(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, bool throwOnError)
        {
            return LoadByName(libraryName,
                              ((RuntimeAssembly)assembly).GetNativeHandle(),
                              searchPath.HasValue,
                              (uint) searchPath.GetValueOrDefault(),
                              throwOnError);
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
