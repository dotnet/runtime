// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Recognition
{
    // EventArgs used in the SpeechRecognizer.StateChanged event.

    public class StateChangedEventArgs : EventArgs
    {
        #region Constructors

        internal StateChangedEventArgs(RecognizerState recognizerState)
        {
            _recognizerState = recognizerState;
        }

        #endregion

        #region public Properties
        public RecognizerState RecognizerState
        {
            get { return _recognizerState; }
        }

        #endregion

        #region Private Fields

        private RecognizerState _recognizerState;

        #endregion
    }
}
