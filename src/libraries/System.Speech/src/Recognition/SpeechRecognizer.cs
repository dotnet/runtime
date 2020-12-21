// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Speech.Internal;
using System.Speech.Internal.SapiInterop;
using System.Speech.AudioFormat;

namespace System.Speech.Recognition
{
    /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer"]/*' />

    public class SpeechRecognizer : IDisposable
    {

        #region Constructors

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.SpeechRecognizer"]/*' />
        public SpeechRecognizer()
        {
            _sapiRecognizer = new SapiRecognizer(SapiRecognizer.RecognizerType.Shared);
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.Dispose1"]/*' />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.Dispose2"]/*' />
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                if (_recognizerBase != null)
                {
                    _recognizerBase.Dispose();
                    _recognizerBase = null;
                }
                if (_sapiRecognizer != null)
                {
                    _sapiRecognizer.Dispose();
                    _sapiRecognizer = null;
                }
                _disposed = true; // Don't set RecognizerBase to null as every method will then need to throw ObjectDisposedException.
            }
        }


        #endregion



        #region public Properties

        // Determines whether the recognizer is listening or not.
        // TODO:
        // What does RecognizerState mean?
        // When Hoolie running we either match GetRecoState and Hoolie should update,
        // or use some other mechanism to tell whether Hoolie is running.
        // When Hoolie not installed - this property has no effect unless there are SAPI apps altering the state
        // or the language bar.
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.State"]/*' />
        public RecognizerState State
        {
            get { return RecoBase.State; }
        }


        // Are the grammars attached to this SpeechRecognizer active?  Default = true
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.Enabled"]/*' />
        public bool Enabled
        {
            get { return RecoBase.Enabled; }
            set { RecoBase.Enabled = value; }
        }


        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.PauseRecognizerOnRecognition"]/*' />
        public bool PauseRecognizerOnRecognition
        {
            get { return RecoBase.PauseRecognizerOnRecognition; }
            set { RecoBase.PauseRecognizerOnRecognition = value; }
        }


        // Gives access to the collection of grammars that are currently active. Read-only.
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.Grammars"]/*' />
        public ReadOnlyCollection<Grammar> Grammars
        {
            get { return RecoBase.Grammars; }
        }

        // Gives access to the set of attributes exposed by this recognizer.
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.RecognizerInfo"]/*' />
        public RecognizerInfo RecognizerInfo
        {
            get { return RecoBase.RecognizerInfo; }
        }

        // Data on the audio stream the recognizer is processing
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.AudioStatus"]/*' />
        public AudioState AudioState
        {
            get { return RecoBase.AudioState; }
        }

        // Data on the audio stream the recognizer is processing
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.AudioStatus"]/*' />
        public int AudioLevel
        {
            get { return RecoBase.AudioLevel; }
        }

        // Data on the audio stream the recognizer is processing
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.AudioStatus"]/*' />
        public TimeSpan AudioPosition
        {
            get { return RecoBase.AudioPosition; }
        }

        // Data on the audio stream the recognizer is processing
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.AudioStatus"]/*' />
        public TimeSpan RecognizerAudioPosition
        {
            get { return RecoBase.RecognizerAudioPosition; }
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.AudioFormat"]/*' />
        public SpeechAudioFormatInfo AudioFormat
        {
            get { return RecoBase.AudioFormat; }
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.MaxAlternates"]/*' />
        public int MaxAlternates
        {
            get { return RecoBase.MaxAlternates; }
            set { RecoBase.MaxAlternates = value; }
        }

        #endregion


        #region public Methods


        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.LoadGrammar"]/*' />
        public void LoadGrammar(Grammar grammar)
        {
            RecoBase.LoadGrammar(grammar);
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.LoadGrammarAsync"]/*' />
        public void LoadGrammarAsync(Grammar grammar)
        {
            RecoBase.LoadGrammarAsync(grammar);
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.UnloadGrammar"]/*' />
        public void UnloadGrammar(Grammar grammar)
        {
            RecoBase.UnloadGrammar(grammar);
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.UnloadAllGrammars"]/*' />
        public void UnloadAllGrammars()
        {
            RecoBase.UnloadAllGrammars();
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.EmulateRecognize1"]/*' />
        public RecognitionResult EmulateRecognize(string inputText)
        {
            if (Enabled)
            {
                return RecoBase.EmulateRecognize(inputText);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerNotEnabled));
            }
        }


        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.EmulateRecognize2"]/*' />
        public RecognitionResult EmulateRecognize(string inputText, CompareOptions compareOptions)
        {
            if (Enabled)
            {
                return RecoBase.EmulateRecognize(inputText, compareOptions);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerNotEnabled));
            }
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.EmulateRecognize2"]/*' />
        public RecognitionResult EmulateRecognize(RecognizedWordUnit[] wordUnits, CompareOptions compareOptions)
        {
            if (Enabled)
            {
                return RecoBase.EmulateRecognize(wordUnits, compareOptions);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerNotEnabled));
            }
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.EmulateRecognize1"]/*' />
        public void EmulateRecognizeAsync(string inputText)
        {
            if (Enabled)
            {
                RecoBase.EmulateRecognizeAsync(inputText);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerNotEnabled));
            }
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.EmulateRecognize2"]/*' />
        public void EmulateRecognizeAsync(string inputText, CompareOptions compareOptions)
        {
            if (Enabled)
            {
                RecoBase.EmulateRecognizeAsync(inputText, compareOptions);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerNotEnabled));
            }
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.EmulateRecognize2"]/*' />
        public void EmulateRecognizeAsync(RecognizedWordUnit[] wordUnits, CompareOptions compareOptions)
        {
            if (Enabled)
            {
                RecoBase.EmulateRecognizeAsync(wordUnits, compareOptions);
            }
            else
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerNotEnabled));
            }
        }

        // Methods to pause the recognizer to do atomic updates:
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.RequestRecognizerUpdate1"]/*' />
        public void RequestRecognizerUpdate()
        {
            RecoBase.RequestRecognizerUpdate();
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.RequestRecognizerUpdate2"]/*' />
        public void RequestRecognizerUpdate(object userToken)
        {
            RecoBase.RequestRecognizerUpdate(userToken);
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.RequestRecognizerUpdate3"]/*' />
        public void RequestRecognizerUpdate(object userToken, TimeSpan audioPositionAheadToRaiseUpdate)
        {
            RecoBase.RequestRecognizerUpdate(userToken, audioPositionAheadToRaiseUpdate);
        }

        #endregion


        #region public Events

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.StateChanged"]/*' />
        public event EventHandler<StateChangedEventArgs> StateChanged;

        // Fired when the RecognizeAsync process completes.
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.RecognizeCompleted"]/*' />
        public event EventHandler<EmulateRecognizeCompletedEventArgs> EmulateRecognizeCompleted;

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.LoadGrammarCompleted"]/*' />
        public event EventHandler<LoadGrammarCompletedEventArgs> LoadGrammarCompleted;

        // The event fired when speech is detected. Used for barge-in.
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.SpeechDetected"]/*' />
        public event EventHandler<SpeechDetectedEventArgs> SpeechDetected;

        // The event fired on a recognition.
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.SpeechRecognized"]/*' />
        public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;

        // The event fired on a no recognition
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.SpeechRecognitionRejected"]/*' />
        public event EventHandler<SpeechRecognitionRejectedEventArgs> SpeechRecognitionRejected;

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.RecognizerUpdateReached"]/*' />
        public event EventHandler<RecognizerUpdateReachedEventArgs> RecognizerUpdateReached;

        // Occurs when a spoken phrase is partially recognized.
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.SpeechHypothesized"]/*' />
        public event EventHandler<SpeechHypothesizedEventArgs> SpeechHypothesized
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                Helpers.ThrowIfNull(value, nameof(value));
                if (_speechHypothesizedDelegate == null)
                {
                    RecoBase.SpeechHypothesized += SpeechHypothesizedProxy;
                }
                _speechHypothesizedDelegate += value;
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                Helpers.ThrowIfNull(value, nameof(value));
                _speechHypothesizedDelegate -= value;
                if (_speechHypothesizedDelegate == null)
                {
                    RecoBase.SpeechHypothesized -= SpeechHypothesizedProxy;
                }
            }
        }

        /// TODOC
        public event EventHandler<AudioSignalProblemOccurredEventArgs> AudioSignalProblemOccurred
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                Helpers.ThrowIfNull(value, nameof(value));
                if (_audioSignalProblemOccurredDelegate == null)
                {
                    RecoBase.AudioSignalProblemOccurred += AudioSignalProblemOccurredProxy;
                }
                _audioSignalProblemOccurredDelegate += value;
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                Helpers.ThrowIfNull(value, nameof(value));
                _audioSignalProblemOccurredDelegate -= value;
                if (_audioSignalProblemOccurredDelegate == null)
                {
                    RecoBase.AudioSignalProblemOccurred -= AudioSignalProblemOccurredProxy;
                }
            }
        }

        /// TODOC
        public event EventHandler<AudioLevelUpdatedEventArgs> AudioLevelUpdated
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                Helpers.ThrowIfNull(value, nameof(value));
                if (_audioLevelUpdatedDelegate == null)
                {
                    RecoBase.AudioLevelUpdated += AudioLevelUpdatedProxy;
                }
                _audioLevelUpdatedDelegate += value;
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                Helpers.ThrowIfNull(value, nameof(value));
                _audioLevelUpdatedDelegate -= value;
                if (_audioLevelUpdatedDelegate == null)
                {
                    RecoBase.AudioLevelUpdated -= AudioLevelUpdatedProxy;
                }
            }
        }

        /// TODOC
        public event EventHandler<AudioStateChangedEventArgs> AudioStateChanged
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                Helpers.ThrowIfNull(value, nameof(value));
                if (_audioStateChangedDelegate == null)
                {
                    RecoBase.AudioStateChanged += AudioStateChangedProxy;
                }
                _audioStateChangedDelegate += value;
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                Helpers.ThrowIfNull(value, nameof(value));
                _audioStateChangedDelegate -= value;
                if (_audioStateChangedDelegate == null)
                {
                    RecoBase.AudioStateChanged -= AudioStateChangedProxy;
                }
            }
        }

        #endregion


        #region Private Methods

        // Proxy event handlers used to translate the sender from the RecognizerBase to this class:

        private void StateChangedProxy(object sender, StateChangedEventArgs e)
        {
            EventHandler<StateChangedEventArgs> stateChangedHandler = StateChanged;
            if (stateChangedHandler != null)
            {
                stateChangedHandler(this, e);
            }
        }

        private void EmulateRecognizeCompletedProxy(object sender, EmulateRecognizeCompletedEventArgs e)
        {
            EventHandler<EmulateRecognizeCompletedEventArgs> emulateRecognizeCompletedHandler = EmulateRecognizeCompleted;
            if (emulateRecognizeCompletedHandler != null)
            {
                emulateRecognizeCompletedHandler(this, e);
            }
        }

        private void LoadGrammarCompletedProxy(object sender, LoadGrammarCompletedEventArgs e)
        {
            EventHandler<LoadGrammarCompletedEventArgs> loadGrammarCompletedHandler = LoadGrammarCompleted;
            if (loadGrammarCompletedHandler != null)
            {
                loadGrammarCompletedHandler(this, e);
            }
        }

        private void SpeechDetectedProxy(object sender, SpeechDetectedEventArgs e)
        {
            EventHandler<SpeechDetectedEventArgs> speechDetectedHandler = SpeechDetected;
            if (speechDetectedHandler != null)
            {
                speechDetectedHandler(this, e);
            }
        }

        private void SpeechRecognizedProxy(object sender, SpeechRecognizedEventArgs e)
        {
            EventHandler<SpeechRecognizedEventArgs> speechRecognizedHandler = SpeechRecognized;
            if (speechRecognizedHandler != null)
            {
                speechRecognizedHandler(this, e);
            }
        }

        private void SpeechRecognitionRejectedProxy(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            EventHandler<SpeechRecognitionRejectedEventArgs> speechRecognitionRejectedHandler = SpeechRecognitionRejected;
            if (speechRecognitionRejectedHandler != null)
            {
                speechRecognitionRejectedHandler(this, e);
            }
        }

        private void RecognizerUpdateReachedProxy(object sender, RecognizerUpdateReachedEventArgs e)
        {
            EventHandler<RecognizerUpdateReachedEventArgs> recognizerUpdateReachedHandler = RecognizerUpdateReached;
            if (recognizerUpdateReachedHandler != null)
            {
                recognizerUpdateReachedHandler(this, e);
            }
        }

        private void SpeechHypothesizedProxy(object sender, SpeechHypothesizedEventArgs e)
        {
            EventHandler<SpeechHypothesizedEventArgs> speechHypothesizedHandler = _speechHypothesizedDelegate;
            if (speechHypothesizedHandler != null)
            {
                speechHypothesizedHandler(this, e);
            }
        }

        private void AudioSignalProblemOccurredProxy(object sender, AudioSignalProblemOccurredEventArgs e)
        {
            EventHandler<AudioSignalProblemOccurredEventArgs> audioSignalProblemOccurredHandler = _audioSignalProblemOccurredDelegate;
            if (audioSignalProblemOccurredHandler != null)
            {
                audioSignalProblemOccurredHandler(this, e);
            }
        }

        private void AudioLevelUpdatedProxy(object sender, AudioLevelUpdatedEventArgs e)
        {
            EventHandler<AudioLevelUpdatedEventArgs> audioLevelUpdatedHandler = _audioLevelUpdatedDelegate;
            if (audioLevelUpdatedHandler != null)
            {
                audioLevelUpdatedHandler(this, e);
            }
        }

        private void AudioStateChangedProxy(object sender, AudioStateChangedEventArgs e)
        {
            EventHandler<AudioStateChangedEventArgs> audioStateChangedHandler = _audioStateChangedDelegate;
            if (audioStateChangedHandler != null)
            {
                audioStateChangedHandler(this, e);
            }
        }

        #endregion


        #region Private Properties
        private RecognizerBase RecoBase
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException("SpeechRecognitionEngine");
                }

                if (_recognizerBase == null)
                {
                    _recognizerBase = new RecognizerBase();

                    try
                    {
                        _recognizerBase.Initialize(_sapiRecognizer, false);
                    }
                    catch (COMException e)
                    {
                        throw RecognizerBase.ExceptionFromSapiCreateRecognizerError(e);
                    }

                    // This means the SpeechRecognizer will, by default, not pause after every recognition to allow updates.
                    PauseRecognizerOnRecognition = false;

                    // We always have an input on the SpeechRecognizer.
                    _recognizerBase._haveInputSource = true;

                    // If audio is already being processed then update AudioState.
                    if (AudioPosition != TimeSpan.Zero)
                    {
                        _recognizerBase.AudioState = AudioState.Silence; // Technically it might be Speech but that's okay.
                    }

                    // For the SpeechRecognizer the RecoState is never altered:
                    // - By default that will mean recognition will progress as long as one grammar is loaded and enabled.
                    // - If Hoolie is running it will control the RecoState.

                    // Add event handlers for low-overhead events:
                    _recognizerBase.StateChanged += StateChangedProxy;
                    _recognizerBase.EmulateRecognizeCompleted += EmulateRecognizeCompletedProxy;
                    _recognizerBase.LoadGrammarCompleted += LoadGrammarCompletedProxy;
                    _recognizerBase.SpeechDetected += SpeechDetectedProxy;
                    _recognizerBase.SpeechRecognized += SpeechRecognizedProxy;
                    _recognizerBase.SpeechRecognitionRejected += SpeechRecognitionRejectedProxy;
                    _recognizerBase.RecognizerUpdateReached += RecognizerUpdateReachedProxy;
                }

                return _recognizerBase;
            }
        }
        #endregion


        #region Private Fields

        private bool _disposed;
        private RecognizerBase _recognizerBase;
        private SapiRecognizer _sapiRecognizer;

        private EventHandler<AudioSignalProblemOccurredEventArgs> _audioSignalProblemOccurredDelegate;
        private EventHandler<AudioLevelUpdatedEventArgs> _audioLevelUpdatedDelegate;
        private EventHandler<AudioStateChangedEventArgs> _audioStateChangedDelegate;
        private EventHandler<SpeechHypothesizedEventArgs> _speechHypothesizedDelegate;

        #endregion
    }
}
