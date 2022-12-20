// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
        /// <summary>
        /// This function stops playback on a specified waveform output device and
        /// resets the current position to 0. All pending playback buffers are marked
        /// as done and returned to the application.
        /// </summary>
        /// <param name="hwo">Handle to the waveform-audio output device.</param>
        /// <returns>MMSYSERR</returns>
        [LibraryImport(Libraries.WinMM)]
        internal static partial MMSYSERR waveOutReset(IntPtr hwo);
    }
}
