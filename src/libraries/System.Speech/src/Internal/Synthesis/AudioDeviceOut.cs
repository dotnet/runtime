// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Speech.Internal.Synthesis
{
    /// <summary>
    /// Encapsulates Waveform Audio Interface playback functions and provides a simple
    /// interface for playing audio.
    /// </summary>
    internal class AudioDeviceOut : AudioBase, IDisposable
    {
        #region Constructors

        /// <summary>
        /// Create an instance of AudioDeviceOut.
        /// </summary>
        internal AudioDeviceOut(int curDevice, IAsyncDispatch asyncDispatch)
        {
            _delegate = new SafeNativeMethods.WaveOutProc(CallBackProc);
            _asyncDispatch = asyncDispatch;
            _curDevice = curDevice;
        }

        ~AudioDeviceOut()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_deviceOpen && _hwo != IntPtr.Zero)
            {
                SafeNativeMethods.waveOutClose(_hwo);
                _deviceOpen = false;
            }
            if (disposing)
            {
                ((IDisposable)_evt).Dispose();
            }
        }

        #endregion

        #region Internal Methods

        #region AudioDevice implementation

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

            // Get the alignments values
            WAVEFORMATEX.AvgBytesPerSec(wfx, out _nAvgBytesPerSec, out _blockAlign);

            MMSYSERR result;
            lock (_noWriteOutLock)
            {
                result = SafeNativeMethods.waveOutOpen(ref _hwo, _curDevice, wfx, _delegate, IntPtr.Zero, SafeNativeMethods.CALLBACK_FUNCTION);

                if (_fPaused && result == MMSYSERR.NOERROR)
                {
                    result = SafeNativeMethods.waveOutPause(_hwo);
                }
                // set the flags
                _aborted = false;
                _deviceOpen = true;
            }

            if (result != MMSYSERR.NOERROR)
            {
                throw new AudioException(result);
            }

            // Reset the counter for the number of bytes written so far
            _bytesWritten = 0;

            // Nothing in the queue
            _evt.Set();
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
            lock (_noWriteOutLock)
            {
                _deviceOpen = false;

                MMSYSERR result;

                CheckForAbort();

                if (_queueIn.Count != 0)
                {
                    SafeNativeMethods.waveOutReset(_hwo);
                }

                // Close it; no point in returning errors if this fails
                result = SafeNativeMethods.waveOutClose(_hwo);

                if (result != MMSYSERR.NOERROR)
                {
                    // This may create a dead lock
                    System.Diagnostics.Debug.Assert(false);
                }
            }
        }

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
                int bufferSize = buffer.Length;
                _bytesWritten += bufferSize;

                System.Diagnostics.Debug.Assert(bufferSize % _blockAlign == 0);

                WaveHeader waveHeader = new(buffer);
                GCHandle waveHdr = waveHeader.WAVEHDR;
                MMSYSERR result = SafeNativeMethods.waveOutPrepareHeader(_hwo, waveHdr.AddrOfPinnedObject(), waveHeader.SizeHDR);

                if (result != MMSYSERR.NOERROR)
                {
                    throw new AudioException(result);
                }

                lock (_noWriteOutLock)
                {
                    if (!_aborted)
                    {
                        lock (_queueIn)
                        {
                            InItem item = new(waveHeader);

                            _queueIn.Add(item);

                            // Something in the queue cannot exit anymore
                            _evt.Reset();
                        }

                        // Start playback of the first buffer
                        result = SafeNativeMethods.waveOutWrite(_hwo, waveHdr.AddrOfPinnedObject(), waveHeader.SizeHDR);
                        if (result != MMSYSERR.NOERROR)
                        {
                            lock (_queueIn)
                            {
                                _queueIn.RemoveAt(_queueIn.Count - 1);
                                throw new AudioException(result);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Pause the playback of a sound.
        /// </summary>
        internal override void Pause()
        {
            lock (_noWriteOutLock)
            {
                if (!_aborted && !_fPaused)
                {
                    if (_deviceOpen)
                    {
                        MMSYSERR result = SafeNativeMethods.waveOutPause(_hwo);
                        if (result != MMSYSERR.NOERROR)
                        {
                            System.Diagnostics.Debug.Assert(false, ((int)result).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                    }
                    _fPaused = true;
                }
            }
        }

        /// <summary>
        /// Resume the playback of a paused sound.
        /// </summary>
        internal override void Resume()
        {
            lock (_noWriteOutLock)
            {
                if (!_aborted && _fPaused)
                {
                    if (_deviceOpen)
                    {
                        MMSYSERR result = SafeNativeMethods.waveOutRestart(_hwo);
                        if (result != MMSYSERR.NOERROR)
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }
                    }
                }
            }
            _fPaused = false;
        }

        /// <summary>
        /// Wait for all the queued buffers to be played
        /// </summary>
        internal override void Abort()
        {
            lock (_noWriteOutLock)
            {
                _aborted = true;
                if (_queueIn.Count > 0)
                {
                    SafeNativeMethods.waveOutReset(_hwo);
                    _evt.WaitOne();
                }
            }
        }

        internal override void InjectEvent(TTSEvent ttsEvent)
        {
            if (_asyncDispatch != null && !_aborted)
            {
                lock (_queueIn)
                {
                    // Throw immediately if the queue is empty
                    if (_queueIn.Count == 0)
                    {
                        _asyncDispatch.Post(ttsEvent);
                    }
                    else
                    {
                        // Will be thrown before the next write to the audio device
                        _queueIn.Add(new InItem(ttsEvent));
                    }
                }
            }
        }

        /// <summary>
        /// Wait for all the queued buffers to be played
        /// </summary>
        internal override void WaitUntilDone()
        {
            if (!_deviceOpen)
            {
                System.Diagnostics.Debug.Assert(false);
                throw new InvalidOperationException();
            }

            _evt.WaitOne();
        }

        #endregion

        #region Audio device specific methods

        /// <summary>
        ///  Determine the number of available playback devices.
        /// </summary>
        /// <returns>Number of output devices</returns>
        internal static int NumDevices()
        {
            return SafeNativeMethods.waveOutGetNumDevs();
        }

        internal static int GetDevicedId(string name)
        {
            for (int iDevice = 0; iDevice < NumDevices(); iDevice++)
            {
                string device;
                if (GetDeviceName(iDevice, out device) == MMSYSERR.NOERROR && string.Equals(device, name, StringComparison.OrdinalIgnoreCase))
                {
                    return iDevice;
                }
            }
            return -1;
        }

        /// <summary>
        /// Get the name of the specified playback device.
        /// </summary>
        /// <param name="deviceId">ID of the device</param>
        /// <param name="prodName">Destination string assigned the name</param>
        /// <returns>MMSYSERR.NOERROR if successful</returns>
        internal static MMSYSERR GetDeviceName(int deviceId, [MarshalAs(UnmanagedType.LPWStr)] out string prodName)
        {
            prodName = string.Empty;
            SafeNativeMethods.WAVEOUTCAPS caps = new();

            MMSYSERR result = SafeNativeMethods.waveOutGetDevCaps((IntPtr)deviceId, ref caps, Marshal.SizeOf(caps));
            if (result != MMSYSERR.NOERROR)
            {
                return result;
            }

            prodName = caps.szPname;

            return MMSYSERR.NOERROR;
        }

        #endregion

        #endregion

        #region Internal Fields

        internal override TimeSpan Duration
        {
            get
            {
                if (_nAvgBytesPerSec == 0)
                {
                    return new TimeSpan(0);
                }
                return new TimeSpan((_bytesWritten * TimeSpan.TicksPerSecond) / _nAvgBytesPerSec);
            }
        }

        #endregion

        #region Private Methods

        private void CallBackProc(IntPtr hwo, MM_MSG uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
        {
            if (uMsg == MM_MSG.MM_WOM_DONE)
            {
                InItem inItem;
                lock (_queueIn)
                {
                    inItem = _queueIn[0];
                    inItem.ReleaseData();
                    _queueIn.RemoveAt(0);
                    _queueOut.Add(inItem);

                    // look for the next elements in the queue if they are events to throw!
                    while (_queueIn.Count > 0)
                    {
                        inItem = _queueIn[0];
                        // Do we have an event or a sound buffer
                        if (inItem._waveHeader == null)
                        {
                            if (_asyncDispatch != null && !_aborted)
                            {
                                _asyncDispatch.Post(inItem._userData);
                            }
                            _queueIn.RemoveAt(0);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // if the queue is empty, then restart the callers thread
                if (_queueIn.Count == 0)
                {
                    _evt.Set();
                }
            }
        }

        private void ClearBuffers()
        {
            foreach (InItem item in _queueOut)
            {
                WaveHeader waveHeader = item._waveHeader;
                MMSYSERR result;

                result = SafeNativeMethods.waveOutUnprepareHeader(
                            _hwo, waveHeader.WAVEHDR.AddrOfPinnedObject(), waveHeader.SizeHDR);
                if (result != MMSYSERR.NOERROR)
                {
                    //System.Diagnostics.Debug.Assert (false);
                }
                waveHeader.Dispose();
            }

            _queueOut.Clear();
        }

        private void CheckForAbort()
        {
            if (_aborted)
            {
                // Synchronous operation
                lock (_queueIn)
                {
                    foreach (InItem inItem in _queueIn)
                    {
                        // Do we have an event or a sound buffer
                        if (inItem._waveHeader != null)
                        {
                            WaveHeader waveHeader = inItem._waveHeader;
                            SafeNativeMethods.waveOutUnprepareHeader(
                                _hwo, waveHeader.WAVEHDR.AddrOfPinnedObject(), waveHeader.SizeHDR);
                            waveHeader.Dispose();
                        }
                        else
                        {
                            _asyncDispatch.Post(inItem._userData);
                        }
                    }
                    _queueIn.Clear();

                    // if the queue is empty, then restart the callers thread
                    _evt.Set();
                }
            }
            ClearBuffers();
        }

        #endregion

        #region Private Types

        /// <summary>
        /// This object must keep a reference to the waveHeader object
        /// so that the pinned buffer containing the data is not
        /// released before it is finished being played
        /// </summary>
        private sealed class InItem : IDisposable
        {
            internal InItem(WaveHeader waveHeader)
            {
                _waveHeader = waveHeader;
            }

            internal InItem(object userData)
            {
                _userData = userData;
            }
            public void Dispose()
            {
                if (_waveHeader != null)
                {
                    _waveHeader.Dispose();
                }

                GC.SuppressFinalize(this);
            }

            internal void ReleaseData()
            {
                if (_waveHeader != null)
                {
                    _waveHeader.ReleaseData();
                }
            }

            internal WaveHeader _waveHeader;
            internal object _userData;
        }

        #endregion

        #region Private Fields

        private List<InItem> _queueIn = new();

        private List<InItem> _queueOut = new();

        private int _blockAlign;
        private int _bytesWritten;
        private int _nAvgBytesPerSec;

        private IntPtr _hwo;

        private int _curDevice;

        private ManualResetEvent _evt = new(false);

        private SafeNativeMethods.WaveOutProc _delegate;

        private IAsyncDispatch _asyncDispatch;

        private bool _deviceOpen;
        private object _noWriteOutLock = new();
        private bool _fPaused;

        #endregion
    }
}
