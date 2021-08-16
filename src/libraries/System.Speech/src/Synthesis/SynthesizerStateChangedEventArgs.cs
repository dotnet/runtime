// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Synthesis
{
    public class StateChangedEventArgs : EventArgs
    {
        #region Constructors
        internal StateChangedEventArgs(SynthesizerState state, SynthesizerState previousState)
        {
            _state = state;
            _previousState = previousState;
        }

        #endregion

        #region public Properties

        // Use Add* naming convention.
        public SynthesizerState State
        {
            get
            {
                return _state;
            }
        }
        public SynthesizerState PreviousState
        {
            get
            {
                return _previousState;
            }
        }

        #endregion

        #region Private Fields

        private SynthesizerState _state;

        private SynthesizerState _previousState;

        #endregion
    }
}
