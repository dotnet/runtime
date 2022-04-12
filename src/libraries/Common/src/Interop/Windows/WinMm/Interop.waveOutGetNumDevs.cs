// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
        /// <summary>
        /// This function retrieves the number of waveform output devices present
        /// in the system.
        /// </summary>
        /// <returns>The number of devices indicates success. Zero indicates that
        /// no devices are present or that an error occurred.</returns>
        [LibraryImport(Libraries.WinMM)]
        internal static partial int waveOutGetNumDevs();
    }
}
