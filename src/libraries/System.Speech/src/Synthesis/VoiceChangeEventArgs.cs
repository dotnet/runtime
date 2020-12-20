// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.Synthesis
{
    /// <summary>
    /// TODOC - Summary description for VoiceChangeEventArgs.
    /// </summary>
    public class VoiceChangeEventArgs : PromptEventArgs
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
        /// <param name="voice"></param>
        internal VoiceChangeEventArgs(Prompt prompt, VoiceInfo voice) : base(prompt)
        {
            _voice = voice;
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
        public VoiceInfo Voice
        {
            get
            {
                return _voice;
            }
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private VoiceInfo _voice;

        #endregion
    }
}
