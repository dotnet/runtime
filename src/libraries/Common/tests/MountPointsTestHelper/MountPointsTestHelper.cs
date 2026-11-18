// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

public static class MountPointsTestHelper
{
    private static readonly object s_lock = new();

    static MountPointsTestHelper()
    {
        NativeLibrary.SetDllImportResolver(
            typeof(MountPointsTestHelper).Assembly,
            static (name, _, _) => name == Interop.Libraries.SystemNative ?
                throw new NativeFallbackException() :
                IntPtr.Zero);
    }

    public static string[] GetAllMountPoints(bool isLinux, bool isAndroid, string mountInfoPath)
    {
        lock (s_lock)
        {
            Interop.Sys.OperatingSystem.s_isLinux = isLinux;
            Interop.Sys.OperatingSystem.s_isAndroid = isAndroid;
            Interop.procfs.ProcMountInfoFilePath = mountInfoPath;
            return Interop.Sys.GetAllMountPoints();
        }
    }

    public sealed class NativeFallbackException : Exception
    {
    }
}

internal static partial class Interop
{
    internal static class Libraries
    {
        internal const string SystemNative = "MountPointsTestHelper.Native";
    }

    internal static partial class @procfs
    {
        internal static string ProcMountInfoFilePath = "";
    }

    internal static partial class Sys
    {
        internal static class OperatingSystem
        {
            internal static bool s_isLinux;
            internal static bool s_isAndroid;

            internal static bool IsLinux() => s_isLinux;
            internal static bool IsAndroid() => s_isAndroid;
        }
    }
}
