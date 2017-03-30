// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use CopyFileEx.
        /// </summary>
        [DllImport(Libraries.Kernel32, EntryPoint = "CopyFileExW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern bool CopyFileExPrivate(
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
            src = PathInternal.EnsureExtendedPrefixOverMaxPath(src);
            dst = PathInternal.EnsureExtendedPrefixOverMaxPath(dst);
            return CopyFileExPrivate(src, dst, progressRoutine, progressData, ref cancel, flags);
        }
    }
}
