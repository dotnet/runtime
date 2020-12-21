// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

namespace System.Speech.Synthesis
{
    /// <summary>
    /// TODOC
    /// </summary>
    public abstract class PromptEventArgs : AsyncCompletedEventArgs
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
        internal PromptEventArgs(Prompt prompt) : base(prompt._exception, prompt._exception != null, prompt)
        {
            _prompt = prompt;
        }

        #endregion

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region public Properties

        // Use Add* naming convention.

        /// <summary>
        /// TODOC
        /// </summary>
        /// <value></value>
        public Prompt Prompt
        {
            get
            {
                return _prompt;
            }
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private Prompt _prompt;

        #endregion
    }
    /// <summary>
    /// TODOC
    /// </summary>
    public class SpeakStartedEventArgs : PromptEventArgs
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
        internal SpeakStartedEventArgs(Prompt prompt)
            : base(prompt)
        {
        }

        #endregion
    }
}
