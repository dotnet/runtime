// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Creates a pseudo-terminal pair with the specified window size.
        /// </summary>
        /// <returns>0 on success, -1 on failure with errno set.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_OpenPseudoTerminal", SetLastError = true)]
        internal static partial int OpenPseudoTerminal(out int primaryFd, out int secondaryFd, int columns, int rows);

        /// <summary>
        /// Resizes the pseudo-terminal to the specified dimensions.
        /// </summary>
        /// <returns>0 on success, -1 on failure with errno set.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_ResizePseudoTerminal", SetLastError = true)]
        internal static partial int ResizePseudoTerminal(SafeFileHandle primaryFd, int columns, int rows);
    }
}
