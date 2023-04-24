// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Speech.AudioFormat;
using System.Speech.Internal;
using System.Speech.Internal.SapiInterop;

namespace System.Speech.Recognition
{
    public class SpeechRecognizer : IDisposable
    {
        #region Constructors
        public SpeechRecognizer()
        {
            _sapiRecognizer = new SapiRecognizer(SapiRecognizer.RecognizerType.Shared);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
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
        public RecognizerState State
        {
            get { return RecoBase.State; }
        }

        // Are the grammars attached to this SpeechRecognizer active?  Default = true
        public bool Enabled
        {
            get { return RecoBase.Enabled; }
            set { RecoBase.Enabled = value; }
        }
        public bool PauseRecognizerOnRecognition
        {
            get { return RecoBase.PauseRecognizerOnRecognition; }
            set { RecoBase.PauseRecognizerOnRecognition = value; }
        }

        // Gives access to the collection of grammars that are currently active. Read-only.
        public ReadOnlyCollection<Grammar> Grammars
        {
            get { return RecoBase.Grammars; }
        }

        // Gives access to the set of attributes exposed by this recognizer.
        public RecognizerInfo RecognizerInfo
        {
            get { return RecoBase.RecognizerInfo; }
        }

        // Data on the audio stream the recognizer is processing
        public AudioState AudioState
        {
            get { return RecoBase.AudioState; }
        }

        // Data on the audio stream the recognizer is processing
        public int AudioLevel
        {
            get { return RecoBase.AudioLevel; }
        }

        // Data on the audio stream the recognizer is processing
        public TimeSpan AudioPosition
        {
            get { return RecoBase.AudioPosition; }
        }

        // Data on the audio stream the recognizer is processing
        public TimeSpan RecognizerAudioPosition
        {
            get { return RecoBase.RecognizerAudioPosition; }
        }
        public SpeechAudioFormatInfo AudioFormat
        {
            get { return RecoBase.AudioFormat; }
        }
        public int MaxAlternates
        {
            get { return RecoBase.MaxAlternates; }
            set { RecoBase.MaxAlternates = value; }
        }

        #endregion

        #region public Methods
        public void LoadGrammar(Grammar grammar)
        {
            RecoBase.LoadGrammar(grammar);
        }
        public void LoadGrammarAsync(Grammar grammar)
        {
            RecoBase.LoadGrammarAsync(grammar);
        }
        public void UnloadGrammar(Grammar grammar)
        {
            RecoBase.UnloadGrammar(grammar);
        }
        public void UnloadAllGrammars()
        {
            RecoBase.UnloadAllGrammars();
        }
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
        public void RequestRecognizerUpdate()
        {
            RecoBase.RequestRecognizerUpdate();
        }
        public void RequestRecognizerUpdate(object userToken)
        {
            RecoBase.RequestRecognizerUpdate(userToken);
        }
        public void RequestRecognizerUpdate(object userToken, TimeSpan audioPositionAheadToRaiseUpdate)
        {
            RecoBase.RequestRecognizerUpdate(userToken, audioPositionAheadToRaiseUpdate);
        }

        #endregion

        #region public Events
        public event EventHandler<StateChangedEventArgs> StateChanged;

        // Fired when the RecognizeAsync process completes.
        public event EventHandler<EmulateRecognizeCompletedEventArgs> EmulateRecognizeCompleted;
        public event EventHandler<LoadGrammarCompletedEventArgs> LoadGrammarCompleted;

        // The event fired when speech is detected. Used for barge-in.
        public event EventHandler<SpeechDetectedEventArgs> SpeechDetected;

        // The event fired on a recognition.
        public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;

        // The event fired on a no recognition
        public event EventHandler<SpeechRecognitionRejectedEventArgs> SpeechRecognitionRejected;
        public event EventHandler<RecognizerUpdateReachedEventArgs> RecognizerUpdateReached;

        // Occurs when a spoken phrase is partially recognized.
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
            StateChanged?.Invoke(this, e);
        }

        private void EmulateRecognizeCompletedProxy(object sender, EmulateRecognizeCompletedEventArgs e)
        {
            EmulateRecognizeCompleted?.Invoke(this, e);
        }

        private void LoadGrammarCompletedProxy(object sender, LoadGrammarCompletedEventArgs e)
        {
            LoadGrammarCompleted?.Invoke(this, e);
        }

        private void SpeechDetectedProxy(object sender, SpeechDetectedEventArgs e)
        {
            SpeechDetected?.Invoke(this, e);
        }

        private void SpeechRecognizedProxy(object sender, SpeechRecognizedEventArgs e)
        {
            SpeechRecognized?.Invoke(this, e);
        }

        private void SpeechRecognitionRejectedProxy(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            SpeechRecognitionRejected?.Invoke(this, e);
        }

        private void RecognizerUpdateReachedProxy(object sender, RecognizerUpdateReachedEventArgs e)
        {
            RecognizerUpdateReached?.Invoke(this, e);
        }

        private void SpeechHypothesizedProxy(object sender, SpeechHypothesizedEventArgs e)
        {
            _speechHypothesizedDelegate?.Invoke(this, e);
        }

        private void AudioSignalProblemOccurredProxy(object sender, AudioSignalProblemOccurredEventArgs e)
        {
            _audioSignalProblemOccurredDelegate?.Invoke(this, e);
        }

        private void AudioLevelUpdatedProxy(object sender, AudioLevelUpdatedEventArgs e)
        {
            _audioLevelUpdatedDelegate?.Invoke(this, e);
        }

        private void AudioStateChangedProxy(object sender, AudioStateChangedEventArgs e)
        {
            _audioStateChangedDelegate?.Invoke(this, e);
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
