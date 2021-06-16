// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Recognition
{
    // Event args used in the RecognizerUpdateReached event, which is raised after a call is made to RequestRecognizerUpdate.

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
        public object UserToken
        {
            get { return _userToken; }
        }
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
