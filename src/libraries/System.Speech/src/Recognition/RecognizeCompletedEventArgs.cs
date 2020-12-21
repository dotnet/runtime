// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

namespace System.Speech.Recognition
{
    /// TODOC <_include file='doc\RecognizeCompletedEventArgs.uex' path='docs/doc[@for="RecognizeCompletedEventArgs"]/*' />

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

        /// TODOC <_include file='doc\RecognizeCompletedEventArgs.uex' path='docs/doc[@for="RecognizeCompletedEventArgs.Result"]/*' />
        public RecognitionResult Result
        {
            get { return _result; }
        }

        /// TODOC <_include file='doc\RecognizeCompletedEventArgs.uex' path='docs/doc[@for="RecognizeCompletedEventArgs.SilenceTimeout"]/*' />
        public bool InitialSilenceTimeout
        {
            get { return _initialSilenceTimeout; }
        }

        /// TODOC <_include file='doc\RecognizeCompletedEventArgs.uex' path='docs/doc[@for="RecognizeCompletedEventArgs.BabbleTimeout"]/*' />
        public bool BabbleTimeout
        {
            get { return _babbleTimeout; }
        }

        /// TODOC <_include file='doc\RecognizeCompletedEventArgs.uex' path='docs/doc[@for="RecognizeCompletedEventArgs.BabbleTimeout"]/*' />
        public bool InputStreamEnded
        {
            get { return _inputStreamEnded; }
        }

        /// TODOC <_include file='doc\RecognizeCompletedEventArgs.uex' path='docs/doc[@for="RecognizeCompletedEventArgs.AudioPosition"]/*' />
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
