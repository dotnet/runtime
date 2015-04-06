// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

// TODO: This implementation is temporary until Environment.OSVersion is officially exposed,
//       at which point that should be used instead.

internal static partial class Interop
{
    internal enum OperatingSystem
    {
        Windows,
        Linux,
        OSX
    }

    internal static bool IsWindows
    {
        get { return OperatingSystem.Windows == PlatformDetection.OperatingSystem; }
    }

    internal static bool IsLinux
    {
        get { return OperatingSystem.Linux == PlatformDetection.OperatingSystem; }
    }

    internal static bool IsOSX
    {
        get { return OperatingSystem.OSX == PlatformDetection.OperatingSystem; }
    }

    internal static class PlatformDetection
    {
        internal static OperatingSystem OperatingSystem { get { return s_os.Value; } }

        private static readonly Lazy<OperatingSystem> s_os = new Lazy<OperatingSystem>(() =>
        {
            if (Environment.NewLine != "\r\n")
            {
                unsafe
                {
                    byte* buffer = stackalloc byte[8192]; // the size use with uname is platform specific; this should be large enough for any OS
                    if (uname(buffer) == 0)
                    {
                        return Marshal.PtrToStringAnsi((IntPtr)buffer) == "Darwin" ?
                            OperatingSystem.OSX :
                            OperatingSystem.Linux;
                    }
                }
            }
            return OperatingSystem.Windows;
        });

        // not in src\Interop\Unix to avoiding pulling platform-dependent files into all projects
        [DllImport("libc")]
        private static extern unsafe uint uname(byte* buf); 
    }
}
