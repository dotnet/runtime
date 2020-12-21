// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Speech.Internal;


namespace System.Speech.Recognition
{
    /// TODOC <_include file='doc\RecognitionResult.uex' path='docs/doc[@for="RecognizedWordUnit"]/*' />
    [Serializable]
    [DebuggerDisplay("Text: {Text}")]

    public class RecognizedWordUnit
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************
        #region Constructors

#pragma warning disable 6504
#pragma warning disable 6507

        // Constructor for recognized 'word'
        /// TODOC <_include file='doc\RecognitionResult.uex' path='docs/doc[@for="RecognizedWordUnit.ctor"]/*' />
        public RecognizedWordUnit(string text, float confidence, string pronunciation, string lexicalForm, DisplayAttributes displayAttributes, TimeSpan audioPosition, TimeSpan audioDuration)
        {
            if (lexicalForm == null)
            {
                throw new ArgumentNullException(nameof(lexicalForm));
            }

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

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************
        #region Public Properties
        // Spoken text of the word {No conversion to display form}
        /// TODOC <_include file='doc\RecognitionResult.uex' path='docs/doc[@for="RecognizedWordUnit.Text"]/*' />
        public string Text
        {
            get { return _text; }
        }

        // Confidence score
        /// TODOC <_include file='doc\RecognitionResult.uex' path='docs/doc[@for="RecognizedWordUnit.Confidence"]/*' />
        public float Confidence
        {
            get { return _confidence; }
        }

        /// TODOC <_include file='doc\RecognitionResult.uex' path='docs/doc[@for="RecognizedWordUnit.PronunciationString"]/*' />
        public string Pronunciation
        {
            get
            {
                return _pronunciation;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public string LexicalForm
        {
            get { return _lexicalForm; }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public DisplayAttributes DisplayAttributes
        {
            get { return _displayAttributes; }
        }

        #endregion

        //*******************************************************************
        //
        // Internal Methods
        //
        //*******************************************************************

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

        //*******************************************************************
        //
        // Internal Fields
        //
        //*******************************************************************

        #region Internal Fields

        internal TimeSpan _audioPosition;
        internal TimeSpan _audioDuration;

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private string _text;
        private string _lexicalForm;
        private float _confidence;
        private string _pronunciation;
        private DisplayAttributes _displayAttributes;

        #endregion
    }

    /// TODOC
    [Flags]
    public enum DisplayAttributes
    {
        /// TODOC
        None = 0x00,
        /// TODOC
        ZeroTrailingSpaces = 0x02,
        /// TODOC
        OneTrailingSpace = 0x04,
        /// TODOC
        TwoTrailingSpaces = 0x08,
        /// TODOC
        ConsumeLeadingSpaces = 0x10,
    }
}

