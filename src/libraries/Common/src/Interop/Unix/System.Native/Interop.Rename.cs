// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Renames a file, moving to the correct destination if necessary. There are many edge cases to this call, check man 2 rename for more info
        /// </summary>
        /// <param name="oldPath">Path to the source item</param>
        /// <param name="newPath">Path to the desired new item</param>
        /// <returns>
        /// Returns 0 on success; otherwise, returns -1
        /// </returns>
        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Rename", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static partial int Rename(string oldPath, string newPath);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Rename", SetLastError = true)]
        internal static partial int Rename(ref byte oldPath, ref byte newPath);

        internal static int Rename(ReadOnlySpan<char> oldPath, ReadOnlySpan<char> newPath)
        {
            ValueUtf8Converter converterNewPath = new(stackalloc byte[DefaultPathBufferSize]);
            ValueUtf8Converter converterOldPath = new(stackalloc byte[DefaultPathBufferSize]);
            int result = Rename(
                ref MemoryMarshal.GetReference(converterOldPath.ConvertAndTerminateString(oldPath)),
                ref MemoryMarshal.GetReference(converterNewPath.ConvertAndTerminateString(newPath)));
            converterNewPath.Dispose();
            converterOldPath.Dispose();
            return result;
        }
    }
}
