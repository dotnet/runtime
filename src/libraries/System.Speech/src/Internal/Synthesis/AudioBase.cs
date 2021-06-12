// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;

namespace System.Speech.Internal.Synthesis
{
    /// <summary>
    /// Encapsulates Waveform Audio Interface playback functions and provides a simple
    /// interface for playing audio.
    /// </summary>
    internal abstract class AudioBase
    {
        #region Constructors

        /// <summary>
        /// Create an instance of AudioBase.
        /// </summary>
        internal AudioBase()
        {
        }

        #endregion

        #region Internal Methods

        #region abstract Members

        /// <summary>
        /// Play a wave file.
        /// </summary>
        internal abstract void Begin(byte[] wfx);

        /// <summary>
        /// Play a wave file.
        /// </summary>
        internal abstract void End();

        /// <summary>
        /// Play a wave file.
        /// </summary>
        internal virtual void Play(IntPtr pBuff, int cb)
        {
            byte[] buffer = new byte[cb];
            Marshal.Copy(pBuff, buffer, 0, cb);
            Play(buffer);
        }

        /// <summary>
        /// Play a wave file.
        /// </summary>
        internal virtual void Play(byte[] buffer)
        {
            GCHandle gc = GCHandle.Alloc(buffer);
            Play(gc.AddrOfPinnedObject(), buffer.Length);
            gc.Free();
        }

        /// <summary>
        /// Pause the playback of a sound.
        /// </summary>
        internal abstract void Pause();

        /// <summary>
        /// Resume the playback of a paused sound.
        /// </summary>
        internal abstract void Resume();

        /// <summary>
        /// Throw an event synchronized with the audio stream
        /// </summary>
        internal abstract void InjectEvent(TTSEvent ttsEvent);

        /// <summary>
        /// File operation are synchronous no wait
        /// </summary>
        internal abstract void WaitUntilDone();

        /// <summary>
        /// Wait for all the queued buffers to be played
        /// </summary>
        internal abstract void Abort();

        #endregion

        #region helpers

        internal void PlayWaveFile(AudioData audio)
        {
            // allocate some memory for the largest header
            try
            {
                // Fake a header for ALaw and ULaw
                if (!string.IsNullOrEmpty(audio._mimeType))
                {
                    WAVEFORMATEX wfx = new();

                    wfx.nChannels = 1;
                    wfx.nSamplesPerSec = 8000;
                    wfx.nAvgBytesPerSec = 8000;
                    wfx.nBlockAlign = 1;
                    wfx.wBitsPerSample = 8;
                    wfx.cbSize = 0;

                    switch (audio._mimeType)
                    {
                        case "audio/basic":
                            wfx.wFormatTag = (short)AudioFormat.EncodingFormat.ULaw;
                            break;

                        case "audio/x-alaw-basic":
                            wfx.wFormatTag = (short)AudioFormat.EncodingFormat.ALaw;
                            break;

                        default:
                            throw new FormatException(SR.Get(SRID.UnknownMimeFormat));
                    }

                    Begin(wfx.ToBytes());
                    try
                    {
                        byte[] data = new byte[(int)audio._stream.Length];
                        audio._stream.Read(data, 0, data.Length);
                        Play(data);
                    }
                    finally
                    {
                        WaitUntilDone();
                        End();
                    }
                }
                else
                {
                    BinaryReader br = new(audio._stream);

                    try
                    {
                        byte[] wfx = GetWaveFormat(br);

                        if (wfx == null)
                        {
                            throw new FormatException(SR.Get(SRID.NotValidAudioFile, audio._uri.ToString()));
                        }

                        Begin(wfx);

                        try
                        {
                            while (true)
                            {
                                DATAHDR dataHdr = new();

                                // check for the end of file (+8 for the 2 DWORD)
                                if (audio._stream.Position + 8 >= audio._stream.Length)
                                {
                                    break;
                                }
                                dataHdr._id = br.ReadUInt32();
                                dataHdr._len = br.ReadInt32();

                                // Is this the WAVE data?
                                if (dataHdr._id == DATA_MARKER)
                                {
                                    byte[] ab = Helpers.ReadStreamToByteArray(audio._stream, dataHdr._len);
                                    Play(ab);
                                }
                                else
                                {
                                    // Skip this RIFF fragment.
                                    audio._stream.Seek(dataHdr._len, SeekOrigin.Current);
                                }
                            }
                        }
                        finally
                        {
                            WaitUntilDone();
                            End();
                        }
                    }
                    finally
                    {
                        ((IDisposable)br).Dispose();
                    }
                }
            }
            finally
            {
                audio.Dispose();
            }
        }

