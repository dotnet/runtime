// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Speech.AudioFormat;
using System.Speech.Internal;
using System.Speech.Internal.ObjectTokens;
using System.Speech.Internal.Synthesis;
using System.Speech.Synthesis.TtsEngine;
using System.Threading;

using RegistryDataKey = System.Speech.Internal.ObjectTokens.RegistryDataKey;
using RegistryEntry = System.Collections.Generic.KeyValuePair<string, object>;

namespace System.Speech.Synthesis
{
    /// <summary>
    /// TODOC
    /// </summary>
    public sealed class SpeechSynthesizer : IDisposable
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// <summary>
        /// TODOC
        /// </summary>
        public SpeechSynthesizer()
        {
        }

        /// <summary>
        ///
        /// </summary>
        ~SpeechSynthesizer()
        {
            Dispose(false);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        //*******************************************************************
        //
        // Public Methods
        //
        //*******************************************************************

        #region public Methods

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="name"></param>
        public void SelectVoice(string name)
        {
            Helpers.ThrowIfEmptyOrNull(name, nameof(name));
            TTSVoice ttsVoice = VoiceSynthesizer.GetEngine(name, CultureInfo.CurrentUICulture, VoiceGender.NotSet, VoiceAge.NotSet, 1, true);

            if (ttsVoice == null || name != ttsVoice.VoiceInfo.Name)
            {
                // No match - throw
                throw new ArgumentException(SR.Get(SRID.SynthesizerSetVoiceNoMatch));
            }
            VoiceSynthesizer.Voice = ttsVoice;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="gender"></param>
        public void SelectVoiceByHints(VoiceGender gender)
        {
            SelectVoiceByHints(gender, VoiceAge.NotSet, 1, CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="gender"></param>
        /// <param name="age"></param>
        public void SelectVoiceByHints(VoiceGender gender, VoiceAge age)
        {
            SelectVoiceByHints(gender, age, 1, CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="gender"></param>
        /// <param name="age"></param>
        /// <param name="voiceAlternate"></param>
        public void SelectVoiceByHints(VoiceGender gender, VoiceAge age, int voiceAlternate)
        {
            SelectVoiceByHints(gender, age, voiceAlternate, CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="gender"></param>
        /// <param name="age"></param>
        /// <param name="voiceAlternate"></param>
        /// <param name="culture"></param>
        public void SelectVoiceByHints(VoiceGender gender, VoiceAge age, int voiceAlternate, CultureInfo culture)
        {
            Helpers.ThrowIfNull(culture, nameof(culture));

            if (voiceAlternate < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(voiceAlternate), SR.Get(SRID.PromptBuilderInvalidVariant));
            }
            if (!VoiceInfo.ValidateGender(gender))
            {
                throw new ArgumentException(SR.Get(SRID.EnumInvalid, "VoiceGender"), nameof(gender));
            }

            if (!VoiceInfo.ValidateAge(age))
            {
                throw new ArgumentException(SR.Get(SRID.EnumInvalid, "VoiceAge"), nameof(age));
            }

            TTSVoice ttsVoice = VoiceSynthesizer.GetEngine(null, culture, gender, age, voiceAlternate, true);

            if (ttsVoice == null)
            {
                // No match - throw
                throw new InvalidOperationException(SR.Get(SRID.SynthesizerSetVoiceNoMatch));
            }
            VoiceSynthesizer.Voice = ttsVoice;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="textToSpeak"></param>
        /// <returns></returns>
        public Prompt SpeakAsync(string textToSpeak)
        {
            Helpers.ThrowIfNull(textToSpeak, nameof(textToSpeak));

            Prompt prompt = new(textToSpeak, SynthesisTextFormat.Text);
            SpeakAsync(prompt);
            return prompt;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="prompt"></param>
        /// <returns></returns>
        public void SpeakAsync(Prompt prompt)
        {
            Helpers.ThrowIfNull(prompt, nameof(prompt));

            prompt.Synthesizer = this;
            VoiceSynthesizer.SpeakAsync(prompt);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="textToSpeak"></param>
        /// <returns></returns>
        public Prompt SpeakSsmlAsync(string textToSpeak)
        {
            Helpers.ThrowIfNull(textToSpeak, nameof(textToSpeak));

            Prompt prompt = new(textToSpeak, SynthesisTextFormat.Ssml);
            SpeakAsync(prompt);
            return prompt;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="promptBuilder"></param>
        /// <returns></returns>
        public Prompt SpeakAsync(PromptBuilder promptBuilder)
        {
            Helpers.ThrowIfNull(promptBuilder, nameof(promptBuilder));

            Prompt prompt = new(promptBuilder);
            SpeakAsync(prompt);
            return prompt;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="textToSpeak"></param>
        /// <returns></returns>
        public void Speak(string textToSpeak)
        {
            Speak(new Prompt(textToSpeak, SynthesisTextFormat.Text));
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="prompt"></param>
        /// <returns></returns>
        public void Speak(Prompt prompt)
        {
            Helpers.ThrowIfNull(prompt, nameof(prompt));

            // Avoid a dead lock if the synthesizer is Paused
            if (State == SynthesizerState.Paused)
            {
                throw new InvalidOperationException(SR.Get(SRID.SynthesizerSyncSpeakWhilePaused));
            }

            prompt.Synthesizer = this;
            prompt._syncSpeak = true;
            VoiceSynthesizer.Speak(prompt);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="promptBuilder"></param>
        /// <returns></returns>
        public void Speak(PromptBuilder promptBuilder)
        {
            Speak(new Prompt(promptBuilder));
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="textToSpeak"></param>
        /// <returns></returns>
        public void SpeakSsml(string textToSpeak)
        {
            Speak(new Prompt(textToSpeak, SynthesisTextFormat.Ssml));
        }

        /// <summary>
        /// Pause the playback of all speech in this synthesizer.
        /// </summary>
        public void Pause()
        {
            // Increment the Paused count
            if (!_paused)
            {
                VoiceSynthesizer.Pause();
                _paused = true;
            }
        }

        /// <summary>
        /// Resume the playback of all speech in this synthesizer.
        /// </summary>
        public void Resume()
        {
            if (_paused)
            {
                VoiceSynthesizer.Resume();
                _paused = false;
            }
        }

        /// <summary>
        /// Cancel playback of all Prompts currently in the queue.
        /// </summary>
        /// <param name="prompt"></param>
        public void SpeakAsyncCancel(Prompt prompt)
        {
            Helpers.ThrowIfNull(prompt, nameof(prompt));

            VoiceSynthesizer.Abort(prompt);
        }

        /// <summary>
        /// Cancel playback of all Prompts currently in the queue.
        /// </summary>
        public void SpeakAsyncCancelAll()
        {
            VoiceSynthesizer.Abort();
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="path"></param>
        // The stream is disposed when the speech synthesizer is disposed
        public void SetOutputToWaveFile(string path)
        {
            Helpers.ThrowIfEmptyOrNull(path, nameof(path));

            SetOutputToNull();
            SetOutputStream(new FileStream(path, FileMode.Create, FileAccess.Write), null, true, true);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="path"></param>
        /// <param name="formatInfo"></param>
        // The stream is disposed when the speech synthesizer is disposed
        public void SetOutputToWaveFile(string path, SpeechAudioFormatInfo formatInfo)
        {
            Helpers.ThrowIfEmptyOrNull(path, nameof(path));
            Helpers.ThrowIfNull(formatInfo, nameof(formatInfo));

            SetOutputToNull();
            SetOutputStream(new FileStream(path, FileMode.Create, FileAccess.Write), formatInfo, true, true);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="audioDestination"></param>
        public void SetOutputToWaveStream(Stream audioDestination)
        {
            Helpers.ThrowIfNull(audioDestination, nameof(audioDestination));

            SetOutputStream(audioDestination, null, true, false);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="audioDestination"></param>
        /// <param name="formatInfo"></param>
        public void SetOutputToAudioStream(Stream audioDestination, SpeechAudioFormatInfo formatInfo)
        {
            Helpers.ThrowIfNull(audioDestination, nameof(audioDestination));
            Helpers.ThrowIfNull(formatInfo, nameof(formatInfo));

            SetOutputStream(audioDestination, formatInfo, false, false);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public void SetOutputToDefaultAudioDevice()
        {
            SetOutputStream(null, null, true, false);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        // The stream is disposed when the speech synthesizer is disposed
        public void SetOutputToNull()
        {
            // Close the existing stream
            if (_outputStream != Stream.Null)
            {
                VoiceSynthesizer.SetOutput(Stream.Null, null, true);
            }

            if (_outputStream != null)
            {
                if (_closeStreamOnExit)
                {
                    _outputStream.Close();
                }
            }
            _outputStream = Stream.Null;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        // Dynamic content, use a method instead of a property to denote that fact
        public Prompt GetCurrentlySpokenPrompt()
        {
            return VoiceSynthesizer.Prompt;
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public ReadOnlyCollection<InstalledVoice> GetInstalledVoices()
        {
            return VoiceSynthesizer.GetInstalledVoices(null);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public ReadOnlyCollection<InstalledVoice> GetInstalledVoices(CultureInfo culture)
        {
            Helpers.ThrowIfNull(culture, nameof(culture));

            if (culture.Equals(CultureInfo.InvariantCulture))
            {
                throw new ArgumentException(SR.Get(SRID.InvariantCultureInfo), nameof(culture));
            }

            return VoiceSynthesizer.GetInstalledVoices(culture);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="mediaType"></param>
        public void AddLexicon(Uri uri, string mediaType)
        {
            Helpers.ThrowIfNull(uri, nameof(uri));

            VoiceSynthesizer.AddLexicon(uri, mediaType);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="uri"></param>
        public void RemoveLexicon(Uri uri)
        {
            Helpers.ThrowIfNull(uri, nameof(uri));

            VoiceSynthesizer.RemoveLexicon(uri);
        }

        //*******************************************************************
        //
        // Public Events
        //
        //*******************************************************************

        #region public Events

        /// <summary>
        /// TODOC
        /// </summary>
        public event EventHandler<SpeakStartedEventArgs> SpeakStarted
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer._speakStarted += value;
            }
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer._speakStarted -= value;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public event EventHandler<SpeakCompletedEventArgs> SpeakCompleted
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer._speakCompleted += value;
            }
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer._speakCompleted -= value;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public event EventHandler<SpeakProgressEventArgs> SpeakProgress
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer.AddEvent<SpeakProgressEventArgs>(TtsEventId.WordBoundary, ref VoiceSynthesizer._speakProgress, value);
            }
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer.RemoveEvent<SpeakProgressEventArgs>(TtsEventId.WordBoundary, ref VoiceSynthesizer._speakProgress, value);
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public event EventHandler<BookmarkReachedEventArgs> BookmarkReached
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer.AddEvent<BookmarkReachedEventArgs>(TtsEventId.Bookmark, ref VoiceSynthesizer._bookmarkReached, value);
            }
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer.RemoveEvent<BookmarkReachedEventArgs>(TtsEventId.Bookmark, ref VoiceSynthesizer._bookmarkReached, value);
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public event EventHandler<VoiceChangeEventArgs> VoiceChange
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer.AddEvent<VoiceChangeEventArgs>(TtsEventId.VoiceChange, ref VoiceSynthesizer._voiceChange, value);
            }
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer.RemoveEvent<VoiceChangeEventArgs>(TtsEventId.VoiceChange, ref VoiceSynthesizer._voiceChange, value);
            }
        }


        #region WinFx

        /// <summary>
        /// TODOC
        /// </summary>
        public event EventHandler<PhonemeReachedEventArgs> PhonemeReached
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer.AddEvent<PhonemeReachedEventArgs>(TtsEventId.Phoneme, ref VoiceSynthesizer._phonemeReached, value);
            }
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer.RemoveEvent<PhonemeReachedEventArgs>(TtsEventId.Phoneme, ref VoiceSynthesizer._phonemeReached, value);
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public event EventHandler<VisemeReachedEventArgs> VisemeReached
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer.AddEvent<VisemeReachedEventArgs>(TtsEventId.Viseme, ref VoiceSynthesizer._visemeReached, value);
            }
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer.RemoveEvent<VisemeReachedEventArgs>(TtsEventId.Viseme, ref VoiceSynthesizer._visemeReached, value);
            }
        }

        #endregion


        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public event EventHandler<StateChangedEventArgs> StateChanged
        {
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            add
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer._stateChanged += value;
            }
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            remove
            {
                Helpers.ThrowIfNull(value, nameof(value));
                VoiceSynthesizer._stateChanged -= value;
            }
        }

        #endregion

        #endregion Events

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region public Properties

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public SynthesizerState State
        {
            get
            {
                return VoiceSynthesizer.State;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public int Rate
        {
            get
            {
                return VoiceSynthesizer.Rate;
            }
            set
            {
                if (value < -10 || value > 10)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Get(SRID.RateOutOfRange));
                }
                VoiceSynthesizer.Rate = value;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public int Volume
        {
            get
            {
                return VoiceSynthesizer.Volume;
            }
            set
            {
                if (value < 0 || value > 100)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.Get(SRID.ResourceUsageOutOfRange));
                }
                VoiceSynthesizer.Volume = value;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public VoiceInfo Voice
        {
            get
            {
                // Get the sapi voice
                return VoiceSynthesizer.CurrentVoice(true).VoiceInfo;
            }
        }

        //*******************************************************************
        //
        // Internal Properties
        //
        //*******************************************************************

        #region Internal Properties


        #endregion

        #endregion

        //*******************************************************************
        //
        // Private Methods
        //
        //*******************************************************************

        #region Private Methods

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="formatInfo"></param>
        /// <param name="headerInfo"></param>
        /// <param name="closeStreamOnExit"></param>
        private void SetOutputStream(Stream stream, SpeechAudioFormatInfo formatInfo, bool headerInfo, bool closeStreamOnExit)
        {
            SetOutputToNull();
            _outputStream = stream;
            _closeStreamOnExit = closeStreamOnExit;

            // Need to serialize into a proper wav file before closing the stream
            VoiceSynthesizer.SetOutput(stream, formatInfo, headerInfo);
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                if (_voiceSynthesis != null)
                {
                    // flag it first so asynchronous operation has more time to finish
                    _isDisposed = true;
                    SpeakAsyncCancelAll();
                    // Flush the Output stream
                    if (_outputStream != null)
                    {
                        if (_closeStreamOnExit)
                        {
                            _outputStream.Close();
                        }
                        else
                        {
                            _outputStream.Flush();
                        }
                        _outputStream = null;
                    }
                }
            }

            if (_voiceSynthesis != null)
            {
                // Terminate the background synthesis object the thread.
                _voiceSynthesis.Dispose();
                _voiceSynthesis = null;
            }

            _isDisposed = true;
        }

        #endregion

        //*******************************************************************
        //
        // Private Properties
        //
        //*******************************************************************

        #region Private Properties
        private VoiceSynthesis VoiceSynthesizer
        {
            get
            {
                if (_voiceSynthesis == null && _isDisposed)
                {
                    throw new ObjectDisposedException("SpeechSynthesizer");
                }
                if (_voiceSynthesis == null)
                {
                    WeakReference wr = new(this);
                    _voiceSynthesis = new VoiceSynthesis(wr);
                }
                return _voiceSynthesis;
            }
        }
        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        // SpVoice for this synthesizer
        private VoiceSynthesis _voiceSynthesis;

        // Is the object disposed?
        private bool _isDisposed;

        // Count of number of consecutive calls to Paused
        private bool _paused;

        // .Net Stream - keep a reference to it to avoid it to be GC
        private Stream _outputStream;

        // If stream were created in SpeechFx then close it, otherwise it should remain open.
        private bool _closeStreamOnExit;

        #endregion Fields
    }

    //*******************************************************************
    //
    // Public Enums
    //
    //*******************************************************************

    #region Public Enums

    /// <summary>
    /// TODOC
    /// </summary>
    public enum SynthesizerState
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Ready,
        /// <summary>
        /// TODOC
        /// </summary>
        Speaking,
        /// <summary>
        /// TODOC
        /// </summary>
        Paused
    }


    /// <summary>
    /// TODOC
    /// </summary>
    [Flags]
    public enum SynthesizerEmphasis
    {
        /// <summary>
        /// TODOC
        /// </summary>
        Stressed = 1,
        /// <summary>
        /// TODOC
        /// </summary>
        Emphasized = 2
    }

    #endregion
}
