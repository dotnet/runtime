// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Recognition
{
    // EventArgs used in the AudioStateChangedEventArgs event.

    public class AudioStateChangedEventArgs : EventArgs
    {
        #region Constructors

        internal AudioStateChangedEventArgs(AudioState audioState)
        {
            _audioState = audioState;
        }

        #endregion

        #region public Properties
        public AudioState AudioState
        {
            get { return _audioState; }
        }

        #endregion

        #region Private Fields

        private AudioState _audioState;

        #endregion
    }
}
