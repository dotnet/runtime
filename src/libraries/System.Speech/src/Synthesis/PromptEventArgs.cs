// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Speech.Synthesis
{
    public abstract class PromptEventArgs : AsyncCompletedEventArgs
    {
        #region Constructors
        internal PromptEventArgs(Prompt prompt) : base(prompt.Exception, prompt.Exception != null, prompt)
        {
            _prompt = prompt;
        }

        #endregion

        #region public Properties

        // Use Add* naming convention.
        public Prompt Prompt
        {
            get
            {
                return _prompt;
            }
        }

        #endregion

        #region Private Fields

        private Prompt _prompt;

        #endregion
    }
    public class SpeakStartedEventArgs : PromptEventArgs
    {
        #region Constructors
        internal SpeakStartedEventArgs(Prompt prompt)
            : base(prompt)
        {
        }

        #endregion
    }
}
