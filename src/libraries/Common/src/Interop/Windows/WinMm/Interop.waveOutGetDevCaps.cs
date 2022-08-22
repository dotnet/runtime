// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

internal static partial class Interop
{
    internal static partial class WinMM
    {
#pragma warning disable CA1823 // unused fields
#if NET7_0_OR_GREATER
        [NativeMarshalling(typeof(Marshaller))]
#endif
        internal struct WAVEOUTCAPS
        {
            private const int szPnameLength = 32;
            private ushort wMid;
            private ushort wPid;
            private uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = szPnameLength)]
            internal string szPname;
            private uint dwFormats;
            private ushort wChannels;
            private ushort wReserved1;
            private ushort dwSupport;
#if NET7_0_OR_GREATER
            [CustomMarshaller(typeof(WAVEOUTCAPS), MarshalMode.Default, typeof(Marshaller))]
            public static class Marshaller
            {
                public static Native ConvertToUnmanaged(WAVEOUTCAPS managed) => new(managed);
                public static WAVEOUTCAPS ConvertToManaged(Native native) => native.ToManaged();

                internal unsafe struct Native
                {
                    private ushort wMid;
                    private ushort wPid;
                    private uint vDriverVersion;
                    internal fixed char szPname[szPnameLength];
                    private uint dwFormats;
                    private ushort wChannels;
                    private ushort wReserved1;
                    private ushort dwSupport;

                    public Native(WAVEOUTCAPS managed)
                    {
                        wMid = managed.wMid;
                        wPid = managed.wPid;
                        vDriverVersion = managed.vDriverVersion;
                        managed.szPname.CopyTo(MemoryMarshal.CreateSpan(ref szPname[0], szPnameLength));
                        dwFormats = managed.dwFormats;
                        wChannels = managed.wChannels;
                        wReserved1 = managed.wReserved1;
                        dwSupport = managed.dwSupport;
                    }

                    public WAVEOUTCAPS ToManaged() =>
                        new WAVEOUTCAPS
                        {
                            wMid = wMid,
                            wPid = wPid,
                            vDriverVersion = vDriverVersion,
                            szPname = MemoryMarshal.CreateReadOnlySpan(ref szPname[0], szPnameLength).ToString(),
                            dwFormats = dwFormats,
                            wChannels = wChannels,
                            wReserved1 = wReserved1,
                            dwSupport = dwSupport,
                        };
                }
            }
#endif
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
        [LibraryImport(Libraries.WinMM, EntryPoint = "waveOutGetDevCapsW")]
        internal static partial MMSYSERR waveOutGetDevCaps(IntPtr uDeviceID, ref WAVEOUTCAPS caps, int cbwoc);
    }
}
