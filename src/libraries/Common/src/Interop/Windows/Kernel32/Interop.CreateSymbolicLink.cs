// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        /// <summary>
        /// The link target is a directory.
        /// </summary>
        internal const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;

        /// <summary>
        /// Allows creation of symbolic links from a process that is not elevated. Requires Windows 10 Insiders build 14972 or later.
        /// Developer Mode must first be enabled on the machine before this option will function.
        /// </summary>
        internal const int SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 0x2;

        [LibraryImport(Libraries.Kernel32, EntryPoint = "CreateSymbolicLinkW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static partial bool CreateSymbolicLinkPrivate(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        /// <summary>
        /// Creates a symbolic link.
        /// </summary>
        /// <param name="symlinkFileName">The symbolic link to be created.</param>
        /// <param name="targetFileName">The name of the target for the symbolic link to be created.
        /// If it has a device name associated with it, the link is treated as an absolute link; otherwise, the link is treated as a relative link.</param>
        /// <param name="isDirectory"><see langword="true" /> if the link target is a directory; <see langword="false" /> otherwise.</param>
        internal static void CreateSymbolicLink(string symlinkFileName, string targetFileName, bool isDirectory)
        {
            string originalPath = symlinkFileName;
            symlinkFileName = PathInternal.EnsureExtendedPrefixIfNeeded(symlinkFileName);
            targetFileName = PathInternal.EnsureExtendedPrefixIfNeeded(targetFileName);

            int flags = 0;

            Version osVersion = Environment.OSVersion.Version;
            bool isAtLeastWin10Build14972 = osVersion.Major >= 11 || osVersion.Major == 10 && osVersion.Build >= 14972;

            if (isAtLeastWin10Build14972)
            {
                flags = SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE;
            }

            if (isDirectory)
            {
                flags |= SYMBOLIC_LINK_FLAG_DIRECTORY;
            }

            bool success = CreateSymbolicLinkPrivate(symlinkFileName, targetFileName, flags);

            if (!success)
            {
                throw Win32Marshal.GetExceptionForLastWin32Error(originalPath);
            }
        }
    }
}
