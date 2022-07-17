// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Speech.AudioFormat;
using System.Speech.Internal.ObjectTokens;
using System.Speech.Synthesis;
using System.Speech.Synthesis.TtsEngine;
using System.Text;
using System.Threading;

#pragma warning disable 56502       // Empty catch statements

namespace System.Speech.Internal.Synthesis
{
    internal sealed class VoiceSynthesis : IDisposable
    {
        #region Constructors

        internal VoiceSynthesis(WeakReference speechSynthesizer)
        {
            _asyncWorker = new AsyncSerializedWorker(new WaitCallback(ProcessPostData), null);
            _asyncWorkerUI = new AsyncSerializedWorker(null, SynchronizationContext.Current);

            // Setup the event dispatcher for state changed events
            _eventStateChanged = new WaitCallback(OnStateChanged);

            // Setup the event dispatcher for all other events
            _signalWorkerCallback = new WaitCallback(SignalWorkerThread);

            //
            _speechSyntesizer = speechSynthesizer;

            // Initialize the engine site;
            _resourceLoader = new ResourceLoader();
            _site = new EngineSite(_resourceLoader);

            // No pending work and speaking is done
            _evtPendingSpeak.Reset();

            // Create the default audio device (speaker)
            _waveOut = new AudioDeviceOut(SAPICategories.DefaultDeviceOut(), _asyncWorker);

            // Build the installed voice collection on first run
            if (s_allVoices == null)
            {
                s_allVoices = BuildInstalledVoices(this);

                // If no voice are installed, then bail out.
                if (s_allVoices.Count == 0)
                {
                    s_allVoices = null;
                    throw new PlatformNotSupportedException(SR.Get(SRID.SynthesizerVoiceFailed));
                }
            }

            // Create a dynamic list of installed voices from the list of all available voices.
            _installedVoices = new List<InstalledVoice>(s_allVoices.Count);
            foreach (InstalledVoice installedVoice in s_allVoices)
            {
                _installedVoices.Add(new InstalledVoice(this, installedVoice.VoiceInfo));
            }

            // Get the default rate
            _site.VoiceRate = _defaultRate = (int)GetDefaultRate();

            // Start the worker thread
            _workerThread = new Thread(new ThreadStart(ThreadProc))
            {
                IsBackground = true
            };
            _workerThread.Start();

            // Default TTS engines events to be notified
            SetInterest(_ttsEvents);
        }

        ~VoiceSynthesis()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Internal Methods

        #region SpeechSynthesis 'public' API implementation
        internal void Speak(Prompt prompt)
        {
            bool done = false;
            EventHandler<StateChangedEventArgs> eventHandler = delegate (object sender, StateChangedEventArgs args)
            {
                if (prompt.IsCompleted && args.State == SynthesizerState.Ready)
                {
                    done = true;
                    _workerWaitHandle.Set();
                }
            };

            try
            {
                _stateChanged += eventHandler;
                _asyncWorkerUI.AsyncMode = false;
                _asyncWorkerUI.WorkItemPending += _signalWorkerCallback;

                // SpeakAsync the prompt
                QueuePrompt(prompt);

                while (!done && !_isDisposed)
                {
                    _workerWaitHandle.WaitOne();
                    _asyncWorkerUI.ConsumeQueue();
                }

                // Throw if an exception occurred
                if (prompt.Exception != null)
                {
                    ExceptionDispatchInfo.Throw(prompt.Exception);
                }
            }
            finally
            {
                _asyncWorkerUI.AsyncMode = true;
                _asyncWorkerUI.WorkItemPending -= _signalWorkerCallback;
                _stateChanged -= eventHandler;
            }
        }
        internal void SpeakAsync(Prompt prompt)
        {
            QueuePrompt(prompt);
        }

        #region Speech Synthesis events

        internal void OnSpeakStarted(SpeakStartedEventArgs e)
        {
            if (_speakStarted != null)
            {
                _asyncWorkerUI.PostOperation(_speakStarted, _speechSyntesizer.Target, e);
            }
        }

        internal void FireSpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            if (_speakCompleted != null && !e.Prompt._syncSpeak)
            {
                _speakCompleted(sender, e);
            }
            e.Prompt.Synthesizer = null;
        }

        internal void OnSpeakCompleted(SpeakCompletedEventArgs e)
        {
            e.Prompt.IsCompleted = true;
            _asyncWorkerUI.PostOperation(new EventHandler<SpeakCompletedEventArgs>(FireSpeakCompleted), _speechSyntesizer.Target, e);
        }

        internal void OnSpeakProgress(SpeakProgressEventArgs e)
        {
            if (_speakProgress != null)
            {
                string text = string.Empty;
                if (e.Prompt._media == SynthesisMediaType.Ssml)
                {
                    int length = e.CharacterCount;
                    text = RemoveEscapeString(e.Prompt._text, e.CharacterPosition, length, out length);
                    e.CharacterCount = length;
                }
                else
                {
                    text = e.Prompt._text.Substring(e.CharacterPosition, e.CharacterCount);
                }

                e.Text = text;
                _asyncWorkerUI.PostOperation(_speakProgress, _speechSyntesizer.Target, e);
            }
        }

        private string RemoveEscapeString(string text, int start, int length, out int newLength)
        {
            newLength = length;

            // Find the pos '>' from the start position and so substitution from this point on
            int startInXml = text.LastIndexOf('>', start);

            System.Diagnostics.Debug.Assert(startInXml >= 0);

            // Check for special character strings "%gt;", etc... and convert them to "<" etc...
            int curPos = startInXml;
            StringBuilder sb = new(text.Substring(0, curPos));

            do
            {
                // Look for one of the Xml escape string
                int iEscapeString = -1;
                int pos = int.MaxValue;
                for (int i = 0; i < _xmlEscapeStrings.Length; i++)
                {
                    int idx;
                    if ((idx = text.IndexOf(_xmlEscapeStrings[i], curPos, StringComparison.Ordinal)) >= 0)
                    {
                        if (pos > idx)
                        {
                            pos = idx;
                            iEscapeString = i;
                        }
                    }
                }

                if (iEscapeString < 0)
                {
                    // If no special string have been found then the current position is the end of the string.
                    pos = text.Length;
                }
                else if (pos >= startInXml)
                {
                    // For the character that is replacing the escape sequence.
                    newLength += _xmlEscapeStrings[iEscapeString].Length - 1;
                }
                else
                {
                    // Found an escape sequence but it is it before the current text fragment.
                    pos += _xmlEscapeStrings[iEscapeString].Length;
                    iEscapeString = -1;
                }

                // add the new string
                int len = pos - curPos;
                sb.Append(text.Substring(curPos, len));
                if (iEscapeString >= 0)
                {
                    sb.Append(_xmlEscapeChars[iEscapeString]);
                    int lenEscape = _xmlEscapeStrings[iEscapeString].Length;
                    pos += lenEscape;
                }
                curPos = pos;
            }
            while (start + length > sb.Length);
            return sb.ToString().Substring(start, length);
        }

