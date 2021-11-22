// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
        /// <summary>
        /// This function closes the specified waveform output device.
        /// </summary>
        /// <param name="hwo">Handle to the waveform-audio output device. If the function
        /// succeeds, the handle is no longer valid after this call.</param>
        /// <returns>MMSYSERR</returns>
        [DllImport(Libraries.WinMM)]
        internal static extern MMSYSERR waveOutClose(IntPtr hwo);
    }
}
