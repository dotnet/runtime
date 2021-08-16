// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Speech.Recognition
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]

    public class ReplacementText
    {
        #region Constructors

        internal ReplacementText(DisplayAttributes displayAttributes, string text, int wordIndex, int countOfWords)
        {
            _displayAttributes = displayAttributes;
            _text = text;
            _wordIndex = wordIndex;
            _countOfWords = countOfWords;
        }

        #endregion

        #region Public Properties
        public DisplayAttributes DisplayAttributes
        {
            get
            {
                return _displayAttributes;
            }
        }
        public string Text
        {
            get
            {
                return _text;
            }
        }
        public int FirstWordIndex
        {
            get
            {
                return _wordIndex;
            }
        }
        public int CountOfWords
        {
            get
            {
                return _countOfWords;
            }
        }

        #endregion

        #region Private Fields

        private DisplayAttributes _displayAttributes;
        private string _text;
        private int _wordIndex;
        private int _countOfWords;

        #endregion
    }
}
