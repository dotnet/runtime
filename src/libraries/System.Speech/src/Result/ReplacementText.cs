// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Speech.Internal.SapiInterop;
using System.Speech.Internal.SrgsCompiler;
using System.Speech.Internal.SrgsParser;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace System.Speech.Recognition
{
    /// <summary>
    /// TODOC
    /// </summary>
    [Serializable]
    [StructLayout (LayoutKind.Sequential)]
    
    public class ReplacementText
    {
        //*******************************************************************
        //
        // Constructors
        //
        //*******************************************************************

        #region Constructors

        internal ReplacementText (DisplayAttributes displayAttributes, string text, int wordIndex, int countOfWords)
        {
            _displayAttributes = displayAttributes;
            _text = text;
            _wordIndex = wordIndex;
            _countOfWords = countOfWords;
        }

        #endregion

        //*******************************************************************
        //
        // Public Properties
        //
        //*******************************************************************

        #region Public Properties

        /// <summary>
        /// TODOC
        /// </summary>
        public DisplayAttributes DisplayAttributes
        {
            get
            {
                return _displayAttributes;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public string Text
        {
            get
            {
                return _text;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public int FirstWordIndex
        {
            get
            {
                return _wordIndex;
            }
        }

        /// <summary>
        /// TODOC
        /// </summary>
        public int CountOfWords
        {
            get
            {
                return _countOfWords;
            }
        }

        #endregion

        //*******************************************************************
        //
        // Private Fields
        //
        //*******************************************************************

        #region Private Fields

        private DisplayAttributes _displayAttributes;
        private string _text;
        private int _wordIndex;
        private int _countOfWords;

        #endregion
    }
}
