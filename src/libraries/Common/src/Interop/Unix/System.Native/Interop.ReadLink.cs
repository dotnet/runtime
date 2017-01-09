// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Takes a path to a symbolic link and attempts to place the link target path into the buffer. If the buffer is too
        /// small, the path will be truncated. No matter what, the buffer will not be null terminated. 
        /// </summary>
        /// <param name="path">The path to the symlink</param>
        /// <param name="buffer">The buffer to hold the output path</param>
        /// <param name="bufferSize">The size of the buffer</param>
        /// <returns>
        /// Returns the number of bytes placed into the buffer on success; otherwise, -1 is returned
        /// </returns>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ReadLink", SetLastError = true)]
        internal static extern unsafe int ReadLink(string path, byte[] buffer, int bufferSize);
    }
}
