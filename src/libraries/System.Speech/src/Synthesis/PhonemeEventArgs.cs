// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Synthesis
{
    public class PhonemeReachedEventArgs : PromptEventArgs
    {
        #region Constructors
        internal PhonemeReachedEventArgs(Prompt prompt, string currentPhoneme, TimeSpan audioPosition, TimeSpan duration, SynthesizerEmphasis emphasis, string nextPhoneme) : base(prompt)
        {
            _currentPhoneme = currentPhoneme;
            _audioPosition = audioPosition;
            _duration = duration;
            _emphasis = emphasis;
            _nextPhoneme = nextPhoneme;
        }

        #endregion

        #region Public Properties
        public string Phoneme
        {
            get { return _currentPhoneme; }
        }
        public TimeSpan AudioPosition
        {
            get { return _audioPosition; }
        }
        public TimeSpan Duration
        {
            get { return _duration; }
        }
        public SynthesizerEmphasis Emphasis
        {
            get { return _emphasis; }
        }
        public string NextPhoneme
        {
            get { return _nextPhoneme; }
        }

        #endregion

        #region Private Fields

        // Current phoneme being synthesized
        private string _currentPhoneme;

        // Audio position of current phoneme
        private TimeSpan _audioPosition;

        // Duration of current phoneme
        private TimeSpan _duration;

        // Features of the current phoneme
        private SynthesizerEmphasis _emphasis;

        // Next phoneme to be synthesized
        private string _nextPhoneme;

        #endregion
    }
}
