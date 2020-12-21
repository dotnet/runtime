// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.Recognition
{
    // Event args used in the RecognizerUpdateReached event, which is raised after a call is made to RequestRecognizerUpdate.
    /// TODOC <_include file='doc\RecognizerBase.uex' path='docs/doc[@for="UpdateEventArgs"]/*' />

    public class RecognizerUpdateReachedEventArgs : EventArgs
    {

        #region Constructors

        internal RecognizerUpdateReachedEventArgs(object userToken, TimeSpan audioPosition)
        {
            _userToken = userToken;
            _audioPosition = audioPosition;
        }

        #endregion


        #region Public Properties

        // Application supplied object reference.
        /// TODOC <_include file='doc\RecognizerBase.uex' path='docs/doc[@for="UpdateEventArgs.UserToken"]/*' />
        public object UserToken
        {
            get { return _userToken; }
        }

        /// TODOC <_include file='doc\RecognizerBase.uex' path='docs/doc[@for="UpdateEventArgs.AudioPosition"]/*' />
        public TimeSpan AudioPosition
        {
            get { return _audioPosition; }
        }

        #endregion


        #region Private Fields

        private object _userToken;
        private TimeSpan _audioPosition;

        #endregion
    }
}
