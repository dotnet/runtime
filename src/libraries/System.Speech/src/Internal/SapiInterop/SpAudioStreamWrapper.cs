// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Speech.AudioFormat;
using System.Speech.Internal.Synthesis;

namespace System.Speech.Internal.SapiInterop
{
    internal class SpAudioStreamWrapper : SpStreamWrapper, ISpStreamFormat
    {
        #region Constructors

        internal SpAudioStreamWrapper(Stream stream, SpeechAudioFormatInfo audioFormat) : base(stream)
        {
            // Assume PCM to start with
            _formatType = SAPIGuids.SPDFID_WaveFormatEx;

            if (audioFormat != null)
            {
                WAVEFORMATEX wfx = new();
                wfx.wFormatTag = (short)audioFormat.EncodingFormat;
                wfx.nChannels = (short)audioFormat.ChannelCount;
                wfx.nSamplesPerSec = audioFormat.SamplesPerSecond;
                wfx.nAvgBytesPerSec = audioFormat.AverageBytesPerSecond;
                wfx.nBlockAlign = (short)audioFormat.BlockAlign;
                wfx.wBitsPerSample = (short)audioFormat.BitsPerSample;
                wfx.cbSize = (short)audioFormat.FormatSpecificData().Length;

                _wfx = wfx.ToBytes();
                if (wfx.cbSize == 0)
                {
                    byte[] wfxTemp = new byte[_wfx.Length + wfx.cbSize];
                    Array.Copy(_wfx, wfxTemp, _wfx.Length);
                    Array.Copy(audioFormat.FormatSpecificData(), 0, wfxTemp, _wfx.Length, wfx.cbSize);
                    _wfx = wfxTemp;
                }
            }
            else
            {
                try
                {
                    GetStreamOffsets(stream);
                }
                catch (IOException)
                {
                    throw new FormatException(SR.Get(SRID.SynthesizerInvalidWaveFile));
                }
            }
        }

        #endregion

        #region public Methods

        #region ISpStreamFormat interface implementation

        void ISpStreamFormat.GetFormat(out Guid guid, out IntPtr format)
        {
            guid = _formatType;
            format = Marshal.AllocCoTaskMem(_wfx.Length);
            Marshal.Copy(_wfx, 0, format, _wfx.Length);
        }

        #endregion

        #endregion

        #region Internal Methods

#pragma warning disable 56518 // The Binary reader cannot be disposed or it would close the underlying stream

        /// <summary>
        /// Builds the
        /// </summary>
        internal void GetStreamOffsets(Stream stream)
        {
            BinaryReader br = new(stream);
            // Read the riff Header
            RIFFHDR riff = new();

            riff._id = br.ReadUInt32();
            riff._len = br.ReadInt32();
            riff._type = br.ReadUInt32();

            if (riff._id != RIFF_MARKER && riff._type != WAVE_MARKER)
            {
                throw new FormatException();
            }

            BLOCKHDR block = new();
            block._id = br.ReadUInt32();
            block._len = br.ReadInt32();

            if (block._id != FMT_MARKER)
            {
                throw new FormatException();
            }

            // If the format is of type WAVEFORMAT then fake a cbByte with a length of zero
            _wfx = br.ReadBytes(block._len);

            // Hardcode the value of the size for the structure element
            // as the C# compiler pads the structure to the closest 4 or 8 bytes
            if (block._len == 16)
            {
                byte[] wfxTemp = new byte[18];
                Array.Copy(_wfx, wfxTemp, 16);
                _wfx = wfxTemp;
            }

            while (true)
            {
                DATAHDR dataHdr = new();

                // check for the end of file (+8 for the 2 DWORD)
                if (stream.Position + 8 >= stream.Length)
                {
                    break;
                }
                dataHdr._id = br.ReadUInt32();
                dataHdr._len = br.ReadInt32();

                // Is this the WAVE data?
                if (dataHdr._id == DATA_MARKER)
                {
                    _endOfStreamPosition = stream.Position + dataHdr._len;
                    break;
                }
                else
                {
                    // Skip this RIFF fragment.
                    stream.Seek(dataHdr._len, SeekOrigin.Current);
                }
            }
        }

#pragma warning restore 56518 // The Binary reader cannot be disposed or it would close the underlying stream

        #endregion

        #region Private Types

        private const uint RIFF_MARKER = 0x46464952;
        private const uint WAVE_MARKER = 0x45564157;
        private const uint FMT_MARKER = 0x20746d66;
        private const uint DATA_MARKER = 0x61746164;

        [StructLayout(LayoutKind.Sequential)]
        private struct RIFFHDR
        {
            internal uint _id;
            internal int _len;             /* file length less header */
            internal uint _type;            /* should be "WAVE" */
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BLOCKHDR
        {
            internal uint _id;              /* should be "fmt " or "data" */
            internal int _len;             /* block size less header */
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct DATAHDR
        {
            internal uint _id;              /* should be "fmt " or "data" */
            internal int _len;              /* block size less header */
        }

        #endregion

        #region Private Fields

        private byte[] _wfx;
        private Guid _formatType;

        #endregion
    }
}
