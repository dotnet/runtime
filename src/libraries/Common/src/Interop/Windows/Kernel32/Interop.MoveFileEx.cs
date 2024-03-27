// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        private const uint MOVEFILE_REPLACE_EXISTING = 0x01;
        private const uint MOVEFILE_COPY_ALLOWED = 0x02;

        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use MoveFile.
        /// </summary>
        [LibraryImport(Libraries.Kernel32, EntryPoint = "MoveFileExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool MoveFileExPrivate(
            string src, string dst, uint flags);

        /// <summary>
        /// Moves a file or directory, optionally overwriting existing destination file. NOTE: overwrite must be false for directories.
        /// </summary>
        /// <param name="src">Source file or directory</param>
        /// <param name="dst">Destination file or directory</param>
        /// <param name="overwrite">True to overwrite existing destination file. NOTE: must pass false for directories as overwrite of directories is not supported.</param>
        /// <returns></returns>
        internal static bool MoveFile(string src, string dst, bool overwrite)
        {
            src = PathInternal.EnsureExtendedPrefixIfNeeded(src);
            dst = PathInternal.EnsureExtendedPrefixIfNeeded(dst);

            uint flags = MOVEFILE_COPY_ALLOWED;
            if (overwrite)
            {
                flags |= MOVEFILE_REPLACE_EXISTING;
            }

            return MoveFileExPrivate(src, dst, flags);
        }
    }
}
