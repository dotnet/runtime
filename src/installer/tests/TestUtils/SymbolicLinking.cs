// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.CoreSetup.Test
{
    public static class SymbolicLinking
    {
#if WINDOWS
        [Flags]
        internal enum SymbolicLinkFlag
        {
            IsFile = 0x0,
            IsDirectory = 0x1,
            AllowUnprivilegedCreate = 0x2
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
        internal static extern bool CreateSymbolicLink(
            string symbolicLinkName,
            string targetFileName,
            SymbolicLinkFlag flags);
#else
        [DllImport("libc", SetLastError = true)]
        internal static extern int symlink(
            string symbolicLinkName,
            string targetFileName);

        [DllImport("libc", CharSet = CharSet.Ansi)]
        internal static extern IntPtr strerror(int errnum);
#endif

        public static bool MakeSymbolicLink(string symbolicLinkName, string targetFileName, out string errorMessage)
        {
            errorMessage = string.Empty;
#if WINDOWS
            if (!CreateSymbolicLink(symbolicLinkName, targetFileName, SymbolicLinkFlag.IsFile))
            {
                int errno = Marshal.GetLastWin32Error();
                return false;
            }
#else
            if (symlink(symbolicLinkName, targetFileName) == -1)
            {
                int errno = Marshal.GetLastWin32Error();
                errorMessage = Marshal.PtrToStringAnsi(strerror(errno));
                return false; 
            }
#endif

            return true;
        }
    }
}