// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
        [StructLayout(LayoutKind.Sequential)]
        internal sealed class WAVEFORMATEX
        {
            internal short wFormatTag;
            internal short nChannels;
            internal int nSamplesPerSec;
            internal int nAvgBytesPerSec;
            internal short nBlockAlign;
            internal short wBitsPerSample;
            internal short cbSize;
        }

        internal const int WAVE_FORMAT_PCM = 0x0001;
        internal const int WAVE_FORMAT_ADPCM = 0x0002;
        internal const int WAVE_FORMAT_IEEE_FLOAT = 0x0003;

        [DllImport(Libraries.WinMM)]
        internal static extern int mmioRead(IntPtr hMIO, [MarshalAs(UnmanagedType.LPArray)] byte[] wf, int cch);
    }
}
