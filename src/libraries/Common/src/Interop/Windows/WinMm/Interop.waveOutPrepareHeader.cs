// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
        /// <summary>
        /// MM WAVEHDR structure
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct WAVEHDR
        {
            internal IntPtr lpData; // disposed by the GCHandle
            internal uint dwBufferLength;
            internal uint dwBytesRecorded;
            internal uint dwUser;
            internal uint dwFlags;
            internal uint dwLoops;
            internal IntPtr lpNext; // unused
            internal uint reserved;
        }

        /// <summary>
        /// This function prepares a waveform data block for playback.
        /// </summary>
        /// <param name="hwo">Handle to the waveform-audio output device.</param>
        /// <param name="pwh">Pointer to a WaveHeader structure that identifies the data
        /// block to be prepared. The buffer's base address must be aligned with the
        /// respect to the sample size.</param>
        /// <param name="cbwh">Size, in bytes, of the WaveHeader structure.</param>
        /// <returns>MMSYSERR</returns>
        [LibraryImport(Libraries.WinMM)]
        internal static partial MMSYSERR waveOutPrepareHeader(IntPtr hwo, IntPtr pwh, int cbwh);

        /// <summary>
        /// This function cleans up the preparation performed by waveOutPrepareHeader.
        /// The function must be called after the device driver is finished with a data
        /// block. You must call this function before freeing the data buffer.
        /// </summary>
        /// <param name="hwo">Handle to the waveform-audio output device.</param>
        /// <param name="pwh">Pointer to a WaveHeader structure identifying the data block
        /// to be cleaned up.</param>
        /// <param name="cbwh">Size, in bytes, of the WaveHeader structure.</param>
        /// <returns>MMSYSERR</returns>
        [LibraryImport(Libraries.WinMM)]
        internal static partial MMSYSERR waveOutUnprepareHeader(IntPtr hwo, IntPtr pwh, int cbwh);
    }
}
