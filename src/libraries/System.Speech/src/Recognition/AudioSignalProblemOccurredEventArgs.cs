// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.Recognition
{
    /// TODOC <_include file='doc\AudioStatusChangedEventArgs.uex' path='docs/doc[@for="AudioStatusChangedEventArgs"]/*' />
    // EventArgs used in the AudioSignalProblemOccurredEventArgs event.

    public class AudioSignalProblemOccurredEventArgs : EventArgs
    {
        #region Constructors

        internal AudioSignalProblemOccurredEventArgs(AudioSignalProblem audioSignalProblem, int audioLevel, TimeSpan audioPosition, TimeSpan recognizerPosition)
        {
            _audioSignalProblem = audioSignalProblem;
            _audioLevel = audioLevel;
            _audioPosition = audioPosition;
            _recognizerPosition = recognizerPosition;
        }

        #endregion

        #region public Properties

        /// TODOC
        public AudioSignalProblem AudioSignalProblem
        {
            get { return _audioSignalProblem; }
        }

        /// TODOC
        public int AudioLevel
        {
            get { return _audioLevel; }
        }

        /// TODOC
        public TimeSpan AudioPosition
        {
            get { return _audioPosition; }
        }

        /// TODOC
        public TimeSpan RecognizerAudioPosition
        {
            get { return _recognizerPosition; }
        }

        #endregion

        #region Private Fields

        private AudioSignalProblem _audioSignalProblem;
        private TimeSpan _recognizerPosition;
        private TimeSpan _audioPosition;
        private int _audioLevel;

        #endregion
    }
}
