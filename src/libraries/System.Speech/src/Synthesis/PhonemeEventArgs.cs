// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.Synthesis
{
    /// <summary>
    /// Summary description for PhonemeEventArgs.
    /// </summary>
    public class PhonemeReachedEventArgs : PromptEventArgs
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
        /// <param name="prompt"></param>
        /// <param name="currentPhoneme"></param>
        /// <param name="audioPosition"></param>
        /// <param name="duration"></param>
        /// <param name="emphasis"></param>
        /// <param name="nextPhoneme"></param>
        internal PhonemeReachedEventArgs(Prompt prompt, string currentPhoneme, TimeSpan audioPosition, TimeSpan duration, SynthesizerEmphasis emphasis, string nextPhoneme) : base(prompt)
        {
            _currentPhoneme = currentPhoneme;
            _audioPosition = audioPosition;
            _duration = duration;
            _emphasis = emphasis;
            _nextPhoneme = nextPhoneme;
        }

        #endregion

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region Public Properties

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public string Phoneme
        {
            get { return _currentPhoneme; }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public TimeSpan AudioPosition
        {
            get { return _audioPosition; }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public TimeSpan Duration
        {
            get { return _duration; }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public SynthesizerEmphasis Emphasis
        {
            get { return _emphasis; }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public string NextPhoneme
        {
            get { return _nextPhoneme; }
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

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
