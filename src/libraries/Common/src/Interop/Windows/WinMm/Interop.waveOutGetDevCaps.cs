// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
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
#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable types.
        [DllImport(Libraries.WinMM)]
        internal static extern MMSYSERR waveOutGetDevCaps(IntPtr uDeviceID, ref WAVEOUTCAPS caps, int cbwoc);
#pragma warning restore DLLIMPORTGENANALYZER015
    }
}
