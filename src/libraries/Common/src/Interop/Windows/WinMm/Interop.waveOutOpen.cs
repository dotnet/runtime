// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
        internal enum MM_MSG
        {
            MM_WOM_OPEN = 0x03BB,
            MM_WOM_CLOSE = 0x03BC,
            MM_WOM_DONE = 0x03BD
        }

        // Flag specifying the use of a callback window for sound messages
        internal const uint CALLBACK_WINDOW = 0x10000;
        internal const uint CALLBACK_NULL = 0x00000000;
        internal const uint CALLBACK_FUNCTION = 0x00030000;

        internal delegate void WaveOutProc(IntPtr hwo, MM_MSG uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

        /// <summary>
        /// This function opens a specified waveform output device for playback.
        /// </summary>
        /// <param name="phwo">Address filled with a handle identifying the open
        /// waveform-audio output device. Use the handle to identify the device
        /// when calling other waveform-audio output functions. This parameter might
        /// be NULL if the WAVE_FORMAT_QUERY flag is specified for fdwOpen.</param>
        /// <param name="uDeviceID">Identifier of the waveform-audio output device to
        /// open. It can be either a device identifier or a Handle to an open
        /// waveform-audio input device.</param>
        /// <param name="pwfx">Pointer to a WaveFormat structure that identifies
        /// the format of the waveform-audio data to be sent to the device. You can
        /// free this structure immediately after passing it to waveOutOpen.</param>
        /// <param name="dwCallback">Specifies the address of a fixed callback function,
        /// an event handle, a handle to a window, or the identifier of a thread to be
        /// called during waveform-audio playback to process messages related to the
        /// progress of the playback. If no callback function is required, this value
        /// can be zero.</param>
        /// <param name="dwInstance">Specifies user-instance data passed to the
        /// callback mechanism. This parameter is not used with the window callback
        /// mechanism.</param>
        /// <param name="fdwOpen">Flags for opening the device.</param>
        /// <returns>MMSYSERR</returns>
        [LibraryImport(Libraries.WinMM)]
        internal static partial MMSYSERR waveOutOpen(ref IntPtr phwo, int uDeviceID, byte[] pwfx, WaveOutProc dwCallback, IntPtr dwInstance, uint fdwOpen);
    }
}