        internal static byte[] GetWaveFormat(BinaryReader br)
        {
            // Read the riff Header
            RIFFHDR riff = new();

            riff._id = br.ReadUInt32();
            riff._len = br.ReadInt32();
            riff._type = br.ReadUInt32();

            if (riff._id != RIFF_MARKER && riff._type != WAVE_MARKER)
            {
                return null;
            }

            BLOCKHDR block = new();
            block._id = br.ReadUInt32();
            block._len = br.ReadInt32();

            if (block._id != FMT_MARKER)
            {
                return null;
            }

            // If the format is of type WAVEFORMAT then fake a cbByte with a length of zero
            byte[] wfx;
            wfx = br.ReadBytes(block._len);

            // Hardcode the value of the size for the structure element
            // as the C# compiler pads the structure to the closest 4 or 8 bytes
            if (block._len == 16)
            {
                byte[] wfxTemp = new byte[18];
                Array.Copy(wfx, wfxTemp, 16);
                wfx = wfxTemp;
            }
            return wfx;
        }

        internal static void WriteWaveHeader(Stream stream, WAVEFORMATEX waveEx, long position, int cData)
        {
            RIFFHDR riff = new(0);
            BLOCKHDR block = new(0);
            DATAHDR dataHdr = new(0);

            int cRiff = Marshal.SizeOf(riff);
            int cBlock = Marshal.SizeOf(block);
            int cWaveEx = waveEx.Length;// Marshal.SizeOf (waveEx); // The CLR automatically pad the waveEx structure to dword boundary. Force 16.
            int cDataHdr = Marshal.SizeOf(dataHdr);

            int total = cRiff + cBlock + cWaveEx + cDataHdr;

            using (MemoryStream memStream = new())
            {
                BinaryWriter bw = new(memStream);
                try
                {
                    // Write the RIFF section
                    riff._len = total + cData - 8/* - cRiff*/; // for the "WAVE" 4 characters
                    bw.Write(riff._id);
                    bw.Write(riff._len);
                    bw.Write(riff._type);

                    // Write the wave header section
                    block._len = cWaveEx;
                    bw.Write(block._id);
                    bw.Write(block._len);

                    // Write the FormatEx structure
                    bw.Write(waveEx.ToBytes());
                    //bw.Write (waveEx.cbSize);

                    // Write the data section
                    dataHdr._len = cData;
                    bw.Write(dataHdr._id);
                    bw.Write(dataHdr._len);

                    stream.Seek(position, SeekOrigin.Begin);
                    stream.Write(memStream.GetBuffer(), 0, (int)memStream.Length);
                }
                finally
                {
                    ((IDisposable)bw).Dispose();
                }
            }
        }

        #endregion

        #endregion

        #region Internal Property

        internal abstract TimeSpan Duration { get; }

        internal virtual long Position { get { return 0; } }

        internal virtual bool IsAborted
        {
            get
            {
                return _aborted;
            }
            set
            {
                _aborted = false;
            }
        }

        internal virtual byte[] WaveFormat { get { return null; } }

        #endregion

        #region Protected Property

        protected bool _aborted;

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