        internal void OnBookmarkReached(BookmarkReachedEventArgs e)
        {
            if (_bookmarkReached != null)
            {
                _asyncWorkerUI.PostOperation(_bookmarkReached, _speechSyntesizer.Target, e);
            }
        }

        internal void OnVoiceChange(VoiceChangeEventArgs e)
        {
            if (_voiceChange != null)
            {
                _asyncWorkerUI.PostOperation(_voiceChange, _speechSyntesizer.Target, e);
            }
        }

        internal void OnPhonemeReached(PhonemeReachedEventArgs e)
        {
            if (_phonemeReached != null)
            {
                _asyncWorkerUI.PostOperation(_phonemeReached, _speechSyntesizer.Target, e);
            }
        }

        private void OnVisemeReached(VisemeReachedEventArgs e)
        {
            if (_visemeReached != null)
            {
                _asyncWorkerUI.PostOperation(_visemeReached, _speechSyntesizer.Target, e);
            }
        }

        private void OnStateChanged(object o)
        {
            // For all other events the lock is done in the dispatch method
            lock (_thisObjectLock)
            {
                StateChangedEventArgs e = (StateChangedEventArgs)o;
                if (_stateChanged != null)
                {
                    _asyncWorkerUI.PostOperation(_stateChanged, _speechSyntesizer.Target, e);
                }
            }
        }

        internal void AddEvent<T>(TtsEventId ttsEvent, ref EventHandler<T> internalEventHandler, EventHandler<T> eventHandler) where T : PromptEventArgs
        {
            lock (_thisObjectLock)
            {
                Helpers.ThrowIfNull(eventHandler, nameof(eventHandler));

                // could through if unsuccessful - delay the SetEventInterest
                bool fSetSapiInterest = internalEventHandler == null;
                internalEventHandler += eventHandler;

                if (fSetSapiInterest)
                {
                    _ttsEvents |= (1 << (int)ttsEvent);

                    SetInterest(_ttsEvents);
                }
            }
        }

        internal void RemoveEvent<T>(TtsEventId ttsEvent, ref EventHandler<T> internalEventHandler, EventHandler<T> eventHandler) where T : EventArgs
        {
            lock (_thisObjectLock)
            {
                Helpers.ThrowIfNull(eventHandler, nameof(eventHandler));

                // could through if unsuccessful - delay the SetEventInterest
                internalEventHandler -= eventHandler;

                if (internalEventHandler == null)
                {
                    _ttsEvents &= ~(1 << (int)ttsEvent);

                    SetInterest(_ttsEvents);
                }
            }
        }

        #endregion

        #endregion
        internal void SetOutput(Stream stream, SpeechAudioFormatInfo formatInfo, bool headerInfo)
        {
            lock (_pendingSpeakQueue)
            {
                // Output is not supposed to change while speaking.
                if (State == SynthesizerState.Speaking)
                {
                    throw new InvalidOperationException(SR.Get(SRID.SynthesizerSetOutputSpeaking));
                }

                if (State == SynthesizerState.Paused)
                {
                    throw new InvalidOperationException(SR.Get(SRID.SynthesizerSyncSetOutputWhilePaused));
                }

                lock (_processingSpeakLock)
                {
                    if (stream == null)
                    {
                        _waveOut = new AudioDeviceOut(SAPICategories.DefaultDeviceOut(), _asyncWorker);
                    }
                    else
                    {
                        _waveOut = new AudioFileOut(stream, formatInfo, headerInfo, _asyncWorker);
                    }
                }
            }
        }

        /// <summary>
        /// Description:
        ///     This method synchronously purges all data that is currently in the
        /// rendering pipeline.
        /// </summary>
        internal void Abort()
        {
            //--- Purge all pending speak requests and reset the voice
            lock (_pendingSpeakQueue)
            {
                lock (_site)
                {
                    if (_currentPrompt != null)
                    {
                        _site.Abort();
                        _waveOut.Abort();
                    }
                }
                lock (_processingSpeakLock)
                {
                    Parameters[] parameters = _pendingSpeakQueue.ToArray();
                    foreach (Parameters parameter in parameters)
                    {
                        ParametersSpeak paramSpeak = parameter._parameter as ParametersSpeak;
                        if (paramSpeak != null)
                        {
                            paramSpeak._prompt.Exception = new OperationCanceledException(SR.Get(SRID.PromptAsyncOperationCancelled));
                        }
                    }
                    // Restart the worker thread
                    _evtPendingSpeak.Set();
                }
            }
        }

