// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Forces a write of all modified I/O buffers to their storage mediums.
        /// </summary>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Sync")]
        internal static extern void Sync();
    }
}
