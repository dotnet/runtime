// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Speech.AudioFormat;
using System.Speech.Internal.SapiInterop;
using System.Speech.Internal.Synthesis;

#pragma warning disable 1634, 1691 // Allows suppression of certain PreSharp messages.

using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace System.Speech.Internal.SapiInterop
{
    internal class SpAudioStreamWrapper : SpStreamWrapper, ISpStreamFormat
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        internal SpAudioStreamWrapper(Stream stream, SpeechAudioFormatInfo audioFormat) : base(stream)
        {
            // Assume PCM to start with
            _formatType = SAPIGuids.SPDFID_WaveFormatEx;

            if (audioFormat != null)
            {
                WAVEFORMATEX wfx = new WAVEFORMATEX();
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

        //*******************************************************************
        //
        // Public Methods
        //
        //*******************************************************************

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

        //*******************************************************************
        //
        // Internal Properties
        //
        //*******************************************************************

        #region Internal Methods

#pragma warning disable 56518 // The Binary reader cannot be disposed or it would close the underlying stream

        /// <summary>
        /// Builds the
        /// </summary>
        /// <param name="stream"></param>
        internal void GetStreamOffsets(Stream stream)
        {
            BinaryReader br = new BinaryReader(stream);
            // Read the riff Header
            RIFFHDR riff = new RIFFHDR();

            riff._id = br.ReadUInt32();
            riff._len = br.ReadInt32();
            riff._type = br.ReadUInt32();

            if (riff._id != RIFF_MARKER && riff._type != WAVE_MARKER)
            {
                throw new FormatException();
            }

            BLOCKHDR block = new BLOCKHDR();
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
                DATAHDR dataHdr = new DATAHDR();

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

        //*******************************************************************
        //
        // Private Types
        //
        //*******************************************************************

        #region Private Types

        private const UInt32 RIFF_MARKER = 0x46464952;
        private const UInt32 WAVE_MARKER = 0x45564157;
        private const UInt32 FMT_MARKER = 0x20746d66;
        private const UInt32 DATA_MARKER = 0x61746164;

        [StructLayout(LayoutKind.Sequential)]
        private struct RIFFHDR
        {
            internal UInt32 _id;
            internal Int32 _len;             /* file length less header */
            internal UInt32 _type;            /* should be "WAVE" */
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BLOCKHDR
        {
            internal UInt32 _id;              /* should be "fmt " or "data" */
            internal Int32 _len;             /* block size less header */
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct DATAHDR
        {
            internal UInt32 _id;              /* should be "fmt " or "data" */
            internal Int32 _len;              /* block size less header */
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private byte[] _wfx;
        private Guid _formatType;

        #endregion
    }
}
