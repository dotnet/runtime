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
        internal static string GetNativeLibraryName()
        {
            if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsWatchOS())
            {
                return "libodbc.2.dylib";
            }
            return "libodbc.so.2";
        }

        static Odbc()
        {
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), (libraryName, assembly, searchPath) =>
            {
                if (libraryName == Libraries.Odbc32)
                {
                    return NativeLibrary.Load(GetNativeLibraryName(), assembly, default);
                }
                return default;
            });
        }
    }
}
