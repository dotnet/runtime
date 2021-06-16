// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, EntryPoint = "ReplaceFileW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern bool ReplaceFilePrivate(
            string replacedFileName, string replacementFileName, string? backupFileName,
            int dwReplaceFlags, IntPtr lpExclude, IntPtr lpReserved);

        internal static bool ReplaceFile(
            string replacedFileName, string replacementFileName, string? backupFileName,
            int dwReplaceFlags, IntPtr lpExclude, IntPtr lpReserved)
        {
            replacedFileName = PathInternal.EnsureExtendedPrefixIfNeeded(replacedFileName);
            replacementFileName = PathInternal.EnsureExtendedPrefixIfNeeded(replacementFileName);
            backupFileName = PathInternal.EnsureExtendedPrefixIfNeeded(backupFileName);

            return ReplaceFilePrivate(
                replacedFileName, replacementFileName, backupFileName,
                dwReplaceFlags, lpExclude, lpReserved);
        }

        internal const int REPLACEFILE_IGNORE_MERGE_ERRORS = 0x2;
    }
}
