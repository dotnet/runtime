// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

partial class Interop
{
    partial class mincore
    {
        /// <summary>
        /// WARNING: This overload does not implicitly handle long paths.
        /// </summary>
        [DllImport(Libraries.CoreFile_L1, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal unsafe static extern int GetFullPathNameW(char* path, int numBufferChars, char* buffer, IntPtr mustBeZero);

        [DllImport(Libraries.CoreFile_L1, EntryPoint = "GetFullPathNameW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern int GetFullPathNameWPrivate(string path, int numBufferChars, [Out]StringBuilder buffer, IntPtr mustBeZero);

        internal static int GetFullPathNameW(string path, int numBufferChars, [Out]StringBuilder buffer, IntPtr mustBeZero)
        {
            bool wasExtended = PathInternal.IsExtended(path);
            if (!wasExtended)
            {
                path = PathInternal.AddExtendedPathPrefixForLongPaths(path);
            }
            int result = GetFullPathNameWPrivate(path, buffer.Capacity, buffer, mustBeZero);

            if (!wasExtended)
            {
                // We don't want to give back \\?\ if we possibly added it ourselves
                PathInternal.RemoveExtendedPathPrefix(buffer);
            }
            return result;
        }
    }
}
