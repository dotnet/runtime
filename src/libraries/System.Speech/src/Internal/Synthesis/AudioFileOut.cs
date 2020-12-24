// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Speech.AudioFormat;
using System.Threading;

namespace System.Speech.Internal.Synthesis
{
    /// <summary>
    /// Encapsulates Waveform Audio Interface playback functions and provides a simple
    /// interface for playing audio.
    /// </summary>
    internal class AudioFileOut : AudioBase, IDisposable
    {
        #region Constructors

        /// <summary>
        /// Create an instance of AudioFileOut.
        /// </summary>
        internal AudioFileOut(Stream stream, SpeechAudioFormatInfo formatInfo, bool headerInfo, IAsyncDispatch asyncDispatch)
        {
            _asyncDispatch = asyncDispatch;
            _stream = stream;
            _startStreamPosition = _stream.Position;
            _hasHeader = headerInfo;

            _wfxOut = new WAVEFORMATEX();
            // if we have a formatInfo object, format conversion may be necessary
            if (formatInfo != null)
            {
                // Build the Wave format from the formatInfo
                _wfxOut.wFormatTag = (short)formatInfo.EncodingFormat;
                _wfxOut.wBitsPerSample = (short)formatInfo.BitsPerSample;
                _wfxOut.nSamplesPerSec = formatInfo.SamplesPerSecond;
                _wfxOut.nChannels = (short)formatInfo.ChannelCount;
            }
            else
            {
                // Set the default values
                _wfxOut = WAVEFORMATEX.Default;
            }
            _wfxOut.nBlockAlign = (short)(_wfxOut.nChannels * _wfxOut.wBitsPerSample / 8);
            _wfxOut.nAvgBytesPerSec = _wfxOut.wBitsPerSample * _wfxOut.nSamplesPerSec * _wfxOut.nChannels / 8;
        }

        public void Dispose()
        {
            _evt.Close();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Begin to play
        /// </summary>
        internal override void Begin(byte[] wfx)
        {
            if (_deviceOpen)
            {
                System.Diagnostics.Debug.Assert(false);
                throw new InvalidOperationException();
            }

            // Get the audio format if conversion is needed
            _wfxIn = WAVEFORMATEX.ToWaveHeader(wfx);
            _doConversion = _pcmConverter.PrepareConverter(ref _wfxIn, ref _wfxOut);

            if (_totalByteWrittens == 0 && _hasHeader)
            {
                WriteWaveHeader(_stream, _wfxOut, _startStreamPosition, 0);
            }

            _bytesWritten = 0;

            // set the flags
            _aborted = false;
            _deviceOpen = true;
        }

        /// <summary>
        /// Begin to play
        /// </summary>
        internal override void End()
        {
            if (!_deviceOpen)
            {
                System.Diagnostics.Debug.Assert(false);
                throw new InvalidOperationException();
            }
            _deviceOpen = false;

            if (!_aborted)
            {
                if (_hasHeader)
                {
                    long position = _stream.Position;
                    WriteWaveHeader(_stream, _wfxOut, _startStreamPosition, _totalByteWrittens);
                    _stream.Seek(position, SeekOrigin.Begin);
                }
            }
        }

        #region AudioDevice implementation

        /// <summary>
        /// Play a wave file.
        /// </summary>
        internal override void Play(byte[] buffer)
        {
            if (!_deviceOpen)
            {
                System.Diagnostics.Debug.Assert(false);
            }
            else
            {
                byte[] abOut = _doConversion ? _pcmConverter.ConvertSamples(buffer) : buffer;

                if (_paused)
                {
                    _evt.WaitOne();
                    _evt.Reset();
                }
                if (!_aborted)
                {
                    _stream.Write(abOut, 0, abOut.Length);
                    _totalByteWrittens += abOut.Length;
                    _bytesWritten += abOut.Length;
                }
            }
        }

        /// <summary>
        /// Pause the playback of a sound.
        /// </summary>
        internal override void Pause()
        {
            if (!_aborted && !_paused)
            {
                lock (_noWriteOutLock)
                {
                    _paused = true;
                }
            }
        }

        /// <summary>
        /// Resume the playback of a paused sound.
        /// </summary>
        internal override void Resume()
        {
            if (!_aborted && _paused)
            {
                lock (_noWriteOutLock)
                {
                    _paused = false;
                    _evt.Set();
                }
            }
        }

        /// <summary>
        /// Wait for all the queued buffers to be played
        /// </summary>
        internal override void Abort()
        {
            lock (_noWriteOutLock)
            {
                _aborted = true;
                _paused = false;
                _evt.Set();
            }
        }

        internal override void InjectEvent(TTSEvent ttsEvent)
        {
            if (!_aborted && _asyncDispatch != null)
            {
                _asyncDispatch.Post(ttsEvent);
            }
        }

        /// <summary>
        /// File operation are basically synchronous
        /// </summary>
        internal override void WaitUntilDone()
        {
            lock (_noWriteOutLock)
            {
            }
        }

        #endregion

        #endregion

        #region Internal Fields

        internal override TimeSpan Duration
        {
            get
            {
                if (_wfxIn.nAvgBytesPerSec == 0)
                {
                    return new TimeSpan(0);
                }
                return new TimeSpan((_bytesWritten * TimeSpan.TicksPerSecond) / _wfxIn.nAvgBytesPerSec);
            }
        }

        internal override long Position
        {
            get
            {
                return _stream.Position;
            }
        }

        internal override byte[] WaveFormat
        {
            get
            {
                return _wfxOut.ToBytes();
            }
        }

        #endregion

        #region Private Fields

        protected ManualResetEvent _evt = new(false);
        protected bool _deviceOpen;

        protected Stream _stream;

        protected PcmConverter _pcmConverter = new();
        protected bool _doConversion;

        protected bool _paused;
        protected int _totalByteWrittens;
        protected int _bytesWritten;

        #endregion

        #region Private Fields

        private IAsyncDispatch _asyncDispatch;
        private object _noWriteOutLock = new();

        private WAVEFORMATEX _wfxIn;
        private WAVEFORMATEX _wfxOut;
        private bool _hasHeader;

        private long _startStreamPosition;

        #endregion
    }
}
