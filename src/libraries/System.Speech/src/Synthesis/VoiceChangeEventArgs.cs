// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Synthesis
{
    public class VoiceChangeEventArgs : PromptEventArgs
    {
        #region Constructors
        internal VoiceChangeEventArgs(Prompt prompt, VoiceInfo voice) : base(prompt)
        {
            _voice = voice;
        }

        #endregion

        #region public Properties
        public VoiceInfo Voice
        {
            get
            {
                return _voice;
            }
        }

        #endregion

        #region Private Fields

        private VoiceInfo _voice;

        #endregion
    }
}
