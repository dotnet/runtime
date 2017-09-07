// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use FindFirstFile.
        /// </summary>
        [DllImport(Libraries.Kernel32, EntryPoint = "FindFirstFileExW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFindHandle FindFirstFileExPrivate(string lpFileName, FINDEX_INFO_LEVELS fInfoLevelId, ref WIN32_FIND_DATA lpFindFileData, FINDEX_SEARCH_OPS fSearchOp, IntPtr lpSearchFilter, int dwAdditionalFlags);

        internal static SafeFindHandle FindFirstFile(string fileName, ref WIN32_FIND_DATA data)
        {
            fileName = PathInternal.EnsureExtendedPrefixOverMaxPath(fileName);

            // use FindExInfoBasic since we don't care about short name and it has better perf
            return FindFirstFileExPrivate(fileName, FINDEX_INFO_LEVELS.FindExInfoBasic, ref data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, 0);
        }

        internal enum FINDEX_INFO_LEVELS : uint
        {
            FindExInfoStandard = 0x0u,
            FindExInfoBasic = 0x1u,
            FindExInfoMaxInfoLevel = 0x2u,
        }

        internal enum FINDEX_SEARCH_OPS : uint
        {
            FindExSearchNameMatch = 0x0u,
            FindExSearchLimitToDirectories = 0x1u,
            FindExSearchLimitToDevices = 0x2u,
            FindExSearchMaxSearchOp = 0x3u,
        }
    }
}
