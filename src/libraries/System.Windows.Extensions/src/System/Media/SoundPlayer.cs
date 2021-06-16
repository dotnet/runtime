// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace System.Media
{
    [ToolboxItem(false)]
    public class SoundPlayer : Component, ISerializable
    {
        private const int BlockSize = 1024;
        private const int DefaultLoadTimeout = 10000; // 10 secs

        private Uri _uri;
        private string _soundLocation = string.Empty;
        private int _loadTimeout = DefaultLoadTimeout;

        // used to lock all synchronous calls to the SoundPlayer object
        private readonly ManualResetEvent _semaphore = new ManualResetEvent(true);

        // the worker copyTask
        // we start the worker copyTask ONLY from entry points in the SoundPlayer API
        // we also set the tread to null only from the entry points in the SoundPlayer API
        private Task _copyTask;
        private CancellationTokenSource _copyTaskCancellation;

        // local buffer information
        private int _currentPos;
        private Stream _stream;
        private Exception _lastLoadException;
        private bool _doesLoadAppearSynchronous;
        private byte[] _streamData;
        private AsyncOperation _asyncOperation;
        private readonly SendOrPostCallback _loadAsyncOperationCompleted;

        // event
        private static readonly object s_eventLoadCompleted = new object();
        private static readonly object s_eventSoundLocationChanged = new object();
        private static readonly object s_eventStreamChanged = new object();

        public SoundPlayer()
        {
            _loadAsyncOperationCompleted = new SendOrPostCallback(LoadAsyncOperationCompleted);
        }

        public SoundPlayer(string soundLocation) : this()
        {
            SetupSoundLocation(soundLocation ?? string.Empty);
        }

        public SoundPlayer(Stream stream) : this()
        {
            _stream = stream;
        }

        protected SoundPlayer(SerializationInfo serializationInfo, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public int LoadTimeout
        {
            get => _loadTimeout;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(LoadTimeout), value, SR.SoundAPILoadTimeout);
                }

                _loadTimeout = value;
            }
        }

        public string SoundLocation
        {
            get => _soundLocation;
            set
            {
                if (value == null)
                {
                    value = string.Empty;
                }

                if (_soundLocation.Equals(value))
                {
                    return;
                }

                SetupSoundLocation(value);
                OnSoundLocationChanged(EventArgs.Empty);
            }
        }

        public Stream Stream
        {
            get
            {
                // if the path is set, we should return null
                // Path and Stream are mutually exclusive
                if (_uri != null)
                {
                    return null;
                }

                return _stream;
            }
            set
            {
                if (_stream == value)
                {
                    return;
                }

                SetupStream(value);
                OnStreamChanged(EventArgs.Empty);
            }
        }

        public bool IsLoadCompleted { get; private set; }

        public object Tag { get; set; }

        public void LoadAsync()
        {
            // if we have a file there is nothing to load - we just pass the file to the PlaySound function
            // if we have a stream, then we start loading the stream async
            if (_uri != null && _uri.IsFile)
            {
                Debug.Assert(_stream == null, "we can't have a stream and a path at the same time");
                IsLoadCompleted = true;

                FileInfo fi = new FileInfo(_uri.LocalPath);
                if (!fi.Exists)
                {
                    throw new FileNotFoundException(SR.SoundAPIFileDoesNotExist, _soundLocation);
                }

                OnLoadCompleted(new AsyncCompletedEventArgs(null, false, null));
                return;
            }

            // if we are actively loading, keep it running
            if (_copyTask != null && !_copyTask.IsCompleted)
            {
                return;
            }
            IsLoadCompleted = false;
            _streamData = null;
            _currentPos = 0;

            _asyncOperation = AsyncOperationManager.CreateOperation(null);

            LoadStream(false);
        }

        private void LoadAsyncOperationCompleted(object arg)
        {
            OnLoadCompleted((AsyncCompletedEventArgs)arg);
        }

        // called for loading a stream synchronously
        // called either when the user is setting the path/stream and we are loading
        // or when loading took more time than the time out
        private void CleanupStreamData()
        {
            _currentPos = 0;
            _streamData = null;
            IsLoadCompleted = false;
            _lastLoadException = null;
            _doesLoadAppearSynchronous = false;
            _copyTask = null;
            _semaphore.Set();
        }

        public void Load()
        {
            // if we have a file there is nothing to load - we just pass the file to the PlaySound function
            // if we have a stream, then we start loading the stream sync
            if (_uri != null && _uri.IsFile)
            {
                Debug.Assert(_stream == null, "we can't have a stream and a path at the same time");
                FileInfo fi = new FileInfo(_uri.LocalPath);
                if (!fi.Exists)
                {
                    throw new FileNotFoundException(SR.SoundAPIFileDoesNotExist, _soundLocation);
                }
                IsLoadCompleted = true;
                OnLoadCompleted(new AsyncCompletedEventArgs(null, false, null));
                return;
            }

            LoadSync();
        }

        private void LoadAndPlay(int flags)
        {
            // When the user does not specify a sound location nor a stream, play Beep
            if (string.IsNullOrEmpty(_soundLocation) && _stream == null)
            {
                SystemSounds.Beep.Play();
                return;
            }

            if (_uri != null && _uri.IsFile)
            {
                // Someone can call SoundPlayer::set_Location between the time
                // LoadAndPlay validates the sound file and the time it calls PlaySound.
                // The SoundPlayer will end up playing an un-validated sound file.
                // The solution is to store the uri.LocalPath on a local variable
                string localPath = _uri.LocalPath;

                // Play the path - don't use uri.AbsolutePath because that gives problems
                // when there are whitespaces in file names
                IsLoadCompleted = true;

                ValidateSoundFile(localPath);
                Interop.WinMM.PlaySound(localPath, IntPtr.Zero, Interop.WinMM.SND_NODEFAULT | flags);
            }
            else
            {
                LoadSync();
                ValidateSoundData(_streamData);
                Interop.WinMM.PlaySound(_streamData, IntPtr.Zero, Interop.WinMM.SND_MEMORY | Interop.WinMM.SND_NODEFAULT | flags);
            }
        }

        private void CancelLoad()
        {
            _copyTaskCancellation?.Cancel();
            _copyTaskCancellation = null;
        }

        private void LoadSync()
        {
            Debug.Assert((_uri == null || !_uri.IsFile), "we only load streams");

            // first make sure that any possible download ended
            if (!_semaphore.WaitOne(LoadTimeout, false))
            {
                CancelLoad();
                CleanupStreamData();
                throw new TimeoutException(SR.SoundAPILoadTimedOut);
            }

            // if we have data, then we are done
            if (_streamData != null)
            {
                return;
            }

            // setup the http stream
            if (_uri != null && !_uri.IsFile && _stream == null)
            {
#pragma warning disable SYSLIB0014 // WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.
                WebRequest webRequest = WebRequest.Create(_uri);
#pragma warning restore SYSLIB0014
                webRequest.Timeout = LoadTimeout;

                WebResponse webResponse;
                webResponse = webRequest.GetResponse();

                // now get the stream
                _stream = webResponse.GetResponseStream();
            }

            if (_stream.CanSeek)
            {
                // if we can get data synchronously, then get it
                LoadStream(true);
            }
            else
            {
                // the data can't be loaded synchronously
                // load it async, then wait for it to finish
                _doesLoadAppearSynchronous = true; // to avoid OnFailed call.
                LoadStream(false);

                if (!_semaphore.WaitOne(LoadTimeout, false))
                {
                    CancelLoad();
                    CleanupStreamData();
                    throw new TimeoutException(SR.SoundAPILoadTimedOut);
                }

                _doesLoadAppearSynchronous = false;

                if (_lastLoadException != null)
                {
                    throw _lastLoadException;
                }
            }

            // we don't need the worker copyThread anymore
            _copyTask = null;
        }

        private void LoadStream(bool loadSync)
        {
            if (loadSync && _stream.CanSeek)
            {
                int streamLen = (int)_stream.Length;
                _currentPos = 0;
                _streamData = new byte[streamLen];
                _stream.Read(_streamData, 0, streamLen);
                IsLoadCompleted = true;
                OnLoadCompleted(new AsyncCompletedEventArgs(null, false, null));
            }
            else
            {
                // lock any synchronous calls on the Sound object
                _semaphore.Reset();
                // start loading
                var cts = new CancellationTokenSource();
                _copyTaskCancellation = cts;
                _copyTask = CopyStreamAsync(cts.Token);
            }
        }

        public void Play()
        {
            LoadAndPlay(Interop.WinMM.SND_ASYNC);
        }

        public void PlaySync()
        {
            LoadAndPlay(Interop.WinMM.SND_SYNC);
        }

        public void PlayLooping()
        {
            LoadAndPlay(Interop.WinMM.SND_LOOP | Interop.WinMM.SND_ASYNC);
        }

        private static Uri ResolveUri(string partialUri)
        {
            Uri result = null;
            try
            {
                result = new Uri(partialUri);
            }
            catch (UriFormatException)
            {
                // eat URI parse exceptions
            }

            if (result == null)
            {
                // try relative to appbase
                try
                {
                    result = new Uri(Path.GetFullPath(partialUri));
                }
                catch (UriFormatException)
                {
                    // eat URI parse exceptions
                }
            }

            return result;
        }

        private void SetupSoundLocation(string soundLocation)
        {
            // if we are loading a file, stop it right now
            if (_copyTask != null)
            {
                CancelLoad();
                CleanupStreamData();
            }

            _uri = ResolveUri(soundLocation);
            _soundLocation = soundLocation;
            _stream = null;
            if (_uri == null)
            {
                if (!string.IsNullOrEmpty(soundLocation))
                {
                    throw new UriFormatException(SR.SoundAPIBadSoundLocation);
                }
            }
            else
            {
                if (!_uri.IsFile)
                {
                    // we are referencing a web resource ...
                    // we treat it as a stream...
                    _streamData = null;
                    _currentPos = 0;
                    IsLoadCompleted = false;
                }
            }
        }

        private void SetupStream(Stream stream)
        {
            if (_copyTask != null)
            {
                CancelLoad();
                CleanupStreamData();
            }

            _stream = stream;
            _soundLocation = string.Empty;
            _streamData = null;
            _currentPos = 0;
            IsLoadCompleted = false;
            if (stream != null)
            {
                _uri = null;
            }
        }

        public void Stop()
        {
            Interop.WinMM.PlaySound((byte[])null, IntPtr.Zero, Interop.WinMM.SND_PURGE);
        }

        public event AsyncCompletedEventHandler LoadCompleted
        {
            add
            {
                Events.AddHandler(s_eventLoadCompleted, value);
            }
            remove
            {
                Events.RemoveHandler(s_eventLoadCompleted, value);
            }
        }

        public event EventHandler SoundLocationChanged
        {
            add
            {
                Events.AddHandler(s_eventSoundLocationChanged, value);
            }
            remove
            {
                Events.RemoveHandler(s_eventSoundLocationChanged, value);
            }
        }

        public event EventHandler StreamChanged
        {
            add
            {
                Events.AddHandler(s_eventStreamChanged, value);
            }
            remove
            {
                Events.RemoveHandler(s_eventStreamChanged, value);
            }
        }

        protected virtual void OnLoadCompleted(AsyncCompletedEventArgs e)
        {
            ((AsyncCompletedEventHandler)Events[s_eventLoadCompleted])?.Invoke(this, e);
        }

        protected virtual void OnSoundLocationChanged(EventArgs e)
        {
            ((EventHandler)Events[s_eventSoundLocationChanged])?.Invoke(this, e);
        }

        protected virtual void OnStreamChanged(EventArgs e)
        {
            ((EventHandler)Events[s_eventStreamChanged])?.Invoke(this, e);
        }

        private async Task CopyStreamAsync(CancellationToken cancellationToken)
        {
            try
            {
                // setup the http stream
                if (_uri != null && !_uri.IsFile && _stream == null)
                {
#pragma warning disable SYSLIB0014 // WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.
                    WebRequest webRequest = WebRequest.Create(_uri);
#pragma warning restore SYSLIB0014
                    using (cancellationToken.Register(r => ((WebRequest)r).Abort(), webRequest))
                    {
                        WebResponse webResponse = await webRequest.GetResponseAsync().ConfigureAwait(false);
                        _stream = webResponse.GetResponseStream();
                    }
                }

                _streamData = new byte[BlockSize];

                int readBytes = await _stream.ReadAsync(_streamData.AsMemory(_currentPos, BlockSize), cancellationToken).ConfigureAwait(false);
                int totalBytes = readBytes;

                while (readBytes > 0)
                {
                    _currentPos += readBytes;
                    if (_streamData.Length < _currentPos + BlockSize)
                    {
                        byte[] newData = new byte[_streamData.Length * 2];
                        Array.Copy(_streamData, newData, _streamData.Length);
                        _streamData = newData;
                    }
                    readBytes = await _stream.ReadAsync(_streamData.AsMemory(_currentPos, BlockSize), cancellationToken).ConfigureAwait(false);
                    totalBytes += readBytes;
                }

                _lastLoadException = null;
            }
            catch (Exception exception)
            {
                _lastLoadException = exception;
            }

            IsLoadCompleted = true;
            _semaphore.Set();

            if (!_doesLoadAppearSynchronous)
            {
                // Post notification back to the UI thread.
                AsyncCompletedEventArgs ea = _lastLoadException is OperationCanceledException ?
                    new AsyncCompletedEventArgs(null, cancelled: true, null) :
                    new AsyncCompletedEventArgs(_lastLoadException, cancelled: false, null);
                _asyncOperation.PostOperationCompleted(_loadAsyncOperationCompleted, ea);
            }
        }

        private unsafe void ValidateSoundFile(string fileName)
        {
            IntPtr hMIO = Interop.WinMM.mmioOpen(fileName, IntPtr.Zero, Interop.WinMM.MMIO_READ | Interop.WinMM.MMIO_ALLOCBUF);
            if (hMIO == IntPtr.Zero)
            {
                throw new FileNotFoundException(SR.SoundAPIFileDoesNotExist, _soundLocation);
            }

            try
            {
                Interop.WinMM.WAVEFORMATEX waveFormat = null;
                var ckRIFF = new Interop.WinMM.MMCKINFO()
                {
                    fccType = mmioFOURCC('W', 'A', 'V', 'E')
                };
                var ck = new Interop.WinMM.MMCKINFO();
                if (Interop.WinMM.mmioDescend(hMIO, ckRIFF, null, Interop.WinMM.MMIO_FINDRIFF) != 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.SoundAPIInvalidWaveFile, _soundLocation));
                }

                while (Interop.WinMM.mmioDescend(hMIO, ck, ckRIFF, 0) == 0)
                {
                    if (ck.dwDataOffset + ck.cksize > ckRIFF.dwDataOffset + ckRIFF.cksize)
                    {
                        throw new InvalidOperationException(SR.SoundAPIInvalidWaveHeader);
                    }

                    if (ck.ckID == mmioFOURCC('f', 'm', 't', ' '))
                    {
                        if (waveFormat == null)
                        {
                            int dw = ck.cksize;
                            if (dw < Marshal.SizeOf(typeof(Interop.WinMM.WAVEFORMATEX)))
                            {
                                dw = Marshal.SizeOf(typeof(Interop.WinMM.WAVEFORMATEX));
                            }

                            waveFormat = new Interop.WinMM.WAVEFORMATEX();
                            var data = new byte[dw];
                            if (Interop.WinMM.mmioRead(hMIO, data, dw) != dw)
                            {
                                throw new InvalidOperationException(SR.Format(SR.SoundAPIReadError, _soundLocation));
                            }

                            fixed (byte* pdata = data)
                            {
                                Marshal.PtrToStructure((IntPtr)pdata, waveFormat);
                            }
                        }
                        else
                        {
                            // multiple formats?
                        }
                    }
                    Interop.WinMM.mmioAscend(hMIO, ck, 0);
                }

                if (waveFormat == null)
                {
                    throw new InvalidOperationException(SR.SoundAPIInvalidWaveHeader);
                }
                if (waveFormat.wFormatTag != Interop.WinMM.WAVE_FORMAT_PCM &&
                    waveFormat.wFormatTag != Interop.WinMM.WAVE_FORMAT_ADPCM &&
                    waveFormat.wFormatTag != Interop.WinMM.WAVE_FORMAT_IEEE_FLOAT)
                {
                    throw new InvalidOperationException(SR.SoundAPIFormatNotSupported);
                }

            }
            finally
            {
                if (hMIO != IntPtr.Zero)
                {
                    Interop.WinMM.mmioClose(hMIO, 0);
                }
            }
        }

        private static void ValidateSoundData(byte[] data)
        {
            int position = 0;
            short wFormatTag = -1;
            bool fmtChunkFound = false;

            // the RIFF header should be at least 12 bytes long.
            if (data.Length < 12)
            {
                throw new InvalidOperationException(SR.SoundAPIInvalidWaveHeader);
            }

            // validate the RIFF header
            if (data[0] != 'R' || data[1] != 'I' || data[2] != 'F' || data[3] != 'F')
            {
                throw new InvalidOperationException(SR.SoundAPIInvalidWaveHeader);
            }
            if (data[8] != 'W' || data[9] != 'A' || data[10] != 'V' || data[11] != 'E')
            {
                throw new InvalidOperationException(SR.SoundAPIInvalidWaveHeader);
            }

            // we only care about "fmt " chunk
            position = 12;
            int len = data.Length;
            while (!fmtChunkFound && position < len - 8)
            {
                if (data[position] == (byte)'f' && data[position + 1] == (byte)'m' && data[position + 2] == (byte)'t' && data[position + 3] == (byte)' ')
                {
                    // fmt chunk
                    fmtChunkFound = true;
                    int chunkSize = BytesToInt(data[position + 7], data[position + 6], data[position + 5], data[position + 4]);

                    // get the cbSize from the WAVEFORMATEX
                    int sizeOfWAVEFORMAT = 16;
                    if (chunkSize != sizeOfWAVEFORMAT)
                    {
                        // we are dealing w/ WAVEFORMATEX
                        // do extra validation
                        int sizeOfWAVEFORMATEX = 18;

                        // make sure the buffer is big enough to store a short
                        if (len < position + 8 + sizeOfWAVEFORMATEX - 1)
                        {
                            throw new InvalidOperationException(SR.SoundAPIInvalidWaveHeader);
                        }

                        short cbSize = BytesToInt16(data[position + 8 + sizeOfWAVEFORMATEX - 1],
                                                    data[position + 8 + sizeOfWAVEFORMATEX - 2]);
                        if (cbSize + sizeOfWAVEFORMATEX != chunkSize)
                        {
                            throw new InvalidOperationException(SR.SoundAPIInvalidWaveHeader);
                        }
                    }

                    // make sure the buffer passed in is big enough to store a short
                    if (len < position + 9)
                    {
                        throw new InvalidOperationException(SR.SoundAPIInvalidWaveHeader);
                    }
                    wFormatTag = BytesToInt16(data[position + 9], data[position + 8]);

                    position += chunkSize + 8;
                }
                else
                {
                    position += 8 + BytesToInt(data[position + 7], data[position + 6], data[position + 5], data[position + 4]);
                }
            }

            if (!fmtChunkFound)
            {
                throw new InvalidOperationException(SR.SoundAPIInvalidWaveHeader);
            }

            if (wFormatTag != Interop.WinMM.WAVE_FORMAT_PCM &&
                wFormatTag != Interop.WinMM.WAVE_FORMAT_ADPCM &&
                wFormatTag != Interop.WinMM.WAVE_FORMAT_IEEE_FLOAT)
            {
                throw new InvalidOperationException(SR.SoundAPIFormatNotSupported);
            }
        }

        private static short BytesToInt16(byte ch0, byte ch1)
        {
            int res;
            res = ch1;
            res |= ch0 << 8;
            return (short)res;
        }

        private static int BytesToInt(byte ch0, byte ch1, byte ch2, byte ch3)
        {
            return mmioFOURCC((char)ch3, (char)ch2, (char)ch1, (char)ch0);
        }

        private static int mmioFOURCC(char ch0, char ch1, char ch2, char ch3)
        {
            int result = 0;
            result |= ch0;
            result |= ch1 << 8;
            result |= ch2 << 16;
            result |= ch3 << 24;
            return result;
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
