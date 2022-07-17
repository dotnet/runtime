// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Speech.Recognition
{
    [Serializable]
    [DebuggerDisplay("Text: {Text}")]

    public class RecognizedWordUnit
    {
        #region Constructors

#pragma warning disable 6504
#pragma warning disable 6507

        // Constructor for recognized 'word'
        public RecognizedWordUnit(string text, float confidence, string pronunciation, string lexicalForm, DisplayAttributes displayAttributes, TimeSpan audioPosition, TimeSpan audioDuration)
        {
            ArgumentNullException.ThrowIfNull(lexicalForm);

            if (confidence < 0.0f || confidence > 1.0f)
            {
                throw new ArgumentOutOfRangeException(SR.Get(SRID.InvalidConfidence));
            }

            _text = text == null || text.Length == 0 ? null : text;
            _confidence = confidence;
            _pronunciation = pronunciation == null || pronunciation.Length == 0 ? null : pronunciation;
            _lexicalForm = lexicalForm;
            _displayAttributes = displayAttributes;
            _audioPosition = audioPosition;
            _audioDuration = audioDuration;
        }

#pragma warning restore 6504
#pragma warning restore 6507

        #endregion

        #region Public Properties
        // Spoken text of the word {No conversion to display form}
        public string Text
        {
            get { return _text; }
        }

        // Confidence score
        public float Confidence
        {
            get { return _confidence; }
        }
        public string Pronunciation
        {
            get
            {
                return _pronunciation;
            }
        }
        public string LexicalForm
        {
            get { return _lexicalForm; }
        }
        public DisplayAttributes DisplayAttributes
        {
            get { return _displayAttributes; }
        }

        #endregion

        #region Internal Methods

        internal static byte DisplayAttributesToSapiAttributes(DisplayAttributes displayAttributes)
        {
            return (byte)((uint)displayAttributes >> 1);
        }

        internal static DisplayAttributes SapiAttributesToDisplayAttributes(byte sapiAttributes)
        {
            return (DisplayAttributes)(sapiAttributes << 1);
        }

        #endregion

        #region Internal Fields

        internal TimeSpan _audioPosition;
        internal TimeSpan _audioDuration;

        #endregion

        #region Private Fields

        private string _text;
        private string _lexicalForm;
        private float _confidence;
        private string _pronunciation;
        private DisplayAttributes _displayAttributes;

        #endregion
    }
    [Flags]
    public enum DisplayAttributes
    {
        None = 0x00,
        ZeroTrailingSpaces = 0x02,
        OneTrailingSpace = 0x04,
        TwoTrailingSpaces = 0x08,
        ConsumeLeadingSpaces = 0x10,
    }
}
