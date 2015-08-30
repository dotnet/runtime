// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class mincore
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use GetFullPathName.
        /// </summary>
        [DllImport(Libraries.CoreFile_L1, EntryPoint = "GetFullPathNameW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal unsafe static extern int GetFullPathNameUnsafe(char* path, int numBufferChars, char* buffer, IntPtr mustBeZero);

        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use GetFullPathName.
        /// </summary>
        [DllImport(Libraries.CoreFile_L1, EntryPoint = "GetFullPathNameW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern int GetFullPathNamePrivate(string path, int numBufferChars, [Out]StringBuilder buffer, IntPtr mustBeZero);

        internal static int GetFullPathName(string path, int numBufferChars, [Out]StringBuilder buffer)
        {
            // GetFullPathName is backwards from most APIs. It handles long "DOS" paths without a problem. While it understands
            // extended paths (\\?\), it roots them incorrectly, eating into the volume name with relative paths.

            PathInternal.ExtendedType extendedType = PathInternal.RemoveExtendedPrefix(ref path);
            int result = GetFullPathNamePrivate(path, buffer.Capacity, buffer, IntPtr.Zero);

            if ((extendedType & PathInternal.ExtendedType.Extended) != 0)
            {
                // We want to *always* give back whatever we removed
                if (extendedType.HasFlag(PathInternal.ExtendedType.ExtendedUnc))
                {
                    buffer.Insert(2, PathInternal.UncExtendedPrefixToInsert);
                }
                else
                {
                    buffer.Insert(0, PathInternal.ExtendedPathPrefix);
                }
            }
            return result;
        }
    }
}
