// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Synthesis
{
    public class VisemeReachedEventArgs : PromptEventArgs
    {
        #region Constructors
        internal VisemeReachedEventArgs(Prompt speakPrompt, int currentViseme, TimeSpan audioPosition, TimeSpan duration, SynthesizerEmphasis emphasis, int nextViseme) : base(speakPrompt)
        {
            _currentViseme = currentViseme;
            _audioPosition = audioPosition;
            _duration = duration;
            _emphasis = emphasis;
            _nextViseme = nextViseme;
        }

        #endregion

        #region Public Properties
        public int Viseme
        {
            get { return _currentViseme; }
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
        public int NextViseme
        {
            get { return _nextViseme; }
        }

        #endregion

        #region Private Fields

        // Current Viseme being synthesized
        private int _currentViseme;

        // Audio position of current phoneme
        private TimeSpan _audioPosition;

        // Duration of current Viseme
        private TimeSpan _duration;

        // Features of the current phoneme
        private SynthesizerEmphasis _emphasis;

        // Next Viseme to be synthesized
        private int _nextViseme;

        #endregion
    }
}
