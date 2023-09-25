// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Libraries
    {
        internal const string Odbc32 = "libodbc";
    }

    internal static partial class Odbc
    {
        private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == Libraries.Odbc32)
            {
                if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsWatchOS())
                {
                    libraryName = "libodbc.2.dylib";
                }
                else
                {
                    libraryName = "libodbc.so.2";
                }
                return NativeLibrary.Load(libraryName, assembly, default);
            }
            return default;
        }

        static Odbc()
        {
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
        }
    }
}
