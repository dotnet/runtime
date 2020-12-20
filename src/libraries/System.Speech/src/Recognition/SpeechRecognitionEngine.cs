// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Speech.AudioFormat;
using System.Speech.Internal;
using System.Speech.Internal.SapiInterop;
using System.Speech.Internal.ObjectTokens;
using System.Speech.Recognition.SrgsGrammar;

namespace System.Speech.Recognition
{
    /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine"]/*' />

    public class SpeechRecognitionEngine : IDisposable
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.SpeechRecognitionEngine1"]/*' />
        public SpeechRecognitionEngine()
        {
            Initialize(null);
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.SpeechRecognitionEngine4"]/*' />
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

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.SpeechRecognitionEngine3"]/*' />
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

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.SpeechRecognitionEngine2"]/*' />
        public SpeechRecognitionEngine(RecognizerInfo recognizerInfo)
        {
            Helpers.ThrowIfNull(recognizerInfo, nameof(recognizerInfo));

            Initialize(recognizerInfo);
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.Dispose1"]/*' />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.Dispose2"]/*' />
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

        //*******************************************************************
        //
        // Static Public Methods
        //
        //*******************************************************************

        #region Static Methods

        // Get attributes of all the recognizers that are installed
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.InstalledRecognizers"]/*' />
        static public ReadOnlyCollection<RecognizerInfo> InstalledRecognizers()
        {
            List<RecognizerInfo> recognizers = new List<RecognizerInfo>();

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



        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region public Properties

        // Settings:
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.InitialSilenceTimeout"]/*' />
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public TimeSpan InitialSilenceTimeout
        {
            get { return RecoBase.InitialSilenceTimeout; }
            set { RecoBase.InitialSilenceTimeout = value; }
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.BabbleTimeout"]/*' />
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public TimeSpan BabbleTimeout
        {
            get { return RecoBase.BabbleTimeout; }
            set { RecoBase.BabbleTimeout = value; }
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.EndSilenceTimeout"]/*' />
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

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.EndSilenceTimeoutAmbiguous"]/*' />
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
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.Grammars"]/*' />
        public ReadOnlyCollection<Grammar> Grammars
        {
            get { return RecoBase.Grammars; }
        }

        // Gives access to the set of attributes exposed by this recognizer.
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.RecognizerInfo"]/*' />
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
        public TimeSpan RecognizerAudioPosition
        {
            get { return RecoBase.RecognizerAudioPosition; }
        }

        // Data on the audio stream the recognizer is processing
        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.AudioStatus"]/*' />
        public TimeSpan AudioPosition
        {
            get { return RecoBase.AudioPosition; }
        }


        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.AudioFormat"]/*' />
        public SpeechAudioFormatInfo AudioFormat
        {
            get { return RecoBase.AudioFormat; }
        }


        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.MaxAlternates"]/*' />
        public int MaxAlternates
        {
            get { return RecoBase.MaxAlternates; }
            set { RecoBase.MaxAlternates = value; }
        }

        /* TODO
	    // User profile related methods:
        public SpeakerProfile Profile
        {
            get { return null; }
        }
        */



        #endregion



        //*******************************************************************
        //
        // Public Methods
        //
        //*******************************************************************

        #region public Methods

        /* TODO
	    // User profile related methods:
        public void LoadProfile(RecognizerProfile profile)
        {
        }
        */


        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.SetInput2"]/*' />
        public void SetInputToWaveFile(string path)
        {
            Helpers.ThrowIfEmptyOrNull(path, nameof(path));

            RecoBase.SetInput(path);
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.SetInput3"]/*' />
        public void SetInputToWaveStream(Stream audioSource)
        {
            RecoBase.SetInput(audioSource, null);
        }


        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.SetInput4"]/*' />
        public void SetInputToAudioStream(Stream audioSource, SpeechAudioFormatInfo audioFormat)
        {
            Helpers.ThrowIfNull(audioSource, nameof(audioSource));
            Helpers.ThrowIfNull(audioFormat, nameof(audioFormat));

            RecoBase.SetInput(audioSource, audioFormat);
        }


        // Detach the audio input
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.SetInputToDefaultAudioDevice"]/*' />
        public void SetInputToNull()
        {
            RecoBase.SetInput(null, null);
        }

        // Data on the audio stream the recognizer is processing
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.SetInputToDefaultAudioDevice"]/*' />
        public void SetInputToDefaultAudioDevice()
        {
            RecoBase.SetInputToDefaultAudioDevice();
        }

        // Methods to control recognition process:

        // Does a single synchronous Recognition and then stops the audio stream.
        // Returns null if there was a timeout. Throws on error.
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.Recognize"]/*' />
        public RecognitionResult Recognize()
        {
            return RecoBase.Recognize(RecoBase.InitialSilenceTimeout);
        }

        /// TODOC
        public RecognitionResult Recognize(TimeSpan initialSilenceTimeout)
        {
            if (Grammars.Count == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerHasNoGrammar));
            }

            return RecoBase.Recognize(initialSilenceTimeout);
        }

