// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Forces a write of all modified I/O buffers to their storage mediums.
        /// </summary>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_Sync")]
        internal static partial void Sync();
    }
}
