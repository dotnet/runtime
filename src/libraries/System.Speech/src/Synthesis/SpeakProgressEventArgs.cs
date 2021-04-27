// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Synthesis
{
    public class SpeakProgressEventArgs : PromptEventArgs
    {
        #region Constructors
        internal SpeakProgressEventArgs(Prompt prompt, TimeSpan audioPosition, int iWordPos, int cWordLen) : base(prompt)
        {
            _audioPosition = audioPosition;
            _iWordPos = iWordPos;
            _cWordLen = cWordLen;
        }
        #endregion

        #region public Properties
        public TimeSpan AudioPosition
        {
            get
            {
                return _audioPosition;
            }
        }
        public int CharacterPosition
        {
            get
            {
                return _iWordPos;
            }
        }
        public int CharacterCount
        {
            get
            {
                return _cWordLen;
            }
            internal set
            {
                _cWordLen = value;
            }
        }
        public string Text
        {
            get
            {
                return _word;
            }
            internal set
            {
                _word = value;
            }
        }

        #endregion

        #region Private Fields

        private TimeSpan _audioPosition;
        private int _iWordPos;
        private int _cWordLen;
        private string _word;

        #endregion
    }
}
