// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

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

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "NativeLibrary_LoadByName", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial IntPtr LoadByName(string libraryName, QCallAssembly callingAssembly,
                                                 [MarshalAs(UnmanagedType.Bool)] bool hasDllImportSearchPathFlag, uint dllImportSearchPathFlag,
                                                 [MarshalAs(UnmanagedType.Bool)] bool throwOnError);

        [UnmanagedCallersOnly]
        [RequiresUnsafe]
        private static unsafe IntPtr LoadLibraryCallbackStub(char* pLibraryName, Assembly* pAssembly, bool hasDllImportSearchPathFlags, uint dllImportSearchPathFlags, Exception* pException)
        {
            try
            {
                return LoadLibraryCallbackStub(new string(pLibraryName), *pAssembly, hasDllImportSearchPathFlags, dllImportSearchPathFlags);
            }
            catch (Exception ex)
            {
                *pException = ex;
                return default;
            }
        }
    }
}
