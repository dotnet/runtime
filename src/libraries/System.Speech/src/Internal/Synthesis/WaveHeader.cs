// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Speech.Internal.Synthesis
{

    internal sealed class WaveHeader : IDisposable
    {
        #region Constructors

        /// <summary>
        /// Initialize an instance of a byte array.
        /// </summary>
        /// <returns>MMSYSERR.NOERROR if successful</returns>
        internal WaveHeader(byte[] buffer)
        {
            _dwBufferLength = buffer.Length;
            _gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        }

        /// <summary>
        /// Frees any memory allocated for the buffer.
        /// </summary>
        ~WaveHeader()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees any memory allocated for the buffer.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseData();
                if (_gcHandleWaveHdr.IsAllocated)
                {
                    _gcHandleWaveHdr.Free();
                }
            }
        }

        #endregion

        #region Internal Methods

        internal void ReleaseData()
        {
            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }
        }

        #endregion

        #region Internal Properties
        internal GCHandle WAVEHDR
        {
            get
            {
                if (!_gcHandleWaveHdr.IsAllocated)
                {
                    _waveHdr.lpData = _gcHandle.AddrOfPinnedObject();
                    _waveHdr.dwBufferLength = (uint)_dwBufferLength;
                    _waveHdr.dwBytesRecorded = 0;
                    _waveHdr.dwUser = 0;
                    _waveHdr.dwFlags = 0;
                    _waveHdr.dwLoops = 0;
                    _waveHdr.lpNext = IntPtr.Zero;
                    _gcHandleWaveHdr = GCHandle.Alloc(_waveHdr, GCHandleType.Pinned);
                }
                return _gcHandleWaveHdr;
            }
        }

        internal int SizeHDR
        {
            get
            {
                return Marshal.SizeOf<Interop.WinMM.WAVEHDR>();
            }
        }

        #endregion

        #region Internal Fields

        /// <summary>
        /// Used by dwFlags in WaveHeader
        /// Set by the device driver to indicate that it is finished with the buffer
        /// and is returning it to the application.
        /// </summary>
        internal const int WHDR_DONE = 0x00000001;
        /// <summary>
        /// Used by dwFlags in WaveHeader
        /// Set by Windows to indicate that the buffer has been prepared with the
        /// waveInPrepareHeader or waveOutPrepareHeader function.
        /// </summary>
        internal const int WHDR_PREPARED = 0x00000002;
        /// <summary>
        /// Used by dwFlags in WaveHeader
        /// This buffer is the first buffer in a loop. This flag is used only with
        /// output buffers.
        /// </summary>
        internal const int WHDR_BEGINLOOP = 0x00000004;
        /// <summary>
        /// Used by dwFlags in WaveHeader
        /// This buffer is the last buffer in a loop. This flag is used only with
        /// output buffers.
        /// </summary>
        internal const int WHDR_ENDLOOP = 0x00000008;
        /// <summary>
        /// Used by dwFlags in WaveHeader
        /// Set by Windows to indicate that the buffer is queued for playback.
        /// </summary>
        internal const int WHDR_INQUEUE = 0x00000010;

        /// <summary>
        /// Set in WaveFormat.wFormatTag to specify PCM data.
        /// </summary>
        internal const int WAVE_FORMAT_PCM = 1;

        #endregion

        #region private Fields

        /// <summary>
        /// Long pointer to the address of the waveform buffer. This buffer must
        /// be block-aligned according to the nBlockAlign member of the
        /// WaveFormat structure used to open the device.
        /// </summary>
        private GCHandle _gcHandle;

        private GCHandle _gcHandleWaveHdr;

        private Interop.WinMM.WAVEHDR _waveHdr;

        /// <summary>
        /// Specifies the length, in bytes, of the buffer.
        /// </summary>
        internal int _dwBufferLength;

        #endregion
    }
}
