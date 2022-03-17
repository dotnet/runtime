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
        /// WARNING: This method does not implicitly handle long paths. Use CopyFileEx.
        /// </summary>
        [LibraryImport(Libraries.Kernel32, EntryPoint = "CopyFileExW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CopyFileExPrivate(
            string src,
            string dst,
            IntPtr progressRoutine,
            IntPtr progressData,
            ref int cancel,
            int flags);

        internal static bool CopyFileEx(
            string src,
            string dst,
            IntPtr progressRoutine,
            IntPtr progressData,
            ref int cancel,
            int flags)
        {
            src = PathInternal.EnsureExtendedPrefixIfNeeded(src);
            dst = PathInternal.EnsureExtendedPrefixIfNeeded(dst);
            return CopyFileExPrivate(src, dst, progressRoutine, progressData, ref cancel, flags);
        }
    }
}
