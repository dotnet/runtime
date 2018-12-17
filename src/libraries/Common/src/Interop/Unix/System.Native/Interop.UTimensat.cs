// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal struct TimeSpec
        {
            internal long TvSec;
            internal long TvNsec;
        }

        /// <summary>
        /// Sets the last access and last modified time of a file 
        /// </summary>
        /// <param name="path">The path to the item to get time values for</param>
        /// <param name="times">The output time values of the item</param>
        /// <returns>
        /// Returns 0 on success; otherwise, returns -1 
        /// </returns>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_UTimensat", SetLastError = true)]
        internal static unsafe extern int UTimensat(string path, TimeSpec* times);
    }
}
