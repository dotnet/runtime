// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO;

#if NETFRAMEWORK

[Flags]
internal enum UnixFileMode
{
    None = 0,
    OtherExecute = 1,
    OtherWrite = 2,
    OtherRead = 4,
    GroupExecute = 8,
    GroupWrite = 16,
    GroupRead = 32,
    UserExecute = 64,
    UserWrite = 128,
    UserRead = 256,
    StickyBit = 512,
    SetGroup = 1024,
    SetUser = 2048,
}

internal static class FileExtensions
{
    extension(File)
    {
        public static void SetUnixFileMode(string path, UnixFileMode mode)
        {
            int user = ((mode & UnixFileMode.UserRead) != 0 ? 4 : 0)
                     | ((mode & UnixFileMode.UserWrite) != 0 ? 2 : 0)
                     | ((mode & UnixFileMode.UserExecute) != 0 ? 1 : 0);
            int group = ((mode & UnixFileMode.GroupRead) != 0 ? 4 : 0)
                      | ((mode & UnixFileMode.GroupWrite) != 0 ? 2 : 0)
                      | ((mode & UnixFileMode.GroupExecute) != 0 ? 1 : 0);
            int other = ((mode & UnixFileMode.OtherRead) != 0 ? 4 : 0)
                      | ((mode & UnixFileMode.OtherWrite) != 0 ? 2 : 0)
                      | ((mode & UnixFileMode.OtherExecute) != 0 ? 1 : 0);
            int octal = (user << 6) | (group << 3) | other;

            const int EINTR = 4;
            int res;
            int iterations = 0;
            do
            {
                res = chmod(path, octal);
            } while (res == -1
                && Marshal.GetLastWin32Error() == EINTR
                && iterations++ < 8);
            if (res == -1)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not set file permission {Convert.ToString(octal, 8)} for {path}.");
            }
        }
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int chmod([MarshalAs(UnmanagedType.LPStr)] string pathname, int mode);
}
#endif
