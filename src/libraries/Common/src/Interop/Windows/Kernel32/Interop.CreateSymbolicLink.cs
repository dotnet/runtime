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
        /// The link target is a file.
        /// </summary>
        internal const int SYMBOLIC_LINK_FLAG_FILE = 0x0;

        /// <summary>
        /// The link target is a directory.
        /// </summary>
        internal const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;

        /// <summary>
        /// Allows creation of symbolic links when the process is not elevated. Starting with Windows 10 Insiders build 14972.
        /// Developer Mode must first be enabled on the machine before this option will function.
        /// </summary>
        internal const int SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 0x2;

        [DllImport(Libraries.Kernel32, EntryPoint = "CreateSymbolicLinkW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = true)]
        private static extern bool CreateSymbolicLinkPrivate(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        /// <summary>
        /// Creates a symbolic link.
        /// </summary>
        /// <param name="symlinkFileName">The symbolic link to be created.</param>
        /// <param name="targetFileName">The name of the target for the symbolic link to be created.
        /// If it has a device name associated with it, the link is treated as an absolute link; otherwise, the link is treated as a relative link.</param>
        /// <param name="isDirectory"><see langword="true" /> if the link target is a directory; <see langword="false" /> otherwise.</param>
        /// <returns><see langword="true" /> if the operation succeeds; <see langword="false" /> otherwise.</returns>
        internal static bool CreateSymbolicLink(string symlinkFileName, string targetFileName, bool isDirectory)
        {
            symlinkFileName = PathInternal.EnsureExtendedPrefixIfNeeded(symlinkFileName);
            targetFileName = PathInternal.EnsureExtendedPrefixIfNeeded(targetFileName);

            int flags = SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE |
                (isDirectory ? SYMBOLIC_LINK_FLAG_DIRECTORY : SYMBOLIC_LINK_FLAG_FILE);

            return CreateSymbolicLinkPrivate(symlinkFileName, targetFileName, flags);
        }
    }
}
