// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Writes the specified buffer to the provided open file descriptor
        /// </summary>
        /// <param name="fd">The file descriptor to try and write to</param>
        /// <param name="buffer">The data to attempt to write</param>
        /// <param name="bufferSize">The amount of data to write, in bytes</param>
        /// <returns>
        /// Returns the number of bytes written on success; otherwise, returns -1 and sets errno
        /// </returns>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Write", SetLastError = true)]
        internal static extern unsafe int Write(SafeHandle fd, byte* buffer, int bufferSize);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Write", SetLastError = true)]
        internal static extern unsafe int Write(int fd, byte* buffer, int bufferSize);
    }
}
