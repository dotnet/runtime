// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Recognition
{
    // EventArgs used in the SpeechDetected event.

    public class SpeechDetectedEventArgs : EventArgs
    {
        #region Constructors

        internal SpeechDetectedEventArgs(TimeSpan audioPosition)
        {
            _audioPosition = audioPosition;
        }

        #endregion

        #region public Properties
        public TimeSpan AudioPosition
        {
            get { return _audioPosition; }
        }

        #endregion

        #region Private Fields

        private TimeSpan _audioPosition;

        #endregion
    }
}
