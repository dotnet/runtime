// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal static void CreateHardLink(string hardLinkFilePath, string targetFilePath)
        {
            string originalPath = hardLinkFilePath;
            hardLinkFilePath = PathInternal.EnsureExtendedPrefix(hardLinkFilePath);
            targetFilePath = PathInternal.EnsureExtendedPrefix(targetFilePath);

            if (!CreateHardLinkPrivate(hardLinkFilePath, targetFilePath, IntPtr.Zero))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error(originalPath);
            }
        }

        [LibraryImport(Libraries.Kernel32, EntryPoint = "CreateHardLinkW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CreateHardLinkPrivate(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
    }
}
