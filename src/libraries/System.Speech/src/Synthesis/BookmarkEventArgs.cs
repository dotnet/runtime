// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Synthesis
{
    public class BookmarkReachedEventArgs : PromptEventArgs
    {
        #region Constructors
        internal BookmarkReachedEventArgs(Prompt prompt, string bookmark, TimeSpan audioPosition)
            : base(prompt)
        {
            _bookmark = bookmark;
            _audioPosition = audioPosition;
        }

        #endregion

        #region public Properties
        public string Bookmark
        {
            get
            {
                return _bookmark;
            }
        }
        public TimeSpan AudioPosition
        {
            get
            {
                return _audioPosition;
            }
        }

        #endregion

        #region Private Fields

        private string _bookmark;

        // Audio and stream position
        private TimeSpan _audioPosition;

        #endregion
    }
}
