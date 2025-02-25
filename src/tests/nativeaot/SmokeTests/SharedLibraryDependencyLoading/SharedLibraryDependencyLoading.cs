// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharedLibrary
{
    public class ClassLibrary
    {
        [UnmanagedCallersOnly(EntryPoint = "MultiplyIntegers", CallConvs = [typeof(CallConvStdcall)])]
        public static int MultiplyIntegersExport(int x, int y)
        {
            return MultiplyIntegers(x, y);
        }

        [UnmanagedCallersOnly(EntryPoint = "GetBaseDirectory", CallConvs = [typeof(CallConvStdcall)])]
        public static IntPtr GetBaseDirectory()
        {
            return Marshal.StringToCoTaskMemAnsi(AppContext.BaseDirectory);
        }

        [DllImport("SharedLibraryDependency")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        public static extern int MultiplyIntegers(int x, int y);
    }
}
