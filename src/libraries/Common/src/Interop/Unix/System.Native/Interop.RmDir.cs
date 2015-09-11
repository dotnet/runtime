// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Deletes the specified empty directory.
        /// </summary>
        /// <param name="path">The path of the directory to delete</param>
        /// <returns>
        /// Returns 0 on success; otherwise, returns -1
        /// </returns>
        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int RmDir(string path);
    }
}
