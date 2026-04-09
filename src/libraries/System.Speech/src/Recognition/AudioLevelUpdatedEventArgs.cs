// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Recognition
{
    // EventArgs used in the AudioLevelUpdatedEventArgs event.

    public class AudioLevelUpdatedEventArgs : EventArgs
    {
        #region Constructors

        internal AudioLevelUpdatedEventArgs(int audioLevel)
        {
            _audioLevel = audioLevel;
        }

        #endregion

        #region public Properties
        public int AudioLevel
        {
            get { return _audioLevel; }
        }

        #endregion

        #region Private Fields

        private int _audioLevel;

        #endregion
    }
}
