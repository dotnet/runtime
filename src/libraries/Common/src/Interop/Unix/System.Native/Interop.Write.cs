// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

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
        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static unsafe extern int Write(int fd, byte* buffer, int bufferSize);
    }
}
