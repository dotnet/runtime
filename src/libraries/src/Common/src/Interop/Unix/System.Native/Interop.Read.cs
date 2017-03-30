// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

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
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Read", SetLastError = true)]
        internal static extern unsafe int Read(SafeFileHandle fd, byte* buffer, int count);
    }
}
