// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Speech.AudioFormat;
using System.Speech.Internal;
using System.Speech.Internal.ObjectTokens;
using System.Speech.Internal.SapiInterop;

namespace System.Speech.Recognition
{
    public class SpeechRecognitionEngine : IDisposable
    {
        #region Constructors
        public SpeechRecognitionEngine()
        {
            Initialize(null);
        }
        public SpeechRecognitionEngine(CultureInfo culture)
        {
            Helpers.ThrowIfNull(culture, nameof(culture));

            if (culture.Equals(CultureInfo.InvariantCulture))
            {
                throw new ArgumentException(SR.Get(SRID.InvariantCultureInfo), nameof(culture));
            }

            // Enumerate using collection. It would also be possible to directly access the token from SAPI.
            foreach (RecognizerInfo recognizerInfo in InstalledRecognizers())
            {
                if (culture.Equals(recognizerInfo.Culture))
                {
                    Initialize(recognizerInfo);
                    return;
                }
            }
            // No exact match for the culture, try out with a SR engine of the same base culture.
            foreach (RecognizerInfo recognizerInfo in InstalledRecognizers())
            {
                if (Helpers.CompareInvariantCulture(recognizerInfo.Culture, culture))
                {
                    Initialize(recognizerInfo);
                    return;
                }
            }

            // No match even with culture having the same parent
            throw new ArgumentException(SR.Get(SRID.RecognizerNotFound), nameof(culture));
        }
        public SpeechRecognitionEngine(string recognizerId)
        {
            Helpers.ThrowIfEmptyOrNull(recognizerId, nameof(recognizerId));

            foreach (RecognizerInfo recognizerInfo in InstalledRecognizers())
            {
                if (recognizerId.Equals(recognizerInfo.Id, StringComparison.OrdinalIgnoreCase))
                {
                    Initialize(recognizerInfo);
                    return;
                }
            }

            throw new ArgumentException(SR.Get(SRID.RecognizerNotFound), nameof(recognizerId));
        }
        public SpeechRecognitionEngine(RecognizerInfo recognizerInfo)
        {
            Helpers.ThrowIfNull(recognizerInfo, nameof(recognizerInfo));

            Initialize(recognizerInfo);
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

        #region Static Methods

        // Get attributes of all the recognizers that are installed
        public static ReadOnlyCollection<RecognizerInfo> InstalledRecognizers()
        {
            List<RecognizerInfo> recognizers = new();

            // Get list of ObjectTokens
            using (ObjectTokenCategory category = ObjectTokenCategory.Create(SAPICategories.Recognizers))
            {
                if (category != null)
                {
                    // For each element in list
                    foreach (ObjectToken token in (IEnumerable<ObjectToken>)category)
                    {
                        // Create RecognizerInfo + add to collection
                        RecognizerInfo recognizerInfo = RecognizerInfo.Create(token);

                        if (recognizerInfo == null)
                        {
                            // But if this entry has a corrupt registry entry then skip it.
                            // Otherwise one bogus entry prevents the whole method from working.
                            continue;
                        }
                        recognizers.Add(recognizerInfo);
                    }
                }
            }
            return new ReadOnlyCollection<RecognizerInfo>(recognizers);
        }

        #endregion

        #region public Properties

        // Settings:
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public TimeSpan InitialSilenceTimeout
        {
            get { return RecoBase.InitialSilenceTimeout; }
            set { RecoBase.InitialSilenceTimeout = value; }
        }
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public TimeSpan BabbleTimeout
        {
            get { return RecoBase.BabbleTimeout; }
            set { RecoBase.BabbleTimeout = value; }
        }
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public TimeSpan EndSilenceTimeout
        {
            get { return TimeSpan.FromMilliseconds(RecoBase.QueryRecognizerSettingAsInt(SapiConstants.SPPROP_RESPONSE_SPEED)); }
            set
            {
                if (value.TotalMilliseconds < 0.0f || value.TotalMilliseconds > 10000.0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Get(SRID.EndSilenceOutOfRange));
                }
                RecoBase.UpdateRecognizerSetting(SapiConstants.SPPROP_RESPONSE_SPEED, (int)value.TotalMilliseconds);
            }
        }
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public TimeSpan EndSilenceTimeoutAmbiguous
        {
            get { return TimeSpan.FromMilliseconds(RecoBase.QueryRecognizerSettingAsInt(SapiConstants.SPPROP_COMPLEX_RESPONSE_SPEED)); }
            set
            {
                if (value.TotalMilliseconds < 0.0f || value.TotalMilliseconds > 10000.0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Get(SRID.EndSilenceOutOfRange));
                }
                RecoBase.UpdateRecognizerSetting(SapiConstants.SPPROP_COMPLEX_RESPONSE_SPEED, (int)value.TotalMilliseconds);
            }
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
        public TimeSpan RecognizerAudioPosition
        {
            get { return RecoBase.RecognizerAudioPosition; }
        }

