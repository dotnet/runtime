// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

namespace System.Speech.Recognition
{
    /// TODOC <_include file='doc\EmulateRecognizeCompletedEventArgs.uex' path='docs/doc[@for="EmulateRecognizeCompletedEventArgs"]/*' />
    public class EmulateRecognizeCompletedEventArgs : AsyncCompletedEventArgs
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        internal EmulateRecognizeCompletedEventArgs(RecognitionResult result, Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
            _result = result;
        }

        #endregion

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region Public Properties

        /// TODOC <_include file='doc\RecognizeCompletedEventArgs.uex' path='docs/doc[@for="RecognizeCompletedEventArgs.Result"]/*' />
        public RecognitionResult Result
        {
            get { return _result; }
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private RecognitionResult _result;

        #endregion
    }
}