        /// <summary>
        /// Description:
        ///     This method synchronously purges all data that is currently in the
        /// rendering pipeline.
        /// </summary>
        internal void Abort(Prompt prompt)
        {
            //--- Purge all pending speak requests and reset the voice
            lock (_pendingSpeakQueue)
            {
                bool found = false;
                foreach (Parameters parameters in _pendingSpeakQueue)
                {
                    ParametersSpeak paramSpeak = parameters._parameter as ParametersSpeak;
                    if (paramSpeak._prompt == prompt)
                    {
                        paramSpeak._prompt.Exception = new OperationCanceledException(SR.Get(SRID.PromptAsyncOperationCancelled));
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // Not in the list, it could be the current prompt
                    lock (_site)
                    {
                        if (_currentPrompt == prompt)
                        {
                            _site.Abort();
                            _waveOut.Abort();
                        }
                    }
                    // Wait for completion
                    lock (_processingSpeakLock)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Pause the audio
        /// </summary>
        internal void Pause()
        {
            lock (_waveOut)
            {
                _waveOut?.Pause();

                lock (_pendingSpeakQueue)
                {
                    // The pause arrived after a speak call was initiated but before it started to speak
                    // Simulated a Re
                    if (_pendingSpeakQueue.Count > 0 && State == SynthesizerState.Ready)
                    {
                        OnStateChanged(SynthesizerState.Speaking);
                    }
                    OnStateChanged(SynthesizerState.Paused);
                }
            }
        }

        /// <summary>
        /// Resume the audio
        /// </summary>
        internal void Resume()
        {
            lock (_waveOut)
            {
                _waveOut?.Resume();
                lock (_pendingSpeakQueue)
                {
                    if (_pendingSpeakQueue.Count > 0 || _currentPrompt != null)
                    {
                        OnStateChanged(SynthesizerState.Speaking);
                    }
                    else
                    {
                        // The state could be set to paused if the Paused happened after the speak happened
                        if (State == SynthesizerState.Paused)
                        {
                            OnStateChanged(SynthesizerState.Speaking);
                        }
                        OnStateChanged(SynthesizerState.Ready);
                    }
                }
            }
        }

        internal void AddLexicon(Uri uri, string mediaType)
        {
            LexiconEntry lexiconEntry = new(uri, mediaType);
            lock (_processingSpeakLock)
            {
                foreach (LexiconEntry lexicon in _lexicons)
                {
                    if (lexicon._uri.Equals(uri))
                    {
                        throw new InvalidOperationException(SR.Get(SRID.DuplicatedEntry));
                    }
                }
                _lexicons.Add(lexiconEntry);
            }
        }

        internal void RemoveLexicon(Uri uri)
        {
            lock (_processingSpeakLock)
            {
                foreach (LexiconEntry lexicon in _lexicons)
                {
                    if (lexicon._uri.Equals(uri))
                    {
                        _lexicons.Remove(lexicon);

                        // Bail out found
                        return;
                    }
                }
                throw new InvalidOperationException(SR.Get(SRID.FileNotFound, uri.ToString()));
            }
        }

        /// <summary>
        /// This method is used to create the Engine voice and initialize the culture
        /// </summary>
        internal TTSVoice GetEngine(string name, CultureInfo culture, VoiceGender gender, VoiceAge age, int variant, bool switchContext)
        {
            TTSVoice defaultVoice = _currentVoice ?? GetVoice(switchContext);

            return GetEngineWithVoice(defaultVoice, null, name, culture, gender, age, variant, switchContext);
        }

        /// <summary>
        /// Returns the voices for a given (or all cultures)
        /// </summary>
        /// <param name="culture">Culture or null for all culture</param>
        internal ReadOnlyCollection<InstalledVoice> GetInstalledVoices(CultureInfo culture)
        {
            if (culture == null || culture == CultureInfo.InvariantCulture)
            {
                return new ReadOnlyCollection<InstalledVoice>(_installedVoices);
            }
            else
            {
                Collection<InstalledVoice> voices = new();

                // loop all the available voices in the registry
                // no check if the voice are valid
                foreach (InstalledVoice voice in _installedVoices)
                {
                    // Either all voices if culture is
                    if (culture.Equals(voice.VoiceInfo.Culture))
                    {
                        voices.Add(voice);
                    }
                }
                return new ReadOnlyCollection<InstalledVoice>(voices);
            }
        }

        #endregion

        #region Internal Properties
        internal Prompt Prompt
        {
            get
            {
                lock (_pendingSpeakQueue)
                {
                    return _currentPrompt;
                }
            }
        }
        internal SynthesizerState State
        {
            get
            {
                return _synthesizerState;
            }
        }
        internal int Rate
        {
            get
            {
                return _site.VoiceRate;
            }
            set
            {
                _site.VoiceRate = _defaultRate = value;
            }
        }
        internal int Volume
        {
            get
            {
                return _site.VoiceVolume;
            }
            set
            {
                _site.VoiceVolume = value;
            }
        }

        /// <summary>
        /// Set/Get the default voice
        /// </summary>
        internal TTSVoice Voice
        {
            set
            {
                lock (_defaultVoiceLock)
                {
                    if (_currentVoice == _defaultVoice && value == null)
                    {
                        _defaultVoiceInitialized = false;
                    }
                    _currentVoice = value;
                }
            }
        }

        /// <summary>
        /// Set/Get the default voice
        /// </summary>
        internal TTSVoice CurrentVoice(bool switchContext)
        {
            lock (_defaultVoiceLock)
            {
                // If no voice defined then get the default voice
                if (_currentVoice == null)
                {
                    GetVoice(switchContext);
                }
                return _currentVoice;
            }
        }

        #endregion

        #region Internal Fields

        // Internal event handlers
        internal EventHandler<StateChangedEventArgs> _stateChanged;
        // Internal event handlers
        internal EventHandler<SpeakStartedEventArgs> _speakStarted;
        internal EventHandler<SpeakCompletedEventArgs> _speakCompleted;
        internal EventHandler<SpeakProgressEventArgs> _speakProgress;
        internal EventHandler<BookmarkReachedEventArgs> _bookmarkReached;
        internal EventHandler<VoiceChangeEventArgs> _voiceChange;

        internal EventHandler<PhonemeReachedEventArgs> _phonemeReached;

        internal EventHandler<VisemeReachedEventArgs> _visemeReached;

        #endregion

        #region Private Members

        //
        //=== ISpThreadTask ================================================================
        //
        //  These methods implement the ISpThreadTask interface.  They will all be called on
        //  a worker thread.

        /// <summary>
        /// This method is the task proc used for text rendering and for event
        /// forwarding.  It may be called on a worker thread for asynchronous speaking, or
        /// it may be called on the client thread for synchronous speaking.  If it is
        /// called on the client thread, the hExitThreadEvent handle will be null.
        /// </summary>
        private void ThreadProc()
        {
            while (true)
            {
                Parameters parameters;

                _evtPendingSpeak.WaitOne();

                //--- Get the next speak item
                lock (_pendingSpeakQueue)
                {
                    if (_pendingSpeakQueue.Count > 0)
                    {
                        parameters = _pendingSpeakQueue.Dequeue();
                        ParametersSpeak paramSpeak = parameters._parameter as ParametersSpeak;
                        if (paramSpeak != null)
                        {
                            lock (_site)
                            {
                                if (_currentPrompt == null && State != SynthesizerState.Paused)
                                {
                                    OnStateChanged(SynthesizerState.Speaking);
                                }
                                _currentPrompt = paramSpeak._prompt;
                                _waveOut.IsAborted = false;
                            }
                        }
                        else
                        {
                            _currentPrompt = null;
                        }
                    }
                    else
                    {
                        parameters = null;
                    }
                }

                // The client thread may have cleared the list to abort the audio
                if (parameters != null)
                {
                    switch (parameters._action)
                    {
                        case Action.GetVoice:
                            {
                                try
                                {
                                    _pendingVoice = null;
                                    _pendingException = null;
                                    _pendingVoice = GetProxyEngine((VoiceInfo)parameters._parameter);
                                }
#pragma warning disable 6500
                                catch (Exception e)
                                {
                                    // this thread cannot be terminated.
                                    _pendingException = e;
                                }
#pragma warning restore 6500
                                finally
                                {
                                    // unlock the client
                                    _evtPendingGetProxy.Set();
                                }
                            }
                            break;

                        case Action.SpeakText:
                            {
                                ParametersSpeak paramSpeak = (ParametersSpeak)parameters._parameter;
                                try
                                {
                                    InjectEvent(TtsEventId.StartInputStream, paramSpeak._prompt, paramSpeak._prompt.Exception, null);

                                    if (paramSpeak._prompt.Exception == null)
                                    {
                                        // No lexicon yet
                                        List<LexiconEntry> lexicons = new();

                                        //--- Create a single speak info structure for all the text
                                        TTSVoice voice = _currentVoice ?? GetVoice(false);
                                        //--- Create the speak info

                                        SpeakInfo speakInfo = new(this, voice);

                                        if (paramSpeak._textToSpeak != null)
                                        {
                                            //--- Make sure we have a voice defined by now
                                            if (!paramSpeak._isXml)
                                            {
                                                FragmentState fragmentState = new();
                                                fragmentState.Action = TtsEngineAction.Speak;
                                                fragmentState.Prosody = new Prosody();
                                                TextFragment textFragment = new(fragmentState, paramSpeak._textToSpeak);
                                                speakInfo.AddText(voice, textFragment);
                                            }
                                            else
                                            {
                                                TextFragmentEngine engine = new(speakInfo, paramSpeak._textToSpeak, _pexml, _resourceLoader, lexicons);
                                                SsmlParser.Parse(paramSpeak._textToSpeak, engine, speakInfo.Voice);
                                            }
                                        }
                                        else
                                        {
                                            speakInfo.AddAudio(new AudioData(paramSpeak._audioFile, _resourceLoader));
                                        }

                                        // Add the global synthesizer lexicon
                                        lexicons.AddRange(_lexicons);

                                        System.Diagnostics.Debug.Assert(speakInfo != null);
                                        SpeakText(speakInfo, paramSpeak._prompt, lexicons);
                                    }
                                    ChangeStateToReady(paramSpeak._prompt, paramSpeak._prompt.Exception);
                                }

#pragma warning disable 6500

                                catch (Exception e)
                                {
                                    //--- Always inject the end of stream and complete even on failure
                                    //    Note: we're not getting the return codes from these so we
                                    //          don't overwrite a possible error from above. Also we
                                    //          really don't care about these errors.
                                    ChangeStateToReady(paramSpeak._prompt, e);
                                }
                            }
                            break;

#pragma warning restore 6500

                        default:
                            System.Diagnostics.Debug.Assert(false, "Unknown Action!");
                            break;
                    }
                }

                //--- Get the next speak item
                lock (_pendingSpeakQueue)
                {
                    // if nothing left then reset the wait handle.
                    if (_pendingSpeakQueue.Count == 0)
                    {
                        _evtPendingSpeak.Reset();
                    }
                }

                // check if we need to terminate this thread
                if (_fExitWorkerThread)
                {
                    _synthesizerState = SynthesizerState.Ready;
                    break;
                }
            }
        }

        private void AddSpeakParameters(Parameters param)
        {
            lock (_pendingSpeakQueue)
            {
                _pendingSpeakQueue.Enqueue(param);

                // Start the worker thread if the list was empty
                if (_pendingSpeakQueue.Count == 1)
                {
                    _evtPendingSpeak.Set();
                }
            }
        }

        /// <summary>
        /// This method renders the current speak info structure. It may be
        /// made up of one or more speech segments, each intended for a different
        /// voice/engine.
        /// </summary>
        private void SpeakText(SpeakInfo speakInfo, Prompt prompt, List<LexiconEntry> lexicons)
        {
            VoiceInfo currentVoiceId = null;

            //=== Main processing loop ===========================================
            for (SpeechSeg speechSeg; (speechSeg = speakInfo.RemoveFirst()) != null;)
            {
                TTSVoice voice;

                //--- Update the current rendering engine
                voice = speechSeg.Voice;

                // Fire the voice change object token if necessary
                if (voice != null && (currentVoiceId == null || !currentVoiceId.Equals(voice.VoiceInfo)))
                {
                    currentVoiceId = voice.VoiceInfo;
                    InjectEvent(TtsEventId.VoiceChange, prompt, null, currentVoiceId);
                }

                lock (_processingSpeakLock)
                {
                    if (speechSeg.IsText)
                    {
                        //--- Speak the segment
                        lock (_site)
                        {
                            if (_waveOut.IsAborted)
                            {
                                _waveOut.IsAborted = false;
                                //--- Always inject the end of stream and complete event on failure
                                throw new OperationCanceledException(SR.Get(SRID.PromptAsyncOperationCancelled));
                            }
                            _site.InitRun(_waveOut, _defaultRate, prompt);
                            _waveOut.Begin(voice.WaveFormat(_waveOut.WaveFormat));
                        }

                        // Set the Lexicons if any
                        try
                        {
                            // Update the lexicon and set the default events to trap
                            voice.UpdateLexicons(lexicons);
                            _site.SetEventsInterest(_ttsInterest);

                            // Calls GetOutputFormat if needed on the TTS engine
                            byte[] outputWaveFormat = voice.WaveFormat(_waveOut.WaveFormat);

                            // Get the TTS engine or a backup voice
                            ITtsEngineProxy engineProxy = voice.TtsEngine;

                            // Set the events specific to the desktop
                            if ((_ttsInterest & (1 << (int)TtsEventId.Phoneme)) != 0 && engineProxy.EngineAlphabet != AlphabetType.Ipa)
                            {
                                _site.EventMapper = new PhonemeEventMapper(_site, PhonemeEventMapper.PhonemeConversion.SapiToIpa, engineProxy.AlphabetConverter);
                            }
                            else
                            {
                                _site.EventMapper = null;
                            }
                            // Call the TTS engine to perform the speak through the proxy layer that
                            // converts SSML fragments to whatever the TTS engine supports
                            _site.LastException = null;
                            engineProxy.Speak(speechSeg.FragmentList, outputWaveFormat);
                        }
                        finally
                        {
                            _waveOut.WaitUntilDone();
                            _waveOut.End();
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(speechSeg.Audio != null);

                        _waveOut.PlayWaveFile(speechSeg.Audio);

                        // Done with the audio, release the underlying stream
                        speechSeg.Audio.Dispose();
                    }
                    lock (_site)
                    {
                        // The current prompt has now been played
                        _currentPrompt = null;

                        // Check for abort or errors during the play
                        if (_waveOut.IsAborted || _site.LastException != null)
                        {
                            _waveOut.IsAborted = false;

                            if (_site.LastException != null)
                            {
                                Exception lastException = _site.LastException;
                                _site.LastException = null;
                                ExceptionDispatchInfo.Throw(lastException);
                            }
                            //--- Always inject the end of stream and complete event on failure
                            throw new OperationCanceledException(SR.Get(SRID.PromptAsyncOperationCancelled));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the user's default rate from the registry
        /// </summary>
        private static uint GetDefaultRate()
        {
            //--- Read the current user's default rate
            uint lCurrRateAd = 0;
            using (ObjectTokenCategory category = ObjectTokenCategory.Create(SAPICategories.CurrentUserVoices))
            {
                category?.TryGetDWORD(defaultVoiceRate, ref lCurrRateAd);
            }
            return lCurrRateAd;
        }

        private void InjectEvent(TtsEventId evtId, Prompt prompt, Exception exception, VoiceInfo voiceInfo)
        {
            // If the prompt is terminated, release it ASAP
            if (evtId == TtsEventId.EndInputStream)
            {
                _site.EventMapper?.FlushEvent();
                prompt.Exception = exception;
            }

            int evtMask = 1 << (int)evtId;
            if ((evtMask & _ttsInterest) != 0)
            {
                TTSEvent ttsEvent = new(evtId, prompt, exception, voiceInfo);
                _asyncWorker.Post(ttsEvent);
            }
        }

        /// <summary>
        /// Calls the client notification delegate.
        /// </summary>
        private void OnStateChanged(SynthesizerState state)
        {
            if (_synthesizerState != state)
            {
                // Keep the last state
                SynthesizerState previousState = _synthesizerState;
                _synthesizerState = state;

                // Fire the events
                if (_eventStateChanged != null)
                {
                    _asyncWorker.PostOperation(_eventStateChanged, new StateChangedEventArgs(state, previousState));
                }
            }
        }

        /// <summary>
        /// Set the state to ready if nothing anymore needs to be spoken.
        /// </summary>
        private void ChangeStateToReady(Prompt prompt, Exception exception)
        {
            lock (_waveOut)
            {
                //--- Get the next speak item
                lock (_pendingSpeakQueue)
                {
                    // if nothing left then reset the wait handle.
                    if (_pendingSpeakQueue.Count == 0)
                    {
                        _currentPrompt = null;
                        System.Diagnostics.Debug.Assert(State == SynthesizerState.Speaking || State == SynthesizerState.Paused);

                        if (State != SynthesizerState.Paused)
                        {
                            // Keep the last state
                            SynthesizerState previousState = _synthesizerState;
                            _synthesizerState = SynthesizerState.Ready;

                            // Fire the notification for end of prompt
                            InjectEvent(TtsEventId.EndInputStream, prompt, exception, null);
                            if (_eventStateChanged != null)
                            {
                                _asyncWorker.PostOperation(_eventStateChanged, new StateChangedEventArgs(_synthesizerState, previousState));
                            }
                        }
                        else
                        {
                            // Pause mode. Send a single notification for end of prompt
                            InjectEvent(TtsEventId.EndInputStream, prompt, exception, null);
                        }
                    }
                    else
                    {
                        // More prompts to play.
                        // Send a single notification that this one is over.
                        InjectEvent(TtsEventId.EndInputStream, prompt, exception, null);
                    }
                }
            }
        }

        /// <summary>
        /// This method is used to create the Engine voice and initialize
        /// </summary>
        private TTSVoice GetVoice(VoiceInfo voiceInfo, bool switchContext)
        {
            TTSVoice voice = null;

            lock (_voiceDictionary)
            {
                if (!_voiceDictionary.TryGetValue(voiceInfo, out voice))
                {
                    if (switchContext)
                    {
                        ExecuteOnBackgroundThread(Action.GetVoice, voiceInfo);

                        // Voice is null if exception occurred
                        voice = _pendingException == null ? _pendingVoice : null;
                    }
                    else
                    {
                        // Get the voice
                        voice = GetProxyEngine(voiceInfo);
                    }
                }
            }
            return voice;
        }

        private void ExecuteOnBackgroundThread(Action action, object parameter)
        {
            //--- Get the voice on the worker thread
            lock (_pendingSpeakQueue)
            {
                _evtPendingGetProxy.Reset();
                _pendingSpeakQueue.Enqueue(new Parameters(action, parameter));

                // Start the worker thread if the list was empty
                if (_pendingSpeakQueue.Count == 1)
                {
                    _evtPendingSpeak.Set();
                }
            }
            _evtPendingGetProxy.WaitOne();
        }

        private TTSVoice GetEngineWithVoice(TTSVoice defaultVoice, VoiceInfo defaultVoiceId, string name, CultureInfo culture, VoiceGender gender, VoiceAge age, int variant, bool switchContext)
        {
            TTSVoice voice = null;

            // The list of enabled voices can be changed by a speech application
            lock (_enabledVoicesLock)
            {
                // Do we have a name?
                if (!string.IsNullOrEmpty(name))
                {
                    // try to find a voice for a given name
                    voice = MatchVoice(name, variant, switchContext);
                }

                // Still no voice loop to find a matching one.
                if (voice == null)
                {
                    InstalledVoice viDefault = null;

                    // Easy out if the voice is the default voice
                    if (defaultVoice != null || defaultVoiceId != null)
                    {
                        // try to select the default voice
                        viDefault = InstalledVoice.Find(_installedVoices, defaultVoice != null ? defaultVoice.VoiceInfo : defaultVoiceId);

                        if (viDefault != null && viDefault.Enabled && variant == 1)
                        {
                            VoiceInfo vi = viDefault.VoiceInfo;
                            if (viDefault.Enabled && vi.Culture.Equals(culture) && (gender == VoiceGender.NotSet || gender == VoiceGender.Neutral || gender == vi.Gender) && (age == VoiceAge.NotSet || age == vi.Age))
                            {
                                voice = defaultVoice;
                            }
                        }
                    }

                    // Pick the first one in the list as the backup default
                    while (voice == null && _installedVoices.Count > 0)
                    {
                        viDefault ??= InstalledVoice.FirstEnabled(_installedVoices, CultureInfo.CurrentUICulture);

                        if (viDefault != null)
                        {
                            voice = MatchVoice(culture, gender, age, variant, switchContext, ref viDefault);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                //--- Create the default voice
                if (voice == null)
                {
                    if (defaultVoice == null)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.SynthesizerVoiceFailed));
                    }
                    else
                    {
                        voice = defaultVoice;
                    }
                }
            }
            return voice;
        }

        /// <summary>
        /// Try to find a voice for a given name
        /// </summary>
        private TTSVoice MatchVoice(string name, int variant, bool switchContext)
        {
            TTSVoice voice = null;
            // Look for it in the object tokens
            VoiceInfo voiceInfo = null;
            int cVariant = variant;

            foreach (InstalledVoice sysVoice in _installedVoices)
            {
                int firstCharacter;
                if (sysVoice.Enabled && (firstCharacter = name.IndexOf(sysVoice.VoiceInfo.Name, StringComparison.Ordinal)) >= 0)
                {
                    int lastCharacter = firstCharacter + sysVoice.VoiceInfo.Name.Length;
                    if ((firstCharacter == 0 || name[firstCharacter - 1] == ' ') && (lastCharacter == name.Length || name[lastCharacter] == ' '))
                    {
                        voiceInfo = sysVoice.VoiceInfo;
                        if (cVariant-- == 1)
                        {
                            break;
                        }
                    }
                }
            }

            // If we had a name, try to get engine from it
            if (voiceInfo != null)
            {
                // Do we already have an voice for this voiceInfo?
                voice = GetVoice(voiceInfo, switchContext);
            }
            return voice;
        }

        private TTSVoice MatchVoice(CultureInfo culture, VoiceGender gender, VoiceAge age, int variant, bool switchContext, ref InstalledVoice viDefault)
        {
            TTSVoice voice = null;

            // Build a list with all the tokens
            List<InstalledVoice> tokens = new(_installedVoices);

            // Remove all the voices that are disabled
            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                if (!tokens[i].Enabled)
                {
                    tokens.RemoveAt(i);
                }
            }

            // Try to select the best available voice
            for (; voice == null && tokens.Count > 0;)
            {
                InstalledVoice sysVoice = MatchVoice(viDefault, culture, gender, age, variant, tokens);
                if (sysVoice != null)
                {
                    // Find a voice and a match engine!
                    voice = GetVoice(sysVoice.VoiceInfo, switchContext);

                    if (voice == null)
                    {
                        // The voice associated with this token cannot be instantiated.
                        // Remove it from the list of possible voices
                        tokens.Remove(sysVoice);
                        sysVoice.SetEnabledFlag(false, switchContext);
                        if (sysVoice == viDefault)
                        {
                            viDefault = null;
                        }
                    }
                    break;
                }
            }
            return voice;
        }

        private static InstalledVoice MatchVoice(InstalledVoice defaultTokenInfo, CultureInfo culture, VoiceGender gender, VoiceAge age, int variant, List<InstalledVoice> tokensInfo)
        {
            // Set the default return value
            InstalledVoice sysVoice = defaultTokenInfo;
            int bestMatch = CalcMatchValue(culture, gender, age, sysVoice.VoiceInfo);
            int iPosDefault = -1;

            // calc the best possible match
            for (int iToken = 0; iToken < tokensInfo.Count; iToken++)
            {
                InstalledVoice ti = tokensInfo[iToken];
                if (ti.Enabled)
                {
                    int matchValue = CalcMatchValue(culture, gender, age, ti.VoiceInfo);

                    if (ti.Equals(defaultTokenInfo))
                    {
                        iPosDefault = iToken;
                    }

                    // Is this a better match?
                    if (matchValue > bestMatch)
                    {
                        sysVoice = ti;
                        bestMatch = matchValue;
                    }

                    // If we cannot get a better voice, exit
                    if (matchValue == 0x7 && (variant == 1 || iPosDefault >= 0))
                    {
                        break;
                    }
                }
            }

            if (variant > 1)
            {
                // Set the default voice as the first entry
                tokensInfo[iPosDefault] = tokensInfo[0];
                tokensInfo[0] = defaultTokenInfo;
                int requestedVariant = variant;

                do
                {
                    foreach (InstalledVoice ti in tokensInfo)
                    {
                        if (ti.Enabled && CalcMatchValue(culture, gender, age, ti.VoiceInfo) == bestMatch)
                        {
                            // If we are looking for a variant and are matching the best match, switch voice
                            --variant;
                            sysVoice = ti;
                        }
                        if (variant == 0)
                        {
                            break;
                        }
                    }

                    // if the variant number is large, calc the modulo and restart from there
                    if (variant > 0)
                    {
                        variant = requestedVariant % (requestedVariant - variant);
                    }
                }
                while (variant > 0);
            }
            return sysVoice;
        }

        private static int CalcMatchValue(CultureInfo culture, VoiceGender gender, VoiceAge age, VoiceInfo voiceInfo)
        {
            int matchValue;

            if (voiceInfo != null)
            {
                matchValue = 0;
                CultureInfo tokCulture = voiceInfo.Culture;

                if (culture != null && Helpers.CompareInvariantCulture(tokCulture, culture))
                {
                    // Exact Culture match has priority over gender and age.
                    if (culture.Equals(tokCulture))
                    {
                        matchValue |= 0x4;
                    }

                    // Male / Female has priority over age
                    if (gender == VoiceGender.NotSet || voiceInfo.Gender == gender)
                    {
                        matchValue |= 0x2;
                    }

                    // Age check
                    if (age == VoiceAge.NotSet || voiceInfo.Age == age)
                    {
                        matchValue |= 0x1;
                    }
                }
            }
            else
            {
                matchValue = -1;
            }
            return matchValue;
        }

        private TTSVoice GetProxyEngine(VoiceInfo voiceInfo)
        {
            // Create the TTS voice

            // Try to get a managed SSML engine
            ITtsEngineProxy engineProxy = GetSsmlEngine(voiceInfo);

            // Try to get a COM engine
            engineProxy ??= GetComEngine(voiceInfo);

            // store the proxy object
            TTSVoice voice = null;
            if (engineProxy != null)
            {
                voice = new TTSVoice(engineProxy, voiceInfo);
                _voiceDictionary.Add(voiceInfo, voice);
            }
            return voice;
        }

        private ITtsEngineProxy GetSsmlEngine(VoiceInfo voiceInfo)
        {
            // Try first to get a TtsEngineSsml for it
            ITtsEngineProxy engineProxy = null;
            try
            {
                Assembly assembly;
                if (!string.IsNullOrEmpty(voiceInfo.AssemblyName) && (assembly = Assembly.Load(voiceInfo.AssemblyName)) != null)
                {
                    Type[] types = assembly.GetTypes();
                    TtsEngineSsml ssmlEngine = null;
                    foreach (Type type in types)
                    {
                        if (type.IsSubclassOf(typeof(TtsEngineSsml)))
                        {
                            string[] args = new string[] { voiceInfo.Clsid };
                            ssmlEngine = assembly.CreateInstance(type.ToString(), false, BindingFlags.Default, null, args, CultureInfo.CurrentUICulture, null) as TtsEngineSsml;
                            break;
                        }
                    }
                    if (ssmlEngine != null)
                    {
                        // Create the engine site if not yet available
                        engineProxy = new TtsProxySsml(ssmlEngine, _site, voiceInfo.Culture.LCID);
                    }
                }
            }
            catch (ArgumentException)
            {
            }
            catch (IOException)
            {
            }
            catch (BadImageFormatException)
            {
            }
            return engineProxy;
        }

        private ITtsEngineProxy GetComEngine(VoiceInfo voiceInfo)
        {
            ITtsEngineProxy engineProxy = null;
            try
            {
                ObjectToken token = ObjectToken.Open(null, voiceInfo.RegistryKeyPath, false);
                if (token != null)
                {
                    object engine = token.CreateObjectFromToken<object>("CLSID");

                    if (engine != null)
                    {
                        ITtsEngine iTtsEngine = engine as ITtsEngine;
                        if (iTtsEngine != null)
                        {
                            engineProxy = new TtsProxySapi(iTtsEngine, ComEngineSite, voiceInfo.Culture.LCID);
                        }
                    }
                }
            }
            catch (ArgumentException)
            {
            }
            catch (IOException)
            {
            }
            catch (BadImageFormatException)
            {
            }
            catch (COMException)
            {
            }
            catch (FormatException)
            {
            }
            return engineProxy;
        }

        /// <summary>
        /// Returns the default voice for the synth
        /// </summary>
        private TTSVoice GetVoice(bool switchContext)
        {
            lock (_defaultVoiceLock)
            {
                if (!_defaultVoiceInitialized)
                {
                    _defaultVoice = null;
                    ObjectToken defaultVoice = SAPICategories.DefaultToken("Voices");

                    if (defaultVoice != null)
                    {
                        // Try to load a default voice from the default token parameters
                        VoiceGender gender = VoiceGender.NotSet;
                        VoiceAge age = VoiceAge.NotSet;
                        SsmlParserHelpers.TryConvertGender(defaultVoice.Gender.ToLowerInvariant(), out gender);
                        SsmlParserHelpers.TryConvertAge(defaultVoice.Age.ToLowerInvariant(), out age);

                        _defaultVoice = GetEngineWithVoice(null, new VoiceInfo(defaultVoice), defaultVoice.TokenName(), defaultVoice.Culture, gender, age, 1, switchContext);

                        // If failed to get the default, then reset the default token to null.
                        defaultVoice = null;
                    }

                    if (_defaultVoice == null)
                    {
                        // Try to find a default voice that matches the current UI culture
                        VoiceInfo defaultInfo = defaultVoice != null ? new VoiceInfo(defaultVoice) : null;
                        _defaultVoice = GetEngineWithVoice(null, defaultInfo, null, CultureInfo.CurrentUICulture, VoiceGender.NotSet, VoiceAge.NotSet, 1, switchContext);
                    }
                    _defaultVoiceInitialized = true;
                    _currentVoice = _defaultVoice;
                }
            }
            return _defaultVoice;
        }

        private static List<InstalledVoice> BuildInstalledVoices(VoiceSynthesis voiceSynthesizer)
        {
            List<InstalledVoice> voices = new();

            using (ObjectTokenCategory category = ObjectTokenCategory.Create(SAPICategories.Voices))
            {
                if (category != null)
                {
                    // Build a list with all the voicesInfo
                    foreach (ObjectToken voiceToken in category.FindMatchingTokens(null, null))
                    {
                        if (voiceToken != null && voiceToken.Attributes != null)
                        {
                            voices.Add(new InstalledVoice(voiceSynthesizer, new VoiceInfo(voiceToken)));
                        }
                    }
                }
            }
            return voices;
        }

        #region Signal Client application

        private void SignalWorkerThread(object ignored)
        {
            if (_asyncWorkerUI.AsyncMode == false)
            {
                _workerWaitHandle.Set();
            }
        }

        private void ProcessPostData(object arg)
        {
            TTSEvent ttsEvent = arg as TTSEvent;
            if (ttsEvent == null)
            {
                Debug.WriteLine("ProcessPostData: post data is not a TTSEvent object");
                return;
            }
            lock (_thisObjectLock)
            {
                if (!_isDisposed)
                {
                    DispatchEvent(ttsEvent);
                }
            }
        }

        private void DispatchEvent(TTSEvent ttsEvent)
        {
            Prompt prompt = ttsEvent.Prompt;
            Debug.Assert(prompt != null);

            // Raise the appropriate events
            TtsEventId eventId = ttsEvent.Id;
            prompt.Exception = ttsEvent.Exception;
            switch (eventId)
            {
                case TtsEventId.StartInputStream:
                    // SpeakStarted
                    OnSpeakStarted(new SpeakStartedEventArgs(prompt));
                    break;

                case TtsEventId.EndInputStream:
                    // SpeakCompleted
                    OnSpeakCompleted(new SpeakCompletedEventArgs(prompt));
                    break;

                case TtsEventId.SentenceBoundary:
                    break;

                case TtsEventId.WordBoundary:
                    // SpeakProgressChanged
                    OnSpeakProgress(new SpeakProgressEventArgs(prompt, ttsEvent.AudioPosition, (int)ttsEvent.LParam, (int)ttsEvent.WParam));
                    break;

                case TtsEventId.Bookmark:
                    // BookmarkDetected
                    OnBookmarkReached(new BookmarkReachedEventArgs(prompt, ttsEvent.Bookmark, ttsEvent.AudioPosition));
                    break;

                case TtsEventId.VoiceChange:
                    VoiceInfo voice = ttsEvent.Voice;
                    OnVoiceChange(new VoiceChangeEventArgs(prompt, voice));
                    break;

                case TtsEventId.Phoneme:
                    // SynthesizePhoneme
                    OnPhonemeReached(new PhonemeReachedEventArgs(
                        prompt,                                             // Prompt
                        ttsEvent.Phoneme,                                   // Current phoneme
                        ttsEvent.AudioPosition,                             // audioPosition
                        ttsEvent.PhonemeDuration,
                        ttsEvent.PhonemeEmphasis,
                        ttsEvent.NextPhoneme));                             // next phoneme
                    break;

                case TtsEventId.Viseme:
                    // SynthesizeViseme
                    OnVisemeReached(new VisemeReachedEventArgs(
                        prompt,                                             // Prompt
                        (int)ttsEvent.LParam & 0xFFFF,                   // currentViseme
                        ttsEvent.AudioPosition,                             // audioPosition
                        TimeSpan.FromMilliseconds(ttsEvent.WParam >> 16),  // duration
                        (SynthesizerEmphasis)((uint)ttsEvent.LParam >> 16),      // Emphasis
                        (int)(ttsEvent.WParam & 0xFFFF)));                 // nextViseme
                    break;

                default:
                    throw new InvalidOperationException(SR.Get(SRID.SynthesizerUnknownEvent));
            }
        }

        #endregion

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                lock (_thisObjectLock)
                {
                    _fExitWorkerThread = true;

                    // Wait for 2 second max for any pending speak
                    Abort();
                    for (int i = 0; i < 20 && State != SynthesizerState.Ready; i++)
                    {
                        Thread.Sleep(100);
                    }
                    if (disposing)
                    {
                        _evtPendingSpeak.Set();

                        // Wait for the background thread to be done.
                        _workerThread.Join();

                        // Free the COM resources used
                        foreach (KeyValuePair<VoiceInfo, TTSVoice> kv in _voiceDictionary)
                        {
                            kv.Value?.TtsEngine.ReleaseInterface();
                        }
                        _voiceDictionary.Clear();

                        _evtPendingSpeak.Close();
                        _evtPendingGetProxy.Close();
                        _workerWaitHandle.Close();
                    }

                    // If the TTS engine was a COM object, release it.
                    if (_iSite != IntPtr.Zero)
                    {
                        Marshal.Release(_iSite);
                    }

                    // Mark this object as disposed
                    _isDisposed = true;
                }
            }
        }
        private void QueuePrompt(Prompt prompt)
        {
            // Call Sapi Speak with the appropriate flags based on mediaType
            switch (prompt._media)
            {
                case SynthesisMediaType.Text:
                    // Synthesize the speech based on plain text
                    Speak(prompt._text, prompt, false);
                    break;

                case SynthesisMediaType.Ssml:
                    // Synthesize the speech based on Ssml input
                    Speak(prompt._text, prompt, true);
                    break;

                case SynthesisMediaType.WaveAudio:
                    // Synthesize the speech based for Audio
                    SpeakStream(prompt._audio, prompt);
                    break;

                default:
                    throw new ArgumentException(SR.Get(SRID.SynthesizerUnknownMediaType));
            }
        }

        /// <summary>
        /// This method is used to speak a text buffer.
        /// </summary>
        private void Speak(string textToSpeak, Prompt prompt, bool fIsXml)
        {
            Helpers.ThrowIfNull(textToSpeak, nameof(textToSpeak));

            if (_isDisposed)
            {
                throw new ObjectDisposedException("VoiceSynthesis");
            }

            //--- Add the Speak info to the pending TTS rendering list
            AddSpeakParameters(new Parameters(Action.SpeakText, new ParametersSpeak(textToSpeak, prompt, fIsXml, null)));
        }

        private void SpeakStream(Uri audio, Prompt prompt)
        {
            //--- Add the Speak info to the pending TTS rendering list
            AddSpeakParameters(new Parameters(Action.SpeakText, new ParametersSpeak(null, prompt, false, audio)));
        }
        private void SetInterest(int ttsInterest)
        {
            _ttsInterest = ttsInterest;
            //--- Purge all pending speak requests and reset the voice
            lock (_pendingSpeakQueue)
            {
                _site.SetEventsInterest(_ttsInterest);
            }
        }

        #endregion

        #region Private Properties

        private IntPtr ComEngineSite
        {
            get
            {
                // Get the local EngineSite as a COM component
                if (_iSite == IntPtr.Zero)
                {
                    _siteSapi = new EngineSiteSapi(_site, _resourceLoader);
                    _iSite = Marshal.GetComInterfaceForObject(_siteSapi, typeof(ISpEngineSite));
                }
                return _iSite;
            }
        }

        #endregion

        #region Private Types

#pragma warning disable 56524 // No instances of a class created in this module and should not be disposed

        private enum Action
        {
            GetVoice,
            SpeakText,
        }

        private sealed class Parameters
        {
            internal Parameters(Action action, object parameter)
            {
                _action = action;
                _parameter = parameter;
            }

            internal Action _action;
            internal object _parameter;
        }

        private sealed class ParametersSpeak
        {
            internal ParametersSpeak(string textToSpeak, Prompt prompt, bool isXml, Uri audioFile)
            {
                _textToSpeak = textToSpeak;
                _prompt = prompt;
                _isXml = isXml;
                _audioFile = audioFile;
            }

            internal string _textToSpeak;
            internal Prompt _prompt;
            internal bool _isXml;
            internal Uri _audioFile;
        }

#pragma warning restore 56524 // No instances of a class created in this module and should not be disposed

        #endregion

        #region Private Fields

        // Notifications
        private WaitCallback _eventStateChanged;
        private WaitCallback _signalWorkerCallback;

        // Engine site references
        private readonly ResourceLoader _resourceLoader;
        private readonly EngineSite _site;
        private EngineSiteSapi _siteSapi;
        private IntPtr _iSite;
        private int _ttsInterest;

        // Background synchronization
        private ManualResetEvent _evtPendingSpeak = new(false);
        private ManualResetEvent _evtPendingGetProxy = new(false);
        private Exception _pendingException;
        private Queue<Parameters> _pendingSpeakQueue = new();
        private TTSVoice _pendingVoice;

        // Background thread
        private Thread _workerThread;
        private bool _fExitWorkerThread;
        private object _processingSpeakLock = new();

        // Voices info
        private Dictionary<VoiceInfo, TTSVoice> _voiceDictionary = new();
        private List<InstalledVoice> _installedVoices;
        private static List<InstalledVoice> s_allVoices;
        private object _enabledVoicesLock = new();

        // Default voice
        private TTSVoice _defaultVoice;
        private TTSVoice _currentVoice;
        private bool _defaultVoiceInitialized;
        private object _defaultVoiceLock = new();

        private AudioBase _waveOut;
        private int _defaultRate;

        // Is the object disposed?
        private bool _isDisposed;

        // Lexicons associated with this voice
        private List<LexiconEntry> _lexicons = new();

        // output object
        private SynthesizerState _synthesizerState = SynthesizerState.Ready;

        // Currently played prompt
        private Prompt _currentPrompt;

        private const string defaultVoiceRate = "DefaultTTSRate";

        private AsyncSerializedWorker _asyncWorker, _asyncWorkerUI;

        // Prompt Engine
        private const bool _pexml = false;

        /// <summary>
        /// Could be a phrase of an SSML doc or a file reference
        /// </summary>
        private int _ttsEvents = (1 << (int)TtsEventId.StartInputStream) | (1 << (int)TtsEventId.EndInputStream);

        // make sure the object is always in safe state
        private object _thisObjectLock = new();

        private AutoResetEvent _workerWaitHandle = new(false);

        private WeakReference _speechSyntesizer;

        private readonly string[] _xmlEscapeStrings = new string[] { "&quot;", "&apos;", "&amp;", "&lt;", "&gt;" };
        private readonly char[] _xmlEscapeChars = new char[] { '"', '\'', '&', '<', '>' };

        #endregion
    }
}