        // Does a single asynchronous Recognition and then stops the audio stream.
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.RecognizeAsync1"]/*' />
        public void RecognizeAsync()
        {
            RecognizeAsync(RecognizeMode.Single);
        }

        // Can do either a single or multiple recognitions depending on the mode.
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.RecognizeAsync2"]/*' />
        public void RecognizeAsync(RecognizeMode mode)
        {
            if (Grammars.Count == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerHasNoGrammar));
            }

            RecoBase.RecognizeAsync(mode);
        }

        // This method stops recognition immediately without completing processing the audio. Then a RecognizeCompelted event is sent.
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.RecognizeAsyncCancel"]/*' />
        public void RecognizeAsyncCancel()
        {
            RecoBase.RecognizeAsyncCancel();
        }

        // This method stops recognition but audio currently buffered is still processed, so a final SpeechRecognized event may be sent {before the RecognizeCompleted event}.
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.RecognizeAsyncStop"]/*' />
        public void RecognizeAsyncStop()
        {
            RecoBase.RecognizeAsyncStop();
        }

        // Note: Currently this can't be exposed as a true collection in Yakima {it can't be enumerated}. If we think this would be useful we could do this.
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.QueryRecognizerSetting"]/*' />
        public object QueryRecognizerSetting(string settingName)
        {
            return RecoBase.QueryRecognizerSetting(settingName);
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.UpdateRecognizerSetting"]/*' />
        public void UpdateRecognizerSetting(string settingName, string updatedValue)
        {
            RecoBase.UpdateRecognizerSetting(settingName, updatedValue);
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.UpdateRecognizerSetting"]/*' />
        public void UpdateRecognizerSetting(string settingName, int updatedValue)
        {
            RecoBase.UpdateRecognizerSetting(settingName, updatedValue);
        }


        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.LoadGrammar"]/*' />
        public void LoadGrammar(Grammar grammar)
        {
            RecoBase.LoadGrammar(grammar);
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.LoadGrammarAsync"]/*' />
        public void LoadGrammarAsync(Grammar grammar)
        {
            RecoBase.LoadGrammarAsync(grammar);
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.UnloadGrammar"]/*' />
        public void UnloadGrammar(Grammar grammar)
        {
            RecoBase.UnloadGrammar(grammar);
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.UnloadAllGrammars"]/*' />
        public void UnloadAllGrammars()
        {
            RecoBase.UnloadAllGrammars();
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.EmulateRecognize1"]/*' />
        public RecognitionResult EmulateRecognize(string inputText)
        {
            return EmulateRecognize(inputText, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth);
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.EmulateRecognize2"]/*' />
        public RecognitionResult EmulateRecognize(string inputText, CompareOptions compareOptions)
        {
            if (Grammars.Count == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerHasNoGrammar));
            }

            return RecoBase.EmulateRecognize(inputText, compareOptions);
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.EmulateRecognize2"]/*' />
        public RecognitionResult EmulateRecognize(RecognizedWordUnit[] wordUnits, CompareOptions compareOptions)
        {
            if (Grammars.Count == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerHasNoGrammar));
            }

            return RecoBase.EmulateRecognize(wordUnits, compareOptions);
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.EmulateRecognize1"]/*' />
        public void EmulateRecognizeAsync(string inputText)
        {
            EmulateRecognizeAsync(inputText, CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth);
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.EmulateRecognizeAsync2"]/*' />
        public void EmulateRecognizeAsync(string inputText, CompareOptions compareOptions)
        {
            if (Grammars.Count == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerHasNoGrammar));
            }

            RecoBase.EmulateRecognizeAsync(inputText, compareOptions);
        }

        /// TODOC <_include file='doc\SpeechRecognizer.uex' path='docs/doc[@for="SpeechRecognizer.EmulateRecognizeAsync2"]/*' />
        public void EmulateRecognizeAsync(RecognizedWordUnit[] wordUnits, CompareOptions compareOptions)
        {
            if (Grammars.Count == 0)
            {
                throw new InvalidOperationException(SR.Get(SRID.RecognizerHasNoGrammar));
            }

            RecoBase.EmulateRecognizeAsync(wordUnits, compareOptions);
        }

        // Methods to pause the recognizer to do atomic updates:
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.RequestRecognizerUpdate1"]/*' />
        public void RequestRecognizerUpdate()
        {
            RecoBase.RequestRecognizerUpdate();
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.RequestRecognizerUpdate2"]/*' />
        public void RequestRecognizerUpdate(object userToken)
        {
            RecoBase.RequestRecognizerUpdate(userToken);
        }

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.RequestRecognizerUpdate3"]/*' />
        public void RequestRecognizerUpdate(object userToken, TimeSpan audioPositionAheadToRaiseUpdate)
        {
            RecoBase.RequestRecognizerUpdate(userToken, audioPositionAheadToRaiseUpdate);
        }


        #endregion



        //*******************************************************************
        //
        // Public Events
        //
        //*******************************************************************

        #region public Events

        // Fired when the RecognizeAsync process completes.
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.RecognizeCompleted"]/*' />
        public event EventHandler<RecognizeCompletedEventArgs> RecognizeCompleted;

        // Fired when the RecognizeAsync process completes.
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.RecognizeCompleted"]/*' />
        public event EventHandler<EmulateRecognizeCompletedEventArgs> EmulateRecognizeCompleted;

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.LoadGrammarCompleted"]/*' />
        public event EventHandler<LoadGrammarCompletedEventArgs> LoadGrammarCompleted;

        // The event fired when speech is detected. Used for barge-in.
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.SpeechDetected"]/*' />
        public event EventHandler<SpeechDetectedEventArgs> SpeechDetected;

        // The event fired on a recognition.
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.SpeechRecognized"]/*' />
        public event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;

        // The event fired on a no recognition
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.SpeechRecognitionRejected"]/*' />
        public event EventHandler<SpeechRecognitionRejectedEventArgs> SpeechRecognitionRejected;

        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.RecognizerUpdateReached"]/*' />
        public event EventHandler<RecognizerUpdateReachedEventArgs> RecognizerUpdateReached;

        // Occurs when a spoken phrase is partially recognized.
        /// TODOC <_include file='doc\SpeechRecognitionEngine.uex' path='docs/doc[@for="SpeechRecognitionEngine.SpeechHypothesized"]/*' />
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

        //*******************************************************************
        //
        // Private Methods
        //
        //*******************************************************************

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
            EventHandler<RecognizeCompletedEventArgs> recognizeCompletedHandler = RecognizeCompleted;
            if (recognizeCompletedHandler != null)
            {
                recognizeCompletedHandler(this, e);
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

        //*******************************************************************
        //
        // Private Properties
        //
        //*******************************************************************

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

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

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

