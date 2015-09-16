// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Reads a number of bytes from an open file descriptor into a specified buffer.
        /// </summary>
        /// <param name="fd">The open file descriptor to try to read from</param>
        /// <param name="buffer">The buffer to read info into</param>
        /// <param name="count">The size of the buffer</param>
        /// <returns>
        /// Returns the number of bytes read on success; otherwise, -1 is returned
        /// Note - on fail. the position of the stream may change depending on the platform; consult man 2 read for more info
        /// </returns>
        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static unsafe extern int Read(int fd, byte* buffer, int count);
    }
}
