// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Speech.Recognition
{
    [Serializable]
    public abstract class RecognitionEventArgs : EventArgs
    {
        #region Constructors
        internal RecognitionEventArgs(RecognitionResult result)
        {
            _result = result;
        }
        #endregion

        #region Public Properties
        // All this class has is a property to access the main result.
        public RecognitionResult Result
        {
            get { return _result; }
        }
        #endregion

        #region Private Fields
        private RecognitionResult _result;
        #endregion
    }
    [Serializable]
    public class SpeechRecognizedEventArgs : RecognitionEventArgs
    {
        #region Constructors

        internal SpeechRecognizedEventArgs(RecognitionResult result)
            : base(result)
        {
        }

        #endregion
    }
    [Serializable]
    public class SpeechRecognitionRejectedEventArgs : RecognitionEventArgs
    {
        #region Constructors

        internal SpeechRecognitionRejectedEventArgs(RecognitionResult result)
            : base(result)
        {
        }

        #endregion
    }
    [Serializable]
    public class SpeechHypothesizedEventArgs : RecognitionEventArgs
    {
        #region Constructors

        internal SpeechHypothesizedEventArgs(RecognitionResult result)
            : base(result)
        {
        }

        #endregion
    }
}
