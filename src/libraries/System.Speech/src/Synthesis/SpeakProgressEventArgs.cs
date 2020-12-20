// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.Synthesis
{
    /// <summary>
    /// TODOC - Summary description for SpeakProgressEventArgs.
    /// </summary>
    public class SpeakProgressEventArgs : PromptEventArgs
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="audioPosition"></param>
        /// <param name="iWordPos"></param>
        /// <param name="cWordLen"></param>
        internal SpeakProgressEventArgs(Prompt prompt, TimeSpan audioPosition, int iWordPos, int cWordLen) : base(prompt)
        {
            _audioPosition = audioPosition;
            _iWordPos = iWordPos;
            _cWordLen = cWordLen;
        }
        #endregion

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region public Properties

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

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public int CharacterPosition
        {
            get
            {
                return _iWordPos;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public int CharacterCount
        {
            get
            {
                return _cWordLen;
            }
            internal set
            {
                _cWordLen = value;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public string Text
        {
            get
            {
                return _word;
            }
            internal set
            {
                _word = value;
            }
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private TimeSpan _audioPosition;
        private int _iWordPos;
        private int _cWordLen;
        private string _word;

        #endregion
    }
}
