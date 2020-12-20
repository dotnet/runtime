// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.Recognition
{

    /// TODOC <_include file='doc\AudioStatusChangedEventArgs.uex' path='docs/doc[@for="AudioStatusChangedEventArgs"]/*' />
    // EventArgs used in the AudioLevelUpdatedEventArgs event.
    
    public class AudioLevelUpdatedEventArgs : EventArgs
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        internal AudioLevelUpdatedEventArgs(int audioLevel)
        {
            _audioLevel = audioLevel;
        }

        #endregion



        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region public Properties

        /// TODOC <_include file='doc\AudioStatusChangedEventArgs.uex' path='docs/doc[@for="AudioStatusChangedEventArgs.AudioStatus"]/*' />
        public int AudioLevel
        {
            get { return _audioLevel; }
        }

        #endregion



        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private int _audioLevel;

        #endregion
    }

}
