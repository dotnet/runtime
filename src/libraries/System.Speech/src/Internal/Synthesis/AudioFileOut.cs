// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Speech.AudioFormat;
using System.Threading;
using System.Diagnostics;

namespace System.Speech.Internal.Synthesis
{
    /// <summary>
    /// Encapsulates Waveform Audio Interface playback functions and provides a simple
    /// interface for playing audio.
    /// </summary>
    internal class AudioFileOut : AudioBase, IDisposable
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// <summary>
        /// Create an instance of AudioFileOut.
        /// </summary>
        internal AudioFileOut (Stream stream, SpeechAudioFormatInfo formatInfo, bool headerInfo, IAsyncDispatch asyncDispatch)
        {
            _asyncDispatch = asyncDispatch;
            _stream = stream;
            _startStreamPosition = _stream.Position;
            _hasHeader = headerInfo;

            _wfxOut = new WAVEFORMATEX ();
            // if we have a formatInfo object, format conversion may be necessary
            if (formatInfo != null)
            {
                // Build the Wave format from the formatInfo
                _wfxOut.wFormatTag = (short) formatInfo.EncodingFormat;
                _wfxOut.wBitsPerSample = (short) formatInfo.BitsPerSample;
                _wfxOut.nSamplesPerSec = formatInfo.SamplesPerSecond;
                _wfxOut.nChannels = (short) formatInfo.ChannelCount;
            }
            else
            {
                // Set the default values 
                _wfxOut = WAVEFORMATEX.Default;
            }
            _wfxOut.nBlockAlign = (short) (_wfxOut.nChannels * _wfxOut.wBitsPerSample / 8);
            _wfxOut.nAvgBytesPerSec = _wfxOut.wBitsPerSample * _wfxOut.nSamplesPerSec * _wfxOut.nChannels / 8;
        }

        public void Dispose ()
        {
            _evt.Close ();
            GC.SuppressFinalize(this);
        }

        #endregion

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

        #region Internal Methods

        /// <summary>
        /// Begin to play
        /// </summary>
        /// <param name="wfx"></param>
        override internal void Begin (byte [] wfx)
        {
            if (_deviceOpen)
            {
                System.Diagnostics.Debug.Assert (false);
                throw new InvalidOperationException ();
            }

            // Get the audio format if conversion is needed
            _wfxIn = WAVEFORMATEX.ToWaveHeader (wfx);
            _doConversion = _pcmConverter.PrepareConverter (ref _wfxIn, ref _wfxOut);

            if (_totalByteWrittens == 0 && _hasHeader)
            {
                WriteWaveHeader (_stream, _wfxOut, _startStreamPosition, 0);
            }

            _bytesWritten = 0;

            // set the flags
            _aborted = false;
            _deviceOpen = true;
        }

        /// <summary>
        /// Begin to play
        /// </summary>
        override internal void End ()
        {
            if (!_deviceOpen)
            {
                System.Diagnostics.Debug.Assert (false);
                throw new InvalidOperationException ();
            }
            _deviceOpen = false;

            if (!_aborted)
            {
                if (_hasHeader)
                {
                    long position = _stream.Position;
                    WriteWaveHeader (_stream, _wfxOut, _startStreamPosition, _totalByteWrittens);
                    _stream.Seek (position, SeekOrigin.Begin);
                }
            }
        }

        #region AudioDevice implementation

        /// <summary>
        /// Play a wave file.
        /// </summary>
        /// <param name="buffer"></param>
        override internal void Play (byte [] buffer)
        {
            if (!_deviceOpen)
            {
                System.Diagnostics.Debug.Assert (false);
            }
            else
            {
                byte [] abOut = _doConversion ? _pcmConverter.ConvertSamples (buffer) : buffer;

                if (_paused)
                {
                    _evt.WaitOne ();
                    _evt.Reset ();
                }
                if (!_aborted)
                {
                    _stream.Write (abOut, 0, abOut.Length);
                    _totalByteWrittens += abOut.Length;
                    _bytesWritten += abOut.Length;
                }
            }
        }

        /// <summary>
        /// Pause the playback of a sound.
        /// </summary>
        /// <returns>MMSYSERR.NOERROR if successful</returns>
        override internal void Pause ()
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
        /// <returns>MMSYSERR.NOERROR if successful</returns>
        override internal void Resume ()
        {
            if (!_aborted && _paused)
            {
                lock (_noWriteOutLock)
                {
                    _paused = false;
                    _evt.Set ();
                }
            }
        }

        /// <summary>
        /// Wait for all the queued buffers to be played
        /// </summary>
        override internal void Abort ()
        {
            lock (_noWriteOutLock)
            {
                _aborted = true;
                _paused = false;
                _evt.Set ();
            }
        }


        override internal void InjectEvent (TTSEvent ttsEvent)
        {
            if (!_aborted && _asyncDispatch != null)
            {
                _asyncDispatch.Post (ttsEvent);
            }
        }

        /// <summary>
        /// File operation are basically synchonous
        /// </summary>
        override internal void WaitUntilDone ()
        {
            lock (_noWriteOutLock)
            {
            }
        }

        #endregion

        #endregion

        //*******************************************************************
        //
        // Internal Fields
        //
        //*******************************************************************

        #region Internal Fields

        override internal TimeSpan Duration
        {
            get
            {
                if (_wfxIn.nAvgBytesPerSec == 0)
                {
                    return new TimeSpan (0);
                }
                return new TimeSpan ((_bytesWritten * TimeSpan.TicksPerSecond) / _wfxIn.nAvgBytesPerSec);
            }
        }

        override internal long Position
        {
            get
            {
                return _stream.Position;
            }
        }

        internal override byte [] WaveFormat
        {
            get
            {
                return _wfxOut.ToBytes ();
            }
        }

        #endregion

        //*******************************************************************
        //
        // Protected Fields
        //
        //*******************************************************************

        #region Private Fields

        protected ManualResetEvent _evt = new ManualResetEvent (false);
        protected bool _deviceOpen;

        protected Stream _stream;

        protected PcmConverter _pcmConverter = new PcmConverter ();
        protected bool _doConversion;

        protected bool _paused;
        protected int _totalByteWrittens;
        protected int _bytesWritten;

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private IAsyncDispatch _asyncDispatch;
        private object _noWriteOutLock = new object ();

        private WAVEFORMATEX _wfxIn;
        private WAVEFORMATEX _wfxOut;
        private bool _hasHeader;

        private long _startStreamPosition;

        #endregion
    }
}