        // Data on the audio stream the recognizer is processing
        public TimeSpan AudioPosition
        {
            get { return RecoBase.AudioPosition; }
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
        public void SetInputToWaveFile(string path)
        {
            Helpers.ThrowIfEmptyOrNull(path, nameof(path));

            RecoBase.SetInput(path);
        }
        public void SetInputToWaveStream(Stream audioSource)
        {
            RecoBase.SetInput(audioSource, null);
        }
        public void SetInputToAudioStream(Stream audioSource, SpeechAudioFormatInfo audioFormat)
        {
            Helpers.ThrowIfNull(audioSource, nameof(audioSource));
            Helpers.ThrowIfNull(audioFormat, nameof(audioFormat));

            RecoBase.SetInput(audioSource, audioFormat);
        }

        // Detach the audio input
        public void SetInputToNull()
        {
            RecoBase.SetInput(null, null);
        }

        // Data on the audio stream the recognizer is processing
        public void SetInputToDefaultAudioDevice()
        {
            RecoBase.SetInputToDefaultAudioDevice();
        }

        // Methods to control recognition process:

        // Does a single synchronous Recognition and then stops the audio stream.
        // Returns null if there was a timeout. Throws on error.
        public RecognitionResult Recognize()
        {
            return RecoBase.Recognize(RecoBase.InitialSilenceTimeout);
        }
        public RecognitionResult Recognize(TimeSpan initialSilenceTimeout)
        {
            if (Grammars.Count == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerHasNoGrammar));
            }

            return RecoBase.Recognize(initialSilenceTimeout);
        }

        // Does a single asynchronous Recognition and then stops the audio stream.
        public void RecognizeAsync()
        {
            RecognizeAsync(RecognizeMode.Single);
        }

        // Can do either a single or multiple recognitions depending on the mode.
        public void RecognizeAsync(RecognizeMode mode)
        {
            if (Grammars.Count == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerHasNoGrammar));
            }

            RecoBase.RecognizeAsync(mode);
        }

        // This method stops recognition immediately without completing processing the audio. Then a RecognizeCompelted event is sent.
        public void RecognizeAsyncCancel()
        {
            RecoBase.RecognizeAsyncCancel();
        }

        // This method stops recognition but audio currently buffered is still processed, so a final SpeechRecognized event may be sent {before the RecognizeCompleted event}.
        public void RecognizeAsyncStop()
        {
            RecoBase.RecognizeAsyncStop();
        }

        // Note: Currently this can't be exposed as a true collection in Yakima {it can't be enumerated}. If we think this would be useful we could do this.
        public object QueryRecognizerSetting(string settingName)
        {
            return RecoBase.QueryRecognizerSetting(settingName);
        }
        public void UpdateRecognizerSetting(string settingName, string updatedValue)
        {
            RecoBase.UpdateRecognizerSetting(settingName, updatedValue);
        }
        public void UpdateRecognizerSetting(string settingName, int updatedValue)
        {
            RecoBase.UpdateRecognizerSetting(settingName, updatedValue);
        }
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
            return EmulateRecognize(inputText, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth);
        }
        public RecognitionResult EmulateRecognize(string inputText, CompareOptions compareOptions)
        {
            if (Grammars.Count == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerHasNoGrammar));
            }

            return RecoBase.EmulateRecognize(inputText, compareOptions);
        }
        public RecognitionResult EmulateRecognize(RecognizedWordUnit[] wordUnits, CompareOptions compareOptions)
        {
            if (Grammars.Count == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerHasNoGrammar));
            }

            return RecoBase.EmulateRecognize(wordUnits, compareOptions);
        }
        public void EmulateRecognizeAsync(string inputText)
        {
            EmulateRecognizeAsync(inputText, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth);
        }
        public void EmulateRecognizeAsync(string inputText, CompareOptions compareOptions)
        {
            if (Grammars.Count == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerHasNoGrammar));
            }

            RecoBase.EmulateRecognizeAsync(inputText, compareOptions);
        }
        public void EmulateRecognizeAsync(RecognizedWordUnit[] wordUnits, CompareOptions compareOptions)
        {
            if (Grammars.Count == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerHasNoGrammar));
            }

            RecoBase.EmulateRecognizeAsync(wordUnits, compareOptions);
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

        // Fired when the RecognizeAsync process completes.
        public event EventHandler<RecognizeCompletedEventArgs> RecognizeCompleted;

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

        private void Initialize(RecognizerInfo recognizerInfo)
        {
            try
            {
                _sapiRecognizer = new SapiRecognizer(SapiRecognizer.RecognizerType.InProc);
            }
            catch (COMException e)
            {
                throw RecognizerBase.ExceptionFromSapiCreateRecognizerError(e);
            }

            if (recognizerInfo != null)
            {
                ObjectToken token = recognizerInfo.GetObjectToken();
                if (token == null)
                {
                    throw new ArgumentException(SR.Get(SRID.NullParamIllegal), nameof(recognizerInfo));
                }
                try
                {
                    _sapiRecognizer.SetRecognizer(token.SAPIToken);
                }
                catch (COMException e)
                {
                    throw new ArgumentException(SR.Get(SRID.RecognizerNotFound), RecognizerBase.ExceptionFromSapiCreateRecognizerError(e));
                }
            }

            // For the SpeechRecognitionEngine we don't want recognition to start until the Recognize() or RecognizeAsync() methods are called.
            _sapiRecognizer.SetRecoState(SPRECOSTATE.SPRST_INACTIVE);
        }

        // Proxy event handlers used to translate the sender from the RecognizerBase to this class:

        private void RecognizeCompletedProxy(object sender, RecognizeCompletedEventArgs e)
        {
            RecognizeCompleted?.Invoke(this, e);
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
                    _recognizerBase.Initialize(_sapiRecognizer, true);

                    // Add event handlers for low-overhead events:
                    _recognizerBase.RecognizeCompleted += RecognizeCompletedProxy;
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
