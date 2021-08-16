// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Speech.AudioFormat;

namespace System.Speech.Internal
{
    // Helper class which wraps AudioFormat and handles WaveFormatEx variable sized structure
    internal static class AudioFormatConverter
    {
        #region Internal Methods

        internal static SpeechAudioFormatInfo ToSpeechAudioFormatInfo(IntPtr waveFormatPtr)
        {
            WaveFormatEx waveFormatEx = (WaveFormatEx)Marshal.PtrToStructure(waveFormatPtr, typeof(WaveFormatEx));

            byte[] extraData = new byte[waveFormatEx.cbSize];
            IntPtr extraDataPtr = new(waveFormatPtr.ToInt64() + Marshal.SizeOf(waveFormatEx));
            for (int i = 0; i < waveFormatEx.cbSize; i++)
            {
                extraData[i] = Marshal.ReadByte(extraDataPtr, i);
            }

            return new SpeechAudioFormatInfo((EncodingFormat)waveFormatEx.wFormatTag, (int)waveFormatEx.nSamplesPerSec, (short)waveFormatEx.wBitsPerSample, (short)waveFormatEx.nChannels, (int)waveFormatEx.nAvgBytesPerSec, (short)waveFormatEx.nBlockAlign, extraData);
        }

        internal static SpeechAudioFormatInfo ToSpeechAudioFormatInfo(string formatString)
        {
            // Is it normal format?
            short streamFormat;
            if (short.TryParse(formatString, NumberStyles.None, CultureInfo.InvariantCulture, out streamFormat))
            {
                // Now convert enum value into real info
                return ConvertFormat((StreamFormat)streamFormat);
            }
            return null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This method converts the specified stream format into a wave format
        /// </summary>
        private static SpeechAudioFormatInfo ConvertFormat(StreamFormat eFormat)
        {
            WaveFormatEx waveEx = new();
            byte[] extra = null;

            if (eFormat >= StreamFormat.PCM_8kHz8BitMono && eFormat <= StreamFormat.PCM_48kHz16BitStereo)
            {
                uint index = (uint)(eFormat - StreamFormat.PCM_8kHz8BitMono);
                bool isStereo = (index & 0x1) != 0;
                bool is16 = (index & 0x2) != 0;
                uint dwKHZ = (index & 0x3c) >> 2;
                uint[] adwKHZ = new uint[] { 8000, 11025, 12000, 16000, 22050, 24000, 32000, 44100, 48000 };
                waveEx.wFormatTag = (ushort)WaveFormatId.Pcm;
                waveEx.nChannels = waveEx.nBlockAlign = (ushort)(isStereo ? 2 : 1);
                waveEx.nSamplesPerSec = adwKHZ[dwKHZ];
                waveEx.wBitsPerSample = 8;
                if (is16)
                {
                    waveEx.wBitsPerSample *= 2;
                    waveEx.nBlockAlign *= 2;
                }
                waveEx.nAvgBytesPerSec = waveEx.nSamplesPerSec * waveEx.nBlockAlign;
            }
            else if (eFormat == StreamFormat.TrueSpeech_8kHz1BitMono)
            {
                waveEx.wFormatTag = (ushort)WaveFormatId.TrueSpeech;
                waveEx.nChannels = 1;
                waveEx.nSamplesPerSec = 8000;
                waveEx.nAvgBytesPerSec = 1067;
                waveEx.nBlockAlign = 32;
                waveEx.wBitsPerSample = 1;
                waveEx.cbSize = 32;
                extra = new byte[32];
                extra[0] = 1;
                extra[2] = 0xF0;
            }
            else if ((eFormat >= StreamFormat.CCITT_ALaw_8kHzMono) && (eFormat <= StreamFormat.CCITT_ALaw_44kHzStereo))
            {
                uint index = (uint)(eFormat - StreamFormat.CCITT_ALaw_8kHzMono);
                uint dwKHZ = index / 2;
                uint[] adwKHZ = { 8000, 11025, 22050, 44100 };
                bool isStereo = (index & 0x1) != 0;
                waveEx.wFormatTag = (ushort)WaveFormatId.Alaw;
                waveEx.nChannels = waveEx.nBlockAlign = (ushort)(isStereo ? 2 : 1);
                waveEx.nSamplesPerSec = adwKHZ[dwKHZ];
                waveEx.wBitsPerSample = 8;
                waveEx.nAvgBytesPerSec = waveEx.nSamplesPerSec * waveEx.nBlockAlign;
            }
            else if ((eFormat >= StreamFormat.CCITT_uLaw_8kHzMono) &&
                (eFormat <= StreamFormat.CCITT_uLaw_44kHzStereo))
            {
                uint index = (uint)(eFormat - StreamFormat.CCITT_uLaw_8kHzMono);
                uint dwKHZ = index / 2;
                uint[] adwKHZ = new uint[] { 8000, 11025, 22050, 44100 };
                bool isStereo = (index & 0x1) != 0;
                waveEx.wFormatTag = (ushort)WaveFormatId.Mulaw;
                waveEx.nChannels = waveEx.nBlockAlign = (ushort)(isStereo ? 2 : 1);
                waveEx.nSamplesPerSec = adwKHZ[dwKHZ];
                waveEx.wBitsPerSample = 8;
                waveEx.nAvgBytesPerSec = waveEx.nSamplesPerSec * waveEx.nBlockAlign;
            }
            else if ((eFormat >= StreamFormat.ADPCM_8kHzMono) &&
                (eFormat <= StreamFormat.ADPCM_44kHzStereo))
            {
                //--- Some of these values seem odd. We used what the codec told us.
                uint[] adwKHZ = new uint[] { 8000, 11025, 22050, 44100 };
                uint[] BytesPerSec = new uint[] { 4096, 8192, 5644, 11289, 11155, 22311, 22179, 44359 };
                uint[] BlockAlign = new uint[] { 256, 256, 512, 1024 };
                byte[] Extra811 = new byte[32]
            {
                0xF4, 0x01, 0x07, 0x00, 0x00, 0x01, 0x00, 0x00,
                0x00, 0x02, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00,
                0xC0, 0x00, 0x40, 0x00, 0xF0, 0x00, 0x00, 0x00,
                0xCC, 0x01, 0x30, 0xFF, 0x88, 0x01, 0x18, 0xFF
            };

                byte[] Extra22 = new byte[32]
            {
                0xF4, 0x03, 0x07, 0x00, 0x00, 0x01, 0x00, 0x00,
                0x00, 0x02, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00,
                0xC0, 0x00, 0x40, 0x00, 0xF0, 0x00, 0x00, 0x00,
                0xCC, 0x01, 0x30, 0xFF, 0x88, 0x01, 0x18, 0xFF
            };

                byte[] Extra44 = new byte[32]
            {
                0xF4, 0x07, 0x07, 0x00, 0x00, 0x01, 0x00, 0x00,
                0x00, 0x02, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00,
                0xC0, 0x00, 0x40, 0x00, 0xF0, 0x00, 0x00, 0x00,
                0xCC, 0x01, 0x30, 0xFF, 0x88, 0x01, 0x18, 0xFF
            };

                byte[][] Extra = new byte[][] { Extra811, Extra811, Extra22, Extra44 };
                uint index = (uint)(eFormat - StreamFormat.ADPCM_8kHzMono);
                uint dwKHZ = index / 2;
                bool isStereo = (index & 0x1) != 0;
                waveEx.wFormatTag = (ushort)WaveFormatId.AdPcm;
                waveEx.nChannels = (ushort)(isStereo ? 2 : 1);
                waveEx.nSamplesPerSec = adwKHZ[dwKHZ];
                waveEx.nAvgBytesPerSec = BytesPerSec[index];
                waveEx.nBlockAlign = (ushort)(BlockAlign[dwKHZ] * waveEx.nChannels);
                waveEx.wBitsPerSample = 4;
                waveEx.cbSize = 32;
                extra = (byte[])Extra[dwKHZ].Clone();
            }
            else if ((eFormat >= StreamFormat.GSM610_8kHzMono) &&
                (eFormat <= StreamFormat.GSM610_44kHzMono))
            {
                //--- Some of these values seem odd. We used what the codec told us.
                uint[] adwKHZ = new uint[] { 8000, 11025, 22050, 44100 };
                uint[] BytesPerSec = new uint[] { 1625, 2239, 4478, 8957 };
                uint index = (uint)(eFormat - StreamFormat.GSM610_8kHzMono);
                waveEx.wFormatTag = (ushort)WaveFormatId.Gsm610;
                waveEx.nChannels = 1;
                waveEx.nSamplesPerSec = adwKHZ[index];
                waveEx.nAvgBytesPerSec = BytesPerSec[index];
                waveEx.nBlockAlign = 65;
                waveEx.wBitsPerSample = 0;
                waveEx.cbSize = 2;
                extra = new byte[2];
                extra[0] = 0x40;
                extra[1] = 0x01;
            }
            else
            {
                waveEx = null;
                switch (eFormat)
                {
                    case StreamFormat.NoAssignedFormat:
                        break;

                    case StreamFormat.Text:
                        break;

                    default:
                        throw new FormatException();
                }
            }

            return waveEx != null ? new SpeechAudioFormatInfo((EncodingFormat)waveEx.wFormatTag, (int)waveEx.nSamplesPerSec, waveEx.wBitsPerSample, waveEx.nChannels, (int)waveEx.nAvgBytesPerSec, waveEx.nBlockAlign, extra) : null;
        }

        private enum StreamFormat
        {
            Default = -1,
            NoAssignedFormat = 0,  // Similar to GUID_NULL
            Text,
            NonStandardFormat,     // Non-SAPI 5.1 standard format with no WAVEFORMATEX description
            ExtendedAudioFormat,   // Non-SAPI 5.1 standard format but has WAVEFORMATEX description
            // Standard PCM wave formats
            PCM_8kHz8BitMono,
            PCM_8kHz8BitStereo,
            PCM_8kHz16BitMono,
            PCM_8kHz16BitStereo,
            PCM_11kHz8BitMono,
            PCM_11kHz8BitStereo,
            PCM_11kHz16BitMono,
            PCM_11kHz16BitStereo,
            PCM_12kHz8BitMono,
            PCM_12kHz8BitStereo,
            PCM_12kHz16BitMono,
            PCM_12kHz16BitStereo,
            PCM_16kHz8BitMono,
            PCM_16kHz8BitStereo,
            PCM_16kHz16BitMono,
            PCM_16kHz16BitStereo,
            PCM_22kHz8BitMono,
            PCM_22kHz8BitStereo,
            PCM_22kHz16BitMono,
            PCM_22kHz16BitStereo,
            PCM_24kHz8BitMono,
            PCM_24kHz8BitStereo,
            PCM_24kHz16BitMono,
            PCM_24kHz16BitStereo,
            PCM_32kHz8BitMono,
            PCM_32kHz8BitStereo,
            PCM_32kHz16BitMono,
            PCM_32kHz16BitStereo,
            PCM_44kHz8BitMono,
            PCM_44kHz8BitStereo,
            PCM_44kHz16BitMono,
            PCM_44kHz16BitStereo,
            PCM_48kHz8BitMono,
            PCM_48kHz8BitStereo,
            PCM_48kHz16BitMono,
            PCM_48kHz16BitStereo,
            // TrueSpeech format

            TrueSpeech_8kHz1BitMono,
            // A-Law formats
            CCITT_ALaw_8kHzMono,
            CCITT_ALaw_8kHzStereo,
            CCITT_ALaw_11kHzMono,
            CCITT_ALaw_11kHzStereo,
            CCITT_ALaw_22kHzMono,
            CCITT_ALaw_22kHzStereo,
            CCITT_ALaw_44kHzMono,
            CCITT_ALaw_44kHzStereo,
            // u-Law formats
            CCITT_uLaw_8kHzMono,
            CCITT_uLaw_8kHzStereo,
            CCITT_uLaw_11kHzMono,
            CCITT_uLaw_11kHzStereo,
            CCITT_uLaw_22kHzMono,
            CCITT_uLaw_22kHzStereo,
            CCITT_uLaw_44kHzMono,
            CCITT_uLaw_44kHzStereo,
            // ADPCM formats
            ADPCM_8kHzMono,
            ADPCM_8kHzStereo,
            ADPCM_11kHzMono,
            ADPCM_11kHzStereo,
            ADPCM_22kHzMono,
            ADPCM_22kHzStereo,
            ADPCM_44kHzMono,
            ADPCM_44kHzStereo,
            // GSM 6.10 formats
            GSM610_8kHzMono,
            GSM610_11kHzMono,
            GSM610_22kHzMono,
            GSM610_44kHzMono,
            NUM_FORMATS
        }

        #endregion

        #region Private Type

        private enum WaveFormatId
        {
            Pcm = 1,
            AdPcm = 0x0002,
            TrueSpeech = 0x0022,
            Alaw = 0x0006,
            Mulaw = 0x0007,
            Gsm610 = 0x0031
        }

        [StructLayout(LayoutKind.Sequential)]
        private sealed class WaveFormatEx
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        #endregion
    }
}
