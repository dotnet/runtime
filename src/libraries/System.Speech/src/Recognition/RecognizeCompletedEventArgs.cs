// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Speech.Recognition
{
    public class RecognizeCompletedEventArgs : AsyncCompletedEventArgs
    {
        #region Constructors

        internal RecognizeCompletedEventArgs(RecognitionResult result, bool initialSilenceTimeout, bool babbleTimeout,
            bool inputStreamEnded, TimeSpan audioPosition,
            Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            _result = result;
            _initialSilenceTimeout = initialSilenceTimeout;
            _babbleTimeout = babbleTimeout;
            _inputStreamEnded = inputStreamEnded;
            _audioPosition = audioPosition;
        }

        #endregion

        #region Public Properties
        public RecognitionResult Result
        {
            get { return _result; }
        }
        public bool InitialSilenceTimeout
        {
            get { return _initialSilenceTimeout; }
        }
        public bool BabbleTimeout
        {
            get { return _babbleTimeout; }
        }
        public bool InputStreamEnded
        {
            get { return _inputStreamEnded; }
        }
        public TimeSpan AudioPosition
        {
            get { return _audioPosition; }
        }

        #endregion

        #region Private Fields

        private RecognitionResult _result;
        private bool _initialSilenceTimeout;
        private bool _babbleTimeout;
        private bool _inputStreamEnded;
        private TimeSpan _audioPosition;

        #endregion
    }
}
