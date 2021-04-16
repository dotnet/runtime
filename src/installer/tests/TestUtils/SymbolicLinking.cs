// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class SymbolicLinking
    {
        static class Kernel32
        {
            [Flags]
            internal enum SymbolicLinkFlag
            {
                IsFile = 0x0,
                IsDirectory = 0x1,
                AllowUnprivilegedCreate = 0x2
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool CreateSymbolicLink(
                string symbolicLinkName,
                string targetFileName,
                SymbolicLinkFlag flags);
        }

        static class libc
        {
            [DllImport("libc", SetLastError = true)]
            internal static extern int symlink(
                string targetFileName,
                string linkPath);

            [DllImport("libc", CharSet = CharSet.Ansi)]
            internal static extern IntPtr strerror(int errnum);
        }

        public static bool MakeSymbolicLink(string symbolicLinkName, string targetFileName, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!Kernel32.CreateSymbolicLink(symbolicLinkName, targetFileName, Kernel32.SymbolicLinkFlag.IsFile))
                {
                    int errno = Marshal.GetLastWin32Error();
                    errorMessage = $"CreateSymbolicLink failed with error number {errno}";
                    return false;
                }
            }
            else 
            {
                if (libc.symlink(targetFileName, symbolicLinkName) == -1)
                {
                    int errno = Marshal.GetLastWin32Error();
                    errorMessage = Marshal.PtrToStringAnsi(libc.strerror(errno));
                    return false; 
                }
            }

            return true;
        }
    }
}
