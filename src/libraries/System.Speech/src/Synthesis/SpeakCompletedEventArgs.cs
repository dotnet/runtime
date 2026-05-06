// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Synthesis
{
    public class SpeakCompletedEventArgs : PromptEventArgs
    {
        #region Constructors
        internal SpeakCompletedEventArgs(Prompt prompt) : base(prompt)
        {
        }

        #endregion
    }
}
