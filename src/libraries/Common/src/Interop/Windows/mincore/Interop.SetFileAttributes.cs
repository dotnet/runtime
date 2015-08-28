// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use SetFileAttributes.
        /// </summary>
        [DllImport(Libraries.CoreFile_L1, EntryPoint = "SetFileAttributesW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern bool SetFileAttributesPrivate(string name, int attr);

        internal static bool SetFileAttributes(string name, int attr)
        {
            name = PathInternal.EnsureExtendedPrefixOverMaxPath(name);
            return SetFileAttributesPrivate(name, attr);
        }
    }
}
