// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Speech.Internal.Synthesis
{
    // This class *MUST* be internal for security purposes
    //CASRemoval:[SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        /// <summary>
        /// This function prepares a waveform data block for playback.
        /// </summary>
        /// <param name="hwo">Handle to the waveform-audio output device.</param>
        /// <param name="pwh">Pointer to a WaveHeader structure that identifies the data
        /// block to be prepared. The buffer's base address must be aligned with the
        /// respect to the sample size.</param>
        /// <param name="cbwh">Size, in bytes, of the WaveHeader structure.</param>
        /// <returns>MMSYSERR</returns>
        [DllImport("winmm.dll")]
        internal static extern MMSYSERR waveOutPrepareHeader(IntPtr hwo, IntPtr pwh, int cbwh);

        /// <summary>
        /// This function sends a data block to the specified waveform output device.
        /// </summary>
        /// <param name="hwo">Handle to the waveform-audio output device.</param>
        /// <param name="pwh">Pointer to a WaveHeader structure containing information
        /// about the data block.</param>
        /// <param name="cbwh">Size, in bytes, of the WaveHeader structure.</param>
        /// <returns>MMSYSERR</returns>
        [DllImport("winmm.dll")]
        internal static extern MMSYSERR waveOutWrite(IntPtr hwo, IntPtr pwh, int cbwh);

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
        [DllImport("winmm.dll")]
        internal static extern MMSYSERR waveOutUnprepareHeader(IntPtr hwo, IntPtr pwh, int cbwh);

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
        [DllImport("winmm.dll")]
        internal static extern MMSYSERR waveOutOpen(ref IntPtr phwo, int uDeviceID, byte[] pwfx, WaveOutProc dwCallback, IntPtr dwInstance, uint fdwOpen);

        /// <summary>
        /// This function closes the specified waveform output device.
        /// </summary>
        /// <param name="hwo">Handle to the waveform-audio output device. If the function
        /// succeeds, the handle is no longer valid after this call.</param>
        /// <returns>MMSYSERR</returns>
        [DllImport("winmm.dll")]
        internal static extern MMSYSERR waveOutClose(IntPtr hwo);

        /// <summary>
        /// This function stops playback on a specified waveform output device and
        /// resets the current position to 0. All pending playback buffers are marked
        /// as done and returned to the application.
        /// </summary>
        /// <param name="hwo">Handle to the waveform-audio output device.</param>
        /// <returns>MMSYSERR</returns>
        [DllImport("winmm.dll")]
        internal static extern MMSYSERR waveOutReset(IntPtr hwo);

        /// <summary>
        /// This function pauses playback on a specified waveform output device. The
        /// current playback position is saved. Use waveOutRestart to resume playback
        /// from the current playback position.
        /// </summary>
        /// <param name="hwo">Handle to the waveform-audio output device.</param>
        /// <returns>MMSYSERR</returns>
        [DllImport("winmm.dll")]
        internal static extern MMSYSERR waveOutPause(IntPtr hwo);

        /// <summary>
        /// This function restarts a paused waveform output device.
        /// </summary>
        /// <param name="hwo">Handle to the waveform-audio output device.</param>
        /// <returns>MMSYSERR</returns>
        [DllImport("winmm.dll")]
        internal static extern MMSYSERR waveOutRestart(IntPtr hwo);

        internal delegate void WaveOutProc(IntPtr hwo, MM_MSG uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

#pragma warning disable CA1823 // unused fields
        internal struct WAVEOUTCAPS
        {
            private ushort wMid;
            private ushort wPid;
            private uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            internal string szPname;
            private uint dwFormats;
            private ushort wChannels;
            private ushort wReserved1;
            private ushort dwSupport;
        }
#pragma warning restore CA1823

        /// <summary>
        /// This function queries a specified waveform device to determine its
        /// capabilities.
        /// </summary>
        /// <param name="uDeviceID">Identifier of the waveform-audio output device.
        /// It can be either a device identifier or a Handle to an open waveform-audio
        /// output device.</param>
        /// <param name="caps">Pointer to a WAVEOUTCAPS structure to be filled with
        /// information about the capabilities of the device.</param>
        /// <param name="cbwoc">Size, in bytes, of the WAVEOUTCAPS structure.</param>
        /// <returns>MMSYSERR</returns>
        [DllImport("winmm.dll")]
        internal static extern MMSYSERR waveOutGetDevCaps(IntPtr uDeviceID, ref WAVEOUTCAPS caps, int cbwoc);

        /// <summary>
        /// This function retrieves the number of waveform output devices present
        /// in the system.
        /// </summary>
        /// <returns>The number of devices indicates success. Zero indicates that
        /// no devices are present or that an error occurred.</returns>
        [DllImport("winmm.dll")]
        internal static extern int waveOutGetNumDevs();

        // Used by MMTIME.wType
        internal const uint TIME_MS = 0x0001;
        internal const uint TIME_SAMPLES = 0x0002;
        internal const uint TIME_BYTES = 0x0004;
        internal const uint TIME_TICKS = 0x0020;

        // Flag specifying the use of a callback window for sound messages
        internal const uint CALLBACK_WINDOW = 0x10000;
        internal const uint CALLBACK_NULL = 0x00000000;
        internal const uint CALLBACK_FUNCTION = 0x00030000;
    }

    #region Internal Types

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

    // Enum equivalent to MMSYSERR_*
    internal enum MMSYSERR : int
    {
        NOERROR = 0,
        ERROR = (1),
        BADDEVICEID = (2),
        NOTENABLED = (3),
        ALLOCATED = (4),
        INVALHANDLE = (5),
        NODRIVER = (6),
        NOMEM = (7),
        NOTSUPPORTED = (8),
        BADERRNUM = (9),
        INVALFLAG = (10),
        INVALPARAM = (11),
        HANDLEBUSY = (12),
        INVALIDALIAS = (13),
        BADDB = (14),
        KEYNOTFOUND = (15),
        READERROR = (16),
        WRITEERROR = (17),
        DELETEERROR = (18),
        VALNOTFOUND = (19),
        NODRIVERCB = (20),
        LASTERROR = (20)
    }

    internal enum MM_MSG
    {
        MM_WOM_OPEN = 0x03BB,
        MM_WOM_CLOSE = 0x03BC,
        MM_WOM_DONE = 0x03BD
    }

    #endregion
}
