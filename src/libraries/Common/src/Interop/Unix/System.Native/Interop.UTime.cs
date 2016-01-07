// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal struct UTimBuf
        {
            internal long AcTime;
            internal long ModTime;
        }

        /// <summary>
        /// Sets the last access and last modified time of a file 
        /// </summary>
        /// <param name="path">The path to the item to get time values for</param>
        /// <param name="time">The output time values of the item</param>
        /// <returns>
        /// Returns 0 on success; otherwise, returns -1 
        /// </returns>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_UTime", SetLastError = true)]
        internal static extern int UTime(string path, ref UTimBuf time);
    }
}
