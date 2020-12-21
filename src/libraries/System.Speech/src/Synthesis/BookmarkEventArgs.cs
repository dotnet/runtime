// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.Synthesis
{
    /// <summary>
    /// TODOC - Summary description for BookmarkEventArgs.
    /// </summary>
    public class BookmarkReachedEventArgs : PromptEventArgs
    {
        #region Constructors

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="bookmark"></param>
        /// <param name="audioPosition"></param>
        internal BookmarkReachedEventArgs(Prompt prompt, string bookmark, TimeSpan audioPosition)
            : base(prompt)
        {
            _bookmark = bookmark;
            _audioPosition = audioPosition;
        }

        #endregion

        #region public Properties

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public string Bookmark
        {
            get
            {
                return _bookmark;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
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
