// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

#nullable enable

namespace Microsoft.DotNet.CoreSetup.Test
{
    public sealed class SymLink : IDisposable
    {
        public string SrcPath { get; private set; }
        public SymLink(string src, string dest)
        {
            if (!MakeSymbolicLink(src, dest, out var errorMessage))
            {
                throw new IOException($"Error creating symbolic link at {src} pointing to {dest}: {errorMessage}");
            }
            SrcPath = src;
        }

        public void Dispose()
        {
            if (SrcPath is not null)
            {
                File.Delete(SrcPath);
                SrcPath = null!;
            }
        }

        private static bool MakeSymbolicLink(string symbolicLinkName, string targetFileName, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (OperatingSystem.IsWindows())
            {
                if (!CreateSymbolicLink(symbolicLinkName, targetFileName, SymbolicLinkFlag.IsFile))
                {
                    int errno = Marshal.GetLastWin32Error();
                    errorMessage = $"CreateSymbolicLink failed with error number {errno}";
                    return false;
                }
            }
            else
            {
                if (symlink(targetFileName, symbolicLinkName) == -1)
                {
                    int errno = Marshal.GetLastWin32Error();
                    errorMessage = Marshal.PtrToStringAnsi(strerror(errno))!;
                    return false;
                }
            }

            return true;
        }

        [Flags]
        private enum SymbolicLinkFlag
        {
            IsFile = 0x0,
            IsDirectory = 0x1,
            AllowUnprivilegedCreate = 0x2
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool CreateSymbolicLink(
            string symbolicLinkName,
            string targetFileName,
            SymbolicLinkFlag flags);

        [DllImport("libc", SetLastError = true)]
        private static extern int symlink(
            string targetFileName,
            string linkPath);

        [DllImport("libc", CharSet = CharSet.Ansi)]
        private static extern IntPtr strerror(int errnum);
    }
}
