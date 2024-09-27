// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Swift.Runtime
{
    // <summary>
    // Represents dynamic library loader in C#.
    // </summary>
    internal static partial class DynamicLibraryLoader
    {
        public const int RTLD_LAZY = 0x1;
        public const int RTLD_NOW = 0x2;

        [LibraryImport("libdl.dylib")]
        internal static partial IntPtr dlopen([MarshalAs(UnmanagedType.LPStr)] string path, int mode);

        [LibraryImport("libdl.dylib")]
        internal static partial IntPtr dlsym(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string symbol);

        [LibraryImport("libdl.dylib")]
        internal static partial int dlclose(IntPtr handle);

        [LibraryImport("libdl.dylib")]
        internal static partial IntPtr dlerror();
    }
}