            internal RIFFHDR(int length)
            {
                _id = RIFF_MARKER;
                _type = WAVE_MARKER;
                _len = length;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BLOCKHDR
        {
            internal uint _id;              /* should be "fmt " or "data" */
            internal int _len;             /* block size less header */

            internal BLOCKHDR(int length)
            {
                _id = FMT_MARKER;
                _len = length;
            }
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct DATAHDR
        {
            internal uint _id;              /* should be "fmt " or "data" */
            internal int _len;              /* block size less header */

            internal DATAHDR(int length)
            {
                _id = DATA_MARKER;
                _len = length;
            }
        }

        #endregion
    }

    #region Internal Methods

    [System.Runtime.InteropServices.TypeLibTypeAttribute(16)]
    internal struct WAVEFORMATEX
    {

        internal short wFormatTag;
        internal short nChannels;
        internal int nSamplesPerSec;
        internal int nAvgBytesPerSec;
        internal short nBlockAlign;
        internal short wBitsPerSample;
        internal short cbSize;

        internal static WAVEFORMATEX ToWaveHeader(byte[] waveHeader)
        {
            GCHandle gc = GCHandle.Alloc(waveHeader, GCHandleType.Pinned);
            IntPtr ptr = gc.AddrOfPinnedObject();
            WAVEFORMATEX wfx = new();
            wfx.wFormatTag = Marshal.ReadInt16(ptr);
            wfx.nChannels = Marshal.ReadInt16(ptr, 2);
            wfx.nSamplesPerSec = Marshal.ReadInt32(ptr, 4);
            wfx.nAvgBytesPerSec = Marshal.ReadInt32(ptr, 8);
            wfx.nBlockAlign = Marshal.ReadInt16(ptr, 12);
            wfx.wBitsPerSample = Marshal.ReadInt16(ptr, 14);
            wfx.cbSize = Marshal.ReadInt16(ptr, 16);

            if (wfx.cbSize != 0)
            {
                throw new InvalidOperationException();
            }
            gc.Free();
            return wfx;
        }

        internal static void AvgBytesPerSec(byte[] waveHeader, out int avgBytesPerSec, out int nBlockAlign)
        {
            // Hardcode the value of the size for the structure element
            // as the C# compiler pads the structure to the closest 4 or 8 bytes
            GCHandle gc = GCHandle.Alloc(waveHeader, GCHandleType.Pinned);
            IntPtr ptr = gc.AddrOfPinnedObject();
            avgBytesPerSec = Marshal.ReadInt32(ptr, 8);
            nBlockAlign = Marshal.ReadInt16(ptr, 12);
            gc.Free();
        }

        internal byte[] ToBytes()
        {
            System.Diagnostics.Debug.Assert(cbSize == 0);
            GCHandle gc = GCHandle.Alloc(this, GCHandleType.Pinned);
            byte[] ab = ToBytes(gc.AddrOfPinnedObject());
            gc.Free();
            return ab;
        }

        internal static byte[] ToBytes(IntPtr waveHeader)
        {
            // Hardcode the value of the size for the structure element
            // as the C# compiler pads the structure to the closest 4 or 8 bytes

            int cbSize = Marshal.ReadInt16(waveHeader, 16);
            byte[] ab = new byte[18 + cbSize];
            Marshal.Copy(waveHeader, ab, 0, 18 + cbSize);
            return ab;
        }

        internal static WAVEFORMATEX Default
        {
            get
            {
                WAVEFORMATEX wfx = new();
                wfx.wFormatTag = 1;
                wfx.nChannels = 1;
                wfx.nSamplesPerSec = 22050;
                wfx.nAvgBytesPerSec = 44100;
                wfx.nBlockAlign = 2;
                wfx.wBitsPerSample = 16;
                wfx.cbSize = 0;
                return wfx;
            }
        }

        internal int Length
        {
            get
            {
                return 18 + cbSize;
            }
        }
    }

    #endregion
}
