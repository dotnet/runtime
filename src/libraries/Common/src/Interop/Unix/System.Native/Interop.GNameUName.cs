// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Buffers;
using System.Text;
using System;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Gets the group name associated to the specified group ID.
        /// </summary>
        /// <param name="gid">The group ID.</param>
        /// <returns>On success, return a string with the group name. On failure, returns a null string.</returns>
        internal static string? GetGName(uint gid) => GetGNameOrUName(gid, isGName: true);

        /// <summary>
        /// Gets the user name associated to the specified user ID.
        /// </summary>
        /// <param name="uid">The user ID.</param>
        /// <returns>On success, return a string with the user name. On failure, returns a null string.</returns>
        internal static string? GetUName(uint uid) => GetGNameOrUName(uid, isGName: false);

        private static string? GetGNameOrUName(uint id, bool isGName)
        {
            // Common max name length, like /etc/passwd, useradd, groupadd
            int outputBufferSize = 32;

            while (true)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(outputBufferSize);
                try
                {
                    int resultLength = isGName ?
                        Interop.Sys.GetGName(id, buffer, buffer.Length) :
                        Interop.Sys.GetUName(id, buffer, buffer.Length);

                    if (resultLength < 0)
                    {
                        // error
                        return null;
                    }
                    else if (resultLength < buffer.Length)
                    {
                        // success
                        return Encoding.UTF8.GetString(buffer, 0, resultLength);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                // Output buffer was too small, loop around again and try with a larger buffer.
                outputBufferSize = buffer.Length * 2;

                if (outputBufferSize > 256) // Upper limit allowed for login name in kernel
                {
                    return null;
                }
            }
        }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetGName", SetLastError = true)]
        private static partial int GetGName(uint uid, byte[] buffer, int bufferSize);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetUName", SetLastError = true)]
        private static partial int GetUName(uint uid, byte[] buffer, int bufferSize);
    }
}
