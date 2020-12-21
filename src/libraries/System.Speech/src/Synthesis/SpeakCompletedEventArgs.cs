// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.Synthesis
{
    /// <summary>
    /// TODOC - Summary description for SpeakProgressEventArgs.
    /// </summary>
    public class SpeakCompletedEventArgs : PromptEventArgs
    {

        #region Constructors

        /// <summary>
        /// TODOC
        /// </summary>
        /// <param name="prompt"></param>
        internal SpeakCompletedEventArgs(Prompt prompt) : base(prompt)
        {
        }

        #endregion
    }
}
