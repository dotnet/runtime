// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
